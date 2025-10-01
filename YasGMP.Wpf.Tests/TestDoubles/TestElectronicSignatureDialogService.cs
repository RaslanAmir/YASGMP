using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Tests.TestDoubles;

public sealed class TestElectronicSignatureDialogService : IElectronicSignatureDialogService
{
    private readonly Queue<CaptureInstruction> _captureQueue = new();
    private readonly Queue<Func<ElectronicSignatureDialogResult, Exception?>> _persistQueue = new();
    private int _nextSignatureId = 1;

    public List<ElectronicSignatureContext> Requests { get; } = new();
    public List<ElectronicSignatureDialogResult?> CapturedResults { get; } = new();
    public List<ElectronicSignatureDialogResult> PersistedResults { get; } = new();
    public List<PersistedSignatureRecord> PersistedSignatureRecords { get; } = new();
    public List<ElectronicSignatureDialogResult> LoggedAuditResults { get; } = new();

    public int PersistInvocationCount { get; private set; }
    public int LogPersistInvocationCount { get; private set; }

    public bool WasPersistInvoked => PersistInvocationCount > 0;
    public bool WasLogPersistInvoked => LogPersistInvocationCount > 0;

    public PersistedSignatureRecord? LastPersistedSignature
        => PersistedSignatureRecords.Count > 0 ? PersistedSignatureRecords[^1] : null;

    public int? LastPersistedSignatureId => LastPersistedSignature?.SignatureId;

    public int? LastPersistedRecordId => LastPersistedSignature?.RecordId;

    public ElectronicSignatureDialogResult DefaultResult { get; set; } = new ElectronicSignatureDialogResult(
        "password",
        "QA",
        "Automated test",
        "QA Reason",
        new DigitalSignature
        {
            SignatureHash = "test-signature",
            Method = "password",
            Status = "valid"
        });

    public Exception? ExceptionToThrow { get; set; }
    public Exception? PersistExceptionToThrow { get; set; }

    public Func<ElectronicSignatureContext, Exception?>? ExceptionFactory { get; set; }
    public Func<ElectronicSignatureDialogResult, Exception?>? PersistExceptionFactory { get; set; }

    public Func<ElectronicSignatureContext, ElectronicSignatureDialogResult?>? ResultFactory { get; set; }

    public static TestElectronicSignatureDialogService CreateConfirmed(ElectronicSignatureDialogResult? result = null)
    {
        var service = new TestElectronicSignatureDialogService();
        service.QueueResult(result ?? service.DefaultResult);
        return service;
    }

    public static TestElectronicSignatureDialogService CreateCancelled()
    {
        var service = new TestElectronicSignatureDialogService();
        service.QueueCancellation();
        return service;
    }

