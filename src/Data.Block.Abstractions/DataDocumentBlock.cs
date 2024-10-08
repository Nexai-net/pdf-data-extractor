﻿// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace Data.Block.Abstractions
{
    using Data.Block.Abstractions.MetaData;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;

    /// <summary>
    /// Data block representing a full document
    /// </summary>
    /// <seealso cref="DataBlock" />
    [DataContract]
    public sealed class DataDocumentBlock : DataBlock
    {
        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="DataDocumentBlock"/> class.
        /// </summary>
        public DataDocumentBlock(Guid uid,
                                 string? fileName,
                                 BlockArea area,
                                 IReadOnlyCollection<DataBlock> children,
                                 string pdfVersion,
                                 string author,
                                 string keywords,
                                 string producer,
                                 string subject,
                                 string title,
                                 IReadOnlyCollection<TextFontMetaData>? fonts,
                                 IReadOnlyCollection<ImageMetaData>? images)
            : base(uid, BlockTypeEnum.Document, area, null, children)
        {
            this.FileName = fileName;
            this.PDFVersion = pdfVersion;
            this.Author = author;
            this.Keywords = keywords;
            this.Producer = producer;
            this.Subject = subject;
            this.Title = title;
            this.Fonts = fonts?.ToArray() ?? Array.Empty<TextFontMetaData>();
            this.Images = images?.ToArray() ?? Array.Empty<ImageMetaData>();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        [DataMember]
        public string? FileName { get; }

        /// <summary>
        /// Gets the PDF version.
        /// </summary>
        [DataMember]
        public string PDFVersion { get; }

        /// <summary>
        /// Gets the author.
        /// </summary>
        [DataMember]
        public string Author { get; }

        /// <summary>
        /// Gets the keywords.
        /// </summary>
        [DataMember]
        public string Keywords { get; }

        /// <summary>
        /// Gets the producer.
        /// </summary>
        [DataMember]
        public string Producer { get; }

        /// <summary>
        /// Gets the Subject.
        /// </summary>
        [DataMember]
        public string Subject { get; }

        /// <summary>
        /// Gets the title.
        /// </summary>
        [DataMember]
        public string Title { get; }

        /// <summary>
        /// Gets the fonts used in the documents
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public IReadOnlyCollection<TextFontMetaData>? Fonts { get; }

        /// <summary>
        /// Gets the images resources
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public IReadOnlyCollection<ImageMetaData>? Images { get; }

        #endregion
    }
}
