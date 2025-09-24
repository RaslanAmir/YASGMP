using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models.Enums
{
    /// <summary>
    /// <b>InventoryTransactionType</b> – Types of transactions supported by the inventory module.
    /// Provides audit-compliant tracking for every change in inventory/stock.
    /// </summary>
    public enum InventoryTransactionType
    {
        [Display(Name = "Ulaz u skladište")] In = 0,
        [Display(Name = "Izlaz iz skladišta")] Out = 1,
        [Display(Name = "Transfer između skladišta")] Transfer = 2,
        [Display(Name = "Korekcija/adjustment")] Adjustment = 3,
        [Display(Name = "Oštećenje/defekt")] Damage = 4,
        [Display(Name = "Zastarjeli/otpis")] Obsolete = 5,
        [Display(Name = "Rezervacija za nalog")] Reserved = 6,
        [Display(Name = "Storno/poništenje")] Reversal = 7,
        [Display(Name = "Custom/other")] Custom = 1000
    }
}
