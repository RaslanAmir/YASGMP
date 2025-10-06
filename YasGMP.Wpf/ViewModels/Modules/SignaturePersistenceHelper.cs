using System;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Models.DTO;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.ViewModels.Modules;

internal static class SignaturePersistenceHelper
{
    public static void ApplyEntityMetadata(
        ElectronicSignatureDialogResult signatureResult,
        string tableName,
        int recordId,
        SignatureMetadataDto? metadata,
        string? fallbackSignatureHash,
        string? fallbackMethod,
        string? fallbackStatus,
        string? fallbackNote,
        DateTime? signedAt,
        string? fallbackDeviceInfo,
        string? fallbackIpAddress,
        string? fallbackSessionId)
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

        if (metadata?.Id is { } metadataId && metadataId > 0)
        {
            signature.Id = metadataId;
        }

        var signatureHash = !string.IsNullOrWhiteSpace(metadata?.Hash)
            ? metadata!.Hash
            : fallbackSignatureHash;
        if (!string.IsNullOrWhiteSpace(signatureHash))
        {
            signature.SignatureHash = signatureHash;
        }

        var method = !string.IsNullOrWhiteSpace(metadata?.Method)
            ? metadata!.Method
            : fallbackMethod;
        if (!string.IsNullOrWhiteSpace(method))
        {
            signature.Method = method;
        }

        var status = !string.IsNullOrWhiteSpace(metadata?.Status)
            ? metadata!.Status
            : fallbackStatus;
        if (!string.IsNullOrWhiteSpace(status))
        {
            signature.Status = status;
        }

        var note = !string.IsNullOrWhiteSpace(metadata?.Note)
            ? metadata!.Note
            : fallbackNote;
        if (!string.IsNullOrWhiteSpace(note))
        {
            signature.Note = note;
        }

        if (signedAt.HasValue)
        {
            signature.SignedAt = signedAt;
        }

        var deviceInfo = !string.IsNullOrWhiteSpace(metadata?.Device)
            ? metadata!.Device
            : fallbackDeviceInfo;
        if (!string.IsNullOrWhiteSpace(deviceInfo))
        {
            signature.DeviceInfo = deviceInfo;
        }

        var ipAddress = !string.IsNullOrWhiteSpace(metadata?.IpAddress)
            ? metadata!.IpAddress
            : fallbackIpAddress;
        if (!string.IsNullOrWhiteSpace(ipAddress))
        {
            signature.IpAddress = ipAddress;
        }

        var sessionId = !string.IsNullOrWhiteSpace(metadata?.Session)
            ? metadata!.Session
            : fallbackSessionId;
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            signature.SessionId = sessionId;
        }
    }

    public static async Task PersistIfRequiredAsync(
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
            await signatureDialog
                .LogPersistedSignatureAsync(signatureResult, cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        await signatureDialog
            .PersistSignatureAsync(signatureResult, cancellationToken)
            .ConfigureAwait(false);
    }
}



