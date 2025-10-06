namespace YasGMP.Models.Enums
{
    /// <summary>
    /// <b>SupplierStatus</b> – Statuses representing the full lifecycle and qualification state of a supplier/vendor.
    /// <para>
    /// Enables advanced audit, risk management, procurement, and regulatory compliance (GMP, ISO 9001/13485, CSV).
    /// </para>
    /// </summary>
    public enum SupplierStatus
    {
        /// <summary>
        /// Actively approved and available for procurement or partnership.
        /// </summary>
        Active = 0,

        /// <summary>
        /// Supplier contract or qualification has expired.
        /// </summary>
        Expired = 1,

        /// <summary>
        /// Supplier is temporarily suspended (pending investigation or review).
        /// </summary>
        Suspended = 2,

        /// <summary>
        /// Fully validated and compliant with all requirements.
        /// </summary>
        Validated = 3,

        /// <summary>
        /// Officially qualified (passed GMP/ISO qualification or audit).
        /// </summary>
        Qualified = 4,

        /// <summary>
        /// Under a CAPA (Corrective/Preventive Action) process (risk mitigation).
        /// </summary>
        CAPA = 5,

        /// <summary>
        /// Under review or (re)qualification (due to audit or issue).
        /// </summary>
        UnderReview = 6,

        /// <summary>
        /// Pending approval (e.g., in onboarding, waiting for docs).
        /// </summary>
        PendingApproval = 7,

        /// <summary>
        /// Delisted/removed—no longer in use but preserved for audit.
        /// </summary>
        Delisted = 8,

        /// <summary>
        /// Blacklisted—supplier is prohibited due to major compliance or performance failure.
        /// </summary>
        Blacklisted = 9,

        /// <summary>
        /// On probation (trial period or follow-up after CAPA).
        /// </summary>
        Probation = 10
    }
}

