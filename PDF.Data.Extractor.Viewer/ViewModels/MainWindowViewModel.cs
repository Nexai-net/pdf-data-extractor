// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Viewer.ViewModels
{
    using iText.Kernel.Pdf;
    using iText.Kernel.Pdf.Canvas.Parser;

    using Microsoft.Win32;

    using PDF.Data.Extractor.Services;
    using PDF.Data.Extractor.Viewer.Models;
    using PDF.Data.Extractor.Viewer.Tools;

    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Security.Permissions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Input;

    /// <summary>
    /// View model of the main window
    /// </summary>
    public sealed class MainWindowViewModel : BaseViewModel
    {
        #region Fields

        private string? _pdfFilePath;
        private long _workingCounter;
        private PdfPageInfo? _currentPage;
        private IronPdf.PdfDocument? _ironDocument;
        private iText.Kernel.Pdf.PdfDocument? _document;
        private PdfReader? _docReader;
        private int _displayPage;
        private IReadOnlyCollection<DataBlockViewModel> _dataBlocks;
        private bool _autoAnalyzeWhenChangePage;
        private readonly AsyncDelegateCommand _analyzeCommand;

        private readonly DelegateCommand _pickPdfCommand;
        private readonly DelegateCommand _prevPageCommand;
        private readonly DelegateCommand _nextPageCommand;

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
        /// </summary>
        public MainWindowViewModel()
        {
            this._pickPdfCommand = new DelegateCommand(PickPdf, _ => this.IsWorking == false);
            this.PickPdfCommand = this._pickPdfCommand;

            this._analyzeCommand = new AsyncDelegateCommand(AnalyzeAsync, _ => this.IsWorking == false);
            this.AnalyzeCommand = this._analyzeCommand;

            this._nextPageCommand = new DelegateCommand(() => this.DisplayPage = Math.Min(this.MaxPage, this.DisplayPage + 1),
                                                         _ => this.IsWorking == false && this.DisplayPage + 1 <= this.MaxPage);
            this.NextPageCommand = this._nextPageCommand;

            this._prevPageCommand = new DelegateCommand(() => this.DisplayPage = Math.Max(1, this.DisplayPage - 1),
                                                         _ => this.IsWorking == false && this.DisplayPage - 1 > 0);
            this.PrevPageCommand = this._prevPageCommand;
        }

        #endregion

        #region Properties

        public ICommand PickPdfCommand { get; }

        public ICommand AnalyzeCommand { get; set; }


        public ICommand NextPageCommand { get; set; }


        public ICommand PrevPageCommand { get; set; }

        public string PdfFilePath
        {
            get { return this._pdfFilePath; }
            private set { SetProperty(ref this._pdfFilePath, value); }
        }

        /// <summary>
        /// Gets the page.
        /// </summary>
        public PdfPageInfo Page
        {
            get { return this._currentPage; }
            private set { SetProperty(ref this._currentPage, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public IronPdf.PdfDocument PdfDocument
        {
            get { return this._ironDocument; }
        }

        public bool AutoAnalyzeWhenChangePage
        {
            get { return this._autoAnalyzeWhenChangePage; }
            set { SetProperty(ref this._autoAnalyzeWhenChangePage, value); }
        }

        public bool IsWorking
        {
            get { return Interlocked.Read(ref this._workingCounter) > 0; }
        }

        public int MaxPage
        {
            get { return this._document?.GetNumberOfPages() ?? 0; }
        }

        public int DisplayPage
        {
            get { return this._displayPage; }
            private set
            {
                if (SetProperty(ref this._displayPage, value))
                    SelectPage(value);
            }
        }

        public IReadOnlyCollection<DataBlockViewModel> DataBlocks
        {
            get { return this._dataBlocks; }
            private set { SetProperty(ref this._dataBlocks, value); }
        }

        #endregion

        #region Methods

        private async ValueTask AnalyzeAsync()
        {
            if (this._document == null || string.IsNullOrEmpty(this._pdfFilePath))
                return;

            Interlocked.Increment(ref this._workingCounter);
            RefreshViewModelState();
            try
            {
                using (var extractor = new PDFExtractor())
                {
                    var analyzeDoc = await extractor.AnalyseAsync(this._document!,
                                                                  Path.GetFileNameWithoutExtension(this._pdfFilePath)!,
                                                                  options: new PDFExtractorOptions()
                                                                  {
                                                                      //OverrideStrategies = new List<IDataBlockMergeStrategy>(),
                                                                      PageRange = new Range(this.DisplayPage - 1, this.DisplayPage)
                                                                  });

                    this.DataBlocks = analyzeDoc.Children
                                                .FirstOrDefault()
                                                ?.Children
                                                 .Select(b => new DataBlockViewModel(b))
                                                 .ToArray() ?? Array.Empty<DataBlockViewModel>();
                }
            }
            finally
            {
                Interlocked.Decrement(ref this._workingCounter);
                RefreshViewModelState();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void PickPdf()
        {
            Interlocked.Increment(ref this._workingCounter);
            RefreshViewModelState();
            try
            {
                var openFileDialog = new OpenFileDialog();
                openFileDialog.CheckFileExists = true;
                openFileDialog.Filter = "PDF file (*.pdf) | *.pdf";
                openFileDialog.Multiselect = false;

                if (openFileDialog.ShowDialog() == true)
                {
                    this._docReader?.Close();
                    this._document?.Close();

                    this.PdfFilePath = openFileDialog.FileName;
                    this._docReader = new PdfReader(this._pdfFilePath);
                    this._document = new iText.Kernel.Pdf.PdfDocument(this._docReader);
                    this._ironDocument = new IronPdf.PdfDocument(this._pdfFilePath);
                    OnPropertyChanged(nameof(this.PdfDocument));

                    this._displayPage = 1;
                    OnPropertyChanged(nameof(this.MaxPage));
                    OnPropertyChanged(nameof(this.DisplayPage));

                    SelectPage(1);
                }
            }
            finally
            {
                Interlocked.Decrement(ref this._workingCounter);
                RefreshViewModelState();
            }
        }

        private void SelectPage(int pageNumber)
        {
            if (pageNumber < 1 && pageNumber > this.MaxPage)
                return;

            var itextPage = this._document!.GetPage(pageNumber);
            if (itextPage == null)
                return;

            var pageSize = itextPage.GetPageSize();
            this.Page = new PdfPageInfo(pageNumber, (int)pageSize.GetWidth(), (int)pageSize.GetHeight());
            RefreshViewModelState();
            this.DataBlocks = Array.Empty<DataBlockViewModel>();

            if (this.AutoAnalyzeWhenChangePage)
                this.AnalyzeCommand.Execute(null);
        }

        /// <summary>
        /// Refreshes the state of the view model.
        /// </summary>
        private void RefreshViewModelState()
        {
            UIDispatchHost.Call(() =>
            {
                OnPropertyChanged(nameof(this.IsWorking));
                this._pickPdfCommand.RaiseCanExecuteChanged();
                this._analyzeCommand.RaiseCanExecuteChanged();
                this._nextPageCommand.RaiseCanExecuteChanged();
                this._prevPageCommand.RaiseCanExecuteChanged();
            });
        }

        #endregion
    }
}
