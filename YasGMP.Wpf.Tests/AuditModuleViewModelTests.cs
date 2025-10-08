using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using YasGMP.Models.DTO;
using YasGMP.Services;
using YasGMP.Wpf.ViewModels;
using YasGMP.Wpf.ViewModels.Modules;
using YasGMP.Wpf.Tests.TestDoubles;

namespace YasGMP.Wpf.Tests;

public class AuditModuleViewModelTests
{
    [Fact]
    public async Task InitializeAsync_LoadsAuditEntries_WithInspectorDetails()
    {
        var database = CreateDatabaseService();
        var auditService = new AuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new StubShellInteractionService();
        var navigation = new StubModuleNavigationService();
        var exportService = CreateExportService(database, auditService);

        var sampleTimestamp = new DateTime(2025, 1, 15, 8, 30, 0, DateTimeKind.Utc);
        var audits = new[]
        {
            new AuditEntryDto
            {
                Id = 42,
                Entity = "work_orders",
                EntityId = "77",
                Action = "UPDATE",
                Timestamp = sampleTimestamp,
                Username = "qa.user",
                UserId = 9,
                IpAddress = "10.0.0.5",
                DeviceInfo = "OS=Windows; Host=QA-WS",
                DigitalSignature = "qa-signature",
                SignatureHash = "AABBCCDDEE",
                Note = "Status changed to CLOSED",
                Status = "audit"
            }
        };

        var viewModel = new TestAuditModuleViewModel(database, auditService, exportService, cfl, shell, navigation, audits);
        viewModel.FilterUser = "qa";
        viewModel.FilterEntity = "work_orders";
        viewModel.SelectedAction = "UPDATE";
        viewModel.FilterFrom = new DateTime(2025, 1, 1);
        viewModel.FilterTo = new DateTime(2025, 1, 31);

        viewModel.StatusMessage = "Placeholder";

        await viewModel.RefreshAsync();

        Assert.Single(viewModel.Records);
        var record = viewModel.Records.First();
        Assert.Equal("work_orders #77", record.Title);
        Assert.Equal("UPDATE", record.Code);
        Assert.Equal("audit", record.Status);
        Assert.Equal("Status changed to CLOSED", record.Description);
        Assert.Equal("qa.user (#9)", record.InspectorFields[1].Value);
        Assert.Equal("10.0.0.5", record.InspectorFields[4].Value);
        Assert.Equal("qa-signature", record.InspectorFields[7].Value);
        Assert.Equal("AABBCCDDEE", record.InspectorFields[8].Value);
        Assert.Equal("Loaded 1 audit entry.", viewModel.StatusMessage);
        Assert.True(viewModel.HasResults);
        Assert.False(viewModel.HasError);

        Assert.Equal("qa", viewModel.LastUserFilter);
        Assert.Equal("work_orders", viewModel.LastEntityFilter);
        Assert.Equal("UPDATE", viewModel.LastActionFilter);
        Assert.Equal(viewModel.FilterFrom!.Value, viewModel.LastFromFilter);
        var expectedTo = viewModel.FilterTo!.Value.Date.AddDays(1).AddTicks(-1);
        Assert.Equal(expectedTo, viewModel.LastToFilter);

        viewModel.SelectedRecord = record;
        Assert.NotNull(shell.LastContext);
        var inspectorContext = shell.LastContext!;
        Assert.Equal(AuditModuleViewModel.ModuleKey, inspectorContext.ModuleKey);
        var timestampField = Assert.Single(inspectorContext.Fields.Where(f => f.Label == "Timestamp"));
        var moduleToken = AutomationIdSanitizer.Normalize(inspectorContext.ModuleKey, "module");
        var recordToken = AutomationIdSanitizer.Normalize(inspectorContext.RecordKey, "record");
        var labelToken = AutomationIdSanitizer.Normalize(timestampField.Label, "field");
        var expectedAutomationId = $"Dock.Inspector.{moduleToken}.{recordToken}.{labelToken}";
        Assert.Equal(expectedAutomationId, timestampField.AutomationId);
        Assert.Contains("Timestamp", timestampField.AutomationName, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Timestamp", timestampField.AutomationTooltip, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FilterUser_WhenChangedAfterInitialization_TriggersRefreshAndResetsState()
    {
        var database = CreateDatabaseService();
        var auditService = new AuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new StubShellInteractionService();
        var navigation = new StubModuleNavigationService();
        var exportService = CreateExportService(database, auditService);

        var audits = new[]
        {
            new AuditEntryDto { Id = 1, Entity = "systems", Action = "UPDATE", Timestamp = DateTime.UtcNow }
        };

        var viewModel = new TestAuditModuleViewModel(database, auditService, exportService, cfl, shell, navigation, audits);

        await viewModel.InitializeAsync();
        Assert.True(viewModel.IsInitialized);

        var initialCount = viewModel.RefreshInvocationCount;

        viewModel.HasError = true;
        viewModel.HasResults = true;
        viewModel.StatusMessage = "stale";

        var refreshAwaiter = viewModel.AwaitNextRefreshAsync();

        viewModel.FilterUser = "qa";

        await refreshAwaiter;

        Assert.Equal(initialCount + 1, viewModel.RefreshInvocationCount);
        var snapshot = viewModel.RefreshSnapshots.Last();
        Assert.False(snapshot.HasError);
        Assert.False(snapshot.HasResults);
        Assert.Equal("Loading Audit Trail records...", snapshot.StatusMessage);
        Assert.Equal("qa", viewModel.LastUserFilter);
    }

    [Fact]
    public async Task FilterEntity_WhenChangedAfterInitialization_TriggersRefreshAndResetsState()
    {
        var database = CreateDatabaseService();
        var auditService = new AuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new StubShellInteractionService();
        var navigation = new StubModuleNavigationService();
        var exportService = CreateExportService(database, auditService);

        var audits = new[]
        {
            new AuditEntryDto { Id = 7, Entity = "systems", Action = "UPDATE", Timestamp = DateTime.UtcNow }
        };

        var viewModel = new TestAuditModuleViewModel(database, auditService, exportService, cfl, shell, navigation, audits);

        await viewModel.InitializeAsync();
        Assert.True(viewModel.IsInitialized);

        var initialCount = viewModel.RefreshInvocationCount;

        viewModel.HasError = true;
        viewModel.HasResults = true;
        viewModel.StatusMessage = "stale";

        var refreshAwaiter = viewModel.AwaitNextRefreshAsync();

        viewModel.FilterEntity = "work_orders";

        await refreshAwaiter;

        Assert.Equal(initialCount + 1, viewModel.RefreshInvocationCount);
        var snapshot = viewModel.RefreshSnapshots.Last();
        Assert.False(snapshot.HasError);
        Assert.False(snapshot.HasResults);
        Assert.Equal("Loading Audit Trail records...", snapshot.StatusMessage);
        Assert.Equal("work_orders", viewModel.LastEntityFilter);
    }

    [Fact]
    public async Task SelectedAction_WhenChangedAfterInitialization_TriggersRefreshAndResetsState()
    {
        var database = CreateDatabaseService();
        var auditService = new AuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new StubShellInteractionService();
        var navigation = new StubModuleNavigationService();
        var exportService = CreateExportService(database, auditService);

        var audits = new[]
        {
            new AuditEntryDto { Id = 8, Entity = "systems", Action = "UPDATE", Timestamp = DateTime.UtcNow }
        };

        var viewModel = new TestAuditModuleViewModel(database, auditService, exportService, cfl, shell, navigation, audits);

        await viewModel.InitializeAsync();
        Assert.True(viewModel.IsInitialized);

        var initialCount = viewModel.RefreshInvocationCount;

        viewModel.HasError = true;
        viewModel.HasResults = true;
        viewModel.StatusMessage = "stale";

        var refreshAwaiter = viewModel.AwaitNextRefreshAsync();

        viewModel.SelectedAction = "UPDATE";

        await refreshAwaiter;

        Assert.Equal(initialCount + 1, viewModel.RefreshInvocationCount);
        var snapshot = viewModel.RefreshSnapshots.Last();
        Assert.False(snapshot.HasError);
        Assert.False(snapshot.HasResults);
        Assert.Equal("Loading Audit Trail records...", snapshot.StatusMessage);
        Assert.Equal("UPDATE", viewModel.LastActionFilter);
    }

    [Fact]
    public async Task FilterFrom_WhenChangedAfterInitialization_TriggersRefreshAndResetsState()
    {
        var database = CreateDatabaseService();
        var auditService = new AuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new StubShellInteractionService();
        var navigation = new StubModuleNavigationService();
        var exportService = CreateExportService(database, auditService);

        var audits = new[]
        {
            new AuditEntryDto { Id = 9, Entity = "systems", Action = "UPDATE", Timestamp = DateTime.UtcNow }
        };

        var viewModel = new TestAuditModuleViewModel(database, auditService, exportService, cfl, shell, navigation, audits);

        await viewModel.InitializeAsync();
        Assert.True(viewModel.IsInitialized);

        var initialCount = viewModel.RefreshInvocationCount;

        viewModel.HasError = true;
        viewModel.HasResults = true;
        viewModel.StatusMessage = "stale";

        var refreshAwaiter = viewModel.AwaitNextRefreshAsync();

        var expectedFrom = new DateTime(2025, 1, 1);
        viewModel.FilterFrom = expectedFrom;

        await refreshAwaiter;

        Assert.Equal(initialCount + 1, viewModel.RefreshInvocationCount);
        var snapshot = viewModel.RefreshSnapshots.Last();
        Assert.False(snapshot.HasError);
        Assert.False(snapshot.HasResults);
        Assert.Equal("Loading Audit Trail records...", snapshot.StatusMessage);
        Assert.Equal(expectedFrom, viewModel.FilterFrom);
        Assert.Equal(expectedFrom, viewModel.LastFromFilter);
    }

    [Fact]
    public async Task FilterTo_WhenChangedAfterInitialization_TriggersRefreshAndResetsState()
    {
        var database = CreateDatabaseService();
        var auditService = new AuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new StubShellInteractionService();
        var navigation = new StubModuleNavigationService();
        var exportService = CreateExportService(database, auditService);

        var audits = new[]
        {
            new AuditEntryDto { Id = 10, Entity = "systems", Action = "UPDATE", Timestamp = DateTime.UtcNow }
        };

        var viewModel = new TestAuditModuleViewModel(database, auditService, exportService, cfl, shell, navigation, audits);

        await viewModel.InitializeAsync();
        Assert.True(viewModel.IsInitialized);

        var initialCount = viewModel.RefreshInvocationCount;

        viewModel.HasError = true;
        viewModel.HasResults = true;
        viewModel.StatusMessage = "stale";

        var refreshAwaiter = viewModel.AwaitNextRefreshAsync();

        var expectedTo = new DateTime(2025, 1, 31);
        viewModel.FilterTo = expectedTo;

        await refreshAwaiter;

        Assert.Equal(initialCount + 1, viewModel.RefreshInvocationCount);
        var snapshot = viewModel.RefreshSnapshots.Last();
        Assert.False(snapshot.HasError);
        Assert.False(snapshot.HasResults);
        Assert.Equal("Loading Audit Trail records...", snapshot.StatusMessage);
        Assert.Equal(expectedTo, viewModel.FilterTo);
        Assert.Equal(expectedTo.Date.AddDays(1).AddTicks(-1), viewModel.LastToFilter);
    }

    [Fact]
    public async Task InitializeAsync_NoAudits_SetsEmptyStatusMessage()
    {
        var database = CreateDatabaseService();
        var auditService = new AuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new StubShellInteractionService();
        var navigation = new StubModuleNavigationService();
        var exportService = CreateExportService(database, auditService);

        var viewModel = new TestAuditModuleViewModel(database, auditService, exportService, cfl, shell, navigation, Array.Empty<AuditEntryDto>());

        viewModel.StatusMessage = "Placeholder";

        await viewModel.RefreshAsync();

        Assert.Empty(viewModel.Records);
        Assert.Equal("No audit entries match the current filters.", viewModel.StatusMessage);
        Assert.Equal(string.Empty, viewModel.LastActionFilter);
        Assert.False(viewModel.HasResults);
        Assert.False(viewModel.HasError);
    }

    [Fact]
    public async Task InitializeAsync_MultipleAudits_UsesPluralizedStatusMessage()
    {
        var database = CreateDatabaseService();
        var auditService = new AuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new StubShellInteractionService();
        var navigation = new StubModuleNavigationService();
        var exportService = CreateExportService(database, auditService);

        var audits = new[]
        {
            new AuditEntryDto { Id = 1, Entity = "systems", Action = "CREATE", Timestamp = DateTime.UtcNow },
            new AuditEntryDto { Id = 2, Entity = "systems", Action = "UPDATE", Timestamp = DateTime.UtcNow }
        };

        var viewModel = new TestAuditModuleViewModel(database, auditService, exportService, cfl, shell, navigation, audits);

        viewModel.StatusMessage = "Placeholder";

        await viewModel.RefreshAsync();

        Assert.Equal(2, viewModel.Records.Count);
        Assert.Equal("Loaded 2 audit entries.", viewModel.StatusMessage);
        Assert.True(viewModel.HasResults);
        Assert.False(viewModel.HasError);
    }

    [Fact]
    public async Task ExportToPdfCommand_Succeeds_UpdatesStatusAndBusyLifecycle()
    {
        var database = CreateDatabaseService();
        var auditService = new AuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new StubShellInteractionService();
        var navigation = new StubModuleNavigationService();
        var exportService = CreateExportService(database, auditService);

        var audits = new[]
        {
            new AuditEntryDto
            {
                Id = 9,
                Entity = "systems",
                EntityId = "12",
                Action = "EXPORT",
                Timestamp = DateTime.UtcNow
            }
        };

        var busyStates = new List<bool>();
        var viewModel = new TestAuditModuleViewModel(database, auditService, exportService, cfl, shell, navigation, audits)
        {
            PdfExportHandler = (entries, description) =>
            {
                busyStates.Add(viewModel.IsBusy);
                Assert.Same(audits, entries);
                Assert.False(string.IsNullOrWhiteSpace(description));
                return Task.FromResult("C:/exports/audit.pdf");
            }
        };

        await viewModel.RefreshAsync();

        viewModel.HasError = true;
        viewModel.StatusMessage = "stale";

        Assert.True(viewModel.ExportToPdfCommand.CanExecute(null));

        await viewModel.ExportToPdfCommand.ExecuteAsync(null);

        Assert.NotEmpty(busyStates);
        Assert.All(busyStates, state => Assert.True(state));
        Assert.False(viewModel.IsBusy);
        Assert.False(viewModel.HasError);
        Assert.Equal("Audit log exported to PDF: C:/exports/audit.pdf", viewModel.StatusMessage);
        Assert.True(viewModel.ExportToPdfCommand.CanExecute(null));
        Assert.True(viewModel.ExportToExcelCommand.CanExecute(null));
    }

    [Fact]
    public async Task ExportToPdfCommand_WhenExportThrows_SetsErrorAndReenablesCommands()
    {
        var database = CreateDatabaseService();
        var auditService = new AuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new StubShellInteractionService();
        var navigation = new StubModuleNavigationService();
        var exportService = CreateExportService(database, auditService);

        var audits = new[]
        {
            new AuditEntryDto { Id = 10, Entity = "systems", Action = "EXPORT", Timestamp = DateTime.UtcNow }
        };

        var exception = new InvalidOperationException("export failed");
        var viewModel = new TestAuditModuleViewModel(database, auditService, exportService, cfl, shell, navigation, audits)
        {
            PdfExportHandler = (_, _) => throw exception
        };

        await viewModel.RefreshAsync();

        Assert.True(viewModel.ExportToPdfCommand.CanExecute(null));

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => viewModel.ExportToPdfCommand.ExecuteAsync(null));

        Assert.Same(exception, thrown);
        Assert.True(viewModel.HasError);
        Assert.False(viewModel.IsBusy);
        Assert.Equal("Failed to export audit log to PDF: export failed", viewModel.StatusMessage);
        Assert.True(viewModel.ExportToPdfCommand.CanExecute(null));
        Assert.True(viewModel.ExportToExcelCommand.CanExecute(null));
    }

    [Fact]
    public async Task ExportToExcelCommand_Succeeds_UpdatesStatusAndBusyLifecycle()
    {
        var database = CreateDatabaseService();
        var auditService = new AuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new StubShellInteractionService();
        var navigation = new StubModuleNavigationService();
        var exportService = CreateExportService(database, auditService);

        var audits = new[]
        {
            new AuditEntryDto
            {
                Id = 15,
                Entity = "systems",
                EntityId = "55",
                Action = "EXPORT",
                Timestamp = DateTime.UtcNow
            }
        };

        var busyStates = new List<bool>();
        var viewModel = new TestAuditModuleViewModel(database, auditService, exportService, cfl, shell, navigation, audits)
        {
            ExcelExportHandler = (entries, description) =>
            {
                busyStates.Add(viewModel.IsBusy);
                Assert.Same(audits, entries);
                Assert.False(string.IsNullOrWhiteSpace(description));
                return Task.FromResult("C:/exports/audit.xlsx");
            }
        };

        await viewModel.RefreshAsync();

        viewModel.HasError = true;
        viewModel.StatusMessage = "stale";

        Assert.True(viewModel.ExportToExcelCommand.CanExecute(null));

        await viewModel.ExportToExcelCommand.ExecuteAsync(null);

        Assert.NotEmpty(busyStates);
        Assert.All(busyStates, state => Assert.True(state));
        Assert.False(viewModel.IsBusy);
        Assert.False(viewModel.HasError);
        Assert.Equal("Audit log exported to Excel: C:/exports/audit.xlsx", viewModel.StatusMessage);
        Assert.True(viewModel.ExportToPdfCommand.CanExecute(null));
        Assert.True(viewModel.ExportToExcelCommand.CanExecute(null));
    }

    [Fact]
    public async Task ExportToExcelCommand_WhenExportThrows_SetsErrorAndReenablesCommands()
    {
        var database = CreateDatabaseService();
        var auditService = new AuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new StubShellInteractionService();
        var navigation = new StubModuleNavigationService();
        var exportService = CreateExportService(database, auditService);

        var audits = new[]
        {
            new AuditEntryDto { Id = 16, Entity = "systems", Action = "EXPORT", Timestamp = DateTime.UtcNow }
        };

        var exception = new InvalidOperationException("excel export failed");
        var viewModel = new TestAuditModuleViewModel(database, auditService, exportService, cfl, shell, navigation, audits)
        {
            ExcelExportHandler = (_, _) => throw exception
        };

        await viewModel.RefreshAsync();

        Assert.True(viewModel.ExportToExcelCommand.CanExecute(null));

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => viewModel.ExportToExcelCommand.ExecuteAsync(null));

        Assert.Same(exception, thrown);
        Assert.True(viewModel.HasError);
        Assert.False(viewModel.IsBusy);
        Assert.Equal("Failed to export audit log to Excel: excel export failed", viewModel.StatusMessage);
        Assert.True(viewModel.ExportToPdfCommand.CanExecute(null));
        Assert.True(viewModel.ExportToExcelCommand.CanExecute(null));
    }

