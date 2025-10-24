using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace YasGMP.Diagnostics
{
    public sealed class Profiler : IProfiler
    {
        private readonly ITrace _trace;

        public Profiler(ITrace trace)
        {
            _trace = trace;
        }

        public IDisposable Span(string category, string name, IDictionary<string, object?>? data = null)
            => _trace.StartSpan(category, name, data);

        public async Task<T> TimeAsync<T>(string category, string name, Func<Task<T>> action, IDictionary<string, object?>? data = null)
        {
            using var span = _trace.StartSpan(category, name, data);
            var sw = Stopwatch.StartNew();
            try { return await action().ConfigureAwait(false); }
            finally
            {
                sw.Stop();
                _trace.Log(DiagLevel.Debug, category, "timing", name, data: new Dictionary<string, object?>
                {
                    ["duration_ms"] = (int)sw.Elapsed.TotalMilliseconds
                });
            }
        }

        public async Task TimeAsync(string category, string name, Func<Task> action, IDictionary<string, object?>? data = null)
        {
            using var span = _trace.StartSpan(category, name, data);
            var sw = Stopwatch.StartNew();
            try { await action().ConfigureAwait(false); }
            finally
            {
                sw.Stop();
                _trace.Log(DiagLevel.Debug, category, "timing", name, data: new Dictionary<string, object?>
                {
                    ["duration_ms"] = (int)sw.Elapsed.TotalMilliseconds
                });
            }
        }
    }

    internal sealed class NullProfilerSpan : IDisposable
    {
        public static readonly NullProfilerSpan Instance = new();
        public void Dispose() { }
    }
}

