namespace PDF.Data.Extractor.Strategies
{
    using global::Data.Block.Abstractions;
    using global::Data.Block.Abstractions.MetaData;

    using iText.Kernel.Geom;
    using iText.Kernel.Pdf;
    using iText.Kernel.Pdf.Canvas.Parser;
    using iText.Kernel.Pdf.Canvas.Parser.Data;
    using iText.Kernel.Pdf.Canvas.Parser.Listener;

    using Microsoft.Extensions.Logging;

    using PDF.Data.Extractor.Extensions;
    using PDF.Data.Extractor.Services;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using Vector = iText.Kernel.Geom.Vector;

    /// <summary>
    /// Strategy used to extract pdf <see cref="DataBlock"/>
    /// </summary>
    /// <seealso cref="IEventListener" />
    public sealed class DataBlockExtractStrategy : IEventListener
    {
        #region Fields

        private readonly IImageManager _imageManager;
        private readonly IFontManager _fontManager;
        private readonly CancellationToken _token;
        private readonly IReadOnlyDictionary<Type, Action<IEventData>> _dataTypeProcessor;
        private readonly Rectangle _pageSize;
        private readonly bool _skipImage;
        private readonly ILogger _logger;
        private readonly PdfPage _page;

        private static readonly ICollection<EventType> s_eventTypeManaged;

        private readonly List<DataBlockBuilder> _roots;

