// ==============================================================================
//  File: Helpers/ComponentMapper.cs
//  Project: YasGMP
//  Summary:
//      Robust mapping helpers between domain model MachineComponent and UI model Component.
//      • Handles nullable MachineId from domain safely when UI requires non-nullable int
//      • Fixes CS0266/CS8629 (int? -> int) and CS0019 (?? on non-nullable int)
//      • Round-trips SOP document and linked documents
//      • Rich XML docs for IntelliSense
// ==============================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using YasGMP.Models;

namespace YasGMP.Helpers
{
    /// <summary>
    /// Utility class for mapping between <see cref="MachineComponent"/> (domain) and <see cref="Component"/> (UI).
    /// <para>
    /// <b>Null-safety:</b> Methods avoid returning <c>null</c> for non-nullable members and coerce uncertain
    /// values to safe defaults where the target contract requires a value.
    /// </para>
    /// </summary>
    public static class ComponentMapper
    {
        /// <summary>
        /// Converts a domain <see cref="MachineComponent"/> to a UI <see cref="Component"/>.
        /// </summary>
        /// <param name="mc">Source domain component (must not be <c>null</c>).</param>
        /// <returns>Mapped UI component with lists and metadata populated.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="mc"/> is <c>null</c>.</exception>
        public static Component ToComponent(MachineComponent mc)
        {
            if (mc is null)
                throw new ArgumentNullException(nameof(mc));

            // Prefer explicit SOP path on the domain; otherwise fall back to first document if any.
            var docs   = mc.Documents?.ToList() ?? new List<string>();
            var sopDoc = !string.IsNullOrWhiteSpace(mc.SopDoc)
                         ? mc.SopDoc!
                         : (docs.Count > 0 ? docs[0] : string.Empty);

            return new Component
            {
                Id               = mc.Id,
                // ---- CS0266/CS8629 fix: domain is int?; UI requires int -> coerce null to 0
                MachineId        = mc.MachineId ?? 0,
                Machine          = mc.Machine!, // optional at runtime; null-forgiving for UI contract
                MachineName      = mc.Machine?.Name ?? string.Empty,
                Code             = mc.Code ?? string.Empty,
                Name             = mc.Name ?? string.Empty,
                Type             = mc.Type ?? string.Empty,
                SopDoc           = sopDoc,
                LinkedDocuments  = docs,
                InstallDate      = mc.InstallDate,
                Status           = mc.Status ?? string.Empty,
                SerialNumber     = mc.SerialNumber ?? string.Empty,
                Supplier         = mc.Supplier ?? string.Empty,
                LastModified     = mc.LastModified,
                LastModifiedById = mc.LastModifiedById,
                LastModifiedBy   = mc.LastModifiedBy!, // optional; UI keeps reference
                SourceIp         = mc.SourceIp ?? string.Empty,
                WarrantyUntil    = mc.WarrantyUntil,
                Comments         = mc.Note ?? string.Empty,
                LifecycleState   = mc.LifecyclePhase ?? string.Empty,
                DigitalSignature = mc.DigitalSignature ?? string.Empty,
                IsDeleted        = mc.IsDeleted,
                Calibrations     = mc.Calibrations?.ToList() ?? new List<Calibration>(),
                CapaCases        = mc.CapaCases?.ToList() ?? new List<CapaCase>(),
                WorkOrders       = mc.WorkOrders?.ToList() ?? new List<WorkOrder>(),
                ChangeVersion    = mc.ChangeVersion
            };
        }

        /// <summary>
        /// Converts a UI <see cref="Component"/> to a domain <see cref="MachineComponent"/>.
        /// </summary>
        /// <param name="c">Source UI component (must not be <c>null</c>).</param>
        /// <remarks>
        /// The UI model uses non-nullable <c>int</c> for <see cref="Component.MachineId"/>. The domain model
        /// allows <c>null</c> (<c>int?</c>) to match the database schema. We assign the UI value directly.
        /// </remarks>
        /// <returns>Mapped domain component.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="c"/> is <c>null</c>.</exception>
        public static MachineComponent ToMachineComponent(Component c)
        {
            if (c is null)
                throw new ArgumentNullException(nameof(c));

            // Normalize documents from UI (ensure deterministic content).
            List<string> docs = c.LinkedDocuments?.ToList() ?? new List<string>();
            if (docs.Count == 0 && !string.IsNullOrWhiteSpace(c.SopDoc))
            {
                docs = new List<string> { c.SopDoc! };
            }

            // ---- CS0019 fix: c.MachineId is non-nullable int; assign directly (no ?? operator)
            int machineIdFromUi = c.MachineId;

            return new MachineComponent
            {
                Id               = c.Id,
                MachineId        = machineIdFromUi, // implicit int -> int? is allowed
                Machine          = c.Machine!, // may be null at runtime; kept for UI convenience
                Code             = c.Code ?? string.Empty,
                Name             = c.Name ?? string.Empty,
                Type             = c.Type ?? string.Empty,
                // Round-trip SOP: write explicit property and maintain docs collection.
                SopDoc           = string.IsNullOrWhiteSpace(c.SopDoc) ? null : c.SopDoc,
                Documents        = docs,
                InstallDate      = c.InstallDate,
                Status           = c.Status ?? string.Empty,
                SerialNumber     = c.SerialNumber ?? string.Empty,
                Supplier         = c.Supplier ?? string.Empty,
                LastModified     = c.LastModified,
                LastModifiedById = c.LastModifiedById,
                LastModifiedBy   = c.LastModifiedBy!,
                SourceIp         = c.SourceIp ?? string.Empty,
                WarrantyUntil    = c.WarrantyUntil,
                Note             = c.Comments ?? string.Empty,
                LifecyclePhase   = c.LifecycleState ?? string.Empty,
                DigitalSignature = c.DigitalSignature ?? string.Empty,
                IsDeleted        = c.IsDeleted,
                Calibrations     = c.Calibrations?.ToList() ?? new List<Calibration>(),
                CapaCases        = c.CapaCases?.ToList() ?? new List<CapaCase>(),
                WorkOrders       = c.WorkOrders?.ToList() ?? new List<WorkOrder>(),
                ChangeVersion    = c.ChangeVersion
            };
        }

        /// <summary>
        /// Batch converts a sequence of domain <see cref="MachineComponent"/> instances to UI <see cref="Component"/> models.
        /// </summary>
        public static List<Component> ToComponentList(IEnumerable<MachineComponent> list)
            => (list ?? Enumerable.Empty<MachineComponent>())
               .Where(x => x is not null)
               .Select(x => ToComponent(x!))
               .ToList();

        /// <summary>
        /// Batch converts a sequence of UI <see cref="Component"/> instances to domain <see cref="MachineComponent"/> models.
        /// </summary>
        public static List<MachineComponent> ToMachineComponentList(IEnumerable<Component> list)
            => (list ?? Enumerable.Empty<Component>())
               .Where(x => x is not null)
               .Select(x => ToMachineComponent(x!))
               .ToList();
    }
}
