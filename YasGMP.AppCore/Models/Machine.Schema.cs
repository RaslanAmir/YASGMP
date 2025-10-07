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
        /// <summary>
        /// Gets or sets the created at.
        /// </summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the updated at.
        /// </summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the location id.
        /// </summary>
        [Column("location_id")]
        public int? LocationId { get; set; }

        /// <summary>
        /// Represents the machine type raw value.
        /// </summary>
        [Column("machine_type")]
        public string? MachineTypeRaw
        {
            get => MachineType;
            set => MachineType = value;
        }

        /// <summary>
        /// Represents the responsible party raw value.
        /// </summary>
        [Column("responsible_party")]
        public string? ResponsiblePartyRaw
        {
            get => ResponsibleParty;
            set => ResponsibleParty = value;
        }

        /// <summary>
        /// Gets or sets the manufacturer id.
        /// </summary>
        [Column("manufacturer_id")]
        public int? ManufacturerId { get; set; }

        /// <summary>
        /// Gets or sets the machine type id.
        /// </summary>
        [Column("machine_type_id")]
        public int? MachineTypeId { get; set; }

        /// <summary>
        /// Gets or sets the status id.
        /// </summary>
        [Column("status_id")]
        public int? StatusId { get; set; }

        /// <summary>
        /// Gets or sets the lifecycle phase id.
        /// </summary>
        [Column("lifecycle_phase_id")]
        public int? LifecyclePhaseId { get; set; }

        /// <summary>
        /// Gets or sets the responsible party id.
        /// </summary>
        [Column("responsible_party_id")]
        public int? ResponsiblePartyId { get; set; }

        /// <summary>
        /// Gets or sets the tenant id.
        /// </summary>
        [Column("tenant_id")]
        public int? TenantId { get; set; }

        /// <summary>
        /// Gets or sets the room id.
        /// </summary>
        [Column("room_id")]
        public int? RoomId { get; set; }

        /// <summary>
        /// Gets or sets the is deleted.
        /// </summary>
        [Column("is_deleted")]
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Gets or sets the deleted at.
        /// </summary>
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// Gets or sets the deleted by id.
        /// </summary>
        [Column("deleted_by")]
        public int? DeletedById { get; set; }

        /// <summary>
        /// Gets or sets the purchase date raw.
        /// </summary>
        [Column("purchase_date")]
        public DateTime? PurchaseDateRaw { get; set; }

        /// <summary>
        /// Gets or sets the warranty expiry raw.
        /// </summary>
        [Column("warranty_expiry")]
        public DateTime? WarrantyExpiryRaw { get; set; }

        /// <summary>
        /// Gets or sets the notes raw.
        /// </summary>
        [Column("notes")]
        public string? NotesRaw { get; set; }

        /// <summary>
        /// Gets or sets the legacy machine components collection.
        /// </summary>
        [Column("icollection<machine_component>")]
        public string? LegacyMachineComponentsCollection { get; set; }

        /// <summary>
        /// Gets or sets the legacy lifecycle events collection.
        /// </summary>
        [Column("icollection<machine_lifecycle_event>")]
        public string? LegacyLifecycleEventsCollection { get; set; }

        /// <summary>
        /// Gets or sets the legacy capa cases collection.
        /// </summary>
        [Column("icollection<capa_case>")]
        public string? LegacyCapaCasesCollection { get; set; }

        /// <summary>
        /// Gets or sets the legacy quality events collection.
        /// </summary>
        [Column("icollection<quality_event>")]
        public string? LegacyQualityEventsCollection { get; set; }

        /// <summary>
        /// Gets or sets the legacy validations collection.
        /// </summary>
        [Column("icollection<validation>")]
        public string? LegacyValidationsCollection { get; set; }

        /// <summary>
        /// Gets or sets the legacy inspections collection.
        /// </summary>
        [Column("icollection<inspection>")]
        public string? LegacyInspectionsCollection { get; set; }

        /// <summary>
        /// Gets or sets the legacy work orders collection.
        /// </summary>
        [Column("icollection<work_order>")]
        public string? LegacyWorkOrdersCollection { get; set; }

        /// <summary>
        /// Gets or sets the legacy photos collection.
        /// </summary>
        [Column("icollection<photo>")]
        public string? LegacyPhotosCollection { get; set; }

        /// <summary>
        /// Gets or sets the legacy attachments collection.
        /// </summary>
        [Column("icollection<attachment>")]
        public string? LegacyAttachmentsCollection { get; set; }

        /// <summary>
        /// Gets or sets the legacy calibrations collection.
        /// </summary>
        [Column("icollection<calibration>")]
        public string? LegacyCalibrationsCollection { get; set; }

        /// <summary>
        /// Gets or sets the legacy user label.
        /// </summary>
        [Column("user?")]
        public string? LegacyUserLabel { get; set; }
    }
}
