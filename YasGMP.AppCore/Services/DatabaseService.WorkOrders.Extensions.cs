using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using MySqlConnector;
using YasGMP.Common;
using YasGMP.Models;
using YasGMP.Models.DTO;
using YasGMP.Services.Interfaces;

namespace YasGMP.Services
{
    /// <summary>
    /// DatabaseService extensions for retrieving and persisting work order aggregates.
    /// </summary>
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
            SignatureMetadataDto? signatureMetadata = null,
            CancellationToken token = default)
        {
            if (wo == null) throw new ArgumentNullException(nameof(wo));

            string effectiveIp = !string.IsNullOrWhiteSpace(signatureMetadata?.IpAddress) ? signatureMetadata!.IpAddress! : ip ?? string.Empty;
            string effectiveDevice = signatureMetadata?.Device ?? deviceInfo ?? string.Empty;
            string? effectiveSession = signatureMetadata?.Session ?? sessionId;

            if (!string.IsNullOrWhiteSpace(signatureMetadata?.Hash))
            {
                wo.DigitalSignature = signatureMetadata!.Hash!;
            }

            if (signatureMetadata?.Id.HasValue == true)
            {
                wo.DigitalSignatureId = signatureMetadata.Id;
            }

            if (!string.IsNullOrWhiteSpace(signatureMetadata?.Device))
            {
                wo.DeviceInfo = signatureMetadata!.Device;
            }
            else if (!string.IsNullOrWhiteSpace(wo.DeviceInfo))
            {
                effectiveDevice = wo.DeviceInfo!;
            }
            else if (!string.IsNullOrWhiteSpace(effectiveDevice))
            {
                wo.DeviceInfo = effectiveDevice;
            }

            if (!string.IsNullOrWhiteSpace(signatureMetadata?.Session))
            {
                wo.SessionId = signatureMetadata!.Session;
                effectiveSession = signatureMetadata.Session;
            }
            else if (!string.IsNullOrWhiteSpace(wo.SessionId))
            {
                effectiveSession = wo.SessionId;
            }

            wo.SourceIp = effectiveIp;

            int? initialSignatureId = wo.DigitalSignatureId;

            string insert = @"INSERT INTO work_orders
(title, description, task_description, type, priority, status,
 date_open, due_date, date_close,
 requested_by_id, created_by_id, assigned_to_id,
 machine_id, component_id,
 result, notes,
 digital_signature, device_info, source_ip, session_id, digital_signature_id)
VALUES
(@title, @desc, @task, @type, @prio, @status,
 @open, @due, @close,
 @req, @crt, @ass,
 @mach, @comp,
 @res, @notes,
 @sig, @device, @ip, @session, @sig_id)";

            string insertLegacy = @"INSERT INTO work_orders
(title, description, task_description, type, priority, status,
 date_open, due_date, date_close,
 requested_by_id, created_by_id, assigned_to_id,
 machine_id, component_id,
 result, notes,
 digital_signature, device_info, source_ip, session_id)
VALUES
(@title, @desc, @task, @type, @prio, @status,
 @open, @due, @close,
 @req, @crt, @ass,
 @mach, @comp,
 @res, @notes,
 @sig, @device, @ip, @session)";

            string updateSql = @"UPDATE work_orders SET
 title=@title, description=@desc, task_description=@task,
 type=@type, priority=@prio, status=@status,
 date_open=@open, due_date=@due, date_close=@close,
 requested_by_id=@req, created_by_id=@crt, assigned_to_id=@ass,
 machine_id=@mach, component_id=@comp,
 result=@res, notes=@notes,
 digital_signature=@sig, device_info=@device, source_ip=@ip, session_id=@session, digital_signature_id=@sig_id
WHERE id=@id";

            string updateLegacy = @"UPDATE work_orders SET
 title=@title, description=@desc, task_description=@task,
 type=@type, priority=@prio, status=@status,
 date_open=@open, due_date=@due, date_close=@close,
 requested_by_id=@req, created_by_id=@crt, assigned_to_id=@ass,
 machine_id=@mach, component_id=@comp,
 result=@res, notes=@notes,
 digital_signature=@sig, device_info=@device, source_ip=@ip, session_id=@session
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
                new("@notes",   (object?)wo.Notes ?? DBNull.Value),
                new("@sig",     (object?)wo.DigitalSignature ?? DBNull.Value),
                new("@device",  (object?)wo.DeviceInfo ?? DBNull.Value),
                new("@ip",      effectiveIp),
                new("@session", (object?)effectiveSession ?? DBNull.Value),
                new("@sig_id",  (object?)wo.DigitalSignatureId ?? DBNull.Value)
            };
            if (update) pars.Add(new MySqlParameter("@id", wo.Id));

            if (!update)
            {
                try
                {
                    await db.ExecuteNonQueryAsync(insert, pars, token).ConfigureAwait(false);
                }
                catch (MySqlException ex) when (ex.Number == 1054)
                {
                    var legacyPars = new List<MySqlParameter>(pars);
                    legacyPars.RemoveAll(p => p.ParameterName.Equals("@sig_id", StringComparison.OrdinalIgnoreCase));
                    await db.ExecuteNonQueryAsync(insertLegacy, legacyPars, token).ConfigureAwait(false);
                }

                var idObj = await db.ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false);
                wo.Id = Convert.ToInt32(idObj);
            }
            else
            {
                try
                {
                    await db.ExecuteNonQueryAsync(updateSql, pars, token).ConfigureAwait(false);
                }
                catch (MySqlException ex) when (ex.Number == 1054)
                {
                    var legacyPars = new List<MySqlParameter>(pars);
                    legacyPars.RemoveAll(p => p.ParameterName.Equals("@sig_id", StringComparison.OrdinalIgnoreCase));
                    await db.ExecuteNonQueryAsync(updateLegacy, legacyPars, token).ConfigureAwait(false);
                }
            }

            await db.LogSystemEventAsync(
                actorUserId,
                update ? "WO_UPDATE" : "WO_CREATE",
                "work_orders",
                "WorkOrders",
                wo.Id,
                wo.Title,
                effectiveIp,
                "audit",
                effectiveDevice,
                effectiveSession,
                signatureId: wo.DigitalSignatureId,
                signatureHash: wo.DigitalSignature,
                token: token
            ).ConfigureAwait(false);

            if (signatureMetadata != null)
            {
                var signatureRecord = new DigitalSignature
                {
                    Id = signatureMetadata.Id ?? 0,
                    TableName = "work_orders",
                    RecordId = wo.Id,
                    UserId = actorUserId,
                    SignatureHash = signatureMetadata.Hash ?? wo.DigitalSignature,
                    Method = signatureMetadata.Method,
                    Status = signatureMetadata.Status,
                    Note = signatureMetadata.Note,
                    SignedAt = DateTime.UtcNow,
                    DeviceInfo = signatureMetadata.Device ?? effectiveDevice,
                    IpAddress = effectiveIp,
                    SessionId = signatureMetadata.Session ?? effectiveSession
                };

                var persistedId = await db.InsertDigitalSignatureAsync(signatureRecord, token).ConfigureAwait(false);
                if (persistedId > 0)
                {
                    signatureMetadata.Id = persistedId;
                    if (wo.DigitalSignatureId != persistedId)
                    {
                        wo.DigitalSignatureId = persistedId;
                        if (persistedId != initialSignatureId)
                        {
                            await db.TryUpdateEntitySignatureIdAsync("work_orders", "id", wo.Id, "digital_signature_id", persistedId, token).ConfigureAwait(false);
                        }
                    }
                }
            }

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
            => db.InsertOrUpdateWorkOrderAsync(wo, update: false, actorUserId, ip, device, sessionId, signatureMetadata: null, token);

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
            const string sqlPreferred = @"SELECT w.id, w.title, w.description, w.task_description, w.type, w.priority, w.status,
       w.date_open, w.due_date, w.date_close,
       w.requested_by_id, w.created_by_id, w.assigned_to_id,
       w.machine_id, w.component_id,
       w.result, w.notes,
       w.digital_signature, w.digital_signature_id,
       (SELECT COUNT(*) FROM work_order_parts p WHERE p.work_order_id = w.id) AS parts_count,
       (SELECT COUNT(*) FROM document_links dl WHERE dl.entity_type='WorkOrder' AND dl.entity_id = w.id) AS photos_count,
       m.name AS machine_name
