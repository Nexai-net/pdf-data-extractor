// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Abstractions
{
    using System;
    using System.Numerics;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Helper used to apply mathematical operation of coordoante object
    /// </summary>
    public static class BlockCoordHelper
    {
        #region Fields

        public static readonly double RIGHT_ANGLE_RADIAN = Math.PI / 2.0d;

        public const float EQUALITY_TOLERANCE = 0.001f;
        public static readonly float ALIGN_MAGNITUDE_TOLERANCE = (float)Math.Cos(3 / Math.PI);

        #endregion

        #region Methods

        /// <summary>
        /// Get the difference between <paramref name="source"/> and point <paramref name="target"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Diff(in BlockPoint source, in BlockPoint target)
        {
            return new Vector2(target.X - source.X, target.Y - source.Y);
        }

        /// <summary>
        /// Define if two double values are equal following the <see cref="EQUALITY_TOLERANCE"/> marge.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CoordEquality(float a, float b)
        {
            return Math.Abs(a - b) > EQUALITY_TOLERANCE;
        }

        /// <summary>
        /// Calculate the dot product between two vectors.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dot(Vector2 vectA, Vector2 vectB)
        {
            return Vector2.Dot(Vector2.Normalize(vectA), Vector2.Normalize(vectB));
        }

        /// <summary>
        /// Radians the angle.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float RadianAngle(Vector2 vectA, Vector2 vectB, bool matchSin = true)
        {
            var dotProduct = Dot(vectA, vectB);
            var directionFactor = 1;

            if (matchSin)
            {
                var sin = Math.Asin(dotProduct);
                directionFactor = (sin > 0 ? 1 : -1);
            }

            return (float)Math.Acos(dotProduct * directionFactor);
        }

        #endregion
    }
}
