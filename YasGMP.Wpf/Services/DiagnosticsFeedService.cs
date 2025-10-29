using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Diagnostics;
using YasGMP.Services;
using YasGMP.Services.Logging;

namespace YasGMP.Wpf.Services
{
    /// <summary>
    /// Streams diagnostics data (telemetry, log tail, and health snapshots) to UI consumers.
    /// </summary>
    public sealed class DiagnosticsFeedService : IAsyncDisposable, IDisposable
    {
        private readonly FileLogService _fileLog;
        private readonly DiagnosticContext _context;
        private readonly ITrace _trace;
        private readonly IUiDispatcher _dispatcher;
        private readonly object _gate = new();
        private readonly List<Action<IReadOnlyDictionary<string, object?>>> _telemetrySubscribers = new();
        private readonly List<Action<string>> _logSubscribers = new();
        private readonly List<Action<IReadOnlyDictionary<string, object?>>> _healthSubscribers = new();

        private CancellationTokenSource? _cts;
        private Task? _logLoop;
        private Task? _telemetryLoop;
        private Task? _healthLoop;
        private FileStream? _logStream;
        private StreamReader? _logReader;
        private string? _logPath;
        private bool _started;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiagnosticsFeedService"/> class.
        /// </summary>
        public DiagnosticsFeedService(
            FileLogService fileLog,
            DiagnosticContext context,
            ITrace trace,
            IUiDispatcher dispatcher)
        {
            _fileLog = fileLog ?? throw new ArgumentNullException(nameof(fileLog));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _trace = trace ?? throw new ArgumentNullException(nameof(trace));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        /// <summary>Begins streaming diagnostics on background workers.</summary>
        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                if (_started)
                {
                    return Task.CompletedTask;
                }

                _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var token = _cts.Token;

                _logLoop = Task.Run(() => RunLogLoopAsync(token), token);
                _telemetryLoop = Task.Run(() => RunTelemetryLoopAsync(token), token);
                _healthLoop = Task.Run(() => RunHealthLoopAsync(token), token);
                _started = true;
            }

            return Task.CompletedTask;
        }

        /// <summary>Stops background workers and releases file handles.</summary>
        public async Task StopAsync()
        {
            Task[] tasks;
            lock (_gate)
            {
                if (!_started)
                {
                    return;
                }

                _started = false;
                try
                {
                    _cts?.Cancel();
                }
                catch
                {
                    // Ignore cancellation failures.
                }

                tasks = new[] { _logLoop, _telemetryLoop, _healthLoop }
                    .Where(t => t is not null)
                    .Cast<Task>()
                    .ToArray();
            }

            try
            {
                if (tasks.Length > 0)
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when shutting down.
            }
            catch (AggregateException ex)
            {
                foreach (var inner in ex.InnerExceptions)
                {
                    _trace.Log(DiagLevel.Error, "diag_feed", "stop_failure", inner.Message, inner);
                }
            }
            finally
            {
                lock (_gate)
                {
                    _cts?.Dispose();
                    _cts = null;
                    _logLoop = _telemetryLoop = _healthLoop = null;
                    CloseLogStream();
                }
            }
        }

        /// <summary>
        /// Subscribes to periodic telemetry snapshots captured from the diagnostic context.
        /// </summary>
        public IDisposable SubscribeTelemetry(Action<IReadOnlyDictionary<string, object?>> callback)
        {
            if (callback is null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            lock (_gate)
            {
                _telemetrySubscribers.Add(callback);
            }

            return new Subscription<IReadOnlyDictionary<string, object?>>(this, _telemetrySubscribers, callback);
        }

        /// <summary>Subscribes to live log entries appended to the rolling file logger.</summary>
        public IDisposable SubscribeLog(Action<string> callback)
        {
            if (callback is null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            lock (_gate)
            {
                _logSubscribers.Add(callback);
            }

            return new Subscription<string>(this, _logSubscribers, callback);
        }

        /// <summary>Subscribes to periodic health snapshots built from <see cref="HealthReport"/>.</summary>
        public IDisposable SubscribeHealth(Action<IReadOnlyDictionary<string, object?>> callback)
        {
            if (callback is null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            lock (_gate)
            {
                _healthSubscribers.Add(callback);
            }

            return new Subscription<IReadOnlyDictionary<string, object?>>(this, _healthSubscribers, callback);
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            await StopAsync().ConfigureAwait(false);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            StopAsync().GetAwaiter().GetResult();
        }

        private async Task RunLogLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await EnsureLogReaderAsync(token).ConfigureAwait(false);
                    if (_logReader is null)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), token).ConfigureAwait(false);
                        continue;
                    }

