// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Strategies
{
    using global::Data.Block.Abstractions;
    using global::Data.Block.Abstractions.MetaData;
    using global::Data.Block.Abstractions.Tags;

    using iText.IO.Image;
    using iText.Kernel.Pdf;
    using iText.Kernel.Pdf.Canvas;

    using PDF.Data.Extractor.Services;

    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

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

        /// <summary>
        /// Gets the blocks.
        /// </summary>
        public IReadOnlyCollection<DataBlock> Blocks
        {
            get { return this._dataBlocks; }
        }

        /// <summary>
        /// Gets the block builders.
        /// </summary>
        public IReadOnlyCollection<DataBlockBuilder> BlockBuilders
        {
            get { return this._children; }
        }

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
                                 ImageMetaData? image,
                                 BlockArea area,
                                 IList<CanvasTag> tags)
        {
            this._dataBlocks.Add(new DataImageBlock(Guid.NewGuid(),
                                                    imgName?.GetValue() ?? string.Empty,
                                                    image?.Uid,
                                                    area,
                                                    AnalyzeTags(tags),
                                                    null));
        }

        /// <summary>
        /// Consolidates the block created, try group close text ...
        /// </summary>
        public IReadOnlyCollection<IDataBlock> Consolidate(IReadOnlyCollection<IDataBlockMergeStrategy> strategies,
                                                          CancellationToken token)
        {
            var childrenBlocks = new List<IDataBlock>(this._dataBlocks);
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
            var dataTag = new List<DataTag>();

            foreach (var tag in tags)
            {
                var lang = tag.GetProperty(PdfName.Lang);
                var language = tag.GetProperty(PdfName.Language);

                if (lang != null || language != null)
                {
                    var raw = (lang ?? language)?.ToString() ?? string.Empty;

                    if (!string.IsNullOrEmpty(raw))
                    {
                        var culture = CultureInfo.GetCultureInfo(raw);
                        dataTag.Add(new DataLangTag(culture?.TextInfo?.CultureName ?? string.Empty, raw));

                        continue;
                    }
                }

                var objs = new List<(string category, PdfObject obj)>();

                var metaData = tag.GetProperty(PdfName.Metadata);
                if (metaData != null)
                    objs.Add((nameof(PdfName.Metadata), metaData));

                var flatObjs = FlatternPdfObjectStream(objs);

                foreach (var flat in flatObjs)
                {
                    if (flat.obj is PdfNumber number)
                    {
                        dataTag.Add(new DataPropTag(flat.category, number.GetValue().ToString(), number.ToString()));
                        continue;
                    }

                    if (flat.obj is PdfString literal)
                    {
                        dataTag.Add(new DataPropTag(flat.category, literal.GetValue().ToString(), literal.ToString()));
                        continue;
                    }
                }
            }
            return dataTag.Distinct().ToArray();
        }

        /// <summary>
        /// Flatterns the PDF object.
        /// </summary>
        private static IReadOnlyCollection<(string category, PdfObject obj)> FlatternPdfObjectStream(List<(string category, PdfObject obj)> objs)
        {
            bool flattern = true;
            do
            {
                flattern = false;
                var objArray = objs.ToArray();
                foreach (var obj in objArray)
                {
                    if (obj.obj.IsStream())
                    {
                        var stream = (PdfStream)obj.obj;
                        objs.Remove(obj);
                        var insides = stream.Values();
                        objs.AddRange(insides.Select(i => (obj.category, i)));

                        flattern = true;
                    }
                }
            } while (flattern);

            return objs; 
        }

        private string ConvertImageType(ImageType imgType)
        {
            return imgType.ToString();
        }

        #endregion

        #endregion
    }
}
