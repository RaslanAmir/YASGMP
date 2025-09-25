
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
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Tests;

public class PartsModuleViewModelTests
{
    [Fact]
    public async Task OnSaveAsync_AddMode_PersistsPartThroughAdapter()
    {
        var database = new DatabaseService();
        database.Suppliers.Add(new Supplier { Id = 3, Name = "Contoso" });
        var partAdapter = new FakePartCrudService();
        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 8, FullName = "QA" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "127.0.0.50"
        };
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();

        var viewModel = new PartsModuleViewModel(database, partAdapter, attachments, filePicker, auth, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.Mode = FormMode.Add;
        viewModel.Editor.Code = "PRT-500";
        viewModel.Editor.Name = "Pressure Gauge";
        viewModel.Editor.Description = "Test gauge";
        viewModel.Editor.Category = "Instrumentation";
        viewModel.Editor.Status = "active";
        viewModel.Editor.Stock = 5;
        viewModel.Editor.MinStockAlert = 2;
        viewModel.Editor.Location = "Main";
        viewModel.Editor.DefaultSupplierId = 3;

        var saved = await InvokeSaveAsync(viewModel);

        Assert.True(saved);
        Assert.Single(partAdapter.Saved);
        var persisted = partAdapter.Saved[0];
        Assert.Equal("Pressure Gauge", persisted.Name);
        Assert.Equal(3, persisted.DefaultSupplierId);
        Assert.Equal("contoso", persisted.DefaultSupplierName.ToLowerInvariant());
    }

    [Fact]
    public async Task AttachCommand_UploadsDocument()
    {
        var database = new DatabaseService();
        database.Suppliers.Add(new Supplier { Id = 1, Name = "Contoso" });
        var partAdapter = new FakePartCrudService();
        partAdapter.Saved.Add(new Part
        {
            Id = 10,
            Code = "PRT-10",
            Name = "Filter",
            DefaultSupplierId = 1,
            DefaultSupplierName = "Contoso"
        });

        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 2, FullName = "QA" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "10.0.0.42"
        };
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();

        var bytes = Encoding.UTF8.GetBytes("hello part");
        filePicker.Files = new[]
        {
            new PickedFile("part.txt", "text/plain", () => Task.FromResult<Stream>(new MemoryStream(bytes, writable: false)), bytes.Length)
        };

        var viewModel = new PartsModuleViewModel(database, partAdapter, attachments, filePicker, auth, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);
        viewModel.SelectedRecord = viewModel.Records.First();

        Assert.True(viewModel.AttachDocumentCommand.CanExecute(null));
        await viewModel.AttachDocumentCommand.ExecuteAsync(null);

        Assert.Single(attachments.Uploads);
        var request = attachments.Uploads[0];
        Assert.Equal("parts", request.EntityType);
        Assert.Equal(10, request.EntityId);
    }

    [Fact]
    public async Task StockHealthMessage_WarnsWhenBelowMinimum()
    {
        var database = new DatabaseService();
        database.Suppliers.Add(new Supplier { Id = 1, Name = "Contoso" });
        var partAdapter = new FakePartCrudService();
        partAdapter.Saved.Add(new Part
        {
            Id = 12,
            Code = "PRT-12",
            Name = "Sensor",
            Stock = 2,
            MinStockAlert = 5,
            LowWarehouseCount = 1,
            WarehouseSummary = "Main:2",
            DefaultSupplierId = 1,
            DefaultSupplierName = "Contoso"
        });

        var auth = new TestAuthContext { CurrentUser = new User { Id = 5, FullName = "QA" } };
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();

        var viewModel = new PartsModuleViewModel(database, partAdapter, attachments, filePicker, auth, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.SelectedRecord = viewModel.Records.First();

        Assert.Contains("warehouse location", viewModel.StockHealthMessage, StringComparison.OrdinalIgnoreCase);
    }

    private static Task<bool> InvokeSaveAsync(PartsModuleViewModel viewModel)
    {
        var method = typeof(PartsModuleViewModel)
            .GetMethod("OnSaveAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(PartsModuleViewModel), "OnSaveAsync");
        return (Task<bool>)method.Invoke(viewModel, null)!;
    }
}
