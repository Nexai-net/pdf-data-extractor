// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Strategies
{
    using global::Data.Block.Abstractions;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// Merge strategy using compostion through block proximity
    /// </summary>
    /// <seealso cref="DataBlockMergeBaseStrategy{DataTextBlock}" />
    public abstract class DataTextBlockGroupBaseStrategy : DataBlockMergeBaseStrategy<IDataTextBlock>
    {
        #region Fields

        //private static readonly Queue<DataTextBlockGroup> s_groupPool;
        //private static readonly SemaphoreSlim s_locker;

        private readonly Func<DataTextBlockGroup, IDataBlock>? _customCompile;

        #endregion

        #region Ctor

        ///// <summary>
        ///// Initializes the <see cref="DataTextBlockProximityStrategy"/> class.
        ///// </summary>
        //static DataTextBlockGroupBaseStrategy()
        //{
        //    s_groupPool = new Queue<DataTextBlockGroup>(Enumerable.Range(0, 2000).Select(_ => new DataTextBlockGroup()));
        //    s_locker = new SemaphoreSlim(1);
        //}

        /// <summary>
        /// Instanciate a new instance of the class <see cref="DataTextBlockProximityStrategy"/>
        /// </summary>
        public DataTextBlockGroupBaseStrategy(Func<DataTextBlockGroup, IDataBlock>? customCompile = null)
        {
            this._customCompile = customCompile;
        }

        #endregion

        #region Methods

        /// <inheritdoc />
        protected sealed override IReadOnlyCollection<IDataBlock> Merge(IEnumerable<IDataTextBlock> dataBlocks, CancellationToken token)
        {
            if (dataBlocks == null || dataBlocks.Count() < 2)
                return dataBlocks?.Cast<DataBlock>().ToArray() ?? Array.Empty<DataBlock>();

            var grps = DataTextBlockGroup.PullGroupItems(dataBlocks.Count());

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

                            DataTextBlockGroup.ReleaseItems(other);
                        }
                    }
                }

                var blocks = grps.Select(g => this._customCompile?.Invoke(g) ?? g.Compile())
                                 .ToArray();
                return blocks;
            }
            finally
            {
                DataTextBlockGroup.ReleaseItems(grps.ToArray());
            }
        }

        #region Tools

        /// <summary>
        /// Determines whether this instance can merge the specified current.
        /// </summary>
        protected abstract bool CanMerge(DataTextBlockGroup current, DataTextBlockGroup other);

        #endregion

        #endregion
    }
}
