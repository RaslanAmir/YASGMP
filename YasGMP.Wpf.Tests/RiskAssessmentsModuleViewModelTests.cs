using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.ViewModels;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.Tests.TestDoubles;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Tests;

public class RiskAssessmentsModuleViewModelTests
{
    [Fact]
    public async Task InitializeAsync_LoadsRiskAssessmentsAndProjectsRecords()
    {
        var database = CreateDatabase();
        SeedRiskAssessments(database);
        var (module, shared) = CreateModule(database);

        await module.InitializeAsync(null).ConfigureAwait(false);

        Assert.Equal(shared.RiskAssessments.Count, module.Records.Count);
        Assert.Equal(shared.StatusMessage, module.StatusMessage);
        var selected = Assert.NotNull(module.SelectedRecord);
        var firstRisk = shared.FilteredRiskAssessments.First();

        Assert.Equal(firstRisk.Id.ToString(CultureInfo.InvariantCulture), selected.Key);
        Assert.Equal(firstRisk.Title, selected.Title);
        Assert.Equal(firstRisk.Code, selected.Code);
        Assert.Contains(selected.InspectorFields, field => field.Value == firstRisk.Category);
        Assert.Contains(selected.InspectorFields, field => field.Value == firstRisk.Owner?.FullName);

        module.Dispose();
    }

    [Fact]
    public async Task SearchStatusCategoryFilters_StayInSyncWithSharedViewModel()
    {
        var database = CreateDatabase();
        SeedRiskAssessments(database);
        var (module, shared) = CreateModule(database);

        await module.InitializeAsync(null).ConfigureAwait(false);

        module.SearchText = "supplier";
        Assert.Equal("supplier", shared.SearchTerm);

        shared.SearchTerm = "equipment";
        Assert.Equal("equipment", module.SearchText);

        module.StatusFilter = "pending_approval";
        Assert.Equal("pending_approval", shared.StatusFilter);
        Assert.All(shared.FilteredRiskAssessments, risk => Assert.Equal("pending_approval", risk.Status));
        Assert.All(module.Records, record => Assert.Equal("Pending Approval", record.Status));

        shared.CategoryFilter = "equipment";
        Assert.Equal("equipment", module.CategoryFilter);

        var expectedKeys = shared.FilteredRiskAssessments
            .Select(risk => risk.Id.ToString(CultureInfo.InvariantCulture))
            .ToList();
        var actualKeys = module.Records.Select(record => record.Key).ToList();
        Assert.Equal(expectedKeys, actualKeys);

        module.Dispose();
    }

    [Fact]
    public async Task ToolbarCommands_InvokeUnderlyingDatabaseWorkflow()
    {
        var database = CreateDatabase();
        SeedRiskAssessments(database);
        var (module, shared) = CreateModule(database, sessionId: "session-risk-tests");

        await module.InitializeAsync(null).ConfigureAwait(false);

        await module.InitiateCommand.ExecuteAsync(null).ConfigureAwait(false);
        Assert.Single(database.RiskAssessmentInitiateCalls);
        Assert.Contains(database.RiskAssessmentAuditEntries, entry => entry.Action == "INITIATE");
        Assert.Equal(SeededRiskCount + 1, database.RiskAssessments.Count);

        var updateTarget = shared.RiskAssessments.First(risk => risk.Status == "in_progress");
        shared.SelectedRiskAssessment = updateTarget;
        shared.SelectedRiskAssessment.Severity = 4;
        shared.SelectedRiskAssessment.Probability = 3;
        shared.SelectedRiskAssessment.Detection = 5;

        await module.UpdateCommand.ExecuteAsync(null).ConfigureAwait(false);
        var updateCall = Assert.Single(database.RiskAssessmentUpdateCalls);
        Assert.Equal(updateTarget.Id, updateCall.Risk.Id);
        Assert.Contains(database.RiskAssessmentAuditEntries, entry => entry.Action == "UPDATE");
        Assert.Equal(updateTarget.RiskScore, database.RiskAssessments.Single(risk => risk.Id == updateTarget.Id).RiskScore);

        var approveTarget = shared.RiskAssessments.First(risk => risk.Status == "pending_approval");
        shared.SelectedRiskAssessment = approveTarget;

        await module.ApproveCommand.ExecuteAsync(null).ConfigureAwait(false);
        var approveCall = Assert.Single(database.RiskAssessmentApproveCalls);
        Assert.Equal(approveTarget.Id, approveCall.RiskId);
        Assert.Contains(database.RiskAssessmentAuditEntries, entry => entry.Action == "APPROVE");
        Assert.Equal("effectiveness_check", database.RiskAssessments.Single(risk => risk.Id == approveTarget.Id).Status);

        var closeTarget = shared.RiskAssessments.First(risk => risk.Status == "effectiveness_check");
        shared.SelectedRiskAssessment = closeTarget;

        await module.CloseCommand.ExecuteAsync(null).ConfigureAwait(false);
        var closeCall = Assert.Single(database.RiskAssessmentCloseCalls);
        Assert.Equal(closeTarget.Id, closeCall.RiskId);
        Assert.Contains(database.RiskAssessmentAuditEntries, entry => entry.Action == "CLOSE");
        Assert.Equal("closed", database.RiskAssessments.Single(risk => risk.Id == closeTarget.Id).Status);

        await module.ExportCommand.ExecuteAsync(null).ConfigureAwait(false);
        var exportCall = Assert.Single(database.RiskAssessmentExportCalls);
        Assert.Equal(shared.FilteredRiskAssessments.Count, exportCall.Items.Count);
        Assert.Contains(database.RiskAssessmentAuditEntries, entry => entry.Action == "EXPORT");
        Assert.Equal(shared.StatusMessage, module.StatusMessage);

        module.Dispose();
    }

