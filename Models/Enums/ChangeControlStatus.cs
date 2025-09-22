using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models.Enums
{
    /// <summary>
    /// Lifecycle stages for change control records.
    /// </summary>
    public enum ChangeControlStatus
    {
        [Display(Name = "Draft")]
        Draft = 0,

        [Display(Name = "Submitted")]
        Submitted = 1,

        [Display(Name = "Under Review")]
        UnderReview = 2,

        [Display(Name = "Approved")]
        Approved = 3,

        [Display(Name = "Implemented")]
        Implemented = 4,

        [Display(Name = "Closed")]
        Closed = 5,

        [Display(Name = "Rejected")]
        Rejected = 6,

        [Display(Name = "Cancelled")]
        Cancelled = 7
    }
}
