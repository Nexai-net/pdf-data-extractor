// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Viewer.Models
{
    using Microsoft.Extensions.Logging;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal class RelayLoggerProvider : ILoggerProvider
    {
        #region Fields

        private readonly Action<Log> _onLog;

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayLoggerProvider"/> class.
        /// </summary>
        public RelayLoggerProvider(Action<Log> onLog)
        {
            this._onLog = onLog;
        }

        #endregion

        /// <inheritdoc />
        public ILogger CreateLogger(string categoryName)
        {
            return new RelayLogger(categoryName, this._onLog);
        }

        public void Dispose()
        {
            return;
        }
    }
}
