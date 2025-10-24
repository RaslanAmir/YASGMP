namespace YasGMP.Models.Enums
{
    /// <summary>
    /// <b>WarehouseEventType</b> – All transaction/event types in warehouse, inventory, and stock operations.
    /// <para>
    /// Supports traceability, audit, reporting, and real-time inventory dashboard logic.
    /// </para>
    /// </summary>
    public enum WarehouseEventType
    {
        /// <summary>Stock received into warehouse (purchase, return, supplier delivery).</summary>
        In = 0,

        /// <summary>Stock issued/withdrawn for work order, maintenance, or usage.</summary>
        Out = 1,

        /// <summary>Stock transfer between warehouses/locations.</summary>
        Transfer = 2,

        /// <summary>Inventory adjustment (manual correction, recount, audit).</summary>
        Adjust = 3,

        /// <summary>Stock marked as damaged (CAPA/incident traceability).</summary>
        Damage = 4,

        /// <summary>Stock marked as obsolete or expired (scrap, write-off).</summary>
        Obsolete = 5,

        /// <summary>Reservation for planned work (not available for other use).</summary>
        Reserved = 6,

        /// <summary>Stock released from reservation (made available again).</summary>
        ReleaseReservation = 7,

        /// <summary>Quarantine or blocked (CAPA, recall, investigation).</summary>
        Blocked = 8,

        /// <summary>Stock returned (customer, supplier, internal transfer back).</summary>
        Return = 9,

        /// <summary>Other/future (extensions, automation, IoT event).</summary>
        Other = 1000
    }
}

