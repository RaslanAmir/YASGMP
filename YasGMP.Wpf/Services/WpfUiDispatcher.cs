using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using YasGMP.Services;

namespace YasGMP.Wpf.Services
{
    /// <summary>WPF implementation of <see cref="IUiDispatcher"/> backed by the application dispatcher.</summary>
    public sealed class WpfUiDispatcher : IUiDispatcher
    {
        private static Dispatcher CurrentDispatcher => Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

        public bool IsDispatchRequired => !CurrentDispatcher.CheckAccess();

        public Task InvokeAsync(Action action)
        {
            if (!IsDispatchRequired)
            {
                action();
                return Task.CompletedTask;
            }

            return CurrentDispatcher.InvokeAsync(action).Task;
        }

        public Task<T> InvokeAsync<T>(Func<T> func)
        {
            if (!IsDispatchRequired)
                return Task.FromResult(func());

            return CurrentDispatcher.InvokeAsync(func).Task;
        }

        public async Task InvokeAsync(Func<Task> asyncAction)
        {
            if (!IsDispatchRequired)
            {
                await asyncAction().ConfigureAwait(false);
                return;
            }

            await CurrentDispatcher.InvokeAsync(asyncAction).Task.Unwrap().ConfigureAwait(false);
        }

        public void BeginInvoke(Action action)
        {
            CurrentDispatcher.BeginInvoke(action);
        }
    }
}

