using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Abstraction that encapsulates the WPF shell orchestration for training record workflows.
/// </summary>
public interface ITrainingRecordService
{
    /// <summary>Loads all training records visible to the current operator.</summary>
    Task<TrainingRecordLoadResult> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Initiates a new training record.</summary>
    Task<TrainingRecordOperationResult> InitiateAsync(
        TrainingRecord draft,
        string? note = null,
        CancellationToken cancellationToken = default);

    /// <summary>Assigns the supplied training record to an operator.</summary>
    Task<TrainingRecordOperationResult> AssignAsync(
        TrainingRecord record,
        int? assigneeUserId = null,
        string? note = null,
        CancellationToken cancellationToken = default);

    /// <summary>Approves the supplied training record using an electronic signature.</summary>
    Task<TrainingRecordOperationResult> ApproveAsync(
        TrainingRecord record,
        CancellationToken cancellationToken = default);

    /// <summary>Marks the supplied training record as completed.</summary>
    Task<TrainingRecordOperationResult> CompleteAsync(
        TrainingRecord record,
        string? note = null,
        IEnumerable<TrainingRecordAttachmentUpload>? attachments = null,
        CancellationToken cancellationToken = default);

    /// <summary>Closes the supplied training record after the effectiveness check.</summary>
    Task<TrainingRecordOperationResult> CloseAsync(
        TrainingRecord record,
        string? note = null,
        IEnumerable<TrainingRecordAttachmentUpload>? attachments = null,
        CancellationToken cancellationToken = default);

    /// <summary>Exports the provided snapshot of training records.</summary>
    Task<TrainingRecordExportResult> ExportAsync(
        IList<TrainingRecord> records,
        string format,
        CancellationToken cancellationToken = default);
}

/// <summary>Represents the outcome of a load operation.</summary>
/// <param name="Success">True when the operation completed successfully.</param>
/// <param name="Message">Localized or user-facing status message.</param>
/// <param name="Records">Snapshot of training records (empty when the load failed).</param>
public sealed record TrainingRecordLoadResult(
    bool Success,
    string Message,
    IReadOnlyList<TrainingRecord> Records);

/// <summary>Represents the outcome of a workflow operation for a specific training record.</summary>
/// <param name="Success">True when the workflow action completed successfully.</param>
/// <param name="Message">Localized or user-facing status message.</param>
/// <param name="RecordId">Identifier of the affected record when available.</param>
public sealed record TrainingRecordOperationResult(
    bool Success,
    string Message,
    int? RecordId = null);

/// <summary>Represents the outcome of an export operation.</summary>
/// <param name="Success">True when the export completed successfully.</param>
/// <param name="Message">Localized or user-facing status message.</param>
/// <param name="FilePath">Destination file path when emitted by the export helper.</param>
public sealed record TrainingRecordExportResult(
    bool Success,
    string Message,
    string? FilePath);

/// <summary>Descriptor used when uploading attachments for a training record workflow step.</summary>
/// <param name="FileName">Attachment file name.</param>
/// <param name="ContentType">Detected content type.</param>
/// <param name="OpenStreamAsync">Factory that produces a readable stream.</param>
public sealed record TrainingRecordAttachmentUpload(
    string FileName,
    string? ContentType,
    Func<CancellationToken, Task<Stream>> OpenStreamAsync)
{
    /// <summary>Create a descriptor from a local file path.</summary>
    public static TrainingRecordAttachmentUpload FromFile(string path, string? contentType = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path must be provided", nameof(path));
        }

        return new TrainingRecordAttachmentUpload(
            Path.GetFileName(path),
            contentType ?? "application/octet-stream",
            token => Task.FromResult<Stream>(File.OpenRead(path)));
    }
}
