using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Xunit;
using YasGMP.Diagnostics;
using YasGMP.Services;
using YasGMP.Services.Logging;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.Tests.TestDoubles;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Tests;

public sealed class DiagnosticsModuleViewModelTests : IDisposable
{
    private readonly string _logDirectory;

    public DiagnosticsModuleViewModelTests()
    {
        _logDirectory = Path.Combine(Path.GetTempPath(), "yasgmp-diag-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_logDirectory);
    }

    [Fact]
    public async Task InitializeAsync_WhenFeedPublishesEvents_UpdatesCollectionsAfterDispatcherCallback()
    {
        // Arrange
        var dispatcher = new RecordingDispatcher();
        using var feed = CreateFeedService(dispatcher, _logDirectory);
        var localization = CreateLocalization();
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var viewModel = new DiagnosticsModuleViewModel(
            database,
            audit,
            new StubCflDialogService(),
            new TestShellInteractionService(),
            new StubModuleNavigationService(),
            localization,
            feed);

        // Act
        await viewModel.InitializeAsync(null);

        var telemetryPayload = new Dictionary<string, object?>
        {
            ["ts_utc"] = "2026-07-30T10:15:30Z",
            ["corr_id"] = "corr-123",
            ["span_id"] = "span-456",
            ["parent_span"] = "parent-789",
            ["user_id"] = 42,
            ["username"] = "darko",
            ["roles"] = "Admin,Quality",
            ["ip"] = "127.0.0.1",
            ["session_id"] = "session-abc",
            ["device"] = "Surface",
            ["os"] = "Windows",
            ["app_version"] = "9.0.0",
            ["git_commit"] = "abcdef123456",
            ["diag_level"] = "Info",
            ["diag_enabled"] = true,
            ["extra"] = "value"
        };

        var healthPayload = new Dictionary<string, object?>
        {
            ["ts_utc"] = "2026-07-30T10:20:00Z",
            ["process_id"] = 4242,
            ["proc_start_utc"] = "2026-07-30T04:00:00Z",
            ["working_set_mb"] = 512,
            ["os"] = "Windows",
            ["framework"] = ".NET 9.0",
            ["assembly"] = "YasGMP.Wpf",
            ["assembly_ver"] = "9.0.0.0",
            ["other"] = "ignored"
        };

        InvokePublish(feed, "PublishTelemetry", telemetryPayload);
        InvokePublish(feed, "PublishLog", "10:21:00 [INF] Diagnostics ready");
        InvokePublish(feed, "PublishHealth", healthPayload);

        // Assert - dispatcher should hold queued work until executed
        Assert.Equal(3, dispatcher.BeginInvokeCount);
        Assert.Empty(viewModel.TelemetrySummaries);
        Assert.Empty(viewModel.LogEntries);
        Assert.Empty(viewModel.HealthResults);

        dispatcher.RunQueued();

        Assert.NotNull(viewModel.LatestTelemetrySnapshot);
        Assert.Equal("corr-123", viewModel.LatestTelemetrySnapshot!["corr_id"]);
        Assert.Equal("Module.Diagnostics.Status.Healthy", viewModel.HealthStatus);

        Assert.Collection(
            viewModel.TelemetrySummaries,
            item =>
            {
                Assert.Equal("Module.Diagnostics.Telemetry.ts_utc", item.Label);
                Assert.Equal("2026-07-30T10:15:30Z", item.Value);
            },
            item =>
            {
                Assert.Equal("Module.Diagnostics.Telemetry.corr_id", item.Label);
                Assert.Equal("corr-123", item.Value);
            },
            item =>
            {
                Assert.Equal("Module.Diagnostics.Telemetry.span_id", item.Label);
                Assert.Equal("span-456", item.Value);
            });

        Assert.Equal("10:21:00 [INF] Diagnostics ready", Assert.Single(viewModel.LogEntries));

        Assert.Collection(
            viewModel.HealthResults,
            item => Assert.Equal("Module.Diagnostics.Health.ts_utc", item.Label),
            item => Assert.Equal("Module.Diagnostics.Health.process_id", item.Label),
            item => Assert.Equal("Module.Diagnostics.Health.proc_start_utc", item.Label));

        var telemetryRecord = viewModel.Records.Single(r => r.Key == "DIAG-TELEMETRY");
        Assert.Equal("Module.Diagnostics.Status.Live", telemetryRecord.Status);

        var logRecord = viewModel.Records.Single(r => r.Key == "DIAG-LOG");
        Assert.Equal("Module.Diagnostics.Status.Streaming", logRecord.Status);

        var healthRecord = viewModel.Records.Single(r => r.Key == "DIAG-HEALTH");
        Assert.Equal("Module.Diagnostics.Status.Healthy", healthRecord.Status);
    }

    [Fact]
    public async Task InitializeAsync_WhenFeedUnavailable_UsesDesignFallbackCollections()
    {
        // Arrange
        var localization = CreateLocalization();
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var viewModel = new DiagnosticsModuleViewModel(
            database,
            audit,
            new StubCflDialogService(),
            new TestShellInteractionService(),
            new StubModuleNavigationService(),
            localization,
            diagnosticsFeedService: null);

        // Act
        await viewModel.InitializeAsync(null);

        // Assert
        Assert.NotNull(viewModel.LatestTelemetrySnapshot);
        Assert.NotEmpty(viewModel.TelemetrySummaries);
        Assert.NotEmpty(viewModel.LogEntries);
        Assert.NotEmpty(viewModel.HealthResults);
        Assert.Equal("Module.Diagnostics.Status.Healthy", viewModel.HealthStatus);
        Assert.Equal(3, viewModel.Records.Count);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_logDirectory))
            {
                Directory.Delete(_logDirectory, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup failures in test environment.
        }
    }

