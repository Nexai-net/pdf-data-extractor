// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Abstractions
{
    using Newtonsoft.Json;

    using System.Runtime.Serialization;

    /// <summary>
    /// Area covert by a block
    /// </summary>
    public sealed class BlockArea
    {
        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="BlockArea"/> class.
        /// </summary>
        public BlockArea(float x, float y, float width, float height)
        {
            this.X = x;
            this.Y = y;
            this.Width = width;
            this.Height = height;

            this.TopLeft = new BlockPoint(x, y);
            this.TopRight = new BlockPoint(x + width, y);
            this.BottomRight = new BlockPoint(x + width, y + height);
            this.BottomLeft = new BlockPoint(x, y + height);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the x coord.
        /// </summary>
        public float X { get; }

        /// <summary>
        /// Gets the y coord.
        /// </summary>
        public float Y { get; }

        /// <summary>
        /// Gets the bounding box width.
        /// </summary>
        /// <remarks>
        ///     Draw from top to bottom (0, 0) is on the top right
        /// </remarks>
        public float Width { get; }

        /// <summary>
        /// Gets the bouding box height.
        /// </summary>
        /// <remarks>
        ///     Draw from top to bottom (0, 0) is on the top right
        /// </remarks>
        public float Height { get; }

        /// <summary>
        /// Gets the top left corner
        /// </summary>
        [JsonIgnore]
        [IgnoreDataMember]
        public BlockPoint TopLeft { get; }

        /// <summary>
        /// Gets the top right corner
        /// </summary>
        [JsonIgnore]
        [IgnoreDataMember]
        public BlockPoint TopRight { get; }

        /// <summary>
        /// Gets the bottom right corner
        /// </summary>
        [JsonIgnore]
        [IgnoreDataMember]
        public BlockPoint BottomRight { get; }

        /// <summary>
        /// Gets the bottom right corner
        /// </summary>
        [JsonIgnore]
        [IgnoreDataMember]
        public BlockPoint BottomLeft { get; }

        #endregion
    }
}
