// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.InternalModels
{
    using global::Data.Block.Abstractions;
    using global::Data.Block.Abstractions.Tags;

    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Relation group that simulate the block grouping in merge algorithme
    /// </summary>
    /// <seealso cref="DataBlock" />
    /// <seealso cref="IDataTextBlock" />
    public sealed class DataTextBlockRelationGroup : DataBlock, IDataTextBlock
    {
        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTextBlockRelationGroup"/> class.
        /// </summary>
        public DataTextBlockRelationGroup(Guid uid,
                                          BlockArea area,
                                          IEnumerable<DataTag>? tags,
                                          IEnumerable<IDataTextBlock>? children)
            : base(uid, BlockTypeEnum.Relation, area, tags, children?.Cast<DataBlock>())
        {
            var first = children?.FirstOrDefault();

            this.Text = string.Empty;

            if (first != null)
            {
                this.LineSize = first.LineSize;
                this.PointValue = first.PointValue;
                this.Scale = first.Scale;
                this.FontInfoUid = first.FontInfoUid;
                this.TextBoxIds = first.TextBoxIds;
                this.Text = first.Text;
                this.Magnitude = first.Magnitude;
                this.SpaceWidth = first.SpaceWidth;
                this.FontLevel = first.FontLevel;
            }
        }

        #endregion

        #region Properties

        public float LineSize { get; }

        public float FontLevel { get; }

        public float PointValue { get; }

        public float Scale { get; }

        public string Text { get; }

        public float Magnitude { get; }

        public Guid FontInfoUid { get; }

        public float SpaceWidth { get; }

        public IReadOnlyCollection<float>? TextBoxIds { get; }

        #endregion
    }
}
