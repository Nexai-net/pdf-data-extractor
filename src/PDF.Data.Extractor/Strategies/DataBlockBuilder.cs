// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Strategies
{
    using iText.IO.Image;
    using iText.Kernel.Geom;
    using iText.Kernel.Pdf;
    using iText.Kernel.Pdf.Canvas;

    using PDF.Data.Extractor.Abstractions;
    using PDF.Data.Extractor.Abstractions.Tags;
    using PDF.Data.Extractor.Models;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Build information about a data block
    /// </summary>
    internal sealed class DataBlockBuilder
    {
        #region Fields

        private readonly List<DataBlockBuilder> _children;
        private readonly List<DataBlock> _dataBlocks;

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="DataBlockBuilder"/> class.
        /// </summary>
        public DataBlockBuilder(DataBlockBuilder? parent = null)
        {
            this._dataBlocks = new List<DataBlock>();

            this.Parent = parent;
            this._children = new List<DataBlockBuilder>();

            parent?._children.Add(this);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the parent.
        /// </summary>
        public DataBlockBuilder? Parent { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Adds the text data.
        /// </summary>
        public void AddTextData(string? actualTxtStr,
                                float fontSize,
                                float xScale,
                                Rectangle bbox,
                                string text,
                                IEnumerable<CanvasTag> tags)
        {
            this._dataBlocks.Add(new DataTextBlock(Guid.NewGuid(),
                                                   actualTxtStr,
                                                   fontSize,
                                                   xScale / 100.0f,
                                                   text,
                                                   new BlockArea(bbox.GetX(), bbox.GetY(), bbox.GetWidth(), bbox.GetHeight()),
                                                   AnalyzeTags(tags),
                                                   null));
        }

        /// <summary>
        /// Adds the image data.
        /// </summary>
        public void AddImageData(PdfName imgName,
                                 ImageType imgType,
                                 byte[] imageEncodedBytes,
                                 float width,
                                 float height,
                                 Point[] drawShapePoints,
                                 IList<CanvasTag> tags)
        {
            var drawBlockPoints = new List<BlockPoint>();

            var areaX = 0.0f;
            var areaY = 0.0f;
            var areaMaxX = 0.0f;
            var areaMaxY = 0.0f;

            foreach (var point in drawShapePoints)
            {
                var x = point.GetX();
                var y = point.GetX();

                drawBlockPoints.Add(new BlockPoint(x, y));

                if (x < areaX)
                    areaX = x;

                if (y < areaY)
                    areaY = y;

                if (x > areaMaxX)
                    areaMaxX = x;

                if (y < areaMaxY)
                    areaMaxY = y;
            }

            var area = new BlockArea(areaX, areaY, areaMaxX - areaX, areaMaxY - areaY);

            this._dataBlocks.Add(new DataImageBlock(Guid.NewGuid(),
                                                    imgName.GetValue(),
                                                    ConvertImageType(imgType),
                                                    imageEncodedBytes,
                                                    width,
                                                    height,
                                                    area,
                                                    drawBlockPoints,
                                                    AnalyzeTags(tags),
                                                    null));
        }

        /// <summary>
        /// Consolidates the block created, try group close text ...
        /// </summary>
        public void Consolidate()
        {
            foreach (var child in this._children)
                child.Consolidate();

            if (this._dataBlocks.Count <= 1)
                return;

            var texts = new List<DataTextBlock>(this._dataBlocks.OfType<DataTextBlock>()
                                                                // Order on top to botton on reading direction Top -> Bottom
                                                                .OrderBy(t => t.Area.Y)
                                                                // Order Left to right in the reading order Left -> Right
                                                                .ThenBy(t => t.Area.X));

            for (int i = 0; i < texts.Count; i++)
            {
                var current = texts[i];
                for (int nextIndx = i + 1; nextIndx < texts.Count; nextIndx++)
                {
                    var next = texts[nextIndx];

                    if (current.FontLevel != next.FontLevel)
                        continue;

                    if (current.Scale != next.Scale)
                        continue;

                    current.Area.TopRight 

                }
            }
        }

        #region Tools

        /// <summary>
        /// Analyzes <see cref="CanvasTag"/> into <see cref="DataTag"/>
        /// </summary>
        private IReadOnlyCollection<DataTag> AnalyzeTags(IEnumerable<CanvasTag> tags)
        {
            // TODO : analyze tags
            return tags?.Select(t => new DataRawTag(t.GetActualText() ?? t.GetExpansionText()))
                        .ToArray() ?? Array.Empty<DataTag>();
        }

        private string ConvertImageType(ImageType imgType)
        {
            throw new NotImplementedException();
        }

        #endregion

        #endregion
    }
}
