// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Abstractions
{
    using System.Diagnostics;
    using System.Numerics;
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Area covert by a block
    /// </summary>
    [DataContract]
    [DebuggerDisplay("X:{X}, Y:{Y}, Width: {Width}, Height: {Height}")]
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
            this.TopLine = BlockCoordHelper.Diff(this.TopLeft, this.TopRight);
            this.BottomLine = BlockCoordHelper.Diff(this.BottomLeft, this.BottomRight);
            this.LeftLine = BlockCoordHelper.Diff(this.TopLeft, this.BottomLeft);
            this.RightLine = BlockCoordHelper.Diff(this.TopRight, this.BottomRight);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the x coord.
        /// </summary>
        [DataMember]
        public float X { get; }

        /// <summary>
        /// Gets the y coord.
        /// </summary>
        [DataMember]
        public float Y { get; }

        /// <summary>
        /// Gets the bounding box width.
        /// </summary>
        /// <remarks>
        ///     Draw from top to bottom (0, 0) is on the top right
        /// </remarks>
        [DataMember]
        public float Width { get; }

        /// <summary>
        /// Gets the bouding box height.
        /// </summary>
        /// <remarks>
        ///     Draw from top to bottom (0, 0) is on the top right
        /// </remarks>
        [DataMember]
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

        /// <summary>
        /// Gets the bottom line (Left to Right)
        /// </summary>
        [JsonIgnore]
        [IgnoreDataMember]
        public Vector2 BottomLine { get; }

        /// <summary>
        /// Gets the top line (Left to Right)
        /// </summary>
        [JsonIgnore]
        [IgnoreDataMember]
        public Vector2 TopLine { get; }

        /// <summary>
        /// Gets the left line (Top to bottom)
        /// </summary>
        [JsonIgnore]
        [IgnoreDataMember]
        public Vector2 LeftLine { get; }

        /// <summary>
        /// Gets the right line Top to bottom)
        /// </summary>
        [JsonIgnore]
        [IgnoreDataMember]
        public Vector2 RightLine { get; }

        #endregion
    }
}
