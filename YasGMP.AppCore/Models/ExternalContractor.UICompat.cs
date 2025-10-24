using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// UI-compatibility extensions for ExternalContractor used by MAUI views.
    /// Adds non-mapped convenience properties that some XAML views bind to.
    /// </summary>
    public partial class ExternalContractor
    {
        /// <summary>
        /// Optional UI-only start date of cooperation (not persisted).
        /// </summary>
        [NotMapped]
        public DateTime? CooperationStart { get; set; }

        /// <summary>
        /// Optional UI-only end date of cooperation (not persisted).
        /// </summary>
        [NotMapped]
        public DateTime? CooperationEnd { get; set; }

        /// <summary>
        /// Compatibility alias for <see cref="Note"/> to match older XAML bindings.
        /// </summary>
        [NotMapped]
        public string? Comment
        {
            get => Note;
            set => Note = value;
        }
    }
}

