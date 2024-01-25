// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Services
{
    using iText.Kernel.Pdf.Xobject;

    using PDF.Data.Extractor.Abstractions.MetaData;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Manager in charge to store the image sources
    /// </summary>
    /// <seealso cref="IDisposable" />
    /// <seealso cref="IImageManager" />
    public sealed class DefaultImageManager : IDisposable, IImageManager
    {
        #region Fields

        private readonly Dictionary<string, ImageMetaData> _imageCache;
        private readonly ReaderWriterLockSlim _locker;

        private long _disposeCounter;

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultImageManager"/> class.
        /// </summary>
        public DefaultImageManager()
        {
            this._imageCache = new Dictionary<string, ImageMetaData>();
            this._locker = new ReaderWriterLockSlim();
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="DefaultImageManager"/> class.
        /// </summary>
        ~DefaultImageManager()
        {
            Dispose(true);
        }

        #endregion

        #region Methods

        /// <inheritdoc />
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(false);
        }

        /// <inheritdoc />
        public ImageMetaData AddImageResource(PdfImageXObject image)
        {
            var bytes = image.GetImageBytes();
            string hash = string.Empty;

            using (var hasher = SHA512.Create())
            {
                var hashBytes = hasher.ComputeHash(bytes);
                hash = Convert.ToBase64String(hashBytes);
            }

            this._locker.EnterReadLock();
            try
            {
                if (this._imageCache.TryGetValue(hash, out var metaData))
                    return metaData;
            }
            finally
            {
                this._locker.ExitReadLock();
            }

            this._locker.EnterWriteLock();
            try
            {
                if (this._imageCache.TryGetValue(hash, out var metaData))
                    return metaData;

                var raw = image.GetImageBytes();
                if (raw != null && raw.Any())
                    raw = Encoding.UTF8.GetBytes(Convert.ToBase64String(raw));

                var newMetaData = new ImageMetaData(Guid.NewGuid(),
                                                    image.IdentifyImageFileExtension(),
                                                    image.IdentifyImageType().ToString(),
                                                    raw,
                                                    image.GetWidth(),
                                                    image.GetHeight(),
                                                    hash);

                this._imageCache.Add(hash, newMetaData);
                return newMetaData;
            }
            finally
            {
                this._locker.ExitWriteLock();
            }
        }

        /// <inheritdoc />
        public IReadOnlyCollection<ImageMetaData> GetAll()
        {
            this._locker.EnterReadLock();
            try
            {
                return this._imageCache.Values.ToArray();
            }
            finally
            {
                this._locker.ExitReadLock();
            }
        }

        /// <inheritdoc cref="IDisposable.Dispose" />
        private void Dispose(bool fromFinalizer)
        {
            if (Interlocked.Increment(ref _disposeCounter) > 1)
                return;

            if (fromFinalizer == false)
                this._locker.Dispose();
        }

        #endregion
    }
}
