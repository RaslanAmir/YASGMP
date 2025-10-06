using System;

namespace YasGMP.Models.DTO
{
    /// <summary>
    /// Lightweight DTO representing a change control entry used for selection dialogs.
    /// Provides enough information for users to identify the record without loading
    /// the entire <see cref="Models.ChangeControl"/> aggregate.
    /// </summary>
    public sealed class ChangeControlSummaryDto
    {
        /// <summary>Primary key of the change control record.</summary>
        public int Id { get; set; }

        /// <summary>Business/traceability code (e.g., CC-2024-001).</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Human readable title/summary.</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Current workflow status (draft, under_review, approved, implemented, closed...).</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Optional requested date for quick context in pickers.</summary>
        public DateTime? DateRequested { get; set; }
    }
}

