using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Keyless]
    [Table("v_machine_components_ui")]
    public class VMachineComponentsUi
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("machine_id")]
        public int? MachineId { get; set; }

        [Column("code")]
        [StringLength(50)]
        public string? Code { get; set; }

        [Column("name")]
        [StringLength(100)]
        public string? Name { get; set; }

        [Column("type")]
        [StringLength(50)]
        public string? Type { get; set; }

        [Column("sop_doc")]
        [StringLength(255)]
        public string? SopDoc { get; set; }

        [Column("status")]
        public string? Status { get; set; }

        [Column("install_date")]
        public DateTime? InstallDate { get; set; }
    }
}
