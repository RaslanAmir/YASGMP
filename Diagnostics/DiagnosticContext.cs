using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Storage;

namespace YasGMP.Diagnostics
{
    /// <summary>
    /// Represents the Diagnostic Context.
    /// </summary>
    public sealed class DiagnosticContext
    {
        /// <summary>
        /// Initializes a new instance of the DiagnosticContext class.
        /// </summary>
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
        /// <summary>
        /// Gets or sets the config.
        /// </summary>

        public IConfiguration Config { get; }

        // Ambient fields
        /// <summary>
        /// Gets or sets the corr id.
        /// </summary>
        public string CorrId { get; private set; }
        /// <summary>
        /// Gets or sets the span id.
        /// </summary>
        public string SpanId { get; private set; }
        /// <summary>
        /// Gets or sets the parent span id.
        /// </summary>
        public string? ParentSpanId { get; private set; }
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        public int? UserId { get; set; }
        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        public string? Username { get; set; }
        /// <summary>
        /// Gets or sets the role ids csv.
        /// </summary>
        public string? RoleIdsCsv { get; set; }
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
        public string? Device { get; }
        /// <summary>
        /// Gets or sets the os version.
        /// </summary>
        public string? OsVersion { get; }
        /// <summary>
        /// Gets or sets the app version.
        /// </summary>
        public string? AppVersion { get; }
        /// <summary>
        /// Gets or sets the git commit.
        /// </summary>
        public string? GitCommit { get; }
        /// <summary>
        /// Gets or sets the db schema hash.
        /// </summary>
        public string? DbSchemaHash { get; set; }
        /// <summary>
        /// Executes the redaction enabled operation.
        /// </summary>

        public bool RedactionEnabled => GetBool(DiagnosticsConstants.KeyRedactionEnabled, defaultValue: true);

        // Derived settings
        /// <summary>
        /// Executes the enabled operation.
        /// </summary>
        public bool Enabled => GetBool(DiagnosticsConstants.KeyEnabled, defaultValue: true);
        /// <summary>
        /// Executes the sinks operation.
        /// </summary>
        public string[] Sinks => GetString(DiagnosticsConstants.KeySinks, "file,stdout").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        /// <summary>
        /// Executes the level operation.
        /// </summary>
        public DiagLevel Level => ParseLevel(GetString(DiagnosticsConstants.KeyLevel, "info"));
        /// <summary>
        /// Executes the slow query ms operation.
        /// </summary>
        public int SlowQueryMs => GetInt(DiagnosticsConstants.KeySlowQueryMs, DiagnosticsConstants.DefaultSlowQueryMs);
        /// <summary>
        /// Executes the rolling max mb operation.
        /// </summary>
        public int RollingMaxMb => GetInt(DiagnosticsConstants.KeyRollingMaxMb, DiagnosticsConstants.DefaultRollingMaxMb);
        /// <summary>
        /// Executes the rolling max days operation.
        /// </summary>
        public int RollingMaxDays => GetInt(DiagnosticsConstants.KeyRollingMaxDays, DiagnosticsConstants.DefaultRollingMaxDays);
        /// <summary>
        /// Executes the top n release operation.
        /// </summary>
        public int TopNRelease => GetInt(DiagnosticsConstants.KeyTopNRelease, DiagnosticsConstants.DefaultTopNRelease);
        /// <summary>
        /// Executes the random percent release operation.
        /// </summary>
        public double RandomPercentRelease => GetDouble(DiagnosticsConstants.KeyRandomPercentRelease, DiagnosticsConstants.DefaultRandomPercentRel);
        /// <summary>
        /// Executes the new correlation operation.
        /// </summary>

        public void NewCorrelation(string? corrId = null)
        {
            CorrId = string.IsNullOrWhiteSpace(corrId) ? Guid.NewGuid().ToString("N") : corrId!;
            ParentSpanId = null;
            SpanId = NewSpanId();
        }
        /// <summary>
        /// Executes the push span operation.
        /// </summary>

        public string PushSpan()
        {
            var parent = SpanId;
            ParentSpanId = parent;
            SpanId = NewSpanId();
            return parent;
        }
        /// <summary>
        /// Executes the pop span operation.
        /// </summary>

        public void PopSpan(string? parentSpan)
        {
            SpanId = NewSpanId();
            ParentSpanId = parentSpan;
        }
        /// <summary>
        /// Executes the new span id operation.
        /// </summary>

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
        /// <summary>
        /// Represents the arch value.
        /// </summary>
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

