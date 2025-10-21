using System;
using System.Collections.Generic;
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
        var audit = new AuditService(database);
        const int adapterSignatureId = 5511;
        var supplierAdapter = new FakeSupplierCrudService
        {
            SignatureMetadataIdSource = _ => adapterSignatureId
        };
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

        var viewModel = new SuppliersModuleViewModel(database, audit, supplierAdapter, attachments, filePicker, auth, signatureDialog, dialog, shell, navigation);
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
        Assert.Equal("Electronic signature captured (QA Reason).", viewModel.StatusMessage);
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
        var capturedResult = Assert.Single(signatureDialog.CapturedResults);
        var signatureResult = Assert.NotNull(capturedResult);
        Assert.Equal(supplierAdapter.Saved[0].Id, signatureResult.Signature.RecordId);
        Assert.Equal(adapterSignatureId, signatureResult.Signature.Id);
        Assert.Empty(signatureDialog.PersistedResults);
        Assert.Equal(0, signatureDialog.PersistInvocationCount);
    }

    [Fact]
    public async Task OnSaveAsync_AddMode_AppliesSignatureMetadataToEditor()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        const int metadataId = 90210;
        var supplierAdapter = new FakeSupplierCrudService
        {
            SignatureMetadataIdSource = _ => metadataId
        };
        const string sessionId = "session-test";
        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 7, FullName = "QA" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "127.0.0.1",
            CurrentSessionId = sessionId
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();

        var viewModel = new SuppliersModuleViewModel(database, audit, supplierAdapter, attachments, filePicker, auth, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.Mode = FormMode.Add;
        viewModel.Editor.Name = "Contoso";
        viewModel.Editor.SupplierType = "Calibration";
        viewModel.Editor.Status = "Active";
        viewModel.Editor.VatNumber = "HR123";
        viewModel.Editor.Email = "info@contoso.example";
        viewModel.Editor.Phone = "+385 91 000 111";
        viewModel.Editor.Country = "Croatia";

        var beforeSave = DateTime.UtcNow;
        var saved = await InvokeSaveAsync(viewModel);
        var afterSave = DateTime.UtcNow;

        Assert.True(saved);
        var editor = Assert.IsType<SupplierEditor>(viewModel.Editor);
        Assert.Equal(metadataId, editor.DigitalSignatureId);
        Assert.Equal("test-signature", editor.DigitalSignature);
        Assert.Equal("test-signature", editor.SignatureHash);
        Assert.Equal("QA Reason", editor.SignatureReason);
        Assert.Equal("Automated test", editor.SignatureNote);
        var timestamp = Assert.NotNull(editor.SignatureTimestampUtc);
        Assert.InRange(timestamp, beforeSave.AddSeconds(-5), afterSave.AddSeconds(5));
        Assert.Equal(7, editor.SignerUserId);
        Assert.Equal("QA", editor.SignerUserName);
        Assert.Equal(7, editor.LastModifiedById);
        Assert.Equal("QA", editor.LastModifiedByName);
        Assert.Equal("127.0.0.1", editor.SourceIp);
        Assert.Equal(sessionId, editor.SessionId);
        Assert.Equal("UnitTest", editor.DeviceInfo);
        Assert.False(viewModel.IsDirty);
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
        var audit = new AuditService(database);

        var viewModel = new SuppliersModuleViewModel(database, audit, supplierAdapter, attachments, filePicker, auth, signatureDialog,
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
        var audit = new AuditService(database);

        var viewModel = new SuppliersModuleViewModel(database, audit, supplierAdapter, attachments, filePicker, auth, signatureDialog,
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
        var audit = new AuditService(database);

        var bytes = Encoding.UTF8.GetBytes("supplier document");
        filePicker.Files = new[]
        {
            new PickedFile("supplier.txt", "text/plain", () => Task.FromResult<Stream>(new MemoryStream(bytes, writable: false)), bytes.Length)
        };

        var viewModel = new SuppliersModuleViewModel(database, audit, supplierAdapter, attachments, filePicker, auth, signatureDialog, dialog, shell, navigation);
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
        var audit = new AuditService(database);

        var viewModel = new SuppliersModuleViewModel(database, audit, supplierAdapter, attachments, filePicker, auth, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.SelectedRecord = viewModel.Records.First();
        await viewModel.EnterUpdateModeCommand.ExecuteAsync(null);
        var originalName = viewModel.Editor.Name;
        viewModel.Editor.Name = "Updated";

        viewModel.CancelCommand.Execute(null);

        Assert.Equal(originalName, viewModel.Editor.Name);
    }

    [Fact]
    public async Task SelectingRecord_LoadsAuditTimelineForSupplier()
    {
        var database = new DatabaseService();
        var audit = new StubAuditService(database);
        var now = DateTime.UtcNow;
        audit.FilteredAudits.AddRange(new[]
        {
            new AuditEntryDto
            {
                Id = 1,
                Entity = "suppliers",
                EntityId = "12",
                Action = "Created",
                Timestamp = now.AddMinutes(-30),
                Username = "qa"
            },
            new AuditEntryDto
            {
                Id = 2,
                Entity = "Suppliers",
                EntityId = "12",
                Action = "ContractUpload",
                Timestamp = now.AddMinutes(-10),
                Username = "qa"
            },
            new AuditEntryDto
            {
                Id = 3,
                Entity = "suppliers",
                EntityId = "99",
                Action = "Irrelevant",
                Timestamp = now
            }
        });

        var supplierAdapter = new FakeSupplierCrudService();
        supplierAdapter.Saved.Add(new Supplier
        {
            Id = 12,
            Name = "Precision Labs",
            SupplierType = "Calibration",
            Status = "Active",
            Email = "precision@example.com",
            VatNumber = "VAT-12",
            ContractFile = "precision.pdf"
        });

        var attachments = new ConfigurableAttachmentWorkflowService();
        var filePicker = new TestFilePicker();
        var auth = new TestAuthContext();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new StubCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new StubModuleNavigationService();

        var viewModel = new SuppliersModuleViewModel(database, audit, supplierAdapter, attachments, filePicker, auth,
            signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.SelectedRecord = viewModel.Records.Single(r => r.Key == "12");
        await Task.Delay(10);

        Assert.Equal("suppliers", audit.LastFilter?.Entity);
        Assert.Equal(2, viewModel.AuditTimeline.Count);
        Assert.All(viewModel.AuditTimeline, entry =>
        {
            Assert.Equal("12", entry.EntityId);
            Assert.True(string.Equals("suppliers", entry.Entity, StringComparison.OrdinalIgnoreCase));
        });
        Assert.True(viewModel.AuditTimeline[0].Timestamp <= viewModel.AuditTimeline[^1].Timestamp);
    }

    [Fact]
    public async Task ClearingSelection_ClearsAuditTimeline()
    {
        var database = new DatabaseService();
        var audit = new StubAuditService(database);
        var now = DateTime.UtcNow;
        audit.FilteredAudits.AddRange(new[]
        {
            new AuditEntryDto
            {
                Id = 1,
                Entity = "suppliers",
                EntityId = "12",
                Action = "Created",
                Timestamp = now.AddMinutes(-20),
                Username = "qa"
            },
            new AuditEntryDto
            {
                Id = 2,
                Entity = "suppliers",
                EntityId = "12",
                Action = "Updated",
                Timestamp = now.AddMinutes(-5),
                Username = "qa"
            }
        });

        var supplierAdapter = new FakeSupplierCrudService();
        supplierAdapter.Saved.Add(new Supplier
        {
            Id = 12,
            Name = "Precision Labs",
            SupplierType = "Calibration",
            Status = "Active",
            Email = "precision@example.com",
            VatNumber = "VAT-12",
            ContractFile = "precision.pdf"
        });

        var attachments = new ConfigurableAttachmentWorkflowService();
        var filePicker = new TestFilePicker();
        var auth = new TestAuthContext();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new StubCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new StubModuleNavigationService();

        var viewModel = new SuppliersModuleViewModel(database, audit, supplierAdapter, attachments, filePicker, auth,
            signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.SelectedRecord = viewModel.Records.Single(r => r.Key == "12");
        await Task.Delay(10);

        Assert.NotEmpty(viewModel.AuditTimeline);

        viewModel.SelectedRecord = null;
        await Task.Delay(10);

        Assert.Empty(viewModel.AuditTimeline);
    }

    [Fact]
    public async Task PreviewContractCommand_DownloadsAndPreviewsContract()
    {
        var database = new DatabaseService();
        var audit = new StubAuditService(database);
        var supplierAdapter = new FakeSupplierCrudService();
        supplierAdapter.Saved.Add(new Supplier
        {
            Id = 3,
            Name = "Contoso Calibration",
            SupplierType = "Calibration",
            Status = "Active",
            Email = "contoso@example.com",
            VatNumber = "VAT-3",
            ContractFile = "master-contract.pdf"
        });

        var attachmentWorkflow = new ConfigurableAttachmentWorkflowService();
        var contractAttachment = new Attachment
        {
            Id = 42,
            FileName = "master-contract.pdf",
            Name = "Master Contract",
            EntityTable = "suppliers",
            EntityId = 3
        };
        attachmentWorkflow.Links[("suppliers", 3)] = new List<AttachmentLinkWithAttachment>
        {
            new(
                new AttachmentLink
                {
                    Id = 100,
                    EntityType = "suppliers",
                    EntityId = 3,
                    AttachmentId = contractAttachment.Id
                },
                contractAttachment)
        };
        attachmentWorkflow.DownloadStreamFactory = _
            => new MemoryStream(Encoding.UTF8.GetBytes("contract"), writable: false);

        var filePicker = new TestFilePicker();
        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 7, FullName = "QA" }
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new StubCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new StubModuleNavigationService();

        var viewModel = new SuppliersModuleViewModel(database, audit, supplierAdapter, attachmentWorkflow, filePicker, auth,
            signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.SelectedRecord = viewModel.Records.Single(r => r.Key == "3");
        await Task.Delay(10);

        Assert.True(viewModel.PreviewContractCommand.CanExecute(null));
        await viewModel.PreviewContractCommand.ExecuteAsync(null);

        var download = Assert.Single(attachmentWorkflow.DownloadRequests);
        Assert.Equal(contractAttachment.Id, download.AttachmentId);
        Assert.Equal(auth.CurrentUser?.Id, download.Request?.RequestedById);
        var preview = Assert.Single(shell.PreviewedDocuments);
        Assert.Contains("master-contract.pdf", preview, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Previewing contract", viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    private static Task<bool> InvokeSaveAsync(SuppliersModuleViewModel viewModel)
    {
        var method = typeof(SuppliersModuleViewModel)
            .GetMethod("OnSaveAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(SuppliersModuleViewModel), "OnSaveAsync");
        return (Task<bool>)method.Invoke(viewModel, null)!;
    }
}
