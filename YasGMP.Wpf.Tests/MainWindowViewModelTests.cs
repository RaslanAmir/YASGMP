using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;
using YasGMP.Diagnostics;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Services.Logging;
using YasGMP.Services.Ui;
using YasGMP.ViewModels;
using YasGMP.Wpf.Configuration;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.Tests.TestDoubles;
using YasGMP.Wpf.ViewModels;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public async Task CloseDiagnosticsDocument_DisposesFeedSubscriptions()
    {
        // Arrange
        var localization = new FakeLocalizationService(CreateLocalizationResources(), "neutral");
        var dispatcher = new RecordingDispatcher();
        using var feed = new TrackingDiagnosticsFeedService(dispatcher);
        var shellInteraction = new ShellInteractionService();
        var moduleRegistry = new StubModuleRegistry(shellInteraction, localization, feed);
        var notificationsPane = new NotificationsPaneViewModel(new StubNotificationAnalyticsViewModel(), new StubExportService(), localization);
        var inspectorPane = new InspectorPaneViewModel(localization);
        var statusBar = new ShellStatusBarViewModel(
            TimeProvider.System,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build(),
            DatabaseOptions.FromConnectionString("Server=stub;Database=stub;Uid=stub;Pwd=stub;"),
            new StubHostEnvironment(),
            new StubUserSession(),
            localization,
            new StubSignalRClientService());
        var smokeTestService = new DebugSmokeTestService(new StubUserSession(), new TestAuthContext(), new DatabaseService("Server=stub;Database=stub;Uid=stub;Pwd=stub;"), shellInteraction, moduleRegistry);
        var alertService = new StubShellAlertService();
        var modulesPane = new ModulesPaneViewModel(moduleRegistry, shellInteraction, localization);

        var viewModel = new MainWindowViewModel(
            moduleRegistry,
            modulesPane,
            notificationsPane,
            inspectorPane,
            statusBar,
            shellInteraction,
            smokeTestService,
            localization,
            alertService);

        // Act
        var document = viewModel.OpenModule(DiagnosticsModuleViewModel.ModuleKey);
        Assert.IsType<DiagnosticsModuleViewModel>(document);
        await ((DiagnosticsModuleViewModel)document).InitializeAsync(null);

        shellInteraction.CloseDocument(document);

        // Assert
        Assert.DoesNotContain(document, viewModel.Documents);
        Assert.Equal(3, feed.Subscriptions.Count);
        Assert.All(feed.Subscriptions, disposable => Assert.True(disposable.IsDisposed));
    }

    private static IDictionary<string, IDictionary<string, string>> CreateLocalizationResources()
        => new Dictionary<string, IDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["neutral"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Module.Title.Diagnostics"] = "Diagnostics",
                ["Module.Diagnostics.Status.Unknown"] = "Status.Unknown",
                ["Module.Diagnostics.Status.Waiting"] = "Status.Waiting",
                ["Module.Diagnostics.Status.Live"] = "Status.Live",
                ["Module.Diagnostics.Status.Streaming"] = "Status.Streaming",
                ["Module.Diagnostics.Status.Healthy"] = "Status.Healthy",
                ["Module.Diagnostics.StatusMessage.Ready"] = "Ready",
                ["Module.Diagnostics.StatusMessage.TelemetryUpdated"] = "Telemetry",
                ["Module.Diagnostics.StatusMessage.HealthUpdated"] = "Health",
                ["Dock.Modules.Title"] = "Modules",
                ["Dock.Modules.AutomationId"] = "Modules.AutomationId",
                ["Dock.Modules.AutomationName"] = "Modules.AutomationName",
                ["Dock.Modules.ToolTip"] = "Modules.Tooltip",
                ["Dock.Inspector.Title"] = "Inspector",
                ["Dock.Inspector.AutomationId"] = "Inspector.AutomationId",
                ["Dock.Inspector.ToolTip"] = "Inspector.Tooltip",
                ["Dock.Inspector.ModuleTitle"] = "Module",
                ["Dock.Inspector.ModuleTitle.Template"] = "{0}",
                ["Dock.Inspector.NoRecord"] = "No record",
                ["Dock.Inspector.RecordTitle.Template"] = "{0}",
                ["Dock.Inspector.Module.AutomationName"] = "Inspector.Module.AutomationName",
                ["Dock.Inspector.Module.AutomationId"] = "Inspector.Module.AutomationId",
                ["Dock.Inspector.Module.ToolTip"] = "Inspector.Module.Tooltip",
                ["Dock.Inspector.Module.AutomationName.Template"] = "{0}",
                ["Dock.Inspector.Module.AutomationId.Template"] = "{0}",
                ["Dock.Inspector.Module.ToolTip.Template"] = "{0}",
                ["Dock.Inspector.Record.AutomationName.Template"] = "{0}",
                ["Dock.Inspector.Record.AutomationId.Template"] = "{0}",
                ["Dock.Inspector.Record.ToolTip.Template"] = "{0}",
                ["Dock.Inspector.Field.AutomationName.Template"] = "{0}",
                ["Dock.Inspector.Field.AutomationId.Template"] = "{0}",
                ["Dock.Inspector.Field.AutomationTooltip.Template"] = "{0}",
                ["Shell.StatusBar.Company.Default"] = "Company",
                ["Shell.StatusBar.Environment.Default"] = "Environment",
                ["Shell.StatusBar.Server.Default"] = "Server",
                ["Shell.StatusBar.Database.Default"] = "Database",
                ["Shell.StatusBar.User.Default"] = "User",
                ["Shell.Status.SignalR.Connecting"] = "Connecting",
                ["Shell.Status.SignalR.Connected"] = "Connected",
                ["Shell.Status.SignalR.Retrying"] = "Retrying",
                ["Shell.Status.SignalR.Disconnected"] = "Disconnected",
                ["Shell.Status.SignalR.Failed"] = "Failed",
                ["Shell.Status.Ready"] = "Ready"
            }
        };

    private sealed class StubModuleRegistry : IModuleRegistry
    {
        private readonly ShellInteractionService _shellInteraction;
        private readonly ILocalizationService _localization;
        private readonly TrackingDiagnosticsFeedService _feedService;
        private readonly List<ModuleMetadata> _modules;

        public StubModuleRegistry(ShellInteractionService shellInteraction, ILocalizationService localization, TrackingDiagnosticsFeedService feedService)
        {
            _shellInteraction = shellInteraction;
            _localization = localization;
            _feedService = feedService;
            _modules =
            [
                new ModuleMetadata(
                    DiagnosticsModuleViewModel.ModuleKey,
                    "Module.Title.Diagnostics",
                    "Diagnostics",
                    "Diagnostics module")
            ];
        }

        public IReadOnlyList<ModuleMetadata> Modules => _modules;

        public ModuleDocumentViewModel CreateModule(string moduleKey)
        {
            if (!string.Equals(moduleKey, DiagnosticsModuleViewModel.ModuleKey, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Unexpected module key '{moduleKey}'.");
            }

            var database = new DatabaseService("Server=stub;Database=stub;Uid=stub;Pwd=stub;");
            var audit = new AuditService(database);
            return new DiagnosticsModuleViewModel(
                database,
                audit,
                new StubCflDialogService(),
                _shellInteraction,
                _shellInteraction,
                _localization,
                _feedService);
        }
    }

    private sealed class StubNotificationAnalyticsViewModel : ObservableObject, INotificationAnalyticsViewModel
    {
        private readonly RelayCommand _load;
        private readonly RelayCommand _export;
        private readonly RelayCommand _filterChanged;
        private readonly RelayCommand _acknowledge;
        private readonly RelayCommand _mute;
        private readonly RelayCommand _delete;

        public StubNotificationAnalyticsViewModel()
        {
            Notifications = new ObservableCollection<Notification>();
            FilteredNotifications = new ObservableCollection<Notification>();
            AvailableTypes = Array.Empty<string>();
            _load = new RelayCommand(() => { });
            _export = new RelayCommand(() => { });
            _filterChanged = new RelayCommand(() => { });
            _acknowledge = new RelayCommand(() => { });
            _mute = new RelayCommand(() => { });
            _delete = new RelayCommand(() => { });
        }

        public ObservableCollection<Notification> Notifications { get; }

        public ObservableCollection<Notification> FilteredNotifications { get; set; }

        public Notification? SelectedNotification { get; set; }

        public string? SearchTerm { get; set; }

        public string? TypeFilter { get; set; }

        public string? EntityFilter { get; set; }

        public string? StatusFilter { get; set; }

        public IReadOnlyList<string> AvailableTypes { get; }

        public bool IsBusy { get; set; }

        public string StatusMessage { get; set; } = string.Empty;

        public ICommand LoadNotificationsCommand => _load;

        public ICommand ExportNotificationsCommand => _export;

        public ICommand FilterChangedCommand => _filterChanged;

        public ICommand AcknowledgeNotificationCommand => _acknowledge;

        public ICommand MuteNotificationCommand => _mute;

        public ICommand DeleteNotificationCommand => _delete;

        public Task ExportNotificationsAsync() => Task.CompletedTask;

        public void FilterNotifications()
        {
        }
    }

    private sealed class StubExportService : ExportService
    {
        public StubExportService()
            : base(new DatabaseService("Server=stub;Database=stub;Uid=stub;Pwd=stub;"))
        {
        }

        public override Task<string> ExportNotificationsToPdfAsync(IEnumerable<Notification> notifications, string filterUsed = "")
            => Task.FromResult("notifications.pdf");

        public override Task<string> ExportNotificationsToExcelAsync(IEnumerable<Notification> notifications, string filterUsed = "")
            => Task.FromResult("notifications.xlsx");
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";

        public string ApplicationName { get; set; } = "YasGMP";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    }

    private sealed class StubUserSession : IUserSession
    {
        public User? CurrentUser { get; set; }

        public int? UserId => CurrentUser?.Id;

        public string? Username => CurrentUser?.Username ?? "darko";

        public string? FullName => CurrentUser?.FullName ?? "Darko";

        public string SessionId { get; set; } = Guid.NewGuid().ToString("N");
    }

    private sealed class StubSignalRClientService : ISignalRClientService
    {
        public event EventHandler<AuditEventArgs>? AuditReceived;

        public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

        public RealtimeConnectionState ConnectionState { get; set; } = RealtimeConnectionState.Disconnected;

        public string? LastError { get; set; }

        public DateTimeOffset? NextRetryUtc { get; set; }

        public void Start()
        {
            ConnectionState = RealtimeConnectionState.Connected;
            ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(ConnectionState, null, null));
        }
    }

    private sealed class StubShellAlertService : IShellAlertService
    {
        private readonly ObservableCollection<ToastNotificationViewModel> _toasts = new();

        public ReadOnlyObservableCollection<ToastNotificationViewModel> Toasts { get; }

        public StubShellAlertService()
        {
            Toasts = new ReadOnlyObservableCollection<ToastNotificationViewModel>(_toasts);
        }

        public Task AlertAsync(string title, string message, string cancel = "OK") => Task.CompletedTask;

        public Task<bool> ConfirmAsync(string title, string message, string accept = "OK", string cancel = "Cancel")
            => Task.FromResult(false);

        public void PublishStatus(string message, AlertSeverity severity = AlertSeverity.Info, bool propagateToStatusBar = false)
        {
        }
    }

    private sealed class TrackingDiagnosticsFeedService : DiagnosticsFeedService
    {
        private readonly List<TrackingDisposable> _subscriptions = new();

        public TrackingDiagnosticsFeedService(IUiDispatcher dispatcher)
            : base(
                new FileLogService(() => null, baseDir: AppContext.BaseDirectory, sessionId: "test"),
                new DiagnosticContext(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build()),
                new NoopTrace(),
                dispatcher)
        {
        }

        public IReadOnlyList<TrackingDisposable> Subscriptions => _subscriptions;

        public override IDisposable SubscribeTelemetry(Action<IReadOnlyDictionary<string, object?>> callback)
        {
            var disposable = new TrackingDisposable();
            _subscriptions.Add(disposable);
            return disposable;
        }

        public override IDisposable SubscribeLog(Action<string> callback)
        {
            var disposable = new TrackingDisposable();
            _subscriptions.Add(disposable);
            return disposable;
        }

        public override IDisposable SubscribeHealth(Action<IReadOnlyDictionary<string, object?>> callback)
        {
            var disposable = new TrackingDisposable();
            _subscriptions.Add(disposable);
            return disposable;
        }
    }

    private sealed class TrackingDisposable : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    private sealed class RecordingDispatcher : IUiDispatcher
    {
        private readonly Queue<Action> _queue = new();

        public bool IsDispatchRequired => false;

        public void BeginInvoke(Action action)
        {
            _queue.Enqueue(action);
            RunQueued();
        }

        public Task InvokeAsync(Action action)
        {
            action();
            return Task.CompletedTask;
        }

        public Task<T> InvokeAsync<T>(Func<T> func)
        {
            return Task.FromResult(func());
        }

        public Task InvokeAsync(Func<Task> asyncAction)
        {
            return asyncAction();
        }

        private void RunQueued()
        {
            while (_queue.Count > 0)
            {
                var action = _queue.Dequeue();
                action();
            }
        }
    }
}
