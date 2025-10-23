using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Adapter that composes the legacy DatabaseService SOP helpers with attachment and
/// electronic signature workflows for the WPF shell.
/// </summary>
public sealed class SopGovernanceServiceAdapter : ISopGovernanceService
{
    private const string EntityType = "sop_documents";

    private readonly DatabaseService _databaseService;
    private readonly IAuthContext _authContext;
    private readonly IAttachmentWorkflowService _attachmentWorkflow;
    private readonly IAttachmentService _attachmentService;
    private readonly IElectronicSignatureDialogService _signatureDialog;

    /// <summary>
    /// Initializes a new instance of the <see cref="SopGovernanceServiceAdapter"/> class.
    /// </summary>
    public SopGovernanceServiceAdapter(
        DatabaseService databaseService,
        IAuthContext authContext,
        IAttachmentWorkflowService attachmentWorkflow,
        IAttachmentService attachmentService,
        IElectronicSignatureDialogService signatureDialog)
    {
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _attachmentWorkflow = attachmentWorkflow ?? throw new ArgumentNullException(nameof(attachmentWorkflow));
        _attachmentService = attachmentService ?? throw new ArgumentNullException(nameof(attachmentService));
        _signatureDialog = signatureDialog ?? throw new ArgumentNullException(nameof(signatureDialog));
    }

