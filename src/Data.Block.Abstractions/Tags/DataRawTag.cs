// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace Data.Block.Abstractions.Tags
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Define raw tag information directly extract from pdf
    /// </summary>
    /// <seealso cref="DataTag" />
    [DataContract]
    public sealed class DataRawTag : DataTag
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataRawTag"/> class.
        /// </summary>
        public DataRawTag(string raw)
            : base(DataTagTypeEnum.Raw, raw)
        {
        }
    }
}
