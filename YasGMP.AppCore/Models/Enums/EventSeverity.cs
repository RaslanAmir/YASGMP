namespace YasGMP.Models.Enums
{
    /// <summary>
    /// <b>EventSeverity</b> – Classification of the severity/importance of system, audit, security, and compliance events.
    /// <para>
    /// <b>Usage:</b> Used in system event logs, audit trails, notification systems, dashboard alerts, SIEM integrations, and forensic compliance reports.
    /// </para>
    /// <b>Compliant with:</b> FDA 21 CFR Part 11, EU GMP Annex 11, ISO 27001, GAMP5.
    /// </summary>
    public enum EventSeverity
    {
        /// <summary>
        /// Informational event – normal system operation, no action required.
        /// </summary>
        Info = 0,

        /// <summary>
        /// Warning event – potentially problematic or abnormal event, but not critical.
        /// </summary>
        Warning = 1,

        /// <summary>
        /// Critical event – urgent attention required; system failure, safety, data loss, regulatory risk.
        /// </summary>
        Critical = 2,

        /// <summary>
        /// Audit event – regulatory, compliance, or internal audit event.
        /// </summary>
        Audit = 3,

        /// <summary>
        /// Security event – unauthorized access, attempted breach, authentication failure, or privilege misuse.
        /// </summary>
        Security = 4,

        /// <summary>
        /// Forensic event – used for post-incident analysis, tamper detection, or legal chain-of-custody.
        /// </summary>
        Forensic = 5,

        /// <summary>
        /// Debug event – only visible in debug/developer mode for troubleshooting (not for production logs).
        /// </summary>
        Debug = 6,

        /// <summary>
        /// Notification – triggers user/system notifications (may be info, warning, or critical).
        /// </summary>
        Notification = 7
    }
}
