// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor
{
    using PDF.Data.Extractor.Services;

    using System;

    /// <summary>
    /// Option use to custom extraction process
    /// </summary>
    public sealed class PDFExtractorOptions
    {
        #region Properties

        /// <summary>
        /// Gets or sets the page range to extract; null by default
        /// </summary>
        public Range? PageRange { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the extraction must be asynchronous.
        /// </summary>
        /// <remarks>
        ///     Async by page
        /// </remarks>
        public bool Asynchronous {  get; set; }

        /// <summary>
        /// Gets or sets the override strategies.
        /// </summary>
        public IList<IDataBlockMergeStrategy>? OverrideStrategies { get; set; }

        #endregion
    }
}
