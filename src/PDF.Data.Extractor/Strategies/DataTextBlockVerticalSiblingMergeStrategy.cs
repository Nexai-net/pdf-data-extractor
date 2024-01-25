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
            return base.PrepateBlocks(texts.OrderBy(t => t.Area.Y).ThenBy(t => t.Area.X));
        }

        /// <inheritdoc />
        protected override float GetAllowedSpaceBetwenBlocks(DataTextBlock source, TextFontMetaData sourceFont, DataTextBlock target, TextFontMetaData targetFont)
        {
            // Take max ligne space space with 10% delta
            return source.LineSize;
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
            var result = new BlockArea(source.TopLeft, source.TopRight,
                                       target.BottomRight, target.BottomLeft);

            result = EnsureLeftAlign(result);

            var topLineLen = result.TopLine.Length();
            var bottomLineLen = result.BottomLine.Length();

            if (topLineLen != bottomLineLen)
            {
                var maxLineLen = Math.Max(topLineLen, bottomLineLen);

                if (topLineLen != maxLineLen)
                {
                    var newTopLine = (result.TopLine / result.TopLine.Length()) * maxLineLen;
                    var newEnd = new Vector2(result.TopLeft.X, result.TopLeft.Y) + newTopLine;
                    result = new BlockArea(result.TopLeft,
                                           new BlockPoint(newEnd.X, newEnd.Y),
                                           result.BottomRight,
                                           result.BottomLeft);
                }

                if (bottomLineLen != maxLineLen)
                {
                    var newBottomLine = (result.BottomLine / result.BottomLine.Length()) * maxLineLen;
                    var newBottomEnd = new Vector2(result.BottomLeft.X, result.BottomLeft.Y) + newBottomLine;
                    result = new BlockArea(result.TopLeft,
                                           result.TopRight,
                                           new BlockPoint(newBottomEnd.X, newBottomEnd.Y),
                                           result.BottomLeft);
                }
            }

            return result;
        }

        private BlockArea EnsureLeftAlign(BlockArea result)
        {
            var leftOriginTopAngle = BlockCoordHelper.RadianAngle(result.TopLine, result.LeftLine, true);
            var leftTopAngle = BlockCoordHelper.RadianAngle(result.TopLine, result.LeftLine, false);

            var delta = BlockCoordHelper.RIGHT_ANGLE_RADIAN - leftTopAngle;
            if (Math.Abs(delta) <= BlockCoordHelper.EQUALITY_TOLERANCE)
                return result;

            var missingLineLen = Math.Abs(BlockCoordHelper.Diff(result.TopLeft, result.BottomLeft).Length()) * Math.Sin(delta);
            if (leftTopAngle != leftOriginTopAngle)
            {
                // If TopLeft need to be moved
                var move = Vector2.Normalize(result.TopLine) * (float)missingLineLen;
                var targetTopLeft = new BlockPoint(result.TopLeft.X + move.X, result.TopLeft.Y + move.Y);
                return new BlockArea(targetTopLeft, result.TopRight, result.BottomRight, result.BottomLeft);
            }

            var moveBottom = Vector2.Normalize(result.BottomLine) * (float)missingLineLen;
            var targetBottomLeft = new BlockPoint(result.BottomLeft.X - moveBottom.X, result.BottomLeft.Y - moveBottom.Y);
            return new BlockArea(result.TopLeft, result.TopRight, result.BottomRight, targetBottomLeft);
        }

        #endregion
    }
}
