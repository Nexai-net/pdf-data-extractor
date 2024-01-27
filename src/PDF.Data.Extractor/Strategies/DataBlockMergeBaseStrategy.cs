// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Strategies
{
    using PDF.Data.Extractor.Abstractions;
    using PDF.Data.Extractor.Services;

    using System.Collections.Generic;

    /// <summary>
    /// Base class of all strategy of merge algorithme
    /// </summary>
    /// <seealso cref="IDataTextBlockMergeStrategy" />
    public abstract class DataBlockMergeBaseStrategy<TBlockType> : IDataBlockMergeStrategy
        where TBlockType : IDataBlock
    {
        #region Methods

        /// <inheritdoc />
        public virtual bool IsDataBlockManaged(IDataBlock block)
        {
            return block.GetType().IsAssignableTo(typeof(TBlockType));
        }

        /// <inheritdoc />
        public IReadOnlyCollection<IDataBlock> Merge(IEnumerable<IDataBlock> dataBlocks, CancellationToken token)
        {
            return Merge(dataBlocks.Cast<TBlockType>(), token);
        }

        /// <inheritdoc cref="IDataBlockMergeStrategy.Merge(IEnumerable{DataBlock})" />
        protected abstract IReadOnlyCollection<IDataBlock> Merge(IEnumerable<TBlockType> dataBlocks, CancellationToken token);

        #endregion
    }
}
