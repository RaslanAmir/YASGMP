using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using YasGMP.AppCore.Models.Signatures;
using YasGMP.Services;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.Tests.TestDoubles;
using YasGMP.Wpf.ViewModels.Dialogs;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Tests;

public sealed class InventoryTransactionServiceAdapterTests
{
    [Fact]
    public async Task ExecuteAsync_Receive_ForwardsMetadataPersistsSignatureAndLogsAudit()
    {
        var database = new RecordingDatabaseService();
        var audit = new RecordingAuditService(database);
        var signatureDialog = TestElectronicSignatureDialogService.CreateConfirmed();

        var adapter = new InventoryTransactionServiceAdapter(database, audit, signatureDialog);
        var signature = CreateSignatureResult("hash-receive");
        var context = new InventoryTransactionContext(42, "10.0.0.5", "QA-LAB", "session-xyz", signature);
        var request = InventoryTransactionRequest.CreateReceive(7, 3, 5, "PO-123", "Initial receipt");

        var result = await adapter.ExecuteAsync(request, context);

        var receive = Assert.Single(database.ReceiveStockCalls);
        Assert.Equal(request.PartId, receive.PartId);
        Assert.Equal(request.WarehouseId, receive.WarehouseId);
        Assert.Equal(request.Quantity, receive.Quantity);
        Assert.Equal(context.EnsureUserId(), receive.UserId);
        Assert.Equal(request.Document, receive.Document);
        Assert.Equal(request.Note, receive.Note);
        Assert.Equal(context.EnsureIp(), receive.Ip);
        Assert.Equal(context.EnsureDevice(), receive.Device);
        Assert.Equal(context.EnsureSession(), receive.SessionId);

        Assert.Equal(1, signatureDialog.PersistInvocationCount);
        Assert.Equal(0, signatureDialog.LogPersistInvocationCount);

        var persisted = Assert.Single(signatureDialog.PersistedSignatureRecords);
        Assert.Equal(request.PartId, persisted.RecordId);
        Assert.Equal("hash-receive", persisted.SignatureHash);

        var auditEvent = Assert.Single(audit.Events);
        Assert.Equal("STOCK_TRANSACTION_SIGNATURE", auditEvent.EventType);
        Assert.Equal("inventory_transactions", auditEvent.TableName);
        Assert.Equal("Inventory", auditEvent.Module);
        Assert.Equal(request.PartId, auditEvent.RecordId);
        Assert.Equal("wpf", auditEvent.Severity);
        Assert.Equal(context.EnsureIp(), auditEvent.Ip);
        Assert.Equal(context.EnsureDevice(), auditEvent.DeviceInfo);
        Assert.Contains("type=Receive", auditEvent.Description, StringComparison.Ordinal);
        Assert.Contains("warehouse=3", auditEvent.Description, StringComparison.Ordinal);
        Assert.Contains("qty=5", auditEvent.Description, StringComparison.Ordinal);
        Assert.Contains("doc=PO-123", auditEvent.Description, StringComparison.Ordinal);

        Assert.Equal(request.Type, result.Type);
        Assert.Equal(request.PartId, result.PartId);
        Assert.Equal(request.WarehouseId, result.WarehouseId);
        Assert.Equal(request.Quantity, result.Quantity);
        Assert.Equal(request.Document, result.Document);
        Assert.Equal(request.Note, result.Note);
        Assert.Equal("hash-receive", result.Signature.Signature!.SignatureHash);
    }

