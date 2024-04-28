// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor
{
    using global::Data.Block.Abstractions;
    using global::Data.Block.Abstractions.Tags;

    using System;
    using System.Diagnostics;
    using System.Numerics;
    using System.Text;

    /// <summary>
    /// Used to simulate DataBlock regroup result
    /// </summary>
    public sealed class DataTextBlockGroup
    {
        #region Fields

        private static readonly Queue<DataTextBlockGroup> s_groupPool;
        private static readonly SemaphoreSlim s_locker;

        private readonly List<IDataTextBlock> _blocks;
        private IReadOnlyCollection<DataTag>? _tags;

        private IDataTextBlock? _referentialBlock;

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
        /// Initializes the <see cref="DataTextBlockGroup"/> class.
        /// </summary>
        static DataTextBlockGroup()
        {
            s_groupPool = new Queue<DataTextBlockGroup>(Enumerable.Range(0, 2000).Select(_ => new DataTextBlockGroup()));
            s_locker = new SemaphoreSlim(1);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTextBlockGroup"/> class.
        /// </summary>
        public DataTextBlockGroup()
        {
            this._blocks = new List<IDataTextBlock>();
            this._tags = new List<DataTag>();

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
        /// Gets the width of the space.
        /// </summary>
        public float SpaceWidth { get; private set; }

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

        /// <summary>
        /// Gets the block origin.
        /// </summary>
        public BlockPoint? Origin
        {
            get { return this._topLeftOriginPoint; }
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
        public void Push(IDataTextBlock data)
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
        public IDataTextBlock Compile()
        {
            var blocks = GetBlockOrderByRelativeCoord();

            if (blocks.Count == 0 || blocks.Count == 1)
                return blocks.FirstOrDefault().Block;

            var children = new List<DataBlock>(blocks.Count);
            var sb = new StringBuilder();

            float? lastY = null;
            foreach (var block in blocks)
            {
                children.Add((DataBlock)block.Block);
                if (lastY is not null)
                {
                    var yDiff = block.RelArea.TopLeft.Y - lastY;
                    if (yDiff > 0 && yDiff > (this.LineSize / 2.0f))
                        sb.AppendLine();
                }

                lastY = block.RelArea.TopLeft.Y;
                sb.Append(block.Block.Text);
            }

            return new DataTextBlock(Guid.NewGuid(),
                                     this._referentialBlock!.FontLevel,
                                     this._referentialBlock!.PointValue,
                                     this.LineSize,
                                     this._referentialBlock!.Scale,
                                     this.Magnitude,
                                     sb.ToString(),
                                     this._referentialBlock!.FontInfoUid,
                                     this.SpaceWidth,
                                     GetWorldArea(),
                                     (this.TextBoxIds?.Any() ?? false ? this.TextBoxIds.Distinct() : null)?.ToArray(),
                                     (this._tags?.Any() ?? false ? this._tags.Where(t => !string.IsNullOrEmpty(t.Raw)).Distinct() : null)?.ToArray(),
                                     children);
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

        /// <inheritdoc />
        public BlockArea GetWorldArea()
        {
            return new BlockArea(new BlockPoint(this._topLeftWorld.X, this._topLeftWorld.Y),
                                 new BlockPoint(this._topRightWorld.X, this._topRightWorld.Y),
                                 new BlockPoint(this._bottomRightWorld.X, this._bottomRightWorld.Y),
                                 new BlockPoint(this._bottomLeftWorld.X, this._bottomLeftWorld.Y));
        }

        /// <inheritdoc />
        public BlockArea GetLocalArea()
        {
            return new BlockArea(new BlockPoint(this._topLeftRel.X, this._topLeftRel.Y),
                                 new BlockPoint(this._topRightRel.X, this._topRightRel.Y),
                                 new BlockPoint(this._bottomRightRel.X, this._bottomRightRel.Y),
                                 new BlockPoint(this._bottomLeftRel.X, this._bottomLeftRel.Y));
        }

        /// <inheritdoc />
        public IReadOnlyCollection<DataTag>? GetTags()
        {
            return this._tags;
        }

        /// <inheritdoc />
        public IReadOnlyCollection<IDataTextBlock>? GetOrdererChildren()
        {
            return GetBlockOrderByRelativeCoord().Select(kv => kv.Block)?.ToArray();
        }

        /// <summary>
        /// Pulls the group items.
        /// </summary>
        public static IList<DataTextBlockGroup> PullGroupItems(int quantity)
        {
            s_locker.Wait();
            try
            {
                var items = new List<DataTextBlockGroup>();

                for (int i = 0; i < quantity; ++i)
                {
                    DataTextBlockGroup? item;
                    if (s_groupPool.Count > 0)
                    {
                        item = s_groupPool.Dequeue();
                    }
                    else
                    {
                        item = new DataTextBlockGroup();
                    }

                    Debug.Assert(item.IsUsed == false);

                    item.Initialize();
                    items.Add(item);
                }

                return items;
            }
            finally
            {
                s_locker.Release();
            }
        }

        /// <summary>
        /// Releases the items.
        /// </summary>
        public static void ReleaseItems(params DataTextBlockGroup[] dataBlockGroups)
        {
            s_locker.Wait();
            try
            {
                foreach (var grp in dataBlockGroups)
                {
                    grp.Clear();
                    Debug.Assert(grp.IsUsed == false);
                    s_groupPool.Enqueue(grp);
                }
            }
            finally
            {
                s_locker.Release();
            }
        }

        #region Tools

        /// <summary>
        /// Pushes the data.
        /// </summary>
        private void PushDataImpl(IDataTextBlock data)
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

            var textBoxIds = new List<float>();
            var tags = new List<DataTag>();
            var spaceWith = float.MinValue;
            var lineSize = float.MaxValue;

            foreach (var block in this._blocks)
            {
                if (block.TextBoxIds is not null)
                    textBoxIds.AddRange(block.TextBoxIds);

                if (block.Tags is not null)
                    tags.AddRange(block.Tags);

                spaceWith = Math.Max(spaceWith, block.SpaceWidth);

                lineSize = Math.Min(lineSize, block.LineSize);
            }

            this.TextBoxIds = textBoxIds.Where(b => b > -1)
                                        .Distinct()
                                        .OrderBy(b => b)
                                        .ToArray();

            this._tags = tags.Where(b => !string.IsNullOrEmpty(b.Raw))
                             .Distinct()
                             .ToArray();

            this.LineSize = lineSize;
            this.SpaceWidth = spaceWith;

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
        private BlockArea GetRelativePosition(IDataTextBlock block)
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
        private IReadOnlyCollection<(BlockArea RelArea, IDataTextBlock Block)> GetBlockOrderByRelativeCoord()
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
