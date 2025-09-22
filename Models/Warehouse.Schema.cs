using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace YasGMP.Models
{
    /// <summary>
    /// Schema helpers that keep <see cref="Warehouse"/> aligned with legacy columns while
    /// exposing friendlier domain shape for the application.
    /// </summary>
    public partial class Warehouse
    {
        [Column("compliance_docs")]
        public string? ComplianceDocsRaw
        {
            get => ComplianceDocs.Count == 0 ? null : string.Join(';', ComplianceDocs);
            set => ComplianceDocs = string.IsNullOrWhiteSpace(value)
                ? new List<string>()
                : value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }
    }
}
