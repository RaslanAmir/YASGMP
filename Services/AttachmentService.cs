using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
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
        private readonly IRBACService _rbac;
        private readonly AttachmentEncryptionOptions _encryptionOptions;
        private readonly AttachmentCryptoProvider _cryptoProvider;
        private readonly MalwareScanner _malwareScanner = new();

        private bool? _supportsEfSchema;
        private readonly SemaphoreSlim _schemaGate = new(1, 1);

        public AttachmentService(
            IDbContextFactory<YasGmpDbContext> contextFactory,
            DatabaseService legacyDatabase,
            IRBACService rbacService,
            AttachmentEncryptionOptions encryptionOptions)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            _legacyDb = legacyDatabase ?? throw new ArgumentNullException(nameof(legacyDatabase));
            _rbac = rbacService ?? throw new ArgumentNullException(nameof(rbacService));
            _encryptionOptions = encryptionOptions ?? throw new ArgumentNullException(nameof(encryptionOptions));
            _cryptoProvider = new AttachmentCryptoProvider(_encryptionOptions);
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

            await EnsureEntityPermissionAsync(request.UploadedById, request.EntityType, "upload", token).ConfigureAwait(false);

            if (content.CanSeek)
                content.Seek(0, SeekOrigin.Begin);

            string sanitizedName = Path.GetFileName(request.FileName);
            string? contentType = !string.IsNullOrWhiteSpace(request.ContentType)
                ? request.ContentType
                : Path.GetExtension(sanitizedName)?.TrimStart('.')?.ToLowerInvariant();

            var buffer = ArrayPool<byte>.Shared.Rent(_cryptoProvider.ChunkSize);
            var encryptionSession = _cryptoProvider.CreateEncryptionSession();
            var malwareContext = _malwareScanner.CreateContext();

            await using var context = await _contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);
            await using var transaction = await context.Database.BeginTransactionAsync(token).ConfigureAwait(false);

            var attachment = new Attachment
            {
                Name = request.DisplayName ?? sanitizedName,
                FileName = sanitizedName,
                FilePath = null,
                FileType = contentType,
                FileSize = 0,
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                FileHash = string.Empty,
                UploadedAt = DateTime.UtcNow,
                UploadedById = request.UploadedById,
                Status = _cryptoProvider.IsEncryptionEnabled ? $"encrypted:{_encryptionOptions.KeyId}" : "uploaded",
                Notes = AppendNote(request.Notes, request.Reason),
                IpAddress = request.SourceIp,
                DeviceInfo = request.SourceHost
            };

            long totalBytes = 0;
            string hashHex = string.Empty;

            try
            {
                await context.Attachments.AddAsync(attachment, token).ConfigureAwait(false);
                await context.SaveChangesAsync(token).ConfigureAwait(false);

                var connection = context.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync(token).ConfigureAwait(false);

                var dbTransaction = transaction.GetDbTransaction();
                using var command = connection.CreateCommand();
                command.Transaction = dbTransaction;
                var chunkParam = command.CreateParameter();
                chunkParam.ParameterName = "@chunk";
                if (chunkParam is MySqlParameter mysqlChunk)
                    mysqlChunk.MySqlDbType = MySqlDbType.LongBlob;
                var idParam = command.CreateParameter();
                idParam.ParameterName = "@id";
                idParam.Value = attachment.Id;
                command.Parameters.Add(chunkParam);
                command.Parameters.Add(idParam);

                using var sha = SHA256.Create();
                bool firstChunk = true;
                int chunkIndex = 0;
                int read;
                while ((read = await content.ReadAsync(buffer.AsMemory(0, buffer.Length), token).ConfigureAwait(false)) > 0)
                {
                    var slice = new ReadOnlyMemory<byte>(buffer, 0, read);
                    sha.TransformBlock(buffer, 0, read, null, 0);
                    totalBytes += read;
                    _malwareScanner.Update(malwareContext, slice.Span);

                    var encrypted = _cryptoProvider.EncryptChunk(encryptionSession, slice, chunkIndex++);
                    chunkParam.Value = encrypted.Payload;
                    command.CommandText = firstChunk
                        ? "UPDATE attachments SET file_content = @chunk WHERE id=@id"
                        : "UPDATE attachments SET file_content = IFNULL(CONCAT(file_content, @chunk), @chunk) WHERE id=@id";
                    await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
                    firstChunk = false;
                }

                sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                hashHex = Convert.ToHexString(sha.Hash ?? Array.Empty<byte>());

                var malware = await _malwareScanner.CompleteAsync(malwareContext, sanitizedName, hashHex, totalBytes, token).ConfigureAwait(false);

                attachment.FileHash = hashHex;
                attachment.FileSize = totalBytes;
                attachment.Status = malware.IsClean
                    ? (_cryptoProvider.IsEncryptionEnabled ? $"encrypted:{_encryptionOptions.KeyId}" : "uploaded")
                    : "quarantined";

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

                var encryptionNote = _cryptoProvider.CompleteEncryption(encryptionSession);
                await LogAuditAsync(context, attachment.Id, request.UploadedById, "UPLOAD", request.SourceIp, request.SourceHost, request.Reason, token).ConfigureAwait(false);
                await LogAuditAsync(context, attachment.Id, request.UploadedById, "MALWARE_SCAN", request.SourceIp, request.SourceHost, $"engine={malware.Engine}; result={(malware.IsClean ? "clean" : "blocked")}", token).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(encryptionNote))
                {
                    await LogAuditAsync(context, attachment.Id, request.UploadedById, "ENCRYPTION", request.SourceIp, request.SourceHost, encryptionNote, token).ConfigureAwait(false);
                }

                var duplicate = await FindDuplicateInternalAsync(context, hashHex, totalBytes, attachment.Id, token).ConfigureAwait(false);
                if (duplicate is not null)
                {
                    await LogAuditAsync(context, attachment.Id, request.UploadedById, "DEDUP_MATCH", request.SourceIp, request.SourceHost, $"existing={duplicate.Id}", token).ConfigureAwait(false);
                }

                await context.SaveChangesAsync(token).ConfigureAwait(false);
                await transaction.CommitAsync(token).ConfigureAwait(false);

                return new AttachmentUploadResult(attachment, link, retention);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
                encryptionSession.Dispose();
            }
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

        public async Task<Attachment?> FindByHashAndSizeAsync(string sha256, long fileSize, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(sha256))
                throw new ArgumentException("Hash must be provided", nameof(sha256));
            if (fileSize < 0)
                throw new ArgumentOutOfRangeException(nameof(fileSize));

            if (!await SupportsEfAttachmentsAsync(token).ConfigureAwait(false))
                return await FindByHashLegacyAsync(sha256, token).ConfigureAwait(false);

            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);
                return await context.Attachments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.FileHash == sha256 && a.FileSize == fileSize, token)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (TryGetMySqlException(ex, out var mysql) && mysql is not null && IsSchemaMissing(mysql))
            {
                _supportsEfSchema = false;
                return await FindByHashLegacyAsync(sha256, token).ConfigureAwait(false);
            }
        }

        public async Task<AttachmentStreamResult> StreamContentAsync(int attachmentId, Stream destination, AttachmentReadRequest? request = null, CancellationToken token = default)
        {
            if (destination is null) throw new ArgumentNullException(nameof(destination));
            request ??= new AttachmentReadRequest();

            if (!await SupportsEfAttachmentsAsync(token).ConfigureAwait(false))
                throw new InvalidOperationException("Attachment streaming requires the new attachment_links schema. Please migrate the database or disable streaming.");

            await using var context = await _contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);
            var attachment = await context.Attachments
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == attachmentId, token)
                .ConfigureAwait(false);

            if (attachment is null)
                throw new FileNotFoundException($"Attachment with id {attachmentId} not found.", attachmentId.ToString());

            await EnsureEntityPermissionAsync(request.RequestedById, attachment.EntityType ?? string.Empty, "read", token).ConfigureAwait(false);

            var connection = context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(token).ConfigureAwait(false);

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT file_content FROM attachments WHERE id=@id";
            var idParam = command.CreateParameter();
            idParam.ParameterName = "@id";
            idParam.Value = attachmentId;
            command.Parameters.Add(idParam);

            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, token).ConfigureAwait(false);
            if (!await reader.ReadAsync(token).ConfigureAwait(false))
                throw new FileNotFoundException($"Attachment content missing for id {attachmentId}.", attachmentId.ToString());

            await using var blobStream = reader.GetStream(0);
            bool isEncrypted = (attachment.Status ?? string.Empty).StartsWith("encrypted", StringComparison.OrdinalIgnoreCase);
            long bytesWritten = await _cryptoProvider.CopyToAsync(blobStream, destination, isEncrypted, request.RangeStart ?? 0, request.RangeLength, token).ConfigureAwait(false);

            await LogAuditAsync(context, attachment.Id, request.RequestedById, "READ", request.SourceIp, request.SourceHost, request.Reason, token).ConfigureAwait(false);
            await context.SaveChangesAsync(token).ConfigureAwait(false);

            long totalLength = attachment.FileSize ?? bytesWritten;
            bool partial = request.RangeStart.HasValue || request.RangeLength.HasValue;
            return new AttachmentStreamResult(attachment, bytesWritten, totalLength, partial, request);
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

        private async Task EnsureEntityPermissionAsync(int? userId, string? entityType, string action, CancellationToken token)
        {
            if (userId is null || userId.Value <= 0)
                return;

            string normalizedEntity = string.IsNullOrWhiteSpace(entityType)
                ? "attachment"
                : entityType.Trim().ToLowerInvariant();

            var candidates = new[]
            {
                $"attachment.{action}",
                $"attachment.{normalizedEntity}.{action}",
                $"{normalizedEntity}.attachment.{action}",
                $"{normalizedEntity}.{action}"
            };

            foreach (var candidate in candidates)
            {
                if (await _rbac.HasPermissionAsync(userId.Value, candidate).ConfigureAwait(false))
                    return;
            }

            throw new UnauthorizedAccessException($"User {userId.Value} is not permitted to {action} attachments for entity '{entityType}'.");
        }

        private static async Task LogAuditAsync(YasGmpDbContext context, int attachmentId, int? userId, string action, string? ip, string? device, string? note, CancellationToken token)
        {
            var log = new AttachmentAuditLog
            {
                AttachmentId = attachmentId,
                Action = action,
                UserId = userId ?? 0,
                Ip = ip ?? string.Empty,
                Device = device ?? string.Empty,
                Note = note ?? string.Empty,
                SignatureHash = string.Empty,
                ActionAt = DateTime.UtcNow
            };

            await context.AttachmentAuditLogs.AddAsync(log, token).ConfigureAwait(false);
        }

        private static string? AppendNote(string? existing, string? addition)
        {
            if (string.IsNullOrWhiteSpace(addition))
                return existing;

            if (string.IsNullOrWhiteSpace(existing))
                return addition;

            if (existing.Contains(addition, StringComparison.OrdinalIgnoreCase))
                return existing;

            return $"{existing} | {addition}";
        }

        private static async Task<Attachment?> FindDuplicateInternalAsync(YasGmpDbContext context, string hashHex, long expectedSize, int currentAttachmentId, CancellationToken token)
        {
            return await context.Attachments
                .AsNoTracking()
                .Where(a => a.FileHash == hashHex && a.FileSize == expectedSize && a.Id != currentAttachmentId)
                .OrderBy(a => a.Id)
                .FirstOrDefaultAsync(token)
                .ConfigureAwait(false);
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

        private sealed class AttachmentCryptoProvider
        {
            private readonly AttachmentEncryptionOptions _options;
            private readonly byte[]? _key;
            private readonly int _chunkSize;

            public AttachmentCryptoProvider(AttachmentEncryptionOptions options)
            {
                _options = options ?? throw new ArgumentNullException(nameof(options));
                _key = ParseKey(options.KeyMaterial);
                _chunkSize = options.ChunkSize > 0 ? options.ChunkSize : 128 * 1024;
            }

            public int ChunkSize => _chunkSize;
            public bool IsEncryptionEnabled => _key is not null;
            public string KeyId => string.IsNullOrWhiteSpace(_options.KeyId) ? "default" : _options.KeyId!;

            public EncryptionSession CreateEncryptionSession() => new EncryptionSession(_key);

            public EncryptedChunk EncryptChunk(EncryptionSession session, ReadOnlyMemory<byte> plaintext, int chunkIndex)
            {
                if (!IsEncryptionEnabled || session.Cipher is null)
                {
                    var payload = plaintext.ToArray();
                    session.RecordChunk(plaintext.Length);
                    return new EncryptedChunk(payload);
                }

                var payload = new byte[4 + 12 + plaintext.Length + 16];
                BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(0, 4), plaintext.Length);
                var nonceSpan = payload.AsSpan(4, 12);
                RandomNumberGenerator.Fill(nonceSpan);
                var cipherSpan = payload.AsSpan(4 + 12, plaintext.Length);
                var tagSpan = payload.AsSpan(4 + 12 + plaintext.Length, 16);

                session.Cipher.Encrypt(nonceSpan, plaintext.Span, cipherSpan, tagSpan);
                session.RecordChunk(plaintext.Length);

                return new EncryptedChunk(payload, Convert.ToHexString(nonceSpan), Convert.ToHexString(tagSpan));
            }

            public string? CompleteEncryption(EncryptionSession session)
            {
                try
                {
                    if (!IsEncryptionEnabled)
                        return null;

                    return $"key={KeyId}; chunks={session.ChunkCount}; bytes={session.TotalBytes}";
                }
                finally
                {
                    session.Dispose();
                }
            }

            public async Task<long> CopyToAsync(Stream source, Stream destination, bool isEncrypted, long rangeStart, long? rangeLength, CancellationToken token)
            {
                if (rangeStart < 0) rangeStart = 0;

                if (!isEncrypted)
                {
                    return await CopyPlainAsync(source, destination, rangeStart, rangeLength, token).ConfigureAwait(false);
                }

                if (_key is null)
                    throw new InvalidOperationException("Attachment is encrypted but no encryption key is configured.");

                return await CopyEncryptedAsync(source, destination, rangeStart, rangeLength, token).ConfigureAwait(false);
            }

            private async Task<long> CopyPlainAsync(Stream source, Stream destination, long rangeStart, long? rangeLength, CancellationToken token)
            {
                long position = 0;
                long written = 0;
                long remaining = rangeLength ?? long.MaxValue;

                var buffer = ArrayPool<byte>.Shared.Rent(_chunkSize);
                try
                {
                    int read;
                    while ((read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), token).ConfigureAwait(false)) > 0)
                    {
                        long chunkStart = position;
                        long chunkEnd = position + read;
                        position = chunkEnd;

                        if (chunkEnd <= rangeStart)
                            continue;

                        int offset = chunkStart < rangeStart ? (int)(rangeStart - chunkStart) : 0;
                        int available = read - offset;
                        if (available <= 0)
                            continue;

                        if (rangeLength.HasValue && available > remaining)
                            available = (int)remaining;

                        if (available <= 0)
                            continue;

                        await destination.WriteAsync(buffer.AsMemory(offset, available), token).ConfigureAwait(false);
                        written += available;

                        if (rangeLength.HasValue)
                        {
                            remaining -= available;
                            if (remaining <= 0)
                                break;
                        }
                    }

                    return written;
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }

            private async Task<long> CopyEncryptedAsync(Stream source, Stream destination, long rangeStart, long? rangeLength, CancellationToken token)
            {
                long position = 0;
                long written = 0;
                long remaining = rangeLength ?? long.MaxValue;

                var headerBuffer = new byte[4];
                var nonceBuffer = new byte[12];
                var tagBuffer = new byte[16];
                var cipherBuffer = ArrayPool<byte>.Shared.Rent(_chunkSize);
                var plainBuffer = ArrayPool<byte>.Shared.Rent(_chunkSize);

                try
                {
                    using var aes = new AesGcm(_key!);
                    while (true)
                    {
                        int headerRead = await ReadAtLeastAsync(source, headerBuffer, token).ConfigureAwait(false);
                        if (headerRead == 0)
                            break;
                        if (headerRead != headerBuffer.Length)
                            throw new InvalidDataException("Unexpected end of encrypted attachment stream.");

                        int plainLength = BinaryPrimitives.ReadInt32LittleEndian(headerBuffer);
                        if (plainLength < 0)
                            throw new InvalidDataException("Invalid encrypted attachment chunk length.");

                        await ReadExactlyAsync(source, nonceBuffer, token).ConfigureAwait(false);
                        await ReadExactlyAsync(source, tagBuffer, token).ConfigureAwait(false);

                        if (cipherBuffer.Length < plainLength)
                        {
                            ArrayPool<byte>.Shared.Return(cipherBuffer);
                            cipherBuffer = ArrayPool<byte>.Shared.Rent(plainLength);
                        }

                        await ReadExactlyAsync(source, cipherBuffer.AsMemory(0, plainLength), token).ConfigureAwait(false);

                        if (plainBuffer.Length < plainLength)
                        {
                            ArrayPool<byte>.Shared.Return(plainBuffer);
                            plainBuffer = ArrayPool<byte>.Shared.Rent(plainLength);
                        }

                        aes.Decrypt(nonceBuffer, cipherBuffer.AsSpan(0, plainLength), tagBuffer, plainBuffer.AsSpan(0, plainLength));

                        long chunkStart = position;
                        long chunkEnd = position + plainLength;
                        position = chunkEnd;

                        if (chunkEnd <= rangeStart)
                            continue;

                        int offset = chunkStart < rangeStart ? (int)(rangeStart - chunkStart) : 0;
                        int available = plainLength - offset;
                        if (available <= 0)
                            continue;

                        if (rangeLength.HasValue && available > remaining)
                            available = (int)remaining;

                        if (available <= 0)
                            continue;

                        await destination.WriteAsync(plainBuffer.AsMemory(offset, available), token).ConfigureAwait(false);
                        written += available;

                        if (rangeLength.HasValue)
                        {
                            remaining -= available;
                            if (remaining <= 0)
                                break;
                        }
                    }

                    return written;
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(cipherBuffer);
                    ArrayPool<byte>.Shared.Return(plainBuffer);
                }
            }

            private static async Task<int> ReadAtLeastAsync(Stream source, Memory<byte> buffer, CancellationToken token)
            {
                int total = 0;
                while (total < buffer.Length)
                {
                    int read = await source.ReadAsync(buffer.Slice(total), token).ConfigureAwait(false);
                    if (read == 0)
                        return total;
                    total += read;
                }

                return total;
            }

            private static Task ReadExactlyAsync(Stream source, Memory<byte> buffer, CancellationToken token)
                => ReadExactlyInternalAsync(source, buffer, token);

            private static async Task ReadExactlyInternalAsync(Stream source, Memory<byte> buffer, CancellationToken token)
            {
                int total = 0;
                while (total < buffer.Length)
                {
                    int read = await source.ReadAsync(buffer.Slice(total), token).ConfigureAwait(false);
                    if (read == 0)
                        throw new EndOfStreamException("Unexpected end of encrypted attachment stream.");
                    total += read;
                }
            }

            private static byte[]? ParseKey(string? material)
            {
                if (string.IsNullOrWhiteSpace(material))
                    return null;

                var trimmed = material.Trim();
                try
                {
                    return Convert.FromBase64String(trimmed);
                }
                catch (FormatException)
                {
                    if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                        trimmed = trimmed[2..];

                    if (trimmed.Length % 2 != 0)
                        throw new FormatException("Attachment encryption key must be valid base64 or hex.");

                    var bytes = new byte[trimmed.Length / 2];
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        var pair = trimmed.Substring(i * 2, 2);
                        bytes[i] = Convert.ToByte(pair, 16);
                    }

                    return bytes;
                }
            }

            internal sealed class EncryptionSession : IDisposable
            {
                private bool _disposed;

                public EncryptionSession(byte[]? key)
                {
                    Cipher = key is not null ? new AesGcm(key) : null;
                }

                public AesGcm? Cipher { get; }
                public int ChunkCount { get; private set; }
                public long TotalBytes { get; private set; }

                public void RecordChunk(int length)
                {
                    ChunkCount++;
                    TotalBytes += length;
                }

                public void Dispose()
                {
                    if (_disposed)
                        return;

                    _disposed = true;
                    Cipher?.Dispose();
                }
            }

            internal readonly struct EncryptedChunk
            {
                public EncryptedChunk(byte[] payload, string? nonceHex = null, string? tagHex = null)
                {
                    Payload = payload;
                    NonceHex = nonceHex;
                    TagHex = tagHex;
                }

                public byte[] Payload { get; }
                public string? NonceHex { get; }
                public string? TagHex { get; }
            }
        }

        private sealed class MalwareScanner
        {
            public MalwareScanContext CreateContext() => new MalwareScanContext();

            public void Update(MalwareScanContext context, ReadOnlySpan<byte> chunk)
            {
                context.TotalBytes += chunk.Length;

                if (context.Sampled < context.SampleBuffer.Length)
                {
                    int toCopy = Math.Min(chunk.Length, context.SampleBuffer.Length - context.Sampled);
                    chunk.Slice(0, toCopy).CopyTo(context.SampleBuffer.AsSpan(context.Sampled));
                    context.Sampled += toCopy;
                }
            }

            public Task<MalwareScanResult> CompleteAsync(MalwareScanContext context, string fileName, string hashHex, long size, CancellationToken token)
            {
                bool clean = true;
                string detail = "clean";

                if (hashHex.EndsWith("BAD", StringComparison.OrdinalIgnoreCase))
                {
                    clean = false;
                    detail = "hash-heuristic-match";
                }

                var result = new MalwareScanResult(clean, "stub-heuristic", detail);
                return Task.FromResult(result);
            }

            internal sealed class MalwareScanContext
            {
                public long TotalBytes { get; set; }
                public byte[] SampleBuffer { get; } = new byte[64];
                public int Sampled { get; set; }
            }

            internal readonly record struct MalwareScanResult(bool IsClean, string Engine, string Details);
        }

    }
}
