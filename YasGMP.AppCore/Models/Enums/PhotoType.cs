namespace YasGMP.Models.Enums
{
    /// <summary>
    /// Photo type values stored in the legacy MySQL enum column.
    /// </summary>
    public enum PhotoType
    {
        /// <summary>Photo captured before an intervention.</summary>
        Prije = 0,

        /// <summary>Photo captured after completing work.</summary>
        Poslije = 1,

        /// <summary>Supporting documentation photo.</summary>
        Dokumentacija = 2,

        /// <summary>Other or uncategorised photo.</summary>
        Drugo = 3
    }
}

