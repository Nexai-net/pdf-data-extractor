// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Strategies
{
    using PDF.Data.Extractor.Abstractions;
    using PDF.Data.Extractor.Abstractions.MetaData;
    using PDF.Data.Extractor.Services;

    using System;
    using System.Collections.Generic;
    using System.Numerics;

    /// <summary>
    /// Merge block close that are Vertical aligned
    /// </summary>
    /// <seealso cref="DataTextBlockSiblingMergeBaseStrategy" />
    public sealed class DataTextBlockVerticalSiblingMergeStrategy : DataTextBlockSiblingMergeBaseStrategy
    {
        #region Fields

        private readonly bool _alignRight;

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTextBlockVerticalSiblingMergeStrategy"/> class.
        /// </summary>
        public DataTextBlockVerticalSiblingMergeStrategy(IFontManager fontManager, bool alignRight)
            : base(fontManager)
        {
            this._alignRight = alignRight;
        }

        #endregion

        #region Methods

        /// <inheritdoc />
        protected override List<DataTextBlock> PrepateBlocks(IEnumerable<DataTextBlock> texts)
        {
            return base.PrepateBlocks(texts.OrderByDescending(t => t.Area.Y).ThenBy(t => t.Area.X));
        }

        /// <inheritdoc />
        protected override float GetAllowedSpaceBetwenBlocks(DataTextBlock source, TextFontMetaData sourceFont, DataTextBlock target, TextFontMetaData targetFont)
        {
            // Take max ligne space space with 10% delta
            return source.LineSize * 2.5f;
        }

        /// <inheritdoc />
        protected override Vector2 GetCompareSourceLign(DataTextBlock source)
        {
            return this._alignRight ? source.Area.RightLine : source.Area.LeftLine;
        }

        /// <inheritdoc />
        protected override BlockPoint GetSourceComparePoint(DataTextBlock source)
        {
            return this._alignRight ? source.Area.BottomRight : source.Area.BottomLeft;
        }

        /// <inheritdoc />
        protected override BlockPoint GetTargetComparePoint(DataTextBlock target)
        {
            return this._alignRight ? target.Area.TopRight : target.Area.TopLeft;
        }

        /// <inheritdoc />
        protected override string? MergeText(in string? source, in string? target)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
                return base.MergeText(source, target);

            return source + Environment.NewLine + target;
        }

        /// <inheritdoc />
        protected override BlockArea MergeArea(BlockArea source, BlockArea target)
        {
            var result = new BlockArea(source.X,
                                       source.Y,
                                       Math.Abs(Math.Max(source.TopRight.X, target.TopRight.X) - Math.Min(source.TopLeft.X, target.TopLeft.X)),
                                       Math.Abs(Math.Max(source.BottomLeft.Y, target.BottomLeft.Y) - Math.Min(source.TopLeft.Y, target.TopLeft.Y)));

            return result;
        }

        #endregion
    }
}
