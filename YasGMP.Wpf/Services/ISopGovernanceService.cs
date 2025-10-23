using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Abstraction exposing SOP governance workflows to the WPF shell.
/// </summary>
public interface ISopGovernanceService
{
    /// <summary>Loads SOP documents visible to the current operator.</summary>
    Task<SopGovernanceLoadResult> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Creates a new SOP document.</summary>
    Task<SopGovernanceOperationResult> CreateAsync(
        SopDocument draft,
        IEnumerable<SopAttachmentUpload>? attachments = null,
        CancellationToken cancellationToken = default);

    /// <summary>Updates an existing SOP document.</summary>
    Task<SopGovernanceOperationResult> UpdateAsync(
        SopDocument document,
        IEnumerable<SopAttachmentUpload>? attachments = null,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes an existing SOP document.</summary>
    Task<SopGovernanceOperationResult> DeleteAsync(
        SopDocument document,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>Exports the provided SOP snapshot.</summary>
    Task<SopGovernanceExportResult> ExportAsync(
        IList<SopDocument> documents,
        string format,
        CancellationToken cancellationToken = default);
}

/// <summary>Represents the outcome of a load operation.</summary>
/// <param name="Success">True when the load succeeded.</param>
/// <param name="Message">Localized or user-facing status message.</param>
/// <param name="Documents">Snapshot of SOP documents (empty when the load fails).</param>
public sealed record SopGovernanceLoadResult(
    bool Success,
    string Message,
    IReadOnlyList<SopDocument> Documents);

/// <summary>Represents the outcome of a SOP persistence workflow.</summary>
/// <param name="Success">True when the workflow succeeded.</param>
/// <param name="Message">Localized or user-facing status message.</param>
/// <param name="DocumentId">Identifier of the affected document, when available.</param>
public sealed record SopGovernanceOperationResult(
    bool Success,
    string Message,
    int? DocumentId = null);

/// <summary>Represents the outcome of an export operation.</summary>
/// <param name="Success">True when the export completed successfully.</param>
/// <param name="Message">Localized or user-facing status message.</param>
/// <param name="FilePath">Export file path when emitted by the helper.</param>
public sealed record SopGovernanceExportResult(
    bool Success,
    string Message,
    string? FilePath);

/// <summary>Descriptor used to upload attachments alongside SOP workflows.</summary>
/// <param name="FileName">Attachment file name.</param>
/// <param name="ContentType">Detected content type.</param>
/// <param name="OpenStreamAsync">Factory that produces the attachment stream.</param>
/// <param name="DisplayName">Optional user-facing display name.</param>
/// <param name="Reason">Optional audit reason for the upload.</param>
public sealed record SopAttachmentUpload(
    string FileName,
    string? ContentType,
    Func<CancellationToken, Task<Stream>> OpenStreamAsync,
    string? DisplayName = null,
    string? Reason = null);
