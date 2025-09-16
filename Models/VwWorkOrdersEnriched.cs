using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Keyless]
    [Table("vw_work_orders_enriched")]
    public class VwWorkOrdersEnriched
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("machine_id")]
        public int? MachineId { get; set; }

        [Column("component_id")]
        public int? ComponentId { get; set; }

        [Column("type")]
        public string? Type { get; set; }

        [Column("created_by")]
        public int? CreatedBy { get; set; }

        [Column("assigned_to")]
        public int? AssignedTo { get; set; }

        [Column("date_open")]
        public DateTime? DateOpen { get; set; }

        [Column("date_close")]
        public DateTime? DateClose { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("result")]
        public string? Result { get; set; }

        [Column("status")]
        public string? Status { get; set; }

        [Column("digital_signature")]
        [StringLength(128)]
        public string? DigitalSignature { get; set; }

        [Column("priority")]
        public string? Priority { get; set; }

        [Column("related_incident")]
        public int? RelatedIncident { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("status_id")]
        public int? StatusId { get; set; }

        [Column("type_id")]
        public int? TypeId { get; set; }

        [Column("priority_id")]
        public int? PriorityId { get; set; }

        [Column("tenant_id")]
        public int? TenantId { get; set; }

        [Column("status_name")]
        [StringLength(100)]
        public string? StatusName { get; set; }

        [Column("type_name")]
        [StringLength(100)]
        public string? TypeName { get; set; }

        [Column("priority_name")]
        [StringLength(100)]
        public string? PriorityName { get; set; }

        [Column("machine_name")]
        [StringLength(100)]
        public string? MachineName { get; set; }

        [Column("component_name")]
        [StringLength(100)]
        public string? ComponentName { get; set; }
    }
}
