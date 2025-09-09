using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Maui.Storage;
using YasGMP.Diagnostics.LogSinks;

namespace YasGMP.Diagnostics
{
    public sealed class DiagnosticsHub
    {
        private readonly DiagnosticContext _ctx;
        private readonly List<ILogSink> _sinks;

        public DiagnosticsHub(DiagnosticContext ctx, IEnumerable<ILogSink> sinks)
        {
            _ctx = ctx;
            _sinks = sinks?.ToList() ?? new List<ILogSink>();
        }

        public string PrimaryLogDir => Path.Combine(FileSystem.AppDataDirectory, "logs");
        public string SecondaryLogDir => Path.Combine(AppContext.BaseDirectory, "logs");

        public bool FlushElasticBuffer()
        {
            try
            {
                var elastic = _sinks.OfType<ElasticCompatibleSink>().FirstOrDefault();
                if (elastic == null) return false;
                elastic.FlushBufferNow();
                return true;
            }
            catch { return false; }
        }
    }
}

