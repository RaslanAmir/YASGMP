// ==============================================================================
// File: Services/DatabaseService.ComponentExtensions.cs
// Purpose: Add extension methods so existing component calls (using either
//          Component or MachineComponent) compile. Replace TEMP bodies with
//          your real DB calls when ready.
// ==============================================================================

using System.Threading;
using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Services
{
    /// <summary>
    /// Extension methods for component CRUD invoked from ComponentService/ViewModel.
    /// </summary>
    public static class DatabaseServiceComponentExtensions
    {
        // =========================================================================
        // INSERT / UPDATE — Component
        // =========================================================================

        /// <summary>Upsert using "actor/comment" style context.</summary>
        public static Task<int> InsertOrUpdateComponentAsync(
            this DatabaseService db,
            Component component,
            bool isUpdate,
            int userId,
            string actor,                 // e.g., $"User:{userId}"
            string comment,
            CancellationToken cancellationToken = default)
        {
            // TODO: call your real DB upsert and return new/updated ID.
            return Task.FromResult(component?.Id ?? 0);
        }

        /// <summary>Upsert using IP/Device context.</summary>
        public static Task<int> InsertOrUpdateComponentAsync(
            this DatabaseService db,
            Component component,
            bool isUpdate,
            int userId,
            string ipAddress,
            string deviceInfo,
            string? comment = null,
            CancellationToken cancellationToken = default)
        {
            var actor = $"IP:{ipAddress}|Device:{deviceInfo}";
            return db.InsertOrUpdateComponentAsync(
                component, isUpdate, userId, actor, comment ?? "Component upsert", cancellationToken);
        }

        // =========================================================================
        // INSERT / UPDATE — MachineComponent (fixes CS1503 in ComponentService.cs)
        // =========================================================================

        /// <summary>Upsert MachineComponent using "actor/comment" style context.</summary>
        public static Task<int> InsertOrUpdateComponentAsync(
            this DatabaseService db,
            MachineComponent component,
            bool isUpdate,
            int userId,
            string actor,                 // e.g., $"User:{userId}"
            string comment,
            CancellationToken cancellationToken = default)
        {
            // TODO: call your real DB upsert for MachineComponent and return ID.
            return Task.FromResult(component?.Id ?? 0);
        }

        /// <summary>Upsert MachineComponent using IP/Device context.</summary>
        public static Task<int> InsertOrUpdateComponentAsync(
            this DatabaseService db,
            MachineComponent component,
            bool isUpdate,
            int userId,
            string ipAddress,
            string deviceInfo,
            string? comment = null,
            CancellationToken cancellationToken = default)
        {
            var actor = $"IP:{ipAddress}|Device:{deviceInfo}";
            return db.InsertOrUpdateComponentAsync(
                component, isUpdate, userId, actor, comment ?? "Component upsert", cancellationToken);
        }

        // =========================================================================
        // DELETE (componentId is shared; both types use the same delete)
        // =========================================================================

        /// <summary>Delete using "actor/comment" style context.</summary>
        public static Task DeleteComponentAsync(
            this DatabaseService db,
            int componentId,
            int userId,
            string actor,                 // e.g., $"User:{userId}"
            string comment,
            CancellationToken cancellationToken = default)
        {
            // TODO: call your real DB delete.
            return Task.CompletedTask;
        }

        /// <summary>Delete using IP/Device context.</summary>
        public static Task DeleteComponentAsync(
            this DatabaseService db,
            int componentId,
            int userId,
            string ipAddress,
            string deviceInfo,
            string? comment = null,
            CancellationToken cancellationToken = default)
        {
            var actor = $"IP:{ipAddress}|Device:{deviceInfo}";
            return db.DeleteComponentAsync(
                componentId, userId, actor, comment ?? "Component deleted", cancellationToken);
        }
    }
}
