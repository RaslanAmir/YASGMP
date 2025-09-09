using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Storage;

namespace YasGMP.Diagnostics
{
    public sealed class DiagnosticContext
    {
        public DiagnosticContext(IConfiguration config)
        {
            Config = config;
            CorrId = Guid.NewGuid().ToString("N");
            SpanId = NewSpanId();
            AppVersion = GetAppVersion();
            OsVersion  = GetOsVersion();
            Device     = GetDeviceInfoSafe();
            GitCommit  = TryReadGitCommit();
        }

        public IConfiguration Config { get; }

        // Ambient fields
        public string CorrId { get; private set; }
        public string SpanId { get; private set; }
        public string? ParentSpanId { get; private set; }
        public int? UserId { get; set; }
        public string? Username { get; set; }
        public string? RoleIdsCsv { get; set; }
        public string? Ip { get; set; }
        public string? SessionId { get; set; }
        public string? Device { get; }
        public string? OsVersion { get; }
        public string? AppVersion { get; }
        public string? GitCommit { get; }
        public string? DbSchemaHash { get; set; }

        public bool RedactionEnabled => GetBool(DiagnosticsConstants.KeyRedactionEnabled, defaultValue: true);

        // Derived settings
        public bool Enabled => GetBool(DiagnosticsConstants.KeyEnabled, defaultValue: true);
        public string[] Sinks => GetString(DiagnosticsConstants.KeySinks, "file,stdout").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        public DiagLevel Level => ParseLevel(GetString(DiagnosticsConstants.KeyLevel, "info"));
        public int SlowQueryMs => GetInt(DiagnosticsConstants.KeySlowQueryMs, DiagnosticsConstants.DefaultSlowQueryMs);
        public int RollingMaxMb => GetInt(DiagnosticsConstants.KeyRollingMaxMb, DiagnosticsConstants.DefaultRollingMaxMb);
        public int RollingMaxDays => GetInt(DiagnosticsConstants.KeyRollingMaxDays, DiagnosticsConstants.DefaultRollingMaxDays);
        public int TopNRelease => GetInt(DiagnosticsConstants.KeyTopNRelease, DiagnosticsConstants.DefaultTopNRelease);
        public double RandomPercentRelease => GetDouble(DiagnosticsConstants.KeyRandomPercentRelease, DiagnosticsConstants.DefaultRandomPercentRel);

        public void NewCorrelation(string? corrId = null)
        {
            CorrId = string.IsNullOrWhiteSpace(corrId) ? Guid.NewGuid().ToString("N") : corrId!;
            ParentSpanId = null;
            SpanId = NewSpanId();
        }

        public string PushSpan()
        {
            var parent = SpanId;
            ParentSpanId = parent;
            SpanId = NewSpanId();
            return parent;
        }

        public void PopSpan(string? parentSpan)
        {
            SpanId = NewSpanId();
            ParentSpanId = parentSpan;
        }

        public static string NewSpanId() => Guid.NewGuid().ToString("N").Substring(0, 16);

        private bool GetBool(string key, bool defaultValue)
        {
            var env = Environment.GetEnvironmentVariable(ToEnv(key));
            if (!string.IsNullOrWhiteSpace(env) && bool.TryParse(env, out var b)) return b;
            var val = Config[key];
            if (!string.IsNullOrWhiteSpace(val) && bool.TryParse(val, out var cb)) return cb;
            return defaultValue;
        }

        private int GetInt(string key, int defaultValue)
        {
            var env = Environment.GetEnvironmentVariable(ToEnv(key));
            if (!string.IsNullOrWhiteSpace(env) && int.TryParse(env, out var v)) return v;
            var val = Config[key];
            if (!string.IsNullOrWhiteSpace(val) && int.TryParse(val, out var cv)) return cv;
            return defaultValue;
        }

        private double GetDouble(string key, double defaultValue)
        {
            var env = Environment.GetEnvironmentVariable(ToEnv(key));
            if (!string.IsNullOrWhiteSpace(env) && double.TryParse(env, out var v)) return v;
            var val = Config[key];
            if (!string.IsNullOrWhiteSpace(val) && double.TryParse(val, out var cv)) return cv;
            return defaultValue;
        }

        private string GetString(string key, string defaultValue)
        {
            var env = Environment.GetEnvironmentVariable(ToEnv(key));
            if (!string.IsNullOrWhiteSpace(env)) return env!;
            var val = Config[key];
            return string.IsNullOrWhiteSpace(val) ? defaultValue : val!;
        }

        private static string ToEnv(string key)
        {
            // Diagnostics:SlowQueryMs -> YAS_DIAG_SLOWQUERYMS
            var core = key.Replace("Diagnostics:", string.Empty).Replace(":", string.Empty);
            return DiagnosticsConstants.EnvPrefix + new string(core.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        }

        private static string? GetAppVersion()
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly().GetName();
                return asm.Version?.ToString();
            }
            catch { return null; }
        }

        private static string? GetOsVersion()
        {
            try { return Environment.OSVersion?.VersionString; }
            catch { return null; }
        }

        private static string? GetDeviceInfoSafe()
        {
            try { return $"Host={Environment.MachineName}; Arch={RuntimeInformationShim.Arch}"; }
            catch { return null; }
        }

        private static string? TryReadGitCommit()
        {
            try
            {
                var baseDir = AppContext.BaseDirectory;
                var gitHead = Path.Combine(baseDir, ".git", "HEAD");
                if (File.Exists(gitHead))
                {
                    var headRef = File.ReadAllText(gitHead).Trim();
                    var parts = headRef.Split(' ');
                    var refPath = parts.Length == 2 ? parts[1] : null;
                    if (!string.IsNullOrWhiteSpace(refPath))
                    {
                        var refFile = Path.Combine(baseDir, ".git", refPath.Replace('/', Path.DirectorySeparatorChar));
                        if (File.Exists(refFile)) return File.ReadAllText(refFile).Trim();
                    }
                }
            }
            catch { }
            return null;
        }

        private static DiagLevel ParseLevel(string s)
        {
            return s?.ToLowerInvariant() switch
            {
                "trace" => DiagLevel.Trace,
                "debug" => DiagLevel.Debug,
                "warn"  => DiagLevel.Warn,
                "error" => DiagLevel.Error,
                "fatal" => DiagLevel.Fatal,
                _ => DiagLevel.Info,
            };
        }
    }

    internal static class RuntimeInformationShim
    {
        public static string Arch
        {
            get
            {
                try { return System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(); }
                catch { return "Unknown"; }
            }
        }
    }
}