    [Fact]
    public async Task StatusMessage_FromSharedViewModel_UpdatesModule()
    {
        var database = CreateDatabase();
        SeedRiskAssessments(database);
        var (module, shared) = CreateModule(database);

        await module.InitializeAsync(null).ConfigureAwait(false);

        shared.StatusMessage = "Workflow refreshed.";
        Assert.Equal("Workflow refreshed.", module.StatusMessage);

        module.Dispose();
    }

    private const int SeededRiskCount = 3;

    private static (RiskAssessmentsModuleViewModel Module, RiskAssessmentViewModel Shared) CreateModule(
        DatabaseService database,
        string? sessionId = null)
    {
        var auditService = new AuditService(database);
        var user = new User
        {
            Id = 91,
            FullName = "QA Manager",
            Username = "qa.manager",
            Role = "qa"
        };

        var authService = AuthServiceTestHelper.CreateAuthenticatedAuthService(
            database,
            auditService,
            user: user,
            sessionId: sessionId ?? "session-risk-module-tests");

        var shared = new RiskAssessmentViewModel(database, authService);
        var localization = CreateLocalizationService();
        var module = new RiskAssessmentsModuleViewModel(
            shared,
            localization,
            new StubCflDialogService(),
            new StubShellInteractionService(),
            new StubModuleNavigationService());

        return (module, shared);
    }

    private static DatabaseService CreateDatabase() => new();

    private static void SeedRiskAssessments(DatabaseService database)
    {
        var today = DateTime.Today;
        database.RiskAssessments.Clear();
        database.RiskAssessments.AddRange(new[]
        {
            new RiskAssessment
            {
                Id = 2001,
                Code = "RA-2001",
                Title = "Process containment review",
                Category = "process",
                Status = "in_progress",
                RiskLevel = "High",
                RiskScore = 81,
                Severity = 3,
                Probability = 3,
                Detection = 9,
                Owner = new User { Id = 41, FullName = "QA Lead", Username = "qa.lead" },
                ReviewDate = today.AddMonths(2),
                AssessedAt = today.AddDays(-14)
            },
            new RiskAssessment
            {
                Id = 2002,
                Code = "RA-2002",
                Title = "Supplier packaging drift",
                Category = "supplier",
                Status = "pending_approval",
                RiskLevel = "Medium",
                RiskScore = 45,
                Severity = 3,
                Probability = 3,
                Detection = 5,
                Owner = new User { Id = 52, FullName = "Supply Chain", Username = "supply.chain" },
                ReviewDate = today.AddMonths(1),
                AssessedAt = today.AddDays(-30)
            },
            new RiskAssessment
            {
                Id = 2003,
                Code = "RA-2003",
                Title = "Equipment calibration backlog",
                Category = "equipment",
                Status = "effectiveness_check",
                RiskLevel = "Low",
                RiskScore = 25,
                Severity = 5,
                Probability = 1,
                Detection = 5,
                Owner = new User { Id = 63, FullName = "Maintenance", Username = "maintenance" },
                ReviewDate = today.AddMonths(3),
                AssessedAt = today.AddDays(-7)
            }
        });
    }

    private static ILocalizationService CreateLocalizationService()
    {
        var resources = new Dictionary<string, IDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Module.Title.RiskAssessments"] = "Risk Assessments",
                ["Module.Description.RiskAssessments"] = "Manage risk assessments",
                ["Module.RiskAssessments.Status.Initiated"] = "Initiated",
                ["Module.RiskAssessments.Status.InProgress"] = "In Progress",
                ["Module.RiskAssessments.Status.PendingApproval"] = "Pending Approval",
                ["Module.RiskAssessments.Status.EffectivenessCheck"] = "Effectiveness Check",
                ["Module.RiskAssessments.Status.Closed"] = "Closed",
                ["Module.RiskAssessments.Status.Rejected"] = "Rejected",
                ["Module.RiskAssessments.Field.Category"] = "Category",
                ["Module.RiskAssessments.Field.Owner"] = "Owner",
                ["Module.RiskAssessments.Field.RiskLevel"] = "Risk Level",
                ["Module.RiskAssessments.Field.RiskScore"] = "Risk Score",
                ["Module.RiskAssessments.Field.ReviewDate"] = "Review Date",
                ["Module.RiskAssessments.Field.Status"] = "Status",
                ["Module.RiskAssessments.Field.AssessedAt"] = "Assessed At"
            }
        };

        return new FakeLocalizationService(resources, "en");
    }
}
