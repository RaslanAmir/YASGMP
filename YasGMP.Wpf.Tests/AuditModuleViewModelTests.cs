using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using YasGMP.Models.DTO;
using YasGMP.Services;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Modules;

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

        var viewModel = new TestAuditModuleViewModel(database, auditService, cfl, shell, navigation, audits);
        viewModel.FilterUser = "qa";
        viewModel.FilterEntity = "work_orders";
        viewModel.SelectedAction = "UPDATE";
        viewModel.FilterFrom = new DateTime(2025, 1, 1);
        viewModel.FilterTo = new DateTime(2025, 1, 31);

        await viewModel.InitializeAsync(null);

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

        Assert.Equal("qa", viewModel.LastUserFilter);
        Assert.Equal("work_orders", viewModel.LastEntityFilter);
        Assert.Equal("UPDATE", viewModel.LastActionFilter);
        Assert.Equal(viewModel.FilterFrom, viewModel.LastFromFilter);
        Assert.Equal(viewModel.FilterTo, viewModel.LastToFilter);
    }

    [Fact]
    public async Task InitializeAsync_NoAudits_SetsEmptyStatusMessage()
    {
        var database = CreateDatabaseService();
        var auditService = new AuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new StubShellInteractionService();
        var navigation = new StubModuleNavigationService();

        var viewModel = new TestAuditModuleViewModel(database, auditService, cfl, shell, navigation, Array.Empty<AuditEntryDto>());

        await viewModel.InitializeAsync(null);

        Assert.Empty(viewModel.Records);
        Assert.Equal("No audit entries match the current filters.", viewModel.StatusMessage);
        Assert.Equal(string.Empty, viewModel.LastActionFilter);
    }

    private static DatabaseService CreateDatabaseService()
        => new("Server=localhost;Database=unit_test;Uid=test;Pwd=test;");

    private sealed class TestAuditModuleViewModel : AuditModuleViewModel
    {
        private readonly IReadOnlyList<AuditEntryDto> _entries;

        public TestAuditModuleViewModel(
            DatabaseService databaseService,
            AuditService auditService,
            ICflDialogService cflDialogService,
            IShellInteractionService shellInteraction,
            IModuleNavigationService navigation,
            IReadOnlyList<AuditEntryDto> entries)
            : base(databaseService, auditService, cflDialogService, shellInteraction, navigation)
        {
            _entries = entries;
        }

        public string? LastUserFilter { get; private set; }
        public string? LastEntityFilter { get; private set; }
        public string LastActionFilter { get; private set; } = string.Empty;
        public DateTime LastFromFilter { get; private set; }
        public DateTime LastToFilter { get; private set; }

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
            return Task.FromResult(_entries);
        }
    }

    private sealed class StubCflDialogService : ICflDialogService
    {
        public Task<CflResult?> ShowAsync(CflRequest request) => Task.FromResult<CflResult?>(null);
    }

    private sealed class StubShellInteractionService : IShellInteractionService
    {
        public void UpdateInspector(InspectorContext context) { }
        public void UpdateStatus(string message) { }
    }

    private sealed class StubModuleNavigationService : IModuleNavigationService
    {
        public void Activate(ModuleDocumentViewModel document) { }
        public ModuleDocumentViewModel OpenModule(string moduleKey, object? parameter = null)
            => throw new NotSupportedException();
    }
}
