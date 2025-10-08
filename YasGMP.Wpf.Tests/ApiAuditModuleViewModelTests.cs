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

public class ApiAuditModuleViewModelTests
{
    [Fact]
    public async Task InitializeAsync_LoadsApiAuditEntries_WithInspectorDetails()
    {
        var database = CreateDatabaseService();
        var auditService = new AuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new StubShellInteractionService();
        var navigation = new StubModuleNavigationService();

        var timestamp = new DateTime(2025, 2, 15, 8, 30, 0, DateTimeKind.Utc);
        var entries = new[]
        {
            new ApiAuditEntryDto
            {
                Id = 5,
                ApiKeyId = 12,
                ApiKeyValue = "INT-PRIMARY-KEY-0001",
                ApiKeyDescription = "Integration Primary",
                ApiKeyOwnerUsername = "integration.bot",
                ApiKeyOwnerFullName = "Integration Bot",
                ApiKeyIsActive = true,
                Action = "POST /api/v1/assets",
                Timestamp = timestamp,
                Username = "integration.bot",
                UserId = 88,
                IpAddress = "198.51.100.15",
                RequestDetails = "{\"asset\":\"A-100\"}",
                Details = "HTTP 201 Created"
            }
        };

        var viewModel = new TestApiAuditModuleViewModel(database, auditService, cfl, shell, navigation, entries);
        viewModel.FilterApiKey = "INT";
        viewModel.FilterUser = "integration";
        viewModel.SelectedAction = "POST /api/v1/assets";
        viewModel.FilterFrom = timestamp.Date;
        viewModel.FilterTo = timestamp.Date;

        await viewModel.RefreshAsync();

        Assert.Single(viewModel.Records);
        var record = viewModel.Records.First();
        Assert.Equal("POST /api/v1/assets", record.Code);
        Assert.Contains("#12", record.Title);
        Assert.Contains("•", record.Title);
        Assert.Equal("integration.bot (#88)", record.InspectorFields[2].Value);
        Assert.Equal("198.51.100.15", record.InspectorFields[3].Value);
        Assert.Equal("{\"asset\":\"A-100\"}", record.InspectorFields[4].Value);
        Assert.Equal("HTTP 201 Created", record.Description);
        Assert.Equal("Loaded 1 API audit entry.", viewModel.StatusMessage);
        Assert.True(viewModel.HasResults);
        Assert.False(viewModel.HasError);
        Assert.Equal("INT", viewModel.LastApiKeyFilter);
        Assert.Equal("integration", viewModel.LastUserFilter);
        Assert.Equal("POST /api/v1/assets", viewModel.LastActionFilter);
        Assert.Equal(timestamp.Date, viewModel.LastFromFilter);
        Assert.Equal(timestamp.Date.AddDays(1).AddTicks(-1), viewModel.LastToFilter);
        Assert.Contains("POST /api/v1/assets", viewModel.ActionOptions);
        Assert.Equal("All", viewModel.ActionOptions.First());

        viewModel.SelectedRecord = record;
        Assert.NotNull(shell.LastContext);
        var inspectorContext = shell.LastContext!;
        Assert.Equal(ApiAuditModuleViewModel.ModuleKey, inspectorContext.ModuleKey);
        var timestampField = Assert.Single(inspectorContext.Fields.Where(f => f.Label == "Timestamp"));
        Assert.Equal("Dock.Inspector.ApiAudit.5.Timestamp", timestampField.AutomationId);
        Assert.Contains("Timestamp", timestampField.AutomationName, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Timestamp", timestampField.AutomationTooltip, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FilterApiKey_WhenChanged_TriggersAutoRefresh()
    {
        var database = CreateDatabaseService();
        var auditService = new AuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new StubShellInteractionService();
        var navigation = new StubModuleNavigationService();

        var viewModel = new TestApiAuditModuleViewModel(
            database,
            auditService,
            cfl,
            shell,
            navigation,
            Array.Empty<ApiAuditEntryDto>());

        await viewModel.InitializeAsync(null);

        var initialCount = viewModel.RefreshInvocationCount;
        var refreshAwaiter = viewModel.AwaitNextRefreshAsync();

        viewModel.FilterApiKey = "INT-002";

        await refreshAwaiter;

        Assert.Equal(initialCount + 1, viewModel.RefreshInvocationCount);
        var snapshot = viewModel.RefreshSnapshots.Last();
        Assert.False(snapshot.HasError);
        Assert.False(snapshot.HasResults);
        Assert.Equal("Loading API Audit Trail records...", snapshot.StatusMessage);
    }

    [Fact]
    public async Task FilterUser_WhenChanged_TriggersAutoRefresh()
    {
        var database = CreateDatabaseService();
        var auditService = new AuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new StubShellInteractionService();
        var navigation = new StubModuleNavigationService();

        var viewModel = new TestApiAuditModuleViewModel(
            database,
            auditService,
            cfl,
            shell,
            navigation,
            Array.Empty<ApiAuditEntryDto>());

        await viewModel.InitializeAsync(null);

        var initialCount = viewModel.RefreshInvocationCount;
        var refreshAwaiter = viewModel.AwaitNextRefreshAsync();

        viewModel.FilterUser = "integration.bot";

        await refreshAwaiter;

        Assert.Equal(initialCount + 1, viewModel.RefreshInvocationCount);
        var snapshot = viewModel.RefreshSnapshots.Last();
        Assert.False(snapshot.HasError);
        Assert.False(snapshot.HasResults);
        Assert.Equal("Loading API Audit Trail records...", snapshot.StatusMessage);
    }

    [Fact]
    public async Task SelectedAction_WhenChanged_TriggersAutoRefresh()
    {
        var database = CreateDatabaseService();
        var auditService = new AuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new StubShellInteractionService();
        var navigation = new StubModuleNavigationService();

        var viewModel = new TestApiAuditModuleViewModel(
            database,
            auditService,
            cfl,
            shell,
            navigation,
            Array.Empty<ApiAuditEntryDto>());

        await viewModel.InitializeAsync(null);

        var initialCount = viewModel.RefreshInvocationCount;
        var refreshAwaiter = viewModel.AwaitNextRefreshAsync();

        viewModel.SelectedAction = "POST /api/v1/assets";

        await refreshAwaiter;

        Assert.Equal(initialCount + 1, viewModel.RefreshInvocationCount);
        var snapshot = viewModel.RefreshSnapshots.Last();
        Assert.False(snapshot.HasError);
        Assert.False(snapshot.HasResults);
        Assert.Equal("Loading API Audit Trail records...", snapshot.StatusMessage);
    }

    [Fact]
    public async Task FilterFrom_WhenChanged_TriggersAutoRefresh()
    {
        var database = CreateDatabaseService();
        var auditService = new AuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new StubShellInteractionService();
        var navigation = new StubModuleNavigationService();

        var viewModel = new TestApiAuditModuleViewModel(
            database,
            auditService,
            cfl,
            shell,
            navigation,
            Array.Empty<ApiAuditEntryDto>());

        await viewModel.InitializeAsync(null);

        var initialCount = viewModel.RefreshInvocationCount;
        var refreshAwaiter = viewModel.AwaitNextRefreshAsync();

        viewModel.FilterFrom = DateTime.Today.AddDays(-14);

        await refreshAwaiter;

        Assert.Equal(initialCount + 1, viewModel.RefreshInvocationCount);
        var snapshot = viewModel.RefreshSnapshots.Last();
        Assert.False(snapshot.HasError);
        Assert.False(snapshot.HasResults);
        Assert.Equal("Loading API Audit Trail records...", snapshot.StatusMessage);
    }

    [Fact]
    public async Task FilterTo_WhenChanged_TriggersAutoRefresh()
    {
        var database = CreateDatabaseService();
        var auditService = new AuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new StubShellInteractionService();
        var navigation = new StubModuleNavigationService();

        var viewModel = new TestApiAuditModuleViewModel(
            database,
            auditService,
            cfl,
            shell,
            navigation,
            Array.Empty<ApiAuditEntryDto>());

        await viewModel.InitializeAsync(null);

        var initialCount = viewModel.RefreshInvocationCount;
        var refreshAwaiter = viewModel.AwaitNextRefreshAsync();

        viewModel.FilterTo = DateTime.Today.AddDays(-1);

        await refreshAwaiter;

        Assert.Equal(initialCount + 1, viewModel.RefreshInvocationCount);
        var snapshot = viewModel.RefreshSnapshots.Last();
        Assert.False(snapshot.HasError);
        Assert.False(snapshot.HasResults);
        Assert.Equal("Loading API Audit Trail records...", snapshot.StatusMessage);
    }

    [Fact]
    public async Task RefreshAsync_NoEntries_SetsEmptyStatusMessageAndMaintainsAllOption()
    {
        var database = CreateDatabaseService();
        var auditService = new AuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new StubShellInteractionService();
        var navigation = new StubModuleNavigationService();

        var viewModel = new TestApiAuditModuleViewModel(database, auditService, cfl, shell, navigation, Array.Empty<ApiAuditEntryDto>());
        viewModel.SelectedAction = "DELETE";

        await viewModel.RefreshAsync();

        Assert.Empty(viewModel.Records);
        Assert.Equal("No API audit entries match the current filters.", viewModel.StatusMessage);
        Assert.False(viewModel.HasResults);
        Assert.False(viewModel.HasError);
        Assert.Single(viewModel.ActionOptions);
        Assert.Equal("All", viewModel.ActionOptions.First());
    }

    [Fact]
    public async Task RefreshAsync_WhenFromAfterTo_SwapsAndNormalizesRange()
    {
        var database = CreateDatabaseService();
        var auditService = new AuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new StubShellInteractionService();
        var navigation = new StubModuleNavigationService();

        var later = new DateTime(2025, 5, 10);
        var earlier = new DateTime(2025, 5, 1);

        var viewModel = new TestApiAuditModuleViewModel(database, auditService, cfl, shell, navigation, Array.Empty<ApiAuditEntryDto>());
        viewModel.FilterFrom = later;
        viewModel.FilterTo = earlier;

        await viewModel.RefreshAsync();

        Assert.Equal(earlier, viewModel.FilterFrom!.Value);
        Assert.Equal(later, viewModel.FilterTo!.Value);
        Assert.Equal(earlier, viewModel.LastFromFilter);
        Assert.Equal(later.AddDays(1).AddTicks(-1), viewModel.LastToFilter);
        Assert.False(viewModel.HasError);
        Assert.False(viewModel.HasResults);
    }

    [Fact]
    public async Task RefreshAsync_WhenFiltersNull_DefaultsToTodayRange()
    {
        var database = CreateDatabaseService();
        var auditService = new AuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new StubShellInteractionService();
        var navigation = new StubModuleNavigationService();

        var viewModel = new TestApiAuditModuleViewModel(database, auditService, cfl, shell, navigation, Array.Empty<ApiAuditEntryDto>());
        viewModel.FilterFrom = null;
        viewModel.FilterTo = null;

        await viewModel.RefreshAsync();

        var today = DateTime.Today;
        var expectedFrom = today.AddDays(-30);
        var expectedTo = today;

        Assert.Equal(expectedFrom, viewModel.FilterFrom!.Value);
        Assert.Equal(expectedTo, viewModel.FilterTo!.Value);
        Assert.Equal(expectedFrom, viewModel.LastFromFilter);
        Assert.Equal(expectedTo.AddDays(1).AddTicks(-1), viewModel.LastToFilter);
        Assert.False(viewModel.HasError);
        Assert.False(viewModel.HasResults);
    }

    [Fact]
    public async Task RefreshAsync_WhenQueryThrows_LoadsDesignTimeRecordsAndFlagsError()
    {
        var database = CreateDatabaseService();
        var auditService = new AuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new StubShellInteractionService();
        var navigation = new StubModuleNavigationService();

        var exception = new InvalidOperationException("forced failure");
        var viewModel = new ThrowingApiAuditModuleViewModel(database, auditService, cfl, shell, navigation, exception);

        await viewModel.RefreshAsync();

        Assert.True(viewModel.HasError);
        Assert.NotEmpty(viewModel.Records);
        Assert.StartsWith("Offline data loaded because:", viewModel.StatusMessage);
        Assert.Contains("forced failure", viewModel.StatusMessage);
    }

    [Fact]
    public async Task SearchText_FiltersResultsByInspectorValues()
    {
        var database = CreateDatabaseService();
        var auditService = new AuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new StubShellInteractionService();
        var navigation = new StubModuleNavigationService();

        var entries = new[]
        {
            new ApiAuditEntryDto
            {
                Id = 10,
                ApiKeyId = 20,
                ApiKeyValue = "REPORTING-KEY-0002",
                ApiKeyDescription = "Reporting",
                Action = "GET /api/v1/audit",
                Timestamp = DateTime.UtcNow,
                Username = "qa.viewer",
                UserId = 21,
                IpAddress = "203.0.113.20",
                RequestDetails = "{\"filter\":\"status=OK\"}",
                Details = "HTTP 200 OK"
            },
            new ApiAuditEntryDto
            {
                Id = 11,
                ApiKeyId = 21,
                ApiKeyValue = "SERVICE-KEY-9999",
                ApiKeyDescription = "Service",
                Action = "DELETE /api/v1/assets/99",
                Timestamp = DateTime.UtcNow,
                Username = "service.bot",
                UserId = 99,
                IpAddress = "198.51.100.55",
                RequestDetails = "{\"asset\":\"A-200\"}",
                Details = "HTTP 401 Unauthorized"
            }
        };

        var viewModel = new TestApiAuditModuleViewModel(database, auditService, cfl, shell, navigation, entries);
        await viewModel.RefreshAsync();

        viewModel.SearchText = "status=OK";
        var filtered = viewModel.RecordsView.Cast<ModuleRecord>().ToList();

        Assert.Single(filtered);
        Assert.Equal("GET /api/v1/audit", filtered[0].Code);
    }

    private static DatabaseService CreateDatabaseService()
        => new("Server=localhost;Database=unit_test;Uid=test;Pwd=test;");

    private sealed class TestApiAuditModuleViewModel : ApiAuditModuleViewModel
    {
        public readonly record struct RefreshInvocationSnapshot(bool HasError, bool HasResults, string? StatusMessage);

        private readonly IReadOnlyList<ApiAuditEntryDto> _entries;
        private readonly List<RefreshInvocationSnapshot> _refreshSnapshots = new();
        private TaskCompletionSource<object?>? _refreshSignal;

        public TestApiAuditModuleViewModel(
            DatabaseService databaseService,
            AuditService auditService,
            ICflDialogService cflDialogService,
            IShellInteractionService shellInteraction,
            IModuleNavigationService navigation,
            IReadOnlyList<ApiAuditEntryDto> entries)
            : base(databaseService, auditService, cflDialogService, shellInteraction, navigation)
        {
            _entries = entries;
        }

        public string? LastApiKeyFilter { get; private set; }
        public string? LastUserFilter { get; private set; }
        public string? LastActionFilter { get; private set; }
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

        protected override Task<IReadOnlyList<ApiAuditEntryDto>> QueryApiAuditsAsync(
            string apiKey,
            string user,
            string action,
            DateTime from,
            DateTime to,
            int limit)
        {
            LastApiKeyFilter = apiKey;
            LastUserFilter = user;
            LastActionFilter = action;
            LastFromFilter = from;
            LastToFilter = to;
            RefreshInvocationCount++;
            _refreshSnapshots.Add(new RefreshInvocationSnapshot(HasError, HasResults, StatusMessage));
            _refreshSignal?.TrySetResult(null);
            _refreshSignal = null;
            return Task.FromResult(_entries);
        }
    }

    private sealed class ThrowingApiAuditModuleViewModel : ApiAuditModuleViewModel
    {
        private readonly Exception _exception;

        public ThrowingApiAuditModuleViewModel(
            DatabaseService databaseService,
            AuditService auditService,
            ICflDialogService cflDialogService,
            IShellInteractionService shellInteraction,
            IModuleNavigationService navigation,
            Exception exception)
            : base(databaseService, auditService, cflDialogService, shellInteraction, navigation)
        {
            _exception = exception;
        }

        protected override Task<IReadOnlyList<ApiAuditEntryDto>> QueryApiAuditsAsync(
            string apiKey,
            string user,
            string action,
            DateTime from,
            DateTime to,
            int limit)
            => Task.FromException<IReadOnlyList<ApiAuditEntryDto>>(_exception);
    }

    private sealed class StubCflDialogService : ICflDialogService
    {
        public Task<CflResult?> ShowAsync(CflRequest request) => Task.FromResult<CflResult?>(null);
    }

    private sealed class StubShellInteractionService : IShellInteractionService
    {
        public InspectorContext? LastContext { get; private set; }

        public void UpdateStatus(string message) { }

        public void UpdateInspector(InspectorContext context) => LastContext = context;
    }

    private sealed class StubModuleNavigationService : IModuleNavigationService
    {
        public ModuleDocumentViewModel OpenModule(string moduleKey, object? parameter = null)
            => throw new NotSupportedException();

        public void Activate(ModuleDocumentViewModel document) { }
    }
}
