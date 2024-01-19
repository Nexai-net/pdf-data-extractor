// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Abstractions
{
    using PDF.Data.Extractor.Abstractions.Tags;

    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Define an image in a page
    /// </summary>
    /// <seealso cref="DataBlock" />
    public sealed class DataImageBlock : DataBlock
    {
        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="DataImageBlock"/> class.
        /// </summary>
        public DataImageBlock(Guid uid,
                              string name,
                              string imageType,
                              byte[] imageEncodedBytes,
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
            this.ImageEncodedBytes = imageEncodedBytes;
            this.Width = width;
            this.Height = height;
            this.ShapePoints = shapePoints?.ToArray() ?? Array.Empty<BlockPoint>();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the image name in the document.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the type of the image.
        /// </summary>
        /// <remarks>
        ///     Could be used as file extension
        /// </remarks>
        public string ImageType { get; }

        /// <summary>
        /// Gets the image encoded bytes.
        /// </summary>
        public byte[] ImageEncodedBytes { get; }

        /// <summary>
        /// Gets the width.
        /// </summary>
        public float Width { get; }

        /// <summary>
        /// Gets the height.
        /// </summary>
        public float Height { get; }

        /// <summary>
        /// Gets points forming a shape use by the image in the page
        /// </summary>
        public IEnumerable<BlockPoint> ShapePoints { get; }

        #endregion
    }
}
