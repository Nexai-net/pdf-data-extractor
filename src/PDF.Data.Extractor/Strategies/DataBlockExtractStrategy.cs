﻿namespace PDF.Data.Extractor.Strategies
{
    using iText.Kernel.Geom;
    using iText.Kernel.Pdf;
    using iText.Kernel.Pdf.Canvas.Parser;
    using iText.Kernel.Pdf.Canvas.Parser.Data;
    using iText.Kernel.Pdf.Canvas.Parser.Listener;

    using PDF.Data.Extractor.Abstractions;
    using PDF.Data.Extractor.Extensions;
    using PDF.Data.Extractor.InternalModels;
    using PDF.Data.Extractor.Services;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Numerics;

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
                                        PdfPage page)
        {
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

            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates the <see cref="DataPageBlock"/> from block analyzed
        /// </summary>
        public DataPageBlock Compile(int pageNumber,
                                     CancellationToken token,
                                     params IDataBlockMergeStrategy[] strategies)
        {
            var pageSize = this._page.GetPageSize();

            var pageBlocks = CompileDataBlocks(strategies, token);

            return new DataPageBlock(Guid.NewGuid(),
                                     pageNumber,
                                     this._page.GetRotation(),
                                     new BlockArea(new BlockPoint(pageSize.GetLeft(), pageSize.GetBottom()),
                                                   new BlockPoint(pageSize.GetRight(), pageSize.GetBottom()),
                                                   new BlockPoint(pageSize.GetRight(), pageSize.GetTop()),
                                                   new BlockPoint(pageSize.GetLeft(), pageSize.GetTop())),
                                     ComputeBlockRelation(pageBlocks, token),
                                     pageBlocks.OfType<DataBlock>());
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
        private IEnumerable<DataRelationBlock> ComputeBlockRelation(IReadOnlyCollection<IDataBlock> pageBlocks, CancellationToken token)
        {
            var createTextCloseGroup = new DataTextBlockProximityStrategy(compareFontInfo: false,
                                                                          horizontalDistanceTolerance: 0.2f,
                                                                          verticalDistanceTolerance: 0.6f,
                                                                          customCompile: grp => new DataTextBlockRelationGroup(Guid.NewGuid(),
                                                                                                                               grp.GetWorldArea(),
                                                                                                                               grp.GetTags(),
                                                                                                                               grp.GetOrdererChildren()));

            token.ThrowIfCancellationRequested();
            var globalGrp = new[] { createTextCloseGroup }.Apply(pageBlocks, token, breakOnFirstLoop: false);
            var grpRelation = ConstructRelationStruct(globalGrp, BlockRelationTypeEnum.Group);

            token.ThrowIfCancellationRequested();
            return grpRelation;
        }

        private static DataRelationBlock[] ConstructRelationStruct(IReadOnlyCollection<IDataBlock> globalGrp, BlockRelationTypeEnum blockRelation)
        {
            return globalGrp.Where(g => g.Children is not null && g.Children.GetTreeElement(c => c.Children)
                                                                                       .Where(c => c is not null && c is DataTextBlock)
                                                                                       .Count() > 1)
                                       .Select(g => new DataRelationBlock(g.Uid,
                                                                          g.Area,
                                                                          blockRelation,
                                                                          g.Children!.GetTreeElement(c => c.Children)
                                                                                     .Where(c => c != null && c is DataTextBlock)
                                                                                     .Select(c => c!.Uid).Distinct()))
                                       .ToArray();
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
            var tags = imgRenderInfo.GetCanvasTagHierarchy();
            var imgName = imgRenderInfo.GetImageResourceName();
            var matrix = imgRenderInfo.GetImageCtm();

            var img = imgRenderInfo.GetImage();
            var imgType = img.IdentifyImageType();
            var rawImg = img.GetImageBytes();
            var height = img.GetHeight();
            var width = img.GetWidth();

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

            var metaData = this._imageManager.AddImageResource(img);

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