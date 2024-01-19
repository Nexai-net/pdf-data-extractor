// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Abstractions.Tags
{
    /// <summary>
    /// PDF tag, meta-data link to block
    /// </summary>
    public abstract class DataTag
    {
        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTag"/> class.
        /// </summary>
        protected DataTag(DataTagTypeEnum type, string raw)
        {
            this.Type = type;
            this.Raw = raw;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the type.
        /// </summary>
        public DataTagTypeEnum Type { get; }

        /// <summary>
        /// Gets the raw.
        /// </summary>
        public string Raw { get; }

        #endregion
    }
}
