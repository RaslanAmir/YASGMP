using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>Setting</b> – Super ultra mega robust GMP/CMMS/QMS application/system parameter record.
    /// </summary>
    public class Setting
    {
        /// <summary>Unique setting ID (Primary Key).</summary>
        [Key]
        [Display(Name = "ID postavke")]
        public int Id { get; set; }

        /// <summary>Unique parameter code/key (e.g., "MaxWorkOrderDuration", "EnableDigitalSignatures").</summary>
        [Required, StringLength(100)]
        [Display(Name = "Ključ postavke")]
        public string Key { get; set; } = string.Empty;

        /// <summary>Current value as string (actual value can be any type, stored as text).</summary>
        [StringLength(1024)]
        [Display(Name = "Vrijednost")]
        public string Value { get; set; } = string.Empty;

        /// <summary>Optional: default value (factory).</summary>
        [StringLength(512)]
        [Display(Name = "Zadana vrijednost")]
        public string DefaultValue { get; set; } = string.Empty;

        /// <summary>Value type (string, int, bool, decimal, json, date, enum, etc.).</summary>
        [StringLength(32)]
        [Display(Name = "Tip vrijednosti")]
        public string ValueType { get; set; } = string.Empty;

        /// <summary>Optional: minimum allowed value (for numbers, dates, etc.).</summary>
        [StringLength(255)]
        [Display(Name = "Min. vrijednost")]
        public string MinValue { get; set; } = string.Empty;

        /// <summary>Optional: maximum allowed value.</summary>
        [StringLength(255)]
        [Display(Name = "Max. vrijednost")]
        public string MaxValue { get; set; } = string.Empty;

        /// <summary>Short description/help for UI/admins.</summary>
        [StringLength(255)]
        [Display(Name = "Opis / pomoć")]
        public string Description { get; set; } = string.Empty;

        /// <summary>Category (e.g., "WorkOrders", "Calibration", "Export").</summary>
        [StringLength(50)]
        [Display(Name = "Kategorija")]
        public string Category { get; set; } = string.Empty;

        /// <summary>Subcategory, tag, or grouping (for UI filtering).</summary>
        [StringLength(50)]
        [Display(Name = "Potkategorija / Tag")]
        public string Subcategory { get; set; } = string.Empty;

        /// <summary>True if setting is sensitive or requires audit (e.g., passwords, regulatory toggles).</summary>
        [Display(Name = "Osjetljivo / Audit")]
        public bool IsSensitive { get; set; } = false;

        /// <summary>True if setting is for system-wide/global scope.</summary>
        [Display(Name = "Globalno")]
        public bool IsGlobal { get; set; } = true;

        /// <summary>Optional user/team scope (null = system/global).</summary>
        [Display(Name = "Korisnik / Tim")]
        public int? UserId { get; set; }

        /// <summary>Navigacija na korisnika.</summary>
        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        /// <summary>Optional group/role scope.</summary>
        [Display(Name = "Uloga / Grupa")]
        public int? RoleId { get; set; }

        /// <summary>Navigacija na ulogu.</summary>
        [ForeignKey(nameof(RoleId))]
        public Role Role { get; set; } = null!;

        /// <summary>Approval user (if regulated/critical change).</summary>
        [Display(Name = "Odobrio korisnik")]
        public int? ApprovedById { get; set; }

        /// <summary>Navigacija na odobravatelja.</summary>
        [ForeignKey(nameof(ApprovedById))]
        public User ApprovedBy { get; set; } = null!;

        /// <summary>Approval time.</summary>
        [Display(Name = "Vrijeme odobravanja")]
        public DateTime? ApprovedAt { get; set; }

        /// <summary>Digital signature for regulatory settings.</summary>
        [StringLength(255)]
        [Display(Name = "Digitalni potpis")]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>Status (active, deprecated, pending, review, migrated, hidden, deleted).</summary>
        [StringLength(24)]
        [Display(Name = "Status")]
        public string Status { get; set; } = "active";

        /// <summary>Date of last change.</summary>
        [Display(Name = "Zadnja promjena")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>User who last changed this setting.</summary>
        [Display(Name = "Zadnji izmijenio")]
        public int? UpdatedById { get; set; }

        /// <summary>Navigacija na urednika.</summary>
        [ForeignKey(nameof(UpdatedById))]
        public User UpdatedBy { get; set; } = null!;

        /// <summary>Full version history for rollback, migration, and regulatory audit.</summary>
        [Display(Name = "Povijest verzija")]
        public List<SettingVersion> Versions { get; set; } = new List<SettingVersion>();

        /// <summary>Full audit log of all changes, access, review, and forensics.</summary>
        [Display(Name = "Audit log")]
        public List<SettingAuditLog> AuditLogs { get; set; } = new List<SettingAuditLog>();

        /// <summary>Optional expiry or next review date.</summary>
        [Display(Name = "Datum isteka / revizije")]
        public DateTime? ExpiryDate { get; set; }
    }

    /// <summary>
    /// <b>SettingVersion</b> – Versioned value/history record for every setting change (regulatory trace, rollback).
    /// </summary>
    public class SettingVersion
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "ID postavke")]
        public int SettingId { get; set; }

        [ForeignKey(nameof(SettingId))]
        public Setting Setting { get; set; } = null!;

        [StringLength(1024)]
        [Display(Name = "Vrijednost")]
        public string Value { get; set; } = string.Empty;

        [Display(Name = "Vrijeme promjene")]
        public DateTime ChangedAt { get; set; } = DateTime.Now;

        [Display(Name = "Korisnik")]
        public int? ChangedById { get; set; }

        [ForeignKey(nameof(ChangedById))]
        public User ChangedBy { get; set; } = null!;

        [StringLength(255)]
        [Display(Name = "Napomena")]
        public string Note { get; set; } = string.Empty;
    }

    /// <summary>
    /// <b>SettingAuditLog</b> – Full forensic/audit log for every access, change, and review of a setting.
    /// </summary>
    public class SettingAuditLog
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "ID postavke")]
        public int SettingId { get; set; }

        [ForeignKey(nameof(SettingId))]
        public Setting Setting { get; set; } = null!;

        [Display(Name = "Vrijeme događaja")]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        [Display(Name = "Akcija")]
        public string Action { get; set; } = string.Empty;

        [Display(Name = "Korisnik")]
        public int? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        [StringLength(255)]
        [Display(Name = "Opis")]
        public string Description { get; set; } = string.Empty;
    }
}

