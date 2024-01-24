// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Viewer.ViewModels
{
    using PDF.Data.Extractor.Abstractions;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public sealed class DataBlockViewModel : BaseViewModel
    {
        #region Fields

        private readonly DataBlock _dataBlock;
        private bool _isSelected;

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="DataBlockViewModel"/> class.
        /// </summary>
        public DataBlockViewModel(DataBlock dataBlock)
        {
            this._dataBlock = dataBlock;

            this.DisplayText = (dataBlock as DataTextBlock)?.Text
                                     ?? (dataBlock as DataImageBlock)?.Name
                                     ?? Guid.NewGuid().ToString();

            this.Area = dataBlock.Area;
        }

        #endregion

        #region Properties

        public string DisplayText { get; }

        public BlockArea Area { get; }

        public bool IsSelected
        {
            get { return this._isSelected; }
            set { SetProperty(ref this._isSelected, value); }
        }

        #endregion
    }
}