    [Fact]
    public async Task InitializeAsync_NormalizesDateFiltersBeforeQuery()
    {
        var database = CreateDatabaseService();
        var auditService = new AuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new StubShellInteractionService();
        var navigation = new StubModuleNavigationService();
        var exportService = CreateExportService(database, auditService);

        var audits = new[]
        {
            new AuditEntryDto
            {
                Id = 5,
                Entity = "systems",
                EntityId = "1",
                Action = "CREATE",
                Timestamp = new DateTime(2025, 3, 10, 15, 45, 0, DateTimeKind.Utc)
            }
        };

        var viewModel = new TestAuditModuleViewModel(database, auditService, exportService, cfl, shell, navigation, audits);
        viewModel.FilterFrom = new DateTime(2025, 3, 10, 14, 30, 0);
        viewModel.FilterTo = new DateTime(2025, 3, 15);

        await viewModel.RefreshAsync();

        Assert.Equal(new DateTime(2025, 3, 10), viewModel.LastFromFilter);
        Assert.Equal(new DateTime(2025, 3, 15).Date.AddDays(1).AddTicks(-1), viewModel.LastToFilter);
    }

    [Fact]
    public async Task RefreshAsync_FilterToDateOnly_NormalizesToEndOfDay()
    {
        var database = CreateDatabaseService();
        var auditService = new AuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new StubShellInteractionService();
        var navigation = new StubModuleNavigationService();
        var exportService = CreateExportService(database, auditService);

        var viewModel = new TestAuditModuleViewModel(database, auditService, exportService, cfl, shell, navigation, Array.Empty<AuditEntryDto>());
        viewModel.FilterFrom = new DateTime(2025, 4, 5, 13, 45, 0);
        viewModel.FilterTo = new DateTime(2025, 4, 7);

        await viewModel.RefreshAsync();

        Assert.Equal(new DateTime(2025, 4, 5), viewModel.LastFromFilter);
        Assert.Equal(new DateTime(2025, 4, 7).Date.AddDays(1).AddTicks(-1), viewModel.LastToFilter);
    }

