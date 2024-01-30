// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Viewer.Controls
{
    using PDF.Data.Extractor.Viewer.ViewModels;

    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="FrameworkElement" />
    public sealed class PDFDataBlockView : Control
    {
        #region Fields

        private static readonly DependencyPropertyKey s_pointsPropertyKey = DependencyProperty.RegisterReadOnly(nameof(Points),
                                                                                                              typeof(PointCollection),
                                                                                                              typeof(PDFDataBlockView),
                                                                                                              new PropertyMetadata(null));

        public static readonly DependencyProperty PointsProperty = s_pointsPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey s_cornerTopLeftPropertyKey = DependencyProperty.RegisterReadOnly(nameof(CornerTopLeft),
                                                                                                                       typeof(Point?),
                                                                                                                       typeof(PDFDataBlockView),
                                                                                                                       new PropertyMetadata(null));

        public static readonly DependencyProperty CornerTopLeftProperty = s_cornerTopLeftPropertyKey.DependencyProperty;

        private readonly IDataBlockViewModel _dataBlock;

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes the <see cref="PDFDataBlockView"/> class.
        /// </summary>
        static PDFDataBlockView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PDFDataBlockView),
                                                     new FrameworkPropertyMetadata(typeof(PDFDataBlockView)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PDFDataBlockView"/> class.
        /// </summary>
        public PDFDataBlockView(IDataBlockViewModel dataBlock)
        {
            this._dataBlock = dataBlock;
            this.DataContext = dataBlock;

            var area = dataBlock.Area;
            var topleft = new Point(area.TopLeft.X, area.TopLeft.Y);
            var topRight = new Point(area.TopRight.X, area.TopRight.Y);
            var bottomRight = new Point(area.BottomRight.X, area.BottomRight.Y);
            var bottomLeft = new Point(area.BottomLeft.X, area.BottomLeft.Y);

            var collection = new PointCollection(new Point[]
            {
                topleft, topRight, bottomRight, bottomLeft
            });

            SetValue(s_cornerTopLeftPropertyKey, topleft);
            SetValue(s_pointsPropertyKey, collection);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets shape's points.
        /// </summary>
        public PointCollection Points
        {
            get { return (PointCollection)GetValue(PointsProperty); }
        }

        /// <summary>
        /// Gets the top left corner
        /// </summary>
        public Point? CornerTopLeft
        {
            get { return (Point?)GetValue(CornerTopLeftProperty); }
        }

        #endregion
    }
}
