// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Viewer.Tools
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using System.Windows.Input;

    public sealed class AsyncDelegateCommand : ICommand
    {
        #region Fields

        private readonly Func<ValueTask> _callbackCommand;
        private readonly Predicate<object?>? _predicate;

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncDelegateCommand"/> class.
        /// </summary>
        public AsyncDelegateCommand(Func<ValueTask> callbackCommand,
                                    Predicate<object?>? predicate = null)
        {
            ArgumentNullException.ThrowIfNull(callbackCommand);

            this._callbackCommand = callbackCommand;
            this._predicate = predicate;
        }

        #endregion

        #region Events

        public event EventHandler? CanExecuteChanged;

        #endregion

        #region Methods

        /// <inheritdoc />
        public bool CanExecute(object? parameter)
        {
            return this._predicate?.Invoke(parameter) ?? true;
        }

        /// <inheritdoc />
        public void Execute(object? parameter)
        {
            Task.Run(async () =>
            {
                try
                {
                    await this._callbackCommand();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            });
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}
