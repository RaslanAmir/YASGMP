using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Maui.Storage;
using YasGMP.Diagnostics.LogSinks;

namespace YasGMP.Diagnostics
{
    /// <summary>
    /// Represents the Diagnostics Hub.
    /// </summary>
    public sealed class DiagnosticsHub
    {
        private readonly DiagnosticContext _ctx;
        private readonly List<ILogSink> _sinks;
        /// <summary>
        /// Initializes a new instance of the DiagnosticsHub class.
        /// </summary>

        public DiagnosticsHub(DiagnosticContext ctx, IEnumerable<ILogSink> sinks)
        {
            _ctx = ctx;
            _sinks = sinks?.ToList() ?? new List<ILogSink>();
        }
        /// <summary>
        /// Executes the primary log dir operation.
        /// </summary>

        public string PrimaryLogDir => Path.Combine(FileSystem.AppDataDirectory, "logs");
        /// <summary>
        /// Executes the secondary log dir operation.
        /// </summary>
        public string SecondaryLogDir => Path.Combine(AppContext.BaseDirectory, "logs");
        /// <summary>
        /// Executes the flush elastic buffer operation.
        /// </summary>

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

