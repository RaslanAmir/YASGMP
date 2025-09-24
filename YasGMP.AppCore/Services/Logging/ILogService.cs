using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YasGMP.Services.Logging
{
    /// <summary>
    /// Contract for a structured, thread-safe application log service.
    /// Writes rolling text logs and exposes helpers for Info/Trace/Error entries.
    /// </summary>
    public interface ILogService : IDisposable
    {
        /// <summary>Absolute path to the current log file (JSONL format).</summary>
        string CurrentLogFilePath { get; }

        /// <summary>Writes an informational entry.</summary>
        Task InfoAsync(string source, string message, IDictionary<string, object>? context = null);

        /// <summary>Writes a low-level trace entry (fine-grained, e.g., UI actions, EF SQL).</summary>
        Task TraceAsync(string source, string action, string details, IDictionary<string, object>? context = null);

        /// <summary>Writes an error entry, including exception details.</summary>
        Task ErrorAsync(string source, Exception ex, string? message = null, IDictionary<string, object>? context = null);
    }
}
