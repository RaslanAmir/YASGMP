using System.Collections.Generic;

namespace YasGMP.Models
{
    public partial class PreventiveMaintenancePlan
    {
        public PreventiveMaintenancePlan Clone()
        {
            var clone = new PreventiveMaintenancePlan
            {
                Id = Id,
                Code = Code,
                Name = Name,
                Description = Description,
                MachineId = MachineId,
                Machine = Machine,
                ComponentId = ComponentId,
                Component = Component,
                Frequency = Frequency,
                ChecklistFile = ChecklistFile,
                ResponsibleUserId = ResponsibleUserId,
                ResponsibleUser = ResponsibleUser,
                LastExecuted = LastExecuted,
                NextDue = NextDue,
                Status = Status,
                DigitalSignature = DigitalSignature,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt,
                ChecklistTemplateId = ChecklistTemplateId,
                MachineLabel = MachineLabel,
                ComponentLabel = ComponentLabel,
                ResponsibleUserLabel = ResponsibleUserLabel,
                ExecutionHistoryRaw = ExecutionHistoryRaw,
                RiskScore = RiskScore,
                AiRecommendation = AiRecommendation,
                LastModified = LastModified,
                LastModifiedById = LastModifiedById,
                LastModifiedBy = LastModifiedBy,
                LastModifiedByName = LastModifiedByName,
                SourceIp = SourceIp,
                SessionId = SessionId,
                GeoLocation = GeoLocation,
                AttachmentsRaw = AttachmentsRaw,
                Version = Version,
                PreviousVersionId = PreviousVersionId,
                PreviousVersionLabel = PreviousVersionLabel,
                IsActiveVersion = IsActiveVersion,
                LinkedWorkOrdersRaw = LinkedWorkOrdersRaw,
                IsAutomated = IsAutomated,
                RequiresNotification = RequiresNotification,
                AnomalyScore = AnomalyScore,
                Note = Note
            };

            clone.ExecutionHistory = CloneList(ExecutionHistory);
            clone.Attachments = CloneList(Attachments);
            clone.LinkedWorkOrders = CloneList(LinkedWorkOrders);

            return clone;
        }

        private static List<string> CloneList(List<string>? source)
            => source != null ? new List<string>(source) : new List<string>();
    }
}
