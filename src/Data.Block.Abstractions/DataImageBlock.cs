// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace Data.Block.Abstractions
{
    using Data.Block.Abstractions.Tags;

    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Define an image in a page
    /// </summary>
    /// <seealso cref="DataBlock" />
    [DataContract]
    public sealed class DataImageBlock : DataBlock
    {
        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="DataImageBlock"/> class.
        /// </summary>
        public DataImageBlock(Guid uid,
                              string name,
                              Guid imageResourceUid,
                              BlockArea area,
                              IReadOnlyCollection<DataTag> tags,
                              IEnumerable<DataBlock>? children)
            : base(uid, BlockTypeEnum.Image, area, tags, children)
        {
            this.Name = name;
            this.ImageResourceUid = imageResourceUid;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the image name in the document.
        /// </summary>
        [DataMember]
        public string Name { get; }

        /// <summary>
        /// Gets the image resource uid.
        /// </summary>
        [DataMember]
        public Guid ImageResourceUid { get; }

        #endregion
    }
}
