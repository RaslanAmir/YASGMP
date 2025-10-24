namespace YasGMP.Models.Enums
{
    /// <summary>
    /// <b>PermissionType</b> – All possible granular actions available in the YasGMP system.
    /// <para>
    /// Used for RBAC (Role-Based Access Control), security, audit trail, menu/UI display, automation, and compliance mapping.
    /// Every action can be assigned to a role or a specific user, and logged for audit/compliance.
    /// </para>
    /// </summary>
    public enum PermissionType
    {
        /// <summary>
        /// Can view/read data, records, and screens.
        /// </summary>
        View = 0,

        /// <summary>
        /// Can create/add new records, entities, or files.
        /// </summary>
        Create = 1,

        /// <summary>
        /// Can edit/update existing records, except restricted fields.
        /// </summary>
        Edit = 2,

        /// <summary>
        /// Can delete/remove records (may be soft or hard delete).
        /// </summary>
        Delete = 3,

        /// <summary>
        /// Can approve/validate records, actions, or workflows (for supervisor/QC roles).
        /// </summary>
        Approve = 4,

        /// <summary>
        /// Can assign tasks, work orders, users, or roles.
        /// </summary>
        Assign = 5,

        /// <summary>
        /// Can export data (PDF, Excel, XML, API, etc.).
        /// </summary>
        Export = 6,

        /// <summary>
        /// Can import data (bulk, Excel, API, etc.).
        /// </summary>
        Import = 7,

        /// <summary>
        /// Can digitally or manually sign records (21 CFR Part 11/GMP).
        /// </summary>
        Sign = 8,

        /// <summary>
        /// Can override standard controls (requires justification, for supervisors/admin).
        /// </summary>
        Override = 9,

        /// <summary>
        /// Can configure system parameters, workflows, or integrations.
        /// </summary>
        Configure = 10,

        /// <summary>
        /// Can print documents or records.
        /// </summary>
        Print = 11,

        /// <summary>
        /// Can archive/retire records (not visible in main lists, preserved for audit).
        /// </summary>
        Archive = 12,

        /// <summary>
        /// Can restore records from archive/backup.
        /// </summary>
        Restore = 13,

        /// <summary>
        /// Can access or perform system or data audits.
        /// </summary>
        Audit = 14,

        /// <summary>
        /// Can download files, reports, or data.
        /// </summary>
        Download = 15,

        /// <summary>
        /// Can upload files or data.
        /// </summary>
        Upload = 16,

        /// <summary>
        /// Can add comments, notes, or feedback to records.
        /// </summary>
        Comment = 17,

        /// <summary>
        /// Can escalate cases/incidents/work orders (bonus, for managers).
        /// </summary>
        Escalate = 18,

        /// <summary>
        /// Can link or unlink related records (e.g., work order to CAPA/incident).
        /// </summary>
        Link = 19,

        /// <summary>
        /// Can perform digital verification of signatures/data (21 CFR).
        /// </summary>
        Verify = 20,

        /// <summary>
        /// Can trigger manual or automated notifications/alerts.
        /// </summary>
        Notify = 21,

        /// <summary>
        /// Can reset passwords or user credentials (admin).
        /// </summary>
        Reset = 22,

        /// <summary>
        /// Reserved for custom/advanced permissions not covered above.
        /// </summary>
        Custom = 1000
    }
}

