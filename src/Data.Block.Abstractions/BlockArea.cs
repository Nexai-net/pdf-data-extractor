﻿// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace Data.Block.Abstractions
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Numerics;
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Area covert by a block
    /// </summary>
    [DataContract]
    [DebuggerDisplay("ToString()")]
    public sealed class BlockArea
    {
        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="BlockArea"/> class.
        /// </summary>
        public BlockArea(BlockPoint topLeft,
                         BlockPoint topRight,
                         BlockPoint bottomRight,
                         BlockPoint bottomLeft)
        {
            this.X = topLeft.X;
            this.Y = topLeft.Y;

            this.TopLeft = topLeft;
            this.TopRight = topRight;
            this.BottomRight = bottomRight;
            this.BottomLeft = bottomLeft;

            this.TopLine = BlockCoordHelper.Diff(this.TopLeft, this.TopRight);
            this.BottomLine = BlockCoordHelper.Diff(this.BottomLeft, this.BottomRight);
            this.LeftLine = BlockCoordHelper.Diff(this.TopLeft, this.BottomLeft);
            this.RightLine = BlockCoordHelper.Diff(this.TopRight, this.BottomRight);

            this.Width = this.TopLine.Length();
            this.Height = this.LeftLine.Length();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the x coord.
        /// </summary>
        [JsonIgnore]
        [IgnoreDataMember]
        public float X { get; }

        /// <summary>
        /// Gets the y coord.
        /// </summary>
        [JsonIgnore]
        [IgnoreDataMember]
        public float Y { get; }

        /// <summary>
        /// Gets the bounding box width.
        /// </summary>
        /// <remarks>
        ///     Draw from top to bottom (0, 0) is on the top right
        /// </remarks>
        [JsonIgnore]
        [IgnoreDataMember]
        public float Width { get; }

        /// <summary>
        /// Gets the bouding box height.
        /// </summary>
        /// <remarks>
        ///     Draw from top to bottom (0, 0) is on the top right
        /// </remarks>
        [JsonIgnore]
        [IgnoreDataMember]
        public float Height { get; }

        /// <summary>
        /// Gets the top left corner
        /// </summary>
        [DataMember]
        public BlockPoint TopLeft { get; }

        /// <summary>
        /// Gets the top right corner
        /// </summary>
        [DataMember]
        public BlockPoint TopRight { get; }

        /// <summary>
        /// Gets the bottom right corner
        /// </summary>
        [DataMember]
        public BlockPoint BottomRight { get; }

        /// <summary>
        /// Gets the bottom right corner
        /// </summary>
        [DataMember]
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

        #region Methods

        /// <summary>
        /// Gets new points collection.
        /// </summary>
        public IReadOnlyCollection<BlockPoint> GetPoints()
        {
            return new[] { this.TopLeft, this.TopRight, this.BottomRight, this.BottomLeft };
        }

        /// <summary>
        /// Overlaps the specified other area.
        /// </summary>
        public bool Overlap(BlockArea otherArea)
        {
            return Contains(otherArea.TopLeft) ||
                   Contains(otherArea.TopRight) ||
                   Contains(otherArea.BottomLeft) ||
                   Contains(otherArea.BottomRight) ||

                   otherArea.Contains(this.TopLeft) ||
                   otherArea.Contains(this.TopRight) ||
                   otherArea.Contains(this.BottomLeft) ||
                   otherArea.Contains(this.BottomRight);
        }

        /// <summary>
        /// Overlaps the specified other area.
        /// </summary>
        public bool Overlap(BlockArea otherArea, double tolerance)
        {
            return Contains(otherArea.TopLeft, tolerance) ||
                   Contains(otherArea.TopRight, tolerance) ||
                   Contains(otherArea.BottomLeft, tolerance) ||
                   Contains(otherArea.BottomRight, tolerance) ||

                   otherArea.Contains(this.TopLeft) ||
                   otherArea.Contains(this.TopRight) ||
                   otherArea.Contains(this.BottomLeft) ||
                   otherArea.Contains(this.BottomRight);
        }

        /// <summary>
        /// Determines whether this instance contains the object.
        /// </summary>
        public bool Contains(BlockPoint point)
        {
            return point.X >= this.TopLeft.X &&
                   point.X <= this.TopRight.X &&
                   point.Y >= this.TopLeft.Y &&
                   point.Y <= this.BottomRight.Y;
        }

        /// <summary>
        /// Determines whether this instance contains the object.
        /// </summary>
        public bool Contains(BlockPoint point, double tolerance)
        {
            return (point.X > this.TopLeft.X || Math.Abs(point.X - this.TopLeft.X) < tolerance) &&
                   (point.X < this.TopRight.X || Math.Abs(point.X - this.TopRight.X) < tolerance) &&
                   (point.Y > this.TopLeft.Y || Math.Abs(point.Y - this.TopLeft.Y) < tolerance) &&
                   (point.Y < this.BottomRight.Y || Math.Abs(point.Y - this.BottomRight.Y) < tolerance);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format("TL: {0}, TR: {1}, BR: {2}, BL: {3}",
                                 this.TopLeft,
                                 this.TopRight,
                                 this.BottomRight,
                                 this.BottomLeft);
        }

        /// <inheritdoc />
        public static BlockArea operator+(BlockArea source, BlockPoint translation)
        {
            return new BlockArea(source.TopLeft + translation,
                                 source.TopRight + translation,
                                 source.BottomRight + translation,
                                 source.BottomLeft + translation);
        }

        #endregion
    }
}
