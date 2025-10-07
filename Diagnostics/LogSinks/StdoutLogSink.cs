using System;
using System.Collections.Generic;

namespace YasGMP.Diagnostics.LogSinks
{
    internal sealed class StdoutLogSink : ILogSink
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name => "stdout";
        /// <summary>
        /// Executes the write batch operation.
        /// </summary>
        public void WriteBatch(IReadOnlyList<DiagnosticEvent> batch)
        {
            if (batch == null) return;
            foreach (var e in batch)
            {
                try { Console.WriteLine(e.ToJson()); }
                catch { }
            }
        }
    }
}

