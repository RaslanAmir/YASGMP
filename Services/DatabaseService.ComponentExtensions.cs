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
            // Forward to the robust instance implementation (maps domain -> low-level inside).
            // Actor/comment are recorded via LogSystemEventAsync in the instance method.
            return db.InsertOrUpdateComponentAsync(component, isUpdate, userId, actor, cancellationToken);
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
            // Convert to domain model and forward to instance method that accepts actor context.
            var domain = Helpers.ComponentMapper.ToComponent(component);
            return db.InsertOrUpdateComponentAsync(domain, isUpdate, userId, actor, cancellationToken);
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
            var domain = Helpers.ComponentMapper.ToComponent(component);
            return db.InsertOrUpdateComponentAsync(
                domain, isUpdate, userId, actor, comment ?? "Component upsert", cancellationToken);
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
            // Forward to concrete instance delete (records audit via LogSystemEventAsync).
            return db.DeleteComponentAsync(componentId, userId, actor, deviceInfo: "UI", cancellationToken);
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