    [Fact]
    public async Task RefreshAsync_FilterToDateOnlyUpperBound_PassesEndOfDayToService()
    {
        var database = CreateDatabaseService();
        var auditService = new AuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new StubShellInteractionService();
        var navigation = new StubModuleNavigationService();
        var exportService = CreateExportService(database, auditService);

        var viewModel = new TestAuditModuleViewModel(database, auditService, exportService, cfl, shell, navigation, Array.Empty<AuditEntryDto>());
        viewModel.FilterFrom = new DateTime(2025, 6, 15, 10, 30, 0);
        viewModel.FilterTo = new DateTime(2025, 6, 17);

        await viewModel.RefreshAsync();

        Assert.Equal(new DateTime(2025, 6, 15), viewModel.LastFromFilter);
        Assert.Equal(new DateTime(2025, 6, 17).Date.AddDays(1).AddTicks(-1), viewModel.LastToFilter);
    }

    [Fact]
    public async Task RefreshAsync_FilterToBeforeFilterFrom_QueriesAcrossFullRange()
    {
        var database = CreateDatabaseService();
        var auditService = new AuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new StubShellInteractionService();
        var navigation = new StubModuleNavigationService();
        var exportService = CreateExportService(database, auditService);

        var viewModel = new TestAuditModuleViewModel(database, auditService, exportService, cfl, shell, navigation, Array.Empty<AuditEntryDto>());
        var later = new DateTime(2025, 5, 10, 10, 30, 0);
        var earlier = new DateTime(2025, 5, 1);
        viewModel.FilterFrom = later;
        viewModel.FilterTo = earlier;

        await viewModel.RefreshAsync();

        var normalizedLater = later.Date;
        var normalizedEarlier = earlier.Date;

        Assert.Equal(normalizedEarlier, viewModel.FilterFrom!.Value);
        Assert.Equal(normalizedLater, viewModel.FilterTo!.Value);
        Assert.Equal(normalizedEarlier, viewModel.LastFromFilter);
        Assert.Equal(normalizedLater.Date.AddDays(1).AddTicks(-1), viewModel.LastToFilter);
        Assert.True(viewModel.LastFromFilter <= viewModel.LastToFilter);
    }

