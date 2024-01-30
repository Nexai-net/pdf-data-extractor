// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Services
{
    using iText.Kernel.Font;

    using global::Data.Block.Abstractions.MetaData;

    using System;

    /// <summary>
    /// Manager in charge to store and provide all font information
    /// </summary>
    public interface IFontManager
    {
        /// <summary>
        /// Return a new <see cref="TextFontMetaData"/> create or get from cache
        /// </summary>
        TextFontMetaData AddOrGetFontInfo(float fontSize, PdfFont font);

        /// <summary>
        /// Gets <see cref="TextFontMetaData"/> from it's unique id
        /// </summary>
        TextFontMetaData Get(Guid uid);

        /// <summary>
        /// Gets a new collection of all font stored
        /// </summary>
        IReadOnlyCollection<TextFontMetaData> GetAll();
    }
}
