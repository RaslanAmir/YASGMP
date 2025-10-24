using System;

namespace YasGMP.Models.Enums
{
    /// <summary>
    /// <b>DeviationStatus</b> — All workflow states for deviations/non-conformances.
    /// <para>
    /// Follows full GMP/Annex 11/ICH Q10/21 CFR Part 11 workflow: 
    /// OPEN → INVESTIGATION → ROOT_CAUSE → CAPA_LINKED → CLOSED (+ extras for full tracking)
    /// </para>
    /// </summary>
    public enum DeviationStatus
    {
        /// <summary>Newly reported, not yet assigned or investigated.</summary>
        OPEN = 0,
        /// <summary>Investigation in progress (assigned to investigator).</summary>
        INVESTIGATION = 1,
        /// <summary>Root cause defined/documented.</summary>
        ROOT_CAUSE = 2,
        /// <summary>Linked to CAPA for corrective/preventive actions.</summary>
        CAPA_LINKED = 3,
        /// <summary>Deviation closed with justification and QA signoff.</summary>
        CLOSED = 4,
        /// <summary>On hold/waiting for info (optional, for advanced workflow).</summary>
        ON_HOLD = 5,
        /// <summary>Escalated (QA/Management review required).</summary>
        ESCALATED = 6,
        /// <summary>Invalidated (not a true deviation after review).</summary>
        INVALIDATED = 7
    }
}

