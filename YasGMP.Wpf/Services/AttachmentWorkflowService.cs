using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Provides a WPF-friendly workflow facade for attachment operations that mirrors the MAUI behaviour.
/// </summary>
public interface IAttachmentWorkflowService
{
    /// <summary>Indicates whether envelope encryption is currently enabled.</summary>
    bool IsEncryptionEnabled { get; }

    /// <summary>Logical key identifier used when encryption is active.</summary>
    string EncryptionKeyId { get; }

    /// <summary>
    /// Uploads an attachment, applying deduplication, retention persistence and audit logging.
    /// Returns metadata about the processed attachment and deduplication outcome.
    /// </summary>
    Task<AttachmentWorkflowUploadResult> UploadAsync(Stream content, AttachmentUploadRequest request, CancellationToken token = default);

    /// <summary>Streams an attachment to the provided destination stream.</summary>
    Task<AttachmentStreamResult> DownloadAsync(int attachmentId, Stream destination, AttachmentReadRequest? request = null, CancellationToken token = default);

    /// <summary>Loads attachment links for the specified entity.</summary>
    Task<IReadOnlyList<AttachmentLinkWithAttachment>> GetLinksForEntityAsync(string entityType, int entityId, CancellationToken token = default);

    /// <summary>Returns attachment rows projected through the legacy helper for browse screens.</summary>
    Task<IReadOnlyList<Attachment>> GetAttachmentSummariesAsync(string? entityFilter, string? typeFilter, string? searchTerm, CancellationToken token = default);

    /// <summary>Removes a persisted link by its primary key.</summary>
    Task RemoveLinkAsync(int linkId, CancellationToken token = default);

    /// <summary>Removes a persisted link by tuple.</summary>
    Task RemoveLinkAsync(string entityType, int entityId, int attachmentId, CancellationToken token = default);

    /// <summary>Looks up an attachment by hash.</summary>
    Task<Attachment?> FindByHashAsync(string sha256, CancellationToken token = default);

    /// <summary>Looks up an attachment by hash and size.</summary>
    Task<Attachment?> FindByHashAndSizeAsync(string sha256, long fileSize, CancellationToken token = default);
}

/// <summary>
/// Concrete workflow that composes the EF-backed attachment service with legacy DatabaseService helpers.
/// </summary>
public sealed class AttachmentWorkflowService : IAttachmentWorkflowService
{
    private readonly IAttachmentService _attachmentService;
    private readonly DatabaseService _databaseService;
    private readonly AuditService _auditService;
    private readonly IAttachmentWorkflowAudit _auditWorkflow;
    private readonly AttachmentEncryptionOptions _encryptionOptions;

    public AttachmentWorkflowService(
        IAttachmentService attachmentService,
        DatabaseService databaseService,
        AttachmentEncryptionOptions encryptionOptions,
        AuditService auditService)
    {
        _attachmentService = attachmentService ?? throw new ArgumentNullException(nameof(attachmentService));
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        _encryptionOptions = encryptionOptions ?? throw new ArgumentNullException(nameof(encryptionOptions));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _auditWorkflow = _auditService as IAttachmentWorkflowAudit
            ?? new AuditServiceAttachmentWorkflowAdapter(_auditService);
    }

    public bool IsEncryptionEnabled => !string.IsNullOrWhiteSpace(_encryptionOptions.KeyMaterial);

    public string EncryptionKeyId => string.IsNullOrWhiteSpace(_encryptionOptions.KeyId)
        ? "default"
        : _encryptionOptions.KeyId!;

    public async Task<AttachmentWorkflowUploadResult> UploadAsync(Stream content, AttachmentUploadRequest request, CancellationToken token = default)
    {
        if (content is null) throw new ArgumentNullException(nameof(content));
        if (request is null) throw new ArgumentNullException(nameof(request));

        await using var prepared = await PrepareStreamAsync(content, token).ConfigureAwait(false);
        Attachment? existing = null;
        if (!string.IsNullOrWhiteSpace(prepared.Hash))
        {
            existing = await _attachmentService
                .FindByHashAndSizeAsync(prepared.Hash, prepared.Length, token)
                .ConfigureAwait(false);
        }

        var upload = await _attachmentService
            .UploadAsync(prepared.Stream, request, token)
            .ConfigureAwait(false);

        bool deduplicated = existing is not null && upload.Attachment.Id == existing.Id;

        var timestamp = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        var actor = request.UploadedById?.ToString(CultureInfo.InvariantCulture) ?? "unknown";
        var reason = string.IsNullOrWhiteSpace(request.Reason) ? "none" : request.Reason;
        var dedupState = deduplicated ? "deduplicated" : "new";
        var existingId = existing?.Id.ToString(CultureInfo.InvariantCulture) ?? "-";
        var description = $"actor={actor}; entity={request.EntityType}:{request.EntityId}; attachment={upload.Attachment.Id}; sha={(string.IsNullOrWhiteSpace(prepared.Hash) ? "n/a" : prepared.Hash)}; size={prepared.Length}; dedup={dedupState}; existing={existingId}; reason={reason}; ts={timestamp}";

        await _auditWorkflow
            .LogAttachmentUploadAsync(request.UploadedById, request.EntityType, request.EntityId, description, upload.Attachment.Id, token)
            .ConfigureAwait(false);

        return new AttachmentWorkflowUploadResult(upload, prepared.Hash, prepared.Length, deduplicated, existing);
    }

