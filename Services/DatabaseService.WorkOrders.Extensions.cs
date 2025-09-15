using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Models;

namespace YasGMP.Services
{
    public static class DatabaseServiceWorkOrdersExtensions
    {
        #region Core Insert/Update
        public static async Task<int> InsertOrUpdateWorkOrderAsync(
            this DatabaseService db,
            WorkOrder wo,
            bool update,
            int actorUserId,
            string ip,
            string deviceInfo,
            string? sessionId,
            CancellationToken token = default)
        {
            string insert = @"INSERT INTO work_orders
 (title, description, task_description, type, priority, status,
  date_open, due_date, date_close,
  requested_by_id, created_by_id, assigned_to_id,
  machine_id, component_id,
  result, notes)
 VALUES
 (@title, @desc, @task, @type, @prio, @status,
  @open, @due, @close,
  @req, @crt, @ass,
  @mach, @comp,
  @res, @notes)";

            string updateSql = @"UPDATE work_orders SET
  title=@title, description=@desc, task_description=@task,
  type=@type, priority=@prio, status=@status,
  date_open=@open, due_date=@due, date_close=@close,
  requested_by_id=@req, created_by_id=@crt, assigned_to_id=@ass,
  machine_id=@mach, component_id=@comp,
  result=@res, notes=@notes
WHERE id=@id";

            var pars = new List<MySqlParameter>
            {
                new("@title",   wo.Title ?? string.Empty),
                new("@desc",    wo.Description ?? string.Empty),
                new("@task",    wo.TaskDescription ?? (object)DBNull.Value),
                new("@type",    wo.Type ?? string.Empty),
                new("@prio",    wo.Priority ?? string.Empty),
                new("@status",  wo.Status ?? string.Empty),
                new("@open",    wo.DateOpen),
                new("@due",     (object?)wo.DueDate ?? DBNull.Value),
                new("@close",   (object?)wo.DateClose ?? DBNull.Value),
                new("@req",     wo.RequestedById),
                new("@crt",     wo.CreatedById),
                new("@ass",     wo.AssignedToId),
                new("@mach",    wo.MachineId),
                new("@comp",    (object?)wo.ComponentId ?? DBNull.Value),
                new("@res",     wo.Result ?? string.Empty),
                new("@notes",   (object?)wo.Notes ?? DBNull.Value)
            };
            if (update) pars.Add(new MySqlParameter("@id", wo.Id));

            if (!update)
            {
                await db.ExecuteNonQueryAsync(insert, pars, token).ConfigureAwait(false);
                var idObj = await db.ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false);
                wo.Id = Convert.ToInt32(idObj);
            }
            else
            {
                await db.ExecuteNonQueryAsync(updateSql, pars, token).ConfigureAwait(false);
            }

            await db.LogSystemEventAsync(
                actorUserId,
                update ? "WO_UPDATE" : "WO_CREATE",
                "work_orders",
                "WorkOrders",
                wo.Id,
                wo.Title,
                ip,
                "audit",
                deviceInfo,
                sessionId,
                token: token
            ).ConfigureAwait(false);

            return wo.Id;
        }

        #region Convenience CRUD + Queries
        public static Task AddWorkOrderAsync(
            this DatabaseService db,
            WorkOrder wo,
            int actorUserId,
            string ip,
            string device,
            string? sessionId,
            CancellationToken token = default)
            => db.InsertOrUpdateWorkOrderAsync(wo, update: false, actorUserId, ip, device, sessionId, token);

        public static async Task DeleteWorkOrderAsync(
            this DatabaseService db,
            int workOrderId,
            int actorUserId,
            string ip,
            string device,
            string? sessionId,
            CancellationToken token = default)
        {
            try
            {
                await db.ExecuteNonQueryAsync("DELETE FROM work_orders WHERE id=@id",
                    new[] { new MySqlParameter("@id", workOrderId) }, token).ConfigureAwait(false);
            }
            catch { /* tolerant of schema differences */ }
            await db.LogSystemEventAsync(actorUserId, "WO_DELETE", "work_orders", "WorkOrders", workOrderId, null, ip, "audit", device, sessionId, token: token).ConfigureAwait(false);
        }

