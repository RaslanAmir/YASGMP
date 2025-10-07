using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YasGMP.Diagnostics
{
    /// <summary>
    /// Represents the i profiler value.
    /// </summary>
    public interface IProfiler
    {
        IDisposable Span(string category, string name, IDictionary<string, object?>? data = null);
        Task<T> TimeAsync<T>(string category, string name, Func<Task<T>> action, IDictionary<string, object?>? data = null);
        Task TimeAsync(string category, string name, Func<Task> action, IDictionary<string, object?>? data = null);
    }
}

