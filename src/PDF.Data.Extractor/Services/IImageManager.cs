// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Services
{
    using iText.Kernel.Pdf.Xobject;
    using iText.Layout.Element;

    using PDF.Data.Extractor.Abstractions.MetaData;

    /// <summary>
    /// Manager in charge to store image as resources 
    /// </summary>
    public interface IImageManager
    {
        /// <summary>
        /// Adds the image resource.
        /// </summary>
        ImageMetaData AddImageResource(PdfImageXObject image);

        /// <summary>
        /// Gets the image meta datas.
        /// </summary>
        IReadOnlyCollection<ImageMetaData> GetAll();
    }
}
