// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Viewer.ViewModels
{
    using global::Data.Block.Abstractions;

    public abstract class DataBlockViewBaseModel<TDataBlock> : BaseViewModel, IDataBlockViewModel
        where TDataBlock : IDataBlock
    {
        #region Fields

        private bool _isSelected;
        private bool _visible;

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="DataBlockViewBaseModel"/> class.
        /// </summary>
        public DataBlockViewBaseModel(TDataBlock dataBlock)
        {
            this.DataBlock = dataBlock;
            this.Area = dataBlock.Area;
            this.Type = dataBlock.Type;

            this._visible = true;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the data block.
        /// </summary>
        protected TDataBlock DataBlock { get; }

        /// <inheritdoc />
        public abstract string DisplayText { get; }

        /// <inheritdoc />
        public BlockArea Area { get; }

        /// <inheritdoc />
        public bool IsSelected
        {
            get { return this._isSelected; }
            set { SetProperty(ref this._isSelected, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="DataBlockViewBaseModel{TDataBlock}"/> is visibible.
        /// </summary>
        public bool IsVisible
        {
            get { return this._visible; }
            set { SetProperty(ref this._visible, value); }
        }

        /// <summary>
        /// Gets the type.
        /// </summary>
        public BlockTypeEnum Type { get; }

        #endregion
    }
}
