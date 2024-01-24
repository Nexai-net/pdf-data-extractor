// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Viewer.Controls
{
    using iText.Kernel.Pdf;
    using iText.Kernel.Pdf.Xobject;

    using PDF.Data.Extractor.Viewer.Models;
    using PDF.Data.Extractor.Viewer.ViewModels;

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media.Imaging;

    public sealed class PDFPageViewer : Canvas
    {
        #region Fields

        // Using a DependencyProperty as the backing store for Page.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PageProperty = DependencyProperty.Register(nameof(Page),
                                                                                             typeof(PdfPageInfo),
                                                                                             typeof(PDFPageViewer),
                                                                                             new FrameworkPropertyMetadata(null,
                                                                                                                           propertyChangedCallback: OnPagePropertyChanged));

        public static readonly DependencyProperty DocumentProperty = DependencyProperty.Register(nameof(Document),
                                                                                                 typeof(IronPdf.PdfDocument),
                                                                                                 typeof(PDFPageViewer),
                                                                                                 new FrameworkPropertyMetadata(null,
                                                                                                                               propertyChangedCallback: OnDocumentPropertyChanged));
        public static readonly DependencyProperty DataBlocksProperty = DependencyProperty.Register(nameof(DataBlocks),
                                                                                                   typeof(IReadOnlyCollection<DataBlockViewModel>),
                                                                                                   typeof(PDFPageViewer),
                                                                                                   new FrameworkPropertyMetadata(null,
                                                                                                                                 propertyChangedCallback: OnDataBlocksCollectionChanged));
        private readonly List<PDFDataBlockView> _dataBlocksView;

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="PDFPageViewer"/> class.
        /// </summary>
        public PDFPageViewer()
        {
            this._dataBlocksView = new List<PDFDataBlockView>();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the page.
        /// </summary>
        public PdfPageInfo Page
        {
            get { return (PdfPageInfo)GetValue(PageProperty); }
            set { SetValue(PageProperty, value); }
        }

        /// <summary>
        /// Gets or sets the document.
        /// </summary>
        public IronPdf.PdfDocument Document
        {
            get { return (IronPdf.PdfDocument)GetValue(DocumentProperty); }
            set { SetValue(DocumentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the data blocks.
        /// </summary>
        public IReadOnlyCollection<DataBlockViewModel> DataBlocks
        {
            get { return (IReadOnlyCollection<DataBlockViewModel>)GetValue(DataBlocksProperty); }
            set { SetValue(DataBlocksProperty, value); }
        }

        #endregion

        #region Methods

        private static void OnDocumentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var inst = d as PDFPageViewer;

            if (inst is null)
                return;

            inst.UpdatePageDisplay();
        }

        private static void OnPagePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var inst = d as PDFPageViewer;

            if (inst is null)
                return;

            inst.UpdatePageDisplay();
        }

        private static void OnDataBlocksCollectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var inst = d as PDFPageViewer;

            if (inst is null)
                return;

            inst.UpdateDataBlocks();
        }

        /// <summary>
        /// Updates the data blocks.
        /// </summary>
        private void UpdateDataBlocks()
        {
            this._dataBlocksView.Clear();

            if (this.DataBlocks == null || this.DataBlocks.Count <= 0)
                return;

            this._dataBlocksView.AddRange(this.DataBlocks!.Select(d => new PDFDataBlockView(d, this)));

            foreach (var child in this._dataBlocksView)
                this.Children.Add(child);
        }

        /// <summary>
        /// Inserts the page image.
        /// </summary>
        private void UpdatePageDisplay()
        {
            this.Children.Clear();

            if (this.Document == null || this.Page == null)
                return;

            using (var bitmap = this.Document.PageToBitmap(this.Page.PageNumber - 1, this.Page.Width, this.Page.Height))
            using (var memoryStram = new MemoryStream(bitmap.GetBytes()))
            using (var memory = new MemoryStream())
            {
                //bmp.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memoryStram;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                this.Children.Clear();
                this.Children.Add(new Image()
                {
                    Source = bitmapImage
                });

                this.Width = this.Page.Width;
                this.Height = this.Page.Height;
            }
        }
    }

    #endregion
}
