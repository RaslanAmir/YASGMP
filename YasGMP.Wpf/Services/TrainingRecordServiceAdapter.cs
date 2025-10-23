using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Adapter that composes DatabaseService helpers, attachment workflows, and electronic signatures
/// for training record operations executed from the WPF shell.
/// </summary>
public sealed class TrainingRecordServiceAdapter : ITrainingRecordService
{
    private const string EntityType = "user_training";

    private readonly DatabaseService _databaseService;
    private readonly IAuthContext _authContext;
    private readonly IAttachmentWorkflowService _attachmentWorkflow;
    private readonly IAttachmentService _attachmentService;
    private readonly IElectronicSignatureDialogService _signatureDialog;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrainingRecordServiceAdapter"/> class.
    /// </summary>
    public TrainingRecordServiceAdapter(
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
    public async Task<TrainingRecordLoadResult> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var records = await _databaseService
                .GetAllTrainingRecordsFullAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var record in records)
            {
                ApplyActorMetadata(record);
                if (record.Id > 0)
                {
                    var manifest = await _attachmentService
                        .GetLinksForEntityAsync(EntityType, record.Id, cancellationToken)
                        .ConfigureAwait(false);

                    if (manifest?.Count > 0)
                    {
                        record.Attachments = manifest.Select(m => m.Attachment).ToList();
                    }
                }
            }

            string message = string.Format(
                CultureInfo.CurrentCulture,
                "Loaded {0} training records.",
                records?.Count ?? 0);

