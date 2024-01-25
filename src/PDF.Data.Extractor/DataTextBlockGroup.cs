// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor
{
    using PDF.Data.Extractor.Abstractions;

    using System;
    using System.Numerics;

    /// <summary>
    /// Used to simulate DataBlock regroup result
    /// </summary>
    public sealed class DataTextBlockGroup
    {
        #region Properties

        private readonly List<DataTextBlock> _blocks;

        private long _using;

        private BlockPoint? _topLeftOriginPoint;
        private Vector2? _topLineUnit;
        private Vector2? _leftLineUnit;

        private DataTextBlock? _referentialBlock;

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTextBlockGroup"/> class.
        /// </summary>
        public DataTextBlockGroup()
        {
            this._blocks = new List<DataTextBlock>();
        }

        #endregion

        #region Nested

        //private sealed class BlockInfo
        //{
        //    #region Fields

        //    private readonly DataTextBlock _block;

        //    #endregion

        //    #region Ctor

        //    /// <summary>
        //    /// Initializes a new instance of the <see cref="BlockInfo"/> class.
        //    /// </summary>
        //    public BlockInfo(DataTextBlock block)
        //    {
        //        this._block = block;
        //    }

        //    #endregion

        //    #region Properties

        //    /// <summary>
        //    /// Gets or sets the relative point.
        //    /// </summary>
        //    public BlockPoint RelativeCoord { get; private set; }

        //    #endregion

        //    #region Methods

        //    /// <summary>
        //    /// Updates the relative point to order the data.
        //    /// </summary>
        //    public void UpdateRelativePoint(BlockPoint referentialPoint)
        //    {
        //        this.RelativeCoord = new BlockPoint(referentialPoint.X - this._block.Area.TopLeft.X, referentialPoint.Y - this._block.Area.TopLeft.Y);
        //    }

        //    #endregion
        //}

        #endregion

        #region Properties

        /// <summary>
        /// Gets the top left.
        /// </summary>
        public BlockPoint TopLeft { get; private set; }

        /// <summary>
        /// Gets the top right.
        /// </summary>
        public BlockPoint TopRight { get; private set; }

        /// <summary>
        /// Gets the bottom right.
        /// </summary>
        public BlockPoint BottomRight { get; private set; }

        /// <summary>
        /// Gets the bottom left.
        /// </summary>
        public BlockPoint BottomLeft { get; private set; }

        /// <summary>
        /// Gets the top line.
        /// </summary>
        public Vector2 TopLine { get; private set; }

        /// <summary>
        /// Gets the left line.
        /// </summary>
        public Vector2 LeftLine { get; private set; }

        /// <summary>
        /// Gets the center.
        /// </summary>
        public BlockPoint Center { get; private set; }

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
            foreach (var b in other._blocks)
                PushDataImpl(b);

            UpdateGroupInfo();
        }

        /// <summary>
        /// Compiles this instance into <see cref="DataTextBlock"/>
        /// </summary>
        public DataTextBlock Compile()
        {
            throw new NotImplementedException();
        }

        #region Tools

        /// <summary>
        /// Pushes the data.
        /// </summary>
        private void PushDataImpl(DataTextBlock data)
        {
            if (this._blocks.Any() == false)
            {
                this._referentialBlock = data;
                this._topLeftOriginPoint = data.Area.TopLeft;
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

            if (this._blocks.Count == 1)
            {
                var item = this._blocks.First();
                var area = item.Area;

                this.TopLeft = area.TopLeft;
                this.TopLine = area.TopLine;
                this.LeftLine = area.LeftLine;
                this.TopRight = area.TopRight;
                this.BottomLeft = area.BottomLeft;
                this.BottomRight = area.BottomRight;
            }
            else
            {
                this._blocks.Select(b => (RelPosition: GetRelativePosition(b), Block: b))
                            .OrderBy(kv => kv.RelPosition.Y)
                            .ThenBy(kv => kv.RelPosition.X)

            }


            this._topLineUnit = Vector2.Normalize(this.TopLine);
            this._leftLineUnit = Vector2.Normalize(this.LeftLine);

            this.Center = new BlockPoint(this.TopLeft.X - ((this.TopRight.X - this.TopLeft.X) / 2.0f),
                                         this.TopLeft.Y - ((this.BottomLeft.Y - this.TopLeft.Y) / 2.0f));
        }

        /// <summary>
        /// Gets block relative position based on new Referential <see cref="_topLeftOriginPoint"/> && <see cref="_topLineUnit"/> && <see cref="_leftLineUnit"/>
        /// </summary>
        private BlockPoint GetRelativePosition(DataTextBlock b)
        {
            return new BlockPoint(this._topLeftOriginPoint!.Value.X - b.Area.X, this._topLeftOriginPoint!.Value.Y - b.Area.Y);
        }

        #endregion

        #endregion
    }
}
