using System;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using YasGMP.Services;

namespace YasGMP.Services.Platform
{
    /// <summary>MAUI implementation of <see cref="IUiDispatcher"/> backed by <see cref="MainThread"/>.</summary>
    public sealed class MauiUiDispatcher : IUiDispatcher
    {
        /// <summary>
        /// Gets or sets the is dispatch required.
        /// </summary>
        public bool IsDispatchRequired => !MainThread.IsMainThread;
        /// <summary>
        /// Executes the invoke async operation.
        /// </summary>

        public Task InvokeAsync(Action action) => MainThread.InvokeOnMainThreadAsync(action);
        /// <summary>
        /// Executes the invoke async operation.
        /// </summary>

        public Task<T> InvokeAsync<T>(Func<T> func) => MainThread.InvokeOnMainThreadAsync(func);
        /// <summary>
        /// Executes the invoke async operation.
        /// </summary>

        public Task InvokeAsync(Func<Task> asyncAction) => MainThread.InvokeOnMainThreadAsync(asyncAction);
        /// <summary>
        /// Executes the begin invoke operation.
        /// </summary>

        public void BeginInvoke(Action action) => MainThread.BeginInvokeOnMainThread(action);
    }
}
