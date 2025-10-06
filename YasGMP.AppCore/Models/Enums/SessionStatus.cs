namespace YasGMP.Models.Enums
{
    /// <summary>
    /// <b>SessionStatus</b> – Full lifecycle status for user login sessions in the YasGMP platform.
    /// <para>
    /// Supports security audit, SSO, SAML/OAuth, multi-device management, and regulatory traceability (GMP/CSV/21 CFR).
    /// </para>
    /// </summary>
    public enum SessionStatus
    {
        /// <summary>
        /// Session is active and valid.
        /// </summary>
        Active = 0,

        /// <summary>
        /// User has logged out (normal/manual logout).
        /// </summary>
        LoggedOut = 1,

        /// <summary>
        /// Session ended due to timeout/inactivity.
        /// </summary>
        TimedOut = 2,

        /// <summary>
        /// Session forcibly terminated by admin or security system.
        /// </summary>
        ForcedLogout = 3,

        /// <summary>
        /// Session/account locked due to failed attempts, security policy, or admin action.
        /// </summary>
        Locked = 4,

        /// <summary>
        /// Session blocked (compliance reason, IP ban, or legal hold).
        /// </summary>
        Blocked = 5,

        /// <summary>
        /// Awaiting completion of two-factor/multi-factor authentication.
        /// </summary>
        Pending2FA = 6,

        /// <summary>
        /// Session suspended pending user verification or investigation (bonus).
        /// </summary>
        Suspended = 7,

        /// <summary>
        /// Session expired due to password change, role change, or security event (bonus).
        /// </summary>
        Expired = 8
    }
}

