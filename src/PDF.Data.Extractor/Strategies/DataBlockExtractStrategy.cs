namespace PDF.Data.Extractor.Strategies
{
    using iText.Kernel.Geom;
    using iText.Kernel.Pdf;
    using iText.Kernel.Pdf.Canvas.Parser;
    using iText.Kernel.Pdf.Canvas.Parser.Data;
    using iText.Kernel.Pdf.Canvas.Parser.Listener;

    using PDF.Data.Extractor.Abstractions;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    public sealed class DataBlockExtractStrategy : IEventListener
    {
        #region Fields

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
        public DataBlockExtractStrategy()
        {
            this._roots = new List<DataBlockBuilder>();
        }

        #endregion

        #region Methods

        ///// <summary>
        ///// Gets the analyze data blocks.
        ///// </summary>
        //public IReadOnlyCollection<Abstractions.DataBlock> GetDataBlocks()
        //{
        //    return this._roots;
        //}

        /// <inheritdoc />
        public void EventOccurred(IEventData data, EventType type)
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

            if (data is TextRenderInfo txt)
            {
                var actualTxtStr = txt.GetActualText();
                var fontSize = txt.GetFontSize();
                var xScale = txt.GetHorizontalScaling();
                var tags = txt.GetCanvasTagHierarchy();

                var charact = new CharacterRenderInfo(txt);
                var bbox = charact.GetBoundingBox();
                //var location = charact.GetLocation();
                var text = charact.GetText();

                Debug.Assert(this._currentBlockBuilder != null);
                this._currentBlockBuilder.AddTextData(actualTxtStr, fontSize, xScale, bbox, text, tags);
                return;
            }
            else if (type == EventType.RENDER_TEXT)
            {
                Console.Error.WriteLine("Other type of text not managed");
            }

            if (data is ImageRenderInfo imgRenderInfo)
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

                this._currentBlockBuilder!.AddImageData(imgName, imgType, rawImg, width, height, drawShapePoints, tags);

            }
            else if (type == EventType.RENDER_IMAGE)
            {
                Console.Error.WriteLine("Other type of text not managed");
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates the <see cref="DataPageBlock"/> from block analyzed
        /// </summary>
        public DataPageBlock Compile(int pageNumber, PdfPage page)
        {
            var pageSize = page.GetPageSizeWithRotation();

            var pageBlocks = CompileDataBlocks();

            return new DataPageBlock(Guid.NewGuid(),
                                     pageNumber,     
                                     page.GetRotation(),
                                     new BlockArea(pageSize.GetLeft(), pageSize.GetTop(), pageSize.GetWidth(), pageSize.GetHeight()),
                                     pageBlocks);

        }

        private IReadOnlyCollection<DataBlock> CompileDataBlocks()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public ICollection<EventType> GetSupportedEvents()
        {
            return s_eventTypeManaged;
        }

        #endregion
    }
}
