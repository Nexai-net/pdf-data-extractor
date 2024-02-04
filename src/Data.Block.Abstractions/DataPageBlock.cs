// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace Data.Block.Abstractions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;

    /// <summary>
    /// Define a page in the document
    /// </summary>
    /// <seealso cref="DataBlock" />
    [DataContract]
    public sealed class DataPageBlock : DataBlock
    {
        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="DataPageBlock"/> class.
        /// </summary>
        public DataPageBlock(Guid uid,
                             int number,
                             int rotation,
                             BlockArea area,
                             IReadOnlyCollection<DataRelationBlock> relations,
                             IReadOnlyCollection<DataBlock>? children)
            : base(uid, BlockTypeEnum.Page, area, null, children)
        {
            this.Number = number;
            this.Rotation = rotation;
            this.Relations = relations?.ToArray() ?? Array.Empty<DataRelationBlock>();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the number in the document
        /// </summary>
        [DataMember]
        public int Number { get; }

        /// <summary>
        /// Gets the rotation.
        /// </summary>
        [DataMember]
        public int Rotation { get; }

        /// <summary>
        /// Gets the relations of block inside the pages.
        /// </summary>
        [DataMember]
        public IReadOnlyCollection<DataRelationBlock> Relations { get; }

        #endregion
    }
}
