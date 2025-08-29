namespace YasGMP.Models.Enums
{
    /// <summary>
    /// <b>UserStatus</b> – User account state (full lifecycle: access, workflow, audit, forensics, regulatory compliance).
    /// <para>
    /// Use to control login, API, workflow, and user administration.  
    /// Suitable for GMP/CSV/Annex 11 and 21 CFR Part 11.
    /// </para>
    /// </summary>
    public enum UserStatus
    {
        /// <summary>
        /// User account is fully active (can login, use system).
        /// </summary>
        Active = 0,

        /// <summary>
        /// User account is created, but inactive (manual activation required).
        /// </summary>
        Inactive = 1,

        /// <summary>
        /// User is locked out (e.g., failed logins, admin lockdown).
        /// </summary>
        Locked = 2,

        /// <summary>
        /// User account is temporarily suspended (pending investigation/CAPA).
        /// </summary>
        Suspended = 3,

        /// <summary>
        /// User account is permanently deleted/archived (soft delete for GDPR).
        /// </summary>
        Deleted = 4,

        /// <summary>
        /// Account is awaiting approval (e.g. after registration or role change).
        /// </summary>
        PendingApproval = 5,

        /// <summary>
        /// User must change password (expired/first login/reset).
        /// </summary>
        PasswordExpired = 6,

        /// <summary>
        /// User must complete 2FA (login flow).
        /// </summary>
        TwoFactorRequired = 7,

        /// <summary>
        /// User account access blocked for compliance/security reason.
        /// </summary>
        Blocked = 8,

        /// <summary>
        /// User is retired (left organization, but audit trace required).
        /// </summary>
        Retired = 9
    }
}
