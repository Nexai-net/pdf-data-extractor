// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Abstractions
{
    using PDF.Data.Extractor.Abstractions.Tags;

    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Define a data block
    /// </summary>
    public interface IDataBlock
    {
        /// <summary>
        /// Gets the block unique identifier.
        /// </summary>
        Guid Uid { get; }

        /// <summary>
        /// Gets the rectable area used by the block on the page
        /// </summary>
        /// <remarks>
        ///     This area is mainly used by algorithme to group multiple block that formed the same text.
        ///     Attention: this area is not oriented
        ///     TODO : managed text transformed
        /// </remarks>
        BlockArea Area { get; }

        /// <summary>
        /// Gets the type.
        /// </summary>
        BlockTypeEnum Type { get; }

        /// <summary>
        /// Gets the tags.
        /// </summary>
        IReadOnlyCollection<DataTag>? Tags { get; }

        /// <summary>
        /// Gets the children.
        /// </summary>
        IReadOnlyCollection<DataBlock>? Children { get; }
    }
}