        private DataBlockBuilder? _currentBlockBuilder;

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes the <see cref="DataBlockExtractStrategy"/> class.
        /// </summary>
        static DataBlockExtractStrategy()
        {
            s_eventTypeManaged = new[]
            {
                EventType.BEGIN_TEXT,
                EventType.END_TEXT,
                EventType.RENDER_TEXT,
                EventType.RENDER_IMAGE
            };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataBlockExtractStrategy"/> class.
        /// </summary>
        public DataBlockExtractStrategy(IFontManager fontManager,
                                        IImageManager imageManager,
                                        CancellationToken token,
                                        ILogger logger,
                                        PdfPage page,
                                        bool skipImage)
        {
            this._skipImage = skipImage;
            this._logger = logger;
            this._token = token;
            this._pageSize = page.GetPageSize();
            this._page = page;
            this._fontManager = fontManager;
            this._imageManager = imageManager;
            this._roots = new List<DataBlockBuilder>();

            this._dataTypeProcessor = new Dictionary<Type, Action<IEventData>>()
            {
                { typeof(TextRenderInfo), (IEventData data) => DataTextBlockExtraction((TextRenderInfo)data) },
                { typeof(ImageRenderInfo), (IEventData data) => DataImageBlockExtraction((ImageRenderInfo)data) },
            };
        }

        #endregion

        #region Methods

        /// <inheritdoc />
        public void EventOccurred(IEventData data, EventType type)
        {
            this._token.ThrowIfCancellationRequested();

            try
            {
                if (type == EventType.BEGIN_TEXT)
                {
                    Debug.Assert(data == null);

                    var newBlock = new DataBlockBuilder(this._currentBlockBuilder);

                    if (this._currentBlockBuilder == null)
                        this._roots.Add(newBlock);

                    this._currentBlockBuilder = newBlock;
                    return;
                }

                if (type == EventType.END_TEXT)
                {
                    this._currentBlockBuilder = this._currentBlockBuilder?.Parent;
                    return;
                }

                if (this._dataTypeProcessor.TryGetValue(data.GetType(), out var builder))
                {
                    builder(data);
                    return;
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "EventType " + type + " {exception}", ex);
                return;
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates the <see cref="DataPageBlock"/> from block analyzed
        /// </summary>
        public Task<DataPageBlock> Compile(int pageNumber,
                                           CancellationToken token,
                                           params IDataBlockMergeStrategy[] strategies)
        {
            return Compile(pageNumber, token, strategies, Array.Empty<IDataBlockMergeStrategy>());
        }

        /// <summary>
        /// Creates the <see cref="DataPageBlock"/> from block analyzed
        /// </summary>
        public async Task<DataPageBlock> Compile(int pageNumber,
                                                 CancellationToken token,
                                                 IReadOnlyCollection<IDataBlockMergeStrategy> strategies,
                                                 IReadOnlyCollection<IDataBlockMergeStrategy> relationStrategies)
        {
            var pageSize = this._page.GetPageSize();

            var pageBlocks = CompileDataBlocks(strategies, token);

            var relations = await ComputeBlockRelation(pageBlocks, relationStrategies, token);

            return new DataPageBlock(Guid.NewGuid(),
                                     pageNumber,
                                     this._page.GetRotation(),
                                     new BlockArea(new BlockPoint(pageSize.GetLeft(), pageSize.GetBottom()),
                                                   new BlockPoint(pageSize.GetRight(), pageSize.GetBottom()),
                                                   new BlockPoint(pageSize.GetRight(), pageSize.GetTop()),
                                                   new BlockPoint(pageSize.GetLeft(), pageSize.GetTop())),
                                     relations,
                                     pageBlocks.OfType<DataBlock>()?.ToArray());
        }

        /// <inheritdoc />
        public ICollection<EventType> GetSupportedEvents()
        {
            return s_eventTypeManaged;
        }

        #region Tools

        /// <summary>
        /// Computes the block relation.
        /// </summary>
        private Task<IReadOnlyCollection<DataRelationBlock>> ComputeBlockRelation(IReadOnlyCollection<IDataBlock> pageBlocks,
                                                                                  IReadOnlyCollection<IDataBlockMergeStrategy> relationStrategies,
                                                                                  CancellationToken token)
        {


            IReadOnlyCollection<(IDataBlockMergeStrategy Strategy, BlockRelationTypeEnum BlockRelation, string CustomRelationType, double Weight)> stategies;

            if (relationStrategies is not null && relationStrategies.Any())
            {
                stategies = relationStrategies.Select(r => (Strategy: r, BlockRelation: BlockRelationTypeEnum.Group, CustomRelationType: "", Weight: (double)1)).ToArray();
            }
            else
            {
                var createTextCloseGroup = new DataTextBlockProximityStrategy(compareFontInfo: false,
                                                              horizontalDistanceTolerance: 0.2f,
                                                              verticalDistanceTolerance: 0.6f);

                stategies = new (IDataBlockMergeStrategy Strategy, BlockRelationTypeEnum BlockRelation, string CustomRelationType, double Weight)[]
                {
                    //(new DataBlockMergePDFTextBoxStrategy(), BlockRelationTypeEnum.SectionId, "TextBoxId", 0.4f),
                    //(new DataTextBlockProximityStrategy(), BlockRelationTypeEnum.Group, "ProximityDefault", 0.7f),
                    (new DataTextBlockOverlapStrategy(compareFontInfo: false), BlockRelationTypeEnum.Group, "OverlapWithoutFont", 0.8f)
                };
            }

            return ComputeBlockRelation(pageBlocks, token, stategies);
        }

        /// <summary>
        /// Computes the block relation.
        /// </summary>
        private async Task<IReadOnlyCollection<DataRelationBlock>> ComputeBlockRelation(IReadOnlyCollection<IDataBlock> pageBlocks,
                                                                                        CancellationToken token,
                                                                                        IReadOnlyCollection<(IDataBlockMergeStrategy Strategy,
                                                                                                             BlockRelationTypeEnum BlockRelation,
                                                                                                             string CustomRelationType,
                                                                                                             double Weight)> groupStrategies)
        {
            var allBlocks = pageBlocks.GetTreeElement(b => b.Children)
                                      .Distinct()
                                      .OfType<IDataBlock>()
                                      .ToArray();

            var groupingTasks = groupStrategies.Select(kv =>
            {
                return Task.Run(() =>
                {
                    // Apply on compile data
                    var grp = new[] { kv.Strategy }.Apply(allBlocks, token, breakOnFirstLoop: false);
                    token.ThrowIfCancellationRequested();
                    return ConstructRelationStruct(grp, kv.BlockRelation, kv.CustomRelationType, kv.Weight);
                });
            }).ToArray();

            var relations = (await Task.WhenAll(groupingTasks)).SelectMany(m => m).Distinct().ToArray();

            return relations ?? Array.Empty<DataRelationBlock>();
        }

        private static IReadOnlyCollection<DataRelationBlock> ConstructRelationStruct(IReadOnlyCollection<IDataBlock> resultStrategyGrp,
                                                                                      BlockRelationTypeEnum blockRelation,
                                                                                      string customRelationType,
                                                                                      double weigth)
        {
            if (resultStrategyGrp.Count == 0)
                return Array.Empty<DataRelationBlock>();

            var grpHost = (DataTextBlockGroup.PullGroupItems(resultStrategyGrp.Count)).ToArray();

            try
            {
                var grpQueue = new Queue<DataTextBlockGroup>(grpHost);

                return resultStrategyGrp.OfType<IDataTextBlock>()
                                        .Where(r => r.Children is not null && r.Children.Any())
                                        .Select(r =>
                                        {
                                            var items = r.Children.GetTreeElement(g => g.Children)
                                                                  .Append((DataBlock)r)
                                                                  .Where(c => c!.Children is null)
                                                                  .GroupBy(k => k!.Uid)
                                                                  .Select(grp => grp.First())
                                                                  .ToArray();

                                            var grp = grpQueue.Dequeue();

                                            foreach (var txt in items.OfType<IDataTextBlock>())
                                                grp.Push(txt);

                                            var compile = grp.Compile();

                                            return new DataRelationBlock(Guid.NewGuid(),
                                                                         compile.Area,
                                                                         blockRelation,
                                                                         customRelationType,
                                                                         grp.GetOrdererChildren()?.Select(s => s.Uid).ToArray());
                                        }).ToArray();
            }
            finally
            {
                DataTextBlockGroup.ReleaseItems(grpHost);
            }
        }

        /// <summary>
        /// Extract text information
        /// </summary>
        private void DataTextBlockExtraction(TextRenderInfo txt)
        {
            var actualTxtStr = txt.GetActualText();

            var fontSize = txt.GetFontSize();

            var matrix = txt.GetTextMatrix();

            var sizeHighVector = new Vector(0, fontSize, 0);
            float sizeAdjusted = sizeHighVector.Cross(matrix).Length();

            var font = txt.GetFont();

            var xScale = txt.GetHorizontalScaling();
            var tags = txt.GetCanvasTagHierarchy();

            var charact = new CharacterRenderInfo(txt);
            var bbox = charact.GetBoundingBox();

            var accendLine = txt.GetAscentLine();
            var descendLine = txt.GetDescentLine();

            var topLeftPoint = accendLine.GetStartPoint();
            var topRightPoint = accendLine.GetEndPoint();
            var bottomRightPoint = descendLine.GetEndPoint();
            var bottomLeftPoint = descendLine.GetStartPoint();

            var topLeft = new BlockPoint(topLeftPoint.Get(Vector.I1), this._pageSize.GetHeight() - topLeftPoint.Get(Vector.I2));
            var topRight = new BlockPoint(topRightPoint.Get(Vector.I1), this._pageSize.GetHeight() - topRightPoint.Get(Vector.I2));
            var bottomRight = new BlockPoint(bottomRightPoint.Get(Vector.I1), this._pageSize.GetHeight() - bottomRightPoint.Get(Vector.I2));
            var bottomLeft = new BlockPoint(bottomLeftPoint.Get(Vector.I1), this._pageSize.GetHeight() - bottomLeftPoint.Get(Vector.I2));

            var text = charact.GetText();

            var location = charact.GetLocation();
            var magnitude = location.OrientationMagnitude();

            var fontInfo = this._fontManager.AddOrGetFontInfo(fontSize, font);

            Debug.Assert(this._currentBlockBuilder != null);
            this._currentBlockBuilder.AddTextData(fontSize,
                                                  sizeAdjusted,
                                                  fontInfo,
                                                  txt.GetSingleSpaceWidth(),
                                                  fontInfo.LineSizePoint * sizeAdjusted,
                                                  xScale,
                                                  magnitude,
                                                  new BlockArea(topLeft, topRight, bottomRight, bottomLeft),
                                                  text,
                                                  txt.GetMcid(),
                                                  tags);

            this._token.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Extract image information
        /// </summary>
        private void DataImageBlockExtraction(ImageRenderInfo imgRenderInfo)
        {
            if (this._skipImage)
                return;

            var tags = imgRenderInfo.GetCanvasTagHierarchy();
            var imgName = imgRenderInfo.GetImageResourceName();
            var matrix = imgRenderInfo.GetImageCtm();

            var img = imgRenderInfo.GetImage();
            //var imgType = img.IdentifyImageType();
            //byte[]? rawImg = null;

            //var height = img.GetHeight();
            //var width = img.GetWidth();

            // https://kb.itextpdf.com/itext/how-to-get-the-co-ordinates-of-an-image
            //var matrixX = matrix.Get(Matrix.I31);
            //var matrixY = matrix.Get(Matrix.I32);

            var matrixWidth = matrix.Get(Matrix.I11);
            var matrixHeight = matrix.Get(Matrix.I22);

            var startPoint = imgRenderInfo.GetStartPoint();

            var x = (float)startPoint.Get(0);
            var y = this._pageSize.GetHeight() - (float)startPoint.Get(1);

            var topLeft = new BlockPoint(x, y - matrixHeight);
            var topRight = new BlockPoint(x + matrixWidth, y - matrixHeight);
            var bottomRight = new BlockPoint(x + matrixWidth, y);
            var bottomLeft = new BlockPoint(x, y);

            var needCreateBlock = this._currentBlockBuilder == null;

            if (needCreateBlock)
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                EventOccurred(null, EventType.BEGIN_TEXT);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            ImageMetaData? metaData = null;
            try
            {
                metaData = this._imageManager.AddImageResource(img);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "DataImageBlockExtraction : " + imgName + " {exception}", ex);
            }

            this._currentBlockBuilder!.AddImageData(imgName,
                                                    metaData,
                                                    new BlockArea(topLeft, topRight, bottomRight, bottomLeft),
                                                    tags);

            if (needCreateBlock)
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                EventOccurred(null, EventType.END_TEXT);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            this._token.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Compiles the data blocks.
        /// </summary>
        private IReadOnlyCollection<IDataBlock> CompileDataBlocks(IReadOnlyCollection<IDataBlockMergeStrategy> strategies,
                                                                 CancellationToken token)
        {
            var blocks = new List<IDataBlock>(42);
            foreach (var r in this._roots)
            {
                this._token.ThrowIfCancellationRequested();

                var results = r.Consolidate(strategies, token);
                if (results is not null)
                    blocks.AddRange(results);

                this._token.ThrowIfCancellationRequested();
            }

            return strategies.Apply(blocks, token);
        }

        #endregion

        #endregion
    }
}
