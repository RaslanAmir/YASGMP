using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Xunit;
using YasGMP.Models.DTO;
using YasGMP.Services;
using YasGMP.ViewModels;
using YasGMP.Wpf.ViewModels.Modules;
using YasGMP.Wpf.Tests.TestDoubles;

namespace YasGMP.Wpf.Tests;

public class AuditDashboardDocumentViewModelTests
{
    [Fact]
    public async Task RefreshAsync_ProjectsMauiStateIntoRecordsAndStatus()
    {
        var dashboardAudits = new ObservableCollection<AuditEntryDto>();
        var audits = new List<AuditEntryDto>
        {
            CreateAuditEntry(1, "work_orders", "77", "UPDATE", "qa", 9,
                timestamp: new DateTime(2025, 1, 15, 9, 30, 0, DateTimeKind.Utc),
                note: "Status changed"),
            CreateAuditEntry(2, "work_orders", "78", "DELETE", "qa", 9,
                timestamp: new DateTime(2025, 1, 16, 10, 0, 0, DateTimeKind.Utc),
                note: "Removed duplicate")
        };

        var filterFrom = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var filterTo = new DateTime(2025, 1, 31, 23, 59, 0, DateTimeKind.Utc);

        var dashboard = CreateDashboardViewModel(
            user: "qa",
            entity: "work_orders",
            action: "UPDATE",
            from: filterFrom,
            to: filterTo,
            initialAudits: dashboardAudits,
            pdfCommand: () => Task.CompletedTask,
            excelCommand: () => Task.CompletedTask);

        var loadCalls = new List<(string? user, string? entity, string? action, DateTime from, DateTime to)>();
        Task<IReadOnlyList<AuditEntryDto>> LoadAsync(AuditDashboardViewModel vm)
        {
            loadCalls.Add((vm.FilterUser, vm.FilterEntity, vm.SelectedAction, vm.FilterFrom, vm.FilterTo));
            return Task.FromResult<IReadOnlyList<AuditEntryDto>>(audits);
        }

        var document = CreateDocument(dashboard, loadOverride: LoadAsync);

        await document.InitializeAsync();
        Assert.Empty(document.Records);

        await document.RefreshAsync();

        Assert.Equal(2, document.Records.Count);
        Assert.True(document.HasResults);
        Assert.False(document.HasError);
        Assert.Equal("Loaded 2 audit events.", document.StatusMessage);
        Assert.Single(loadCalls);

        var call = loadCalls.Single();
        Assert.Equal("qa", call.user);
        Assert.Equal("work_orders", call.entity);
        Assert.Equal("UPDATE", call.action);
        Assert.Equal(filterFrom, call.from);
        Assert.Equal(filterTo, call.to);

        var first = document.Records.First();
        Assert.Equal("work_orders #77", first.Title);
        Assert.Equal("UPDATE", first.Code);
        Assert.Equal("Status changed", first.Description);
    }

    [Fact]
    public async Task ExportPdfCommand_UsesMauiCommandAndResetsBusyLifecycle()
    {
        AuditDashboardDocumentViewModel? document = null;
        var busyStates = new List<bool>();
        var dashboard = CreateDashboardViewModel(
            initialAudits: new ObservableCollection<AuditEntryDto>
            {
                CreateAuditEntry(5, "systems", "12", "UPDATE", "ops", 4)
            },
            pdfCommand: () =>
            {
                busyStates.Add(document!.IsBusy);
                return Task.CompletedTask;
            },
            excelCommand: () => Task.CompletedTask);

        document = CreateDocument(dashboard, loadOverride: _ => Task.FromResult<IReadOnlyList<AuditEntryDto>>(dashboard.FilteredAudits.ToList()));

        await document.RefreshAsync();
        Assert.True(document.ExportPdfCommand.CanExecute(null));

        await document.ExportPdfCommand.ExecuteAsync(null);

        Assert.All(busyStates, state => Assert.True(state));
        Assert.False(document.IsBusy);
        Assert.False(document.HasError);
        Assert.Equal("Audit dashboard exported to PDF.", document.StatusMessage);
        Assert.True(document.ExportPdfCommand.CanExecute(null));
        Assert.True(document.ExportExcelCommand.CanExecute(null));
    }

