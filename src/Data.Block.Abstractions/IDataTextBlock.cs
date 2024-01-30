// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace Data.Block.Abstractions
{
    using System;

    /// <summary>
    /// Define a data block type text
    /// </summary>
    public interface IDataTextBlock : IDataBlock
    {
        /// <summary>
        /// Gets the size of the ligne.
        /// </summary>
        float LineSize { get; }

        /// <summary>
        /// Gets a font level value used to identify the font size requested.
        /// </summary>
        float FontLevel { get; }

        /// <summary>
        /// Gets the point value pixel of a point in during extraction.
        /// </summary>
        float PointValue { get; }

        /// <summary>
        /// Gets the scale from 0 to 1 in percent.
        /// </summary>
        float Scale { get; }

        /// <summary>
        /// Gets the text.
        /// </summary>
        string Text { get; }

        /// <summary>
        /// Gets the magnitude in degree
        /// </summary>
        float Magnitude { get; }

        /// <summary>
        /// Gets the font information uid.
        /// </summary>
        Guid FontInfoUid { get; }

        /// <summary>
        /// Gets the inter ligne space.
        /// </summary>
        float SpaceWidth { get; }

        /// <summary>
        /// Gets the text box identifier.
        /// </summary>
        IReadOnlyCollection<float>? TextBoxIds { get; }
    }
}
