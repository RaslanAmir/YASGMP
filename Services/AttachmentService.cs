using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Storage;
using YasGMP.Data;
using YasGMP.Models;
using YasGMP.Services.Interfaces;

namespace YasGMP.Services
{
    /// <summary>
    /// EF Core backed implementation for streaming attachment uploads. Files are
    /// persisted to the local AppData folder while metadata is stored in MySQL via
    /// <see cref="YasGmpDbContext"/> inside a single transaction.
    /// </summary>
    public class AttachmentService : IAttachmentService
    {
        private readonly IDbContextFactory<YasGmpDbContext> _contextFactory;
        private readonly string _rootPath;

        public AttachmentService(IDbContextFactory<YasGmpDbContext> contextFactory)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            _rootPath = Path.Combine(FileSystem.AppDataDirectory, "attachments");
            Directory.CreateDirectory(_rootPath);
        }

        public async Task<AttachmentUploadResult> UploadAsync(Stream content, AttachmentUploadRequest request, CancellationToken token = default)
        {
            if (content is null) throw new ArgumentNullException(nameof(content));
            if (request is null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.FileName))
                throw new ArgumentException("File name must be provided", nameof(request));
            if (string.IsNullOrWhiteSpace(request.EntityType))
                throw new ArgumentException("Entity type must be provided", nameof(request));

            if (content.CanSeek)
            {
                content.Seek(0, SeekOrigin.Begin);
            }

            string sanitizedName = Path.GetFileName(request.FileName);
            string uniqueName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}_{sanitizedName}";
            string destination = Path.Combine(_rootPath, uniqueName);

            long totalBytes = 0;
            string hashHex;
            var buffer = ArrayPool<byte>.Shared.Rent(128 * 1024);
            try
            {
                using var fileStream = File.Create(destination);
                using var sha = SHA256.Create();

                int read;
                while ((read = await content.ReadAsync(buffer.AsMemory(0, buffer.Length), token).ConfigureAwait(false)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, read), token).ConfigureAwait(false);
                    sha.TransformBlock(buffer, 0, read, null, 0);
                    totalBytes += read;
                }

                sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                hashHex = Convert.ToHexString(sha.Hash ?? Array.Empty<byte>());
                await fileStream.FlushAsync(token).ConfigureAwait(false);
            }
            catch
            {
                try { if (File.Exists(destination)) File.Delete(destination); } catch { }
                throw;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            await using var context = await _contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);
            await using var transaction = await context.Database.BeginTransactionAsync(token).ConfigureAwait(false);

            var attachment = new Attachment
            {
                Name = request.DisplayName ?? sanitizedName,
                FileName = sanitizedName,
                FilePath = destination,
                FileType = !string.IsNullOrWhiteSpace(request.ContentType)
                    ? request.ContentType
                    : Path.GetExtension(sanitizedName)?.TrimStart('.')?.ToLowerInvariant(),
                FileSize = totalBytes,
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                FileHash = hashHex,
                UploadedAt = DateTime.UtcNow,
                UploadedById = request.UploadedById,
                Status = "uploaded",
                Notes = request.Notes
            };

            await context.Attachments.AddAsync(attachment, token).ConfigureAwait(false);
            await context.SaveChangesAsync(token).ConfigureAwait(false);

            var link = new AttachmentLink
            {
                AttachmentId = attachment.Id,
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                LinkedAt = DateTime.UtcNow,
                LinkedById = request.UploadedById
            };
            await context.AttachmentLinks.AddAsync(link, token).ConfigureAwait(false);

            var retention = new RetentionPolicy
            {
                AttachmentId = attachment.Id,
                PolicyName = request.RetentionPolicyName ?? "default",
                RetainUntil = request.RetainUntil,
                CreatedAt = DateTime.UtcNow,
                CreatedById = request.UploadedById,
                Notes = request.Notes
            };
            await context.RetentionPolicies.AddAsync(retention, token).ConfigureAwait(false);

            await context.SaveChangesAsync(token).ConfigureAwait(false);
            await transaction.CommitAsync(token).ConfigureAwait(false);

            return new AttachmentUploadResult(attachment, link, retention);
        }

        public async Task<Attachment?> FindByHashAsync(string sha256, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(sha256))
                throw new ArgumentException("Hash must be provided", nameof(sha256));

            await using var context = await _contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);
            return await context.Attachments
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.FileHash == sha256, token)
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<AttachmentLinkWithAttachment>> GetLinksForEntityAsync(string entityType, int entityId, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(entityType))
                throw new ArgumentException("Entity type must be provided", nameof(entityType));

            await using var context = await _contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);
            var rows = await context.AttachmentLinks
                .Include(l => l.Attachment)
                .Where(l => l.EntityType == entityType && l.EntityId == entityId)
                .OrderByDescending(l => l.LinkedAt)
                .ToListAsync(token)
                .ConfigureAwait(false);

            var results = new List<AttachmentLinkWithAttachment>(rows.Count);
            foreach (var link in rows)
            {
                if (link.Attachment is null)
                {
                    continue;
                }

                results.Add(new AttachmentLinkWithAttachment(link, link.Attachment));
            }

            return results;
        }

        public async Task RemoveLinkAsync(int linkId, CancellationToken token = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);
            var link = await context.AttachmentLinks.FirstOrDefaultAsync(l => l.Id == linkId, token).ConfigureAwait(false);
            if (link == null)
            {
                return;
            }

            context.AttachmentLinks.Remove(link);
            await context.SaveChangesAsync(token).ConfigureAwait(false);
        }

        public async Task RemoveLinkAsync(string entityType, int entityId, int attachmentId, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(entityType))
                throw new ArgumentException("Entity type must be provided", nameof(entityType));

            await using var context = await _contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);
            var link = await context.AttachmentLinks
                .FirstOrDefaultAsync(l => l.EntityType == entityType && l.EntityId == entityId && l.AttachmentId == attachmentId, token)
                .ConfigureAwait(false);
            if (link == null)
            {
                return;
            }

            context.AttachmentLinks.Remove(link);
            await context.SaveChangesAsync(token).ConfigureAwait(false);
        }
    }
}