    public static TestElectronicSignatureDialogService CreateCaptureException(Exception exception)
    {
        if (exception is null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        var service = new TestElectronicSignatureDialogService();
        service.QueueCaptureException(exception);
        return service;
    }

    public void QueueResult(ElectronicSignatureDialogResult? result)
        => _captureQueue.Enqueue(CaptureInstruction.FromResult(result));

    public void QueueCancellation()
        => QueueResult(null);

    public void QueueCapture(Func<ElectronicSignatureContext, ElectronicSignatureDialogResult?> factory)
    {
        if (factory is null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        _captureQueue.Enqueue(CaptureInstruction.FromResultFactory(factory));
    }

    public void QueueCaptureException(Exception exception)
    {
        if (exception is null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        _captureQueue.Enqueue(CaptureInstruction.FromException(exception));
    }

    public void QueueCaptureException(Func<ElectronicSignatureContext, Exception?> factory)
    {
        if (factory is null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        _captureQueue.Enqueue(CaptureInstruction.FromExceptionFactory(factory));
    }

    public void ClearQueuedResults()
        => _captureQueue.Clear();

    public void QueuePersist(Func<ElectronicSignatureDialogResult, Exception?> factory)
    {
        if (factory is null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        _persistQueue.Enqueue(factory);
    }

    public void QueuePersistException(Exception exception)
    {
        if (exception is null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        _persistQueue.Enqueue(_ => exception);
    }

    public Task<ElectronicSignatureDialogResult?> CaptureSignatureAsync(
        ElectronicSignatureContext context,
        CancellationToken cancellationToken = default)
    {
        Requests.Add(context);

        if (_captureQueue.Count > 0)
        {
            var outcome = _captureQueue.Dequeue().Execute(context);
            if (outcome.Exception is not null)
            {
                throw outcome.Exception;
            }

            CapturedResults.Add(outcome.Result);
            return Task.FromResult(outcome.Result);
        }

        if (ExceptionFactory is not null)
        {
            var exception = ExceptionFactory(context);
            if (exception is not null)
            {
                throw exception;
            }
        }

        if (ExceptionToThrow is not null)
        {
            var exception = ExceptionToThrow;
            ExceptionToThrow = null;
            throw exception;
        }

        if (ResultFactory is not null)
        {
            var result = ResultFactory(context);
            CapturedResults.Add(result);
            return Task.FromResult(result);
        }

        var defaultResultClone = CloneResult(DefaultResult);
        CapturedResults.Add(defaultResultClone);
        return Task.FromResult<ElectronicSignatureDialogResult?>(defaultResultClone);
    }

    public Task PersistSignatureAsync(
        ElectronicSignatureDialogResult result,
        CancellationToken cancellationToken = default)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        if (_persistQueue.Count > 0)
        {
            var exception = _persistQueue.Dequeue()(result);
            if (exception is not null)
            {
                throw exception;
            }
        }

        if (PersistExceptionFactory is not null)
        {
            var exception = PersistExceptionFactory(result);
            if (exception is not null)
            {
                throw exception;
            }
        }

        if (PersistExceptionToThrow is not null)
        {
            var exception = PersistExceptionToThrow;
            PersistExceptionToThrow = null;
            throw exception;
        }

        var signature = result.Signature;
        if (signature is null)
        {
            throw new InvalidOperationException("Persisted signature result must include a signature instance.");
        }

        PersistInvocationCount++;

        var assignedSignatureId = signature.Id > 0 ? signature.Id : _nextSignatureId++;
        signature.Id = assignedSignatureId;

        var persistedResult = CloneResult(result);
        PersistedResults.Add(persistedResult);

        if (persistedResult.Signature is null)
        {
            throw new InvalidOperationException("Persisted signature result must include a signature instance.");
        }

        persistedResult.Signature.Id = assignedSignatureId;

        PersistedSignatureRecords.Add(new PersistedSignatureRecord(
            assignedSignatureId,
            persistedResult.Signature.SignatureHash,
            persistedResult.Signature.Method,
            persistedResult.Signature.Status,
            persistedResult.Signature.Note,
            persistedResult.Signature.RecordId));

        return Task.CompletedTask;
    }

    public Task LogPersistedSignatureAsync(
        ElectronicSignatureDialogResult result,
        CancellationToken cancellationToken = default)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        if (result.Signature is null)
        {
            throw new ArgumentException("Signature result must include a signature payload.", nameof(result));
        }

        LogPersistInvocationCount++;
        LoggedAuditResults.Add(CloneResult(result));

        return Task.CompletedTask;
    }

    private static ElectronicSignatureDialogResult CloneResult(ElectronicSignatureDialogResult result)
    {
        if (result.Signature is null)
        {
            throw new InvalidOperationException("ElectronicSignatureDialogResult.Signature cannot be null.");
        }

        return result with
        {
            Signature = CloneSignature(result.Signature)
        };
    }

    private static DigitalSignature CloneSignature(DigitalSignature signature)
        => new()
        {
            Id = signature.Id,
            TableName = signature.TableName,
            RecordId = signature.RecordId,
            UserId = signature.UserId,
            SignatureHash = signature.SignatureHash,
            Method = signature.Method,
            Status = signature.Status,
            SignedAt = signature.SignedAt,
            DeviceInfo = signature.DeviceInfo,
            IpAddress = signature.IpAddress,
            Note = signature.Note,
            SessionId = signature.SessionId,
            UserName = signature.UserName,
            PublicKey = signature.PublicKey
        };

    public sealed record PersistedSignatureRecord(
        int SignatureId,
        string? SignatureHash,
        string? Method,
        string? Status,
        string? Note,
        int RecordId);

    private sealed record CaptureInstruction(Func<ElectronicSignatureContext, CaptureOutcome> Resolver)
    {
        public CaptureOutcome Execute(ElectronicSignatureContext context)
            => Resolver(context);

        public static CaptureInstruction FromResult(ElectronicSignatureDialogResult? result)
            => new(_ => new CaptureOutcome(result, null));

        public static CaptureInstruction FromResultFactory(Func<ElectronicSignatureContext, ElectronicSignatureDialogResult?> factory)
            => new(ctx => new CaptureOutcome(factory(ctx), null));

        public static CaptureInstruction FromException(Exception exception)
            => new(_ => new CaptureOutcome(null, exception));

        public static CaptureInstruction FromExceptionFactory(Func<ElectronicSignatureContext, Exception?> factory)
            => new(ctx => new CaptureOutcome(null, factory(ctx)));
    }

    private readonly record struct CaptureOutcome(ElectronicSignatureDialogResult? Result, Exception? Exception);
}