    [Fact]
    public async Task RefreshAsync_SetOnlyFilterToEarlierThanCurrentFrom_PersistsUpperBoundAcrossPropertiesAndQuery()
    {
        var database = CreateDatabaseService();
        var auditService = new AuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new StubShellInteractionService();
        var navigation = new StubModuleNavigationService();
        var exportService = CreateExportService(database, auditService);

        var viewModel = new TestAuditModuleViewModel(database, auditService, exportService, cfl, shell, navigation, Array.Empty<AuditEntryDto>());
        var later = new DateTime(2025, 8, 10, 8, 30, 0);
        var earlier = new DateTime(2025, 8, 5);
        viewModel.FilterFrom = later;
        viewModel.FilterTo = earlier;

        await viewModel.RefreshAsync();

        // User adjusts only the upper bound to an even earlier day without touching FilterFrom.
        var evenEarlier = new DateTime(2025, 7, 20);
        viewModel.FilterTo = evenEarlier;

        await viewModel.RefreshAsync();

        var normalizedLater = later.Date;
        var normalizedEvenEarlier = evenEarlier.Date;

        Assert.Equal(normalizedEvenEarlier, viewModel.FilterFrom!.Value);
        Assert.Equal(normalizedLater, viewModel.FilterTo!.Value);
        Assert.Equal(normalizedEvenEarlier, viewModel.LastFromFilter);
        Assert.Equal(normalizedLater.Date.AddDays(1).AddTicks(-1), viewModel.LastToFilter);
        Assert.True(viewModel.LastFromFilter <= viewModel.LastToFilter);
    }

