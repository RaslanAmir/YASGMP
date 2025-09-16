using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Keyless]
    [Table("v_suppliers_ui")]
    public class VSuppliersUi
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        [StringLength(100)]
        public string? Name { get; set; }

        [Column("vat_number")]
        [StringLength(40)]
        public string? VatNumber { get; set; }

        [Column("address")]
        public string? Address { get; set; }

        [Column("supplier_type")]
        [StringLength(40)]
        public string? SupplierType { get; set; }

        [Column("email")]
        [StringLength(100)]
        public string? Email { get; set; }

        [Column("phone")]
        [StringLength(50)]
        public string? Phone { get; set; }

        [Column("contract_file")]
        [StringLength(255)]
        public string? ContractFile { get; set; }

        [Column("notes")]
        public string? Notes { get; set; }
    }
}
