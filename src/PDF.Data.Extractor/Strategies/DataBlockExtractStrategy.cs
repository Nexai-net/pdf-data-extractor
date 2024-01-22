namespace PDF.Data.Extractor.Strategies
{
    using iText.Kernel.Geom;
    using iText.Kernel.Pdf;
    using iText.Kernel.Pdf.Canvas.Parser;
    using iText.Kernel.Pdf.Canvas.Parser.Data;
    using iText.Kernel.Pdf.Canvas.Parser.Listener;

    using PDF.Data.Extractor.Abstractions;
    using PDF.Data.Extractor.Services;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// Strategy used to extract pdf <see cref="DataBlock"/>
    /// </summary>
    /// <seealso cref="iText.Kernel.Pdf.Canvas.Parser.Listener.IEventListener" />
    public sealed class DataBlockExtractStrategy : IEventListener
    {
        #region Fields

        private readonly IFontMetaDataInfoExtractStrategy _fontMetaDataInfoExtractStrategy;

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
        public DataBlockExtractStrategy(IFontMetaDataInfoExtractStrategy fontMetaDataInfoExtractStrategy)
        {
            this._fontMetaDataInfoExtractStrategy = fontMetaDataInfoExtractStrategy;
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

                var matrix = txt.GetTextMatrix();

                var sizeHighVector = new Vector(0, fontSize, 0);
                float sizeAdjusted = sizeHighVector.Cross(matrix).Length();

                var font = txt.GetFont();

                var xScale = txt.GetHorizontalScaling();
                var tags = txt.GetCanvasTagHierarchy();

                var charact = new CharacterRenderInfo(txt);
                var bbox = charact.GetBoundingBox();
                //var location = charact.GetLocation();
                var text = charact.GetText();

                //var accendLine = txt.GetAscentLine();
                //var descendLine = txt.GetDescentLine();

                //var ligneSize = Math.Abs(accendLine.GetStartPoint().Get(1) - descendLine.GetStartPoint().Get(1));

                // TODO : Use those variable to improve calcul for grouping word

                //var charSpacing = txt.GetCharSpacing();
                //var wordSpacing = txt.GetWordSpacing();

                // TODO : Managed Moved area based on transformed matrix

                var fontInfo = this._fontMetaDataInfoExtractStrategy.AddOrGetFontInfo(fontSize, font);

                Debug.WriteLine("---------------------------");
                Debug.WriteLine(string.Format("### text : '{0}'", actualTxtStr ?? text));
                Debug.WriteLine(string.Format("# fontSize : {0}", fontSize));
                Debug.WriteLine(string.Format("# sizeAdjusted : {0}", sizeAdjusted));
                Debug.WriteLine(string.Format("# xScale : {0}", xScale));
                Debug.WriteLine(string.Format("# txt.GetSingleSpaceWidth() : {0}", txt.GetSingleSpaceWidth()));
                Debug.WriteLine(string.Format("# ligneSize : {0}", fontInfo.LineSizePoint));
                Debug.WriteLine(string.Format("# txt.GetLeading() : {0}", txt.GetLeading()));
                Debug.WriteLine(string.Format("# txt.GetMcid() : {0}", txt.GetMcid()));
                Debug.WriteLine(string.Format("# txt.GetRise() : {0}", txt.GetRise()));
                Debug.WriteLine(string.Format("# txt.GetTextRenderMode() : {0}", txt.GetTextRenderMode()));
                Debug.WriteLine(string.Format("# font.GetFontProgram().GetRegistry() : {0}", font.GetFontProgram().GetRegistry()));
                Debug.WriteLine(string.Format("# font.GetFontProgram().GetFontMetrics().GetXHeight(): {0}", font.GetFontProgram().GetFontMetrics().GetXHeight()));
                Debug.WriteLine(string.Format("# font.CreateGlyphLine(\"pgqtm0i\").Size(): {0}", font.CreateGlyphLine("pgqtm0i").Size()));
                Debug.WriteLine(string.Format("# font.GetAscent(\"pgqtm0i\", fontSize): {0}", font.GetAscent("pgqtm0i", fontSize)));
                Debug.WriteLine(string.Format("# font.GetDescent(\"pgqtm0i\", fontSize): {0}", font.GetDescent("pgqtm0i", fontSize)));
                Debug.WriteLine(string.Format("# font.GetDescent(\"pgqtm0i\", fontSize): TOTAL : {0}", font.GetAscent("pgqtm0i", fontSize) - font.GetDescent("pgqtm0i", fontSize)));

                Debug.Assert(this._currentBlockBuilder != null);
                this._currentBlockBuilder.AddTextData(actualTxtStr,
                                                      fontSize,
                                                      sizeAdjusted,
                                                      fontInfo,
                                                      txt.GetSingleSpaceWidth(),
                                                      fontInfo.LineSizePoint * sizeAdjusted,
                                                      xScale,
                                                      bbox,
                                                      text,
                                                      txt.GetMcid(),
                                                      tags);
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

                var needCreateBlock = this._currentBlockBuilder == null;

                if (needCreateBlock)
                    EventOccurred(null, EventType.BEGIN_TEXT);

                this._currentBlockBuilder!.AddImageData(imgName, imgType, rawImg, width, height, drawShapePoints, tags);

                if (needCreateBlock)
                    EventOccurred(null, EventType.END_TEXT);

                return;
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
        public DataPageBlock Compile(int pageNumber,
                                     PdfPage page,
                                     params IDataBlockMergeStrategy[] strategies)
        {
            var pageSize = page.GetPageSizeWithRotation();

            var pageBlocks = CompileDataBlocks(strategies);

            return new DataPageBlock(Guid.NewGuid(),
                                     pageNumber,
                                     page.GetRotation(),
                                     new BlockArea(pageSize.GetLeft(), pageSize.GetTop(), pageSize.GetWidth(), pageSize.GetHeight()),
                                     null, // TODO : Relation Not comput now
                                     pageBlocks);

        }

        /// <summary>
        /// Compiles the data blocks.
        /// </summary>
        private IReadOnlyCollection<DataBlock> CompileDataBlocks(IReadOnlyCollection<IDataBlockMergeStrategy> strategies)
        {
            var blocks = new List<DataBlock>(42);
            foreach (var r in this._roots)
            {
                var results = r.Consolidate(strategies);
                if (results is not null)
                    blocks.AddRange(results);
            }

            return strategies.Apply(blocks);
        }

        /// <inheritdoc />
        public ICollection<EventType> GetSupportedEvents()
        {
            return s_eventTypeManaged;
        }

        #endregion
    }
}
