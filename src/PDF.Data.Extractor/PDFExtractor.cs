// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor
{
    using iText.Kernel.Pdf;
    using iText.Kernel.Pdf.Canvas.Parser;

    using global::Data.Block.Abstractions;
    using PDF.Data.Extractor.Services;
    using PDF.Data.Extractor.Strategies;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Extract all information and format them in <see cref="DataBlock"/> structure
    /// </summary>
    public sealed class PDFExtractor : IDisposable
    {
        #region Fields

        private readonly ILoggerFactory _loggerFactory;

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
        public PDFExtractor(ILoggerFactory loggerFactory,
                            IEnumerable<IDataBlockMergeStrategy>? dataBlockMergeStrategies = null,
                            IFontManager? fontManager = null,
                            IImageManager? imageManager = null,
                            uint maxSimulaniousParallelAnalyze = 42)
        {
            this._loggerFactory = loggerFactory;
            this.FontManager = fontManager ?? new DefaultFontManager();
            this.ImageManager = imageManager ?? new DefaultImageManager();

            this._defaultMergeStrategies = dataBlockMergeStrategies?.ToArray() ?? new IDataBlockMergeStrategy[]
            {
                new DataTextBlockOverlapStrategy()
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

            var logger = this._loggerFactory.CreateLogger(documentName);

            using (var grpCancelToken = CancellationTokenSource.CreateLinkedTokenSource(this._lifetimeCancellationToken.Token, token))
            {
                var lastPageNumber = doc.GetNumberOfPages();

                var pageRange = options?.PageRange;

                var rangeAbsStart = pageRange?.Start.GetOffset(lastPageNumber - 1) + 1 ?? 1;
                var rangeAbsEnd = pageRange?.End.GetOffset(lastPageNumber - 1) + 1 ?? lastPageNumber;

                token.ThrowIfCancellationRequested();

                DataPageBlock[] pageBlocks;

                if (asyncExtraction)
                {
                    var pageCopies = Enumerable.Range(rangeAbsStart, rangeAbsEnd - rangeAbsStart + 1)
                                               .Where(pageNumber => pageNumber <= lastPageNumber)
                                               .Select(pageNumber =>
                                               {
                                                   byte[] copyBytes;

                                                   using (var memoryStream = new MemoryStream())
                                                   using (var writer = new PdfWriter(memoryStream))
                                                   using (var newDoc = new PdfDocument(writer))
                                                   {
                                                       var newPages = doc.CopyPagesTo(pageNumber, pageNumber, newDoc);

                                                       foreach (var newPage in newPages)
                                                           newPage.Flush();

                                                       newDoc.FlushCopiedObjects(doc);

                                                       newDoc.Close();
                                                       writer.Close();

                                                       copyBytes = memoryStream.ToArray();
                                                   }

                                                   var copyMemoryStream = new MemoryStream(copyBytes);
                                                   var reader = new PdfReader(copyMemoryStream);
                                                   var newDocument = new PdfDocument(reader);

                                                   return (PageNumber: pageNumber, Doc: newDocument, Stream: copyMemoryStream, Writer: reader, Page: newDocument.GetPage(1));
                                               })
                                               .ToArray();

                    var allPages = pageCopies.Select(pageInfo =>
                                              {
                                                  return Task.Run(async () =>
                                                  {
                                                      using (pageInfo.Stream)
                                                      using (pageInfo.Writer)
                                                      using (pageInfo.Doc)
                                                      {
                                                          var pageResult = await AnalysePageAsync(pageInfo.PageNumber,
                                                                                                  pageInfo.Page,
                                                                                                  options?.SkipExtractImages ?? false,
                                                                                                  logger,
                                                                                                  mergeStrategies,
                                                                                                  grpCancelToken.Token);

                                                          return (pageResult, pageInfo.PageNumber);
                                                      }
                                                  });
                                              })
                                              .ToArray();

                    await Task.WhenAll(allPages);

                    pageBlocks = allPages.Select(p => p.Result)
                                         .OrderBy(o => o.PageNumber)
                                         .Select(kv => kv.pageResult)
                                         .ToArray();
                }
                else
                {
                    pageBlocks = new DataPageBlock[rangeAbsEnd - rangeAbsStart];

                    for (int pageNumber = rangeAbsStart; pageNumber < rangeAbsEnd; pageNumber++)
                    {
                        var page = doc.GetPage(pageNumber);

                        token.ThrowIfCancellationRequested();
                        var pageDataBlock = await AnalysePageAsync(pageNumber,
                                                                   page,
                                                                   options?.SkipExtractImages ?? false,
                                                                   logger,
                                                                   mergeStrategies,
                                                                   grpCancelToken.Token);

                        pageBlocks[pageNumber - rangeAbsStart] = pageDataBlock;
                    }
                }

                var docInfo = doc.GetDocumentInfo();

                var defaultPageSize = doc.GetDefaultPageSize();

                var docBlock = new DataDocumentBlock(Guid.NewGuid(),
                                                     documentName,
                                                     new BlockArea(new BlockPoint(defaultPageSize.GetLeft(), defaultPageSize.GetBottom()), // Y invert
                                                                   new BlockPoint(defaultPageSize.GetRight(), defaultPageSize.GetBottom()),
                                                                   new BlockPoint(defaultPageSize.GetRight(), defaultPageSize.GetTop()),
                                                                   new BlockPoint(defaultPageSize.GetLeft(), defaultPageSize.GetTop())),
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
                                                           bool skipImage,
                                                           ILogger logger,
                                                           IDataBlockMergeStrategy[] mergeStrategies,
                                                           CancellationToken token,
                                                           IReadOnlyCollection<IDataBlockMergeStrategy>? relationStrategies = null)
        {
            await this._simulaniousAnalyzeLocker.WaitAsync(token);

            try
            {
                var strategy = new DataBlockExtractStrategy(this.FontManager,
                                                            this.ImageManager,
                                                            token,
                                                            logger,
                                                            page,
                                                            skipImage);

                var processor = new PdfCanvasProcessor(strategy);
                processor.ProcessPageContent(page);

                var pageBlock = await strategy.Compile(number,
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
