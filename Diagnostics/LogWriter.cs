using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace YasGMP.Diagnostics
{
    /// <summary>
    /// Represents the i log writer value.
    /// </summary>
    public interface ILogWriter
    {
        void Enqueue(DiagnosticEvent evt);
        void Flush(TimeSpan? timeout = null);
    }
    /// <summary>
    /// Represents the i log sink value.
    /// </summary>

    public interface ILogSink
    {
        string Name { get; }
        void WriteBatch(IReadOnlyList<DiagnosticEvent> batch);
    }
    /// <summary>
    /// Represents the Log Writer.
    /// </summary>

    public sealed class LogWriter : ILogWriter, IDisposable
    {
        private readonly ConcurrentQueue<DiagnosticEvent> _queue = new();
        private readonly List<ILogSink> _sinks = new();
        private readonly int _capacity;
        private readonly int _drainBatch;
        private readonly int _drainIntervalMs;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _drainLoop;
        private int _count;
        /// <summary>
        /// Initializes a new instance of the LogWriter class.
        /// </summary>

        public LogWriter(int capacity, int drainBatch, int drainIntervalMs, IEnumerable<ILogSink> sinks)
        {
            _capacity = Math.Max(1024, capacity);
            _drainBatch = Math.Max(32, drainBatch);
            _drainIntervalMs = Math.Clamp(drainIntervalMs, 25, 1000);
            _sinks.AddRange(sinks ?? Enumerable.Empty<ILogSink>());
            _drainLoop = Task.Run(DrainLoopAsync);
        }
        /// <summary>
        /// Executes the enqueue operation.
        /// </summary>

        public void Enqueue(DiagnosticEvent evt)
        {
            if (evt.Level <= DiagLevel.Debug)
            {
                if (_count >= _capacity) return; // drop Trace/Debug under backpressure
            }
            else if (_count >= _capacity * 2)
            {
                // As a last resort, drop info too to avoid OOM. Never drop warn+ preferentially.
                if (evt.Level == DiagLevel.Info) return;
            }

            _queue.Enqueue(evt);
            Interlocked.Increment(ref _count);
        }
        /// <summary>
        /// Executes the flush operation.
        /// </summary>

        public void Flush(TimeSpan? timeout = null)
        {
            var until = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(2));
            while (_count > 0 && DateTime.UtcNow < until)
            {
                Thread.Sleep(20);
            }
        }

        private async Task DrainLoopAsync()
        {
            var token = _cts.Token;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(_drainIntervalMs, token).ConfigureAwait(false);
                    DrainOnce();
                }
            }
            catch { /* ignore */ }
            finally
            {
                // Final drain on shutdown
                DrainOnce(forceAll: true);
            }
        }

        private void DrainOnce(bool forceAll = false)
        {
            if (_count == 0) return;
            var batch = new List<DiagnosticEvent>(Math.Min(_drainBatch, _count));
            while (batch.Count < (forceAll ? int.MaxValue : _drainBatch) && _queue.TryDequeue(out var evt))
            {
                Interlocked.Decrement(ref _count);
                batch.Add(evt);
                if (!forceAll && batch.Count >= _drainBatch) break;
            }
            if (batch.Count == 0) return;

            foreach (var sink in _sinks)
            {
                try { sink.WriteBatch(batch); }
                catch { /* never throw from sinks */ }
            }
        }
        /// <summary>
        /// Executes the dispose operation.
        /// </summary>

        public void Dispose()
        {
            try { _cts.Cancel(); } catch { }
            try { _drainLoop.Wait(500); } catch { }
            DrainOnce(forceAll: true);
        }
    }
    /// <summary>
    /// Represents the Diagnostic Event.
    /// </summary>

    public sealed class DiagnosticEvent
    {
        /// <summary>
        /// Gets or sets the ts utc.
        /// </summary>
        public DateTimeOffset TsUtc { get; set; }
        /// <summary>
        /// Gets or sets the level.
        /// </summary>
        public DiagLevel Level { get; set; }
        /// <summary>
        /// Gets or sets the category.
        /// </summary>
        public string Category { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the event.
        /// </summary>
        public string Event { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        public string Message { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the correlation id.
        /// </summary>
        public string? CorrelationId { get; set; }
        /// <summary>
        /// Gets or sets the span id.
        /// </summary>
        public string? SpanId { get; set; }
        /// <summary>
        /// Gets or sets the parent span id.
        /// </summary>
        public string? ParentSpanId { get; set; }
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        public int? UserId { get; set; }
        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        public string? Username { get; set; }
        /// <summary>
        /// Gets or sets the role ids.
        /// </summary>
        public string? RoleIds { get; set; }
        /// <summary>
        /// Gets or sets the ip.
        /// </summary>
        public string? Ip { get; set; }
        /// <summary>
        /// Gets or sets the session id.
        /// </summary>
        public string? SessionId { get; set; }
        /// <summary>
        /// Gets or sets the device.
        /// </summary>
        public string? Device { get; set; }
        /// <summary>
        /// Gets or sets the os ver.
        /// </summary>
        public string? OsVer { get; set; }
        /// <summary>
        /// Gets or sets the app ver.
        /// </summary>
        public string? AppVer { get; set; }
        /// <summary>
        /// Gets or sets the git commit.
        /// </summary>
        public string? GitCommit { get; set; }
        /// <summary>
        /// Gets or sets the db schema hash.
        /// </summary>
        public string? DbSchemaHash { get; set; }
        /// <summary>
        /// Gets or sets the exception type.
        /// </summary>
        public string? ExceptionType { get; set; }
        /// <summary>
        /// Gets or sets the exception message.
        /// </summary>
        public string? ExceptionMessage { get; set; }
        /// <summary>
        /// Gets or sets the stack.
        /// </summary>
        public string? Stack { get; set; }
        /// <summary>
        /// Gets or sets the object.
        /// </summary>
        public Dictionary<string, object?>? Data { get; set; }
        /// <summary>
        /// Gets or sets the sig reason.
        /// </summary>
        public string? SigReason { get; set; }
        /// <summary>
        /// Gets or sets the sig doc ref.
        /// </summary>
        public string? SigDocRef { get; set; }
        /// <summary>
        /// Gets or sets the sig hash.
        /// </summary>
        public string? SigHash { get; set; }
        /// <summary>
        /// Executes the to json operation.
        /// </summary>

        public string ToJson()
        {
            var obj = new Dictionary<string, object?>
            {
                ["ts_utc"] = TsUtc.ToString("o"),
                ["lvl"] = Level.ToString().ToLowerInvariant(),
                ["cat"] = Category,
                ["evt"] = Event,
                ["msg"] = Message,
                ["corr_id"] = CorrelationId,
                ["span_id"] = SpanId,
                ["parent_span_id"] = ParentSpanId,
                ["user_id"] = UserId,
                ["username"] = Username,
                ["role_ids"] = RoleIds,
                ["ip"] = Ip,
                ["session_id"] = SessionId,
                ["device"] = Device,
                ["os_ver"] = OsVer,
                ["app_ver"] = AppVer,
                ["git_commit"] = GitCommit,
                ["db_schema_hash"] = DbSchemaHash,
                ["ex_type"] = ExceptionType,
                ["ex_msg"] = ExceptionMessage,
                ["stack"] = Stack,
                ["data"] = Data,
                ["sig_reason"] = SigReason,
                ["sig_doc_ref"] = SigDocRef,
                ["sig_hash"] = SigHash,
            };
            return JsonSerializer.Serialize(obj, new JsonSerializerOptions
            {
                WriteIndented = false,
                IgnoreReadOnlyFields = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
        }
    }
}
