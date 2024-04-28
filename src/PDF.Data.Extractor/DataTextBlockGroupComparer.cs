// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Used to simulate DataBlock regroup result
    /// </summary>
    public sealed class DataTextBlockGroupComparer : IEqualityComparer<DataTextBlockGroup>
    {
        #region Ctor

        static DataTextBlockGroupComparer()
        {
            Instance = new DataTextBlockGroupComparer();
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="DataTextBlockGroupComparer"/> class from being created.
        /// </summary>
        private DataTextBlockGroupComparer()
        {

        }

        #endregion

        #region Properties

        public static DataTextBlockGroupComparer Instance { get; }

        #endregion

        #region Methods

        public bool Equals(DataTextBlockGroup? x, DataTextBlockGroup? y)
        {
            if (x is null && y is null)
                return true;

            if (x is not null && y is not null)
            {
                return (x.GetOrdererChildren()?.Select(d => d.Uid) ?? Array.Empty<Guid>())
                                              .SequenceEqual(y?.GetOrdererChildren()?.Select(o => o.Uid) ?? Array.Empty<Guid>());
            }

            return false;
        }

        public int GetHashCode([DisallowNull] DataTextBlockGroup obj)
        {
            return obj.GetOrdererChildren()?
                      .Aggregate(0, (acc, c) => acc ^ c.Uid.GetHashCode()) ?? 0;
        }

        #endregion
    }
}
