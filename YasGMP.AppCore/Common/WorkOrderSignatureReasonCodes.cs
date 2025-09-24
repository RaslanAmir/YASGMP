using System.Collections.Generic;

namespace YasGMP.Common
{
    /// <summary>
    /// Canonical work-order signature reason codes exposed to the UI and persistence layers.
    /// Ensures users pick an explicit justification prior to applying a digital signature.
    /// </summary>
    public static class WorkOrderSignatureReasonCodes
    {
        /// <summary>Represents a selectable reason code.</summary>
        public sealed record Reason(string Code, string DisplayName, string Description)
        {
            /// <summary>Friendly label rendered by UI pickers and drop-downs.</summary>
            public string DisplayLabel => string.IsNullOrWhiteSpace(Description)
                ? $"{DisplayName} ({Code})"
                : $"{DisplayName} ({Code}) – {Description}";

            /// <inheritdoc />
            public override string ToString() => DisplayLabel;
        }

        /// <summary>Predefined GMP/CSV compliant reasons for signing a work order.</summary>
        public static IReadOnlyList<Reason> All { get; } = new List<Reason>
        {
            new("WO_CLOSE",    "Zatvaranje naloga",        "Potpis pri formalnom zatvaranju radnog naloga."),
            new("WO_APPROVE",  "Odobrenje naloga",          "Autorizacija planiranog radnog naloga (pred izvršenje)."),
            new("WO_VERIFY",   "Verifikacija izvršenja",   "Potvrda da je nalog izvršen prema proceduri."),
            new("WO_RELEASE",  "Release opreme",           "Puštanje opreme/procesa nakon održavanja."),
            new("CUSTOM",       "Drugo – upiši kod",         "Korisnički definirani GMP razlog (obavezan unos koda).")
        };

        /// <summary>Reason code used when the caller supplies their own justification code.</summary>
        public const string Custom = "CUSTOM";
    }
}