    [Fact]
    public async Task ExportPdfCommand_WhenMauiCommandThrows_SetsErrorAndAllowsRetry()
    {
        var dashboard = CreateDashboardViewModel(
            initialAudits: new ObservableCollection<AuditEntryDto>
            {
                CreateAuditEntry(9, "systems", "22", "EXPORT", "ops", 4)
            },
            pdfCommand: () => Task.FromException(new InvalidOperationException("maui failed")),
            excelCommand: () => Task.CompletedTask);

        var document = CreateDocument(dashboard, loadOverride: _ => Task.FromResult<IReadOnlyList<AuditEntryDto>>(dashboard.FilteredAudits.ToList()));

        await document.RefreshAsync();
        Assert.True(document.ExportPdfCommand.CanExecute(null));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => document.ExportPdfCommand.ExecuteAsync(null));
        Assert.Equal("maui failed", ex.Message);
        Assert.True(document.HasError);
        Assert.False(document.IsBusy);
        Assert.Equal("Failed to export audit dashboard to PDF: maui failed", document.StatusMessage);
        Assert.True(document.ExportPdfCommand.CanExecute(null));
        Assert.True(document.ExportExcelCommand.CanExecute(null));
    }

    [Fact]
    public async Task ExportPdfCommand_FallbackUsesOverrideAndReportsPath()
    {
        var dashboard = CreateDashboardViewModel(
            initialAudits: new ObservableCollection<AuditEntryDto>
            {
                CreateAuditEntry(10, "systems", "30", "EXPORT", "ops", 4)
            },
            pdfCommand: null,
            excelCommand: () => Task.CompletedTask);

        var exportedPaths = new List<string>();
        Task<string> ExportAsync(AuditDashboardViewModel vm)
        {
            var path = $"C:/exports/{vm.FilterEntity ?? "system"}.pdf";
            exportedPaths.Add(path);
            return Task.FromResult(path);
        }

        var document = CreateDocument(dashboard,
            loadOverride: _ => Task.FromResult<IReadOnlyList<AuditEntryDto>>(dashboard.FilteredAudits.ToList()),
            exportPdfOverride: ExportAsync);

        await document.RefreshAsync();
        await document.ExportPdfCommand.ExecuteAsync(null);

        Assert.Single(exportedPaths);
        Assert.Equal("Audit dashboard exported to PDF: C:/exports/systems.pdf", document.StatusMessage);
        Assert.False(document.IsBusy);
        Assert.False(document.HasError);
    }

    [Fact]
    public async Task ExportPdfCommand_FallbackFailureSetsErrorAndReenables()
    {
        var dashboard = CreateDashboardViewModel(
            initialAudits: new ObservableCollection<AuditEntryDto>
            {
                CreateAuditEntry(11, "systems", "40", "EXPORT", "ops", 4)
            },
            pdfCommand: null,
            excelCommand: () => Task.CompletedTask);

        var document = CreateDocument(dashboard,
            loadOverride: _ => Task.FromResult<IReadOnlyList<AuditEntryDto>>(dashboard.FilteredAudits.ToList()),
            exportPdfOverride: _ => Task.FromException<string>(new InvalidOperationException("override failed")));

        await document.RefreshAsync();
        Assert.True(document.ExportPdfCommand.CanExecute(null));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => document.ExportPdfCommand.ExecuteAsync(null));
        Assert.Equal("override failed", ex.Message);
        Assert.True(document.HasError);
        Assert.False(document.IsBusy);
        Assert.Equal("Failed to export audit dashboard to PDF: override failed", document.StatusMessage);
        Assert.True(document.ExportPdfCommand.CanExecute(null));
    }

    [Fact]
    public async Task ExportExcelCommand_FallbackUsesOverrideAndReportsPath()
    {
        var dashboard = CreateDashboardViewModel(
            initialAudits: new ObservableCollection<AuditEntryDto>
            {
                CreateAuditEntry(12, "systems", "50", "EXPORT", "ops", 4)
            },
            pdfCommand: null,
            excelCommand: null);

        Task<string> ExportAsync(AuditDashboardViewModel vm)
            => Task.FromResult($"C:/exports/{vm.FilterEntity ?? "system"}.xlsx");

        var document = CreateDocument(dashboard,
            loadOverride: _ => Task.FromResult<IReadOnlyList<AuditEntryDto>>(dashboard.FilteredAudits.ToList()),
            exportExcelOverride: ExportAsync);

        await document.RefreshAsync();
        await document.ExportExcelCommand.ExecuteAsync(null);

        Assert.Equal("Audit dashboard exported to Excel: C:/exports/systems.xlsx", document.StatusMessage);
        Assert.False(document.IsBusy);
        Assert.False(document.HasError);
    }

    [Fact]
    public async Task CanExecuteExportReflectsHasResultsAndBusy()
    {
        var dashboard = CreateDashboardViewModel(
            initialAudits: new ObservableCollection<AuditEntryDto>
            {
                CreateAuditEntry(13, "systems", "60", "EXPORT", "ops", 4)
            },
            pdfCommand: () => Task.CompletedTask,
            excelCommand: () => Task.CompletedTask);

        var document = CreateDocument(dashboard,
            loadOverride: _ => Task.FromResult<IReadOnlyList<AuditEntryDto>>(dashboard.FilteredAudits.ToList()));

        await document.RefreshAsync();
        Assert.True(document.ExportPdfCommand.CanExecute(null));
        Assert.True(document.ExportExcelCommand.CanExecute(null));

        document.HasResults = false;
        Assert.False(document.ExportPdfCommand.CanExecute(null));
        Assert.False(document.ExportExcelCommand.CanExecute(null));

        document.HasResults = true;
        document.IsBusy = true;
        Assert.False(document.ExportPdfCommand.CanExecute(null));
        Assert.False(document.ExportExcelCommand.CanExecute(null));
    }

    private static AuditDashboardDocumentViewModel CreateDocument(
        AuditDashboardViewModel dashboard,
        Func<AuditDashboardViewModel, Task<IReadOnlyList<AuditEntryDto>>>? loadOverride = null,
        Func<AuditDashboardViewModel, Task<string>>? exportPdfOverride = null,
        Func<AuditDashboardViewModel, Task<string>>? exportExcelOverride = null)
        => new(
            CreateUnused<AuditService>(),
            CreateUnused<ExportService>(),
            dashboard,
            new StubCflDialogService(),
            new StubShellInteractionService(),
            new StubModuleNavigationService(),
            loadOverride,
            exportPdfOverride,
            exportExcelOverride);

    private static AuditDashboardViewModel CreateDashboardViewModel(
        string? user = null,
        string? entity = null,
        string? action = null,
        DateTime? from = null,
        DateTime? to = null,
        ObservableCollection<AuditEntryDto>? initialAudits = null,
        Func<Task>? pdfCommand = null,
        Func<Task>? excelCommand = null)
    {
        var instance = (AuditDashboardViewModel)FormatterServices.GetUninitializedObject(typeof(AuditDashboardViewModel));
        SetAutoProperty(instance, "FilteredAudits", initialAudits ?? new ObservableCollection<AuditEntryDto>());
        SetAutoProperty(instance, "ApplyFilterCommand", new AsyncRelayCommand(() => Task.CompletedTask));
        SetAutoProperty(instance, "ExportPdfCommand", pdfCommand is null ? null : new AsyncRelayCommand(pdfCommand));
        SetAutoProperty(instance, "ExportExcelCommand", excelCommand is null ? null : new AsyncRelayCommand(excelCommand));

        instance.FilterUser = user;
        instance.FilterEntity = entity;
        instance.SelectedAction = action;
        instance.FilterFrom = from ?? DateTime.UtcNow.AddDays(-7);
        instance.FilterTo = to ?? DateTime.UtcNow;

        return instance;
    }

    private static void SetAutoProperty<T>(object target, string propertyName, T value)
    {
        var field = target.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        field!.SetValue(target, value);
    }

    private static T CreateUnused<T>() where T : class
        => (T)FormatterServices.GetUninitializedObject(typeof(T));

    private static AuditEntryDto CreateAuditEntry(
        int id,
        string entity,
        string entityId,
        string action,
        string username,
        int userId,
        DateTime? timestamp = null,
        string? note = null)
        => new()
        {
            Id = id,
            Entity = entity,
            EntityId = entityId,
            Action = action,
            Username = username,
            UserId = userId,
            Timestamp = timestamp ?? DateTime.UtcNow,
            Note = note,
        };
}

