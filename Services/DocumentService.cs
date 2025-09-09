using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using MySqlConnector;
using YasGMP.Models;

namespace YasGMP.Services
{
    /// <summary>
    /// Simple service for persisting files to the local file system and
    /// registering them in the <c>documents</c> and <c>document_links</c> tables.
    /// </summary>
    public class DocumentService
    {
        private readonly DatabaseService _db;
        private readonly string _rootPath;

        public DocumentService(DatabaseService db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _rootPath = Path.Combine(FileSystem.AppDataDirectory, "docs");
            Directory.CreateDirectory(_rootPath);
        }

        /// <summary>
        /// Saves the provided stream as a document and links it to an entity.
        /// </summary>
        public async Task<int> SaveAsync(Stream content, string fileName, string contentType,
            string entityType, int entityId, int? uploadedBy = null, CancellationToken token = default)
        {
            if (content is null) throw new ArgumentNullException(nameof(content));
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("File name required", nameof(fileName));
            if (string.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("Entity type required", nameof(entityType));

            // Persist file to disk
            var destPath = Path.Combine(_rootPath, fileName);
            using (var fs = File.Create(destPath))
            {
                await content.CopyToAsync(fs, token).ConfigureAwait(false);
            }

            // Compute SHA256
            string sha256;
            using (var sha = SHA256.Create())
            using (var fs = File.OpenRead(destPath))
            {
                var hash = await sha.ComputeHashAsync(fs, token).ConfigureAwait(false);
                sha256 = Convert.ToHexString(hash);
            }

            // Insert document record
            const string sqlDoc = "INSERT INTO documents (file_name, content_type, storage_provider, storage_path, sha256, uploaded_by) " +
                                  "VALUES (@n,@t,'FileSystem',@p,@s,@u)";
            var parsDoc = new[]
            {
                new MySqlParameter("@n", fileName),
                new MySqlParameter("@t", (object?)contentType ?? DBNull.Value),
                new MySqlParameter("@p", destPath),
                new MySqlParameter("@s", sha256),
                new MySqlParameter("@u", uploadedBy ?? (object)DBNull.Value)
            };
            await _db.ExecuteNonQueryAsync(sqlDoc, parsDoc, token).ConfigureAwait(false);
            var idObj = await _db.ExecuteScalarAsync("SELECT LAST_INSERT_ID();", null, token).ConfigureAwait(false);
            int docId = Convert.ToInt32(idObj);

            // Link to entity
            const string sqlLink = "INSERT INTO document_links (document_id, entity_type, entity_id) VALUES (@d,@et,@eid)";
            var parsLink = new[]
            {
                new MySqlParameter("@d", docId),
                new MySqlParameter("@et", entityType),
                new MySqlParameter("@eid", entityId)
            };
            await _db.ExecuteNonQueryAsync(sqlLink, parsLink, token).ConfigureAwait(false);

            return docId;
        }
    }
}
