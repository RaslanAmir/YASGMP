using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace YasGMP.Diagnostics
{
    /// <summary>
    /// Represents the Trace Manager.
    /// </summary>
    public sealed class TraceManager : ITrace
    {
        private readonly DiagnosticContext _ctx;
        private readonly ILogWriter _writer;
        /// <summary>
        /// Initializes a new instance of the TraceManager class.
        /// </summary>

        public TraceManager(DiagnosticContext ctx, ILogWriter writer)
        {
            _ctx = ctx;
            _writer = writer;
        }
        /// <summary>
        /// Gets or sets the current correlation id.
        /// </summary>

        public string CurrentCorrelationId => _ctx.CorrId;
        /// <summary>
        /// Gets or sets the current span id.
        /// </summary>
        public string CurrentSpanId => _ctx.SpanId;
        /// <summary>
        /// Executes the log operation.
        /// </summary>

        public void Log(DiagLevel level, string category, string evt, string message, Exception? ex = null, IDictionary<string, object?>? data = null)
        {
            if (!_ctx.Enabled) return;
            if (level < _ctx.Level) return;

            var now = DateTimeOffset.UtcNow;
            var payload = new DiagnosticEvent
            {
                TsUtc = now,
                Level = level,
                Category = category,
                Event = evt,
                Message = message,
                CorrelationId = _ctx.CorrId,
                SpanId = _ctx.SpanId,
                ParentSpanId = _ctx.ParentSpanId,
                UserId = _ctx.UserId,
                Username = _ctx.Username,
                RoleIds = _ctx.RoleIdsCsv,
                Ip = _ctx.Ip,
                SessionId = _ctx.SessionId,
                Device = _ctx.Device,
                OsVer = _ctx.OsVersion,
                AppVer = _ctx.AppVersion,
                GitCommit = _ctx.GitCommit,
                DbSchemaHash = _ctx.DbSchemaHash,
                ExceptionType = ex?.GetType().FullName,
                ExceptionMessage = ex?.Message,
                Stack = ex?.ToString(),
                Data = (Dictionary<string, object?>?)Redactor.Redact(ToDict(data), _ctx.RedactionEnabled)
            };
            _writer.Enqueue(payload);
        }
        /// <summary>
        /// Executes the start span operation.
        /// </summary>

        public IDisposable StartSpan(string category, string name, IDictionary<string, object?>? data = null)
        {
            if (!_ctx.Enabled) return NullProfilerSpan.Instance;
            var parent = _ctx.PushSpan();
            var sw = Stopwatch.StartNew();
            Log(DiagLevel.Debug, category, "span_start", name, data: data);
            return new ProfilerSpan(this, _ctx, parent, sw, category, name, data);
        }

        private static Dictionary<string, object?>? ToDict(IDictionary<string, object?>? src)
        {
            if (src == null) return null;
            return src is Dictionary<string, object?> d ? d : new Dictionary<string, object?>(src);
        }

        private sealed class ProfilerSpan : IDisposable
        {
            private readonly TraceManager _tm;
            private readonly DiagnosticContext _ctx;
            private readonly string? _parent;
            private readonly Stopwatch _sw;
            private readonly string _category;
            private readonly string _name;
            private readonly IDictionary<string, object?>? _data;
            private bool _done;
            /// <summary>
            /// Initializes a new instance of the ProfilerSpan class.
            /// </summary>

            public ProfilerSpan(TraceManager tm, DiagnosticContext ctx, string? parent, Stopwatch sw, string category, string name, IDictionary<string, object?>? data)
            {
                _tm = tm; _ctx = ctx; _parent = parent; _sw = sw; _category = category; _name = name; _data = data;
            }
            /// <summary>
            /// Executes the dispose operation.
            /// </summary>

            public void Dispose()
            {
                if (_done) return; _done = true;
                _sw.Stop();
                _tm.Log(DiagLevel.Debug, _category, "span_end", _name, data: new Dictionary<string, object?>
                {
                    ["duration_ms"] = (int)_sw.Elapsed.TotalMilliseconds
                });
                _ctx.PopSpan(_parent);
            }
        }
    }
}

