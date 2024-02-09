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
    public record struct BlockPoint(float X, float Y);
}
