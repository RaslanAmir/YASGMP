namespace YasGMP.Models.Enums
{
    /// <summary>Audit/action verbs used for incident workflow logging.</summary>
    public enum IncidentActionType
    {
        CREATE = 0,
        UPDATE = 1,
        DELETE = 2,
        INVESTIGATION_START = 3,
        CLASSIFY = 4,
        DEVIATION_LINKED = 5,
        CAPA_LINKED = 6,
        CLOSE = 7
    }
}

