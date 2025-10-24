using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;

namespace YasGMP.Services
{
    public static partial class DatabaseServiceWorkOrdersProvenanceExtensions
    {
        public sealed class WorkOrdersFetchResult
        {
            public required List<Models.WorkOrder> Items { get; init; }
            public required string Variant { get; init; } // "preferred" or "legacy"
        }

        public static async Task<WorkOrdersFetchResult> GetAllWorkOrdersWithProvenanceAsync(
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
            string variant;
            try
            {
                dt = await db.ExecuteSelectAsync(sqlPreferred, null, token).ConfigureAwait(false);
                variant = "preferred";
            }
            catch (MySqlException ex) when (ex.Number == 1054)
            {
                dt = await db.ExecuteSelectAsync(sqlLegacy, null, token).ConfigureAwait(false);
                variant = "legacy";
            }

            var items = new List<Models.WorkOrder>(dt.Rows.Count);
            foreach (System.Data.DataRow r in dt.Rows)
            {
                string S(string c) => r.Table.Columns.Contains(c) ? (r[c]?.ToString() ?? string.Empty) : string.Empty;
                int I(string c) => r.Table.Columns.Contains(c) && r[c] != System.DBNull.Value ? System.Convert.ToInt32(r[c]) : 0;
                int? IN(string c) => r.Table.Columns.Contains(c) && r[c] != System.DBNull.Value ? System.Convert.ToInt32(r[c]) : (int?)null;
                System.DateTime? D(string c) => r.Table.Columns.Contains(c) && r[c] != System.DBNull.Value ? System.Convert.ToDateTime(r[c]) : (System.DateTime?)null;

                items.Add(new Models.WorkOrder
                {
                    Id = I("id"),
                    Title = S("title"),
                    Description = S("description"),
                    TaskDescription = S("task_description"),
                    Type = S("type"),
                    Priority = S("priority"),
                    Status = S("status"),
                    DateOpen = D("date_open") ?? System.DateTime.UtcNow,
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
                    Machine = new Models.Machine { Id = I("machine_id"), Name = S("machine_name") }
                });
            }

            return new WorkOrdersFetchResult { Items = items, Variant = variant };
        }
    }
}

