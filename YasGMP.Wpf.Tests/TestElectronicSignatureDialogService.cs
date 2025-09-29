using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Tests;

public sealed class TestElectronicSignatureDialogService : IElectronicSignatureDialogService
{
    public List<ElectronicSignatureContext> Requests { get; } = new();

    public ElectronicSignatureDialogResult? Result { get; set; } = new ElectronicSignatureDialogResult(
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

    public Task<ElectronicSignatureDialogResult?> CaptureSignatureAsync(
        ElectronicSignatureContext context,
        CancellationToken cancellationToken = default)
    {
        Requests.Add(context);

        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        return Task.FromResult(Result);
    }
}
