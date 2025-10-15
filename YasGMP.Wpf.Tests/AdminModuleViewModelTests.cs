using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using Xunit;
using YasGMP.Common;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Ui;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.Tests.TestDoubles;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Tests;

public class AdminModuleViewModelTests
{
    [Fact]
    public async Task InitializeAsync_LoadsPreferencesAndRecords()
    {
        var provider = RegisterAlertService(out var alertService);
        try
        {
            var database = CreateDatabaseService(
                new Setting { Id = 1, Key = "cfg.locale", Value = "hr-HR", Description = "Default locale", Category = "system", UpdatedAt = DateTime.UtcNow },
                new Setting { Id = 2, Key = "maintenance.window", Value = "Sunday", Description = "Weekly downtime", Category = "system", UpdatedAt = DateTime.UtcNow.AddDays(-1) });
            var audit = new AuditService(database);
            var localization = new TestLocalizationService();
            var notificationPreferences = new FakeNotificationPreferenceService();
            notificationPreferences.QueueReload(new NotificationPreferences
            {
                ShowStatusBarAlerts = true,
                ShowToastAlerts = false,
            });

            var viewModel = new AdminModuleViewModel(
                database,
                audit,
                new StubCflDialogService(),
                new StubShellInteractionService(),
                new StubModuleNavigationService(),
                localization,
                notificationPreferences);

            await viewModel.InitializeAsync(null).ConfigureAwait(false);

            Assert.True(viewModel.StatusBarAlertsEnabled);
            Assert.False(viewModel.ToastAlertsEnabled);
            Assert.False(viewModel.IsNotificationPreferencesDirty);
            Assert.Equal(2, viewModel.Records.Count);
            Assert.Equal("Loaded 2 record(s).", viewModel.StatusMessage);
            Assert.Equal("Loaded 2 record(s).", alertService.LastMessage);
            Assert.Equal(AlertSeverity.Success, alertService.LastSeverity);
            Assert.False(viewModel.SaveNotificationPreferencesCommand.CanExecute(null));
        }
        finally
        {
            ServiceLocator.RegisterFallback(() => null);
            provider.Dispose();
        }
    }

    [Fact]
    public async Task InitializeAsync_WhenPreferencesFail_SetsWarningStatus()
    {
        var provider = RegisterAlertService(out var alertService);
        try
        {
            var database = CreateDatabaseService();
            var audit = new AuditService(database);
            var localization = new TestLocalizationService();
            var notificationPreferences = new FakeNotificationPreferenceService
            {
                ReloadException = new InvalidOperationException("database offline"),
            };

            var viewModel = new AdminModuleViewModel(
                database,
                audit,
                new StubCflDialogService(),
                new StubShellInteractionService(),
                new StubModuleNavigationService(),
                localization,
                notificationPreferences);

            await viewModel.InitializeAsync(null).ConfigureAwait(false);

            Assert.Equal("Failed to load notification preferences: database offline", viewModel.StatusMessage);
            Assert.Equal("Failed to load notification preferences: database offline", alertService.LastMessage);
            Assert.Equal(AlertSeverity.Warning, alertService.LastSeverity);
            Assert.False(viewModel.IsNotificationPreferencesDirty);
        }
        finally
        {
            ServiceLocator.RegisterFallback(() => null);
            provider.Dispose();
        }
    }

