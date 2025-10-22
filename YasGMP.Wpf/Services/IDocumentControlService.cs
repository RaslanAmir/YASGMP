using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Models.DTO;
using YasGMP.Services.Interfaces;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Abstraction that exposes document control lifecycle operations for the WPF shell.
/// </summary>
public interface IDocumentControlService
{
    Task<DocumentLifecycleResult> InitiateDocumentAsync(
        SopDocument draft,
        CancellationToken cancellationToken = default);

    Task<DocumentLifecycleResult> ReviseDocumentAsync(
        SopDocument existing,
        SopDocument revision,
        CancellationToken cancellationToken = default);

    Task<DocumentLifecycleResult> ApproveDocumentAsync(
        SopDocument document,
        CancellationToken cancellationToken = default);

    Task<DocumentLifecycleResult> PublishDocumentAsync(
        SopDocument document,
        CancellationToken cancellationToken = default);

    Task<DocumentLifecycleResult> ExpireDocumentAsync(
        SopDocument document,
        CancellationToken cancellationToken = default);

    Task<DocumentLifecycleResult> LinkChangeControlAsync(
        SopDocument document,
        ChangeControlSummaryDto changeControl,
        CancellationToken cancellationToken = default);

    Task<DocumentExportResult> ExportDocumentsAsync(
        IReadOnlyCollection<SopDocument> documents,
        string format,
        CancellationToken cancellationToken = default);

    Task<DocumentAttachmentUploadResult> UploadAttachmentsAsync(
        SopDocument document,
        IEnumerable<DocumentAttachmentUpload> attachments,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AttachmentLinkWithAttachment>> GetAttachmentManifestAsync(
        int documentId,
        CancellationToken cancellationToken = default);
}

/// <summary>Represents the outcome of a document lifecycle operation.</summary>
/// <param name="Success">True when the operation completed without errors.</param>
/// <param name="Message">User-facing status message.</param>
/// <param name="EntityId">Optional identifier of the affected document.</param>
public sealed record DocumentLifecycleResult(bool Success, string Message, int? EntityId = null);

/// <summary>Represents the outcome of a document export operation.</summary>
/// <param name="Success">True when the export completed successfully.</param>
/// <param name="Message">User-facing status message.</param>
/// <param name="FilePath">Path to the exported payload if available.</param>
public sealed record DocumentExportResult(bool Success, string Message, string? FilePath);

/// <summary>Represents the outcome of an attachment upload batch.</summary>
/// <param name="Success">True when the batch completed without critical errors.</param>
/// <param name="Message">User-facing status message.</param>
/// <param name="ProcessedCount">Number of processed files.</param>
/// <param name="DeduplicatedCount">Number of uploads skipped due to deduplication.</param>
/// <param name="Manifest">Current attachment manifest for the document.</param>
public sealed record DocumentAttachmentUploadResult(
    bool Success,
    string Message,
    int ProcessedCount,
    int DeduplicatedCount,
    IReadOnlyList<AttachmentLinkWithAttachment> Manifest);

/// <summary>Descriptor used when streaming attachment content through the document control service.</summary>
/// <param name="FileName">Attachment file name.</param>
/// <param name="ContentType">Detected content type.</param>
/// <param name="OpenStreamAsync">Factory that produces a readable stream.</param>
public sealed record DocumentAttachmentUpload(
    string FileName,
    string? ContentType,
    Func<CancellationToken, Task<Stream>> OpenStreamAsync)
{
    /// <summary>Create a descriptor from a local file path.</summary>
    public static DocumentAttachmentUpload FromFile(string path, string? contentType = null)
    {
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        return new DocumentAttachmentUpload(
            Path.GetFileName(path),
            contentType ?? "application/octet-stream",
            token => Task.FromResult<Stream>(File.OpenRead(path)));
    }
}