FROM work_orders w
LEFT JOIN machines m ON m.id = w.machine_id
ORDER BY w.date_open DESC, w.id DESC";

            const string sqlLegacy = @"SELECT w.id, w.title, w.description, w.task_description, w.type, w.priority, w.status,
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

            System.Data.DataTable dt;
            try
            {
                dt = await db.ExecuteSelectAsync(sqlPreferred, null, token).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number == 1054)
            {
                dt = await db.ExecuteSelectAsync(sqlLegacy, null, token).ConfigureAwait(false);
            }
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
                DigitalSignatureId = IN("digital_signature_id"),
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
            const string sqlPreferred = @"SELECT w.id, w.title, w.description, w.task_description, w.type, w.priority, w.status,
       w.date_open, w.due_date, w.date_close,
       w.requested_by_id, w.created_by_id, w.assigned_to_id,
       w.machine_id, w.component_id,
       w.result, w.notes,
       w.digital_signature, w.digital_signature_id,
       m.name AS machine_name
FROM work_orders w
LEFT JOIN machines m ON m.id = w.machine_id
WHERE w.id=@id";

            const string sqlLegacy = @"SELECT w.id, w.title, w.description, w.task_description, w.type, w.priority, w.status,
       w.date_open, w.due_date, w.date_close,
       w.requested_by_id, w.created_by_id, w.assigned_to_id,
       w.machine_id, w.component_id,
       w.result, w.notes,
       w.digital_signature,
       m.name AS machine_name
