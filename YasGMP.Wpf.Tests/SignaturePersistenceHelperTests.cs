using System.Threading.Tasks;
using Xunit;
using YasGMP.AppCore.Models.Signatures;
using YasGMP.Wpf.Tests.TestDoubles;
using YasGMP.Wpf.ViewModels.Dialogs;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Tests;

public sealed class SignaturePersistenceHelperTests
{
    [Fact]
    public async Task PersistIfRequiredAsync_WhenSignatureHasIdentifier_LogsWithoutPersisting()
    {
        var dialogService = new TestElectronicSignatureDialogService();
        var signature = new DigitalSignature
        {
            Id = 445,
            TableName = "components",
            RecordId = 99,
            Method = "password",
            Status = "valid"
        };

        var result = new ElectronicSignatureDialogResult(
            "password",
            "QA",
            "detail",
            "QA Reason",
            signature);

        await SignaturePersistenceHelper.PersistIfRequiredAsync(dialogService, result);

        Assert.Equal(0, dialogService.PersistInvocationCount);
        Assert.Empty(dialogService.PersistedResults);

        var logResult = Assert.Single(dialogService.LoggedAuditResults);
        Assert.Equal(signature.Id, logResult.Signature?.Id);
        Assert.Equal(signature.TableName, logResult.Signature?.TableName);
        Assert.Equal(signature.RecordId, logResult.Signature?.RecordId);
    }

    [Fact]
    public async Task PersistIfRequiredAsync_WhenSignatureIsNew_Persists()
    {
        var dialogService = new TestElectronicSignatureDialogService();
        var signature = new DigitalSignature
        {
            TableName = "components",
            RecordId = 100,
            Method = "password",
            Status = "valid"
        };

        var result = new ElectronicSignatureDialogResult(
            "password",
            "QA",
            "detail",
            "QA Reason",
            signature);

        await SignaturePersistenceHelper.PersistIfRequiredAsync(dialogService, result);

        Assert.Equal(1, dialogService.PersistInvocationCount);
        Assert.Single(dialogService.PersistedResults);
        Assert.Empty(dialogService.LoggedAuditResults);
    }
}
