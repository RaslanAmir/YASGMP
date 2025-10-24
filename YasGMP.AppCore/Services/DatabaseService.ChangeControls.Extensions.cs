// ==============================================================================
// File: Services/DatabaseService.ChangeControls.Extensions.cs
// Purpose: Query helpers for change control lookups (picker/list scenarios)
// ==============================================================================

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Models.DTO;

namespace YasGMP.Services
{
    public static class DatabaseServiceChangeControlsExtensions
    {
        /// <summary>
        /// Returns a lightweight list of change controls (id/code/title/status/date) suitable
        /// for selection dialogs. The query only pulls essential fields to keep the dialog fast
        /// even on large datasets.
        /// </summary>
        public static async Task<List<ChangeControlSummaryDto>> GetChangeControlsAsync(
            this DatabaseService db,
            CancellationToken token = default)
        {
            const string sql = @"SELECT id, code, title, status, date_requested
FROM change_controls
ORDER BY id DESC";

            var table = await db.ExecuteSelectAsync(sql, null, token).ConfigureAwait(false);
            var list = new List<ChangeControlSummaryDto>(table.Rows.Count);
            foreach (DataRow row in table.Rows)
            {
                var dto = new ChangeControlSummaryDto
                {
                    Id            = row.Table.Columns.Contains("id") && row["id"] != DBNull.Value
                                    ? Convert.ToInt32(row["id"])
                                    : 0,
                    Code          = row.Table.Columns.Contains("code") ? row["code"]?.ToString() ?? string.Empty : string.Empty,
                    Title         = row.Table.Columns.Contains("title") ? row["title"]?.ToString() ?? string.Empty : string.Empty,
                    Status        = row.Table.Columns.Contains("status") ? row["status"]?.ToString() ?? string.Empty : string.Empty,
                    DateRequested = row.Table.Columns.Contains("date_requested") && row["date_requested"] != DBNull.Value
                                    ? Convert.ToDateTime(row["date_requested"])
                                    : (DateTime?)null
                };
                list.Add(dto);
            }

            return list;
        }
    }
}

