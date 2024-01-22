// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Services
{
    using iText.Kernel.Font;

    using PDF.Data.Extractor.Abstractions.MetaData;

    /// <summary>
    /// Define a strategy in charge to handled the font information
    /// </summary>
    public interface IFontMetaDataInfoExtractStrategy
    {
        /// <summary>
        /// Return a new <see cref="TextFontMetaData"/> create or get from cache
        /// </summary>
        TextFontMetaData AddOrGetFontInfo(float fontSize, PdfFont font);
    }
}
