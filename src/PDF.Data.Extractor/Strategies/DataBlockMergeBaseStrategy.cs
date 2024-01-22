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
    /// <seealso cref="PDF.Data.Extractor.Services.IDataTextBlockMergeStrategy" />
    public abstract class DataBlockMergeBaseStrategy<TBlockType> : IDataBlockMergeStrategy
        where TBlockType : DataBlock
    {
        #region Methods

        /// <inheritdoc />
        public virtual bool IsDataBlockManaged(DataBlock block)
        {
            return block.GetType().IsAssignableTo(typeof(TBlockType));
        }

        /// <inheritdoc />
        public IReadOnlyCollection<DataBlock> Merge(IEnumerable<DataBlock> dataBlocks)
        {
            return Merge(dataBlocks.Cast<TBlockType>().ToArray());
        }

        /// <inheritdoc cref="IDataBlockMergeStrategy.Merge(IEnumerable{DataBlock})" />
        protected abstract IReadOnlyCollection<DataBlock> Merge(IReadOnlyCollection<TBlockType> dataBlocks);

        #endregion
    }
}
