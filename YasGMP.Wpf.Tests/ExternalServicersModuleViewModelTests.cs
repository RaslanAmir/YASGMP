using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.Tests.TestDoubles;
using YasGMP.Wpf.ViewModels.Modules;
using MySqlConnector;

namespace YasGMP.Wpf.Tests;

public class ExternalServicersModuleViewModelTests
{
    [Fact]
    public async Task OnSaveAsync_AddMode_PersistsServicer()
    {
        const int adapterSignatureId = 3344;
        var service = new FakeExternalServicerCrudService
        {
            SignatureMetadataIdSource = _ => adapterSignatureId
        };
        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 7, FullName = "QA" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "127.0.0.1",
            CurrentSessionId = "session"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var database = CreateDatabaseService(Array.Empty<ContractorIntervention>());

        var viewModel = new ExternalServicersModuleViewModel(database, service, auth, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.Mode = FormMode.Add;
        viewModel.Editor.Name = "Contoso Labs";
        viewModel.Editor.Email = "labs@contoso.example";
        viewModel.Editor.Type = "Calibration";
        viewModel.Editor.Status = "Active";
        viewModel.Editor.ContactPerson = "Ivana";

        var saved = await InvokeSaveAsync(viewModel);

        Assert.True(saved);
        Assert.Equal("Electronic signature captured (QA Reason).", viewModel.StatusMessage);
        Assert.Single(service.Saved);
        var servicer = service.Saved[0];
        Assert.Equal("Contoso Labs", servicer.Name);
        Assert.Equal("calibration", servicer.Type?.ToLowerInvariant());
        Assert.Equal("labs@contoso.example", servicer.Email);
        Assert.Equal("test-signature", servicer.DigitalSignature);
        var context = Assert.Single(service.SavedContexts);
        Assert.Equal("test-signature", context.SignatureHash);
        Assert.Equal("password", context.SignatureMethod);
        Assert.Equal("valid", context.SignatureStatus);
        Assert.Equal("Automated test", context.SignatureNote);
        Assert.Collection(signatureDialog.Requests, ctx =>
        {
            Assert.Equal("external_contractors", ctx.TableName);
            Assert.Equal(0, ctx.RecordId);
        });
        var capturedResult = Assert.Single(signatureDialog.CapturedResults);
        var signatureResult = Assert.NotNull(capturedResult);
        Assert.Equal(service.Saved[0].Id, signatureResult.Signature.RecordId);
        Assert.Equal(adapterSignatureId, signatureResult.Signature.Id);
        Assert.Empty(signatureDialog.PersistedResults);
        Assert.Equal(0, signatureDialog.PersistInvocationCount);
    }

    [Fact]
    public async Task Cancel_UpdateMode_RestoresSnapshot()
    {
        var service = new FakeExternalServicerCrudService();
        service.Saved.Add(new ExternalServicer
        {
            Id = 5,
            Name = "Globex",
            Email = "service@globex.example",
            Type = "Maintenance",
            Status = "active"
        });

        var auth = new TestAuthContext();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var database = CreateDatabaseService(Array.Empty<ContractorIntervention>());

        var viewModel = new ExternalServicersModuleViewModel(database, service, auth, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.SelectedRecord = viewModel.Records.First();
        await viewModel.EnterUpdateModeCommand.ExecuteAsync(null);
        var original = viewModel.Editor.Name;
        viewModel.Editor.Name = "Updated";

        viewModel.CancelCommand.Execute(null);

        Assert.Equal(original, viewModel.Editor.Name);
    }

    [Fact]
    public async Task ModeTransitions_ToggleEditorEnablement()
    {
        var service = new FakeExternalServicerCrudService();
        var auth = new TestAuthContext();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var database = CreateDatabaseService(Array.Empty<ContractorIntervention>());

        var viewModel = new ExternalServicersModuleViewModel(database, service, auth, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        Assert.False(viewModel.IsEditorEnabled);

        await viewModel.EnterAddModeCommand.ExecuteAsync(null);
        Assert.True(viewModel.IsEditorEnabled);

        viewModel.CancelCommand.Execute(null);
        Assert.False(viewModel.IsEditorEnabled);
    }

    [Fact]
    public async Task RefreshOversight_PopulatesMetricsForSelectedServicer()
    {
        var interventions = new[]
        {
            new ContractorIntervention
            {
                Id = 1,
                ContractorId = 5,
                ComponentId = 10,
                InterventionDate = DateTime.UtcNow.AddDays(-5),
                Reason = "Calibration",
                Result = "Passed",
                GmpCompliance = true,
                InterventionType = "Calibration",
                Status = "open",
                StartDate = DateTime.UtcNow.AddDays(-6),
                EndDate = DateTime.UtcNow.AddDays(-5).AddHours(4)
            },
            new ContractorIntervention
            {
                Id = 2,
                ContractorId = 6,
                ComponentId = 11,
                InterventionDate = DateTime.UtcNow.AddDays(-15),
                Reason = "Maintenance",
                Result = "In progress",
                GmpCompliance = false,
                InterventionType = "Repair",
                Status = "pending",
                StartDate = DateTime.UtcNow.AddDays(-16)
            }
        };

        var database = CreateDatabaseService(interventions);
        var service = new FakeExternalServicerCrudService();
        service.Saved.Add(new ExternalServicer
        {
            Id = 5,
            Name = "Contoso Calibration",
            Email = "contoso@example.com",
            Status = "Active"
        });
        service.Saved.Add(new ExternalServicer
        {
            Id = 6,
            Name = "Globex Maintenance",
            Email = "globex@example.com",
            Status = "Active"
        });

        var auth = new TestAuthContext();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new ExternalServicersModuleViewModel(database, service, auth, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.SelectedRecord = viewModel.Records.First(r => r.Key == "5");
        await viewModel.RefreshOversightCommand.ExecuteAsync(null);

        Assert.NotEmpty(viewModel.OversightMetrics);
        Assert.Contains(viewModel.OversightMetrics, metric => metric.Title.Contains("Total", StringComparison.OrdinalIgnoreCase));
        Assert.NotEmpty(viewModel.InterventionTimeline);
        Assert.NotEmpty(viewModel.OversightAnalytics);
    }

    [Fact]
    public async Task RefreshOversight_ComputesDetailedMetricsAndAnalytics()
    {
        var now = DateTime.UtcNow;
        var interventions = new[]
        {
            new ContractorIntervention
            {
                Id = 1,
                ContractorId = 42,
                InterventionDate = now.AddDays(-2),
                Status = "Open",
                GmpCompliance = true,
                InterventionType = "Calibration",
                Reason = "Annual",
                Result = "Scheduled",
                StartDate = now.AddDays(-3),
                EndDate = now.AddDays(5)
            },
            new ContractorIntervention
            {
                Id = 2,
                ContractorId = 42,
                InterventionDate = now.AddDays(-10),
                Status = "Pending",
                GmpCompliance = false,
                InterventionType = "Audit",
                Reason = "Investigation",
                StartDate = now.AddDays(-11)
            },
            new ContractorIntervention
            {
                Id = 3,
                ContractorId = 42,
                InterventionDate = now.AddDays(-20),
                Status = "Closed",
                GmpCompliance = true,
                InterventionType = "Repair",
                Reason = "Breakdown",
                Result = "Completed",
                StartDate = now.AddDays(-22),
                EndDate = now.AddDays(-19)
            },
            new ContractorIntervention
            {
                Id = 4,
                ContractorId = 99,
                InterventionDate = now.AddDays(-1),
                Status = "Open"
            }
        };

        var database = CreateDatabaseService(interventions);
        var service = new FakeExternalServicerCrudService();
        service.Saved.Add(new ExternalServicer
        {
            Id = 42,
            Name = "Precision Oversight",
            Email = "precision@example.com",
            Status = "Active"
        });

        var auth = new TestAuthContext();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new YasGMP.Wpf.Tests.TestDoubles.TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new ExternalServicersModuleViewModel(database, service, auth, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.SelectedRecord = viewModel.Records.Single(r => r.Key == "42");
        await Task.Delay(10);

        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            await viewModel.RefreshOversightCommand.ExecuteAsync(null);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }

        Assert.Equal(4, viewModel.OversightMetrics.Count);
        Assert.Equal("3", viewModel.OversightMetrics[0].FormattedValue);
        Assert.Equal("2", viewModel.OversightMetrics[1].FormattedValue);
        Assert.Equal("1", viewModel.OversightMetrics[2].FormattedValue);
        Assert.StartsWith("5.5", viewModel.OversightMetrics[3].FormattedValue, StringComparison.OrdinalIgnoreCase);

        Assert.Equal(3, viewModel.InterventionTimeline.Count);
        Assert.Equal(interventions[0].InterventionDate, viewModel.InterventionTimeline[0].Timestamp);
        Assert.Contains("Calibration", viewModel.InterventionTimeline[0].Summary, StringComparison.OrdinalIgnoreCase);

        Assert.Equal(4, viewModel.OversightAnalytics.Count);
        Assert.Contains("Open", viewModel.OversightAnalytics[0].Value, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("67", viewModel.OversightAnalytics[1].Value, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("6.0", viewModel.OversightAnalytics[2].Value, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DrillIntoOversightCommand_NavigatesToScheduling()
    {
        var interventions = new[]
        {
            new ContractorIntervention
            {
                Id = 1,
                ContractorId = 7,
                InterventionDate = DateTime.UtcNow.AddDays(-1),
                Status = "Open"
            }
        };

        var database = CreateDatabaseService(interventions);
        var service = new FakeExternalServicerCrudService();
        service.Saved.Add(new ExternalServicer
        {
            Id = 7,
            Name = "Field Ops",
            Email = "field@example.com",
            Status = "Active"
        });

        var auth = new TestAuthContext();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new YasGMP.Wpf.Tests.TestDoubles.TestShellInteractionService();
        var navigation = new RecordingModuleNavigationService();

        var viewModel = new ExternalServicersModuleViewModel(database, service, auth, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.SelectedRecord = viewModel.Records.Single(r => r.Key == "7");
        await Task.Delay(10);
        await viewModel.RefreshOversightCommand.ExecuteAsync(null);

        Assert.True(viewModel.DrillIntoOversightCommand.CanExecute(null));
        viewModel.DrillIntoOversightCommand.Execute(null);

        var opened = Assert.Single(navigation.OpenedModules);
        Assert.Equal(SchedulingModuleViewModel.ModuleKey, opened.ModuleKey);
        Assert.Equal("7", opened.Parameter);
        Assert.NotNull(navigation.LastActivated);
        Assert.Contains(viewModel.SelectedRecord.Title, viewModel.StatusMessage);
    }

    private static Task<bool> InvokeSaveAsync(ExternalServicersModuleViewModel viewModel)
    {
        var method = typeof(ExternalServicersModuleViewModel)
            .GetMethod("OnSaveAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(ExternalServicersModuleViewModel), "OnSaveAsync");
        return (Task<bool>)method.Invoke(viewModel, null)!;
    }

    private static DatabaseService CreateDatabaseService(IEnumerable<ContractorIntervention> interventions)
    {
        var database = new DatabaseService("Server=stub;Database=stub;Uid=stub;Pwd=stub;");
        var table = CreateInterventionTable(interventions);
        SetExecuteSelectOverride(database, (_, _, _) => Task.FromResult(table.Copy()));
        return database;
    }

    private static DataTable CreateInterventionTable(IEnumerable<ContractorIntervention> interventions)
    {
        var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("contractor_id", typeof(int));
        table.Columns.Add("component_id", typeof(int));
        table.Columns.Add("intervention_date", typeof(DateTime));
        table.Columns.Add("reason", typeof(string));
        table.Columns.Add("result", typeof(string));
        table.Columns.Add("gmp_compliance", typeof(bool));
        table.Columns.Add("doc_file", typeof(string));
        table.Columns.Add("contractor_name", typeof(string));
        table.Columns.Add("asset_name", typeof(string));
        table.Columns.Add("intervention_type", typeof(string));
        table.Columns.Add("status", typeof(string));
        table.Columns.Add("start_date", typeof(DateTime));
        table.Columns.Add("end_date", typeof(DateTime));
        table.Columns.Add("notes", typeof(string));

        foreach (var intervention in interventions)
        {
            table.Rows.Add(
                intervention.Id,
                intervention.ContractorId,
                intervention.ComponentId,
                intervention.InterventionDate,
                intervention.Reason ?? (object)DBNull.Value,
                intervention.Result ?? (object)DBNull.Value,
                intervention.GmpCompliance,
                intervention.DocFile ?? (object)DBNull.Value,
                intervention.ContractorName ?? (object)DBNull.Value,
                intervention.AssetName ?? (object)DBNull.Value,
                intervention.InterventionType ?? (object)DBNull.Value,
                intervention.Status ?? (object)DBNull.Value,
                intervention.StartDate ?? (object)DBNull.Value,
                intervention.EndDate ?? (object)DBNull.Value,
                intervention.Notes ?? (object)DBNull.Value);
        }

        return table;
    }

    private static void SetExecuteSelectOverride(
        DatabaseService database,
        Func<string, IEnumerable<MySqlParameter>?, CancellationToken, Task<DataTable>> factory)
    {
        var property = typeof(DatabaseService)
            .GetProperty("ExecuteSelectOverride", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMemberException(nameof(DatabaseService), "ExecuteSelectOverride");
        property.SetValue(database, factory);
    }

    private sealed class TestCflDialogService : ICflDialogService
    {
        public Task<CflResult?> ShowAsync(CflRequest request) => Task.FromResult<CflResult?>(null);
    }

    private sealed class TestShellInteractionService : IShellInteractionService
    {
        public void UpdateStatus(string message)
        {
        }

        public void UpdateInspector(InspectorContext context)
        {
        }
    }

    private sealed class TestModuleNavigationService : IModuleNavigationService
    {
        public ModuleDocumentViewModel OpenModule(string moduleKey, object? parameter = null)
            => throw new NotSupportedException();

        public void Activate(ModuleDocumentViewModel document)
        {
        }
    }

    private sealed class TestAuthContext : IAuthContext
    {
        public User? CurrentUser { get; set; }

        public string CurrentSessionId { get; set; } = Guid.NewGuid().ToString("N");

        public string CurrentDeviceInfo { get; set; } = "UnitTest";

        public string CurrentIpAddress { get; set; } = "127.0.0.1";
    }
}
