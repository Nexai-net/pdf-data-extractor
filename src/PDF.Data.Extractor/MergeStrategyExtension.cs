// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor
{
    using PDF.Data.Extractor.Abstractions;
    using PDF.Data.Extractor.Services;

    using System.Collections.Generic;

    /// <summary>
    /// Extension method to simplify treatment
    /// </summary>
    public static class MergeStrategyExtension
    {
        /// <summary>
        /// Apply a group of <see cref="IDataBlockMergeStrategy"/> to group of <see cref="DataBlock"/>
        /// </summary>
        public static IReadOnlyCollection<DataBlock> Apply(this IReadOnlyCollection<IDataBlockMergeStrategy> strategies,
                                                           IReadOnlyCollection<DataBlock> childrenBlocks,
                                                           CancellationToken token)
        {
            if (childrenBlocks == null || !childrenBlocks.Any())
                return Array.Empty<DataBlock>();

            var localChildrenBlocks = childrenBlocks.ToList();
            foreach (var strategy in strategies)
            {
                var impacted = new List<DataBlock>();
                for (int i = 0; i < localChildrenBlocks.Count; ++i)
                {
                    var block = localChildrenBlocks[i];
                    if (strategy.IsDataBlockManaged(block))
                    {
                        impacted.Add(block);
                        localChildrenBlocks.RemoveAt(i);
                        i--;
                    }

                    token.ThrowIfCancellationRequested();
                }

                var remains = strategy.Merge(impacted, token);
                localChildrenBlocks.AddRange(remains);
            }

            return localChildrenBlocks;
        }
    }
}
