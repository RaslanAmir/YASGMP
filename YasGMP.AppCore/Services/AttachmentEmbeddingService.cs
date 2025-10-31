using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using YasGMP.AppCore.Services.Ai;
using YasGMP.Data;
using YasGMP.Models;
using YasGMP.Services.Interfaces;
using System.IO;
using System.Text;
using MySqlConnector;
using System.Data.Common;

namespace YasGMP.Services
{
    /// <summary>
    /// Computes and persists optional embeddings for attachments using IAiAssistantService.
    /// Embeddings are derived from metadata text (name, filename, notes, entity info) to avoid
    /// heavy content extraction; text files may be passed directly if provided.
    /// </summary>
    public sealed class AttachmentEmbeddingService
    {
        private readonly IAiAssistantService _ai;
        private readonly YasGmpDbContext _db;
        private readonly IAttachmentService? _attachments;
        private readonly ITextExtractor? _extractor;

        public AttachmentEmbeddingService(IAiAssistantService ai, YasGmpDbContext db, IAttachmentService? attachments = null, ITextExtractor? extractor = null)
        {
            _ai = ai ?? throw new ArgumentNullException(nameof(ai));
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _attachments = attachments;
            _extractor = extractor;
        }

        /// <summary>
        /// Creates or updates an embedding row for the provided attachment metadata text.
        /// </summary>
        public async Task<int> IndexAttachmentAsync(Attachment attachment, string? extraText = null, CancellationToken token = default)
        {
            if (attachment == null) throw new ArgumentNullException(nameof(attachment));
            string contentText = await TryExtractAttachmentTextAsync(attachment, token).ConfigureAwait(false)
                                  ?? string.Empty;
            string text = BuildTextPayload(attachment, string.IsNullOrWhiteSpace(contentText) ? extraText : contentText);
            var vector = await _ai.EmbedAsync(text, token).ConfigureAwait(false);
            if (vector == null || vector.Count == 0)
            {
                return 0;
            }

            var bytes = ToBytes(vector);
            var record = new AttachmentEmbedding
            {
                AttachmentId = attachment.Id,
                Model = "text-embedding-3-small",
                Dimension = vector.Count,
                Vector = bytes,
                SourceSha256 = attachment.Sha256 ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            // Upsert: keep only latest per (AttachmentId, Model)
            var existing = await _db.AttachmentEmbeddings
                .Where(e => e.AttachmentId == record.AttachmentId && e.Model == record.Model)
                .ToListAsync(token)
                .ConfigureAwait(false);

            if (existing.Count > 0)
            {
                _db.AttachmentEmbeddings.RemoveRange(existing);
            }

            _db.AttachmentEmbeddings.Add(record);
            try
            {
                await _db.SaveChangesAsync(token).ConfigureAwait(false);
                return record.Id;
            }
            catch (DbUpdateException ex) when (IsMissingTable(ex))
            {
                // Schema not provisioned yet (attachment_embeddings missing); degrade gracefully
                return 0;
            }
            catch (MySqlException ex) when (ex.Number == 1146)
            {
                // Table doesn't exist; tolerate and return 0 so callers can proceed
                return 0;
            }
        }

        /// <summary>Finds similar attachments using cosine similarity against stored vectors.</summary>
        public async Task<IReadOnlyList<AttachmentSimilarity>> FindSimilarAsync(int attachmentId, int topK = 5, CancellationToken token = default)
        {
            AttachmentEmbedding? target;
            List<AttachmentEmbedding> all;
            try
            {
                target = await _db.AttachmentEmbeddings.AsNoTracking()
                    .FirstOrDefaultAsync(e => e.AttachmentId == attachmentId, token).ConfigureAwait(false);
                if (target is null) return Array.Empty<AttachmentSimilarity>();

                all = await _db.AttachmentEmbeddings.AsNoTracking()
                    .Where(e => e.Model == target.Model && e.AttachmentId != attachmentId)
                    .ToListAsync(token).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number == 1146)
            {
                // Table doesn't exist; no similarity can be computed
                return Array.Empty<AttachmentSimilarity>();
            }
            catch (DbException) // e.g., SQLite "no such table" in demo/test
            {
                return Array.Empty<AttachmentSimilarity>();
            }

            var targetVec = FromBytes(target.Vector);
            var list = new List<AttachmentSimilarity>(all.Count);
            foreach (var e in all)
            {
                var vec = FromBytes(e.Vector);
                if (vec.Length != targetVec.Length) continue;
                var score = Cosine(targetVec, vec);
                list.Add(new AttachmentSimilarity(e.AttachmentId, score));
            }

            return list.OrderByDescending(s => s.Score).Take(topK).ToList();
        }

        private static string BuildTextPayload(Attachment a, string? extra)
        {
            var parts = new List<string?>
            {
                a.Name,
                a.FileName,
                a.Notes,
                a.EntityType,
                a.EntityId?.ToString(),
                extra
            };
            parts.RemoveAll(s => string.IsNullOrWhiteSpace(s));
            return string.Join("\n", parts!);
        }

        private async Task<string?> TryExtractAttachmentTextAsync(Attachment a, CancellationToken token)
        {
            if (_attachments == null)
            {
                return null;
            }

            try
            {
                using var ms = new MemoryStream();
                var result = await _attachments.StreamContentAsync(a.Id, ms, new AttachmentReadRequest
                {
                    Reason = "ai:embedding",
                    SourceHost = Environment.MachineName,
                    SourceIp = "ai"
                }, token).ConfigureAwait(false);

                var contentType = a.FileType?.ToLowerInvariant();
                var fileName = a.FileName ?? string.Empty;
                ms.Position = 0;

                // Plain text passthrough
                if ((contentType != null && (contentType.Contains("text") || contentType == "txt")) || fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    using var reader = new StreamReader(ms, Encoding.UTF8, true, 4096, leaveOpen: true);
                    return await reader.ReadToEndAsync().ConfigureAwait(false);
                }

                // PDF extraction via ITextExtractor
                if (_extractor != null &&
                    ((contentType != null && contentType.Contains("pdf")) || fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)))
                {
                    ms.Position = 0;
                    return await _extractor.ExtractTextAsync(ms, "application/pdf", fileName, token).ConfigureAwait(false);
                }
            }
            catch
            {
                // ignore extraction failures; fallback to metadata
            }

            return null;
        }