            return new TrainingRecordLoadResult(true, message, records ?? Array.Empty<TrainingRecord>());
        }
        catch (Exception ex)
        {
            return new TrainingRecordLoadResult(
                false,
                $"Failed to load training records: {ex.Message}",
                Array.Empty<TrainingRecord>());
        }
    }

    /// <inheritdoc />
    public async Task<TrainingRecordOperationResult> InitiateAsync(
        TrainingRecord draft,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        if (draft is null)
        {
            throw new ArgumentNullException(nameof(draft));
        }

        try
        {
            ApplyActorMetadata(draft);
            draft.PlannedBy = string.IsNullOrWhiteSpace(draft.PlannedBy)
                ? _authContext.CurrentUser?.UserName ?? string.Empty
                : draft.PlannedBy;
            draft.PlannedAt = draft.PlannedAt == default ? DateTime.UtcNow : draft.PlannedAt;

            await _databaseService
                .InitiateTrainingRecordAsync(draft, cancellationToken)
                .ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(note))
            {
                await _databaseService
                    .LogTrainingRecordAuditAsync(
                        draft,
                        "INITIATE",
                        _authContext.CurrentIpAddress,
                        _authContext.CurrentDeviceInfo,
                        _authContext.CurrentSessionId,
                        note,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            string message = string.Format(
                CultureInfo.CurrentCulture,
                "Training record '{0}' initiated.",
                string.IsNullOrWhiteSpace(draft.Title) ? draft.Name : draft.Title);

            return new TrainingRecordOperationResult(true, message);
        }
        catch (Exception ex)
        {
            return new TrainingRecordOperationResult(false, $"Initiation failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<TrainingRecordOperationResult> AssignAsync(
        TrainingRecord record,
        int? assigneeUserId = null,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        if (record is null)
        {
            throw new ArgumentNullException(nameof(record));
        }

        if (record.Id <= 0)
        {
            return new TrainingRecordOperationResult(false, "Select a training record before assigning.");
        }

        try
        {
            ApplyActorMetadata(record);
            var assignedTo = assigneeUserId ?? record.TraineeId ?? _authContext.CurrentUser?.Id ?? 0;
            if (assignedTo <= 0)
            {
                return new TrainingRecordOperationResult(false, "A valid assignee is required.", record.Id);
            }

            await _databaseService
                .AssignTrainingRecordAsync(
                    record.Id,
                    assignedTo,
                    _authContext.CurrentIpAddress,
                    _authContext.CurrentDeviceInfo,
                    _authContext.CurrentSessionId,
                    note,
                    cancellationToken)
                .ConfigureAwait(false);

            await RefreshAttachmentsAsync(record, cancellationToken).ConfigureAwait(false);

            string message = string.Format(
                CultureInfo.CurrentCulture,
                "Training record {0} assigned to user {1}.",
                record.Id,
                assignedTo);

            return new TrainingRecordOperationResult(true, message, record.Id);
        }
        catch (Exception ex)
        {
            return new TrainingRecordOperationResult(false, $"Assignment failed: {ex.Message}", record.Id);
        }
    }

    /// <inheritdoc />
    public async Task<TrainingRecordOperationResult> ApproveAsync(
        TrainingRecord record,
        CancellationToken cancellationToken = default)
    {
        if (record is null)
        {
            throw new ArgumentNullException(nameof(record));
        }

        if (record.Id <= 0)
        {
            return new TrainingRecordOperationResult(false, "Select a training record before approving.");
        }

        try
        {
            ApplyActorMetadata(record);
            var signatureResult = await _signatureDialog
                .CaptureSignatureAsync(new ElectronicSignatureContext(EntityType, record.Id), cancellationToken)
                .ConfigureAwait(false);

            if (signatureResult?.Signature is null)
            {
                return new TrainingRecordOperationResult(false, "Electronic signature cancelled.", record.Id);
            }

            PrepareSignature(signatureResult.Signature, record.Id);
            var persistedId = await _databaseService
                .InsertDigitalSignatureAsync(signatureResult.Signature, cancellationToken)
                .ConfigureAwait(false);
            signatureResult.Signature.Id = persistedId;

            var actorId = _authContext.CurrentUser?.Id ?? 0;

            await _databaseService
                .ApproveTrainingRecordAsync(
                    record.Id,
                    actorId,
                    _authContext.CurrentIpAddress,
                    _authContext.CurrentDeviceInfo,
                    _authContext.CurrentSessionId,
                    cancellationToken)
                .ConfigureAwait(false);

            await _signatureDialog
                .LogPersistedSignatureAsync(signatureResult, cancellationToken)
                .ConfigureAwait(false);

            await RefreshAttachmentsAsync(record, cancellationToken).ConfigureAwait(false);

            string message = string.Format(
                CultureInfo.CurrentCulture,
                "Training record {0} approved.",
                record.Id);

            return new TrainingRecordOperationResult(true, message, record.Id);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TrainingRecordOperationResult(false, $"Approval failed: {ex.Message}", record.Id);
        }
    }

    /// <inheritdoc />
    public async Task<TrainingRecordOperationResult> CompleteAsync(
        TrainingRecord record,
        string? note = null,
        IEnumerable<TrainingRecordAttachmentUpload>? attachments = null,
        CancellationToken cancellationToken = default)
    {
        if (record is null)
        {
            throw new ArgumentNullException(nameof(record));
        }

        if (record.Id <= 0)
        {
            return new TrainingRecordOperationResult(false, "Select a training record before completing.");
        }

        try
        {
            ApplyActorMetadata(record);
            var signatureResult = await _signatureDialog
                .CaptureSignatureAsync(new ElectronicSignatureContext(EntityType, record.Id), cancellationToken)
                .ConfigureAwait(false);

            if (signatureResult?.Signature is null)
            {
                return new TrainingRecordOperationResult(false, "Electronic signature cancelled.", record.Id);
            }

            PrepareSignature(signatureResult.Signature, record.Id);
            var persistedId = await _databaseService
                .InsertDigitalSignatureAsync(signatureResult.Signature, cancellationToken)
                .ConfigureAwait(false);
            signatureResult.Signature.Id = persistedId;

            var actorId = _authContext.CurrentUser?.Id ?? 0;

            await _databaseService
                .CompleteTrainingRecordAsync(
                    record.Id,
                    actorId,
                    _authContext.CurrentIpAddress,
                    _authContext.CurrentDeviceInfo,
                    _authContext.CurrentSessionId,
                    note,
                    cancellationToken)
                .ConfigureAwait(false);

            await _signatureDialog
                .LogPersistedSignatureAsync(signatureResult, cancellationToken)
                .ConfigureAwait(false);

            await UploadAttachmentsAsync(record.Id, attachments, cancellationToken).ConfigureAwait(false);
            await RefreshAttachmentsAsync(record, cancellationToken).ConfigureAwait(false);

            string message = string.Format(
                CultureInfo.CurrentCulture,
                "Training record {0} completed.",
                record.Id);

            return new TrainingRecordOperationResult(true, message, record.Id);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TrainingRecordOperationResult(false, $"Completion failed: {ex.Message}", record.Id);
        }
    }

    /// <inheritdoc />
    public async Task<TrainingRecordOperationResult> CloseAsync(
        TrainingRecord record,
        string? note = null,
        IEnumerable<TrainingRecordAttachmentUpload>? attachments = null,
        CancellationToken cancellationToken = default)
    {
        if (record is null)
        {
            throw new ArgumentNullException(nameof(record));
        }

        if (record.Id <= 0)
        {
            return new TrainingRecordOperationResult(false, "Select a training record before closing.");
        }

        try
        {
            ApplyActorMetadata(record);
            var signatureResult = await _signatureDialog
                .CaptureSignatureAsync(new ElectronicSignatureContext(EntityType, record.Id), cancellationToken)
                .ConfigureAwait(false);

            if (signatureResult?.Signature is null)
            {
                return new TrainingRecordOperationResult(false, "Electronic signature cancelled.", record.Id);
            }

            PrepareSignature(signatureResult.Signature, record.Id);
            var persistedId = await _databaseService
                .InsertDigitalSignatureAsync(signatureResult.Signature, cancellationToken)
                .ConfigureAwait(false);
            signatureResult.Signature.Id = persistedId;

            var actorId = _authContext.CurrentUser?.Id ?? 0;

            await _databaseService
                .CloseTrainingRecordAsync(
                    record.Id,
                    actorId,
                    _authContext.CurrentIpAddress,
                    _authContext.CurrentDeviceInfo,
                    _authContext.CurrentSessionId,
                    note,
                    cancellationToken)
                .ConfigureAwait(false);

            await _signatureDialog
                .LogPersistedSignatureAsync(signatureResult, cancellationToken)
                .ConfigureAwait(false);

            await UploadAttachmentsAsync(record.Id, attachments, cancellationToken).ConfigureAwait(false);
            await RefreshAttachmentsAsync(record, cancellationToken).ConfigureAwait(false);

            string message = string.Format(
                CultureInfo.CurrentCulture,
                "Training record {0} closed.",
                record.Id);

            return new TrainingRecordOperationResult(true, message, record.Id);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TrainingRecordOperationResult(false, $"Closure failed: {ex.Message}", record.Id);
        }
    }

    /// <inheritdoc />
    public async Task<TrainingRecordExportResult> ExportAsync(
        IList<TrainingRecord> records,
        string format,
        CancellationToken cancellationToken = default)
    {
        if (records is null)
        {
            throw new ArgumentNullException(nameof(records));
        }

        try
        {
            var fmt = string.IsNullOrWhiteSpace(format) ? "csv" : format.Trim();

            await _databaseService
                .ExportTrainingRecordsAsync(
                    records,
                    fmt,
                    _authContext.CurrentIpAddress,
                    _authContext.CurrentDeviceInfo,
                    _authContext.CurrentSessionId,
                    cancellationToken)
                .ConfigureAwait(false);

            string message = string.Format(
                CultureInfo.CurrentCulture,
                "Training records exported ({0}).",
                fmt.ToUpperInvariant());

            return new TrainingRecordExportResult(true, message, null);
        }
        catch (Exception ex)
        {
            return new TrainingRecordExportResult(false, $"Export failed: {ex.Message}", null);
        }
    }

    private void ApplyActorMetadata(TrainingRecord record)
    {
        record.DeviceInfo = _authContext.CurrentDeviceInfo;
        record.SessionId = _authContext.CurrentSessionId;
        record.IpAddress = _authContext.CurrentIpAddress;
    }

    private void PrepareSignature(YasGMP.Models.DigitalSignature signature, int recordId)
    {
        signature.TableName = EntityType;
        signature.RecordId = recordId;
        signature.UserId = signature.UserId == 0 ? _authContext.CurrentUser?.Id ?? 0 : signature.UserId;
        signature.DeviceInfo ??= _authContext.CurrentDeviceInfo;
        signature.IpAddress ??= _authContext.CurrentIpAddress;
        signature.SessionId ??= _authContext.CurrentSessionId;
        signature.Method ??= "password";
        signature.Status ??= "valid";
        signature.SignedAt ??= DateTime.UtcNow;
    }

    private async Task RefreshAttachmentsAsync(TrainingRecord record, CancellationToken cancellationToken)
    {
        if (record.Id <= 0)
        {
            record.Attachments?.Clear();
            return;
        }

        var manifest = await _attachmentService
            .GetLinksForEntityAsync(EntityType, record.Id, cancellationToken)
            .ConfigureAwait(false);

        record.Attachments = manifest.Select(m => m.Attachment).ToList();
    }

    private async Task UploadAttachmentsAsync(
        int recordId,
        IEnumerable<TrainingRecordAttachmentUpload>? attachments,
        CancellationToken cancellationToken)
    {
        if (attachments is null)
        {
            return;
        }

        var actorId = _authContext.CurrentUser?.Id;

        foreach (var descriptor in attachments)
        {
            if (descriptor is null)
            {
                continue;
            }

            await using var stream = await descriptor
                .OpenStreamAsync(cancellationToken)
                .ConfigureAwait(false);

            var request = new AttachmentUploadRequest
            {
                FileName = descriptor.FileName,
                ContentType = descriptor.ContentType,
                EntityType = EntityType,
                EntityId = recordId,
                UploadedById = actorId,
                SourceHost = _authContext.CurrentDeviceInfo,
                SourceIp = _authContext.CurrentIpAddress,
                Notes = $"training_record:{recordId}",
                Reason = $"training_record:{recordId}"
            };

            await _attachmentWorkflow
                .UploadAsync(stream, request, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
