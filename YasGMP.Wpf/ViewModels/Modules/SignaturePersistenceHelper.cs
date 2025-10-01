using System;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.ViewModels.Modules;

internal static class SignaturePersistenceHelper
{
    public static void ApplyEntityMetadata(
        ElectronicSignatureDialogResult signatureResult,
        string tableName,
        int recordId,
        int? signatureId,
        string? signatureHash,
        string? method,
        string? status,
        string? note,
        DateTime? signedAt,
        string? deviceInfo,
        string? ipAddress,
        string? sessionId)
    {
        if (signatureResult is null)
        {
            throw new ArgumentNullException(nameof(signatureResult));
        }

        if (signatureResult.Signature is null)
        {
            throw new ArgumentException("Signature result is missing the signature payload.", nameof(signatureResult));
        }

        var signature = signatureResult.Signature;
        signature.TableName = tableName ?? signature.TableName;
        signature.RecordId = recordId;

        if (signatureId.HasValue && signatureId.Value > 0)
        {
            signature.Id = signatureId.Value;
        }

        if (!string.IsNullOrWhiteSpace(signatureHash))
        {
            signature.SignatureHash = signatureHash;
        }

        if (!string.IsNullOrWhiteSpace(method))
        {
            signature.Method = method;
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            signature.Status = status;
        }

        if (!string.IsNullOrWhiteSpace(note))
        {
            signature.Note = note;
        }

        if (signedAt.HasValue)
        {
            signature.SignedAt = signedAt;
        }

        if (!string.IsNullOrWhiteSpace(deviceInfo))
        {
            signature.DeviceInfo = deviceInfo;
        }

        if (!string.IsNullOrWhiteSpace(ipAddress))
        {
            signature.IpAddress = ipAddress;
        }

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            signature.SessionId = sessionId;
        }
    }

    public static Task PersistIfRequiredAsync(
        IElectronicSignatureDialogService signatureDialog,
        ElectronicSignatureDialogResult signatureResult,
        CancellationToken cancellationToken = default)
    {
        if (signatureDialog is null)
        {
            throw new ArgumentNullException(nameof(signatureDialog));
        }

        if (signatureResult is null)
        {
            throw new ArgumentNullException(nameof(signatureResult));
        }

        if (signatureResult.Signature is null)
        {
            throw new ArgumentException("Signature result is missing the signature payload.", nameof(signatureResult));
        }

        if (signatureResult.Signature.Id > 0)
        {
            return Task.CompletedTask;
        }

        return signatureDialog.PersistSignatureAsync(signatureResult, cancellationToken);
    }
}