    [Fact]
    public async Task ExecuteAsync_Issue_ForwardsMetadataPersistsSignatureAndLogsAudit()
    {
        var database = new RecordingDatabaseService();
        var audit = new RecordingAuditService(database);
        var signatureDialog = TestElectronicSignatureDialogService.CreateConfirmed();

        var adapter = new InventoryTransactionServiceAdapter(database, audit, signatureDialog);
        var signature = CreateSignatureResult("hash-issue");
        var context = new InventoryTransactionContext(77, "172.16.1.10", "PACK-01", "sess-issue", signature);
        var request = InventoryTransactionRequest.CreateIssue(18, 9, 12, "WO-45", "Batch consumption");

        var result = await adapter.ExecuteAsync(request, context);

        var issue = Assert.Single(database.IssueStockCalls);
        Assert.Equal(request.PartId, issue.PartId);
        Assert.Equal(request.WarehouseId, issue.WarehouseId);
        Assert.Equal(request.Quantity, issue.Quantity);
        Assert.Equal(context.EnsureUserId(), issue.UserId);
        Assert.Equal(request.Document, issue.Document);
        Assert.Equal(request.Note, issue.Note);
        Assert.Equal(context.EnsureIp(), issue.Ip);
        Assert.Equal(context.EnsureDevice(), issue.Device);
        Assert.Equal(context.EnsureSession(), issue.SessionId);

        Assert.Equal(1, signatureDialog.PersistInvocationCount);
        Assert.Equal(0, signatureDialog.LogPersistInvocationCount);

        var persisted = Assert.Single(signatureDialog.PersistedSignatureRecords);
        Assert.Equal(request.PartId, persisted.RecordId);
        Assert.Equal("hash-issue", persisted.SignatureHash);

        var auditEvent = Assert.Single(audit.Events);
        Assert.Equal("STOCK_TRANSACTION_SIGNATURE", auditEvent.EventType);
        Assert.Equal("inventory_transactions", auditEvent.TableName);
        Assert.Equal("Inventory", auditEvent.Module);
        Assert.Equal(request.PartId, auditEvent.RecordId);
        Assert.Equal("wpf", auditEvent.Severity);
        Assert.Contains("type=Issue", auditEvent.Description, StringComparison.Ordinal);
        Assert.Contains("warehouse=9", auditEvent.Description, StringComparison.Ordinal);
        Assert.Contains("qty=12", auditEvent.Description, StringComparison.Ordinal);
        Assert.Contains("doc=WO-45", auditEvent.Description, StringComparison.Ordinal);

        Assert.Equal(request.Type, result.Type);
        Assert.Equal(request.PartId, result.PartId);
        Assert.Equal(request.WarehouseId, result.WarehouseId);
        Assert.Equal(request.Quantity, result.Quantity);
        Assert.Equal(request.Document, result.Document);
        Assert.Equal(request.Note, result.Note);
        Assert.Equal("hash-issue", result.Signature.Signature!.SignatureHash);
    }

    [Fact]
    public async Task ExecuteAsync_Adjust_ForwardsMetadataPersistsSignatureAndLogsAudit()
    {
        var database = new RecordingDatabaseService();
        var audit = new RecordingAuditService(database);
        var signatureDialog = TestElectronicSignatureDialogService.CreateConfirmed();

        var adapter = new InventoryTransactionServiceAdapter(database, audit, signatureDialog);
        var signature = CreateSignatureResult("hash-adjust");
        var context = new InventoryTransactionContext(12, "192.168.0.50", "COUNT-02", "sess-adjust", signature);
        var request = InventoryTransactionRequest.CreateAdjustment(5, 4, -3, null, "Cycle count", "Variance");

        var result = await adapter.ExecuteAsync(request, context);

        var adjust = Assert.Single(database.AdjustStockCalls);
        Assert.Equal(request.PartId, adjust.PartId);
        Assert.Equal(request.WarehouseId, adjust.WarehouseId);
        Assert.Equal(request.AdjustmentDelta, adjust.Delta);
        Assert.Equal("Variance", adjust.Reason);
        Assert.Equal(context.EnsureUserId(), adjust.UserId);
        Assert.Equal(context.EnsureIp(), adjust.Ip);
        Assert.Equal(context.EnsureDevice(), adjust.Device);
        Assert.Equal(context.EnsureSession(), adjust.SessionId);

        Assert.Equal(1, signatureDialog.PersistInvocationCount);
        Assert.Equal(0, signatureDialog.LogPersistInvocationCount);

        var persisted = Assert.Single(signatureDialog.PersistedSignatureRecords);
        Assert.Equal(request.PartId, persisted.RecordId);
        Assert.Equal("hash-adjust", persisted.SignatureHash);

        var auditEvent = Assert.Single(audit.Events);
        Assert.Equal("STOCK_TRANSACTION_SIGNATURE", auditEvent.EventType);
        Assert.Equal("inventory_transactions", auditEvent.TableName);
        Assert.Equal("Inventory", auditEvent.Module);
        Assert.Equal(request.PartId, auditEvent.RecordId);
        Assert.Equal("wpf", auditEvent.Severity);
        Assert.Contains("type=Adjust", auditEvent.Description, StringComparison.Ordinal);
        Assert.Contains("warehouse=4", auditEvent.Description, StringComparison.Ordinal);
        Assert.Contains("qty=3", auditEvent.Description, StringComparison.Ordinal);
        Assert.Contains("doc=-", auditEvent.Description, StringComparison.Ordinal);

        Assert.Equal(request.Type, result.Type);
        Assert.Equal(request.PartId, result.PartId);
        Assert.Equal(request.WarehouseId, result.WarehouseId);
        Assert.Equal(request.Quantity, result.Quantity);
        Assert.Null(result.Document);
        Assert.Equal(request.Note, result.Note);
        Assert.Equal("hash-adjust", result.Signature.Signature!.SignatureHash);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDatabaseThrows_PropagatesAndAvoidsDuplicateHelpers()
    {
        var database = new RecordingDatabaseService
        {
            ReceiveException = new InvalidOperationException("boom")
        };
        var audit = new RecordingAuditService(database);
        var signatureDialog = TestElectronicSignatureDialogService.CreateConfirmed();

        var adapter = new InventoryTransactionServiceAdapter(database, audit, signatureDialog);
        var signature = CreateSignatureResult("hash-fail");
        var context = new InventoryTransactionContext(5, "127.0.0.1", "QA", "sess-fail", signature);
        var request = InventoryTransactionRequest.CreateReceive(2, 1, 8, "PO-FAIL", "Failure test");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => adapter.ExecuteAsync(request, context));
        Assert.Equal("boom", exception.Message);

        Assert.Equal(1, database.ReceiveAttemptCount);
        Assert.Empty(database.IssueStockCalls);
        Assert.Empty(database.AdjustStockCalls);
        Assert.Equal(0, signatureDialog.PersistInvocationCount);
        Assert.Equal(0, signatureDialog.LogPersistInvocationCount);
        Assert.Empty(audit.Events);
    }

