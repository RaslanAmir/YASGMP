using CommunityToolkit.Maui;                       // builder.UseMauiCommunityToolkit()
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;    // DI registration
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure; // ServerVersion.AutoDetect
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YasGMP.Data;
using YasGMP.Services;
using YasGMP.Services.Interfaces;                  // IRBACService
using YasGMP.ViewModels;
using Microsoft.Maui.Storage;

namespace YasGMP
{
    /// <summary>
    /// Application bootstrap for YasGMP.
    /// Registers EF Core DbContext, services, view-models and pages into DI,
    /// configures fonts and logging, and wires global exception hooks.
    /// Also mirrors basic framework logs to an AppData JSONL file (DEBUG).
    /// </summary>
    public static class MauiProgram
    {
        /// <summary>
        /// Creates and configures the MAUI app with DI, EF Core, fonts and logging.
        /// </summary>
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
            builder.Logging.AddProvider(new AppDataFileLoggerProvider());
#endif

            // Resolve connection string (env → bin\AppSettings.json → AppData\AppSettings.json → fallback)
            string mysqlConnStr = ResolveMySqlConnString();

            // DbContext (MySQL) + optional EF SQL log to AppData (DEBUG)
            builder.Services.AddDbContext<YasGmpDbContext>((sp, options) =>
            {
                options.UseMySql(mysqlConnStr, ServerVersion.AutoDetect(mysqlConnStr));
#if DEBUG
                var provider = sp.GetService<ILoggerFactory>();
                options.LogTo(sql =>
                {
                    try
                    {
                        var logger = provider?.CreateLogger("EFCore.SQL");
                        logger?.LogInformation("{Sql}", sql);
                    }
                    catch { /* never throw from logging */ }
                });
                options.EnableSensitiveDataLogging(true);
#endif
            });

            // Core Services
            builder.Services.AddSingleton(sp => new DatabaseService(mysqlConnStr));
            builder.Services.AddSingleton<AuditService>();
            builder.Services.AddSingleton<AuthService>();
            builder.Services.AddSingleton<ExportService>();
            builder.Services.AddSingleton<WorkOrderAuditService>();

            // RBAC + Users
            builder.Services.AddSingleton<IRBACService, RBACService>();
            builder.Services.AddSingleton<UserService>();

            // ViewModels
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<CapaViewModel>();
            builder.Services.AddTransient<WorkOrderEditDialogViewModel>();
            builder.Services.AddTransient<WorkOrderViewModel>();
            builder.Services.AddTransient<AuditLogViewModel>();
            builder.Services.AddTransient<AdminViewModel>();

            // Pages
            builder.Services.AddTransient<YasGMP.Views.LoginPage>();
            builder.Services.AddTransient<YasGMP.Views.MainPage>();
            builder.Services.AddTransient<YasGMP.Views.AuditLogPage>();
            builder.Services.AddTransient<YasGMP.Views.AdminPanelPage>();

            // Global exception hooks → JSONL framework log (DEBUG)
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
#if DEBUG
                AppDataFileLoggerProvider.WriteFrameworkLine("GLOBAL", "error", ex?.ToString() ?? "Unknown fatal");
#endif
            };
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
#if DEBUG
                AppDataFileLoggerProvider.WriteFrameworkLine("TASK", "error", e.Exception?.ToString() ?? "Unknown task error");
#endif
                try { e.SetObserved(); } catch { }
            };

#if DEBUG
            AppDataFileLoggerProvider.WriteFrameworkLine("CFG", "info", $"Using MySQL from: {LastResolvedSource}");
            AppDataFileLoggerProvider.WriteFrameworkLine("CFG", "info", $"ConnString (redacted): {Redact(mysqlConnStr)}");
