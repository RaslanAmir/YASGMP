using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Maui.Storage;

namespace YasGMP.Diagnostics.LogSinks
{
    internal sealed class FileLogSink : ILogSink
    {
        private readonly DiagnosticContext _ctx;
        private readonly object _sync = new();

        public FileLogSink(DiagnosticContext ctx)
        {
            _ctx = ctx;
        }

        public string Name => "file";

        public void WriteBatch(IReadOnlyList<DiagnosticEvent> batch)
        {
            if (batch == null || batch.Count == 0) return;
            try
            {
                var lines = string.Join(Environment.NewLine, batch.Select(e => e.ToJson())) + Environment.NewLine;

                // Primary: AppDataDirectory
                var dir1 = Path.Combine(FileSystem.AppDataDirectory, "logs");
                Directory.CreateDirectory(dir1);
                var file1 = Path.Combine(dir1, $"{DateTime.UtcNow:yyyy-MM-dd}_diag.log");

                // Convenience duplicate: App base directory
                var dir2 = Path.Combine(AppContext.BaseDirectory, "logs");
                try { Directory.CreateDirectory(dir2); } catch { }
                var file2 = Path.Combine(dir2, $"{DateTime.UtcNow:yyyy-MM-dd}_diag.log");

                lock (_sync)
                {
                    File.AppendAllText(file1, lines);
                    try { File.AppendAllText(file2, lines); } catch { }
                }
                TryRotate(dir1);
            }
            catch { }
        }

        private void TryRotate(string dir)
        {
            try
            {
                var maxBytes = _ctx.RollingMaxMb * 1024L * 1024L;
                var today = Path.Combine(dir, $"{DateTime.UtcNow:yyyy-MM-dd}_diag.log");
                if (File.Exists(today))
                {
                    var len = new FileInfo(today).Length;
                    if (len > maxBytes)
                    {
                        var rotated = Path.Combine(dir, $"{DateTime.UtcNow:yyyy-MM-dd_HHmmss}_diag.log");
                        try { File.Move(today, rotated, overwrite: false); } catch { }
                    }
                }

                // cleanup old files
                var cutoff = DateTime.UtcNow.Date.AddDays(-_ctx.RollingMaxDays);
                foreach (var f in Directory.EnumerateFiles(dir, "*_diag.log"))
                {
                    try { if (File.GetCreationTimeUtc(f) < cutoff) File.Delete(f); } catch { }
                }
            }
            catch { }
        }
    }
}
