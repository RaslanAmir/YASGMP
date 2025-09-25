using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Services;
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
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();

        var viewModel = new WarehouseModuleViewModel(database, warehouseAdapter, attachments, filePicker, auth, dialog, shell, navigation);
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

    private static Task<bool> InvokeSaveAsync(WarehouseModuleViewModel viewModel)
    {
        var method = typeof(WarehouseModuleViewModel)
            .GetMethod("OnSaveAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(WarehouseModuleViewModel), "OnSaveAsync");
        return (Task<bool>)method.Invoke(viewModel, null)!;
    }
}
