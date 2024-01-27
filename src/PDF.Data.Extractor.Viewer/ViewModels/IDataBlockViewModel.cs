// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Viewer.ViewModels
{
    using PDF.Data.Extractor.Abstractions;

    using System.ComponentModel;

    public interface IDataBlockViewModel : INotifyPropertyChanged
    {
        #region Properties

        /// <summary>
        /// Gets the display text.
        /// </summary>
        string DisplayText { get; }

        /// <summary>
        /// Gets the area.
        /// </summary>
        BlockArea Area { get; }

        /// <summary>
        /// Gets the type.
        /// </summary>
        BlockTypeEnum Type { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is selected.
        /// </summary>
        bool IsSelected { get; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="IDataBlockViewModel"/> is visibible.
        /// </summary>
        public bool IsVisible { get; set; }  

        #endregion
    }
}