    private static DiagnosticsFeedService CreateFeedService(RecordingDispatcher dispatcher, string logDirectory)
    {
        var fileLog = new FileLogService(() => null, baseDir: logDirectory, sessionId: "test-session");
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var context = new DiagnosticContext(configuration)
        {
            UserId = 42,
            Username = "darko",
            RoleIdsCsv = "Admin",
            Ip = "127.0.0.1",
            SessionId = "session-abc"
        };

        return new DiagnosticsFeedService(
            fileLog,
            context,
            new NoopTrace(),
            dispatcher);
    }

    private static FakeLocalizationService CreateLocalization()
        => new(new Dictionary<string, IDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["neutral"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Module.Title.Diagnostics"] = "Diagnostics",
                ["Module.Diagnostics.Status.Unknown"] = "Module.Diagnostics.Status.Unknown",
                ["Module.Diagnostics.Status.Waiting"] = "Module.Diagnostics.Status.Waiting",
                ["Module.Diagnostics.Status.Live"] = "Module.Diagnostics.Status.Live",
                ["Module.Diagnostics.Status.Streaming"] = "Module.Diagnostics.Status.Streaming",
                ["Module.Diagnostics.Status.Healthy"] = "Module.Diagnostics.Status.Healthy",
                ["Module.Diagnostics.StatusMessage.Ready"] = "Module.Diagnostics.StatusMessage.Ready",
                ["Module.Diagnostics.StatusMessage.TelemetryUpdated"] = "Module.Diagnostics.StatusMessage.TelemetryUpdated",
                ["Module.Diagnostics.StatusMessage.HealthUpdated"] = "Module.Diagnostics.StatusMessage.HealthUpdated",
                ["Module.Diagnostics.Record.Telemetry.Title"] = "Telemetry",
                ["Module.Diagnostics.Record.Telemetry.Code"] = "TEL",
                ["Module.Diagnostics.Record.Telemetry.Description"] = "Telemetry summary",
                ["Module.Diagnostics.Record.Log.Title"] = "Log",
                ["Module.Diagnostics.Record.Log.Code"] = "LOG",
                ["Module.Diagnostics.Record.Log.Description"] = "Recent log entries",
                ["Module.Diagnostics.Record.Health.Title"] = "Health",
                ["Module.Diagnostics.Record.Health.Code"] = "HLT",
                ["Module.Diagnostics.Record.Health.Description"] = "Health snapshot",
                ["Module.Diagnostics.Telemetry.extra"] = "Module.Diagnostics.Telemetry.extra",
                ["Module.Diagnostics.Health.other"] = "Module.Diagnostics.Health.other"
            }
        }, "neutral");

    private static void InvokePublish(DiagnosticsFeedService service, string methodName, object payload)
    {
        var method = typeof(DiagnosticsFeedService).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(service, new[] { payload });
    }

    private sealed class RecordingDispatcher : IUiDispatcher
    {
        private readonly Queue<Action> _queue = new();

        public int BeginInvokeCount { get; private set; }

        public bool IsDispatchRequired => true;

        public void BeginInvoke(Action action)
        {
            BeginInvokeCount++;
            _queue.Enqueue(action);
        }

        public Task InvokeAsync(Action action)
        {
            BeginInvoke(action);
            RunQueued();
            return Task.CompletedTask;
        }

        public Task<T> InvokeAsync<T>(Func<T> func)
        {
            return Task.FromResult(func());
        }

        public Task InvokeAsync(Func<Task> asyncAction)
        {
            BeginInvoke(async () => asyncAction().GetAwaiter().GetResult());
            RunQueued();
            return Task.CompletedTask;
        }

        public void RunQueued()
        {
            while (_queue.Count > 0)
            {
                var next = _queue.Dequeue();
                next();
            }
        }
    }

    private sealed class NoopTrace : ITrace
    {
        public string CurrentCorrelationId => string.Empty;

        public string CurrentSpanId => string.Empty;

        public void Log(DiagLevel level, string category, string evt, string message, Exception? ex = null, IDictionary<string, object?>? data = null)
        {
        }

        public IDisposable StartSpan(string category, string name, IDictionary<string, object?>? data = null)
            => new DummyDisposable();

        private sealed class DummyDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
