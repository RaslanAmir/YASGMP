using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.Tests.TestDoubles;
using YasGMP.Wpf.ViewModels.Dialogs;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Tests;

public class PartsModuleViewModelTests
{
    [Fact]
    public async Task InventoryCommands_RefreshesReportsAndStatus()
    {
        // Arrange
        const int partId = 42;
        var database = new DatabaseService();
        database.Warehouses.AddRange(new[]
        {
            new Warehouse { Id = 1, Name = "Main", Location = "Floor", Status = "Active" },
            new Warehouse { Id = 2, Name = "Secondary", Location = "Annex", Status = "Active" },
            new Warehouse { Id = 3, Name = "Overflow", Location = "Annex", Status = "Active" },
            new Warehouse { Id = 4, Name = "Healthy", Location = "Annex", Status = "Active" }
        });

        database.StockLevelsByPart[partId] = new List<(int, string, int, int?, int?)>
        {
            (1, "Main", 2, 10, 100),       // Critical
            (2, "Secondary", 12, 10, 40),  // Warning (within 120% of minimum)
            (3, "Overflow", 260, 10, 200), // Overflow
            (4, "Healthy", 90, 10, 200)    // Healthy
        };

        database.InventoryHistoryByPart[(partId, 50)] = CreateHistoryTable(new[]
        {
            (DateTime.UtcNow.AddHours(-1), "in", 25, 1, 7, "PO-15", "Received"),
            (DateTime.UtcNow.AddHours(-2), "out", 5, 2, 7, "WO-77", "Issued"),
            (DateTime.UtcNow.AddHours(-3), "adjust", -3, 3, 7, null, "Cycle count")
        });

        var part = new Part
        {
            Id = partId,
            Code = "PRT-042",
            Name = "Sterile Filter",
            Description = "0.2µm",
            DefaultSupplierId = 11,
            DefaultSupplierName = "Contoso",
            Stock = 20,
            MinStockAlert = 5,
            Location = "Main"
        };

        var partService = new FakePartCrudService();
        partService.Saved.Add(part);

        var inventoryService = new RecordingInventoryTransactionService();
        var signatureDialog = TestElectronicSignatureDialogService.CreateConfirmed();
        var shell = new TestShellInteractionService();
        var localization = CreateLocalizationService();

        var viewModel = CreateViewModel(
            database,
            partService,
            inventoryService,
            signatureDialog,
            shell,
            localization);

        await viewModel.InitializeAsync(null);
        viewModel.SelectedRecord = viewModel.Records.First();
        LoadPart(viewModel, part);

        var signature = signatureDialog.DefaultResult;

        var receiveRequest = InventoryTransactionRequest.CreateReceive(partId, 1, 5, "PO-123", "Receiving");
        await InvokeInventoryFlowAsync(viewModel, inventoryService, signature, InventoryTransactionType.Receive, receiveRequest, "Main");

        var issueRequest = InventoryTransactionRequest.CreateIssue(partId, 2, 4, "WO-7", "Issuing");
        await InvokeInventoryFlowAsync(viewModel, inventoryService, signature, InventoryTransactionType.Issue, issueRequest, "Secondary");

        var adjustRequest = InventoryTransactionRequest.CreateAdjustment(partId, 3, -6, null, "Cycle Count", "Variance");
        await InvokeInventoryFlowAsync(viewModel, inventoryService, signature, InventoryTransactionType.Adjust, adjustRequest, "Overflow");

        // Assert inventory service captured the payloads
        Assert.Collection(
            inventoryService.Executions,
            execution =>
            {
                Assert.Equal(receiveRequest, execution.Request);
                Assert.Equal(signature.Signature, execution.Context.Signature.Signature);
            },
            execution => Assert.Equal(issueRequest, execution.Request),
            execution =>
            {
                Assert.Equal(adjustRequest, execution.Request);
                Assert.Equal(signature.Signature, execution.Context.Signature.Signature);
            });

        // Assert status messages and shell updates match the most recent transaction
        Assert.Equal("adjusted -6 units at Overflow.", viewModel.StatusMessage);
        Assert.Equal(viewModel.StatusMessage, shell.StatusUpdates.Last());

        // Assert recent transactions projected from the stubbed DataTable
        Assert.Equal(3, viewModel.RecentTransactions.Count);
        var firstReport = viewModel.RecentTransactions[0];
        Assert.Equal("in", firstReport.TransactionType);
        Assert.Equal(25, firstReport.Quantity);
        Assert.Equal("Main", firstReport.Warehouse);
        Assert.Equal("PO-15", firstReport.Document);

        // Assert alerts generated for the critical and overflow zones
        Assert.Equal(2, viewModel.TransactionAlerts.Count);
        Assert.Contains(viewModel.TransactionAlerts, alert => alert.Contains("Main", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(viewModel.TransactionAlerts, alert => alert.Contains("Overflow", StringComparison.OrdinalIgnoreCase));

        // Zone summaries honour each filter label
        var allItems = viewModel.ZoneSummaries.ToList();
        Assert.Equal(4, allItems.Count);

        AssertFilter(viewModel, localization.GetString("Module.Parts.ZoneFilter.All"), 4);
        AssertFilter(viewModel, localization.GetString("Module.Parts.ZoneFilter.Critical"), 1);
        AssertFilter(viewModel, localization.GetString("Module.Parts.ZoneFilter.Warning"), 1);
        AssertFilter(viewModel, localization.GetString("Module.Parts.ZoneFilter.Healthy"), 1);
        AssertFilter(viewModel, localization.GetString("Module.Parts.ZoneFilter.Overflow"), 1);
    }

    [Fact]
    public async Task RefreshInventoryReportsAsync_WhenStockLevelFails_ClearsCollections()
    {
        // Arrange
        const int partId = 5;
        var database = new DatabaseService();
        database.StockLevelsByPart[partId] = new List<(int, string, int, int?, int?)>
        {
            (1, "Any", 1, 1, 10)
        };

        var part = new Part
        {
            Id = partId,
            Code = "PRT-005",
            Name = "Probe",
            DefaultSupplierId = 2,
            DefaultSupplierName = "Globex"
        };

        var partService = new FakePartCrudService();
        partService.Saved.Add(part);

        var inventoryService = new RecordingInventoryTransactionService();
        var signatureDialog = TestElectronicSignatureDialogService.CreateConfirmed();
        var shell = new TestShellInteractionService();
        var localization = CreateLocalizationService();

        var viewModel = CreateViewModel(
            database,
            partService,
            inventoryService,
            signatureDialog,
            shell,
            localization);

        await viewModel.InitializeAsync(null);
        viewModel.SelectedRecord = viewModel.Records.First();
        LoadPart(viewModel, part);

        viewModel.ZoneSummaries.Add(new PartsModuleViewModel.ZoneSummaryItem(1, PartsModuleViewModel.ZoneClassification.Critical, "Critical", "Any", 1, 1, 10));
        viewModel.TransactionAlerts.Add("Existing alert");
        viewModel.RecentTransactions.Add(new PartsModuleViewModel.InventoryTransactionReportItem(DateTime.UtcNow, "in", 1, "Any", null, null, null));

        database.StockLevelsException = new InvalidOperationException("boom");

        var refreshReportsMethod = typeof(PartsModuleViewModel)
            .GetMethod("RefreshInventoryReportsAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(PartsModuleViewModel), "RefreshInventoryReportsAsync");

        // Act
        await (Task)refreshReportsMethod.Invoke(viewModel, new object[] { partId })!;

        // Assert collections were cleared
        Assert.Empty(viewModel.ZoneSummaries);
        Assert.Empty(viewModel.TransactionAlerts);
        Assert.Empty(viewModel.RecentTransactions);
        Assert.Empty(viewModel.ZoneSummariesView.Cast<object>());
        Assert.Contains("Failed to load stock levels", viewModel.StatusMessage);
    }

    private static PartsModuleViewModel CreateViewModel(
        DatabaseService database,
        FakePartCrudService partService,
        RecordingInventoryTransactionService inventoryService,
        TestElectronicSignatureDialogService signatureDialog,
        TestShellInteractionService shell,
        ILocalizationService localization)
    {
        var audit = new RecordingAuditService(database);
        var attachmentWorkflow = new StubAttachmentWorkflowService();
        var filePicker = new TestFilePicker();
        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 7, FullName = "QA" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "127.0.0.1"
        };

        return new PartsModuleViewModel(
            database,
            audit,
            partService,
            inventoryService,
            attachmentWorkflow,
            filePicker,
            auth,
            signatureDialog,
            new StubCflDialogService(),
            shell,
            new StubModuleNavigationService(),
            localization);
    }

    private static async Task InvokeInventoryFlowAsync(
        PartsModuleViewModel viewModel,
        RecordingInventoryTransactionService inventoryService,
        ElectronicSignatureDialogResult signature,
        InventoryTransactionType type,
        InventoryTransactionRequest request,
        string warehouseName)
    {
        var createContextMethod = typeof(PartsModuleViewModel)
            .GetMethod("CreateInventoryContext", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(PartsModuleViewModel), "CreateInventoryContext");

        var refreshMethod = typeof(PartsModuleViewModel)
            .GetMethod("RefreshInventoryStateAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(PartsModuleViewModel), "RefreshInventoryStateAsync");

        var context = (InventoryTransactionContext)createContextMethod.Invoke(viewModel, new object[] { signature })!;
        await inventoryService.ExecuteAsync(request, context).ConfigureAwait(false);
        await (Task)refreshMethod.Invoke(viewModel, new object[] { type, request, warehouseName })!;
    }

    private static void LoadPart(PartsModuleViewModel viewModel, Part part)
    {
        var loadedPartField = typeof(PartsModuleViewModel)
            .GetField("_loadedPart", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(nameof(PartsModuleViewModel), "_loadedPart");
        loadedPartField.SetValue(viewModel, part);

        var loadEditorMethod = typeof(PartsModuleViewModel)
            .GetMethod("LoadEditor", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(PartsModuleViewModel), "LoadEditor");
        loadEditorMethod.Invoke(viewModel, new object[] { part });

        viewModel.Mode = FormMode.View;
    }

    private static void AssertFilter(PartsModuleViewModel viewModel, string filter, int expectedCount)
    {
        viewModel.SelectedZoneFilter = filter;
        viewModel.ZoneSummariesView.Refresh();
        Assert.Equal(expectedCount, viewModel.ZoneSummariesView.Cast<object>().Count());
    }

    private static DataTable CreateHistoryTable(IEnumerable<(DateTime Date, string Type, int Quantity, int WarehouseId, int? UserId, string? Document, string? Note)> rows)
    {
        var table = new DataTable();
        table.Columns.Add("transaction_date", typeof(DateTime));
        table.Columns.Add("transaction_type", typeof(string));
        table.Columns.Add("quantity", typeof(int));
        table.Columns.Add("warehouse_id", typeof(int));
        table.Columns.Add("performed_by_id", typeof(int));
        table.Columns.Add("related_document", typeof(string));
        table.Columns.Add("note", typeof(string));

        foreach (var row in rows)
        {
            table.Rows.Add(row.Date, row.Type, row.Quantity, row.WarehouseId, row.UserId, row.Document ?? (object)DBNull.Value, row.Note ?? (object)DBNull.Value);
        }

        return table;
    }

    private static FakeLocalizationService CreateLocalizationService()
        => new(new Dictionary<string, IDictionary<string, string>>
        {
            ["en"] = new Dictionary<string, string>
            {
                ["Module.Title.PartsStock"] = "Parts Stock",
                ["Module.Parts.ZoneFilter.Critical"] = "Critical",
                ["Module.Parts.ZoneFilter.Warning"] = "Warning",
                ["Module.Parts.ZoneFilter.Healthy"] = "Healthy",
                ["Module.Parts.ZoneFilter.Overflow"] = "Overflow",
                ["Module.Parts.ZoneFilter.All"] = "All Zones",
                ["Module.Parts.Status.Active"] = "Active",
                ["Module.Parts.Status.Inactive"] = "Inactive",
                ["Module.Parts.Status.Blocked"] = "Blocked"
            }
        }, "en");

    private sealed class StubAttachmentWorkflowService : IAttachmentWorkflowService
    {
        public bool IsEncryptionEnabled => false;

        public string EncryptionKeyId => string.Empty;

        public List<AttachmentWorkflowUploadResult> Uploads { get; } = new();

        public Task<AttachmentWorkflowUploadResult> UploadAsync(Stream content, AttachmentUploadRequest request, CancellationToken token = default)
        {
            var result = new AttachmentWorkflowUploadResult(
                new Attachment { Id = 1, FileName = request.FileName },
                new AttachmentLink { Id = 1, EntityId = request.EntityId, EntityType = request.EntityType },
                new RetentionPolicy());
            Uploads.Add(result);
            return Task.FromResult(result);
        }

        public Task<AttachmentStreamResult> DownloadAsync(int attachmentId, Stream destination, AttachmentReadRequest? request = null, CancellationToken token = default)
            => Task.FromResult(new AttachmentStreamResult(new Attachment { Id = attachmentId }, 0, 0, false, request));

        public Task<IReadOnlyList<AttachmentLinkWithAttachment>> GetLinksForEntityAsync(string entityType, int entityId, CancellationToken token = default)
            => Task.FromResult<IReadOnlyList<AttachmentLinkWithAttachment>>(Array.Empty<AttachmentLinkWithAttachment>());

        public Task<IReadOnlyList<Attachment>> GetAttachmentSummariesAsync(string? entityFilter, string? typeFilter, string? searchTerm, CancellationToken token = default)
            => Task.FromResult<IReadOnlyList<Attachment>>(Array.Empty<Attachment>());

        public Task RemoveLinkAsync(int linkId, CancellationToken token = default)
            => Task.CompletedTask;

        public Task RemoveLinkAsync(string entityType, int entityId, int attachmentId, CancellationToken token = default)
            => Task.CompletedTask;

        public Task<Attachment?> FindByHashAsync(string sha256, CancellationToken token = default)
            => Task.FromResult<Attachment?>(null);

        public Task<Attachment?> FindByHashAndSizeAsync(string sha256, long fileSize, CancellationToken token = default)
            => Task.FromResult<Attachment?>(null);
    }
}
