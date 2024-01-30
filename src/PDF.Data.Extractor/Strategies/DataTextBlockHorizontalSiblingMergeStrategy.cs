// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Strategies
{
    using global::Data.Block.Abstractions;
    using global::Data.Block.Abstractions.MetaData;
    using PDF.Data.Extractor.Services;

    using System;
    using System.Collections.Generic;
    using System.Numerics;

    /// <summary>
    /// Merge block close that are horizontal aligned
    /// </summary>
    /// <seealso cref="PDF.Data.Extractor.Strategies.DataTextBlockSiblingMergeBaseStrategy" />
    public sealed class DataTextBlockHorizontalSiblingMergeStrategy : DataTextBlockSiblingMergeBaseStrategy
    {
        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTextBlockHorizontalSiblingMergeStrategy"/> class.
        /// </summary>
        public DataTextBlockHorizontalSiblingMergeStrategy(IFontManager fontManager)
            : base(fontManager)
        {

        }

        #endregion

        #region Methods

        /// <inheritdoc />
        protected override List<DataTextBlock> PrepateBlocks(IEnumerable<DataTextBlock> texts)
        {
            return base.PrepateBlocks(texts.OrderBy(t => t.Area.Y).ThenBy(t => t.Area.X));
        }

        /// <inheritdoc />
        protected override float GetAllowedSpaceBetwenBlocks(DataTextBlock source,
                                                             TextFontMetaData sourceFont,
                                                             DataTextBlock target,
                                                             TextFontMetaData targetFont)
        {
            // return (sourceFont.MaxWidth * source.PointValue) + ((source.Text?.Count(c => c == ' ') ?? 0) * source.SpaceWidth * 1.5f);
            return source.SpaceWidth;
        }

        /// <inheritdoc />
        protected override Vector2 GetCompareSourceLign(DataTextBlock source)
        {
            return source.Area.TopLine;
        }

        /// <inheritdoc />
        protected override BlockPoint GetSourceComparePoint(DataTextBlock source)
        {
            return source.Area.TopRight;
        }

        /// <inheritdoc />
        protected override BlockPoint GetTargetComparePoint(DataTextBlock target)
        {
            return target.Area.TopLeft;
        }

        /// <inheritdoc />
        protected override BlockArea MergeArea(BlockArea source, BlockArea target)
        {
            var result = new BlockArea(source.TopLeft, target.TopRight, target.BottomRight, source.BottomLeft);
            return result;
        }

        #endregion
    }
}
