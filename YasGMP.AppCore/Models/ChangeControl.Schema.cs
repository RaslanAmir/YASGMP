using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    public partial class ChangeControl
    {
        [NotMapped]
        public DateTime? DateRequested
        {
            get => DateTime.TryParse(DateRequestedRaw, out var parsed) ? parsed : null;
            set => DateRequestedRaw = value?.ToString("yyyy-MM-dd HH:mm:ss");
        }

        [NotMapped]
        public string? ChangeType { get; set; }

    }
}
