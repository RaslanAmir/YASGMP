using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace YasGMP.Models
{
    public partial class IncidentLog
    {
        [NotMapped]
        public List<string> Attachments
        {
            get => string.IsNullOrWhiteSpace(AttachmentsRaw)
                ? new List<string>()
                : AttachmentsRaw.Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries).ToList();
            set => AttachmentsRaw = value == null || value.Count == 0 ? null : string.Join(',', value);
        }
    }
}
