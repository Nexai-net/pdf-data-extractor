// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Viewer.ViewModels
{
    using global::Data.Block.Abstractions;

    public sealed class DataBlockRelationViewModel : DataBlockViewBaseModel<DataRelationBlock>, IDataBlockViewModel
    {
        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="DataBlockViewModel"/> class.
        /// </summary>
        public DataBlockRelationViewModel(DataRelationBlock dataBlock)
            : base(dataBlock)
        {
            this.DisplayText = dataBlock.BlockRelationType.ToString() + ": " + (dataBlock.BlocksContained?.Count ?? 0);
        }

        #endregion

        #region Properties

        /// <inheritdoc />
        public override string DisplayText { get; }

        #endregion
    }
}
