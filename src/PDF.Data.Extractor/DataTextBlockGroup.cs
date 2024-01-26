// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor
{
    using PDF.Data.Extractor.Abstractions;
    using PDF.Data.Extractor.Abstractions.Tags;

    using System;
    using System.Numerics;
    using System.Text;

    /// <summary>
    /// Used to simulate DataBlock regroup result
    /// </summary>
    public sealed class DataTextBlockGroup
    {
        #region Properties

        private readonly List<DataTextBlock> _blocks;
        private DataTextBlock? _referentialBlock;

        private long _using;
        private float _midLineSize;

        private BlockPoint? _topLeftOriginPoint;
        private Vector2? _topLeftOriginPointVect;
        private Vector2? _topLineUnit;
        private Vector2? _leftLineUnit;
        private Matrix3x2 _toWorldMatrix;
        private Matrix3x2 _toRelativeMatrix;

        private Vector2 _topLeftWorld;
        private Vector2 _topRightWorld;
        private Vector2 _bottomRightWorld;
        private Vector2 _bottomLeftWorld;
        private Vector2 _topLeftRel;
        private Vector2 _topRightRel;
        private Vector2 _bottomRightRel;
        private Vector2 _bottomLeftRel;

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTextBlockGroup"/> class.
        /// </summary>
        public DataTextBlockGroup()
        {
            this._blocks = new List<DataTextBlock>();
            this.Uid = Guid.NewGuid();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets block unique id
        /// </summary>
        public Guid Uid { get; }

        /// <summary>
        /// Gets the font uid
        /// </summary>
        public Guid FontUid { get; private set; }

        /// <summary>
        /// Get Origin magnitude
        /// </summary>
        public float Magnitude { get; private set; }

        /// <summary>
        /// gets Mcid - text box ids
        /// </summary>
        public IReadOnlyCollection<float>? TextBoxIds { get; private set; }

        /// <summary>
        /// Get line height
        /// </summary>
        public float LineSize { get; private set; }

        /// <summary>
        /// Gets the top line.
        /// </summary>
        public Vector2 TopLine { get; private set; }

        /// <summary>
        /// Gets the length of the top line
        /// </summary>
        public float TopLineLength { get; private set; }


        /// <summary>
        /// Gets the length of the top line / 2
        /// </summary>
        public float HalfTopLineLength { get; private set; }

        /// <summary>
        /// Gets the left line.
        /// </summary>
        public Vector2 LeftLine { get; private set; }

        /// <summary>
        /// Gets the length of the top line
        /// </summary>
        public float LeftLineLength { get; private set; }

        /// <summary>
        /// Gets the length of the top line / 2
        /// </summary>
        public float HalfLeftLineLength { get; private set; }

        /// <summary>
        /// Gets the center with absolute coord
        /// </summary>
        public Vector2 Center { get; private set; }

        /// <summary>
        /// Gets the block counts
        /// </summary>
        public int BlockCount
        {
            get { return this._blocks.Count; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is using.
        /// </summary>
        public bool IsUsed
        {
            get { return Interlocked.Read(ref this._using) != 0; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public void Initialize()
        {
            Interlocked.Increment(ref _using);
        }

        /// <summary>
        /// Clears this instance to be reused
        /// </summary>
        public void Clear()
        {
            Interlocked.Decrement(ref _using);

            this._blocks.Clear();
            this.TopLine = default;
            this.LeftLine = default;
            this.LeftLineLength = 0;
            this.LeftLineLength = 0;
            this._referentialBlock = null;
            this.Center = default;
        }

        /// <summary>
        /// Pushes the data block.
        /// </summary>
        public void Push(DataTextBlock data)
        {
            PushDataImpl(data);
            UpdateGroupInfo();
        }

        /// <summary>
        /// Consumes the specified group.
        /// </summary>
        public void Consume(DataTextBlockGroup other)
        {
            if (other == null || other._blocks.Count == 0) 
                return;

            foreach (var b in other._blocks)
                PushDataImpl(b);

            UpdateGroupInfo();
        }

        /// <summary>
        /// Compiles this instance into <see cref="DataTextBlock"/>
        /// </summary>
        public DataTextBlock Compile()
        {
            var blocks = GetBlockOrderByRelativeCoord();

            if (blocks.Count == 0 || blocks.Count == 1)
                return blocks.FirstOrDefault().Block;

            var tags = new List<DataTag>();
            var sb = new StringBuilder();

            float lastX = float.MinValue;
            foreach (var block in blocks)
            {
                if (block.RelArea.TopLeft.X < lastX)
                    sb.AppendLine();

                lastX = block.RelArea.TopLeft.X;
                sb.Append(block.Block.Text);

                if (block.Block.Tags != null && block.Block.Tags.Count > 0)
                    tags.AddRange(block.Block.Tags);
            }

            return new DataTextBlock(Guid.NewGuid(),
                                     this._referentialBlock!.FontLevel,
                                     this._referentialBlock!.PointValue,
                                     this.LineSize,
                                     this._referentialBlock!.Scale,
                                     this.Magnitude,
                                     sb.ToString(),
                                     this._referentialBlock!.FontInfoUid,
                                     this._referentialBlock!.SpaceWidth,
                                     new BlockArea(new BlockPoint(this._topLeftWorld.X, this._topLeftWorld.Y),
                                                   new BlockPoint(this._topRightWorld.X, this._topRightWorld.Y),
                                                   new BlockPoint(this._bottomRightWorld.X, this._bottomRightWorld.Y),
                                                   new BlockPoint(this._bottomLeftWorld.X, this._bottomLeftWorld.Y)),
                                     this.TextBoxIds.Any() ? this.TextBoxIds.Distinct() : null,
                                     tags.Any() ? tags.Where(t => !string.IsNullOrEmpty(t.Raw)).Distinct() : null,
                                     null);
        }

        /// <summary>
        /// Check relative to current if other grp is local between the top and bottom limit
        /// </summary>
        public bool IsInHorizontalLimit(DataTextBlockGroup grp)
        {
            var topLeft = Vector2.Transform(grp._topLeftWorld - this._topLeftOriginPointVect!.Value, this._toRelativeMatrix);
            var bottomLeft = Vector2.Transform(grp._bottomLeftWorld - this._topLeftOriginPointVect!.Value, this._toRelativeMatrix);

            var limitYMin = this._topLeftRel.Y;
            var limitYMax = this._bottomLeftRel.Y;

            return (topLeft.Y >= limitYMin && topLeft.Y <= limitYMax) || (bottomLeft.Y >= limitYMin && bottomLeft.Y <= limitYMax);
        }

        /// <summary>
        /// Check relative to current if other grp is local between the right and left limit
        /// </summary>
        public bool IsInVerticalLimit(DataTextBlockGroup grp)
        {
            var topLeft = Vector2.Transform(grp._topLeftWorld - this._topLeftOriginPointVect!.Value, this._toRelativeMatrix);
            var topRight = Vector2.Transform(grp._topRightWorld - this._topLeftOriginPointVect!.Value, this._toRelativeMatrix);

            var limitXMin = this._topLeftRel.X;
            var limitXMax = this._topRightRel.X;

            return (topLeft.X >= limitXMin && topLeft.X <= limitXMax) || (topRight.X >= limitXMin && topRight.X <= limitXMax) ||
                   (limitXMin >= topLeft.X && limitXMin <= topRight.X) || (limitXMax >= topLeft.X && limitXMax <= topRight.X);
        }

        #region Tools

        /// <summary>
        /// Pushes the data.
        /// </summary>
        private void PushDataImpl(DataTextBlock data)
        {
            if (this._blocks.Any() == false)
            {
                this.Magnitude = data.Magnitude;
                this.FontUid = data.FontInfoUid;
                this.LineSize = data.LineSize;
                this._midLineSize = this.LineSize / 2.0f;

                this._referentialBlock = data;
                this._topLeftOriginPoint = data.Area.TopLeft;
                this._topLeftOriginPointVect = new Vector2(this._topLeftOriginPoint.Value.X, this._topLeftOriginPoint.Value.Y);

                this.TopLine = data.Area.TopLine;
                this.LeftLine = data.Area.LeftLine;

                this._topLineUnit = Vector2.Normalize(this.TopLine);
                this._leftLineUnit = Vector2.Normalize(this.LeftLine);

                this._toWorldMatrix = new Matrix3x2(this._topLineUnit.Value.X, this._leftLineUnit.Value.X,
                                                   this._topLineUnit.Value.Y, this._leftLineUnit.Value.Y,
                                                   0, 0);

                if (this._toWorldMatrix.IsIdentity)
                    this._toRelativeMatrix = this._toWorldMatrix;
                else if (Matrix3x2.Invert(this._toWorldMatrix, out var resultInvert))
                    this._toRelativeMatrix = resultInvert;
            }

            this._blocks.Add(data);
        }

        /// <summary>
        /// Updates the group information.
        /// </summary>
        private void UpdateGroupInfo()
        {
            if (this._blocks.Count == 0)
                return;

            var relativeBlockPoints = GetBlockOrderByRelativeCoord();

            this.TextBoxIds = this._blocks.SelectMany(b => b.TextBoxIds ?? Array.Empty<float>())
                                          .Where(b => b > -1)
                                          .Distinct()
                                          .OrderBy(b => b)
                                          .ToArray();

            float minX = float.MaxValue;
            float minY = float.MaxValue;

            float maxX = float.MinValue;
            float maxY = float.MinValue;

            foreach (var point in relativeBlockPoints.SelectMany(r => r.RelArea.GetPoints()))
            {
                if (point.X < minX)
                    minX = point.X;

                if (point.X > maxX)
                    maxX = point.X;

                if (point.Y < minY)
                    minY = point.Y;

                if (point.Y > maxY)
                    maxY = point.Y;
            }

            this._topLeftRel = new Vector2(minX, minY);
            this._topRightRel = new Vector2(maxX, minY);
            this._bottomRightRel = new Vector2(maxX, maxY);
            this._bottomLeftRel = new Vector2(minX, maxY);

            var topLeftWorld = Vector2.Transform(new Vector2(minX, minY), this._toWorldMatrix);
            var topRightWorld = Vector2.Transform(new Vector2(maxX, minY), this._toWorldMatrix);
            var bottomRightWorld = Vector2.Transform(new Vector2(maxX, maxY), this._toWorldMatrix);
            var bottomLeftWorld = Vector2.Transform(new Vector2(minX, maxY), this._toWorldMatrix);

            this.TopLine = topRightWorld - topLeftWorld;
            this.LeftLine = bottomLeftWorld - topLeftWorld;

            this.TopLineLength = this.TopLine.Length();
            this.HalfTopLineLength = this.TopLineLength / 2.0f;

            this.LeftLineLength = this.LeftLine.Length();
            this.HalfLeftLineLength = this.LeftLineLength / 2.0f;


            var centerRelCoord = new Vector2(minX + ((maxX - minX) / 2.0f),
                                             minY + ((maxY - minY) / 2.0f));

            this._topLeftWorld = this._topLeftOriginPointVect!.Value + topLeftWorld;
            this._topRightWorld = this._topLeftOriginPointVect!.Value + topRightWorld;
            this._bottomRightWorld = this._topLeftOriginPointVect!.Value + bottomRightWorld;
            this._bottomLeftWorld = this._topLeftOriginPointVect!.Value + bottomLeftWorld;

            this.Center = this._topLeftOriginPointVect!.Value + Vector2.Transform(centerRelCoord, this._toWorldMatrix);
        }

        /// <summary>
        /// Gets block relative position based on new Referential <see cref="_topLeftOriginPoint"/> && <see cref="_topLineUnit"/> && <see cref="_leftLineUnit"/>
        /// </summary>
        private BlockArea GetRelativePosition(DataTextBlock block)
        {
            var area = block.Area;
            return new BlockArea(GetRelativePoint(area.TopLeft),
                                 GetRelativePoint(area.TopRight),
                                 GetRelativePoint(area.BottomRight),
                                 GetRelativePoint(area.BottomLeft));
        }

        /// <summary>
        /// Gets block relative position based on new Referential <see cref="_topLeftOriginPoint"/> && <see cref="_topLineUnit"/> && <see cref="_leftLineUnit"/>
        /// </summary>
        private BlockPoint GetRelativePoint(BlockPoint point)
        {
            var translatePointToZeroBasis = new Vector2(point.X - this._topLeftOriginPoint!.Value.X, point.Y - this._topLeftOriginPoint!.Value.Y);
            var relatieCoord = Vector2.Transform(translatePointToZeroBasis, this._toRelativeMatrix);
            return new BlockPoint(relatieCoord.X, relatieCoord.Y);
        }

        /// <summary>
        /// Get all the block sort by relative coord from top to bottom and left to right
        /// </summary>
        private IReadOnlyCollection<(BlockArea RelArea, DataTextBlock Block)> GetBlockOrderByRelativeCoord()
        {
            return this._blocks.Select(b => (RelArea: GetRelativePosition(b), Block: b))
                               .OrderBy(kv => kv.RelArea.TopLeft.Y / this._midLineSize)
                               .ThenBy(kv => kv.RelArea.TopLeft.X)
                               .ToArray();
        }

        #endregion

        #endregion
    }
}
