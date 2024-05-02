// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace Data.Block.Abstractions
{
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.Serialization;

    /// <summary>
    /// Define a point in user space
    /// </summary>
    [DataContract]
    [Serializable]
    [ImmutableObject(true)]
    [DebuggerDisplay("({X}, {Y})")]
    public readonly struct BlockPoint
    {
        #region Fields

        /// <summary>
        /// Initializes a new instance of the <see cref="BlockPoint"/> struct.
        /// </summary>
        public BlockPoint(float x, float y)
        {
            this.X = x;
            this.Y = y;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the x.
        /// </summary>
        [DataMember]
        public float X { get; }

        /// <summary>
        /// Gets the y.
        /// </summary>
        [DataMember] 
        public float Y { get; }

        #endregion

        #region Methods

        /// <inheritdoc />
        public static BlockPoint operator+(BlockPoint source, BlockPoint other)
        {
            return new BlockPoint(source.X + other.X, source.Y + other.Y);
        }

        /// <inheritdoc />
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is BlockPoint otherBP)
            {
                return this.X == otherBP.X &&
                       this.Y == otherBP.Y; 
            }

            return false;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(this.X, this.Y);
        }

        #endregion
    }
}