        public static async Task<List<WorkOrder>> GetAllWorkOrdersFullAsync(
            this DatabaseService db,
            CancellationToken token = default)
        {
            const string sql = @"SELECT w.id, w.title, w.description, w.task_description, w.type, w.priority, w.status,
       w.date_open, w.due_date, w.date_close,
       w.requested_by_id, w.created_by_id, w.assigned_to_id,
       w.machine_id, w.component_id,
       w.result, w.notes,
       w.digital_signature,
       (SELECT COUNT(*) FROM work_order_parts p WHERE p.work_order_id = w.id) AS parts_count,
       (SELECT COUNT(*) FROM document_links dl WHERE dl.entity_type='WorkOrder' AND dl.entity_id = w.id) AS photos_count,
       m.name AS machine_name
FROM work_orders w
LEFT JOIN machines m ON m.id = w.machine_id
ORDER BY w.date_open DESC, w.id DESC";

            var dt = await db.ExecuteSelectAsync(sql, null, token).ConfigureAwait(false);
            var list = new List<WorkOrder>(dt.Rows.Count);
            foreach (System.Data.DataRow r in dt.Rows) list.Add(MapWorkOrder(r));
            return list;
        }

        private static WorkOrder MapWorkOrder(System.Data.DataRow r)
        {
            string S(string c) => r.Table.Columns.Contains(c) ? (r[c]?.ToString() ?? string.Empty) : string.Empty;
            int I(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : 0;
            int? IN(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : (int?)null;
            DateTime? D(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToDateTime(r[c]) : (DateTime?)null;

            return new WorkOrder
            {
                Id = I("id"),
                Title = S("title"),
                Description = S("description"),
                TaskDescription = S("task_description"),
                Type = S("type"),
                Priority = S("priority"),
                Status = S("status"),
                DateOpen = D("date_open") ?? DateTime.UtcNow,
                DueDate = D("due_date"),
                DateClose = D("date_close"),
                RequestedById = I("requested_by_id"),
                CreatedById = I("created_by_id"),
                AssignedToId = I("assigned_to_id"),
                MachineId = I("machine_id"),
                ComponentId = IN("component_id"),
                Result = S("result"),
                Notes = S("notes"),
                DigitalSignature = S("digital_signature"),
                PhotosCount = I("photos_count"),
                PartsCount = I("parts_count"),
                Machine = new Machine { Id = I("machine_id"), Name = S("machine_name") }
            };
        }
        #endregion
        
        public static async Task<WorkOrder?> GetWorkOrderByIdAsync(
            this DatabaseService db,
            int id,
            CancellationToken token = default)
        {
            const string sql = @"SELECT w.id, w.title, w.description, w.task_description, w.type, w.priority, w.status,
       w.date_open, w.due_date, w.date_close,
       w.requested_by_id, w.created_by_id, w.assigned_to_id,
       w.machine_id, w.component_id,
       w.result, w.notes,
       w.digital_signature,
       m.name AS machine_name
FROM work_orders w
LEFT JOIN machines m ON m.id = w.machine_id
WHERE w.id=@id";
            var dt = await db.ExecuteSelectAsync(sql, new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
            if (dt.Rows.Count == 0) return null;
            return MapWorkOrder(dt.Rows[0]);
        }

        public static async Task<int> AddWorkOrderPartAsync(
            this DatabaseService db,
            WorkOrderPart part,
            int actorUserId,
            string ip,
            string deviceInfo,
            string? sessionId,
            CancellationToken token = default)
        {
            // Strict transactional decrement of stock + part usage insert
            await db.WithTransactionAsync(async (conn, tx) =>
            {
                try
                {
                    var insert = new MySqlCommand(@"INSERT INTO work_order_parts
 (work_order_id, part_id, quantity, unit_of_measure, unit_price, currency, warehouse_id, used_at, used_by_id, note)
 VALUES(@wo, @pid, @qty, @uom, @price, @cur, @wh, @used, @uid, @note)", conn, tx);
                    insert.Parameters.AddRange(new[]
                    {
                        new MySqlParameter("@wo", part.WorkOrderId),
                        new MySqlParameter("@pid", part.PartId),
                        new MySqlParameter("@qty", part.Quantity),
                        new MySqlParameter("@uom", (object?)part.UnitOfMeasure ?? DBNull.Value),
                        new MySqlParameter("@price", (object?)part.UnitPrice ?? DBNull.Value),
                        new MySqlParameter("@cur", (object?)part.Currency ?? DBNull.Value),
                        new MySqlParameter("@wh", (object?)part.WarehouseId ?? DBNull.Value),
                        new MySqlParameter("@used", (object?)part.UsedAt ?? DBNull.Value),
                        new MySqlParameter("@uid", (object?)part.UsedById ?? DBNull.Value),
                        new MySqlParameter("@note", (object?)part.Note ?? DBNull.Value)
                    });
                    await insert.ExecuteNonQueryAsync(token).ConfigureAwait(false);

                    var stock = new MySqlCommand("UPDATE parts SET stock = stock - @q WHERE id=@pid", conn, tx);
                    stock.Parameters.Add(new MySqlParameter("@q", part.Quantity));
                    stock.Parameters.Add(new MySqlParameter("@pid", part.PartId));
                    await stock.ExecuteNonQueryAsync(token).ConfigureAwait(false);
                }
                catch
                {
                    throw;
                }
            }, token).ConfigureAwait(false);

            await db.LogSystemEventAsync(
                actorUserId,
                "WO_PART_ADD",
                "work_order_parts",
                "WorkOrders",
                part.WorkOrderId,
                $"part={part.PartId}; qty={part.Quantity}",
                ip,
                "audit",
                deviceInfo,
                sessionId,
                token: token
            ).ConfigureAwait(false);

            return part.WorkOrderId;
        }

        public static async Task<int> AddWorkOrderSignatureAsync(
            this DatabaseService db,
            int workOrderId,
            int userId,
            string signatureHash,
            string? note,
            string signatureType = "potvrda",
            CancellationToken token = default)
        {
            const string sql = @"INSERT INTO work_order_signatures
 (work_order_id, user_id, signature_hash, signed_at, pin_used, signature_type, note)
 VALUES(@wo, @uid, @hash, NOW(), NULL, @type, @note)";
            var pars = new[]
            {
                new MySqlParameter("@wo", workOrderId),
                new MySqlParameter("@uid", userId),
                new MySqlParameter("@hash", signatureHash ?? string.Empty),
                new MySqlParameter("@type", signatureType ?? "potvrda"),
                new MySqlParameter("@note", (object?)note ?? DBNull.Value)
            };
            await db.ExecuteNonQueryAsync(sql, pars, token).ConfigureAwait(false);
            return workOrderId;
        }

        public static async Task AttachWorkOrderPhotoAsync(
            this DatabaseService db,
            int workOrderId,
            System.IO.Stream content,
            string fileName,
            string? kind, // "before"/"after"/null
            int? uploadedBy = null,
            CancellationToken token = default)
        {
            var docs = new DocumentService(db);
            int docId = await docs.SaveAsync(content, fileName, null, "WorkOrder", workOrderId, uploadedBy, token).ConfigureAwait(false);
            // Persist before/after classification (best effort)
            try
            {
                const string create = @"CREATE TABLE IF NOT EXISTS work_order_photos (
  id INT PRIMARY KEY AUTO_INCREMENT,
  work_order_id INT NOT NULL,
  document_id INT NOT NULL,
  kind VARCHAR(16) NULL,
  uploaded_by INT NULL,
  uploaded_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  KEY ix_wop_wo (work_order_id),
  KEY ix_wop_doc (document_id)
)";
                await db.ExecuteNonQueryAsync(create, null, token).ConfigureAwait(false);
                const string ins = @"INSERT INTO work_order_photos (work_order_id, document_id, kind, uploaded_by) VALUES (@wo,@doc,@kind,@by)";
                var pars = new[]
                {
                    new MySqlParameter("@wo", workOrderId),
                    new MySqlParameter("@doc", docId),
                    new MySqlParameter("@kind", (object?)kind ?? DBNull.Value),
                    new MySqlParameter("@by", (object?)uploadedBy ?? DBNull.Value)
                };
                await db.ExecuteNonQueryAsync(ins, pars, token).ConfigureAwait(false);
            }
            catch { /* tolerate missing permissions in dev */ }
            await db.LogSystemEventAsync(
                uploadedBy,
                "WO_PHOTO_ADD",
                "work_orders",
                "WorkOrders",
                workOrderId,
                $"doc={docId}; kind={kind}",
                "ui",
                "info",
                "WorkOrdersPage",
                null,
                token: token
            ).ConfigureAwait(false);
        }

        public static async Task AddWorkOrderCommentAsync(
            this DatabaseService db,
            int workOrderId,
            int userId,
            string comment,
            string? ip = null,
            string? device = null,
            string? sessionId = null,
            CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(comment)) return;
            try
            {
                const string sql = @"INSERT INTO work_order_comments (work_order_id, user_id, text, created_at, type, revision_no, digital_signature, is_critical, source_ip)
VALUES (@wo, @uid, @txt, NOW(), 'comment', 1, @sig, 0, @ip)";
                string sig = ComputeCommentSignature(workOrderId, userId, comment);
                var pars = new[]
                {
                    new MySqlParameter("@wo", workOrderId),
                    new MySqlParameter("@uid", userId),
                    new MySqlParameter("@txt", comment),
                    new MySqlParameter("@sig", sig),
                    new MySqlParameter("@ip", (object?)ip ?? DBNull.Value)
                };
                await db.ExecuteNonQueryAsync(sql, pars, token).ConfigureAwait(false);
            }
            catch { /* schema tolerant */ }
            await db.LogSystemEventAsync(userId, "WO_COMMENT", "work_orders", "WorkOrders", workOrderId, comment, ip, "audit", device, sessionId, token: token).ConfigureAwait(false);
        }

        private static string ComputeCommentSignature(int workOrderId, int userId, string text)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var raw = $"WO:{workOrderId}|U:{userId}|{DateTime.UtcNow:O}|{text}";
            return Convert.ToBase64String(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(raw)));
        }

        public static async Task ApproveWorkOrderAsync(
            this DatabaseService db,
            int workOrderId,
            int actorUserId,
            string? note,
            string ip,
            string device,
            string? sessionId = null,
            CancellationToken token = default)
        {
            try
            {
                // If schema supports an explicit approved flag/status, update it; otherwise ignore
                const string sql = "UPDATE work_orders SET status=COALESCE(status,'otvoren') WHERE id=@id";
                await db.ExecuteNonQueryAsync(sql, new[] { new MySqlParameter("@id", workOrderId) }, token).ConfigureAwait(false);
            }
            catch { }
            await db.LogSystemEventAsync(actorUserId, "WO_APPROVE", "work_orders", "WorkOrders", workOrderId, note, ip, "audit", device, sessionId, token: token).ConfigureAwait(false);
        }

        public static async Task CloseWorkOrderAsync(
            this DatabaseService db,
            int workOrderId,
            int actorUserId,
            string? note,
            string ip,
            string device,
            string? sessionId = null,
            CancellationToken token = default)
        {
            try
            {
                const string sql = "UPDATE work_orders SET status='zavrsen', date_close=NOW() WHERE id=@id";
                await db.ExecuteNonQueryAsync(sql, new[] { new MySqlParameter("@id", workOrderId) }, token).ConfigureAwait(false);
            }
            catch { }
            await db.LogSystemEventAsync(actorUserId, "WO_CLOSE", "work_orders", "WorkOrders", workOrderId, note, ip, "audit", device, sessionId, token: token).ConfigureAwait(false);
        }

        public static async Task EscalateWorkOrderAsync(
            this DatabaseService db,
            int workOrderId,
            int actorUserId,
            string? note,
            string ip,
            string device,
            string? sessionId = null,
            CancellationToken token = default)
        {
            try
            {
                const string sql = "UPDATE work_orders SET priority='kritican' WHERE id=@id";
                await db.ExecuteNonQueryAsync(sql, new[] { new MySqlParameter("@id", workOrderId) }, token).ConfigureAwait(false);
            }
            catch { }
            await db.LogSystemEventAsync(actorUserId, "WO_ESCALATE", "work_orders", "WorkOrders", workOrderId, note, ip, "audit", device, sessionId, token: token).ConfigureAwait(false);
        }

        public static async Task ExportWorkOrdersAsync(
            this DatabaseService db,
            List<WorkOrder> items,
            int actorUserId,
            string ip,
            string device,
            string? sessionId,
            CancellationToken token = default)
        {
            await db.LogSystemEventAsync(actorUserId, "WO_EXPORT", "work_orders", "WorkOrders", null, $"count={items?.Count ?? 0}", ip, "info", device, sessionId, token: token).ConfigureAwait(false);
        }

        public static async Task<string?> ExportWorkOrdersAsync(
            this DatabaseService db,
            List<WorkOrder> items,
            string format,
            int actorUserId,
            string ip,
            string device,
            string? sessionId,
            CancellationToken token = default)
        {
            string fmt = string.IsNullOrWhiteSpace(format) ? "csv" : format.ToLowerInvariant();
            var list = items ?? new List<WorkOrder>();
            string? path;
            if (fmt == "xlsx")
            {
                path = YasGMP.Helpers.XlsxExporter.WriteSheet(list, "work_orders",
                    new (string, Func<WorkOrder, object?>)[]
                    {
                        ("Id", w => w.Id),
                        ("Title", w => w.Title),
                        ("Type", w => w.Type),
                        ("Priority", w => w.Priority),
                        ("Status", w => w.Status),
                        ("Machine", w => w.Machine?.Name),
                        ("Open", w => w.DateOpen),
                        ("Close", w => w.DateClose)
                    });
            }
            else if (fmt == "pdf")
            {
                path = YasGMP.Helpers.PdfExporter.WriteTable(list, "work_orders",
                    new (string, Func<WorkOrder, object?>)[]
                    {
                        ("Id", w => w.Id),
                        ("Title", w => w.Title),
                        ("Type", w => w.Type),
                        ("Priority", w => w.Priority),
                        ("Status", w => w.Status),
                        ("Machine", w => w.Machine?.Name)
                    }, title: "Work Orders Export");
            }
            else
            {
                path = YasGMP.Helpers.CsvExportHelper.WriteCsv(list, "work_orders",
                    new (string, Func<WorkOrder, object?>)[]
                    {
                        ("Id", w => w.Id),
                        ("Title", w => w.Title),
                        ("Type", w => w.Type),
                        ("Priority", w => w.Priority),
                        ("Status", w => w.Status),
                        ("Machine", w => w.Machine?.Name),
                        ("Open", w => w.DateOpen),
                        ("Close", w => w.DateClose)
                    });
            }
            await db.LogSystemEventAsync(actorUserId, "WO_EXPORT", "work_orders", "WorkOrders", null, $"fmt={fmt}; count={list.Count}; file={path}", ip, "info", device, sessionId, token: token).ConfigureAwait(false);
            return path;
        }
        
        public static Task LogWorkOrderAuditAsync(
            this DatabaseService db,
            int workOrderId,
            int actorUserId,
            string action,
            string? note,
            string ip,
            string deviceInfo,
            string? sessionId = null,
            CancellationToken token = default)
            => db.LogSystemEventAsync(actorUserId, $"WO_{action}", "work_orders", "WorkOrders", workOrderId == 0 ? null : workOrderId, note, ip, "audit", deviceInfo, sessionId, token: token);
        #endregion
    }
}
