namespace YasGMP.Models.Enums
{
    /// <summary>
    /// SupplierActionType – Defines all actions on a supplier that require audit and digital signature.
    /// </summary>
    public enum SupplierActionType
    {
        CREATE = 0,
        UPDATE = 1,
        DELETE = 2,
        SUSPEND = 3,
        REQUALIFICATION = 4,
        CAPA_LINK = 5
    }
}
