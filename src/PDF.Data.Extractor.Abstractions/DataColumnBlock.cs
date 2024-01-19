// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Abstractions
{
    /// <summary>
    /// Define a column in the page
    /// </summary>
    /// <seealso cref="PDF.Data.Extractor.Abstractions.DataBlock" />
    public sealed class DataColumnBlock : DataBlock
    {
        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="DataColumnBlock"/> class.
        /// </summary>
        public DataColumnBlock(Guid uid,
                               BlockArea area,
                               IEnumerable<Guid>? blocksContained)
            : base(uid, BlockTypeEnum.Column, area, null, null)
        {
            this.BlocksContained = blocksContained?.ToArray();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the blocks contained.
        /// </summary>
        public IReadOnlyCollection<Guid>? BlocksContained { get; }

        #endregion
    }
}
