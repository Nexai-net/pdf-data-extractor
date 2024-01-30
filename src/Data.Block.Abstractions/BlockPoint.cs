// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace Data.Block.Abstractions
{
    using System.Diagnostics;
    using System.Runtime.Serialization;

    /// <summary>
    /// Define a point in user space
    /// </summary>
    [DataContract]
    [DebuggerDisplay("({X}, {Y})")]
    public readonly struct BlockPoint
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BlockPoint"/> struct.
        /// </summary>
        public BlockPoint(float x, float y)
        {
            this.X = x;
            this.Y = y;
        }

        [DataMember]
        public float X { get; }

        [DataMember]
        public float Y { get; }
    }
}
