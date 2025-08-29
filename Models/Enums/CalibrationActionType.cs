namespace YasGMP.Models.Enums
{
    /// <summary>
    /// Enumeration of all possible calibration audit actions for full GMP/ISO 17025 traceability.
    /// </summary>
    public enum CalibrationActionType
    {
        CREATE,
        UPDATE,
        DELETE,
        EXECUTE,
        CERTIFICATE_ATTACH,
        CERTIFICATE_REVOKE,
        RECALIBRATION_TRIGGER,
        CAPA_LINK
        // Add any additional actions as needed
    }
}
