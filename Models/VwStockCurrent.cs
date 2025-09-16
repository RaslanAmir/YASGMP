using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Keyless]
    [Table("vw_stock_current")]
    public class VwStockCurrent
    {
        [Column("part_id")]
        public int PartId { get; set; }

        [Column("part_code")]
        [StringLength(50)]
        public string? PartCode { get; set; }

        [Column("part_name")]
        [StringLength(100)]
        public string? PartName { get; set; }

        [Column("warehouse_id")]
        public int? WarehouseId { get; set; }

        [Column("warehouse_name")]
        [StringLength(150)]
        public string? WarehouseName { get; set; }

        [Column("quantity")]
        public int? Quantity { get; set; }

        [Column("min_threshold")]
        public int? MinThreshold { get; set; }

        [Column("max_threshold")]
        public int? MaxThreshold { get; set; }
    }
}