    private static ElectronicSignatureDialogResult CreateSignatureResult(string hash)
    {
        return new ElectronicSignatureDialogResult(
            "password",
            "QA",
            "Inventory transaction",
            "QA Approval",
            new DigitalSignature
            {
                SignatureHash = hash,
                Method = "password",
                Status = "approved",
                SignedAt = DateTime.UtcNow
            });
    }

    private sealed class RecordingDatabaseService : DatabaseService
    {
        public RecordingDatabaseService()
            : base("Server=localhost;Database=unit_test;Uid=test;Pwd=test;")
        {
        }

        public List<(int PartId, int WarehouseId, int Quantity, int? UserId, string? Document, string? Note, string Ip, string Device, string? SessionId)> ReceiveStockCalls { get; } = new();

        public List<(int PartId, int WarehouseId, int Quantity, int? UserId, string? Document, string? Note, string Ip, string Device, string? SessionId)> IssueStockCalls { get; } = new();

        public List<(int PartId, int WarehouseId, int? Delta, string Reason, int? UserId, string Ip, string Device, string? SessionId)> AdjustStockCalls { get; } = new();

        public Exception? ReceiveException { get; set; }

        public Exception? IssueException { get; set; }

        public Exception? AdjustException { get; set; }

        public int ReceiveAttemptCount { get; private set; }

        public Task ReceiveStockAsync(
            int partId,
            int warehouseId,
            int quantity,
            int? userId,
            string? document,
            string? note,
            string ip,
            string device,
            string? sessionId,
            CancellationToken cancellationToken = default)
        {
            ReceiveAttemptCount++;
            if (ReceiveException is not null)
            {
                throw ReceiveException;
            }

            ReceiveStockCalls.Add((partId, warehouseId, quantity, userId, document, note, ip, device, sessionId));
            return Task.CompletedTask;
        }

        public Task IssueStockAsync(
            int partId,
            int warehouseId,
            int quantity,
            int? userId,
            string? document,
            string? note,
            string ip,
            string device,
            string? sessionId,
            CancellationToken cancellationToken = default)
        {
            if (IssueException is not null)
            {
                throw IssueException;
            }

            IssueStockCalls.Add((partId, warehouseId, quantity, userId, document, note, ip, device, sessionId));
            return Task.CompletedTask;
        }

        public Task AdjustStockAsync(
            int partId,
            int warehouseId,
            int delta,
            string reason,
            int? userId,
            string ip,
            string device,
            string? sessionId,
            CancellationToken cancellationToken = default)
        {
            if (AdjustException is not null)
            {
                throw AdjustException;
            }

            AdjustStockCalls.Add((partId, warehouseId, delta, reason, userId, ip, device, sessionId));
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingAuditService : AuditService
    {
        public RecordingAuditService(DatabaseService database)
            : base(database)
        {
        }

        public List<AuditEvent> Events { get; } = new();

        public override Task LogSystemEventAsync(
            int? userId,
            string? eventType,
            string? tableName,
            string? module,
            int? recordId,
            string? description,
            string? ip,
            string? severity,
            string? deviceInfo,
            string? sessionId,
            string? fieldName = null,
            string? oldValue = null,
            string? newValue = null,
            string? eventCode = null,
            string? table = null,
            int? signatureId = null,
            string? signatureHash = null,
            CancellationToken cancellationToken = default)
        {
            Events.Add(new AuditEvent(
                userId,
                eventType,
                tableName,
                module,
                recordId,
                description,
                ip,
                severity,
                deviceInfo,
                sessionId,
                signatureId,
                signatureHash));
            return Task.CompletedTask;
        }

        public sealed record AuditEvent(
            int? UserId,
            string? EventType,
            string? TableName,
            string? Module,
            int? RecordId,
            string? Description,
            string? Ip,
            string? Severity,
            string? DeviceInfo,
            string? SessionId,
            int? SignatureId,
            string? SignatureHash);
    }
}