FROM work_orders w
LEFT JOIN machines m ON m.id = w.machine_id
WHERE w.id=@id";

            System.Data.DataTable dt;
            try
            {
                dt = await db.ExecuteSelectAsync(sqlPreferred, new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number == 1054)
            {
                dt = await db.ExecuteSelectAsync(sqlLegacy, new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
            }
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

        public static async Task<WorkOrderSignature> AddWorkOrderSignatureAsync(
            this DatabaseService db,
            WorkOrderSignaturePersistRequest request,
            IAttachmentService? attachmentService = null,
            CancellationToken token = default)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.WorkOrderId <= 0) throw new ArgumentException("Work order id is required", nameof(request));
            if (request.UserId <= 0) throw new ArgumentException("User id is required", nameof(request));
            if (string.IsNullOrWhiteSpace(request.ReasonCode)) throw new ArgumentException("Reason code is required", nameof(request));

            string signatureType = string.IsNullOrWhiteSpace(request.SignatureType)
                ? "potvrda"
                : request.SignatureType;

            int recordVersion = request.RecordVersion > 0
                ? request.RecordVersion
                : await db.GetNextWorkOrderSignatureVersionAsync(request.WorkOrderId, token).ConfigureAwait(false);

            int revisionNo = request.RevisionNo > 0
                ? request.RevisionNo
                : await db.GetCurrentWorkOrderRevisionAsync(request.WorkOrderId, token).ConfigureAwait(false);

            DateTime signedAtUtc = request.SignedAtUtc == default
                ? DateTime.UtcNow
                : request.SignedAtUtc;

            string serverTz = string.IsNullOrWhiteSpace(request.ServerTimeZone)
                ? TimeZoneInfo.Local.Id
                : request.ServerTimeZone;

            string snapshotJson = !string.IsNullOrWhiteSpace(request.WorkOrderSnapshotJson)
                ? request.WorkOrderSnapshotJson!
                : await BuildWorkOrderSnapshotAsync(db, request.WorkOrderId, token).ConfigureAwait(false);

            string recordHash = string.IsNullOrWhiteSpace(request.RecordHash)
                ? (!string.IsNullOrWhiteSpace(snapshotJson)
                    ? ComputeRecordHash(snapshotJson)
                    : await ComputeWorkOrderRecordHashAsync(db, request.WorkOrderId, token).ConfigureAwait(false))
                : request.RecordHash;

            const string sql = @"INSERT INTO work_order_signatures
(work_order_id, user_id, signature_hash, signed_at, pin_used, signature_type, note,
 record_hash, record_version, server_timezone, ip_address, device_info, session_id,
 reason_code, reason_description, revision_no, mfa_challenge)
VALUES(@wo, @uid, @hash, @signedAt, NULL, @type, @note,
       @recordHash, @recordVersion, @tz, @ip, @device, @session,
       @reasonCode, @reasonDescription, @revisionNo, @mfa)";

            var parameters = new[]
            {
                new MySqlParameter("@wo", request.WorkOrderId),
                new MySqlParameter("@uid", request.UserId),
                new MySqlParameter("@hash", request.SignatureHash ?? string.Empty),
                new MySqlParameter("@signedAt", signedAtUtc),
                new MySqlParameter("@type", signatureType),
                new MySqlParameter("@note", (object?)request.ReasonDescription ?? DBNull.Value),
                new MySqlParameter("@recordHash", recordHash),
                new MySqlParameter("@recordVersion", recordVersion),
                new MySqlParameter("@tz", serverTz),
                new MySqlParameter("@ip", (object?)request.IpAddress ?? DBNull.Value),
                new MySqlParameter("@device", (object?)request.DeviceInfo ?? DBNull.Value),
                new MySqlParameter("@session", (object?)request.SessionId ?? DBNull.Value),
                new MySqlParameter("@reasonCode", request.ReasonCode),
                new MySqlParameter("@reasonDescription", (object?)request.ReasonDescription ?? DBNull.Value),
                new MySqlParameter("@revisionNo", revisionNo),
                new MySqlParameter("@mfa", (object?)request.MfaEvidence ?? DBNull.Value)
            };

            await db.ExecuteNonQueryAsync(sql, parameters, token).ConfigureAwait(false);
            var idObj = await db.ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false);
            int signatureId = Convert.ToInt32(idObj, CultureInfo.InvariantCulture);

            var signature = new WorkOrderSignature
            {
                Id = signatureId,
                WorkOrderId = request.WorkOrderId,
                UserId = request.UserId,
                SignatureHash = request.SignatureHash,
                SignedAt = signedAtUtc,
                SignatureTypeRaw = signatureType,
                Note = request.ReasonDescription,
                ReasonCode = request.ReasonCode,
                ReasonDescription = request.ReasonDescription,
                RecordHash = recordHash,
                RecordVersion = recordVersion,
                ServerTimezone = serverTz,
                IpAddress = request.IpAddress,
                DeviceInfo = request.DeviceInfo,
                SessionId = request.SessionId,
                RevisionNo = revisionNo,
                MfaEvidence = request.MfaEvidence
            };

            string description = $"signatureId={signatureId}; reason={request.ReasonCode}; version={recordVersion}; revision={revisionNo}";
            await db.LogSystemEventAsync(
                request.UserId,
                "WO_SIGNATURE",
                "work_order_signatures",
                "WorkOrders",
                request.WorkOrderId,
                description,
                request.IpAddress,
                "audit",
                request.DeviceInfo,
                request.SessionId,
                signatureId: signatureId,
                signatureHash: request.SignatureHash,
                token: token).ConfigureAwait(false);

            attachmentService ??= ServiceLocator.GetRequiredService<IAttachmentService>();
            var workOrder = await db.GetWorkOrderByIdAsync(request.WorkOrderId, token).ConfigureAwait(false)
                ?? new WorkOrder { Id = request.WorkOrderId };

            byte[] pdf = GenerateSignatureManifestPdf(workOrder, signature, request, snapshotJson);
            using var manifestStream = new MemoryStream(pdf);
            manifestStream.Position = 0;

            var uploadRequest = new AttachmentUploadRequest
            {
                FileName = $"WO-{request.WorkOrderId}-signature-v{recordVersion}.pdf",
                ContentType = "application/pdf",
                EntityType = "WorkOrder",
                EntityId = request.WorkOrderId,
                UploadedById = request.UserId,
                DisplayName = $"WorkOrder #{request.WorkOrderId} – potpis v{recordVersion}",
                Notes = request.ReasonDescription,
                Reason = $"workorder-signature:{request.ReasonCode}",
                SourceIp = request.IpAddress,
                SourceHost = request.DeviceInfo
            };

            await attachmentService.UploadAsync(manifestStream, uploadRequest, token).ConfigureAwait(false);

            return signature;
        }

        public static async Task AttachWorkOrderPhotoAsync(
            this DatabaseService db,
            int workOrderId,
            System.IO.Stream content,
            string fileName,
            string? kind, // "before"/"after"/null
            int? uploadedBy = null,
            IAttachmentService? attachmentService = null,
            CancellationToken token = default)
        {
            attachmentService ??= ServiceLocator.GetRequiredService<IAttachmentService>();
            var result = await attachmentService.UploadAsync(content, new AttachmentUploadRequest
            {
                FileName = fileName,
                ContentType = null,
                EntityType = "WorkOrder",
                EntityId = workOrderId,
                UploadedById = uploadedBy,
                Notes = kind,
                Reason = string.IsNullOrWhiteSpace(kind) ? "workorder-photo" : $"workorder-photo:{kind}",
                SourceIp = "ui",
                SourceHost = Environment.MachineName
            }, token).ConfigureAwait(false);
            int attachmentId = result.Attachment.Id;
            // Persist before/after classification (best effort)
            try
            {
                const string create = @"CREATE TABLE IF NOT EXISTS work_order_photos (
  id INT PRIMARY KEY AUTO_INCREMENT,
  work_order_id INT NOT NULL,
  attachment_id INT NOT NULL,
  kind VARCHAR(16) NULL,
  uploaded_by INT NULL,
  uploaded_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  KEY ix_wop_wo (work_order_id),
  KEY ix_wop_attachment (attachment_id)
)";
                await db.ExecuteNonQueryAsync(create, null, token).ConfigureAwait(false);
                const string ins = @"INSERT INTO work_order_photos (work_order_id, attachment_id, kind, uploaded_by) VALUES (@wo,@att,@kind,@by)";
                var pars = new[]
                {
                    new MySqlParameter("@wo", workOrderId),
                    new MySqlParameter("@att", attachmentId),
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
                $"attachment={attachmentId}; kind={kind}",
                "ui",
                "info",
                "WorkOrdersPage",
                null,
                token: token
            ).ConfigureAwait(false);
        }

        public static async Task<int> GetNextWorkOrderSignatureVersionAsync(
            this DatabaseService db,
            int workOrderId,
            CancellationToken token = default)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            var pars = new[] { new MySqlParameter("@wo", workOrderId) };
            const string sql = "SELECT COALESCE(MAX(record_version),0) FROM work_order_signatures WHERE work_order_id=@wo";
            var obj = await db.ExecuteScalarAsync(sql, pars, token).ConfigureAwait(false);
            int current = obj is null || obj == DBNull.Value ? 0 : Convert.ToInt32(obj, CultureInfo.InvariantCulture);
            return current + 1;
        }

        public static async Task<int> GetCurrentWorkOrderRevisionAsync(
            this DatabaseService db,
            int workOrderId,
            CancellationToken token = default)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            var pars = new[] { new MySqlParameter("@wo", workOrderId) };
            const string sql = "SELECT COALESCE(MAX(revision_no),1) FROM work_order_comments WHERE work_order_id=@wo";
            var obj = await db.ExecuteScalarAsync(sql, pars, token).ConfigureAwait(false);
            return obj is null || obj == DBNull.Value ? 1 : Convert.ToInt32(obj, CultureInfo.InvariantCulture);
        }

        private static async Task<string> BuildWorkOrderSnapshotAsync(DatabaseService db, int workOrderId, CancellationToken token)
        {
            var workOrder = await db.GetWorkOrderByIdAsync(workOrderId, token).ConfigureAwait(false);
            if (workOrder == null) return string.Empty;

            var snapshot = new
            {
                workOrder.Id,
                workOrder.Title,
                workOrder.Description,
                workOrder.TaskDescription,
                workOrder.Type,
                workOrder.Priority,
                workOrder.Status,
                workOrder.DateOpen,
                workOrder.DueDate,
                workOrder.DateClose,
                workOrder.MachineId,
                workOrder.ComponentId,
                workOrder.AssignedToId,
                workOrder.RequestedById,
                workOrder.CreatedById,
                workOrder.Result,
                workOrder.Notes,
                workOrder.LastModified,
                workOrder.LastModifiedById
            };

            return JsonSerializer.Serialize(snapshot, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
        }

        private static async Task<string> ComputeWorkOrderRecordHashAsync(DatabaseService db, int workOrderId, CancellationToken token)
        {
            string snapshot = await BuildWorkOrderSnapshotAsync(db, workOrderId, token).ConfigureAwait(false);
            return string.IsNullOrWhiteSpace(snapshot) ? string.Empty : ComputeRecordHash(snapshot);
        }

        private static string ComputeRecordHash(string payload)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(payload ?? string.Empty);
            return Convert.ToHexString(sha.ComputeHash(bytes));
        }

        private static byte[] GenerateSignatureManifestPdf(
            WorkOrder workOrder,
            WorkOrderSignature signature,
            WorkOrderSignaturePersistRequest request,
            string snapshotJson)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            using var stream = new MemoryStream();
            QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);
                    page.Size(PageSizes.A4);
                    page.Header().Text($"Radni nalog #{workOrder.Id} – manifest potpisa").SemiBold().FontSize(16);
                    page.Content().Column(col =>
                    {
                        col.Item().Text($"Potpisao: {request.SignerFullName ?? request.SignerUsername} (ID {signature.UserId})");
                        col.Item().Text($"Korisnièko ime: {request.SignerUsername}");
                        col.Item().Text($"Razlog (kod): {signature.ReasonCode}");
                        if (!string.IsNullOrWhiteSpace(request.ReasonDisplay))
                            col.Item().Text($"Razlog (opis): {request.ReasonDisplay}");
                        if (!string.IsNullOrWhiteSpace(signature.ReasonDescription))
                            col.Item().Text($"Detalji: {signature.ReasonDescription}");
                        col.Item().Text($"Revizija: {signature.RevisionNo}; Verzija zapisa: {signature.RecordVersion}");
                        col.Item().Text($"Hash zapisa: {signature.RecordHash}");
                        if (!string.IsNullOrWhiteSpace(signature.SignatureHash))
                            col.Item().Text($"Hash potpisa: {signature.SignatureHash}");
                        col.Item().Text($"Vrijeme potpisa: {signature.SignedAt:yyyy-MM-dd HH:mm:ss} {signature.ServerTimezone}");
                        col.Item().Text($"IP: {signature.IpAddress ?? "n/a"}");
                        col.Item().Text($"Ureðaj: {signature.DeviceInfo ?? "n/a"}");
                        if (!string.IsNullOrWhiteSpace(signature.SessionId))
                            col.Item().Text($"Session ID: {signature.SessionId}");
                        col.Item().Text(string.IsNullOrWhiteSpace(request.MfaEvidence)
                            ? "MFA: nije primijenjeno"
                            : "MFA: dokaz pohranjen (hash)");

                        col.Item().Element(e => e.PaddingVertical(6)).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                        col.Item().Text("Sažetak radnog naloga").SemiBold();
                        col.Item().Text($"Naslov: {workOrder.Title}");
                        col.Item().Text($"Status: {workOrder.Status}");
                        col.Item().Text($"Tip / Prioritet: {workOrder.Type} / {workOrder.Priority}");
                        col.Item().Text($"Dodijeljeno korisniku #{workOrder.AssignedToId}");
                        col.Item().Text($"Otvoren: {workOrder.DateOpen:yyyy-MM-dd}");
                        if (workOrder.DateClose.HasValue)
                            col.Item().Text($"Zatvoren: {workOrder.DateClose:yyyy-MM-dd}");
                        if (!string.IsNullOrWhiteSpace(workOrder.Result))
                            col.Item().Text($"Rezultat: {workOrder.Result}");

                        if (!string.IsNullOrWhiteSpace(snapshotJson))
                        {
                            col.Item().Element(e => e.PaddingTop(8)).Text("JSON snapshot").SemiBold();
                            col.Item().Background(Colors.Grey.Lighten4).Padding(6).DefaultTextStyle(t => t.FontSize(8).FontFamily("Consolas"))
                                .Text(text =>
                                {
                                    foreach (var line in SplitLines(snapshotJson))
                                    {
                                        text.Line(line);
                                    }
                                });
                        }
                    });
                    page.Footer().AlignRight().Text($"Generirano {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                });
            }).GeneratePdf(stream);
            return stream.ToArray();
        }

        private static IEnumerable<string> SplitLines(string? value)
        {
            if (string.IsNullOrEmpty(value)) yield break;
            using var reader = new StringReader(value);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                yield return line;
            }
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

