using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Represents the Change Control.
    /// </summary>
    public partial class ChangeControl
    {
        /// <summary>
        /// Represents the date requested value.
        /// </summary>
        [NotMapped]
        public DateTime? DateRequested
        {
            get => DateTime.TryParse(DateRequestedRaw, out var parsed) ? parsed : null;
            set => DateRequestedRaw = value?.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// Gets or sets the change type.
        /// </summary>
        [NotMapped]
        public string? ChangeType { get; set; }

    }
}
