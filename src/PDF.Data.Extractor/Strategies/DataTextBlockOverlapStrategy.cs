// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Strategies
{
    using global::Data.Block.Abstractions;

    using System;

    /// <summary>
    /// Merge strategy using overlap merge strategy
    /// </summary>
    /// <seealso cref="DataBlockMergeBaseStrategy{DataTextBlock}" />
    public sealed class DataTextBlockOverlapStrategy : DataTextBlockGroupBaseStrategy
    {
        #region Fields

        private readonly bool _compareFontInfo;

        #endregion

        #region Ctor

        /// <summary>
        /// Instanciate a new instance of the class <see cref="DataTextBlockProximityStrategy"/>
        /// </summary>
        public DataTextBlockOverlapStrategy(bool compareFontInfo = true,
                                            Func<DataTextBlockGroup, IDataBlock>? customCompile = null)
            : base(customCompile)
        {
            this._compareFontInfo = compareFontInfo;
        }

        #endregion

        #region Methods
        /// <summary>
        /// Determines whether this instance can merge the specified current.
        /// </summary>
        protected override bool CanMerge(DataTextBlockGroup current, DataTextBlockGroup other)
        {
            if (this._compareFontInfo && current.LineSize != other.LineSize) // current.FontUid != other.FontUid
                return false;

            if (current.Magnitude != other.Magnitude)
                return false;

            var currentArea = current.GetWorldArea();
            var otherArea = other.GetWorldArea();

            var overlap = currentArea.Overlap(otherArea);

            if (overlap)
                return true;

            overlap = currentArea.Overlap(otherArea, 4);//Math.Min(other.HalfLeftLineLength, current.HalfLeftLineLength) / 2.0f);

            return overlap;
        }

        #endregion
    }
}
