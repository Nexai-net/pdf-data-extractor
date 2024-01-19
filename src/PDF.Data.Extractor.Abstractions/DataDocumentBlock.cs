// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Abstractions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Data block representing a full document
    /// </summary>
    /// <seealso cref="DataBlock" />
    public sealed class DataDocumentBlock : DataBlock
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataDocumentBlock"/> class.
        /// </summary>
        public DataDocumentBlock(Guid uid,
                                 string? fileName,
                                 BlockArea area,
                                 IEnumerable<DataPageBlock> children,
                                 string pdfVersion,
                                 string author,
                                 string keywords,
                                 string producer,
                                 string subject,
                                 string title)
            : base(uid, BlockTypeEnum.Document, area, null, children)
        {
            this.Guid = guid;
            this.FileName = fileName;
            this.PageBlocks = pageBlocks?.ToArray() ?? Array.Empty<DataPageBlock>();
            this.PDFVersion = pdfVersion;
            this.Author = author;
            this.Keywords = keywords;
            this.Producer = producer;
            this.subject = subject;
            this.Title = title;
        }

        public Guid Guid { get; }
        public string? FileName { get; }
        public string PDFVersion { get; }
        public string Author { get; }
        public string Keywords { get; }
        public string Producer { get; }
        public string subject { get; }
        public string Title { get; }
    }
}
