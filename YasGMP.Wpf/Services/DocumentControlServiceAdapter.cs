using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Models.DTO;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Adapter that composes DatabaseService helpers with attachment and signature workflows for document control operations.
/// </summary>
public sealed class DocumentControlServiceAdapter : IDocumentControlService
{
    private const string EntityType = "sop_documents";

    private readonly DatabaseService _databaseService;
    private readonly IAuthContext _authContext;
    private readonly IAttachmentWorkflowService _attachmentWorkflow;
    private readonly IAttachmentService _attachmentService;
    private readonly IElectronicSignatureDialogService _signatureDialog;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentControlServiceAdapter"/> class.
    /// </summary>
    public DocumentControlServiceAdapter(
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
    public async Task<DocumentLifecycleResult> InitiateDocumentAsync(
        SopDocument draft,
        CancellationToken cancellationToken = default)
    {
        if (draft is null)
        {
            throw new ArgumentNullException(nameof(draft));
        }

        try
        {
            var actorId = _authContext.CurrentUser?.Id ?? 0;
            string code = string.IsNullOrWhiteSpace(draft.Code)
                ? $"DOC-{DateTime.UtcNow:yyyyMMddHHmmss}"
                : draft.Code!;
            string name = string.IsNullOrWhiteSpace(draft.Name) ? "New Document" : draft.Name!;
            string version = draft.VersionNo <= 0
                ? "1"
                : draft.VersionNo.ToString(CultureInfo.InvariantCulture);
            string? notes = string.IsNullOrWhiteSpace(draft.Description)
                ? draft.ReviewNotes
                : draft.Description;

            int id = await _databaseService
                .InitiateDocumentAsync(code, name, version, draft.FilePath, actorId, notes, cancellationToken)
                .ConfigureAwait(false);

            await _databaseService
                .LogDocumentAuditAsync(
                    id,
                    "INITIATE",
                    actorId,
                    notes,
                    _authContext.CurrentIpAddress,
                    _authContext.CurrentDeviceInfo,
                    _authContext.CurrentSessionId,
                    cancellationToken)
                .ConfigureAwait(false);

            return new DocumentLifecycleResult(true, $"Document initiated (ID={id}).", id);
        }
        catch (Exception ex)
        {
            return new DocumentLifecycleResult(false, $"Initiation failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<DocumentLifecycleResult> ReviseDocumentAsync(
        SopDocument existing,
        SopDocument revision,
        CancellationToken cancellationToken = default)
    {
        if (existing is null)
        {
            throw new ArgumentNullException(nameof(existing));
        }

        if (revision is null)
        {
            throw new ArgumentNullException(nameof(revision));
        }

        if (existing.Id <= 0)
        {
            return new DocumentLifecycleResult(false, "Select a document before saving.");
        }

        ElectronicSignatureDialogResult? signatureResult;
        try
        {
            signatureResult = await _signatureDialog
                .CaptureSignatureAsync(new ElectronicSignatureContext(EntityType, existing.Id), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new DocumentLifecycleResult(false, $"Electronic signature failed: {ex.Message}");
        }

        if (signatureResult is null || signatureResult.Signature is null)
        {
            return new DocumentLifecycleResult(false, "Electronic signature cancelled.");
        }

        try
        {
            var actorId = _authContext.CurrentUser?.Id ?? 0;
            string newVersion = revision.VersionNo <= 0
                ? "1"
                : revision.VersionNo.ToString(CultureInfo.InvariantCulture);

            await _databaseService
                .ReviseDocumentAsync(
                    existing.Id,
                    newVersion,
                    revision.FilePath,
                    actorId,
                    _authContext.CurrentIpAddress,
                    _authContext.CurrentDeviceInfo,
                    cancellationToken)
                .ConfigureAwait(false);

            PrepareSignature(signatureResult.Signature, existing.Id, actorId);

            int signatureId = await _databaseService
                .InsertDigitalSignatureAsync(signatureResult.Signature, cancellationToken)
                .ConfigureAwait(false);
            signatureResult.Signature.Id = signatureId;

            await _databaseService
                .LogDocumentAuditAsync(
                    existing.Id,
                    "REVISE",
                    actorId,
                    $"Revised to v{newVersion}",
                    _authContext.CurrentIpAddress,
                    _authContext.CurrentDeviceInfo,
                    _authContext.CurrentSessionId,
                    cancellationToken)
                .ConfigureAwait(false);

            await _signatureDialog
                .LogPersistedSignatureAsync(signatureResult, cancellationToken)
                .ConfigureAwait(false);

            return new DocumentLifecycleResult(true, $"Document '{existing.Name}' revised to v{newVersion}.", existing.Id);
        }
        catch (Exception ex)
        {
            return new DocumentLifecycleResult(false, $"Revision failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<DocumentLifecycleResult> ApproveDocumentAsync(
        SopDocument document,
        CancellationToken cancellationToken = default)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (document.Id <= 0)
        {
            return new DocumentLifecycleResult(false, "Select a document before approving.");
        }

        try
        {
            var actorId = _authContext.CurrentUser?.Id ?? 0;
            await _databaseService
                .ApproveDocumentAsync(
                    document.Id,
                    actorId,
                    _authContext.CurrentIpAddress,
                    _authContext.CurrentDeviceInfo,
                    null,
                    cancellationToken)
                .ConfigureAwait(false);

            await _databaseService
                .LogDocumentAuditAsync(
                    document.Id,
                    "APPROVE",
                    actorId,
                    "Approved",
                    _authContext.CurrentIpAddress,
                    _authContext.CurrentDeviceInfo,
                    _authContext.CurrentSessionId,
                    cancellationToken)
                .ConfigureAwait(false);

            return new DocumentLifecycleResult(true, $"Document '{document.Name}' approved.", document.Id);
        }
        catch (Exception ex)
        {
            return new DocumentLifecycleResult(false, $"Approval failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<DocumentLifecycleResult> PublishDocumentAsync(
        SopDocument document,
        CancellationToken cancellationToken = default)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (document.Id <= 0)
        {
            return new DocumentLifecycleResult(false, "Select a document before publishing.");
        }

        try
        {
            var actorId = _authContext.CurrentUser?.Id ?? 0;
            await _databaseService
                .PublishDocumentAsync(
                    document.Id,
                    actorId,
                    _authContext.CurrentIpAddress,
                    _authContext.CurrentDeviceInfo,
                    cancellationToken)
                .ConfigureAwait(false);

            await _databaseService
                .LogDocumentAuditAsync(
                    document.Id,
                    "PUBLISH",
                    actorId,
                    "Published",
                    _authContext.CurrentIpAddress,
                    _authContext.CurrentDeviceInfo,
                    _authContext.CurrentSessionId,
                    cancellationToken)
                .ConfigureAwait(false);

            return new DocumentLifecycleResult(true, $"Document '{document.Name}' published.", document.Id);
        }
        catch (Exception ex)
        {
            return new DocumentLifecycleResult(false, $"Publishing failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<DocumentLifecycleResult> ExpireDocumentAsync(
        SopDocument document,
        CancellationToken cancellationToken = default)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (document.Id <= 0)
        {
            return new DocumentLifecycleResult(false, "Select a document before expiring.");
        }

        try
        {
            var actorId = _authContext.CurrentUser?.Id ?? 0;
            await _databaseService
                .ExpireDocumentAsync(
                    document.Id,
                    actorId,
                    _authContext.CurrentIpAddress,
                    _authContext.CurrentDeviceInfo,
                    cancellationToken)
                .ConfigureAwait(false);

            await _databaseService
                .LogDocumentAuditAsync(
                    document.Id,
                    "EXPIRE",
                    actorId,
                    "Expired",
                    _authContext.CurrentIpAddress,
                    _authContext.CurrentDeviceInfo,
                    _authContext.CurrentSessionId,
                    cancellationToken)
                .ConfigureAwait(false);

            return new DocumentLifecycleResult(true, $"Document '{document.Name}' expired.", document.Id);
        }
        catch (Exception ex)
        {
            return new DocumentLifecycleResult(false, $"Expiration failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<DocumentLifecycleResult> LinkChangeControlAsync(
        SopDocument document,
        ChangeControlSummaryDto changeControl,
        CancellationToken cancellationToken = default)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (changeControl is null)
        {
            throw new ArgumentNullException(nameof(changeControl));
        }

        if (document.Id <= 0)
        {
            return new DocumentLifecycleResult(false, "Select a document before linking a change control.");
        }

        if (changeControl.Id <= 0)
        {
            return new DocumentLifecycleResult(false, "Select a change control to link.");
        }

        try
        {
            var actorId = _authContext.CurrentUser?.Id ?? 0;
            await _databaseService
                .LinkChangeControlToDocumentAsync(
                    document.Id,
                    changeControl.Id,
                    actorId,
                    _authContext.CurrentIpAddress,
                    _authContext.CurrentDeviceInfo,
                    cancellationToken)
                .ConfigureAwait(false);

            string description = $"Linked CC #{changeControl.Id} ({changeControl.Code})";
            await _databaseService
                .LogDocumentAuditAsync(
                    document.Id,
                    "LINK_CHANGE",
                    actorId,
                    description,
                    _authContext.CurrentIpAddress,
                    _authContext.CurrentDeviceInfo,
                    _authContext.CurrentSessionId,
                    cancellationToken)
                .ConfigureAwait(false);

            return new DocumentLifecycleResult(true, $"Change control '{changeControl.Code}' linked to '{document.Name}'.", document.Id);
        }
        catch (DocumentControlLinkException ex)
        {
            return new DocumentLifecycleResult(false, ex.Message);
        }
        catch (Exception ex)
        {
            return new DocumentLifecycleResult(false, $"Linking failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<DocumentExportResult> ExportDocumentsAsync(
        IReadOnlyCollection<SopDocument> documents,
        string format,
        CancellationToken cancellationToken = default)
    {
        if (documents is null)
        {
            throw new ArgumentNullException(nameof(documents));
        }

        try
        {
            var actorId = _authContext.CurrentUser?.Id ?? 0;
            string exportPath = await _databaseService
                .ExportDocumentsAsync(
                    documents.ToList(),
                    format,
                    actorId,
                    _authContext.CurrentIpAddress,
                    _authContext.CurrentDeviceInfo,
                    _authContext.CurrentSessionId,
                    cancellationToken)
                .ConfigureAwait(false);

            string message = documents.Count == 0
                ? "No documents available to export."
                : $"Exported {documents.Count} document(s).";
            return new DocumentExportResult(true, message, exportPath);
        }
        catch (Exception ex)
        {
            return new DocumentExportResult(false, $"Export failed: {ex.Message}", null);
        }
    }

    /// <inheritdoc />
    public async Task<DocumentAttachmentUploadResult> UploadAttachmentsAsync(
        SopDocument document,
        IEnumerable<DocumentAttachmentUpload> attachments,
        CancellationToken cancellationToken = default)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (attachments is null)
        {
            throw new ArgumentNullException(nameof(attachments));
        }

        if (document.Id <= 0)
        {
            return new DocumentAttachmentUploadResult(false, "Select a document before uploading attachments.", 0, 0, Array.Empty<AttachmentLinkWithAttachment>());
        }

        int processed = 0;
        int deduplicated = 0;

        try
        {
            foreach (var descriptor in attachments)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await using var stream = await descriptor.OpenStreamAsync(cancellationToken).ConfigureAwait(false);
                var request = new AttachmentUploadRequest
                {
                    FileName = descriptor.FileName,
                    ContentType = descriptor.ContentType,
                    EntityType = EntityType,
                    EntityId = document.Id,
                    UploadedById = _authContext.CurrentUser?.Id,
                    Notes = $"WPF:{EntityType}:{DateTime.UtcNow:O}",
                    Reason = $"documentcontrol:{document.Id}",
                    SourceHost = string.IsNullOrWhiteSpace(_authContext.CurrentDeviceInfo)
                        ? Environment.MachineName
                        : _authContext.CurrentDeviceInfo,
                    SourceIp = _authContext.CurrentIpAddress
                };

                var result = await _attachmentWorkflow
                    .UploadAsync(stream, request, cancellationToken)
                    .ConfigureAwait(false);

                processed++;
                if (result.Deduplicated)
                {
                    deduplicated++;
                }
            }

            var manifest = await _attachmentService
                .GetLinksForEntityAsync(EntityType, document.Id, cancellationToken)
                .ConfigureAwait(false);

            string message = AttachmentStatusFormatter.Format(processed, deduplicated);
            return new DocumentAttachmentUploadResult(true, message, processed, deduplicated, manifest);
        }
        catch (Exception ex)
        {
            return new DocumentAttachmentUploadResult(false, $"Attachment upload failed: {ex.Message}", processed, deduplicated, Array.Empty<AttachmentLinkWithAttachment>());
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AttachmentLinkWithAttachment>> GetAttachmentManifestAsync(
        int documentId,
        CancellationToken cancellationToken = default)
    {
        if (documentId <= 0)
        {
            return Task.FromResult<IReadOnlyList<AttachmentLinkWithAttachment>>(Array.Empty<AttachmentLinkWithAttachment>());
        }

        return _attachmentService.GetLinksForEntityAsync(EntityType, documentId, cancellationToken);
    }

    private void PrepareSignature(DigitalSignature signature, int recordId, int userId)
    {
        signature.TableName = EntityType;
        signature.RecordId = recordId;
        signature.UserId = userId == 0 ? signature.UserId : userId;
        signature.DeviceInfo ??= _authContext.CurrentDeviceInfo;
        signature.IpAddress ??= _authContext.CurrentIpAddress;
        signature.SessionId ??= _authContext.CurrentSessionId;
        signature.Method ??= "password";
        signature.Status ??= "valid";
        signature.SignedAt ??= DateTime.UtcNow;
    }
}
