// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Abstractions
{
    using PDF.Data.Extractor.Abstractions.Tags;

    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.Serialization;

    /// <summary>
    /// Represent a data block containing text information
    /// </summary>
    [DataContract]
    [DebuggerDisplay("{Text}")]
    public sealed class DataTextBlock : DataBlock
    {
        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTextBlock"/> class.
        /// </summary>
        public DataTextBlock(Guid uid,
                             float fontLevel,
                             float pointValue,
                             float ligneSize,
                             float scale,
                             float magnitude,
                             string text,
                             Guid fontInfoUid,
                             float spaceWidth,
                             BlockArea area,
                             IEnumerable<float>? textBoxId,
                             IEnumerable<DataTag>? tags,
                             IEnumerable<DataBlock>? children) 
            : base(uid, BlockTypeEnum.Text, area, tags, children)
        {
            this.LineSize = ligneSize;
            this.FontLevel = fontLevel;
            this.PointValue = pointValue;
            this.Scale = scale;
            this.Text = text;
            this.Magnitude = magnitude;
            this.FontInfoUid = fontInfoUid;
            this.SpaceWidth = spaceWidth;
            this.TextBoxIds = textBoxId?.ToArray();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the size of the ligne.
        /// </summary>
        [DataMember]
        public float LineSize { get; }

        /// <summary>
        /// Gets a font level value used to identify the font size requested.
        /// </summary>
        [DataMember]
        public float FontLevel { get; }

        /// <summary>
        /// Gets the point value pixel of a point in during extraction.
        /// </summary>
        [DataMember]
        public float PointValue { get; }

        /// <summary>
        /// Gets the scale from 0 to 1 in percent.
        /// </summary>
        [DataMember]
        public float Scale { get; }

        /// <summary>
        /// Gets the text.
        /// </summary>
        [DataMember]
        public string Text { get; }

        /// <summary>
        /// Gets the magnitude in degree
        /// </summary>
        [DataMember]
        public float Magnitude { get; }

        /// <summary>
        /// Gets the font information uid.
        /// </summary>
        [DataMember]
        public Guid FontInfoUid { get; }

        /// <summary>
        /// Gets the inter ligne space.
        /// </summary>
        [DataMember]
        public float SpaceWidth { get; }

        /// <summary>
        /// Gets the text box identifier.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public IReadOnlyCollection<float>? TextBoxIds { get; }

        #endregion
    }
}
