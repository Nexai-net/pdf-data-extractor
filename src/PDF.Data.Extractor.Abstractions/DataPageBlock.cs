// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Abstractions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Define a page in the document
    /// </summary>
    /// <seealso cref="DataBlock" />
    public sealed class DataPageBlock : DataBlock
    {
        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="DataPageBlock"/> class.
        /// </summary>
        public DataPageBlock(Guid uid,
                             int number,
                             int rotation,
                             BlockArea area,
                             IEnumerable<DataBlock>? children)
            : base(uid, BlockTypeEnum.Page, area, null, children)
        {
            this.Number = number;
            this.Rotation = rotation;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the number in the document
        /// </summary>
        public int Number { get; }

        /// <summary>
        /// Gets the rotation.
        /// </summary>
        public int Rotation { get; }

        #endregion
    }
}
