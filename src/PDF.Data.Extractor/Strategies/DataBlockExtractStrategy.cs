namespace PDF.Data.Extractor.Strategies
{
    using iText.Forms.Form.Element;
    using iText.Kernel.Geom;
    using iText.Kernel.Pdf;
    using iText.Kernel.Pdf.Canvas.Parser;
    using iText.Kernel.Pdf.Canvas.Parser.Data;
    using iText.Kernel.Pdf.Canvas.Parser.Listener;

    using PDF.Data.Extractor.Abstractions;
    using PDF.Data.Extractor.Services;

    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;

    /// <summary>
    /// Strategy used to extract pdf <see cref="DataBlock"/>
    /// </summary>
    /// <seealso cref="IEventListener" />
    public sealed class DataBlockExtractStrategy : IEventListener
    {
        #region Fields

        private const double DEGREE_TO_RADION_RATIO = Math.PI / 180.0f;

        private readonly IFontMetaDataInfoExtractStrategy _fontMetaDataInfoExtractStrategy;
        private readonly CancellationToken _token;
        private readonly IReadOnlyDictionary<Type, Action<IEventData>> _dataTypeProcessor;
        private readonly Rectangle _pageSize;

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
        public DataBlockExtractStrategy(IFontMetaDataInfoExtractStrategy fontMetaDataInfoExtractStrategy,
                                        CancellationToken token,
                                        Rectangle pageSize)
        {
            this._token = token;
            this._pageSize = pageSize;
            this._fontMetaDataInfoExtractStrategy = fontMetaDataInfoExtractStrategy;
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
                                     PdfPage page,
                                     CancellationToken token,
                                     params IDataBlockMergeStrategy[] strategies)
        {
            var pageSize = page.GetPageSizeWithRotation();

            var pageBlocks = CompileDataBlocks(strategies, token);

            return new DataPageBlock(Guid.NewGuid(),
                                     pageNumber,
                                     page.GetRotation(),
                                     new BlockArea(new BlockPoint(pageSize.GetLeft(), pageSize.GetTop()),
                                                   new BlockPoint(pageSize.GetRight(), pageSize.GetTop()),
                                                   new BlockPoint(pageSize.GetRight(), pageSize.GetBottom()),
                                                   new BlockPoint(pageSize.GetLeft(), pageSize.GetBottom())),
                                     null, // TODO : Relation Not comput now
                                     pageBlocks);
        }

        /// <inheritdoc />
        public ICollection<EventType> GetSupportedEvents()
        {
            return s_eventTypeManaged;
        }

        #region Tools

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

            var fontInfo = this._fontMetaDataInfoExtractStrategy.AddOrGetFontInfo(fontSize, font);

            Debug.Assert(this._currentBlockBuilder != null);
            this._currentBlockBuilder.AddTextData(actualTxtStr,
                                                  fontSize,
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
            var startPoint = imgRenderInfo.GetStartPoint();
            var imgName = imgRenderInfo.GetImageResourceName();
            var matrix = imgRenderInfo.GetImageCtm();

            var img = imgRenderInfo.GetImage();
            var imgType = img.IdentifyImageType();
            var rawImg = img.GetImageBytes();
            var height = img.GetHeight();
            var width = img.GetWidth();

            var x = (double)startPoint.Get(0);
            var y = (double)startPoint.Get(1);

            var top = new Line(new Point(x, y), new Point(x + width, y));
            var bottom = new Line(new Point(x, y + height), new Point(x + width, y + height));

            var topTransformed = ShapeTransformUtil.TransformLine(top, matrix);
            var bottomTransformed = ShapeTransformUtil.TransformLine(bottom, matrix);

            var drawShapePoints = topTransformed.GetBasePoints()
                                                .Concat(bottomTransformed.GetBasePoints())
                                                .ToArray();

            var needCreateBlock = this._currentBlockBuilder == null;

            if (needCreateBlock)
                EventOccurred(null, EventType.BEGIN_TEXT);

            this._currentBlockBuilder!.AddImageData(imgName, imgType, rawImg, width, height, drawShapePoints, tags);

            if (needCreateBlock)
                EventOccurred(null, EventType.END_TEXT);

            this._token.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Compiles the data blocks.
        /// </summary>
        private IReadOnlyCollection<DataBlock> CompileDataBlocks(IReadOnlyCollection<IDataBlockMergeStrategy> strategies,
                                                                 CancellationToken token)
        {
            var blocks = new List<DataBlock>(42);
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
