using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using YasGMP.AppCore.Models.Signatures;
using Xunit;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Dialogs;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.Tests.TestDoubles;
using YasGMP.Wpf.ViewModels.Dialogs;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Tests;

public class WarehouseModuleViewModelTests
{
    [Fact]
    public async Task OnSaveAsync_AddMode_PersistsWarehouse()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        const int adapterSignatureId = 4789;
        var warehouseAdapter = new FakeWarehouseCrudService
        {
            SignatureMetadataIdSource = _ => adapterSignatureId
        };
        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 4, FullName = "QA" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "192.168.1.25"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();
        var localization = CreateLocalizationService();

        var viewModel = new WarehouseModuleViewModel(database, audit, warehouseAdapter, attachments, filePicker, auth, signatureDialog, dialog, shell, navigation, localization);
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
        Assert.Equal("Electronic signature captured (QA Reason).", viewModel.StatusMessage);
        Assert.Single(warehouseAdapter.Saved);
        var persisted = warehouseAdapter.Saved[0];
        Assert.Equal("Cleanroom Store", persisted.Name);
        Assert.Equal("building c", persisted.Location.ToLowerInvariant());
        Assert.Equal("qualified", persisted.Status);
        Assert.Equal("test-signature", persisted.DigitalSignature);
        var context = Assert.Single(warehouseAdapter.SavedContexts);
        Assert.Equal("test-signature", context.SignatureHash);
        Assert.Equal("password", context.SignatureMethod);
        Assert.Equal("valid", context.SignatureStatus);
        Assert.Equal("Automated test", context.SignatureNote);
        Assert.Collection(signatureDialog.Requests, ctx =>
        {
            Assert.Equal("warehouses", ctx.TableName);
            Assert.Equal(0, ctx.RecordId);
        });
        var capturedResult = Assert.Single(signatureDialog.CapturedResults);
        var signatureResult = Assert.NotNull(capturedResult);
        Assert.Equal(warehouseAdapter.Saved[0].Id, signatureResult.Signature.RecordId);
        Assert.Equal(adapterSignatureId, signatureResult.Signature.Id);
        Assert.Empty(signatureDialog.PersistedResults);
        Assert.Equal(0, signatureDialog.PersistInvocationCount);
    }

    [Fact]
    public async Task AttachCommand_UploadsWarehouseDocument()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
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
        var signatureDialog = new TestElectronicSignatureDialogService();
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

        var viewModel = new WarehouseModuleViewModel(database, audit, warehouseAdapter, attachments, filePicker, auth, signatureDialog, dialog, shell, navigation, localization);
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
        var audit = new AuditService(database);
        var warehouseAdapter = new FakeWarehouseCrudService();
        warehouseAdapter.Saved.Add(new Warehouse
        {
            Id = 2,
            Name = "Cold Storage",
            Location = "Building C",
            Status = "qualified"
        });
        SeedWarehouseInsights(
            warehouseAdapter,
            new[]
            {
                new WarehouseStockSnapshot(
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
                    ExpiryDate: null)
            },
            new[]
            {
                new InventoryMovementEntry(
                    WarehouseId: 2,
                    Timestamp: DateTime.UtcNow,
                    Type: "IN",
                    Quantity: 8,
                    RelatedDocument: "PO-55",
                    Note: "Initial receipt",
                    PerformedById: 4)
            });

        var auth = new TestAuthContext { CurrentUser = new User { Id = 9, FullName = "QA" } };
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();
        var localization = CreateLocalizationService();

        var viewModel = new WarehouseModuleViewModel(database, audit, warehouseAdapter, attachments, filePicker, auth, signatureDialog, dialog, shell, navigation, localization);
        await viewModel.InitializeAsync(null);

        viewModel.SelectedRecord = viewModel.Records.First();
        await InvokeLoadInsightsAsync(viewModel, 2);

        Assert.True(viewModel.HasStockAlerts);
        Assert.Single(viewModel.StockSnapshot);
        Assert.Single(viewModel.RecentMovements);
    }

    [Fact]
    public async Task InventoryCommands_SubmitTransactions_UpdatesStatusAndShell()
    {
        await RunOnStaThreadAsync(async () =>
        {
            var database = new DatabaseService();
            var audit = new AuditService(database);
            var localization = CreateLocalizationService();

            database.Parts.AddRange(new[]
            {
                new Part { Id = 101, Code = "PRT-101", Name = "Sterile Filter" },
                new Part { Id = 202, Code = "PRT-202", Name = "Transfer Hose" }
            });

            var warehouseAdapter = new FakeWarehouseCrudService();
            warehouseAdapter.Saved.Add(new Warehouse
            {
                Id = 5,
                Name = "Receiving Bay",
                Location = "Dock 1",
                Status = "qualified"
            });

            var auth = new TestAuthContext
            {
                CurrentUser = new User { Id = 42, FullName = "Ops Lead" },
                CurrentDeviceInfo = "UnitTest",
                CurrentIpAddress = "10.0.0.5"
            };

            var signatureDialog = new TestElectronicSignatureDialogService();
            var receiveSignature = new ElectronicSignatureDialogResult(
                "pw",
                "RCV",
                "Receive replenishment",
                "Receive",
                new DigitalSignature { SignatureHash = "sig-receive", Method = "password", Status = "valid" });
            var issueSignature = new ElectronicSignatureDialogResult(
                "pw",
                "ISS",
                "Issue to line",
                "Issue",
                new DigitalSignature { SignatureHash = "sig-issue", Method = "password", Status = "valid" });
            var adjustSignature = new ElectronicSignatureDialogResult(
                "pw",
                "ADJ",
                "Cycle count adjustment",
                "Adjust",
                new DigitalSignature { SignatureHash = "sig-adjust", Method = "password", Status = "valid" });

            signatureDialog.QueueResult(receiveSignature);
            signatureDialog.QueueResult(issueSignature);
            signatureDialog.QueueResult(adjustSignature);

            var dialog = new TestCflDialogService();
            var shell = new TestShellInteractionService();
            var navigation = new TestModuleNavigationService();
            var filePicker = new TestFilePicker();
            var attachments = new TestAttachmentService();

            var viewModel = new WarehouseModuleViewModel(database, audit, warehouseAdapter, attachments, filePicker, auth, signatureDialog, dialog, shell, navigation, localization);
            await viewModel.InitializeAsync(null);

            viewModel.SelectedRecord = viewModel.Records.First();
            viewModel.Mode = FormMode.View;

            var receiveTask = viewModel.ReceiveStockCommand.ExecuteAsync(null);
            await SubmitTransactionAsync(quantityText: "4", document: "PO-42", note: "Restock");
            await receiveTask;

            var issueTask = viewModel.IssueStockCommand.ExecuteAsync(null);
            await SubmitTransactionAsync(quantityText: "3", document: "WO-10", note: "Issue to production");
            await issueTask;

            var adjustTask = viewModel.AdjustStockCommand.ExecuteAsync(null);
            await SubmitTransactionAsync(quantityText: "-2", document: "CC-77", note: "Cycle count variance", adjustmentReason: "Scrap");
            await adjustTask;

            Assert.Equal(3, warehouseAdapter.ExecutedTransactions.Count);

            var receive = warehouseAdapter.ExecutedTransactions[0];
            Assert.Equal(InventoryTransactionType.Receive, receive.Type);
            Assert.Equal(5, receive.WarehouseId);
            Assert.Equal(4, receive.Quantity);
            Assert.Equal("PO-42", receive.Document);
            Assert.Equal("Restock", receive.Note);
            Assert.Null(receive.AdjustmentDelta);

            var issue = warehouseAdapter.ExecutedTransactions[1];
            Assert.Equal(InventoryTransactionType.Issue, issue.Type);
            Assert.Equal(5, issue.WarehouseId);
            Assert.Equal(3, issue.Quantity);
            Assert.Equal("WO-10", issue.Document);
            Assert.Equal("Issue to production", issue.Note);
            Assert.Null(issue.AdjustmentDelta);

            var adjust = warehouseAdapter.ExecutedTransactions[2];
            Assert.Equal(InventoryTransactionType.Adjust, adjust.Type);
            Assert.Equal(5, adjust.WarehouseId);
            Assert.Equal(2, adjust.Quantity);
            Assert.Equal(-2, adjust.AdjustmentDelta);
            Assert.Equal("CC-77", adjust.Document);
            Assert.Equal("Cycle count variance", adjust.Note);
            Assert.Equal("Scrap", adjust.AdjustmentReason);

            Assert.Collection(
                warehouseAdapter.ExecutedSignatures,
                sig => Assert.Equal("sig-receive", sig.Signature?.SignatureHash),
                sig => Assert.Equal("sig-issue", sig.Signature?.SignatureHash),
                sig => Assert.Equal("sig-adjust", sig.Signature?.SignatureHash));

            Assert.Equal(
                new[]
                {
                    "received +4 units for Receiving Bay.",
                    "issued -3 units for Receiving Bay.",
                    "adjusted -2 units for Receiving Bay."
                },
                shell.StatusUpdates);

            Assert.Equal(shell.StatusUpdates[^1], viewModel.StatusMessage);
        });
    }

    [Fact]
    public async Task SelectedZoneFilter_FiltersSummariesAndAlerts()
    {
        await RunOnStaThreadAsync(async () =>
        {
            var database = new DatabaseService();
            var audit = new AuditService(database);
            var localization = CreateLocalizationService();
            var warehouseAdapter = new FakeWarehouseCrudService();
            warehouseAdapter.Saved.Add(new Warehouse
            {
                Id = 2,
                Name = "Cold Storage",
                Location = "Building C",
                Status = "qualified"
            });

            SeedWarehouseInsights(
                warehouseAdapter,
                new[]
                {
                    new WarehouseStockSnapshot(
                        WarehouseId: 2,
                        PartId: 11,
                        PartCode: "PRT-11",
                        PartName: "Buffer A",
                        Quantity: 1,
                        MinThreshold: 5,
                        MaxThreshold: null,
                        Reserved: 0,
                        Blocked: 0,
                        BatchNumber: "B-1",
                        SerialNumber: string.Empty,
                        ExpiryDate: null),
                    new WarehouseStockSnapshot(
                        WarehouseId: 2,
                        PartId: 12,
                        PartCode: "PRT-12",
                        PartName: "Buffer B",
                        Quantity: 12,
                        MinThreshold: 10,
                        MaxThreshold: null,
                        Reserved: 0,
                        Blocked: 0,
                        BatchNumber: "B-2",
                        SerialNumber: string.Empty,
                        ExpiryDate: null),
                    new WarehouseStockSnapshot(
                        WarehouseId: 2,
                        PartId: 13,
                        PartCode: "PRT-13",
                        PartName: "Coolant",
                        Quantity: 30,
                        MinThreshold: null,
                        MaxThreshold: 25,
                        Reserved: 0,
                        Blocked: 0,
                        BatchNumber: "B-3",
                        SerialNumber: string.Empty,
                        ExpiryDate: null)
                });

            var auth = new TestAuthContext { CurrentUser = new User { Id = 9, FullName = "QA" } };
            var signatureDialog = new TestElectronicSignatureDialogService();
            var dialog = new TestCflDialogService();
            var shell = new TestShellInteractionService();
            var navigation = new TestModuleNavigationService();
            var filePicker = new TestFilePicker();
            var attachments = new TestAttachmentService();

            var viewModel = new WarehouseModuleViewModel(database, audit, warehouseAdapter, attachments, filePicker, auth, signatureDialog, dialog, shell, navigation, localization);
            await viewModel.InitializeAsync(null);

            viewModel.SelectedRecord = viewModel.Records.First();
            viewModel.Mode = FormMode.View;

            await InvokeLoadInsightsAsync(viewModel, 2);

            Assert.True(viewModel.HasStockAlerts);
            Assert.Equal(2, viewModel.TransactionAlerts.Count);

            var allSummaries = viewModel.ZoneSummariesView.Cast<WarehouseModuleViewModel.WarehouseZoneSummaryItem>().ToList();
            Assert.Equal(3, allSummaries.Count);

            var criticalLabel = viewModel.ZoneFilters.First(filter => filter.Contains("Critical", StringComparison.OrdinalIgnoreCase));
            viewModel.SelectedZoneFilter = criticalLabel;
            var criticalItems = viewModel.ZoneSummariesView.Cast<WarehouseModuleViewModel.WarehouseZoneSummaryItem>().ToList();
            Assert.Single(criticalItems);
            Assert.Equal("Critical", criticalItems[0].ZoneLabel);

            var overflowLabel = viewModel.ZoneFilters.First(filter => filter.Contains("Overflow", StringComparison.OrdinalIgnoreCase));
            viewModel.SelectedZoneFilter = overflowLabel;
            var overflowItems = viewModel.ZoneSummariesView.Cast<WarehouseModuleViewModel.WarehouseZoneSummaryItem>().ToList();
            Assert.Single(overflowItems);
            Assert.Equal("Overflow", overflowItems[0].ZoneLabel);

            var allLabel = viewModel.ZoneFilters.First(filter => filter.Contains("All", StringComparison.OrdinalIgnoreCase));
            viewModel.SelectedZoneFilter = allLabel;
            var resetItems = viewModel.ZoneSummariesView.Cast<WarehouseModuleViewModel.WarehouseZoneSummaryItem>().ToList();
            Assert.Equal(allSummaries.Count, resetItems.Count);
        });
    }

    [Fact]
    public async Task ExecuteInventoryTransactionAsync_WhenGetAllPartsFails_ReportsError()
    {
        await RunOnStaThreadAsync(async () =>
        {
            var database = new DatabaseService
            {
                PartsException = new InvalidOperationException("Inventory offline")
            };
            var audit = new AuditService(database);
            var localization = CreateLocalizationService();
            var warehouseAdapter = new FakeWarehouseCrudService();
            warehouseAdapter.Saved.Add(new Warehouse
            {
                Id = 3,
                Name = "Dispensary",
                Location = "Building D",
                Status = "qualified"
            });

            var auth = new TestAuthContext
            {
                CurrentUser = new User { Id = 7, FullName = "Technician" }
            };

            var signatureDialog = new TestElectronicSignatureDialogService();
            var dialog = new TestCflDialogService();
            var shell = new TestShellInteractionService();
            var navigation = new TestModuleNavigationService();
            var filePicker = new TestFilePicker();
            var attachments = new TestAttachmentService();

            var viewModel = new WarehouseModuleViewModel(database, audit, warehouseAdapter, attachments, filePicker, auth, signatureDialog, dialog, shell, navigation, localization);
            await viewModel.InitializeAsync(null);

            viewModel.SelectedRecord = viewModel.Records.First();
            viewModel.Mode = FormMode.View;

            await viewModel.ReceiveStockCommand.ExecuteAsync(null);

            Assert.Equal("Failed to load parts: Inventory offline", viewModel.StatusMessage);
            Assert.Empty(shell.StatusUpdates);
            Assert.Empty(warehouseAdapter.ExecutedTransactions);
            Assert.Empty(signatureDialog.Requests);
        });
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

    private static Task RunOnStaThreadAsync(Func<Task> action)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        var completion = new TaskCompletionSource<object?>();
        var thread = new Thread(() =>
        {
            try
            {
                EnsureWpfApplication();
                action().GetAwaiter().GetResult();
                ShutdownWpfApplication();
                completion.SetResult(null);
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        })
        {
            IsBackground = true,
            Name = "WarehouseModuleViewModelTests STA"
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task;
    }

    private static void EnsureWpfApplication()
    {
        if (Application.Current is null)
        {
            Application.ResourceAssembly = typeof(WarehouseModuleViewModel).Assembly;
            var app = new Application
            {
                ShutdownMode = ShutdownMode.OnExplicitShutdown
            };
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(app.Dispatcher));
        }
        else
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Application.Current.Dispatcher));
        }
    }

    private static void ShutdownWpfApplication()
    {
        if (Application.Current is null)
        {
            return;
        }

        foreach (var window in Application.Current.Windows.OfType<Window>().ToArray())
        {
            window.Close();
        }

        Application.Current.Shutdown();
    }

    private static async Task SubmitTransactionAsync(
        string quantityText,
        string? document,
        string? note,
        string? adjustmentReason = null)
    {
        if (Application.Current is null)
        {
            throw new InvalidOperationException("WPF application is not initialized.");
        }

        WarehouseStockTransactionDialogViewModel? dialogVm = null;

        for (var attempt = 0; attempt < 100 && dialogVm is null; attempt++)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                dialogVm = Application.Current.Windows
                    .OfType<WarehouseStockTransactionDialog>()
                    .Select(window => window.DataContext as WarehouseStockTransactionDialogViewModel)
                    .FirstOrDefault(vm => vm is not null);
            }, DispatcherPriority.Background);

            if (dialogVm is null)
            {
                await Task.Delay(20);
            }
        }

        if (dialogVm is null)
        {
            throw new InvalidOperationException("Warehouse stock transaction dialog did not appear.");
        }

        await Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            dialogVm.QuantityText = quantityText;
            dialogVm.Document = document;
            dialogVm.Note = note;

            if (adjustmentReason is not null)
            {
                dialogVm.AdjustmentReason = adjustmentReason;
            }

            await dialogVm.ConfirmCommand.ExecuteAsync(null);
        }, DispatcherPriority.Send);
    }

    private static void SeedWarehouseInsights(
        FakeWarehouseCrudService service,
        IEnumerable<WarehouseStockSnapshot> snapshots,
        IEnumerable<InventoryMovementEntry>? movements = null)
    {
        if (service is null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        if (snapshots is not null)
        {
            service.StockSnapshots.AddRange(snapshots);
        }

        if (movements is not null)
        {
            service.Movements.AddRange(movements);
        }
    }

    private static FakeLocalizationService CreateLocalizationService()
        => new(
            new Dictionary<string, IDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["en"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Module.Title.Warehouse"] = "Warehouses",
                    ["Module.Warehouse.ZoneFilter.All"] = "All Zones",
                    ["Module.Warehouse.ZoneFilter.Critical"] = "Critical",
                    ["Module.Warehouse.ZoneFilter.Warning"] = "Warning",
                    ["Module.Warehouse.ZoneFilter.Healthy"] = "Healthy",
                    ["Module.Warehouse.ZoneFilter.Overflow"] = "Overflow",
                    ["Module.Warehouse.Status.Qualified"] = "Qualified",
                    ["Module.Warehouse.Status.InQualification"] = "In Qualification",
                    ["Module.Warehouse.Status.Maintenance"] = "Maintenance",
                    ["Module.Warehouse.Status.Inactive"] = "Inactive"
                }
            },
            "en");
}
