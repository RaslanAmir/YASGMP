using System;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using YasGMP.Services;

namespace YasGMP.Services.Platform
{
    /// <summary>MAUI implementation of <see cref="IUiDispatcher"/> backed by <see cref="MainThread"/>.</summary>
    public sealed class MauiUiDispatcher : IUiDispatcher
    {
        public bool IsDispatchRequired => !MainThread.IsMainThread;

        public Task InvokeAsync(Action action) => MainThread.InvokeOnMainThreadAsync(action);

        public Task<T> InvokeAsync<T>(Func<T> func) => MainThread.InvokeOnMainThreadAsync(func);

        public Task InvokeAsync(Func<Task> asyncAction) => MainThread.InvokeOnMainThreadAsync(asyncAction);

        public void BeginInvoke(Action action) => MainThread.BeginInvokeOnMainThread(action);
    }
}
