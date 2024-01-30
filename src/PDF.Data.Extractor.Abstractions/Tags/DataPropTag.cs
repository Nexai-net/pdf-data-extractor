// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Abstractions.Tags
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Define raw tag information directly extract from pdf
    /// </summary>
    /// <seealso cref="DataTag" />
    [DataContract]
    public sealed class DataPropTag : DataTag
    {
        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="DataRawTag"/> class.
        /// </summary>
        public DataPropTag(string prop, string value, string raw) 
            : base(DataTagTypeEnum.Prop, raw)
        {
            this.Prop = prop;
            this.Value = value;
        }
        
        #endregion

        #region Properties

        /// <summary>
        /// Gets the property.
        /// </summary>
        [DataMember]
        public string Prop { get; }

        /// <summary>
        /// Gets the value.
        /// </summary>
        [DataMember]
        public string Value { get; }

        #endregion

        #region Methods

        /// <inheritdoc cref="object.Equals(object?)" />
        protected override bool OnEquals(DataTag tag)
        {
            return tag is DataPropTag prop &&
                   string.Equals(this.Prop, prop.Prop, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(this.Value, prop.Value, StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}
