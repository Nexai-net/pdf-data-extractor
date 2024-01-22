// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Abstractions
{
    using PDF.Data.Extractor.Abstractions.Tags;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;

    /// <summary>
    /// Define an image in a page
    /// </summary>
    /// <seealso cref="DataBlock" />
    [DataContract]
    public sealed class DataImageBlock : DataBlock
    {
        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="DataImageBlock"/> class.
        /// </summary>
        public DataImageBlock(Guid uid,
                              string name,
                              string imageType,
                              byte[] imageEncodedBytesBase64,
                              float width,
                              float height,
                              BlockArea area,
                              IEnumerable<BlockPoint> shapePoints,
                              IReadOnlyCollection<DataTag> tags,
                              IEnumerable<DataBlock>? children)
            : base(uid, BlockTypeEnum.Image, area, tags, children)
        {
            this.Name = name;
            this.ImageType = imageType;
            this.ImageEncodedBytesBase64 = imageEncodedBytesBase64;
            this.Width = width;
            this.Height = height;
            this.ShapePoints = shapePoints?.ToArray() ?? Array.Empty<BlockPoint>();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the image name in the document.
        /// </summary>
        [DataMember]
        public string Name { get; }

        /// <summary>
        /// Gets the type of the image.
        /// </summary>
        /// <remarks>
        ///     Could be used as file extension
        /// </remarks>
        [DataMember]
        public string ImageType { get; }

        /// <summary>
        /// Gets the image encoded bytes.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public byte[]? ImageEncodedBytesBase64 { get; private set; }

        /// <summary>
        /// Gets the width.
        /// </summary>
        [DataMember]
        public float Width { get; }

        /// <summary>
        /// Gets the height.
        /// </summary>
        [DataMember]
        public float Height { get; }

        /// <summary>
        /// Gets points forming a shape use by the image in the page
        /// </summary>
        [DataMember]
        public IEnumerable<BlockPoint> ShapePoints { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Clears the image bytes.
        /// </summary>
        public void ClearImageBytes()
        {
            this.ImageEncodedBytesBase64 = null;
        }

        #endregion
    }
}
