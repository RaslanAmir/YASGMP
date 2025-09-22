using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Data;
using MySqlConnector;
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
        private readonly DatabaseService _legacyDb;
        private readonly string _rootPath;

        private bool? _supportsEfSchema;
        private readonly SemaphoreSlim _schemaGate = new(1, 1);

        public AttachmentService(IDbContextFactory<YasGmpDbContext> contextFactory, DatabaseService legacyDatabase)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            _legacyDb = legacyDatabase ?? throw new ArgumentNullException(nameof(legacyDatabase));
            _rootPath = Path.Combine(FileSystem.AppDataDirectory, "attachments");
            Directory.CreateDirectory(_rootPath);
        }

        private async Task<bool> SupportsEfAttachmentsAsync(CancellationToken token)
        {
            if (_supportsEfSchema.HasValue)
                return _supportsEfSchema.Value;

            await _schemaGate.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (_supportsEfSchema.HasValue)
                    return _supportsEfSchema.Value;

                try
                {
                    await using var context = await _contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);
                    var connection = context.Database.GetDbConnection();
                    if (connection.State != ConnectionState.Open)
                        await connection.OpenAsync(token).ConfigureAwait(false);

                    await using var command = connection.CreateCommand();
                    command.CommandText = "SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'attachment_links' LIMIT 1";
                    var result = await command.ExecuteScalarAsync(token).ConfigureAwait(false);
                    _supportsEfSchema = result != null && result != DBNull.Value;
                }
                catch (Exception ex) when (TryGetMySqlException(ex, out var mysql))
                {
                    _supportsEfSchema = false;
                }
                catch
                {
                    _supportsEfSchema = false;
                }

                return _supportsEfSchema.GetValueOrDefault();
            }
            finally
            {
                _schemaGate.Release();
            }
        }

        public async Task<AttachmentUploadResult> UploadAsync(Stream content, AttachmentUploadRequest request, CancellationToken token = default)
        {
            if (content is null) throw new ArgumentNullException(nameof(content));
            if (request is null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.FileName))
                throw new ArgumentException("File name must be provided", nameof(request));
            if (string.IsNullOrWhiteSpace(request.EntityType))
                throw new ArgumentException("Entity type must be provided", nameof(request));

            if (!await SupportsEfAttachmentsAsync(token).ConfigureAwait(false))
                throw new InvalidOperationException("Attachment upload requires the new attachment_links schema. Please migrate the database or disable uploads.");

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

            if (!await SupportsEfAttachmentsAsync(token).ConfigureAwait(false))
                return await FindByHashLegacyAsync(sha256, token).ConfigureAwait(false);

            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);
                return await context.Attachments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.FileHash == sha256, token)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (TryGetMySqlException(ex, out var mysql) && mysql is not null && IsSchemaMissing(mysql))
            {
                _supportsEfSchema = false;
                return await FindByHashLegacyAsync(sha256, token).ConfigureAwait(false);
            }
        }

        public async Task<IReadOnlyList<AttachmentLinkWithAttachment>> GetLinksForEntityAsync(string entityType, int entityId, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(entityType))
                throw new ArgumentException("Entity type must be provided", nameof(entityType));

            if (!await SupportsEfAttachmentsAsync(token).ConfigureAwait(false))
                return await GetLegacyLinksAsync(entityType, entityId, token).ConfigureAwait(false);

            try
            {
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
            catch (Exception ex) when (TryGetMySqlException(ex, out var mysql) && mysql is not null && IsSchemaMissing(mysql))
            {
                _supportsEfSchema = false;
                return await GetLegacyLinksAsync(entityType, entityId, token).ConfigureAwait(false);
            }
        }

        public async Task RemoveLinkAsync(int linkId, CancellationToken token = default)
        {
            if (!await SupportsEfAttachmentsAsync(token).ConfigureAwait(false))
            {
                await RemoveLegacyLinkByIdAsync(linkId, token).ConfigureAwait(false);
                return;
            }

            try
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
            catch (Exception ex) when (TryGetMySqlException(ex, out var mysql) && mysql is not null && IsSchemaMissing(mysql))
            {
                _supportsEfSchema = false;
                await RemoveLegacyLinkByIdAsync(linkId, token).ConfigureAwait(false);
            }
        }

        public async Task RemoveLinkAsync(string entityType, int entityId, int attachmentId, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(entityType))
                throw new ArgumentException("Entity type must be provided", nameof(entityType));

            try
            {
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
            catch (Exception ex) when (TryGetMySqlException(ex, out var mysql) && mysql is not null && IsSchemaMissing(mysql))
            {
                await RemoveLegacyLinkAsync(entityType, entityId, attachmentId, token).ConfigureAwait(false);
            }
        }

        private async Task<IReadOnlyList<AttachmentLinkWithAttachment>> GetLegacyLinksAsync(string entityType, int entityId, CancellationToken token)
        {
            if (_legacyDb is null)
            {
                return Array.Empty<AttachmentLinkWithAttachment>();
            }

            const string sql = @"SELECT dl.id,
                                         dl.document_id,
                                         dl.entity_type,
                                         dl.entity_id,
                                         dl.created_at,
                                         dl.updated_at,
                                         d.id AS doc_id,
                                         d.file_name,
                                         d.storage_path,
                                         d.content_type,
                                         d.sha256,
                                         d.uploaded_by,
                                         d.uploaded_at
                                  FROM document_links dl
                                  JOIN documents d ON d.id = dl.document_id
                                  WHERE dl.entity_type = @et AND dl.entity_id = @eid
                                  ORDER BY dl.created_at DESC, dl.id DESC";

            var parameters = new[]
            {
                new MySqlParameter("@et", entityType),
                new MySqlParameter("@eid", entityId)
            };

            var table = await _legacyDb.ExecuteSelectAsync(sql, parameters, token).ConfigureAwait(false);
            var list = new List<AttachmentLinkWithAttachment>(table.Rows.Count);
            foreach (DataRow row in table.Rows)
            {
                var attachment = CreateLegacyAttachment(row, entityType, entityId);
                var link = CreateLegacyLink(row, entityType, entityId, attachment.Id);
                list.Add(new AttachmentLinkWithAttachment(link, attachment));
            }

            return list;
        }

        private async Task<Attachment?> FindByHashLegacyAsync(string sha256, CancellationToken token)
        {
            if (_legacyDb is null)
            {
                return null;
            }

            const string sql = @"SELECT d.id AS document_id,
                                         d.file_name,
                                         d.storage_path,
                                         d.content_type,
                                         d.sha256,
                                         d.uploaded_by,
                                         d.uploaded_at
                                  FROM documents d
                                  WHERE d.sha256 = @hash
                                  LIMIT 1";

            var table = await _legacyDb.ExecuteSelectAsync(sql, new[] { new MySqlParameter("@hash", sha256) }, token).ConfigureAwait(false);
            if (table.Rows.Count == 0)
            {
                return null;
            }

            return CreateLegacyAttachment(table.Rows[0]);
        }

        private async Task RemoveLegacyLinkByIdAsync(int linkId, CancellationToken token)
        {
            if (_legacyDb is null || linkId <= 0)
            {
                return;
            }

            const string sql = "DELETE FROM document_links WHERE id=@id";
            await _legacyDb.ExecuteNonQueryAsync(sql, new[] { new MySqlParameter("@id", linkId) }, token).ConfigureAwait(false);
        }

        private async Task RemoveLegacyLinkAsync(string entityType, int entityId, int attachmentId, CancellationToken token)
        {
            if (_legacyDb is null)
            {
                return;
            }

            const string sql = "DELETE FROM document_links WHERE entity_type=@et AND entity_id=@eid AND document_id=@doc";
            var parameters = new[]
            {
                new MySqlParameter("@et", entityType),
                new MySqlParameter("@eid", entityId),
                new MySqlParameter("@doc", attachmentId)
            };

            await _legacyDb.ExecuteNonQueryAsync(sql, parameters, token).ConfigureAwait(false);
        }

        private static Attachment CreateLegacyAttachment(DataRow row, string? entityType = null, int? entityId = null)
        {
            var fileName = GetString(row, "file_name");
            var filePath = GetString(row, "storage_path");

            if (string.IsNullOrEmpty(fileName) && !string.IsNullOrEmpty(filePath))
            {
                fileName = Path.GetFileName(filePath);
            }

            var attachmentId = GetInt(row, "document_id");
            if (attachmentId == 0)
            {
                attachmentId = GetInt(row, "doc_id");
            }

            return new Attachment
            {
                Id = attachmentId,
                Name = fileName,
                FileName = fileName,
                FilePath = filePath,
                FileType = GetString(row, "content_type"),
                FileHash = GetString(row, "sha256"),
                UploadedById = GetNullableInt(row, "uploaded_by"),
                UploadedAt = GetNullableDateTime(row, "uploaded_at") ?? DateTime.UtcNow,
                EntityType = entityType ?? GetString(row, "entity_type"),
                EntityId = entityId ?? GetNullableInt(row, "entity_id"),
                Status = "legacy",
                Notes = GetString(row, "note")
            };
        }

        private static AttachmentLink CreateLegacyLink(DataRow row, string entityType, int entityId, int attachmentId)
        {
            return new AttachmentLink
            {
                Id = GetInt(row, "id"),
                AttachmentId = attachmentId,
                EntityType = entityType,
                EntityId = entityId,
                LinkedAt = GetNullableDateTime(row, "created_at") ?? DateTime.UtcNow,
                LinkedById = GetNullableInt(row, "uploaded_by")
            };
        }

        private static string GetString(DataRow row, string column)
            => row.Table.Columns.Contains(column) && row[column] != DBNull.Value ? Convert.ToString(row[column]) ?? string.Empty : string.Empty;

        private static int GetInt(DataRow row, string column)
            => row.Table.Columns.Contains(column) && row[column] != DBNull.Value ? Convert.ToInt32(row[column]) : 0;

        private static int? GetNullableInt(DataRow row, string column)
            => row.Table.Columns.Contains(column) && row[column] != DBNull.Value ? Convert.ToInt32(row[column]) : (int?)null;

        private static DateTime? GetNullableDateTime(DataRow row, string column)
            => row.Table.Columns.Contains(column) && row[column] != DBNull.Value ? Convert.ToDateTime(row[column]) : (DateTime?)null;

        private static bool TryGetMySqlException(Exception ex, out MySqlException? mysql)
        {
            if (ex is MySqlException direct)
            {
                mysql = direct;
                return true;
            }

            if (ex.InnerException is not null)
            {
                return TryGetMySqlException(ex.InnerException, out mysql);
            }

            mysql = null;
            return false;
        }

        private static bool IsSchemaMissing(MySqlException ex)
            => ex.Number == (int)MySqlErrorCode.UnknownTable || ex.Number == (int)MySqlErrorCode.BadFieldError;

    }
}
