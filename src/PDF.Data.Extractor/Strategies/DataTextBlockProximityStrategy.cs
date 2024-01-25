// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Strategies
{
    using PDF.Data.Extractor.Abstractions;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// Merge strategy using compostion through block proximity
    /// </summary>
    /// <seealso cref="DataBlockMergeBaseStrategy{DataTextBlock}" />
    public sealed class DataTextBlockProximityStrategy : DataBlockMergeBaseStrategy<DataTextBlock>
    {
        #region Fields

        private static readonly Queue<DataTextBlockGroup> s_groupPool;
        private static readonly SemaphoreSlim s_locker;

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

        #endregion

        #region Methods

        /// <inheritdoc />
        protected override IReadOnlyCollection<DataBlock> Merge(IEnumerable<DataTextBlock> dataBlocks, CancellationToken token)
        {
            if (dataBlocks == null || dataBlocks.Count() < 2)
                return dataBlocks?.Cast<DataBlock>().ToArray() ?? Array.Empty<DataBlock>();

            var grps = PullGroupItems(dataBlocks.Count());

            try
            {
                int indx = 0;
                foreach (var data in dataBlocks)
                {
                    var grp = grps[indx++];
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

                        if (CanMerge(current, other))
                        {
                            current.Consume(other);
                            grps.RemoveAt(inGrpIndx);
                            inGrpIndx--;

                            ReleaseItems(other);
                        }
                    }
                }

                var blocks = grps.Select(g => g.Compile())
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
            throw new NotImplementedException();
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