                    string? line;
                    while (!token.IsCancellationRequested && (line = await _logReader.ReadLineAsync().ConfigureAwait(false)) is not null)
                    {
                        if (!string.IsNullOrEmpty(line))
                        {
                            PublishLog(line);
                        }
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(250), token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _trace.Log(DiagLevel.Error, "diag_feed", "log_tail_error", ex.Message, ex);
                    await Task.Delay(TimeSpan.FromSeconds(2), token).ConfigureAwait(false);
                    CloseLogStream();
                }
            }
        }

        private async Task EnsureLogReaderAsync(CancellationToken token)
        {
            var path = _fileLog.CurrentLogFilePath;
            if (string.IsNullOrWhiteSpace(path))
            {
                CloseLogStream();
                return;
            }

            if (!string.Equals(path, _logPath, StringComparison.OrdinalIgnoreCase))
            {
                CloseLogStream();
                _logPath = path;
            }

            if (_logReader is not null)
            {
                if (_logStream is not null && !File.Exists(_logPath))
                {
                    CloseLogStream();
                }

                return;
            }

            if (!File.Exists(path))
            {
                return;
            }

            await Task.Yield();
            _logStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            _logReader = new StreamReader(_logStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true);
            _logStream.Seek(0, SeekOrigin.End);
        }

        private async Task RunTelemetryLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var snapshot = BuildTelemetrySnapshot();
                    PublishTelemetry(snapshot);
                }
                catch (Exception ex)
                {
                    _trace.Log(DiagLevel.Error, "diag_feed", "telemetry_error", ex.Message, ex);
                }

                await Task.Delay(TimeSpan.FromSeconds(5), token).ConfigureAwait(false);
            }
        }

        private async Task RunHealthLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var health = HealthReport.BuildBasic();
                    PublishHealth(health);
                }
                catch (Exception ex)
                {
                    _trace.Log(DiagLevel.Error, "diag_feed", "health_error", ex.Message, ex);
                }

                await Task.Delay(TimeSpan.FromSeconds(10), token).ConfigureAwait(false);
            }
        }

        private IReadOnlyDictionary<string, object?> BuildTelemetrySnapshot()
        {
            var snapshot = new Dictionary<string, object?>
            {
                ["ts_utc"] = DateTime.UtcNow.ToString("o"),
                ["corr_id"] = _context.CorrId,
                ["span_id"] = _context.SpanId,
                ["parent_span"] = _context.ParentSpanId,
                ["user_id"] = _context.UserId,
                ["username"] = _context.Username,
                ["roles"] = _context.RoleIdsCsv,
                ["ip"] = _context.Ip,
                ["session_id"] = _context.SessionId,
                ["device"] = _context.Device,
                ["os"] = _context.OsVersion,
                ["app_version"] = _context.AppVersion,
                ["git_commit"] = _context.GitCommit,
                ["diag_level"] = _context.Level.ToString(),
                ["diag_enabled"] = _context.Enabled
            };

            return snapshot;
        }

        private void PublishTelemetry(IReadOnlyDictionary<string, object?> payload)
        {
            List<Action<IReadOnlyDictionary<string, object?>>> subscribers;
            lock (_gate)
            {
                if (_telemetrySubscribers.Count == 0)
                {
                    return;
                }

                subscribers = _telemetrySubscribers.ToList();
            }

            foreach (var subscriber in subscribers)
            {
                var copy = payload;
                try
                {
                    _dispatcher.BeginInvoke(() => subscriber(copy));
                }
                catch (Exception ex)
                {
                    _trace.Log(DiagLevel.Warn, "diag_feed", "telemetry_dispatch_failed", ex.Message, ex);
                }
            }
        }

        private void PublishLog(string line)
        {
            List<Action<string>> subscribers;
            lock (_gate)
            {
                if (_logSubscribers.Count == 0)
                {
                    return;
                }

                subscribers = _logSubscribers.ToList();
            }

            foreach (var subscriber in subscribers)
            {
                var copy = line;
                try
                {
                    _dispatcher.BeginInvoke(() => subscriber(copy));
                }
                catch (Exception ex)
                {
                    _trace.Log(DiagLevel.Warn, "diag_feed", "log_dispatch_failed", ex.Message, ex);
                }
            }
        }

        private void PublishHealth(IReadOnlyDictionary<string, object?> payload)
        {
            List<Action<IReadOnlyDictionary<string, object?>>> subscribers;
            lock (_gate)
            {
                if (_healthSubscribers.Count == 0)
                {
                    return;
                }

                subscribers = _healthSubscribers.ToList();
            }

            foreach (var subscriber in subscribers)
            {
                var copy = payload;
                try
                {
                    _dispatcher.BeginInvoke(() => subscriber(copy));
                }
                catch (Exception ex)
                {
                    _trace.Log(DiagLevel.Warn, "diag_feed", "health_dispatch_failed", ex.Message, ex);
                }
            }
        }

        private void CloseLogStream()
        {
            try { _logReader?.Dispose(); }
            catch { }
            finally { _logReader = null; }

            try { _logStream?.Dispose(); }
            catch { }
            finally { _logStream = null; }
        }

        private sealed class Subscription<T> : IDisposable
        {
            private readonly DiagnosticsFeedService _owner;
            private readonly List<Action<T>> _list;
            private readonly Action<T> _handler;
            private bool _disposed;

            public Subscription(DiagnosticsFeedService owner, List<Action<T>> list, Action<T> handler)
            {
                _owner = owner;
                _list = list;
                _handler = handler;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                lock (_owner._gate)
                {
                    _list.Remove(_handler);
                }
            }
        }
    }
}
