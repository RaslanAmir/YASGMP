namespace YasGMP.Models.Enums
{
    /// <summary>Status values used by the Incident workflow.</summary>
    public enum IncidentStatus
    {
        OPEN = 0,
        REPORTED = 1,
        INVESTIGATION = 2,
        CLASSIFIED = 3,
        DEVIATION_LINKED = 4,
        CAPA_LINKED = 5,
        CLOSED = 6
    }
}
