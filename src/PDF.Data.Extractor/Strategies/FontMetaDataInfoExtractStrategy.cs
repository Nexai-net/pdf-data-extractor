// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Strategies
{
    using iText.IO.Font;
    using iText.Kernel.Font;
    using iText.Kernel.Pdf;

    using PDF.Data.Extractor.Abstractions.MetaData;
    using PDF.Data.Extractor.Services;

    using System.Diagnostics;

    /// <summary>
    /// Default font handler, strategy and manager
    /// </summary>
    /// <seealso cref="IFontMetaDataInfoExtractStrategy" />
    public sealed class FontMetaDataInfoExtractStrategy : IDisposable, IFontMetaDataInfoExtractStrategy, IFontManager
    {
        #region Fields

        private const string TEXT_SIZE = "pgqtmMT-+_G?0i";
        private const string TEXT_SIZE_WITH_NEWLINE = "pgqtmMT-\n+_G?0i";
        private static readonly IReadOnlyCollection<string> s_testTextFont;

        private readonly Dictionary<string, Dictionary<float, TextFontMetaData>> _cache;
        private readonly Dictionary<Guid, TextFontMetaData> _cacheById;
        private readonly ReaderWriterLockSlim _locker;

        private long _disposeCounter;

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes the <see cref="FontMetaDataInfoExtractStrategy"/> class.
        /// </summary>
        static FontMetaDataInfoExtractStrategy()
        {
            s_testTextFont = new[] { "A", "G", "H", "i", "0", "6", "Q", "P", "p" };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FontMetaDataInfoExtractStrategy"/> class.
        /// </summary>
        public FontMetaDataInfoExtractStrategy()
        {
            this._locker = new ReaderWriterLockSlim();
            this._cache = new Dictionary<string, Dictionary<float, TextFontMetaData>>(StringComparer.OrdinalIgnoreCase);
            this._cacheById = new Dictionary<Guid, TextFontMetaData>();
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="FontMetaDataInfoExtractStrategy"/> class.
        /// </summary>
        ~FontMetaDataInfoExtractStrategy()
        {
            Dispose(true);
        }

        #endregion

        #region Methods

        /// <inheritdoc />
        public TextFontMetaData AddOrGetFontInfo(float fontSize, PdfFont font)
        {
            var fontPrg = font.GetFontProgram();
            var fontName = fontPrg.GetFontNames().GetFontName();

            this._locker.EnterReadLock();
            try
            {
                if (this._cache.TryGetValue(fontName, out var fontByName) &&
                    fontByName.TryGetValue(fontSize, out var fontInfo))
                {
                    return fontInfo;
                }
            }
            finally
            {
                this._locker.ExitReadLock();
            }

            this._locker.EnterWriteLock();
            try
            {
                if (this._cache.TryGetValue(fontName, out var fontByName) &&
                    fontByName.TryGetValue(fontSize, out var fontInfo))
                {
                    return fontInfo;
                }

                var info = BuildFontInfo(fontSize, fontPrg, fontName, font);

                Dictionary<float, TextFontMetaData>? cacheBySize;
                if (!this._cache.TryGetValue(fontName, out cacheBySize))
                {
                    cacheBySize = new Dictionary<float, TextFontMetaData>();
                    this._cache.Add(fontName, cacheBySize);
                }

                this._cacheById[info.Uid] = info;
                cacheBySize[fontSize] = info;
                return info;
            }
            finally
            {
                this._locker.ExitWriteLock();
            }
        }

        /// <summary>
        /// Builds the font information.
        /// </summary>
        private TextFontMetaData BuildFontInfo(float fontSize,
                                               FontProgram fontPrg,
                                               string fontName,
                                               PdfFont font)
        {
            float minSize = float.MaxValue;
            float maxSize = float.MinValue;

            foreach (var c in s_testTextFont)
            {
                var width = font.GetWidth(c, fontSize);
                if (minSize > width)
                    minSize = width;

                if (maxSize < width)
                    maxSize = width;
            }

            var lineSize = font.GetAscent(TEXT_SIZE, fontSize) - font.GetDescent(TEXT_SIZE, fontSize);

            return new TextFontMetaData(Guid.NewGuid(), fontName, fontSize, minSize, maxSize, lineSize);
        }

        /// <inheritdoc />
        public TextFontMetaData Get(Guid uid)
        {
            this._locker.EnterReadLock();
            try
            {
                if (this._cacheById.TryGetValue(uid, out var cachedFont))
                    return cachedFont;

                throw new KeyNotFoundException("Font with id : " + uid + " is not register in thos manager");
            }
            finally
            {
                this._locker.ExitReadLock();
            }
        }

        /// <inheritdoc />
        public IReadOnlyCollection<TextFontMetaData> GetAll()
        {
            this._locker.EnterReadLock();
            try
            {
                return this._cacheById.Values.ToArray();
            }
            finally
            {
                this._locker.ExitReadLock();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(false);
        }

        /// <summary>
        /// Disposes the specified from finalizer.
        /// </summary>
        /// <param name="fromFinalizer">if set to <c>true</c> [from finalizer].</param>
        private void Dispose(bool fromFinalizer)
        {
            if (Interlocked.Increment(ref _disposeCounter) > 1)
                return;

            if (fromFinalizer)
                return;

            this._locker.Dispose();
        }

        #endregion
    }
}
