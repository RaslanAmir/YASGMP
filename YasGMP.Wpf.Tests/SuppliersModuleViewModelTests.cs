using System;
using System.IO;
using System.Linq;
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

public class SuppliersModuleViewModelTests
{
    [Fact]
    public async Task OnSaveAsync_AddMode_PersistsSupplier()
    {
        var database = new DatabaseService();
        var supplierAdapter = new FakeSupplierCrudService();
        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 7, FullName = "QA" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "127.0.0.1"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();

        var viewModel = new SuppliersModuleViewModel(database, supplierAdapter, attachments, filePicker, auth, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.Mode = FormMode.Add;
        viewModel.Editor.Name = "Contoso";
        viewModel.Editor.SupplierType = "Calibration";
        viewModel.Editor.Status = "Active";
        viewModel.Editor.VatNumber = "HR123";
        viewModel.Editor.Email = "info@contoso.example";
        viewModel.Editor.Phone = "+385 91 000 111";
        viewModel.Editor.Country = "Croatia";

        var saved = await InvokeSaveAsync(viewModel);

        Assert.True(saved);
        Assert.Single(supplierAdapter.Saved);
        var supplier = supplierAdapter.Saved[0];
        Assert.Equal("contoso", supplier.Name.ToLowerInvariant());
        Assert.Equal("calibration", supplier.SupplierType.ToLowerInvariant());
        Assert.Equal("info@contoso.example", supplier.Email);
        Assert.Equal("test-signature", supplier.DigitalSignature);
        var context = Assert.Single(supplierAdapter.SavedContexts);
        Assert.Equal("test-signature", context.SignatureHash);
        Assert.Equal("password", context.SignatureMethod);
        Assert.Equal("valid", context.SignatureStatus);
        Assert.Equal("Automated test", context.SignatureNote);
        Assert.Collection(signatureDialog.Requests, ctx =>
        {
            Assert.Equal("suppliers", ctx.TableName);
            Assert.Equal(0, ctx.RecordId);
        });
        Assert.Single(signatureDialog.PersistedResults);
        var persistedSignature = signatureDialog.PersistedResults[0];
        Assert.Equal(supplierAdapter.Saved[0].Id, persistedSignature.Signature.RecordId);
        Assert.Equal(signatureDialog.LastPersistedSignatureId, persistedSignature.Signature.Id);
        Assert.True(persistedSignature.Signature.Id > 0);
        var persistedMetadata = Assert.Single(signatureDialog.PersistedSignatureRecords);
        Assert.Equal(persistedSignature.Signature.Id, persistedMetadata.SignatureId);
        Assert.Equal(supplierAdapter.Saved[0].Id, persistedMetadata.RecordId);
        Assert.Equal("test-signature", persistedMetadata.SignatureHash);
        Assert.Equal("password", persistedMetadata.Method);
        Assert.Equal("valid", persistedMetadata.Status);
        Assert.Equal("Automated test", persistedMetadata.Note);
    }

    [Fact]
    public async Task OnSaveAsync_AddMode_CancelledSignatureAbortsWithoutPersistence()
    {
        var database = new DatabaseService();
        var supplierAdapter = new FakeSupplierCrudService();
        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 42, FullName = "QA" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "127.0.0.1"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        signatureDialog.QueueCancellation();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();

        var viewModel = new SuppliersModuleViewModel(database, supplierAdapter, attachments, filePicker, auth, signatureDialog,
            dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.Mode = FormMode.Add;

        var saved = await InvokeSaveAsync(viewModel);

        Assert.False(saved);
        Assert.Equal(FormMode.Add, viewModel.Mode);
        Assert.Equal("Electronic signature cancelled. Save aborted.", viewModel.StatusMessage);
        Assert.Empty(supplierAdapter.Saved);
        Assert.Empty(supplierAdapter.SavedContexts);
        Assert.Empty(signatureDialog.PersistedResults);
        Assert.Empty(signatureDialog.PersistedSignatureRecords);
        var context = Assert.Single(signatureDialog.Requests);
        Assert.Equal("suppliers", context.TableName);
        Assert.Equal(0, context.RecordId);
    }

    [Fact]
    public async Task OnSaveAsync_AddMode_SignatureCaptureExceptionSurfacesError()
    {
        var database = new DatabaseService();
        var supplierAdapter = new FakeSupplierCrudService();
        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 84, FullName = "QA" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "127.0.0.1"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        signatureDialog.QueueCaptureException(new InvalidOperationException("Simulated capture failure."));
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();

        var viewModel = new SuppliersModuleViewModel(database, supplierAdapter, attachments, filePicker, auth, signatureDialog,
            dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.Mode = FormMode.Add;

        var saved = await InvokeSaveAsync(viewModel);

        Assert.False(saved);
        Assert.Equal(FormMode.Add, viewModel.Mode);
        Assert.Equal("Electronic signature failed: Simulated capture failure.", viewModel.StatusMessage);
        Assert.Empty(supplierAdapter.Saved);
        Assert.Empty(supplierAdapter.SavedContexts);
        Assert.Empty(signatureDialog.PersistedResults);
        Assert.Empty(signatureDialog.PersistedSignatureRecords);
        var context = Assert.Single(signatureDialog.Requests);
        Assert.Equal("suppliers", context.TableName);
        Assert.Equal(0, context.RecordId);
    }

    [Fact]
    public async Task AttachCommand_UploadsAttachment()
    {
        var database = new DatabaseService();
        var supplierAdapter = new FakeSupplierCrudService();
        supplierAdapter.Saved.Add(new Supplier
        {
            Id = 5,
            Name = "Globex",
            SupplierType = "Maintenance",
            Status = "active",
            Email = "hq@globex.example",
            VatNumber = "GLX-55"
        });

        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 3, FullName = "QA" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "10.10.10.10"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();

        var bytes = Encoding.UTF8.GetBytes("supplier document");
        filePicker.Files = new[]
        {
            new PickedFile("supplier.txt", "text/plain", () => Task.FromResult<Stream>(new MemoryStream(bytes, writable: false)), bytes.Length)
        };

        var viewModel = new SuppliersModuleViewModel(database, supplierAdapter, attachments, filePicker, auth, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.SelectedRecord = viewModel.Records.First();

        Assert.True(viewModel.AttachDocumentCommand.CanExecute(null));
        await viewModel.AttachDocumentCommand.ExecuteAsync(null);

        Assert.Single(attachments.Uploads);
        var request = attachments.Uploads[0];
        Assert.Equal("suppliers", request.EntityType);
        Assert.Equal(5, request.EntityId);
    }

    [Fact]
    public async Task Cancel_UpdateMode_RestoresSnapshot()
    {
        var database = new DatabaseService();
        var supplierAdapter = new FakeSupplierCrudService();
        supplierAdapter.Saved.Add(new Supplier
        {
            Id = 11,
            Name = "Supplier One",
            SupplierType = "Lab",
            Status = "active",
            Email = "lab@example.com",
            VatNumber = "VAT-11"
        });

        var auth = new TestAuthContext();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();

        var viewModel = new SuppliersModuleViewModel(database, supplierAdapter, attachments, filePicker, auth, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.SelectedRecord = viewModel.Records.First();
        await viewModel.EnterUpdateModeCommand.ExecuteAsync(null);
        var originalName = viewModel.Editor.Name;
        viewModel.Editor.Name = "Updated";

        viewModel.CancelCommand.Execute(null);

        Assert.Equal(originalName, viewModel.Editor.Name);
    }

    private static Task<bool> InvokeSaveAsync(SuppliersModuleViewModel viewModel)
    {
        var method = typeof(SuppliersModuleViewModel)
            .GetMethod("OnSaveAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(SuppliersModuleViewModel), "OnSaveAsync");
        return (Task<bool>)method.Invoke(viewModel, null)!;
    }
}
