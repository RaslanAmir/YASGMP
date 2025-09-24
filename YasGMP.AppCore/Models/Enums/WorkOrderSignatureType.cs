using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models.Enums
{
    /// <summary>
    /// <b>WorkOrderSignatureType</b> – All possible types of signatures for a work order step.
    /// Enables strict audit, multi-stage approval, automation, and full GMP/CSV/21 CFR Part 11 traceability.
    /// Extensible for emerging tech (AI, mobile, remote, etc.).
    /// </summary>
    public enum WorkOrderSignatureType
    {
        /// <summary>
        /// Technician “locks” the work order for exclusive processing (step cannot continue until signed).
        /// </summary>
        [Display(Name = "Zaključavanje")]
        Lock = 0,

        /// <summary>
        /// Supervisor or QA “approves” a stage (e.g., ready for execution, closed).
        /// </summary>
        [Display(Name = "Odobrenje")]
        Approval = 1,

        /// <summary>
        /// Technician confirms that a task/intervention is finished (“done” sign-off).
        /// </summary>
        [Display(Name = "Potvrda izvršenja")]
        ExecutionConfirmation = 2,

        /// <summary>
        /// Quality/QA verifies the result or effectiveness (post-execution/closure).
        /// </summary>
        [Display(Name = "Verifikacija QA")]
        QAVerification = 3,

        /// <summary>
        /// Legal, cryptographic e-signature (full digital signature with hash).
        /// </summary>
        [Display(Name = "Digitalni potpis")]
        DigitalSignature = 4,

        /// <summary>
        /// Biometric signature (fingerprint, facial recognition, retina scan, etc.).
        /// </summary>
        [Display(Name = "Biometrijski potpis")]
        Biometric = 5,

        /// <summary>
        /// Numeric PIN verification (for user authentication/audit).
        /// </summary>
        [Display(Name = "PIN verifikacija")]
        Pin = 6,

        /// <summary>
        /// Inspector, auditor, or 3rd-party review/inspection signature.
        /// </summary>
        [Display(Name = "Potpis audita")]
        AuditSignature = 7,

        /// <summary>
        /// Remote/e-mail workflow approval or sign-off (recorded for audit).
        /// </summary>
        [Display(Name = "E-mail potvrda")]
        EmailApproval = 8,

        /// <summary>
        /// Automated/robotic/API/AI-generated signature (for automation/IoT/AI).
        /// </summary>
        [Display(Name = "Automatski/robotski potpis")]
        Automated = 9,

        /// <summary>
        /// Signature required for rollback or undoing a prior step (mandatory audit).
        /// </summary>
        [Display(Name = "Potpis povlačenja/rollback")]
        Rollback = 10,

        /// <summary>
        /// Mobile app sign-off (for field/remote workflows, future mobile expansion).
        /// </summary>
        [Display(Name = "Mobilna potvrda")]
        Mobile = 11,

        /// <summary>
        /// SSO/external system authentication signature (e.g., AD, SAML, OAuth).
        /// </summary>
        [Display(Name = "SSO/External Auth")]
        SSOExternal = 12,

        /// <summary>
        /// Custom/other/legacy (for unknown, future, or vendor-specific types).
        /// </summary>
        [Display(Name = "Custom/other")]
        Custom = 1000
    }
}
