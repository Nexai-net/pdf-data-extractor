// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Viewer.Models
{
    using Microsoft.Extensions.Logging;

    using System;

    /// <summary>
    /// Relay event logger
    /// </summary>
    /// <seealso cref="ILogger" />
    internal sealed class RelayLogger : ILogger
    {
        #region Fields

        private readonly string _prefix;
        private readonly Action<Log> _onLog;

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayLogger"/> class.
        /// </summary>
        public RelayLogger(string prefix, Action<Log> onLog)
        {
            this._prefix = prefix;
            this._onLog = onLog;
        }

        #endregion

        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        /// <inheritdoc />
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var msg = formatter(state, exception) ?? string.Empty;
            this._onLog(new Log(logLevel, msg));
        }
    }
}
