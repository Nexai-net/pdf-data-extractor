// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace Data.Block.Abstractions.Tags
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Define language tag information directly extract from pdf
    /// </summary>
    /// <seealso cref="DataTag" />
    [DataContract]
    public sealed class DataLangTag : DataTag
    {
        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="DataLangTag"/> class.
        /// </summary>
        public DataLangTag(string lang, string raw)
            : base(DataTagTypeEnum.Lang, raw)
        {
            this.Lang = lang;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the language.
        /// </summary>
        [DataMember]
        public string Lang { get; }

        #endregion

        #region Methods

        /// <inheritdoc cref="object.Equals(object?)" />
        protected override bool OnEquals(DataTag tag)
        {
            return tag is DataLangTag lng &&
                   string.Equals(this.Lang, lng.Lang, StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}
