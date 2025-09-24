using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace YasGMP.Common
{
    /// <summary>
    /// Basic <see cref="ICommand"/> implementation that executes the provided delegates.
    /// </summary>
    public sealed class DelegateCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelegateCommand"/> class.
        /// </summary>
        public DelegateCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute    = execute    ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <inheritdoc />
        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        /// <inheritdoc />
        public void Execute(object? parameter) => _execute();

        /// <inheritdoc />
        public event EventHandler? CanExecuteChanged;

        /// <summary>Signals that <see cref="CanExecute(object?)"/> should be re-evaluated.</summary>
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Async-friendly command implementation that prevents concurrent execution.
    /// </summary>
    public sealed class AsyncDelegateCommand : ICommand
    {
        private readonly Func<Task> _executeAsync;
        private readonly Func<bool>? _canExecute;
        private bool _isExecuting;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncDelegateCommand"/> class.
        /// </summary>
        public AsyncDelegateCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute   = canExecute;
        }

        /// <inheritdoc />
        public bool CanExecute(object? parameter)
        {
            if (_isExecuting)
                return false;

            return _canExecute?.Invoke() ?? true;
        }

        /// <inheritdoc />
        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
                return;

            _isExecuting = true;
            RaiseCanExecuteChanged();

            try
            {
                await _executeAsync().ConfigureAwait(false);
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        /// <inheritdoc />
        public event EventHandler? CanExecuteChanged;

        /// <summary>Signals that <see cref="CanExecute(object?)"/> should be re-evaluated.</summary>
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
