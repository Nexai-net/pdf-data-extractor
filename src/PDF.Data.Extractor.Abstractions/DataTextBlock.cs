// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Abstractions
{
    using PDF.Data.Extractor.Abstractions.Tags;

    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represent a data block containing text information
    /// </summary>
    public sealed class DataTextBlock : DataBlock
    {
        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTextBlock"/> class.
        /// </summary>
        public DataTextBlock(Guid uid,
                             string? actualText,
                             float fontLevel,
                             float scale,
                             string text,
                             BlockArea area,
                             IEnumerable<DataTag> tags,
                             IEnumerable<DataTextBlock>? children) 
            : base(uid, BlockTypeEnum.Text, area, tags, children)
        {
            this.ActualText = actualText;
            this.FontLevel = fontLevel;
            this.Scale = scale;
            this.Text = text;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the actual text.
        /// </summary>
        public string? ActualText { get; }

        /// <summary>
        /// Gets a font level value used to identify the font size requested.
        /// </summary>
        public float FontLevel { get; }

        /// <summary>
        /// Gets the scale from 0 to 1 in percent.
        /// </summary>
        public float Scale { get; }

        /// <summary>
        /// Gets the text.
        /// </summary>
        public string Text { get; }

        #endregion
    }
}
