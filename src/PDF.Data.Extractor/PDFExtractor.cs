// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor
{
    using iText.Kernel.Pdf;
    using iText.Kernel.Pdf.Canvas.Parser;

    using PDF.Data.Extractor.Abstractions;
    using PDF.Data.Extractor.Services;
    using PDF.Data.Extractor.Strategies;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Extract all information and format them in <see cref="DataBlock"/> structure
    /// </summary>
    public sealed class PDFExtractor : IDisposable
    {
        #region Fields

        private readonly IDataBlockMergeStrategy[] _defaultMergeStrategies;

        private readonly CancellationTokenSource _lifetimeCancellationToken;
        private readonly SemaphoreSlim _simulaniousAnalyzeLocker;
        private long _disposeCounter;

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="PDFExtractor"/> class.
        /// </summary>
        /// <param name="maxSimulaniousParallelAnalyze">Minimum 2</param>
        public PDFExtractor(IEnumerable<IDataBlockMergeStrategy>? dataBlockMergeStrategies = null,
                            IFontManager? fontManager = null,
                            IImageManager? imageManager = null,
                            uint maxSimulaniousParallelAnalyze = 42)
        {
            this.FontManager = fontManager ?? new DefaultFontManager();
            this.ImageManager = imageManager ?? new DefaultImageManager();

            this._defaultMergeStrategies = dataBlockMergeStrategies?.ToArray() ?? new IDataBlockMergeStrategy[]
            {
                new DataTextBlockHorizontalSiblingMergeStrategy(this.FontManager),
                new DataTextBlockVerticalSiblingMergeStrategy(this.FontManager, alignRight: false),
                new DataTextBlockVerticalSiblingMergeStrategy(this.FontManager, alignRight: true)
                            // Align by center
            };

            this._lifetimeCancellationToken = new CancellationTokenSource();
            this._simulaniousAnalyzeLocker = new SemaphoreSlim(Math.Max((int)maxSimulaniousParallelAnalyze, 2));
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="PDFExtractor"/> class.
        /// </summary>
        ~PDFExtractor()
        {
            Dispose(true);
        }

        #endregion

        #region Property$

        /// <summary>
        /// Gets the font manager.
        /// </summary>
        public IFontManager FontManager { get; }

        /// <summary>
        /// Gets the image manager.
        /// </summary>
        public IImageManager ImageManager { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Analyses a pdf file and extract all information in format <see cref="DataBlock"/>
        /// </summary>
        public async ValueTask<DataDocumentBlock> AnalyseAsync(string filename,
                                                               CancellationToken token = default,
                                                               PDFExtractorOptions? options = null)
        {
            using (var fileStream = File.OpenRead(filename))
            {
                return await AnalyseAsync(fileStream, Path.GetFileNameWithoutExtension(filename), token, options);
            }
        }

        /// <summary>
        /// Analyses stream from a pdf file and extract all information in format <see cref="DataBlock"/>
        /// </summary>
        public async ValueTask<DataDocumentBlock> AnalyseAsync(Stream stream,
                                                       string documentName,
                                                       CancellationToken token = default,
                                                       PDFExtractorOptions? options = null)
        {
            using (var reader = new PdfReader(stream))
            using (var doc = new PdfDocument(reader))
            {
                return await AnalyseAsync(doc, documentName, token, options);
            }
        }

        /// <summary>
        /// Analyses stream from a pdf file and extract all information in format <see cref="DataBlock"/>
        /// </summary>
        public async ValueTask<DataDocumentBlock> AnalyseAsync(PdfDocument doc,
                                                               string documentName,
                                                               CancellationToken token = default,
                                                               PDFExtractorOptions? options = null)
        {
            var asyncExtraction = options?.Asynchronous ?? true;
            var mergeStrategies = options?.OverrideStrategies?.ToArray() ?? this._defaultMergeStrategies;

            using (var grpCancelToken = CancellationTokenSource.CreateLinkedTokenSource(this._lifetimeCancellationToken.Token, token))
            {
                var lastPageNumber = doc.GetNumberOfPages();

                var pageRange = options?.PageRange;

                var rangeAbsStart = pageRange?.Start.GetOffset(lastPageNumber - 1) + 1 ?? 1;
                var rangeAbsEnd = pageRange?.End.GetOffset(lastPageNumber - 1) + 1 ?? lastPageNumber;

                token.ThrowIfCancellationRequested();

                var pageBlocks = new DataPageBlock[rangeAbsEnd - rangeAbsStart];

                if (asyncExtraction)
                {
                    var allPages = Enumerable.Range(rangeAbsStart, rangeAbsEnd - rangeAbsStart + 1)
                                             .Where(pageNumber => pageNumber <= lastPageNumber)
                                             .Select(pageNumber =>
                                             {
                                                 var page = doc.GetPage(pageNumber);
                                                 return Task.Run(() => AnalysePageAsync(pageNumber,
                                                                                        page,
                                                                                        mergeStrategies,
                                                                                        grpCancelToken.Token));
                                             })
                                             .ToArray();

                    await Task.WhenAll(allPages);
                }
                else
                {
                    for (int pageNumber = rangeAbsStart; pageNumber < rangeAbsEnd; pageNumber++)
                    {
                        var page = doc.GetPage(pageNumber);

                        token.ThrowIfCancellationRequested();
                        var pageDataBlock = await AnalysePageAsync(pageNumber,
                                                                   page,
                                                                   mergeStrategies,
                                                                   grpCancelToken.Token);

                        pageBlocks[pageNumber - rangeAbsStart] = pageDataBlock;
                    }
                }

                var docInfo = doc.GetDocumentInfo();

                var defaultPageSize = doc.GetDefaultPageSize();

                var docBlock = new DataDocumentBlock(Guid.NewGuid(),
                                                     documentName,
                                                     new BlockArea(new BlockPoint(defaultPageSize.GetLeft(), defaultPageSize.GetTop()),
                                                                   new BlockPoint(defaultPageSize.GetRight(), defaultPageSize.GetTop()),
                                                                   new BlockPoint(defaultPageSize.GetRight(), defaultPageSize.GetBottom()),
                                                                   new BlockPoint(defaultPageSize.GetLeft(), defaultPageSize.GetBottom())),
                                                     pageBlocks,
                                                     doc.GetPdfVersion().ToString(),
                                                     docInfo.GetAuthor(),
                                                     docInfo.GetKeywords(),
                                                     docInfo.GetProducer(),
                                                     docInfo.GetSubject(),
                                                     docInfo.GetTitle(),
                                                     this.FontManager.GetAll(),
                                                     (options?.InjectImageMetaData ?? true) ? this.ImageManager.GetAll() : null);

                return docBlock;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(false);
        }

        #region Tools

        /// <summary>
        /// AnalyseAsync and extract information about one page.
        /// </summary>
        private async Task<DataPageBlock> AnalysePageAsync(int number,
                                                           PdfPage page,
                                                           IDataBlockMergeStrategy[] mergeStrategies,
                                                           CancellationToken token)
        {
            await this._simulaniousAnalyzeLocker.WaitAsync(token);

            try
            {
                var strategy = new DataBlockExtractStrategy(this.FontManager,
                                                            this.ImageManager,
                                                            token,
                                                            page);

                var processor = new PdfCanvasProcessor(strategy);
                processor.ProcessPageContent(page);

                var pageBlock = strategy.Compile(number,
                                                 token,
                                                 mergeStrategies);
                return pageBlock;
            }
            finally
            {
                this._simulaniousAnalyzeLocker.Release();
            }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        private void Dispose(bool fromFinalizer)
        {
            if (Interlocked.Increment(ref this._disposeCounter) > 1)
                return;

            this._lifetimeCancellationToken.Cancel();

            if (!fromFinalizer)
            {
                if (this.FontManager is IDisposable disposableFontManager)
                    disposableFontManager.Dispose();

                if (this.FontManager is IDisposable disposableFontMetaData)
                    disposableFontMetaData.Dispose();
            }
        }

        #endregion

        #endregion
    }
}
