// ==============================================================================
// File: Services/DatabaseService.CapaExtensions.cs
// Purpose: CAPA audit helpers as extension methods (no partial/virtual).
// NOTE: Stubbed implementations so project compiles; wire up to SQL later.
//       Likely tables: capa_action_log, capa_status_history (per your SQL).
// ==============================================================================

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Models.Enums;

namespace YasGMP.Services
{
    public static class DatabaseServiceCapaExtensions
    {
        // ---- CAPA AUDIT QUERIES ------------------------------------------------

        public static Task<CapaAudit?> GetCapaAuditByIdAsync(
            this DatabaseService db,
            int id,
            CancellationToken cancellationToken = default)
        {
            // TODO: SELECT * FROM capa_action_log WHERE id = @id;
            return Task.FromResult<CapaAudit?>(null);
        }

        public static Task<List<CapaAudit>> GetCapaAuditsByCapaIdAsync(
            this DatabaseService db,
            int capaId,
            CancellationToken cancellationToken = default)
        {
            // TODO: SELECT * FROM capa_action_log WHERE capa_case_id = @capaId ORDER BY performed_at DESC;
            return Task.FromResult(new List<CapaAudit>());
        }

        public static Task<List<CapaAudit>> GetCapaAuditsByUserIdAsync(
            this DatabaseService db,
            int userId,
            CancellationToken cancellationToken = default)
        {
            // TODO: SELECT * FROM capa_action_log WHERE performed_by = @userId ORDER BY performed_at DESC;
            return Task.FromResult(new List<CapaAudit>());
        }

        public static Task<List<CapaAudit>> GetCapaAuditsByActionAsync(
            this DatabaseService db,
            CapaActionType action,
            CancellationToken cancellationToken = default)
        {
            // TODO: SELECT * FROM capa_action_log WHERE action_type = @action ORDER BY performed_at DESC;
            return Task.FromResult(new List<CapaAudit>());
        }

        public static Task<List<CapaAudit>> GetCapaAuditsByDateRangeAsync(
            this DatabaseService db,
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default)
        {
            // TODO: SELECT * FROM capa_action_log WHERE performed_at BETWEEN @from AND @to ORDER BY performed_at DESC;
            return Task.FromResult(new List<CapaAudit>());
        }
    }
}
