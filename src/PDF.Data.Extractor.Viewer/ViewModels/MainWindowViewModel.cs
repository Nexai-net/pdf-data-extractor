// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Viewer.ViewModels
{
    using global::Data.Block.Abstractions;

    using iText.Kernel.Pdf;

    using Microsoft.Extensions.Logging;
    using Microsoft.Win32;

    using PDF.Data.Extractor.Viewer.Models;
    using PDF.Data.Extractor.Viewer.Tools;

    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
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
#pragma warning disable IDE0032 // Use auto property
        private IronPdf.PdfDocument? _ironDocument;
#pragma warning restore IDE0032 // Use auto property
        private iText.Kernel.Pdf.PdfDocument? _document;
        private PdfReader? _docReader;
        private int _displayPage;
        private IReadOnlyCollection<IDataBlockViewModel>? _dataBlocks;
        private bool _autoAnalyzeWhenChangePage;
        private IDataBlockViewModel[]? _dataBlocksBasic;
        private IDataBlockViewModel[]? _relation;
        private bool _displayDataBlocks;
        private bool _displayDataBlockRelation;
        private readonly AsyncDelegateCommand _analyzeCommand;
        private readonly AsyncDelegateCommand _pickPdfCommand;

        private readonly DelegateCommand _prevPageCommand;
        private readonly DelegateCommand _nextPageCommand;

        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly ObservableCollection<Log> _logs;

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
        /// </summary>
        public MainWindowViewModel()
        {
            this._pickPdfCommand = new AsyncDelegateCommand(PickPdfAsync, _ => this.IsWorking == false);
            this.PickPdfCommand = this._pickPdfCommand;

            this._analyzeCommand = new AsyncDelegateCommand(AnalyzeAsync, _ => this.IsWorking == false);
            this.AnalyzeCommand = this._analyzeCommand;

            this._nextPageCommand = new DelegateCommand(() => this.DisplayPage = Math.Min(this.MaxPage, this.DisplayPage + 1),
                                                         _ => this.IsWorking == false && this.DisplayPage + 1 <= this.MaxPage);
            this.NextPageCommand = this._nextPageCommand;

            this._prevPageCommand = new DelegateCommand(() => this.DisplayPage = Math.Max(1, this.DisplayPage - 1),
                                                         _ => this.IsWorking == false && this.DisplayPage - 1 > 0);
            this.PrevPageCommand = this._prevPageCommand;

            this._displayDataBlockRelation = true;
            this._displayDataBlocks = true;

            this._loggerFactory = LoggerFactory.Create(b => b.AddProvider(new RelayLoggerProvider(OnLog)));
            this._logger = this._loggerFactory.CreateLogger(string.Empty);

            this._logs = new ObservableCollection<Log>();
            this.Logs = new ReadOnlyCollection<Log>(this._logs);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the logs.
        /// </summary>
        public IReadOnlyCollection<Log> Logs { get; }

        public ICommand PickPdfCommand { get; }

        public ICommand AnalyzeCommand { get; set; }

        public ICommand NextPageCommand { get; set; }

        public ICommand PrevPageCommand { get; set; }

        public string? PdfFilePath
        {
            get { return this._pdfFilePath; }
            private set { SetProperty(ref this._pdfFilePath, value); }
        }

        /// <summary>
        /// Gets the page.
        /// </summary>
        public PdfPageInfo? Page
        {
            get { return this._currentPage; }
            private set { SetProperty(ref this._currentPage, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public IronPdf.PdfDocument? PdfDocument
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

        public IReadOnlyCollection<IDataBlockViewModel>? DataBlocks
        {
            get { return this._dataBlocks; }
            private set { SetProperty(ref this._dataBlocks, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [display data block relation].
        /// </summary>
        public bool DisplayDataBlockRelation
        {
            get { return this._displayDataBlockRelation; }
            set
            {
                if (SetProperty(ref this._displayDataBlockRelation, value) && this._relation is not null)
                {
                    foreach (var relation in this._relation)
                        relation.IsVisible = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [display data blocks].
        /// </summary>
        public bool DisplayDataBlocks
        {
            get { return this._displayDataBlocks; }
            set 
            {
                if (SetProperty(ref this._displayDataBlocks, value) && this._dataBlocksBasic is not null)
                {
                    foreach (var basic in this._dataBlocksBasic)
                        basic.IsVisible = value;
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Called when log arrived log.
        /// </summary>
        private void OnLog(Log log)
        {
            UIDispatchHost.Call(() => this._logs.Add(log));

            Debug.WriteLine(log.LogLevel + ":" + log.message);
        }

        private async ValueTask AnalyzeAsync()
        {
            if (this._document == null || string.IsNullOrEmpty(this._pdfFilePath))
                return;

            Interlocked.Increment(ref this._workingCounter);
            RefreshViewModelState();
            try
            {
                using (var extractor = new PDFExtractor(this._loggerFactory))
                {
                    this._logger.LogInformation("Start Analyzing {document} {page}/{maxPage}",
                                                Path.GetFileNameWithoutExtension(this._pdfFilePath),
                                                this.DisplayPage,
                                                this.MaxPage);

                    var analyzeDoc = await extractor.AnalyseAsync(this._document!,
                                                                  Path.GetFileNameWithoutExtension(this._pdfFilePath)!,
                                                                  options: new PDFExtractorOptions()
                                                                  {
                                                                      //OverrideStrategies = new List<IDataBlockMergeStrategy>(),
                                                                      PageRange = new Range(this.DisplayPage - 1, this.DisplayPage),
                                                                      Asynchronous = false
                                                                  });

                    var page = (analyzeDoc.Children ?? Array.Empty<DataBlock>()).OfType<DataPageBlock>().FirstOrDefault();

                    if (page is null)
                    {
                        this.DataBlocks = null;
                        return;
                    }

                    this._dataBlocksBasic = (page.Children ?? Array.Empty<DataBlock>())
                                                  .Select(b => new DataBlockViewModel(b)
                                                  {
                                                      IsVisible = this.DisplayDataBlocks
                                                  })
                                                  .ToArray<IDataBlockViewModel>();

                    this._relation = (page.Relations ?? Array.Empty<DataRelationBlock>())
                                        .Select(b => new DataBlockRelationViewModel(b)
                                        {
                                            IsVisible = this.DisplayDataBlockRelation
                                        })
                                        .ToArray<IDataBlockViewModel>();

                    this.DataBlocks = _dataBlocksBasic.Concat(this._relation).ToArray();
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex,
                                      "Error Analyzing {document} {page}/{maxPage}\n{exception}",
                                      this._document.GetDocumentInfo().GetTitle(),
                                      this.DisplayPage,
                                      this.MaxPage,
                                      ex);
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
        private async ValueTask PickPdfAsync()
        {
            Interlocked.Increment(ref this._workingCounter);
            RefreshViewModelState();
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    CheckFileExists = true,
                    Filter = "PDF file (*.pdf) | *.pdf",
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    this._docReader?.Close();
                    this._document?.Close();

                    this.Page = null;
                    this._currentPage = null;

                    this._logger.LogInformation("Openning new file {fileName} ...", Path.GetFileNameWithoutExtension(openFileDialog.FileName));

                    await Task.Run(() =>
                    {
                        this.PdfFilePath = openFileDialog.FileName;
                        this._docReader = new PdfReader(this._pdfFilePath);
                        this._document = new iText.Kernel.Pdf.PdfDocument(this._docReader);
                        this._ironDocument = new IronPdf.PdfDocument(this._pdfFilePath);
                        OnPropertyChanged(nameof(this.PdfDocument));

                        this._displayPage = 1;
                        OnPropertyChanged(nameof(this.MaxPage));
                        OnPropertyChanged(nameof(this.DisplayPage));

                        SelectPage(1);
                    });
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex,
                                      "Error Picking file\n{exception}",
                                      ex);
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
