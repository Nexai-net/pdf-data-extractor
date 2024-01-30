// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Strategies
{
    using global::Data.Block.Abstractions;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// Merge strategy using compostion through block proximity
    /// </summary>
    /// <seealso cref="DataBlockMergeBaseStrategy{DataTextBlock}" />
    public sealed class DataTextBlockProximityStrategy : DataBlockMergeBaseStrategy<IDataTextBlock>
    {
        #region Fields

        private const float DIST_TOLERANCE_PERCENT_HORIZONTAL = 1.02f;
        private const float DIST_TOLERANCE_PERCENT_VERTICAL = 1.08f;

        private static readonly Queue<DataTextBlockGroup> s_groupPool;
        private static readonly SemaphoreSlim s_locker;

        private readonly bool _compareFontInfo;
        private readonly float _verticalDistanceTolerance;
        private readonly float _horizontalDistanceTolerance;
        private readonly Func<DataTextBlockGroup, IDataBlock>? _customCompile;

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes the <see cref="DataTextBlockProximityStrategy"/> class.
        /// </summary>
        static DataTextBlockProximityStrategy()
        {
            s_groupPool = new Queue<DataTextBlockGroup>(Enumerable.Range(0, 200).Select(_ => new DataTextBlockGroup()));
            s_locker = new SemaphoreSlim(1);
        }

        /// <summary>
        /// Instanciate a new instance of the class <see cref="DataTextBlockProximityStrategy"/>
        /// </summary>
        public DataTextBlockProximityStrategy(bool compareFontInfo = true,
                                              Func<DataTextBlockGroup, IDataBlock>? customCompile = null,
                                              float verticalDistanceTolerance = DIST_TOLERANCE_PERCENT_VERTICAL,
                                              float horizontalDistanceTolerance = DIST_TOLERANCE_PERCENT_HORIZONTAL)
        {
            this._compareFontInfo = compareFontInfo;
            this._customCompile = customCompile;
            this._verticalDistanceTolerance = verticalDistanceTolerance;
            this._horizontalDistanceTolerance = horizontalDistanceTolerance;
        }

        #endregion

        #region Methods

        /// <inheritdoc />
        protected override IReadOnlyCollection<IDataBlock> Merge(IEnumerable<IDataTextBlock> dataBlocks, CancellationToken token)
        {
            if (dataBlocks == null || dataBlocks.Count() < 2)
                return dataBlocks?.Cast<DataBlock>().ToArray() ?? Array.Empty<DataBlock>();

            var grps = PullGroupItems(dataBlocks.Count());

            try
            {
                int indx = 0;
                foreach (var data in dataBlocks)
                {
                    var grp = grps[indx];
                    grp.Push(data);
                    indx++;
                }

                for (int grpIndx = 0; grpIndx < grps.Count; grpIndx++)
                {
                    var current = grps[grpIndx];

                    for (int inGrpIndx = 0; inGrpIndx < grps.Count; inGrpIndx++)
                    {
                        if (grpIndx == inGrpIndx)
                            continue;

                        var other = grps[inGrpIndx];

                        if (current.Uid == other.Uid)
                            continue;

                        if (CanMerge(current, other))
                        {
                            current.Consume(other);
                            grps.RemoveAt(inGrpIndx);
                            inGrpIndx--;

                            ReleaseItems(other);
                        }
                    }
                }

                var blocks = grps.Select(g => this._customCompile?.Invoke(g) ?? g.Compile())
                                 .ToArray();
                return blocks;
            }
            finally
            {
                ReleaseItems(grps.ToArray());
            }
        }

        #region Tools

        /// <summary>
        /// Determines whether this instance can merge the specified current.
        /// </summary>
        private bool CanMerge(DataTextBlockGroup current, DataTextBlockGroup other)
        {
            if (current.TextBoxIds is not null && other.TextBoxIds is not null &&
                current.TextBoxIds.SequenceEqual(other.TextBoxIds))
            {
                return true;
            }

            if (this._compareFontInfo && (current.Magnitude != other.Magnitude || current.FontUid != other.FontUid || current.LineSize != other.LineSize))
                return false;

            var centerDiff = other.Center - current.Center;
            var distLength = Math.Abs(centerDiff.Length());

            var angle = BlockCoordHelper.RadianAngle(current.TopLine, other.TopLine);

            if (Math.Abs(angle) > BlockCoordHelper.ALIGN_MAGNITUDE_TOLERANCE)
                return false;

            var horizontalTestAngleRad = BlockCoordHelper.RadianAngle(current.TopLine, centerDiff);
            var verticalTestAngleRad = BlockCoordHelper.RadianAngle(current.LeftLine, centerDiff);

            bool isHorizontalCompare = (horizontalTestAngleRad < verticalTestAngleRad);

            if (isHorizontalCompare)
            {
                if (this._horizontalDistanceTolerance == 0 || !current.IsInHorizontalLimit(other))
                    return false;

                var projectPointOnHorizontalLenght = Math.Cos(horizontalTestAngleRad) * distLength;

                return Math.Abs(projectPointOnHorizontalLenght) < (current.HalfTopLineLength + other.HalfTopLineLength + (current.SpaceWidth * this._horizontalDistanceTolerance));
            }

            if (this._verticalDistanceTolerance == 0 || !current.IsInVerticalLimit(other))
                return false;

            var projectPointOnVerticalLenght = Math.Cos(verticalTestAngleRad) * distLength;
            return Math.Abs(projectPointOnVerticalLenght) < (current.HalfLeftLineLength + other.HalfLeftLineLength + (current.LineSize * this._verticalDistanceTolerance));

        }

        /// <summary>
        /// Pulls the group items.
        /// </summary>
        private IList<DataTextBlockGroup> PullGroupItems(int quantity)
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
        public void ReleaseItems(params DataTextBlockGroup[] dataBlockGroups)
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

        #endregion

        #endregion
    }
}
