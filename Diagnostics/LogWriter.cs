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
    public interface ILogWriter
    {
        void Enqueue(DiagnosticEvent evt);
        void Flush(TimeSpan? timeout = null);
    }

    public interface ILogSink
    {
        string Name { get; }
        void WriteBatch(IReadOnlyList<DiagnosticEvent> batch);
    }

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

        public LogWriter(int capacity, int drainBatch, int drainIntervalMs, IEnumerable<ILogSink> sinks)
        {
            _capacity = Math.Max(1024, capacity);
            _drainBatch = Math.Max(32, drainBatch);
            _drainIntervalMs = Math.Clamp(drainIntervalMs, 25, 1000);
            _sinks.AddRange(sinks ?? Enumerable.Empty<ILogSink>());
            _drainLoop = Task.Run(DrainLoopAsync);
        }

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

        public void Dispose()
        {
            try { _cts.Cancel(); } catch { }
            try { _drainLoop.Wait(500); } catch { }
            DrainOnce(forceAll: true);
        }
    }

    public sealed class DiagnosticEvent
    {
        public DateTimeOffset TsUtc { get; set; }
        public DiagLevel Level { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Event { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? CorrelationId { get; set; }
        public string? SpanId { get; set; }
        public string? ParentSpanId { get; set; }
        public int? UserId { get; set; }
        public string? Username { get; set; }
        public string? RoleIds { get; set; }
        public string? Ip { get; set; }
        public string? SessionId { get; set; }
        public string? Device { get; set; }
        public string? OsVer { get; set; }
        public string? AppVer { get; set; }
        public string? GitCommit { get; set; }
        public string? DbSchemaHash { get; set; }
        public string? ExceptionType { get; set; }
        public string? ExceptionMessage { get; set; }
        public string? Stack { get; set; }
        public Dictionary<string, object?>? Data { get; set; }
        public string? SigReason { get; set; }
        public string? SigDocRef { get; set; }
        public string? SigHash { get; set; }

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
