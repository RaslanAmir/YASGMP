using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace YasGMP.Models
{
    /// <summary>
    /// Represents the Preventive Maintenance Plan.
    /// </summary>
    public partial class PreventiveMaintenancePlan
    {
        /// <summary>
        /// Represents the execution history value.
        /// </summary>
        [NotMapped]
        public List<string> ExecutionHistory
        {
            get => Split(ExecutionHistoryRaw);
            set => ExecutionHistoryRaw = Join(value);
        }

        /// <summary>
        /// Represents the attachments value.
        /// </summary>
        [NotMapped]
        public List<string> Attachments
        {
            get => Split(AttachmentsRaw);
            set => AttachmentsRaw = Join(value);
        }

        /// <summary>
        /// Represents the linked work orders value.
        /// </summary>
        [NotMapped]
        public List<string> LinkedWorkOrders
        {
            get => Split(LinkedWorkOrdersRaw);
            set => LinkedWorkOrdersRaw = Join(value);
        }

        private List<string> Split(string? raw) => string.IsNullOrWhiteSpace(raw)
            ? new List<string>()
            : raw!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        private string? Join(IEnumerable<string>? values)
            => values == null ? null : string.Join(',', values.Where(v => !string.IsNullOrWhiteSpace(v)));
    }
}
