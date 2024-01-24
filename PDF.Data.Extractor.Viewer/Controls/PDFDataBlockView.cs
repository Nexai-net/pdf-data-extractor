// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Viewer.Controls
{
    using PDF.Data.Extractor.Viewer.ViewModels;

    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Shapes;

    public sealed class PDFDataBlockView
    {
        #region Fields

        private readonly DataBlockViewModel _dataBlock;

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="PDFDataBlockView"/> class.
        /// </summary>
        public PDFDataBlockView(DataBlockViewModel dataBlock)
        {
            this._dataBlock = dataBlock;

            this.Shape = new Polygon();

            Canvas.SetLeft(this.Shape, dataBlock.Area.TopLeft.X);
            Canvas.SetTop(this.Shape, dataBlock.Area.TopLeft.Y);

            var area = dataBlock.Area;

            this.Shape.ToolTip = dataBlock.DisplayText;

            this.Shape.Fill = Brushes.Transparent;

            this.Shape.Stroke = Brushes.Blue;
            this.Shape.StrokeThickness = 2;

            this.Shape.Points.Add(new Point(area.TopLeft.X, area.TopLeft.Y));
            this.Shape.Points.Add(new Point(area.TopRight.X, area.TopRight.Y));
            this.Shape.Points.Add(new Point(area.BottomRight.X, area.BottomRight.Y));
            this.Shape.Points.Add(new Point(area.BottomLeft.X, area.BottomLeft.Y));

            dataBlock.PropertyChanged -= DataBlock_PropertyChanged;
            dataBlock.PropertyChanged += DataBlock_PropertyChanged;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the shape.
        /// </summary>
        public Polygon Shape { get; }

        #endregion

        #region Methods

        private void DataBlock_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (this._dataBlock.IsSelected)
            {
                this.Shape.Fill = new SolidColorBrush(Color.FromArgb(80, 50, 25, 23));
            }
            else
            {
                this.Shape.Fill = Brushes.Transparent;
            }
        }

        #endregion
    }
}
