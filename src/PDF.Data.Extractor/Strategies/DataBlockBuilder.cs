// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Strategies
{
    using iText.IO.Image;
    using iText.Kernel.Geom;
    using iText.Kernel.Pdf;
    using iText.Kernel.Pdf.Canvas;
    using iText.Layout.Element;

    using PDF.Data.Extractor.Abstractions;
    using PDF.Data.Extractor.Abstractions.MetaData;
    using PDF.Data.Extractor.Abstractions.Tags;
    using PDF.Data.Extractor.Services;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

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
        public void AddTextData(float fontSize,
                                float pointValue,
                                TextFontMetaData fontInfo,
                                float spaceWidth,
                                float ligneSize,
                                float xScale,
                                float magnitude,
                                BlockArea area,
                                string text,
                                float textBoxId,
                                IEnumerable<CanvasTag> tags)
        {
            this._dataBlocks.Add(new DataTextBlock(Guid.NewGuid(),
                                                   fontSize,
                                                   pointValue,
                                                   ligneSize,
                                                   xScale / 100.0f,
                                                   magnitude,
                                                   text,
                                                   fontInfo.Uid,
                                                   spaceWidth,
                                                   area,
                                                   new[] { textBoxId },
                                                   AnalyzeTags(tags),
                                                   null));
        }

        /// <summary>
        /// Adds the image data.
        /// </summary>
        public void AddImageData(PdfName imgName,
                                 ImageMetaData image,
                                 BlockArea area,
                                 IList<CanvasTag> tags)
        {
            
            this._dataBlocks.Add(new DataImageBlock(Guid.NewGuid(),
                                                    imgName.GetValue(),
                                                    image.Uid,
                                                    area,
                                                    AnalyzeTags(tags),
                                                    null));
        }

        /// <summary>
        /// Consolidates the block created, try group close text ...
        /// </summary>
        public IReadOnlyCollection<DataBlock> Consolidate(IReadOnlyCollection<IDataBlockMergeStrategy> strategies,
                                                          CancellationToken token)
        {
            var childrenBlocks = new List<DataBlock>(this._dataBlocks);
            foreach (var child in this._children)
            {
                var childResults = child.Consolidate(strategies, token);

                token.ThrowIfCancellationRequested();

                if (childResults is not null)
                    childrenBlocks.AddRange(childResults);
            }

            return strategies.Apply(childrenBlocks, token);
        }

        #region Tools

        /// <summary>
        /// Analyzes <see cref="CanvasTag"/> into <see cref="DataTag"/>
        /// </summary>
        private IReadOnlyCollection<DataTag> AnalyzeTags(IEnumerable<CanvasTag> tags)
        {
            // TODO : analyze tags
            return tags?.Select(t => new DataRawTag(t.GetActualText() ?? t.GetExpansionText()))
                        .Where(t => !string.IsNullOrEmpty(t.Raw))
                        .ToArray() ?? Array.Empty<DataTag>();
        }

        private string ConvertImageType(ImageType imgType)
        {
            return imgType.ToString();
        }

        #endregion

        #endregion
    }
}
