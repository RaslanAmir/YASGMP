// File: Helpers/ComponentMapper.cs
using System;
using System.Collections.Generic;
using System.Linq;
using YasGMP.Models;

namespace YasGMP.Helpers
{
    /// <summary>
    /// Utility class for mapping between <see cref="MachineComponent"/> and <see cref="Component"/> models.
    /// Ensures robust bi-directional conversion for full GMP, traceability, and UI/business logic needs.
    /// <para>
    /// ⚙️ Null-safety: All methods validate inputs and avoid returning null for non-nullable return types.
    /// </para>
    /// </summary>
    public static class ComponentMapper
    {
        /// <summary>
        /// Converts a <see cref="MachineComponent"/> to a <see cref="Component"/>.
        /// </summary>
        /// <param name="mc">The <see cref="MachineComponent"/> to convert. Must not be <c>null</c>.</param>
        /// <returns>A new, fully populated <see cref="Component"/> instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="mc"/> is <c>null</c>.</exception>
        public static Component ToComponent(MachineComponent mc)
        {
            if (mc is null)
                throw new ArgumentNullException(nameof(mc));

            var docs = mc.Documents?.ToList() ?? new List<string>();
            var sopDoc = docs.Count > 0 ? docs[0] : string.Empty;

            return new Component
            {
                Id = mc.Id,
                MachineId = mc.MachineId,
                // If the target property is non-nullable in your model, '!' prevents CS8601 while preserving existing semantics.
                Machine = mc.Machine!,
                MachineName = mc.Machine?.Name ?? string.Empty,
                Code = mc.Code ?? string.Empty,
                Name = mc.Name ?? string.Empty,
                Type = mc.Type ?? string.Empty,
                SopDoc = sopDoc,
                LinkedDocuments = docs,
                InstallDate = mc.InstallDate,
                Status = mc.Status ?? string.Empty,
                SerialNumber = mc.SerialNumber ?? string.Empty,
                Supplier = mc.Supplier!,
                LastModified = mc.LastModified,
                LastModifiedById = mc.LastModifiedById,
                LastModifiedBy = mc.LastModifiedBy!,
                SourceIp = mc.SourceIp ?? string.Empty,
                WarrantyUntil = mc.WarrantyUntil,
                Comments = mc.Note ?? string.Empty,
                LifecycleState = mc.LifecyclePhase ?? string.Empty,
                DigitalSignature = mc.DigitalSignature ?? string.Empty,
                IsDeleted = mc.IsDeleted,
                Calibrations = mc.Calibrations?.ToList() ?? new List<Calibration>(),
                CapaCases = mc.CapaCases?.ToList() ?? new List<CapaCase>(),
                WorkOrders = mc.WorkOrders?.ToList() ?? new List<WorkOrder>(),
                ChangeVersion = mc.ChangeVersion
            };
        }

        /// <summary>
        /// Converts a <see cref="Component"/> to a <see cref="MachineComponent"/>.
        /// </summary>
        /// <param name="c">The <see cref="Component"/> to convert. Must not be <c>null</c>.</param>
        /// <returns>A new, fully populated <see cref="MachineComponent"/> instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="c"/> is <c>null</c>.</exception>
        public static MachineComponent ToMachineComponent(Component c)
        {
            if (c is null)
                throw new ArgumentNullException(nameof(c));

            // Normalize documents to avoid CS8604 and ensure deterministic content.
            List<string> docs = c.LinkedDocuments?.ToList() ?? new List<string>();
            if (docs.Count == 0 && !string.IsNullOrWhiteSpace(c.SopDoc))
            {
                docs = new List<string> { c.SopDoc! };
            }

            return new MachineComponent
            {
                Id = c.Id,
                MachineId = c.MachineId,
                Machine = c.Machine!,
                Code = c.Code ?? string.Empty,
                Name = c.Name ?? string.Empty,
                Type = c.Type ?? string.Empty,
                Documents = docs,
                InstallDate = c.InstallDate,
                Status = c.Status ?? string.Empty,
                SerialNumber = c.SerialNumber ?? string.Empty,
                Supplier = c.Supplier!,
                LastModified = c.LastModified,
                LastModifiedById = c.LastModifiedById,
                LastModifiedBy = c.LastModifiedBy!,
                SourceIp = c.SourceIp ?? string.Empty,
                WarrantyUntil = c.WarrantyUntil,
                Note = c.Comments ?? string.Empty,
                LifecyclePhase = c.LifecycleState ?? string.Empty,
                DigitalSignature = c.DigitalSignature ?? string.Empty,
                IsDeleted = c.IsDeleted,
                Calibrations = c.Calibrations?.ToList() ?? new List<Calibration>(),
                CapaCases = c.CapaCases?.ToList() ?? new List<CapaCase>(),
                WorkOrders = c.WorkOrders?.ToList() ?? new List<WorkOrder>(),
                ChangeVersion = c.ChangeVersion
            };
        }

        /// <summary>
        /// Converts a list of <see cref="MachineComponent"/> to a list of <see cref="Component"/>.
        /// </summary>
        /// <param name="list">Source list (nullable, null-safe).</param>
        /// <returns>List of mapped <see cref="Component"/> objects.</returns>
        public static List<Component> ToComponentList(IEnumerable<MachineComponent> list)
        {
            var safe = (list ?? Enumerable.Empty<MachineComponent>())
                       .Where(x => x is not null)
                       .Select(x => ToComponent(x!))
                       .ToList();
            return safe;
        }

        /// <summary>
        /// Converts a list of <see cref="Component"/> to a list of <see cref="MachineComponent"/>.
        /// </summary>
        /// <param name="list">Source list (nullable, null-safe).</param>
        /// <returns>List of mapped <see cref="MachineComponent"/> objects.</returns>
        public static List<MachineComponent> ToMachineComponentList(IEnumerable<Component> list)
        {
            var safe = (list ?? Enumerable.Empty<Component>())
                       .Where(x => x is not null)
                       .Select(x => ToMachineComponent(x!))
                       .ToList();
            return safe;
        }
    }
}
