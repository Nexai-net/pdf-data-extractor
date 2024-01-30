// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Viewer.ViewModels
{
    using global::Data.Block.Abstractions;

    using System;

    public sealed class DataBlockViewModel : DataBlockViewBaseModel<DataBlock>, IDataBlockViewModel
    {
        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="DataBlockViewModel"/> class.
        /// </summary>
        public DataBlockViewModel(DataBlock dataBlock)
            : base(dataBlock)
        {
            this.DisplayText = (dataBlock as DataTextBlock)?.Text
                                     ?? (dataBlock as DataImageBlock)?.Name
                                     ?? Guid.NewGuid().ToString();
        }

        #endregion

        #region Properties

        public override string DisplayText { get; }

        #endregion
    }
}
