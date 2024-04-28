// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Strategies
{
    using System;
    using System.Linq;

    /// <summary>
    ///  Group by element of the same Text box ID
    /// </summary>
    /// <seealso cref="PDF.Data.Extractor.Strategies.DataTextBlockGroupBaseStrategy" />
    public sealed class DataBlockMergePDFTextBoxStrategy : DataTextBlockGroupBaseStrategy
    {
        protected override bool CanMerge(DataTextBlockGroup current, DataTextBlockGroup other)
        {
            if (current.TextBoxIds is not null && other.TextBoxIds is not null &&
                current.TextBoxIds.SequenceEqual(other.TextBoxIds))
            {
                return true;
            }
            return false;
        }
    }
}
