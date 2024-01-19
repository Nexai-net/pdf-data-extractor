// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Abstractions.Tags
{
    /// <summary>
    /// Define language tag information directly extract from pdf
    /// </summary>
    /// <seealso cref="DataTag" />
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
        public string Lang { get; }

        #endregion
    }
}
