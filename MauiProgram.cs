// MauiProgram.cs
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
using System.Collections.Generic;                  // IEnumerable<>
using YasGMP.Common;
using YasGMP.Data;
using YasGMP.Services;
using YasGMP.Services.Interfaces;                  // IRBACService
using YasGMP.Services.Platform;
using YasGMP.ViewModels;
using Microsoft.Maui.Storage;
using YasGMP.Diagnostics;
using YasGMP.Diagnostics.LogSinks;
using Microsoft.Extensions.Configuration;
using Syncfusion.Maui.Core.Hosting;                 // ConfigureSyncfusionCore()
using Syncfusion.Licensing;                         // SyncfusionLicenseProvider
using YasGMP.AppCore.DependencyInjection;

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
            ServiceLocator.RegisterFallback(() => Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services);

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                // Syncfusion core init (required by Toolkit and controls)
                .ConfigureSyncfusionCore()
                // NOTE: No ZXing.Net.Maui init needed; QR generation uses ZXing core + System.Drawing
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
            builder.Logging.AddProvider(new AppDataFileLoggerProvider());
#endif

            // Diagnostics wiring (lightweight, configurable via appsettings + env YAS_DIAG_*)
            var diagConfig = AppConfigurationHelper.LoadMerged();
            var diagCtx = new DiagnosticContext(diagConfig);
            var sinksList = DiagnosticsSinksFactory.CreateSinks(diagConfig, diagCtx).ToList();
            var logWriter = new LogWriter(
                DiagnosticsConstants.QueueCapacity,
                DiagnosticsConstants.QueueDrainBatch,
                DiagnosticsConstants.QueueDrainIntervalMs,
                sinksList);
            var traceMgr = new TraceManager(diagCtx, logWriter);
            traceMgr.Log(DiagLevel.Info, "diag", "diagnostics_boot", "Diagnostics initialized");
            var profiler = new Profiler(traceMgr);
            var crash = new CrashHandler(traceMgr, diagCtx);
            crash.RegisterGlobal();
            // Expose diagnostics components via DI
            builder.Services.AddSingleton(diagConfig);
            builder.Services.AddSingleton(diagCtx);
            builder.Services.AddSingleton<ILogWriter>(logWriter);
            builder.Services.AddSingleton<ITrace>(traceMgr);
            builder.Services.AddSingleton<IProfiler>(profiler);
            builder.Services.AddSingleton<IEnumerable<YasGMP.Diagnostics.ILogSink>>(sinksList);
            builder.Services.AddSingleton(new DiagnosticsHub(diagCtx, sinksList));

            var encryptionOptions = new AttachmentEncryptionOptions
            {
                KeyMaterial = Environment.GetEnvironmentVariable("YASGMP_ATTACHMENT_KEY")
                              ?? diagConfig["Attachments:Encryption:Key"],
                KeyId = Environment.GetEnvironmentVariable("YASGMP_ATTACHMENT_KEY_ID")
                         ?? diagConfig["Attachments:Encryption:KeyId"]
                         ?? "default"
            };

            var chunkEnv = Environment.GetEnvironmentVariable("YASGMP_ATTACHMENT_CHUNK_SIZE");
            if (!string.IsNullOrWhiteSpace(chunkEnv) && int.TryParse(chunkEnv, out var chunkSizeEnv) && chunkSizeEnv > 0)
            {
                encryptionOptions.ChunkSize = chunkSizeEnv;
            }
            else if (int.TryParse(diagConfig["Attachments:Encryption:ChunkSize"], out var chunkSizeCfg) && chunkSizeCfg > 0)
            {
                encryptionOptions.ChunkSize = chunkSizeCfg;
            }

            builder.Services.AddSingleton(encryptionOptions);

            // Optional: register Syncfusion license if provided (prevents trial watermark)
            try
            {
                // Priority: env var → appsettings.json (AppData/bin) → none
                var sfKey = Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY")
                           ?? Environment.GetEnvironmentVariable("YASGMP_SYNCFUSION_LICENSE")
                           ?? diagConfig["Syncfusion:LicenseKey"];
                if (!string.IsNullOrWhiteSpace(sfKey))
                {
                    SyncfusionLicenseProvider.RegisterLicense(sfKey);
#if DEBUG
                    AppDataFileLoggerProvider.WriteFrameworkLine("SYNCFUSION", "info", "License key registered from configuration.");
#endif
                }
                else
                {
#if DEBUG
                    AppDataFileLoggerProvider.WriteFrameworkLine("SYNCFUSION", "warn", "No Syncfusion license key found; controls may show trial watermark.");
#endif
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                AppDataFileLoggerProvider.WriteFrameworkLine("SYNCFUSION", "error", ex.ToString());
#endif
            }

            // Resolve connection string (env → bin\AppSettings.json → AppData\AppSettings.json → fallback)
            string mysqlConnStr = ResolveMySqlConnString();

            builder.Services.AddYasGmpCoreServices(core =>
            {
                core.UseConnectionString(mysqlConnStr);
                core.ConfigureDbContext((sp, options, connection) =>
                {
                    var ctx = sp.GetService<DiagnosticContext>();
                    var tr = sp.GetService<ITrace>();
                    if (ctx != null && tr != null)
                    {
                        options.AddInterceptors(new EfSqlInterceptor(ctx, tr));
                    }
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

                core.UseDatabaseService<DatabaseService>((sp, connection) => new DatabaseService(connection), (sp, db, connection) =>
                {
                    var ctx = sp.GetService<DiagnosticContext>();
                    var tr = sp.GetService<ITrace>();
                    var cfgRoot = sp.GetService<IConfiguration>();
                    if (ctx != null && tr != null)
                    {
                        db.SetDiagnostics(ctx, tr);
                    }

                    DatabaseService.GlobalDiagnosticContext = ctx;
                    DatabaseService.GlobalTrace = tr;
                    if (cfgRoot != null)
                    {
                        DatabaseService.GlobalConfiguration = cfgRoot;
                    }
                });

                var services = core.Services;

                // Core Services
                services.AddSingleton<IPlatformService, MauiPlatformService>();
                services.AddSingleton<IUiDispatcher, MauiUiDispatcher>();
                services.AddSingleton<IDialogService, MauiDialogService>();
                services.AddSingleton<IFilePicker, MauiFilePicker>();
                services.AddSingleton<IUserSession, MauiUserSession>();
                services.AddSingleton<AuditService>();
                services.AddSingleton<ExportService>();
                services.AddSingleton<WorkOrderAuditService>();
                services.AddSingleton<IAttachmentService, AttachmentService>();
                services.AddSingleton<QRCodeService>();     // QR generation
                services.AddSingleton<BackgroundScheduler>(); // in-app scheduler (PPM/alerts)
                services.AddSingleton<CodeGeneratorService>(); // NEW

                // RBAC + Users (registered before AuthService since AuthService depends on UserService)
                services.AddSingleton<IRBACService, RBACService>();
                services.AddSingleton<UserService>();
                services.AddSingleton<AuthService>();
                services.AddSingleton<IAuthContext>(sp => sp.GetRequiredService<AuthService>());

                // ViewModels
                services.AddTransient<LoginViewModel>();
                services.AddTransient<CapaViewModel>();
                services.AddTransient<WorkOrderEditDialogViewModel>();
                services.AddTransient<UserEditDialogViewModel>();
                services.AddTransient<WorkOrderViewModel>();
                services.AddTransient<AuditLogViewModel>();
                services.AddTransient<AdminViewModel>();
                services.AddTransient<UserViewModel>();
                services.AddTransient<UserRolePermissionViewModel>();
                services.AddTransient<MachineViewModel>(); // NEW
                services.AddTransient<CalibrationsViewModel>();
                services.AddTransient<PpmViewModel>();
                services.AddTransient<WarehouseViewModel>();
                services.AddTransient<DocumentControlViewModel>();

                // Pages
                services.AddTransient<YasGMP.Views.LoginPage>();
                services.AddTransient<YasGMP.Views.MainPage>();
                services.AddTransient<YasGMP.Views.AuditLogPage>();
                services.AddTransient<YasGMP.Views.AdminPanelPage>();
                services.AddTransient<YasGMP.Views.UsersPage>();
                services.AddTransient<YasGMP.Views.UserRolePermissionPage>();
                services.AddTransient<YasGMP.Views.CalibrationsPage>();
                services.AddTransient<YasGMP.Views.MachinesPage>();
                services.AddTransient<YasGMP.Views.PartsPage>();
                services.AddTransient<YasGMP.Views.WarehousePage>();
                services.AddTransient<YasGMP.Views.WorkOrdersPage>();
                services.AddTransient<YasGMP.Views.ComponentsPage>();
                services.AddTransient<YasGMP.Views.SuppliersPage>();
                services.AddTransient<YasGMP.Views.DocumentControlPage>();
                services.AddTransient<YasGMP.ExternalServicersPage>();
                services.AddTransient<YasGMP.Pages.PpmPage>();
                services.AddTransient<YasGMP.Views.ValidationPage>();
                // Debug pages (RBAC-gated in page code-behind)
                services.AddTransient<YasGMP.Views.Debug.DebugDashboardPage>();
                services.AddTransient<YasGMP.Views.Debug.LogViewerPage>();
                services.AddTransient<YasGMP.Views.Debug.HealthPage>();
                services.AddSingleton<YasGMP.Diagnostics.SelfTestRunner>();
            });

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

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetService<YasGmpDbContext>();
                if (db != null)
                {
                    db.Database.Migrate();
                    AttachmentSeedData.EnsureSeeded(db);
                }
            }

            ServiceLocator.Initialize(app.Services);
            return app;
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

    internal static class AppConfigurationHelper
    {
        internal static Microsoft.Extensions.Configuration.IConfiguration LoadMerged()
        {
            var builder = new Microsoft.Extensions.Configuration.ConfigurationBuilder();
            try
            {
                var appData = Microsoft.Maui.Storage.FileSystem.AppDataDirectory;
                var path = System.IO.Path.Combine(appData, "appsettings.json");
                if (System.IO.File.Exists(path))
                    builder.AddJsonFile(path, optional: true, reloadOnChange: true);

                var exePath = System.IO.Path.Combine(System.AppContext.BaseDirectory, "appsettings.json");
                if (System.IO.File.Exists(exePath))
                    builder.AddJsonFile(exePath, optional: true, reloadOnChange: true);
            }
            catch { }
            return builder.Build();
        }
    }

    internal static class DiagnosticsSinksFactory
    {
        internal static System.Collections.Generic.IEnumerable<YasGMP.Diagnostics.ILogSink> CreateSinks(
            Microsoft.Extensions.Configuration.IConfiguration cfg,
            YasGMP.Diagnostics.DiagnosticContext ctx)
        {
            var names = (cfg[DiagnosticsConstants.KeySinks] ?? "file,stdout")
                .Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries);
            foreach (var n in names)
            {
                switch (n.ToLowerInvariant())
                {
                    case "file": yield return new FileLogSink(ctx); break;
                    case "stdout": yield return new StdoutLogSink(); break;
                    case "sqlite": yield return new SQLiteLogSink(); break;
                    case "elastic": yield return new ElasticCompatibleSink(cfg); break;
                }
            }
        }
    }

#if DEBUG
    /// <summary>
    /// Minimal logger provider that mirrors framework/EF logs to AppData/logs as JSON lines (DEBUG only).
    /// </summary>
    internal sealed class AppDataFileLoggerProvider : ILoggerProvider
    {
        private readonly object _sync = new();

        /// <summary>
        /// Creates a logger that mirrors framework diagnostics to AppData JSON logs.
        /// </summary>
        /// <param name="categoryName">The logging category requested by the framework.</param>
        /// <returns>A logger instance that writes to the AppData log directory.</returns>
        public ILogger CreateLogger(string categoryName) => new AppDataFileLogger(categoryName, _sync);

        /// <summary>
        /// Releases resources held by the provider. No-op because only static state is used.
        /// </summary>
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
    /// Uses explicit interface implementations to avoid generic constraint warnings.
    /// </summary>
    internal sealed class AppDataFileLogger : ILogger
    {
        private sealed class NullScope : IDisposable
        {
            /// <summary>Singleton scope instance used when logging does not require additional state.</summary>
            public static readonly NullScope Instance = new();

            /// <summary>Disposes the scope instance (no-op).</summary>
            public void Dispose() { }
        }

        private readonly string _category;
        private readonly object _sync;
        /// <summary>
        /// Initializes a new instance of the AppDataFileLogger class.
        /// </summary>

        public AppDataFileLogger(string category, object sync)
        {
            _category = category;
            _sync = sync;
        }

        /// <summary>
        /// Indicates whether logging is enabled for the specified <paramref name="logLevel"/>. Always true for AppData logging.
        /// </summary>
        /// <param name="logLevel">The level being queried.</param>
        /// <returns>Always <see langword="true"/>.</returns>
        public bool IsEnabled(LogLevel logLevel) => true;

        IDisposable ILogger.BeginScope<TState>(TState state) => NullScope.Instance;

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