        private static byte[] ToBytes(IReadOnlyList<float> vector)
        {
            var bytes = new byte[vector.Count * 4];
            var span = bytes.AsSpan();
            for (int i = 0; i < vector.Count; i++)
            {
                BinaryPrimitives.WriteSingleLittleEndian(span.Slice(i * 4, 4), vector[i]);
            }
            return bytes;
        }

        private static float[] FromBytes(byte[] bytes)
        {
            var count = bytes.Length / 4;
            var vec = new float[count];
            var span = bytes.AsSpan();
            for (int i = 0; i < count; i++)
            {
                vec[i] = BinaryPrimitives.ReadSingleLittleEndian(span.Slice(i * 4, 4));
            }
            return vec;
        }

        private static double Cosine(float[] a, float[] b)
        {
            double dot = 0, na = 0, nb = 0;
            for (int i = 0; i < a.Length; i++)
            {
                var x = a[i];
                var y = b[i];
                dot += x * y;
                na += x * x;
                nb += y * y;
            }
            if (na == 0 || nb == 0) return 0;
            return dot / (Math.Sqrt(na) * Math.Sqrt(nb));
        }

        private static bool IsMissingTable(Exception ex)
        {
            for (var cur = ex; cur != null; cur = cur.InnerException!)
            {
                if (cur is MySqlException mex && mex.Number == 1146)
                    return true;
            }
            return false;
        }
    }

    public sealed record AttachmentSimilarity(int AttachmentId, double Score);
}
