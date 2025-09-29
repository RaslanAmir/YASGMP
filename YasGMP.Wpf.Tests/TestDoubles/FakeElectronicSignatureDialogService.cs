using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Tests.TestDoubles;

public sealed class FakeElectronicSignatureDialogService : IElectronicSignatureDialogService
{
    private readonly Queue<ElectronicSignatureDialogResult?> _queuedResults = new();

    public List<ElectronicSignatureContext> Requests { get; } = new();

    public ElectronicSignatureDialogResult? DefaultResult { get; set; } = new ElectronicSignatureDialogResult(
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

    public Func<ElectronicSignatureContext, Exception?>? ExceptionFactory { get; set; }

    public Func<ElectronicSignatureContext, ElectronicSignatureDialogResult?>? ResultFactory { get; set; }

    public void QueueResult(ElectronicSignatureDialogResult? result)
        => _queuedResults.Enqueue(result);

    public void ClearQueuedResults()
        => _queuedResults.Clear();

    public Task<ElectronicSignatureDialogResult?> CaptureSignatureAsync(
        ElectronicSignatureContext context,
        CancellationToken cancellationToken = default)
    {
        Requests.Add(context);

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

        if (_queuedResults.Count > 0)
        {
            return Task.FromResult(_queuedResults.Dequeue());
        }

        if (ResultFactory is not null)
        {
            return Task.FromResult(ResultFactory(context));
        }

        return Task.FromResult(DefaultResult);
    }
}