#endif

            return builder.Build();
        }

        /// <summary>
        /// Resolves the MySQL connection string from environment/AppSettings.json with safe fallbacks.
        /// </summary>
        private static string ResolveMySqlConnString()
        {
            // 1) Environment overrides
            var env = Environment.GetEnvironmentVariable("YASGMP_ConnectionString")
                      ?? Environment.GetEnvironmentVariable("YASGMP_MYSQL");
            if (!string.IsNullOrWhiteSpace(env))
            {
                LastResolvedSource = "Environment";
                return env!;
            }

            // 2) Try bin directory (AppContext.BaseDirectory)
            var fromBin = TryReadMySqlFromJson(Path.Combine(AppContext.BaseDirectory, "AppSettings.json"));
            if (!string.IsNullOrWhiteSpace(fromBin))
            {
                LastResolvedSource = $"AppSettings.json @ {AppContext.BaseDirectory}";
                TrySeedAppDataFrom(Path.Combine(AppContext.BaseDirectory, "AppSettings.json"));
                return fromBin!;
            }

            // 3) Try MAUI AppData folder
            var appDataPath = Path.Combine(FileSystem.AppDataDirectory, "AppSettings.json");
            var fromAppData = TryReadMySqlFromJson(appDataPath);
            if (!string.IsNullOrWhiteSpace(fromAppData))
            {
                LastResolvedSource = $"AppSettings.json @ {FileSystem.AppDataDirectory}";
                return fromAppData!;
            }

            // 4) Final fallback
            LastResolvedSource = "Hard-coded fallback";
            return "Server=127.0.0.1;Port=3306;Database=YASGMP;User ID=yasgmp_app;Password=Jasenka1;Character Set=utf8mb4;Connection Timeout=5;Default Command Timeout=30;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=50;";
        }

        /// <summary>Reads ConnectionStrings:MySqlDb from a JSON file, if present.</summary>
        private static string? TryReadMySqlFromJson(string jsonPath)
        {
            try
            {
                if (!File.Exists(jsonPath)) return null;
                using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(jsonPath));
                if (doc.RootElement.TryGetProperty("ConnectionStrings", out var cs) &&
                    cs.TryGetProperty("MySqlDb", out var v) &&
                    v.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    var s = v.GetString();
                    return string.IsNullOrWhiteSpace(s) ? null : s;
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Seeds AppData\AppSettings.json from bin on first run to simplify admin edits.
        /// </summary>
        private static void TrySeedAppDataFrom(string sourceJson)
        {
            try
            {
                if (!File.Exists(sourceJson)) return;
                var dest = Path.Combine(FileSystem.AppDataDirectory, "AppSettings.json");
                Directory.CreateDirectory(FileSystem.AppDataDirectory);
                if (!File.Exists(dest))
                {
                    File.Copy(sourceJson, dest);
                }
            }
            catch { }
        }

        private static string LastResolvedSource { get; set; } = "Unknown";

        /// <summary>Redacts sensitive parts of a connection string for safe logging.</summary>
        private static string Redact(string conn)
        {
            if (string.IsNullOrEmpty(conn)) return string.Empty;
            string[] secretKeys = { "Password", "Pwd", "User Id", "UserID", "Uid" };
            var parts = conn.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                var kv = parts[i].Split('=', 2, StringSplitOptions.TrimEntries);
                if (kv.Length == 2 && secretKeys.Any(k => kv[0].Equals(k, StringComparison.OrdinalIgnoreCase)))
                {
                    parts[i] = $"{kv[0]}=****";
                }
            }
            return string.Join(';', parts);
        }
    }

#if DEBUG
    /// <summary>
    /// Minimal logger provider that mirrors framework/EF logs to AppData/logs as JSON lines (DEBUG only).
    /// </summary>
    internal sealed class AppDataFileLoggerProvider : ILoggerProvider
    {
        private readonly object _sync = new();

        /// <inheritdoc />
        public ILogger CreateLogger(string categoryName) => new AppDataFileLogger(categoryName, _sync);

        /// <inheritdoc />
        public void Dispose() { }

        internal static void WriteFrameworkLine(string category, string level, string message)
        {
            try
            {
                var dir = Path.Combine(FileSystem.AppDataDirectory, "logs");
                Directory.CreateDirectory(dir);
                var file = Path.Combine(dir, $"{DateTime.UtcNow:yyyy-MM-dd}_framework.log");
                var json = System.Text.Json.JsonSerializer.Serialize(new
                {
                    ts = DateTimeOffset.UtcNow.ToString("o"),
                    level,
                    src = "MS.Extensions.Logging",
                    category,
                    msg = message
                });
                File.AppendAllText(file, json + Environment.NewLine);
            }
            catch { }
        }
    }

    /// <summary>
    /// File-backed logger writing JSON lines to AppData (DEBUG only).
    /// Uses explicit interface implementations (no generic constraints here),
    /// so it matches <see cref="ILogger"/> across packages and avoids CS0460/CS8633.
    /// </summary>
    internal sealed class AppDataFileLogger : ILogger
    {
        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }

        private readonly string _category;
        private readonly object _sync;

        public AppDataFileLogger(string category, object sync)
        {
            _category = category;
            _sync = sync;
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel) => true;

        /// <inheritdoc />
        IDisposable ILogger.BeginScope<TState>(TState state)
            => NullScope.Instance; // constraints inherited from interface; do not repeat

        /// <inheritdoc />
        void ILogger.Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            try
            {
                var dir = Path.Combine(FileSystem.AppDataDirectory, "logs");
                Directory.CreateDirectory(dir);
                var file = Path.Combine(dir, $"{DateTime.UtcNow:yyyy-MM-dd}_framework.log");

                var json = System.Text.Json.JsonSerializer.Serialize(new
                {
                    ts = DateTimeOffset.UtcNow.ToString("o"),
                    level = logLevel.ToString().ToLowerInvariant(),
                    src = "MS.Extensions.Logging",
                    category = _category,
                    msg = formatter(state, exception),
                    exception = exception?.ToString()
                });

                lock (_sync)
                {
                    File.AppendAllText(file, json + Environment.NewLine);
                }
            }
            catch { }
        }
    }
#endif
}