    public Task<AttachmentStreamResult> DownloadAsync(int attachmentId, Stream destination, AttachmentReadRequest? request = null, CancellationToken token = default)
    {
        if (destination is null) throw new ArgumentNullException(nameof(destination));
        return _attachmentService.StreamContentAsync(attachmentId, destination, request, token);
    }

    public Task<IReadOnlyList<AttachmentLinkWithAttachment>> GetLinksForEntityAsync(string entityType, int entityId, CancellationToken token = default)
        => _attachmentService.GetLinksForEntityAsync(entityType, entityId, token);

    public async Task<IReadOnlyList<Attachment>> GetAttachmentSummariesAsync(string? entityFilter, string? typeFilter, string? searchTerm, CancellationToken token = default)
    {
        var rows = await _databaseService
            .GetAttachmentsFilteredAsync(entityFilter, typeFilter, searchTerm, token)
            .ConfigureAwait(false);
        return rows;
    }

    public Task RemoveLinkAsync(int linkId, CancellationToken token = default)
        => _attachmentService.RemoveLinkAsync(linkId, token);

    public Task RemoveLinkAsync(string entityType, int entityId, int attachmentId, CancellationToken token = default)
        => _attachmentService.RemoveLinkAsync(entityType, entityId, attachmentId, token);

    public Task<Attachment?> FindByHashAsync(string sha256, CancellationToken token = default)
        => _attachmentService.FindByHashAsync(sha256, token);

    public Task<Attachment?> FindByHashAndSizeAsync(string sha256, long fileSize, CancellationToken token = default)
        => _attachmentService.FindByHashAndSizeAsync(sha256, fileSize, token);

    private static async Task<PreparedStream> PrepareStreamAsync(Stream content, CancellationToken token)
    {
        if (content.CanSeek)
        {
            content.Seek(0, SeekOrigin.Begin);
            using var sha = SHA256.Create();
            var buffer = ArrayPool<byte>.Shared.Rent(128 * 1024);
            try
            {
                long total = 0;
                int read;
                while ((read = await content.ReadAsync(buffer.AsMemory(0, buffer.Length), token).ConfigureAwait(false)) > 0)
                {
                    sha.TransformBlock(buffer, 0, read, null, 0);
                    total += read;
                }

                sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                content.Seek(0, SeekOrigin.Begin);
                var hash = sha.Hash is null ? string.Empty : Convert.ToHexString(sha.Hash);
                return new PreparedStream(content, hash, total, null);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        else
        {
            var memory = new MemoryStream();
            using var sha = SHA256.Create();
            var buffer = ArrayPool<byte>.Shared.Rent(128 * 1024);
            try
            {
                long total = 0;
                int read;
                while ((read = await content.ReadAsync(buffer.AsMemory(0, buffer.Length), token).ConfigureAwait(false)) > 0)
                {
                    sha.TransformBlock(buffer, 0, read, null, 0);
                    await memory.WriteAsync(buffer.AsMemory(0, read), token).ConfigureAwait(false);
                    total += read;
                }

                sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                memory.Seek(0, SeekOrigin.Begin);
                var hash = sha.Hash is null ? string.Empty : Convert.ToHexString(sha.Hash);
                return new PreparedStream(memory, hash, total, memory);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }

    private sealed class PreparedStream : IAsyncDisposable
    {
        public PreparedStream(Stream stream, string hash, long length, Stream? owned)
        {
            Stream = stream;
            Hash = hash;
            Length = length;
            _owned = owned;
        }

        public Stream Stream { get; }
        public string Hash { get; }
        public long Length { get; }
        private readonly Stream? _owned;

        public ValueTask DisposeAsync()
        {
            _owned?.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}

/// <summary>
/// Contract used by <see cref="AttachmentWorkflowService"/> to emit audit metadata after uploads.
/// </summary>
public interface IAttachmentWorkflowAudit
{
    Task LogAttachmentUploadAsync(int? actorUserId, string entityType, int entityId, string description, int attachmentId, CancellationToken token);
}

internal sealed class AuditServiceAttachmentWorkflowAdapter : IAttachmentWorkflowAudit
{
    private readonly AuditService _auditService;

    public AuditServiceAttachmentWorkflowAdapter(AuditService auditService)
    {
        _auditService = auditService;
    }

    public Task LogAttachmentUploadAsync(int? actorUserId, string entityType, int entityId, string description, int attachmentId, CancellationToken token)
        => _auditService.LogSystemEventForUserAsync(actorUserId, "ATTACHMENT_UPLOAD", description, entityType, attachmentId);
}

/// <summary>
/// Result returned by <see cref="IAttachmentWorkflowService.UploadAsync"/> exposing deduplication context.
/// </summary>
/// <param name="Upload">The underlying upload result from the AppCore service.</param>
/// <param name="Sha256">Hex encoded SHA-256 hash calculated for the payload.</param>
/// <param name="FileSize">Total bytes processed.</param>
/// <param name="Deduplicated">True when an existing attachment was re-used.</param>
/// <param name="Existing">The existing attachment matched by hash, if any.</param>
public sealed record AttachmentWorkflowUploadResult(
    AttachmentUploadResult Upload,
    string Sha256,
    long FileSize,
    bool Deduplicated,
    Attachment? Existing)
{
    /// <summary>Convenience accessor for the persisted attachment metadata.</summary>
    public Attachment Attachment => Upload.Attachment;

    /// <summary>Convenience accessor for the link created as part of the upload.</summary>
    public AttachmentLink Link => Upload.Link;

    /// <summary>Captured retention policy persisted for the attachment.</summary>
    public RetentionPolicy Retention => Upload.Retention;
}
