// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace Data.Block.Abstractions
{
    using System.ComponentModel;
    using System.Diagnostics;

    /// <summary>
    /// Define a point in user space
    /// </summary>
    //[DataContract]
    [Serializable]
    [ImmutableObject(true)]
    [DebuggerDisplay("({X}, {Y})")]
    public record struct BlockPoint(float X, float Y)
    {
        /// <inheritdoc />
        public static BlockPoint operator +(BlockPoint source, BlockPoint other)
        {
            return new BlockPoint(source.X + other.X, source.Y + other.Y);
        }
    }

    //    /// <summary>
    //    /// Define a point in user space
    //    /// </summary>
    //    [DataContract]
    //    [Serializable]
    //    [ImmutableObject(true)]
    //    [DebuggerDisplay("({X}, {Y})")]
    //#pragma warning disable IDE0250 // Make struct 'readonly' System.Text.Json failed to deserialize readonly struct
    //    public struct BlockPoint
    //#pragma warning restore IDE0250 // Make struct 'readonly'
    //    {
    //        #region Fields

    //        /// <summary>
    //        /// Initializes a new instance of the <see cref="BlockPoint"/> struct.
    //        /// </summary>
    //        public BlockPoint(float x, float y)
    //        {
    //            this.X = x;
    //            this.Y = y;
    //        }

    //        #endregion

    //        #region Properties

    //        /// <summary>
    //        /// Gets the x.
    //        /// </summary>
    //        [DataMember]
    //        public float X { get; }

    //        /// <summary>
    //        /// Gets the y.
    //        /// </summary>
    //        [DataMember] 
    //        public float Y { get; }

    //        #endregion

    //        #region Methods

    //        /// <inheritdoc />
    //        public static BlockPoint operator+(BlockPoint source, BlockPoint other)
    //        {
    //            return new BlockPoint(source.X + other.X, source.Y + other.Y);
    //        }

    //        /// <inheritdoc />
    //        public override bool Equals([NotNullWhen(true)] object? obj)
    //        {
    //            if (obj is BlockPoint otherBP)
    //            {
    //                return this.X == otherBP.X &&
    //                       this.Y == otherBP.Y; 
    //            }

    //            return false;
    //        }

    //        /// <inheritdoc />
    //        public override int GetHashCode()
    //        {
    //            return HashCode.Combine(this.X, this.Y);
    //        }

    //        #endregion
    //    }
}
