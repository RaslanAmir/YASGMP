using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;

namespace YasGMP.Diagnostics
{
    public sealed class CrashHandler
    {
        private readonly ITrace _trace;
        private readonly DiagnosticContext _ctx;

        public CrashHandler(ITrace trace, DiagnosticContext ctx)
        {
            _trace = trace; _ctx = ctx;
        }

        public void RegisterGlobal()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception ?? new Exception("Unknown fatal");
                try { CreateCrashBundle(ex, reason: "UnhandledException"); } catch { }
            };
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                try { e.SetObserved(); } catch { }
                try { CreateCrashBundle(e.Exception ?? new Exception("Unobserved task"), reason: "UnobservedTaskException"); } catch { }
            };
        }

        private void CreateCrashBundle(Exception ex, string reason)
        {
            try
            {
                var dir = Path.Combine(DiagnosticsPathProvider.GetLogsDirectory(), "crash");
                DiagnosticsPathProvider.EnsureDirectory(dir);
                var zipName = $"{DateTime.UtcNow:yyyyMMdd_HHmm}_{_ctx.CorrId}.zip";
                var zipPath = Path.Combine(dir, zipName);
                using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);

                // crash.json
                var crash = new Dictionary<string, object?>
                {
                    ["ts_utc"] = DateTime.UtcNow.ToString("o"),
                    ["reason"] = reason,
                    ["message"] = ex.Message,
                    ["type"] = ex.GetType().FullName,
                    ["stack"] = ex.ToString(),
                    ["corr_id"] = _ctx.CorrId,
                    ["span_id"] = _ctx.SpanId,
                    ["user_id"] = _ctx.UserId,
                    ["username"] = _ctx.Username
                };
                AddJson(zip, "crash.json", crash);

                // breadcrumbs.json (best-effort: last day diag logs)
                TryAddBreadcrumbs(zip);

                // recent_sql.json (best-effort from slow query registry)
                var sql = YasGMP.Services.Database.DbSlowQueryRegistry.TryGetSnapshot();
                if (sql is not null) AddJson(zip, "recent_sql.json", sql);

                // health.json
                var health = HealthReport.BuildBasic();
                AddJson(zip, "health.json", health);

                // appsettings snapshot (from AppData if present)
                try
                {
                    var appCfg = Path.Combine(DiagnosticsPathProvider.GetAppDataDirectory(), "appsettings.json");
                    if (File.Exists(appCfg))
                    {
                        var entry = zip.CreateEntry("appsettings_snapshot.json");
                        using var zs = entry.Open();
                        using var fs = File.OpenRead(appCfg);
                        fs.CopyTo(zs);
                    }
                }
                catch { }

                _trace.Log(DiagLevel.Error, "crash", "bundle_created", zipName, ex);
            }
            catch (Exception bundleEx)
            {
                _trace.Log(DiagLevel.Error, "crash", "bundle_failed", bundleEx.Message, bundleEx);
            }
        }

        private static void AddJson(ZipArchive zip, string name, object payload)
        {
            var entry = zip.CreateEntry(name);
            using var s = entry.Open();
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            using var w = new StreamWriter(s);
            w.Write(json);
        }

        private void TryAddBreadcrumbs(ZipArchive zip)
        {
            try
            {
                var dir = DiagnosticsPathProvider.GetLogsDirectory();
                var file = Path.Combine(dir, $"{DateTime.UtcNow:yyyy-MM-dd}_diag.log");
                if (!File.Exists(file)) return;
                var entry = zip.CreateEntry("breadcrumbs.json");
                using var zs = entry.Open();
                using var fs = File.OpenRead(file);
                fs.CopyTo(zs);
            }
            catch { }
        }
    }
}

