// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace Data.Block.Abstractions
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Define a realtion between blocks in the page
    /// </summary>
    /// <seealso cref="DataBlock" />
    [DataContract]
    public sealed class DataRelationBlock : DataBlock
    {
        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="DataRelationBlock"/> class.
        /// </summary>
        public DataRelationBlock(Guid uid,
                                 BlockArea area,
                                 BlockRelationTypeEnum blockRelationType,
                                 string customGroupType,
                                 IReadOnlyCollection<Guid>? blocksContained)
            : base(uid, BlockTypeEnum.Relation, area, null, null)
        {
            this.BlocksContained = blocksContained?.ToArray();
            this.BlockRelationType = blockRelationType;
            this.CustomGroupType = customGroupType;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets odered blocks in relation.
        /// </summary>
        [DataMember]
        public IReadOnlyCollection<Guid>? BlocksContained { get; }

        /// <summary>
        /// Gets the type of the block relation.
        /// </summary>
        [DataMember]
        public BlockRelationTypeEnum BlockRelationType { get; }

        /// <summary>
        /// Gets the type of the custom group.
        /// </summary>
        [DataMember]
        public string CustomGroupType { get; }

        #endregion
    }
}
