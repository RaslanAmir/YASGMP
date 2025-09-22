using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Services.Interfaces
{
    /// <summary>
    /// Contract for streaming attachment uploads, metadata retrieval and integrity
    /// lookups based on the stored SHA-256 hash.
    /// </summary>
    public interface IAttachmentService
    {
        /// <summary>
        /// Streams the provided content to durable storage, captures metadata and
        /// creates the attachment/link/retention entities within a single database
        /// transaction. Returns the persisted entities for further processing.
        /// </summary>
        Task<AttachmentUploadResult> UploadAsync(Stream content, AttachmentUploadRequest request, CancellationToken token = default);

        /// <summary>
        /// Finds an attachment by its SHA-256 hash (hex encoded). Returns null if
        /// no attachment exists with the provided hash value.
        /// </summary>
        Task<Attachment?> FindByHashAsync(string sha256, CancellationToken token = default);

        /// <summary>
        /// Looks up an attachment candidate for deduplication based on the
        /// provided SHA-256 hash and byte length.
        /// </summary>
        Task<Attachment?> FindByHashAndSizeAsync(string sha256, long fileSize, CancellationToken token = default);

        /// <summary>
        /// Streams an attachment's content into the supplied destination stream.
        /// Supports optional byte range requests and emits metadata about the
        /// transfer, including whether a partial response was produced.
        /// </summary>
        Task<AttachmentStreamResult> StreamContentAsync(int attachmentId, Stream destination, AttachmentReadRequest? request = null, CancellationToken token = default);

        /// <summary>
        /// Retrieves attachments linked to the specified entity type/identifier.
        /// </summary>
        Task<IReadOnlyList<AttachmentLinkWithAttachment>> GetLinksForEntityAsync(string entityType, int entityId, CancellationToken token = default);

        /// <summary>Removes a specific attachment link by its primary key.</summary>
        Task RemoveLinkAsync(int linkId, CancellationToken token = default);

        /// <summary>Removes a link identified by the entity tuple and attachment id.</summary>
        Task RemoveLinkAsync(string entityType, int entityId, int attachmentId, CancellationToken token = default);
    }

    /// <summary>
    /// Input options for uploading and linking an attachment.
    /// </summary>
    public class AttachmentUploadRequest
    {
        public string FileName { get; set; } = string.Empty;
        public string? ContentType { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public int? UploadedById { get; set; }
        public string? DisplayName { get; set; }
        public DateTime? RetainUntil { get; set; }
        public string? RetentionPolicyName { get; set; }
        public string? Notes { get; set; }

        /// <summary>
        /// Human readable rationale for the upload (audit/a11y context).
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// Source IP address (string representation) captured for audit trails.
        /// </summary>
        public string? SourceIp { get; set; }

        /// <summary>
        /// Source host/device identifier if available.
        /// </summary>
        public string? SourceHost { get; set; }
    }

    /// <summary>
    /// Result payload returned when an attachment upload completes successfully.
    /// </summary>
    /// <param name="Attachment">Persisted attachment metadata.</param>
    /// <param name="Link">Link entity pointing to the owning domain object.</param>
    /// <param name="Retention">Retention metadata captured during the upload.</param>
    public record AttachmentUploadResult(Attachment Attachment, AttachmentLink Link, RetentionPolicy Retention);

    /// <summary>
    /// Lightweight projection combining an attachment link with its attachment entity.
    /// </summary>
    /// <param name="Link">The persisted link.</param>
    /// <param name="Attachment">The linked attachment metadata.</param>
    public record AttachmentLinkWithAttachment(AttachmentLink Link, Attachment Attachment);

    /// <summary>
    /// Options supplied when streaming attachment content.
    /// </summary>
    public class AttachmentReadRequest
    {
        /// <summary>Optional requesting user id for RBAC enforcement.</summary>
        public int? RequestedById { get; set; }

        /// <summary>Optional justification that will be persisted in the audit log.</summary>
        public string? Reason { get; set; }

        /// <summary>Optional caller IP address captured for the audit log.</summary>
        public string? SourceIp { get; set; }

        /// <summary>Optional host/device info captured for the audit log.</summary>
        public string? SourceHost { get; set; }

        /// <summary>Zero-based byte offset to start streaming from.</summary>
        public long? RangeStart { get; set; }

        /// <summary>Optional number of bytes to stream.</summary>
        public long? RangeLength { get; set; }
    }

    /// <summary>
    /// Metadata returned after streaming an attachment to a destination stream.
    /// </summary>
    public record AttachmentStreamResult(
        Attachment Attachment,
        long BytesWritten,
        long TotalLength,
        bool IsPartial,
        AttachmentReadRequest? Request);
}
