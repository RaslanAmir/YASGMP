using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace YasGMP.Services.Logging
{
    /// <summary>
    /// High-performance, thread-safe rolling file logger.
    /// Logs JSON lines with keys: ts, level, src, action, msg, ctx (object), sessionId, userId.
    /// </summary>
    public sealed class FileLogService : ILogService
    {
        private readonly string _baseDir;
        private readonly string _sessionId;
        private readonly Func<int?> _getUserId;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private string _currentFile = string.Empty;
        private const long MaxBytes = 5L * 1024 * 1024; // 5 MB per segment
        private volatile bool _disposed;

        public string CurrentLogFilePath => _currentFile;

        public FileLogService(Func<int?> getUserId, string? baseDir = null, string? sessionId = null)
        {
            _sessionId = sessionId ?? Guid.NewGuid().ToString("N");
            _getUserId = getUserId ?? (() => null);
            _baseDir = baseDir ?? Path.Combine(FileSystem.AppDataDirectory, "logs");
            Directory.CreateDirectory(_baseDir);
            _currentFile = ComputeLogPath();
        }

        public async Task InfoAsync(string source, string message, IDictionary<string, object>? context = null)
            => await WriteAsync("info", source, action: "Info", message, context);

        public async Task TraceAsync(string source, string action, string details, IDictionary<string, object>? context = null)
            => await WriteAsync("trace", source, action, details, context);

        public async Task ErrorAsync(string source, Exception ex, string? message = null, IDictionary<string, object>? context = null)
        {
            context ??= new Dictionary<string, object>();
            context["exception"] = new
            {
                type = ex.GetType().FullName,
                ex.Message,
                ex.StackTrace,
                inner = ex.InnerException?.ToString()
            };
            await WriteAsync("error", source, "Exception", message ?? ex.Message, context);
        }

        private async Task WriteAsync(string level, string source, string action, string message, IDictionary<string, object>? context)
        {
            if (_disposed) return;

            var row = new Dictionary<string, object?>
            {
                ["ts"] = DateTimeOffset.UtcNow.ToString("o"),
                ["level"] = level,
                ["src"] = source,
                ["action"] = action,
                ["msg"] = message,
                ["ctx"] = context,
                ["sessionId"] = _sessionId,
                ["userId"] = _getUserId()
            };

            string json = JsonSerializer.Serialize(row, new JsonSerializerOptions { WriteIndented = false });

            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                await RollIfNeeded_NoLock().ConfigureAwait(false);
                await File.AppendAllTextAsync(_currentFile, json + Environment.NewLine, Encoding.UTF8)
                          .ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }

        private string ComputeLogPath()
        {
            var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var stem = Path.Combine(_baseDir, $"{date}_{_sessionId}");
            var path = stem + ".log";
            if (!File.Exists(path)) return path;

            // Find next free segment if current exceeded
            for (int i = 1; i < 1000; i++)
            {
                var seg = $"{stem}.{i:D3}.log";
                if (!File.Exists(seg)) return seg;
                var fi = new FileInfo(seg);
                if (fi.Length < MaxBytes) return seg;
            }
            return stem + ".overflow.log";
        }

        private async Task RollIfNeeded_NoLock()
        {
            if (!File.Exists(_currentFile))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_currentFile)!);
                using var _ = File.Create(_currentFile);
                return;
            }

            var fi = new FileInfo(_currentFile);
            if (fi.Length >= MaxBytes || !fi.Name.StartsWith(DateTime.UtcNow.ToString("yyyy-MM-dd")))
            {
                _currentFile = ComputeLogPath();
                if (!File.Exists(_currentFile))
                {
                    using var _ = File.Create(_currentFile);
                }
            }
            await Task.CompletedTask;
        }

        public void Dispose()
        {
            _disposed = true;
            _gate.Dispose();
        }
    }
}
