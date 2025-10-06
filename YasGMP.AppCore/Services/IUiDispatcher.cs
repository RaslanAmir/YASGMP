using System;
using System.Threading.Tasks;

namespace YasGMP.Services
{
    /// <summary>
    /// Abstraction for marshaling work onto the UI thread.
    /// </summary>
    public interface IUiDispatcher
    {
        /// <summary>Gets a value indicating whether the current thread differs from the UI thread.</summary>
        bool IsDispatchRequired { get; }

        /// <summary>Synchronously schedules <paramref name="action"/> on the UI thread.</summary>
        Task InvokeAsync(Action action);

        /// <summary>Executes the provided function on the UI thread and returns its result.</summary>
        Task<T> InvokeAsync<T>(Func<T> func);

        /// <summary>Executes the asynchronous delegate on the UI thread.</summary>
        Task InvokeAsync(Func<Task> asyncAction);

        /// <summary>Queues an action to the UI thread without awaiting completion.</summary>
        void BeginInvoke(Action action);
    }
}