    [Fact]
    public async Task RefreshAsync_DefaultFilterFrom_SetEarlierFilterTo_PreservesUpperBound()
    {
        var database = CreateDatabaseService();
        var auditService = new AuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new StubShellInteractionService();
        var navigation = new StubModuleNavigationService();
        var exportService = CreateExportService(database, auditService);

        var viewModel = new TestAuditModuleViewModel(database, auditService, exportService, cfl, shell, navigation, Array.Empty<AuditEntryDto>());

        var originalFrom = viewModel.FilterFrom!.Value.Date;
        var earlierUpperBound = originalFrom.AddDays(-5);
        viewModel.FilterTo = earlierUpperBound;

        await viewModel.RefreshAsync();

        Assert.Equal(earlierUpperBound, viewModel.FilterFrom!.Value);
        Assert.Equal(originalFrom, viewModel.FilterTo!.Value);
        Assert.Equal(earlierUpperBound, viewModel.LastFromFilter);
        Assert.Equal(originalFrom.AddDays(1).AddTicks(-1), viewModel.LastToFilter);
        Assert.True(viewModel.LastFromFilter <= viewModel.LastToFilter);
    }

    [Fact]
    public async Task RefreshAsync_FilterToUnset_DefaultsToFilterFromEndOfDay()
    {
        var database = CreateDatabaseService();
        var auditService = new AuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new StubShellInteractionService();
        var navigation = new StubModuleNavigationService();
        var exportService = CreateExportService(database, auditService);

        var viewModel = new TestAuditModuleViewModel(database, auditService, exportService, cfl, shell, navigation, Array.Empty<AuditEntryDto>());
        viewModel.FilterFrom = new DateTime(2025, 5, 1, 9, 30, 0);
        viewModel.FilterTo = null;

        await viewModel.RefreshAsync();

        var expectedFrom = new DateTime(2025, 5, 1);
        Assert.Equal(expectedFrom, viewModel.LastFromFilter);
        Assert.Equal(expectedFrom.AddDays(1).AddTicks(-1), viewModel.LastToFilter);
    }

