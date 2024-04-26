// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Strategies
{
    using global::Data.Block.Abstractions;

    using System;

    /// <summary>
    /// Merge strategy using compostion through block proximity
    /// </summary>
    /// <seealso cref="DataBlockMergeBaseStrategy{DataTextBlock}" />
    public sealed class DataTextBlockProximityStrategy : DataTextBlockGroupBaseStrategy
    {
        #region Fields

        private const float DIST_TOLERANCE_PERCENT_HORIZONTAL = 1.02f;
        private const float DIST_TOLERANCE_PERCENT_VERTICAL = 1.08f;

        private readonly bool _compareFontInfo;
        private readonly float _verticalDistanceTolerance;
        private readonly float _horizontalDistanceTolerance;

        #endregion

        #region Ctor

        /// <summary>
        /// Instanciate a new instance of the class <see cref="DataTextBlockProximityStrategy"/>
        /// </summary>
        public DataTextBlockProximityStrategy(bool compareFontInfo = true,
                                              Func<DataTextBlockGroup, IDataBlock>? customCompile = null,
                                              float verticalDistanceTolerance = DIST_TOLERANCE_PERCENT_VERTICAL,
                                              float horizontalDistanceTolerance = DIST_TOLERANCE_PERCENT_HORIZONTAL)
             : base(customCompile)
        {
            this._compareFontInfo = compareFontInfo;
            this._verticalDistanceTolerance = verticalDistanceTolerance;
            this._horizontalDistanceTolerance = horizontalDistanceTolerance;
        }

        #endregion

        #region Methods

   

        #region Tools

        /// <summary>
        /// Determines whether this instance can merge the specified current.
        /// </summary>
        protected override bool CanMerge(DataTextBlockGroup current, DataTextBlockGroup other)
        {
            if (this._compareFontInfo && (current.FontUid != other.FontUid || current.LineSize != other.LineSize))
                return false;

            if (current.Magnitude != other.Magnitude)
                return false;

            var centerDiff = other.Center - current.Center;
            var distLength = Math.Abs(centerDiff.Length());

            var angle = BlockCoordHelper.RadianAngle(current.TopLine, other.TopLine);

            if (Math.Abs(angle) > BlockCoordHelper.ALIGN_MAGNITUDE_TOLERANCE)
                return false;

            var horizontalTestAngleRad = BlockCoordHelper.RadianAngle(current.TopLine, centerDiff);
            var verticalTestAngleRad = BlockCoordHelper.RadianAngle(current.LeftLine, centerDiff);

            bool isHorizontalCompare = (horizontalTestAngleRad < verticalTestAngleRad);

            if (isHorizontalCompare)
            {
                if (this._horizontalDistanceTolerance == 0 || !current.IsInHorizontalLimit(other))
                    return false;

                var projectPointOnHorizontalLenght = Math.Cos(horizontalTestAngleRad) * distLength;

                return Math.Abs(projectPointOnHorizontalLenght) < (current.HalfTopLineLength + other.HalfTopLineLength + (current.SpaceWidth * this._horizontalDistanceTolerance));
            }

            if (this._verticalDistanceTolerance == 0 || !current.IsInVerticalLimit(other))
                return false;

            var projectPointOnVerticalLenght = Math.Cos(verticalTestAngleRad) * distLength;
            return Math.Abs(projectPointOnVerticalLenght) < (current.HalfLeftLineLength + other.HalfLeftLineLength + (current.LineSize * this._verticalDistanceTolerance));

        }

        #endregion

        #endregion
    }
}