    [Fact]
    public async Task SaveNotificationPreferencesCommand_SavesAndPublishesSuccess()
    {
        var provider = RegisterAlertService(out var alertService);
        try
        {
            var database = CreateDatabaseService();
            var audit = new AuditService(database);
            var localization = new TestLocalizationService();
            var notificationPreferences = new FakeNotificationPreferenceService();
            notificationPreferences.QueueReload(new NotificationPreferences
            {
                ShowStatusBarAlerts = false,
                ShowToastAlerts = false,
            });

            var viewModel = new AdminModuleViewModel(
                database,
                audit,
                new StubCflDialogService(),
                new StubShellInteractionService(),
                new StubModuleNavigationService(),
                localization,
                notificationPreferences);

            await viewModel.InitializeAsync(null).ConfigureAwait(false);

            viewModel.StatusBarAlertsEnabled = true;
            viewModel.ToastAlertsEnabled = true;

            Assert.True(viewModel.IsNotificationPreferencesDirty);
            Assert.True(viewModel.SaveNotificationPreferencesCommand.CanExecute(null));

            await viewModel.SaveNotificationPreferencesCommand.ExecuteAsync(null).ConfigureAwait(false);

            var saved = Assert.Single(notificationPreferences.Saved);
            Assert.True(saved.ShowStatusBarAlerts);
            Assert.True(saved.ShowToastAlerts);
            Assert.False(viewModel.IsNotificationPreferencesDirty);
            Assert.Equal("Notification preferences saved.", viewModel.StatusMessage);
            Assert.Equal("Notification preferences saved.", alertService.LastMessage);
            Assert.Equal(AlertSeverity.Success, alertService.LastSeverity);
        }
        finally
        {
            ServiceLocator.RegisterFallback(() => null);
            provider.Dispose();
        }
    }

    [Fact]
    public async Task SaveNotificationPreferencesCommand_WhenServiceThrows_KeepsDirtyState()
    {
        var provider = RegisterAlertService(out var alertService);
        try
        {
            var database = CreateDatabaseService();
            var audit = new AuditService(database);
            var localization = new TestLocalizationService();
            var notificationPreferences = new FakeNotificationPreferenceService
            {
                SaveException = new InvalidOperationException("write denied"),
            };
            notificationPreferences.QueueReload(new NotificationPreferences
            {
                ShowStatusBarAlerts = false,
                ShowToastAlerts = false,
            });

            var viewModel = new AdminModuleViewModel(
                database,
                audit,
                new StubCflDialogService(),
                new StubShellInteractionService(),
                new StubModuleNavigationService(),
                localization,
                notificationPreferences);

            await viewModel.InitializeAsync(null).ConfigureAwait(false);

            viewModel.StatusBarAlertsEnabled = true;
            viewModel.ToastAlertsEnabled = true;

            await viewModel.SaveNotificationPreferencesCommand.ExecuteAsync(null).ConfigureAwait(false);

            Assert.True(viewModel.IsNotificationPreferencesDirty);
            Assert.Equal("Failed to save notification preferences: write denied", viewModel.StatusMessage);
            Assert.Equal("Failed to save notification preferences: write denied", alertService.LastMessage);
            Assert.Equal(AlertSeverity.Error, alertService.LastSeverity);
        }
        finally
        {
            ServiceLocator.RegisterFallback(() => null);
            provider.Dispose();
        }
    }

    private static ServiceProvider RegisterAlertService(out CapturingAlertService alertService)
    {
        var services = new ServiceCollection();
        alertService = new CapturingAlertService();
        services.AddSingleton<IShellAlertService>(alertService);
        services.AddSingleton<IAlertService>(alertService);
        var provider = services.BuildServiceProvider();
        ServiceLocator.RegisterFallback(() => provider);
        return provider;
    }

