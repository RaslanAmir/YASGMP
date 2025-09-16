using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Keyless]
    [Table("vw_calibrations_filter")]
    public class VwCalibrationsFilter
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("component_name")]
        [StringLength(100)]
        public string? ComponentName { get; set; }

        [Column("supplier_name")]
        [StringLength(100)]
        public string? SupplierName { get; set; }

        [Column("calibration_date")]
        public DateTime? CalibrationDate { get; set; }

        [Column("next_due")]
        public DateTime? NextDue { get; set; }

        [Column("result")]
        public string? Result { get; set; }

        [Column("comment")]
        [StringLength(255)]
        public string? Comment { get; set; }

        [Column("digital_signature")]
        [StringLength(128)]
        public string? DigitalSignature { get; set; }
    }
}
