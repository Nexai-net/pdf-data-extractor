﻿// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Abstractions
{
    using Newtonsoft.Json;

    using PDF.Data.Extractor.Abstractions.Tags;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Define the base type of all the block (Text, image, ...)
    /// </summary>
    [DataContract]
    [JsonDerivedType(typeof(DataTextBlock), "text")]
    [JsonDerivedType(typeof(DataImageBlock), "image")]
    [JsonDerivedType(typeof(DataPageBlock), "page")]
    [JsonDerivedType(typeof(DataDocumentBlock), "document")]
    [JsonDerivedType(typeof(DataColumnBlock), "column")]
    public abstract class DataBlock
    {
        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="DataBlock"/> class.
        /// </summary>
        protected DataBlock(Guid uid,
                            BlockTypeEnum type,
                            BlockArea area,
                            IEnumerable<DataTag>? tags,
                            IEnumerable<DataBlock>? children)
        {
            this.Uid = uid;
            this.Type = type;
            this.Area = area;
            this.Tags = tags?.ToArray();
            this.Children = children?.ToArray();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the block unique identifier.
        /// </summary>
        [DataMember]
        public Guid Uid { get; }

        /// <summary>
        /// Gets the rectable area used by the block on the page
        /// </summary>
        [DataMember]
        public BlockArea Area { get; }

        /// <summary>
        /// Gets the type.
        /// </summary>
        [DataMember]
        public BlockTypeEnum Type { get; }

        /// <summary>
        /// Gets the tags.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public IReadOnlyCollection<DataTag>? Tags { get; }

        /// <summary>
        /// Gets the children.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public IReadOnlyCollection<DataBlock>? Children { get; }


        #endregion
    }
}
