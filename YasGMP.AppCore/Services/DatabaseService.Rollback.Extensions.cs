// ==============================================================================
// File: Services/DatabaseService.Rollback.Extensions.cs
// Purpose: Generic rollback shim used by RollbackPreviewViewModel
// ==============================================================================

using System.Threading;
using System.Threading.Tasks;

namespace YasGMP.Services
{
    /// <summary>
    /// DatabaseService extensions implementing rollback helpers for audited entities.
    /// </summary>
    public static class DatabaseServiceRollbackExtensions
    {
        // Enqueue or log a rollback request for an entity. For now this writes a system event only.
        /// <summary>
        /// Executes the rollback entity async operation.
        /// </summary>
        public static Task RollbackEntityAsync(this DatabaseService db, string entityName, string entityId, string oldJson, CancellationToken token = default)
            => db.LogSystemEventAsync(null, "ROLLBACK_REQUEST", entityName, "RollbackModule", int.TryParse(entityId, out var id) ? id : (int?)null, oldJson, null, "audit", null, null, token: token);
    }
}

