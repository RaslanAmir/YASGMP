using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using YasGMP.Services;

namespace YasGMP.Wpf.Services
{
    /// <summary>
    /// WPF implementation of <see cref="IUiDispatcher"/> that redirects MAUI dispatcher calls to the
    /// shell's <see cref="Dispatcher"/> so shared view-models can marshal back to the UI thread.</summary>
    /// <remarks>
    /// <para><strong>MAUI vs. WPF:</strong> MAUI handlers often run on the UI thread by default, whereas
    /// WPF background work (async commands, Task.Run) must explicitly invoke the dispatcher before
    /// touching dependency properties or visual elements.</para>
    /// <para><strong>Porting guidance:</strong> when migrating from MAUI, wrap any continuations that
    /// update observable collections, dependency properties, or views in
    /// <see cref="InvokeAsync(Action)"/>/<see cref="InvokeAsync(Func{Task})"/>. Avoid
    /// <see cref="Task.Wait()"/> or <see cref="Task.Result"/> on dispatcher calls; prefer
    /// <c>await</c> to prevent deadlocks.</para>
    /// <para><strong>Thread affinity:</strong> callers may inspect <see cref="IsDispatchRequired"/> when
    /// running on the WPF UI thread (e.g., message pumps) to skip redundant marshaling while keeping
    /// logic shared with MAUI.</para>
    /// </remarks>
    public sealed class WpfUiDispatcher : IUiDispatcher
    {
        private static Dispatcher CurrentDispatcher => Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        /// <summary>
        /// Executes the is dispatch required operation.
        /// </summary>

        public bool IsDispatchRequired => !CurrentDispatcher.CheckAccess();
        /// <summary>
        /// Executes the invoke async operation.
        /// </summary>

        public Task InvokeAsync(Action action)
        {
            if (!IsDispatchRequired)
            {
                action();
                return Task.CompletedTask;
            }

            return CurrentDispatcher.InvokeAsync(action).Task;
        }
        /// <summary>
        /// Executes the invoke async operation.
        /// </summary>

        public Task<T> InvokeAsync<T>(Func<T> func)
        {
            if (!IsDispatchRequired)
                return Task.FromResult(func());

            return CurrentDispatcher.InvokeAsync(func).Task;
        }
        /// <summary>
        /// Executes the invoke async operation.
        /// </summary>

        public async Task InvokeAsync(Func<Task> asyncAction)
        {
            if (!IsDispatchRequired)
            {
                await asyncAction().ConfigureAwait(false);
                return;
            }

            await CurrentDispatcher.InvokeAsync(asyncAction).Task.Unwrap().ConfigureAwait(false);
        }
        /// <summary>
        /// Executes the begin invoke operation.
        /// </summary>

        public void BeginInvoke(Action action)
        {
            CurrentDispatcher.BeginInvoke(action);
        }
    }
}
