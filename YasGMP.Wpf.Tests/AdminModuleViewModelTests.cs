using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using Xunit;
using YasGMP.Common;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
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
        var authContext = new StubAuthContext();
        var provider = RegisterAlertService(out var alertService, authContext);
        try
        {
            var signatureService = TestElectronicSignatureDialogService.CreateConfirmed();
            var dialogService = new StubDialogService();
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
                signatureService,
                dialogService,
                authContext,
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
        var authContext = new StubAuthContext();
        var provider = RegisterAlertService(out var alertService, authContext);
        try
        {
            var signatureService = TestElectronicSignatureDialogService.CreateConfirmed();
            var dialogService = new StubDialogService();
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
                signatureService,
                dialogService,
                authContext,
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
        var authContext = new StubAuthContext();
        var provider = RegisterAlertService(out var alertService, authContext);
        try
        {
            var signatureService = TestElectronicSignatureDialogService.CreateConfirmed();
            var dialogService = new StubDialogService();
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
                signatureService,
                dialogService,
                authContext,
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
        var authContext = new StubAuthContext();
        var provider = RegisterAlertService(out var alertService, authContext);
        try
        {
            var signatureService = TestElectronicSignatureDialogService.CreateConfirmed();
            var dialogService = new StubDialogService();
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
                signatureService,
                dialogService,
                authContext,
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

    [Fact]
    public async Task SaveCommand_WhenValidationFailsInAddMode_SurfacesLocalizedErrorsAndSkipsDatabase()
    {
        var authContext = new StubAuthContext();
        var provider = RegisterAlertService(out var alertService, authContext);
        try
        {
            var signatureService = TestElectronicSignatureDialogService.CreateConfirmed();
            var dialogService = new StubDialogService();
            var database = CreateDatabaseService(out _, out var capture);
            var audit = new AuditService(database);
            var localization = new TestLocalizationService();
            var notificationPreferences = new FakeNotificationPreferenceService();
            var viewModel = new AdminModuleViewModel(
                database,
                audit,
                signatureService,
                dialogService,
                authContext,
                new StubCflDialogService(),
                new StubShellInteractionService(),
                new StubModuleNavigationService(),
                localization,
                notificationPreferences);

            await viewModel.InitializeAsync(null).ConfigureAwait(false);
            await viewModel.EnterAddModeCommand.ExecuteAsync(null).ConfigureAwait(false);

            await viewModel.SaveCommand.ExecuteAsync(null).ConfigureAwait(false);

            var expectedMessages = new[]
            {
                localization.GetString("Module.Admin.Settings.Validation.KeyRequired"),
                localization.GetString("Module.Admin.Settings.Validation.ValueRequired"),
                localization.GetString("Module.Admin.Settings.Validation.CategoryRequired"),
            };

            Assert.Equal(expectedMessages, viewModel.ValidationMessages);

            var expectedStatus = string.Format(
                CultureInfo.CurrentCulture,
                localization.GetString("Module.Status.ValidationIssues"),
                localization.GetString("Module.Title.Administration"),
                expectedMessages.Length);

            Assert.Equal(expectedStatus, viewModel.StatusMessage);
            Assert.Equal(expectedStatus, alertService.LastMessage);
            Assert.Equal(AlertSeverity.Warning, alertService.LastSeverity);
            Assert.Equal(0, capture.ExecuteNonQueryCallCount);
            Assert.Empty(capture.NonQueryCommands);
        }
        finally
        {
            ServiceLocator.RegisterFallback(() => null);
            provider.Dispose();
        }
    }

    [Fact]
    public async Task SaveCommand_WhenInAddMode_PersistsTrimmedKeyAndPublishesCreateStatus()
    {
        var authContext = new StubAuthContext();
        var provider = RegisterAlertService(out var alertService, authContext);
        try
        {
            var signatureService = TestElectronicSignatureDialogService.CreateConfirmed();
            var dialogService = new StubDialogService();
            var database = CreateDatabaseService(out _, out var capture);
            capture.QueueNonQueryResult(1);
            capture.QueueScalarResult(42);

            var audit = new AuditService(database);
            var localization = new TestLocalizationService();
            var notificationPreferences = new FakeNotificationPreferenceService();
            var viewModel = new AdminModuleViewModel(
                database,
                audit,
                signatureService,
                dialogService,
                authContext,
                new StubCflDialogService(),
                new StubShellInteractionService(),
                new StubModuleNavigationService(),
                localization,
                notificationPreferences);

            await viewModel.InitializeAsync(null).ConfigureAwait(false);
            await viewModel.EnterAddModeCommand.ExecuteAsync(null).ConfigureAwait(false);

            var editable = Assert.IsType<AdminModuleViewModel.EditableSetting>(viewModel.CurrentSetting);
            editable.Key = "  cfg.new.key  ";
            editable.Value = "enabled";
            editable.Category = "system";
            editable.Description = "Created during test";

            Assert.True(viewModel.IsDirty);

            await viewModel.SaveCommand.ExecuteAsync(null).ConfigureAwait(false);

            var insertCommand = capture.NonQueryCommands.First(command =>
                command.Sql.Contains("INSERT INTO settings", StringComparison.OrdinalIgnoreCase));
            var parameters = insertCommand.Parameters.ToDictionary(p => p.ParameterName, p => p.Value);

            Assert.Equal("cfg.new.key", Assert.IsType<string>(parameters["@key"]));
            Assert.Equal("enabled", Assert.IsType<string>(parameters["@val"]));
            Assert.Equal("system", Assert.IsType<string>(parameters["@cat"]));
            Assert.Equal("Created during test", Assert.IsType<string>(parameters["@desc"]));

            var scalar = Assert.Single(capture.ScalarCommands);
            Assert.Contains("SELECT LAST_INSERT_ID()", scalar.Sql, StringComparison.OrdinalIgnoreCase);

            var expectedStatus = string.Format(
                CultureInfo.CurrentCulture,
                localization.GetString("Module.Admin.Settings.Save.Status.CreateSuccess"),
                "cfg.new.key");
            Assert.Equal(expectedStatus, viewModel.StatusMessage);
            Assert.Equal(expectedStatus, alertService.LastMessage);
            Assert.Equal(AlertSeverity.Success, alertService.LastSeverity);

            Assert.False(editable.IsNew);
            Assert.False(viewModel.IsDirty);
        }
        finally
        {
            ServiceLocator.RegisterFallback(() => null);
            provider.Dispose();
        }
    }

    [Fact]
    public async Task SaveCommand_WhenInUpdateMode_PersistsChangesAndPublishesUpdateStatus()
    {
        var existing = new Setting
        {
            Id = 12,
            Key = "cfg.locale",
            Value = "en-US",
            Category = "system",
            Description = "Original",
            UpdatedAt = DateTime.UtcNow.AddDays(-2),
        };

        var authContext = new StubAuthContext();
        var provider = RegisterAlertService(out var alertService, authContext);
        try
        {
            var signatureService = TestElectronicSignatureDialogService.CreateConfirmed();
            var dialogService = new StubDialogService();
            var database = CreateDatabaseService(out _, out var capture, existing);
            capture.QueueNonQueryResult(1);

            var audit = new AuditService(database);
            var localization = new TestLocalizationService();
            var notificationPreferences = new FakeNotificationPreferenceService();
            var viewModel = new AdminModuleViewModel(
                database,
                audit,
                signatureService,
                dialogService,
                authContext,
                new StubCflDialogService(),
                new StubShellInteractionService(),
                new StubModuleNavigationService(),
                localization,
                notificationPreferences);

            await viewModel.InitializeAsync(null).ConfigureAwait(false);
            viewModel.SelectedRecord = viewModel.Records.First();
            await viewModel.EnterUpdateModeCommand.ExecuteAsync(null).ConfigureAwait(false);

            var editable = Assert.IsType<AdminModuleViewModel.EditableSetting>(viewModel.CurrentSetting);
            editable.Key = "  cfg.locale  ";
            editable.Value = "hr-HR";
            editable.Category = "system-updated";
            editable.Description = "Updated locale";

            Assert.True(viewModel.IsDirty);

            await viewModel.SaveCommand.ExecuteAsync(null).ConfigureAwait(false);

            var updateCommand = capture.NonQueryCommands.First(command =>
                command.Sql.Contains("UPDATE settings", StringComparison.OrdinalIgnoreCase));
            var parameters = updateCommand.Parameters.ToDictionary(p => p.ParameterName, p => p.Value);

            Assert.Equal("cfg.locale", Assert.IsType<string>(parameters["@key"]));
            Assert.Equal("hr-HR", Assert.IsType<string>(parameters["@val"]));
            Assert.Equal("system-updated", Assert.IsType<string>(parameters["@cat"]));
            Assert.Equal("Updated locale", Assert.IsType<string>(parameters["@desc"]));
            Assert.Equal(existing.Id, Assert.IsType<int>(parameters["@id"]));

            Assert.Empty(capture.ScalarCommands);

            var expectedStatus = string.Format(
                CultureInfo.CurrentCulture,
                localization.GetString("Module.Admin.Settings.Save.Status.UpdateSuccess"),
                "cfg.locale");
            Assert.Equal(expectedStatus, viewModel.StatusMessage);
            Assert.Equal(expectedStatus, alertService.LastMessage);
            Assert.Equal(AlertSeverity.Success, alertService.LastSeverity);

            Assert.False(editable.IsNew);
            Assert.False(viewModel.IsDirty);
        }
        finally
        {
            ServiceLocator.RegisterFallback(() => null);
            provider.Dispose();
        }
    }

    [Fact]
    public async Task RestoreSettingCommand_WhenConfirmedAndSignatureCaptured_RestoresSetting()
    {
        var authContext = new StubAuthContext();
        var provider = RegisterAlertService(out var alertService, authContext);
        try
        {
            var signatureService = TestElectronicSignatureDialogService.CreateConfirmed();
            var dialogService = new StubDialogService { ConfirmationResult = true };
            var database = CreateDatabaseService(
                out var table,
                new Setting { Id = 1, Key = "cfg.locale", Value = "hr-HR", Description = "Default locale", Category = "system", UpdatedAt = DateTime.UtcNow });

            var selectCallCount = 0;
            var selectOverride = (Func<string, IEnumerable<MySqlParameter>?, CancellationToken, Task<DataTable>>)((_, _, _) =>
            {
                selectCallCount++;
                return Task.FromResult(table.Copy());
            });
            typeof(DatabaseService)
                .GetProperty("ExecuteSelectOverride", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(database, selectOverride);

            var audit = new AuditService(database);
            var localization = new TestLocalizationService();
            var notificationPreferences = new FakeNotificationPreferenceService();
            var viewModel = new AdminModuleViewModel(
                database,
                audit,
                signatureService,
                dialogService,
                authContext,
                new StubCflDialogService(),
                new StubShellInteractionService(),
                new StubModuleNavigationService(),
                localization,
                notificationPreferences);

            await viewModel.InitializeAsync(null).ConfigureAwait(false);
            Assert.Equal(1, selectCallCount);

            var nonQueryCalls = 0;
            string? lastSql = null;
            var nonQueryOverride = (Func<string, IEnumerable<MySqlParameter>?, CancellationToken, Task<int>>)((sql, _, _) =>
            {
                nonQueryCalls++;
                lastSql = sql;
                return Task.FromResult(1);
            });
            typeof(DatabaseService)
                .GetProperty("ExecuteNonQueryOverride", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(database, nonQueryOverride);
            typeof(DatabaseService)
                .GetProperty("ExecuteScalarOverride", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(database, (Func<string, IEnumerable<MySqlParameter>?, CancellationToken, Task<object?>>)((_, _, _) => Task.FromResult<object?>(0)));

            await viewModel.RestoreSettingCommand.ExecuteAsync(null).ConfigureAwait(false);

            Assert.Equal(2, selectCallCount);
            Assert.True(nonQueryCalls > 0);
            Assert.NotNull(lastSql);
            Assert.Contains("settings", lastSql!, StringComparison.OrdinalIgnoreCase);

            var expectedStatus = string.Format(CultureInfo.CurrentCulture, localization.GetString("Module.Admin.Restore.Status.Success"), "cfg.locale");
            Assert.Equal(expectedStatus, viewModel.StatusMessage);
            Assert.Equal(expectedStatus, alertService.LastMessage);
            Assert.Equal(AlertSeverity.Success, alertService.LastSeverity);

            var expectedSignatureStatus = string.Format(CultureInfo.CurrentCulture, localization.GetString("Module.Admin.Restore.Status.SignatureCaptured"), "QA Reason");
            Assert.Equal(expectedSignatureStatus, viewModel.LastSignatureStatus);

            Assert.True(signatureService.WasPersistInvoked);
            var request = Assert.Single(signatureService.Requests);
            Assert.Equal("settings", request.TableName);
            Assert.Equal(1, request.RecordId);
        }
        finally
        {
            ServiceLocator.RegisterFallback(() => null);
            provider.Dispose();
        }
    }

    [Fact]
    public async Task RestoreSettingCommand_WhenRecordKeyMissing_UsesCodeLookup()
    {
        var authContext = new StubAuthContext();
        var provider = RegisterAlertService(out var alertService, authContext);
        try
        {
            var signatureService = TestElectronicSignatureDialogService.CreateConfirmed();
            var dialogService = new StubDialogService { ConfirmationResult = true };
            var database = CreateDatabaseService(
                out var table,
                new Setting { Id = 1, Key = "cfg.locale", Value = "hr-HR", Description = "Default locale", Category = "system", UpdatedAt = DateTime.UtcNow });

            var selectCallCount = 0;
            var selectOverride = (Func<string, IEnumerable<MySqlParameter>?, CancellationToken, Task<DataTable>>)((_, _, _) =>
            {
                selectCallCount++;
                return Task.FromResult(table.Copy());
            });
            typeof(DatabaseService)
                .GetProperty("ExecuteSelectOverride", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(database, selectOverride);

            var audit = new AuditService(database);
            var localization = new TestLocalizationService();
            var notificationPreferences = new FakeNotificationPreferenceService();
            var viewModel = new AdminModuleViewModel(
                database,
                audit,
                signatureService,
                dialogService,
                authContext,
                new StubCflDialogService(),
                new StubShellInteractionService(),
                new StubModuleNavigationService(),
                localization,
                notificationPreferences);

            await viewModel.InitializeAsync(null).ConfigureAwait(false);
            Assert.Equal(1, selectCallCount);

            viewModel.SelectedRecord = new ModuleRecord("stale-key", "Locale Display", "cfg.locale");

            var nonQueryCalls = 0;
            string? lastSql = null;
            var nonQueryOverride = (Func<string, IEnumerable<MySqlParameter>?, CancellationToken, Task<int>>)((sql, _, _) =>
            {
                nonQueryCalls++;
                lastSql = sql;
                return Task.FromResult(1);
            });
            typeof(DatabaseService)
                .GetProperty("ExecuteNonQueryOverride", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(database, nonQueryOverride);
            typeof(DatabaseService)
                .GetProperty("ExecuteScalarOverride", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(database, (Func<string, IEnumerable<MySqlParameter>?, CancellationToken, Task<object?>>)((_, _, _) => Task.FromResult<object?>(0)));

            await viewModel.RestoreSettingCommand.ExecuteAsync(null).ConfigureAwait(false);

            Assert.Equal(2, selectCallCount);
            Assert.True(nonQueryCalls > 0);
            Assert.NotNull(lastSql);
            Assert.Contains("settings", lastSql!, StringComparison.OrdinalIgnoreCase);

            var expectedStatus = string.Format(CultureInfo.CurrentCulture, localization.GetString("Module.Admin.Restore.Status.Success"), "Locale Display");
            Assert.Equal(expectedStatus, viewModel.StatusMessage);
            Assert.Equal(expectedStatus, alertService.LastMessage);
            Assert.Equal(AlertSeverity.Success, alertService.LastSeverity);

            var expectedSignatureStatus = string.Format(CultureInfo.CurrentCulture, localization.GetString("Module.Admin.Restore.Status.SignatureCaptured"), "QA Reason");
            Assert.Equal(expectedSignatureStatus, viewModel.LastSignatureStatus);

            Assert.True(signatureService.WasPersistInvoked);
            var request = Assert.Single(signatureService.Requests);
            Assert.Equal("settings", request.TableName);
            Assert.Equal(1, request.RecordId);
        }
        finally
        {
            ServiceLocator.RegisterFallback(() => null);
            provider.Dispose();
        }
    }

    [Fact]
    public async Task RestoreSettingCommand_WhenConfirmationDeclined_SkipsOperation()
    {
        var authContext = new StubAuthContext();
        var provider = RegisterAlertService(out var alertService, authContext);
        try
        {
            var signatureService = TestElectronicSignatureDialogService.CreateConfirmed();
            var dialogService = new StubDialogService { ConfirmationResult = false };
            var database = CreateDatabaseService(
                out var table,
                new Setting { Id = 1, Key = "cfg.locale", Value = "hr-HR", Description = "Default locale", Category = "system", UpdatedAt = DateTime.UtcNow });

            var selectCallCount = 0;
            var selectOverride = (Func<string, IEnumerable<MySqlParameter>?, CancellationToken, Task<DataTable>>)((_, _, _) =>
            {
                selectCallCount++;
                return Task.FromResult(table.Copy());
            });
            typeof(DatabaseService)
                .GetProperty("ExecuteSelectOverride", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(database, selectOverride);

            var audit = new AuditService(database);
            var localization = new TestLocalizationService();
            var notificationPreferences = new FakeNotificationPreferenceService();
            var viewModel = new AdminModuleViewModel(
                database,
                audit,
                signatureService,
                dialogService,
                authContext,
                new StubCflDialogService(),
                new StubShellInteractionService(),
                new StubModuleNavigationService(),
                localization,
                notificationPreferences);

            await viewModel.InitializeAsync(null).ConfigureAwait(false);
            Assert.Equal(1, selectCallCount);

            await viewModel.RestoreSettingCommand.ExecuteAsync(null).ConfigureAwait(false);

            Assert.Equal(1, selectCallCount);
            Assert.Empty(signatureService.Requests);

            var expectedStatus = string.Format(CultureInfo.CurrentCulture, localization.GetString("Module.Admin.Restore.Status.ConfirmationDeclined"), "cfg.locale");
            Assert.Equal(expectedStatus, viewModel.StatusMessage);
            Assert.Equal(expectedStatus, alertService.LastMessage);
            Assert.Equal(AlertSeverity.Info, alertService.LastSeverity);
            Assert.Null(viewModel.LastSignatureStatus);
        }
        finally
        {
            ServiceLocator.RegisterFallback(() => null);
            provider.Dispose();
        }
    }

    [Fact]
    public async Task RestoreSettingCommand_WhenSignatureCancelled_SetsWarningStatus()
    {
        var authContext = new StubAuthContext();
        var provider = RegisterAlertService(out var alertService, authContext);
        try
        {
            var signatureService = TestElectronicSignatureDialogService.CreateCancelled();
            var dialogService = new StubDialogService { ConfirmationResult = true };
            var database = CreateDatabaseService(
                out var table,
                new Setting { Id = 1, Key = "cfg.locale", Value = "hr-HR", Description = "Default locale", Category = "system", UpdatedAt = DateTime.UtcNow });

            var selectCallCount = 0;
            var selectOverride = (Func<string, IEnumerable<MySqlParameter>?, CancellationToken, Task<DataTable>>)((_, _, _) =>
            {
                selectCallCount++;
                return Task.FromResult(table.Copy());
            });
            typeof(DatabaseService)
                .GetProperty("ExecuteSelectOverride", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(database, selectOverride);

            var audit = new AuditService(database);
            var localization = new TestLocalizationService();
            var notificationPreferences = new FakeNotificationPreferenceService();
            var viewModel = new AdminModuleViewModel(
                database,
                audit,
                signatureService,
                dialogService,
                authContext,
                new StubCflDialogService(),
                new StubShellInteractionService(),
                new StubModuleNavigationService(),
                localization,
                notificationPreferences);

            await viewModel.InitializeAsync(null).ConfigureAwait(false);
            Assert.Equal(1, selectCallCount);

            await viewModel.RestoreSettingCommand.ExecuteAsync(null).ConfigureAwait(false);

            Assert.Equal(1, selectCallCount);
            Assert.Single(signatureService.Requests);
            Assert.False(signatureService.WasPersistInvoked);

            var expectedStatus = localization.GetString("Module.Admin.Restore.Status.SignatureCancelled");
            Assert.Equal(expectedStatus, viewModel.StatusMessage);
            Assert.Equal(expectedStatus, viewModel.LastSignatureStatus);
            Assert.Equal(expectedStatus, alertService.LastMessage);
            Assert.Equal(AlertSeverity.Warning, alertService.LastSeverity);
        }
        finally
        {
            ServiceLocator.RegisterFallback(() => null);
            provider.Dispose();
        }
    }

    [Fact]
    public async Task RestoreSettingCommand_WhenRollbackFails_SurfacesError()
    {
        var authContext = new StubAuthContext();
        var provider = RegisterAlertService(out var alertService, authContext);
        try
        {
            var signatureService = TestElectronicSignatureDialogService.CreateConfirmed();
            var dialogService = new StubDialogService { ConfirmationResult = true };
            var database = CreateDatabaseService(
                out var table,
                new Setting { Id = 1, Key = "cfg.locale", Value = "hr-HR", Description = "Default locale", Category = "system", UpdatedAt = DateTime.UtcNow });

            var selectCallCount = 0;
            var selectOverride = (Func<string, IEnumerable<MySqlParameter>?, CancellationToken, Task<DataTable>>)((_, _, _) =>
            {
                selectCallCount++;
                return Task.FromResult(table.Copy());
            });
            typeof(DatabaseService)
                .GetProperty("ExecuteSelectOverride", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(database, selectOverride);

            var nonQueryOverride = (Func<string, IEnumerable<MySqlParameter>?, CancellationToken, Task<int>>)((sql, _, _) =>
            {
                if (sql.Contains("UPDATE settings", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("update failed");
                }

                return Task.FromResult(1);
            });
            typeof(DatabaseService)
                .GetProperty("ExecuteNonQueryOverride", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(database, nonQueryOverride);
            typeof(DatabaseService)
                .GetProperty("ExecuteScalarOverride", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(database, (Func<string, IEnumerable<MySqlParameter>?, CancellationToken, Task<object?>>)((_, _, _) => Task.FromResult<object?>(0)));

            var audit = new AuditService(database);
            var localization = new TestLocalizationService();
            var notificationPreferences = new FakeNotificationPreferenceService();
            var viewModel = new AdminModuleViewModel(
                database,
                audit,
                signatureService,
                dialogService,
                authContext,
                new StubCflDialogService(),
                new StubShellInteractionService(),
                new StubModuleNavigationService(),
                localization,
                notificationPreferences);

            await viewModel.InitializeAsync(null).ConfigureAwait(false);
            Assert.Equal(1, selectCallCount);

            await viewModel.RestoreSettingCommand.ExecuteAsync(null).ConfigureAwait(false);

            Assert.Equal(1, selectCallCount);
            Assert.False(signatureService.WasPersistInvoked);
            Assert.Single(signatureService.Requests);

            var expectedStatus = string.Format(CultureInfo.CurrentCulture, localization.GetString("Module.Admin.Restore.Status.Failure"), "cfg.locale", "update failed");
            Assert.Equal(expectedStatus, viewModel.StatusMessage);
            Assert.Equal(expectedStatus, alertService.LastMessage);
            Assert.Equal(AlertSeverity.Error, alertService.LastSeverity);
            Assert.Null(viewModel.LastSignatureStatus);
        }
        finally
        {
            ServiceLocator.RegisterFallback(() => null);
            provider.Dispose();
        }
    }

    private static ServiceProvider RegisterAlertService(out CapturingAlertService alertService, IAuthContext? authContext = null)
    {
        var services = new ServiceCollection();
        alertService = new CapturingAlertService();
        services.AddSingleton<IShellAlertService>(alertService);
        services.AddSingleton<IAlertService>(alertService);
        if (authContext is not null)
        {
            services.AddSingleton(authContext);
        }
        var provider = services.BuildServiceProvider();
        ServiceLocator.RegisterFallback(() => provider);
        return provider;
    }

    private static DatabaseService CreateDatabaseService(params Setting[] settings)
        => CreateDatabaseService(out _, out _, settings);

    private static DatabaseService CreateDatabaseService(out DataTable table, params Setting[] settings)
        => CreateDatabaseService(out table, out _, settings);

    private static DatabaseService CreateDatabaseService(
        out DataTable table,
        out DatabaseCommandCapture capture,
        params Setting[] settings)
    {
        var database = new DatabaseService("Server=stub;Database=stub;Uid=stub;Pwd=stub;");
        capture = new DatabaseCommandCapture();

        table = new DataTable();
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
        var nonQueryOverride = (Func<string, IEnumerable<MySqlParameter>?, CancellationToken, Task<int>>)((sql, parameters, _) =>
        {
            capture.CaptureNonQuery(sql, parameters);
            return Task.FromResult(capture.DequeueNonQueryResult());
        });
        var scalarOverride = (Func<string, IEnumerable<MySqlParameter>?, CancellationToken, Task<object?>>)((sql, parameters, _) =>
        {
            capture.CaptureScalar(sql, parameters);
            return Task.FromResult(capture.DequeueScalarResult());
        });

        typeof(DatabaseService)
            .GetProperty("ExecuteSelectOverride", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(database, selectOverride);
        typeof(DatabaseService)
            .GetProperty("ExecuteNonQueryOverride", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(database, nonQueryOverride);
        typeof(DatabaseService)
            .GetProperty("ExecuteScalarOverride", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(database, scalarOverride);

        return database;
    }

    private sealed class DatabaseCommandCapture
    {
        private readonly Queue<int> _nonQueryResults = new();
        private readonly Queue<object?> _scalarResults = new();

        public List<CapturedCommand> NonQueryCommands { get; } = new();

        public List<CapturedCommand> ScalarCommands { get; } = new();

        public void QueueNonQueryResult(int result)
            => _nonQueryResults.Enqueue(result);

        public void QueueScalarResult(object? result)
            => _scalarResults.Enqueue(result);

        public void CaptureNonQuery(string sql, IEnumerable<MySqlParameter>? parameters)
        {
            ExecuteNonQueryCallCount++;
            NonQueryCommands.Add(CapturedCommand.Create(sql, parameters));
        }

        public void CaptureScalar(string sql, IEnumerable<MySqlParameter>? parameters)
            => ScalarCommands.Add(CapturedCommand.Create(sql, parameters));

        public int DequeueNonQueryResult()
            => _nonQueryResults.Count > 0 ? _nonQueryResults.Dequeue() : 1;

        public object? DequeueScalarResult()
            => _scalarResults.Count > 0 ? _scalarResults.Dequeue() : null;

        public int ExecuteNonQueryCallCount { get; private set; }

        public sealed record CapturedCommand(string Sql, IReadOnlyList<MySqlParameter> Parameters)
        {
            public static CapturedCommand Create(string sql, IEnumerable<MySqlParameter>? parameters)
            {
                var list = new List<MySqlParameter>();
                if (parameters is not null)
                {
                    foreach (var parameter in parameters)
                    {
                        var clone = new MySqlParameter(parameter.ParameterName, parameter.Value);
                        list.Add(clone);
                    }
                }

                return new CapturedCommand(sql, list);
            }
        }
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
            ["Module.Status.ValidationIssues"] = "{0} has {1} validation issue(s).",
            ["Module.Admin.NotificationPreferences.StatusLoadFailed"] = "Failed to load notification preferences: {0}",
            ["Module.Admin.NotificationPreferences.StatusSaved"] = "Notification preferences saved.",
            ["Module.Admin.NotificationPreferences.StatusSaveFailed"] = "Failed to save notification preferences: {0}",
            ["Module.Admin.Settings.Save.Status.CreateSuccess"] = "Setting \"{0}\" created.",
            ["Module.Admin.Settings.Save.Status.UpdateSuccess"] = "Setting \"{0}\" updated.",
            ["Module.Admin.Settings.Validation.KeyRequired"] = "Setting key is required.",
            ["Module.Admin.Settings.Validation.ValueRequired"] = "Setting value is required.",
            ["Module.Admin.Settings.Validation.CategoryRequired"] = "Setting category is required.",
            ["Module.Admin.Settings.Validation.DuplicateKey"] = "A setting with key \"{0}\" already exists.",
            ["Module.Admin.Settings.Validation.SelectionRequired"] = "Select a setting before saving.",
            ["Module.Admin.Settings.Validation.DeleteRequiresExisting"] = "Cannot delete a setting that has not been saved.",
            ["Module.Admin.Settings.Validation.UpdateRequiresExisting"] = "The selected setting could not be located for update.",
            ["Module.Admin.Restore.Status.NoSelection"] = "Select a setting to restore.",
            ["Module.Admin.Restore.Status.NotFound"] = "Unable to resolve the selected setting.",
            ["Module.Admin.Restore.Status.ConfirmationDeclined"] = "Restore skipped for \"{0}\".",
            ["Module.Admin.Restore.Status.SignatureFailed"] = "Electronic signature capture failed: {0}",
            ["Module.Admin.Restore.Status.SignatureCancelled"] = "Electronic signature cancelled.",
            ["Module.Admin.Restore.Status.SignatureMissing"] = "Electronic signature not captured.",
            ["Module.Admin.Restore.Status.SignaturePersistFailed"] = "Failed to persist electronic signature: {0}",
            ["Module.Admin.Restore.Status.SignatureCaptured"] = "Electronic signature captured ({0}).",
            ["Module.Admin.Restore.Status.SignatureCaptured.Unknown"] = "unspecified reason",
            ["Module.Admin.Restore.Status.Success"] = "Setting \"{0}\" restored to default.",
            ["Module.Admin.Restore.Status.Failure"] = "Failed to restore \"{0}\": {1}",
            ["Module.Admin.Restore.Status.AuditFailed"] = "Failed to log restore audit.",
            ["Module.Admin.Restore.Confirm.Title"] = "Restore Setting",
            ["Module.Admin.Restore.Confirm.Message"] = "Restore \"{0}\" to its default value?",
            ["Module.Admin.Restore.Confirm.Accept"] = "Restore",
            ["Module.Admin.Restore.Confirm.Cancel"] = "Cancel",
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

    private sealed class StubDialogService : IDialogService
    {
        public bool ConfirmationResult { get; set; } = true;

        public Exception? ConfirmationException { get; set; }
            = null;

        public Task ShowAlertAsync(string title, string message, string cancel)
            => Task.CompletedTask;

        public Task<bool> ShowConfirmationAsync(string title, string message, string accept, string cancel)
        {
            if (ConfirmationException is not null)
            {
                throw ConfirmationException;
            }

            return Task.FromResult(ConfirmationResult);
        }

        public Task<string?> ShowActionSheetAsync(string title, string cancel, string? destruction, params string[] buttons)
            => Task.FromResult<string?>(null);

        public Task<T?> ShowDialogAsync<T>(string dialogId, object? parameter = null, CancellationToken cancellationToken = default)
            => Task.FromResult<T?>(default);
    }

    private sealed class StubAuthContext : IAuthContext
    {
        public StubAuthContext()
        {
            CurrentUser = new User { Id = 7, Username = "tester" };
            CurrentSessionId = "session-123";
            CurrentDeviceInfo = "UNITTEST";
            CurrentIpAddress = "127.0.0.1";
        }

        public User? CurrentUser { get; set; }

        public string CurrentSessionId { get; set; }

        public string CurrentDeviceInfo { get; set; }

        public string CurrentIpAddress { get; set; }
    }
}