    [Fact]
    public async Task RefreshAsync_FilterToDateOnlyKindUnspecified_NormalizesAndPersistsFilter()
    {
        var database = CreateDatabaseService();
        var auditService = new AuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new StubShellInteractionService();
        var navigation = new StubModuleNavigationService();

        var viewModel = new TestAuditModuleViewModel(database, auditService, exportService, cfl, shell, navigation, Array.Empty<AuditEntryDto>());
        viewModel.FilterFrom = new DateTime(2025, 7, 1, 15, 30, 0);
        viewModel.FilterTo = DateTime.SpecifyKind(new DateTime(2025, 7, 5), DateTimeKind.Unspecified);

        await viewModel.RefreshAsync();

        Assert.Equal(new DateTime(2025, 7, 1), viewModel.LastFromFilter);
        Assert.Equal(new DateTime(2025, 7, 5).AddDays(1).AddTicks(-1), viewModel.LastToFilter);
        Assert.Equal(new DateTime(2025, 7, 1), viewModel.FilterFrom!.Value);
        Assert.Equal(new DateTime(2025, 7, 5), viewModel.FilterTo!.Value);
    }

    [Fact]
    public async Task RefreshAsync_WhenServiceThrows_SetsErrorState()
    {
        var database = CreateDatabaseService();
        var auditService = new AuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new StubShellInteractionService();
        var navigation = new StubModuleNavigationService();

        var exportService = CreateExportService(database, auditService);

        var viewModel = new ThrowingAuditModuleViewModel(
            database,
            auditService,
            exportService,
            cfl,
            shell,
            navigation,
            new InvalidOperationException("forced failure"));

        await viewModel.RefreshAsync();

        Assert.True(viewModel.HasError);
        Assert.False(viewModel.HasResults);
        Assert.NotEmpty(viewModel.Records);
        Assert.StartsWith("Offline data loaded because:", viewModel.StatusMessage);
        Assert.Contains("forced failure", viewModel.StatusMessage);
    }

