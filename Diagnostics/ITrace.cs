using System;
using System.Collections.Generic;

namespace YasGMP.Diagnostics
{
    /// <summary>
    /// Represents the i trace value.
    /// </summary>
    public interface ITrace
    {
        void Log(
            DiagLevel level,
            string category,
            string evt,
            string message,
            Exception? ex = null,
            IDictionary<string, object?>? data = null);

        IDisposable StartSpan(
            string category,
            string name,
            IDictionary<string, object?>? data = null);

        string CurrentCorrelationId { get; }
        string CurrentSpanId { get; }
    }
}

