namespace YasGMP.Models.Enums
{
    /// <summary>
    /// <b>DigitalSignatureMethod</b> – Enumerates all methods for digital or electronic signatures in GMP/CSV/21 CFR Part 11 contexts.
    /// <para>
    /// Fully auditable, extensible for any authentication/signature scheme.
    /// </para>
    /// </summary>
    public enum DigitalSignatureMethod
    {
        /// <summary>PIN code (local or server-side validation).</summary>
        PIN = 0,
        /// <summary>Digital certificate (qualified signature, smart card, etc).</summary>
        Certificate = 1,
        /// <summary>Biometric authentication (fingerprint, face, retina, etc).</summary>
        Biometric = 2,
        /// <summary>Password-based authentication (as signature, less preferred for compliance).</summary>
        Password = 3,
        /// <summary>API signature (remote or federated signature via API).</summary>
        API = 4,
        /// <summary>Mobile app signature (QR, push confirmation, etc).</summary>
        MobileApp = 5,
        /// <summary>SMS/One-Time Password (OTP) confirmation.</summary>
        SMS_OTP = 6,
        /// <summary>Manual/wet ink (with scanned or photo evidence, if permitted by workflow).</summary>
        Manual = 7,
        /// <summary>Hardware token (YubiKey, USB token, etc).</summary>
        HardwareToken = 8,
        /// <summary>Other/custom (for new signature standards).</summary>
        Other = 1000
    }
}