    private static DatabaseService CreateDatabaseService()
        => new("Server=localhost;Database=unit_test;Uid=test;Pwd=test;");

    private static ExportService CreateExportService(DatabaseService databaseService, AuditService auditService)
        => new(databaseService, auditService);

    private sealed class TestAuditModuleViewModel : AuditModuleViewModel
    {
        public readonly record struct RefreshInvocationSnapshot(bool HasError, bool HasResults, string? StatusMessage);

        private readonly IReadOnlyList<AuditEntryDto> _entries;
        private readonly List<RefreshInvocationSnapshot> _refreshSnapshots = new();
        private TaskCompletionSource<object?>? _refreshSignal;

        public TestAuditModuleViewModel(
            DatabaseService databaseService,
            AuditService auditService,
            ExportService exportService,
            ICflDialogService cflDialogService,
            IShellInteractionService shellInteraction,
            IModuleNavigationService navigation,
            IReadOnlyList<AuditEntryDto> entries)
            : base(databaseService, auditService, exportService, cflDialogService, shellInteraction, navigation)
        {
            _entries = entries;
        }

        public Func<IReadOnlyList<AuditEntryDto>, string, Task<string>>? PdfExportHandler { get; set; }

        public Func<IReadOnlyList<AuditEntryDto>, string, Task<string>>? ExcelExportHandler { get; set; }

