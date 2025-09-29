using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.Tests.TestDoubles;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Tests;

public class WarehouseModuleViewModelTests
{
    [Fact]
    public async Task OnSaveAsync_AddMode_PersistsWarehouse()
    {
        var database = new DatabaseService();
        var warehouseAdapter = new FakeWarehouseCrudService();
        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 4, FullName = "QA" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "192.168.1.25"
        };
        var signatureDialog = new FakeElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();

        var viewModel = new WarehouseModuleViewModel(database, warehouseAdapter, attachments, filePicker, auth, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.Mode = FormMode.Add;
        viewModel.Editor.Name = "Cleanroom Store";
        viewModel.Editor.Location = "Building C";
        viewModel.Editor.Status = "qualified";
        viewModel.Editor.Responsible = "Jane";
        viewModel.Editor.ClimateMode = "2-8";
        viewModel.Editor.Note = "Qualification pending";

        var saved = await InvokeSaveAsync(viewModel);

        Assert.True(saved);
        Assert.Single(warehouseAdapter.Saved);
        var persisted = warehouseAdapter.Saved[0];
        Assert.Equal("Cleanroom Store", persisted.Name);
        Assert.Equal("building c", persisted.Location.ToLowerInvariant());
        Assert.Equal("qualified", persisted.Status);
        Assert.Equal("test-signature", persisted.DigitalSignature);
        Assert.Collection(signatureDialog.Requests, ctx =>
        {
            Assert.Equal("warehouses", ctx.TableName);
            Assert.Equal(0, ctx.RecordId);
        });
    }

    [Fact]
    public async Task AttachCommand_UploadsWarehouseDocument()
    {
        var database = new DatabaseService();
        var warehouseAdapter = new FakeWarehouseCrudService();
        warehouseAdapter.Saved.Add(new Warehouse
        {
            Id = 7,
            Name = "Main Warehouse",
            Location = "Building A",
            Status = "qualified"
        });

        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 5, FullName = "QA" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "10.10.0.10"
        };
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();

        var bytes = Encoding.UTF8.GetBytes("warehouse doc");
        filePicker.Files = new[]
        {
            new PickedFile("warehouse.txt", "text/plain", () => Task.FromResult<Stream>(new MemoryStream(bytes, writable: false)), bytes.Length)
        };

        var viewModel = new WarehouseModuleViewModel(database, warehouseAdapter, attachments, filePicker, auth, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);
        viewModel.SelectedRecord = viewModel.Records.First();

        Assert.True(viewModel.AttachDocumentCommand.CanExecute(null));
        await viewModel.AttachDocumentCommand.ExecuteAsync(null);

        Assert.Single(attachments.Uploads);
        var upload = attachments.Uploads[0];
        Assert.Equal("warehouses", upload.EntityType);
        Assert.Equal(7, upload.EntityId);
    }

    [Fact]
    public async Task LoadInsights_PopulatesSnapshotAndMovements()
    {
        var database = new DatabaseService();
        var warehouseAdapter = new FakeWarehouseCrudService();
        warehouseAdapter.Saved.Add(new Warehouse
        {
            Id = 2,
            Name = "Cold Storage",
            Location = "Building C",
            Status = "qualified"
        });
        warehouseAdapter.StockSnapshots.Add(new WarehouseStockSnapshot(
            WarehouseId: 2,
            PartId: 15,
            PartCode: "PRT-15",
            PartName: "Filter",
            Quantity: 3,
            MinThreshold: 5,
            MaxThreshold: null,
            Reserved: 0,
            Blocked: 0,
            BatchNumber: "B-1",
            SerialNumber: string.Empty,
            ExpiryDate: null));
        warehouseAdapter.Movements.Add(new InventoryMovementEntry(
            WarehouseId: 2,
            Timestamp: DateTime.UtcNow,
            Type: "IN",
            Quantity: 8,
            RelatedDocument: "PO-55",
            Note: "Initial receipt",
            PerformedById: 4));

        var auth = new TestAuthContext { CurrentUser = new User { Id = 9, FullName = "QA" } };
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();

        var viewModel = new WarehouseModuleViewModel(database, warehouseAdapter, attachments, filePicker, auth, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.SelectedRecord = viewModel.Records.First();
        await InvokeLoadInsightsAsync(viewModel, 2);

        Assert.True(viewModel.HasStockAlerts);
        Assert.Single(viewModel.StockSnapshot);
        Assert.Single(viewModel.RecentMovements);
    }

    private static Task<bool> InvokeSaveAsync(WarehouseModuleViewModel viewModel)
    {
        var method = typeof(WarehouseModuleViewModel)
            .GetMethod("OnSaveAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(WarehouseModuleViewModel), "OnSaveAsync");
        return (Task<bool>)method.Invoke(viewModel, null)!;
    }

    private static Task InvokeLoadInsightsAsync(WarehouseModuleViewModel viewModel, int id)
    {
        var method = typeof(WarehouseModuleViewModel)
            .GetMethod("LoadInsightsAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(WarehouseModuleViewModel), "LoadInsightsAsync");
        return (Task)method.Invoke(viewModel, new object[] { id })!;
    }
}
