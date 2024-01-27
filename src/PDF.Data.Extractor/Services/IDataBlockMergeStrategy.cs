// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Services
{
    using PDF.Data.Extractor.Abstractions;

    using System.Collections.Generic;

    /// <summary>
    /// Strategy used to merge <see cref="IDataBlock"/>
    /// </summary>
    public interface IDataBlockMergeStrategy
    {
        /// <summary>
        /// Determines whether <paramref name="block"/> is managed by this strategy
        /// </summary>
        bool IsDataBlockManaged(IDataBlock block);

        /// <summary>
        /// Merges the specified data blocks if needed, return all remain blocks
        /// </summary>
        IReadOnlyCollection<IDataBlock> Merge(IEnumerable<IDataBlock> dataBlocks, CancellationToken token);
    }
}