    private static DatabaseService CreateDatabaseService(params Setting[] settings)
    {
        var database = new DatabaseService("Server=stub;Database=stub;Uid=stub;Pwd=stub;");
        var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("key", typeof(string));
        table.Columns.Add("value", typeof(string));
        table.Columns.Add("description", typeof(string));
        table.Columns.Add("category", typeof(string));
        table.Columns.Add("updated_at", typeof(DateTime));

        foreach (var setting in settings)
        {
            var row = table.NewRow();
            row["id"] = setting.Id;
            row["key"] = setting.Key;
            row["value"] = setting.Value;
            row["description"] = setting.Description;
            row["category"] = setting.Category;
            row["updated_at"] = setting.UpdatedAt;
            table.Rows.Add(row);
        }

        var selectOverride = (Func<string, IEnumerable<MySqlParameter>?, CancellationToken, Task<DataTable>>)((_, _, _) => Task.FromResult(table.Copy()));
        typeof(DatabaseService)
            .GetProperty("ExecuteSelectOverride", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(database, selectOverride);

        return database;
    }

    private sealed class CapturingAlertService : IShellAlertService
    {
        private readonly ObservableCollection<ToastNotificationViewModel> _toasts = new();

        public string? LastMessage { get; private set; }

        public AlertSeverity LastSeverity { get; private set; }

        public ReadOnlyObservableCollection<ToastNotificationViewModel> Toasts { get; }

        public CapturingAlertService()
        {
            Toasts = new ReadOnlyObservableCollection<ToastNotificationViewModel>(_toasts);
        }

        public Task AlertAsync(string title, string message, string cancel = "OK") => Task.CompletedTask;

        public Task<bool> ConfirmAsync(string title, string message, string accept = "OK", string cancel = "Cancel")
            => Task.FromResult(true);

        public void PublishStatus(string message, AlertSeverity severity = AlertSeverity.Info, bool propagateToStatusBar = false)
        {
            LastMessage = message;
            LastSeverity = severity;
        }
    }

    private sealed class FakeNotificationPreferenceService : INotificationPreferenceService
    {
        private readonly Queue<NotificationPreferences> _pendingReloads = new();

        public FakeNotificationPreferenceService()
        {
            Current = NotificationPreferences.CreateDefault();
        }

        public event EventHandler<NotificationPreferences>? PreferencesChanged;

        public NotificationPreferences Current { get; private set; }

        public List<NotificationPreferences> Saved { get; } = new();

        public Exception? ReloadException { get; set; }

        public Exception? SaveException { get; set; }

        public void QueueReload(NotificationPreferences preferences)
        {
            if (preferences is null)
            {
                throw new ArgumentNullException(nameof(preferences));
            }

            _pendingReloads.Enqueue(preferences.Clone());
        }

        public Task<NotificationPreferences> ReloadAsync(CancellationToken token = default)
        {
            if (ReloadException is not null)
            {
                throw ReloadException;
            }

            if (_pendingReloads.Count > 0)
            {
                Current = _pendingReloads.Dequeue().Clone();
            }

            return Task.FromResult(Current.Clone());
        }

        public Task SaveAsync(NotificationPreferences preferences, CancellationToken token = default)
        {
            if (preferences is null)
            {
                throw new ArgumentNullException(nameof(preferences));
            }

            if (SaveException is not null)
            {
                throw SaveException;
            }

            Current = preferences.Clone();
            Saved.Add(Current.Clone());
            PreferencesChanged?.Invoke(this, Current.Clone());
            return Task.CompletedTask;
        }
    }

    private sealed class TestLocalizationService : ILocalizationService
    {
        private static readonly Dictionary<string, string> Resources = new()
        {
            ["Module.Title.Administration"] = "Administration",
            ["Module.Status.Ready"] = "Ready",
            ["Module.Status.Loading"] = "Loading {0} record(s)...",
            ["Module.Status.Loaded"] = "Loaded {0} record(s).",
            ["Module.Admin.NotificationPreferences.StatusLoadFailed"] = "Failed to load notification preferences: {0}",
            ["Module.Admin.NotificationPreferences.StatusSaved"] = "Notification preferences saved.",
            ["Module.Admin.NotificationPreferences.StatusSaveFailed"] = "Failed to save notification preferences: {0}",
        };

        public string CurrentLanguage => "en";

        public event EventHandler? LanguageChanged
        {
            add { }
            remove { }
        }

        public string GetString(string key)
            => Resources.TryGetValue(key, out var value) ? value : key;

        public void SetLanguage(string language)
        {
        }
    }
}