        public string? LastUserFilter { get; private set; }
        public string? LastEntityFilter { get; private set; }
        public string LastActionFilter { get; private set; } = string.Empty;
        public DateTime LastFromFilter { get; private set; }
        public DateTime LastToFilter { get; private set; }
        public int RefreshInvocationCount { get; private set; }
        public IReadOnlyList<RefreshInvocationSnapshot> RefreshSnapshots => _refreshSnapshots;

        public Task AwaitNextRefreshAsync()
        {
            var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            _refreshSignal = tcs;
            return tcs.Task;
        }

        protected override Task<IReadOnlyList<AuditEntryDto>> QueryAuditsAsync(
            string user,
            string entity,
            string action,
            DateTime from,
            DateTime to)
        {
            LastUserFilter = user;
            LastEntityFilter = entity;
            LastActionFilter = action;
            LastFromFilter = from;
            LastToFilter = to;
            RefreshInvocationCount++;
            _refreshSnapshots.Add(new RefreshInvocationSnapshot(HasError, HasResults, StatusMessage));
            _refreshSignal?.TrySetResult(null);
            _refreshSignal = null;
            return Task.FromResult(_entries);
        }

        protected override Task<string> ExportAuditToPdfAsync(IReadOnlyList<AuditEntryDto> entries, string filterDescription)
            => PdfExportHandler?.Invoke(entries, filterDescription)
               ?? Task.FromResult("C:/exports/audit.pdf");

        protected override Task<string> ExportAuditToExcelAsync(IReadOnlyList<AuditEntryDto> entries, string filterDescription)
            => ExcelExportHandler?.Invoke(entries, filterDescription)
               ?? Task.FromResult("C:/exports/audit.xlsx");
    }

    private sealed class ThrowingAuditModuleViewModel : AuditModuleViewModel
    {
        private readonly Exception _exception;

        public ThrowingAuditModuleViewModel(
            DatabaseService databaseService,
            AuditService auditService,
            ExportService exportService,
            ICflDialogService cflDialogService,
            IShellInteractionService shellInteraction,
            IModuleNavigationService navigation,
            Exception exception)
            : base(databaseService, auditService, exportService, cflDialogService, shellInteraction, navigation)
        {
            _exception = exception;
        }

        protected override Task<IReadOnlyList<AuditEntryDto>> QueryAuditsAsync(
            string user,
            string entity,
            string action,
            DateTime from,
            DateTime to)
            => Task.FromException<IReadOnlyList<AuditEntryDto>>(_exception);
    }

}
