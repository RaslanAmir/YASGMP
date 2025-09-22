using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Schema-level extension for <see cref="Machine"/> surfacing columns that exist in the database dump
    /// but are not part of the domain convenience surface.
    /// </summary>
    public partial class Machine
    {
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("location_id")]
        public int? LocationId { get; set; }

        [Column("machine_type")]
        public string? MachineTypeRaw
        {
            get => MachineType;
            set => MachineType = value;
        }

        [Column("responsible_party")]
        public string? ResponsiblePartyRaw
        {
            get => ResponsibleParty;
            set => ResponsibleParty = value;
        }

        [Column("manufacturer_id")]
        public int? ManufacturerId { get; set; }

        [Column("machine_type_id")]
        public int? MachineTypeId { get; set; }

        [Column("status_id")]
        public int? StatusId { get; set; }

        [Column("lifecycle_phase_id")]
        public int? LifecyclePhaseId { get; set; }

        [Column("responsible_party_id")]
        public int? ResponsiblePartyId { get; set; }

        [Column("tenant_id")]
        public int? TenantId { get; set; }

        [Column("room_id")]
        public int? RoomId { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; }

        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }

        [Column("deleted_by")]
        public int? DeletedById { get; set; }

        [Column("purchase_date")]
        public DateTime? PurchaseDateRaw { get; set; }

        [Column("warranty_expiry")]
        public DateTime? WarrantyExpiryRaw { get; set; }

        [Column("notes")]
        public string? NotesRaw { get; set; }

        [Column("icollection<machine_component>")]
        public string? LegacyMachineComponentsCollection { get; set; }

        [Column("icollection<machine_lifecycle_event>")]
        public string? LegacyLifecycleEventsCollection { get; set; }

        [Column("icollection<capa_case>")]
        public string? LegacyCapaCasesCollection { get; set; }

        [Column("icollection<quality_event>")]
        public string? LegacyQualityEventsCollection { get; set; }

        [Column("icollection<validation>")]
        public string? LegacyValidationsCollection { get; set; }

        [Column("icollection<inspection>")]
        public string? LegacyInspectionsCollection { get; set; }

        [Column("icollection<work_order>")]
        public string? LegacyWorkOrdersCollection { get; set; }

        [Column("icollection<photo>")]
        public string? LegacyPhotosCollection { get; set; }

        [Column("icollection<attachment>")]
        public string? LegacyAttachmentsCollection { get; set; }

        [Column("icollection<calibration>")]
        public string? LegacyCalibrationsCollection { get; set; }

        [Column("user?")]
        public string? LegacyUserLabel { get; set; }
    }
}
