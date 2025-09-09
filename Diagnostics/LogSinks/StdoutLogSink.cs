using System;
using System.Collections.Generic;

namespace YasGMP.Diagnostics.LogSinks
{
    internal sealed class StdoutLogSink : ILogSink
    {
        public string Name => "stdout";
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