    /// <inheritdoc />
    public async Task<SopGovernanceLoadResult> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var documents = await _databaseService
                .GetSopDocumentsAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var document in documents)
            {
                await HydrateAttachmentsAsync(document, cancellationToken).ConfigureAwait(false);
            }

            string message = string.Format(
                CultureInfo.CurrentCulture,
                "Loaded {0} SOP document(s).",
                documents?.Count ?? 0);

            return new SopGovernanceLoadResult(true, message, documents ?? Array.Empty<SopDocument>());
        }
        catch (Exception ex)
        {
            return new SopGovernanceLoadResult(
                false,
                $"Failed to load SOP documents: {ex.Message}",
                Array.Empty<SopDocument>());
        }
    }

    /// <inheritdoc />
    public async Task<SopGovernanceOperationResult> CreateAsync(
        SopDocument draft,
        IEnumerable<SopAttachmentUpload>? attachments = null,
        CancellationToken cancellationToken = default)
    {
        if (draft is null)
        {
            throw new ArgumentNullException(nameof(draft));
        }

        try
        {
            var prepared = draft.DeepCopy();
            ApplyActorMetadata(prepared, isNew: true);

            var signatureResult = await _signatureDialog
                .CaptureSignatureAsync(new ElectronicSignatureContext(EntityType, 0), cancellationToken)
                .ConfigureAwait(false);

            if (signatureResult?.Signature is null)
            {
                return new SopGovernanceOperationResult(false, "Electronic signature cancelled.");
            }

            var actorId = _authContext.CurrentUser?.Id ?? 0;
            int newId = await _databaseService
                .CreateSopDocumentAsync(
                    prepared,
                    actorId,
                    _authContext.CurrentIpAddress,
                    _authContext.CurrentDeviceInfo,
                    _authContext.CurrentSessionId,
                    cancellationToken)
                .ConfigureAwait(false);

            PrepareSignature(signatureResult.Signature, newId, actorId);
            var signatureId = await _databaseService
                .InsertDigitalSignatureAsync(signatureResult.Signature, cancellationToken)
                .ConfigureAwait(false);
            signatureResult.Signature.Id = signatureId;

            await _signatureDialog
                .LogPersistedSignatureAsync(signatureResult, cancellationToken)
                .ConfigureAwait(false);

            await UploadAttachmentsAsync(newId, attachments, cancellationToken).ConfigureAwait(false);

            draft.Id = newId;
            await HydrateAttachmentsAsync(draft, cancellationToken).ConfigureAwait(false);

            string message = string.Format(
                CultureInfo.CurrentCulture,
                "Created SOP '{0}' (ID={1}).",
                string.IsNullOrWhiteSpace(prepared.Name) ? prepared.Code : prepared.Name,
                newId);

            return new SopGovernanceOperationResult(true, message, newId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new SopGovernanceOperationResult(false, $"Failed to create SOP: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<SopGovernanceOperationResult> UpdateAsync(
        SopDocument document,
        IEnumerable<SopAttachmentUpload>? attachments = null,
        CancellationToken cancellationToken = default)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (document.Id <= 0)
        {
            return new SopGovernanceOperationResult(false, "Select a SOP document before updating.");
        }

        try
        {
            var prepared = document.DeepCopy();
            ApplyActorMetadata(prepared, isNew: false);

            var signatureResult = await _signatureDialog
                .CaptureSignatureAsync(new ElectronicSignatureContext(EntityType, document.Id), cancellationToken)
                .ConfigureAwait(false);

            if (signatureResult?.Signature is null)
            {
                return new SopGovernanceOperationResult(false, "Electronic signature cancelled.", document.Id);
            }

            var actorId = _authContext.CurrentUser?.Id ?? 0;
            await _databaseService
                .UpdateSopDocumentAsync(
                    prepared,
                    actorId,
                    _authContext.CurrentIpAddress,
                    _authContext.CurrentDeviceInfo,
                    _authContext.CurrentSessionId,
                    cancellationToken)
                .ConfigureAwait(false);

            PrepareSignature(signatureResult.Signature, document.Id, actorId);
            var signatureId = await _databaseService
                .InsertDigitalSignatureAsync(signatureResult.Signature, cancellationToken)
                .ConfigureAwait(false);
            signatureResult.Signature.Id = signatureId;

            await _signatureDialog
                .LogPersistedSignatureAsync(signatureResult, cancellationToken)
                .ConfigureAwait(false);

            await UploadAttachmentsAsync(document.Id, attachments, cancellationToken).ConfigureAwait(false);
            await HydrateAttachmentsAsync(document, cancellationToken).ConfigureAwait(false);

            string message = string.Format(
                CultureInfo.CurrentCulture,
                "Updated SOP '{0}'.",
                string.IsNullOrWhiteSpace(prepared.Name) ? prepared.Code : prepared.Name);

            return new SopGovernanceOperationResult(true, message, document.Id);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new SopGovernanceOperationResult(false, $"Failed to update SOP: {ex.Message}", document.Id);
        }
    }

    /// <inheritdoc />
    public async Task<SopGovernanceOperationResult> DeleteAsync(
        SopDocument document,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (document.Id <= 0)
        {
            return new SopGovernanceOperationResult(false, "Select a SOP document before deleting.");
        }

        _ = reason;

        try
        {
            var signatureResult = await _signatureDialog
                .CaptureSignatureAsync(new ElectronicSignatureContext(EntityType, document.Id), cancellationToken)
                .ConfigureAwait(false);

            if (signatureResult?.Signature is null)
            {
                return new SopGovernanceOperationResult(false, "Electronic signature cancelled.", document.Id);
            }

            var actorId = _authContext.CurrentUser?.Id ?? 0;
            await _databaseService
                .DeleteSopDocumentAsync(
                    document.Id,
                    actorId,
                    _authContext.CurrentIpAddress,
                    _authContext.CurrentDeviceInfo,
                    _authContext.CurrentSessionId,
                    cancellationToken)
                .ConfigureAwait(false);

            PrepareSignature(signatureResult.Signature, document.Id, actorId);
            var signatureId = await _databaseService
                .InsertDigitalSignatureAsync(signatureResult.Signature, cancellationToken)
                .ConfigureAwait(false);
            signatureResult.Signature.Id = signatureId;

            await _signatureDialog
                .LogPersistedSignatureAsync(signatureResult, cancellationToken)
                .ConfigureAwait(false);

            await RemoveAttachmentLinksAsync(document.Id, cancellationToken).ConfigureAwait(false);

            string message = string.Format(
                CultureInfo.CurrentCulture,
                "Deleted SOP '{0}' (ID={1}).",
                string.IsNullOrWhiteSpace(document.Name) ? document.Code : document.Name,
                document.Id);

            return new SopGovernanceOperationResult(true, message, document.Id);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new SopGovernanceOperationResult(false, $"Failed to delete SOP: {ex.Message}", document.Id);
        }
    }

    /// <inheritdoc />
    public async Task<SopGovernanceExportResult> ExportAsync(
        IList<SopDocument> documents,
        string format,
        CancellationToken cancellationToken = default)
    {
        if (documents is null)
        {
            throw new ArgumentNullException(nameof(documents));
        }

        if (string.IsNullOrWhiteSpace(format))
        {
            return new SopGovernanceExportResult(false, "Select an export format.", null);
        }

        try
        {
            var actorId = _authContext.CurrentUser?.Id ?? 0;
            var path = await _databaseService
                .ExportDocumentsAsync(
                    documents.ToList(),
                    format!,
                    actorId,
                    _authContext.CurrentIpAddress,
                    _authContext.CurrentDeviceInfo,
                    _authContext.CurrentSessionId,
                    cancellationToken)
                .ConfigureAwait(false);

            string message = string.Format(
                CultureInfo.CurrentCulture,
                "Export completed ({0}).",
                format);

            return new SopGovernanceExportResult(true, message, path);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new SopGovernanceExportResult(false, $"Failed to export SOPs: {ex.Message}", null);
        }
    }

    private async Task HydrateAttachmentsAsync(SopDocument document, CancellationToken cancellationToken)
    {
        if (document is null || document.Id <= 0)
        {
            return;
        }

        var links = await _attachmentService
            .GetLinksForEntityAsync(EntityType, document.Id, cancellationToken)
            .ConfigureAwait(false);

        if (links is null || links.Count == 0)
        {
            document.Attachments = new List<string>();
            return;
        }

        document.Attachments = links
            .Select(l => !string.IsNullOrWhiteSpace(l.Attachment.FileName)
                ? l.Attachment.FileName
                : l.Attachment.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!.Trim())
            .ToList();
    }

    private async Task UploadAttachmentsAsync(
        int documentId,
        IEnumerable<SopAttachmentUpload>? attachments,
        CancellationToken cancellationToken)
    {
        if (attachments is null)
        {
            return;
        }

        var actorId = _authContext.CurrentUser?.Id;

        foreach (var attachment in attachments)
        {
            await using var stream = await attachment.OpenStreamAsync(cancellationToken).ConfigureAwait(false);

            var request = new AttachmentUploadRequest
            {
                FileName = attachment.FileName,
                ContentType = attachment.ContentType ?? "application/octet-stream",
                EntityType = EntityType,
                EntityId = documentId,
                UploadedById = actorId,
                DisplayName = attachment.DisplayName ?? attachment.FileName,
                Notes = attachment.Reason,
                Reason = attachment.Reason,
                SourceIp = _authContext.CurrentIpAddress,
                SourceHost = _authContext.CurrentDeviceInfo
            };

            await _attachmentWorkflow
                .UploadAsync(stream, request, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task RemoveAttachmentLinksAsync(int documentId, CancellationToken cancellationToken)
    {
        var links = await _attachmentService
            .GetLinksForEntityAsync(EntityType, documentId, cancellationToken)
            .ConfigureAwait(false);

        if (links is null || links.Count == 0)
        {
            return;
        }

        foreach (var link in links)
        {
            await _attachmentService
                .RemoveLinkAsync(link.Link.Id, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private void ApplyActorMetadata(SopDocument document, bool isNew)
    {
        var actorId = _authContext.CurrentUser?.Id ?? 0;
        document.LastModified = EnsureUtc(document.LastModified == default ? DateTime.UtcNow : document.LastModified);
        document.SourceIp = string.IsNullOrWhiteSpace(document.SourceIp)
            ? _authContext.CurrentIpAddress
            : document.SourceIp;

        if (actorId > 0)
        {
            document.LastModifiedById = actorId;
            if (isNew)
            {
                if (document.ResponsibleUserId == 0)
                {
                    document.ResponsibleUserId = actorId;
                }

                if (!document.CreatedById.HasValue)
                {
                    document.CreatedById = actorId;
                }
            }
        }

        document.DateIssued = EnsureUtc(document.DateIssued == default ? DateTime.UtcNow : document.DateIssued);
        if (document.DateExpiry.HasValue)
        {
            document.DateExpiry = EnsureUtc(document.DateExpiry.Value);
        }

        if (document.NextReviewDate.HasValue)
        {
            document.NextReviewDate = EnsureUtc(document.NextReviewDate.Value);
        }
    }

    private void PrepareSignature(DigitalSignature signature, int recordId, int actorId)
    {
        signature.TableName = EntityType;
        signature.RecordId = recordId;
        signature.UserId = actorId == 0 ? signature.UserId : actorId;
        signature.DeviceInfo ??= _authContext.CurrentDeviceInfo;
        signature.IpAddress ??= _authContext.CurrentIpAddress;
        signature.SessionId ??= _authContext.CurrentSessionId;
        signature.Method ??= "password";
        signature.Status ??= "valid";
        signature.SignedAt ??= DateTime.UtcNow;
    }

    private static DateTime EnsureUtc(DateTime value)
        => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => value.ToUniversalTime()
        };
}
