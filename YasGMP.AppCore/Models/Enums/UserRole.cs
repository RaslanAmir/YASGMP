using System;

namespace YasGMP.Models.Enums
{
    /// <summary>
    /// <b>UserRole</b> – All possible system roles for the GMP/CMMS/YasGMP platform.
    /// <para>
    /// Structured, extensible, and audit-ready role definitions.
    /// <br/>Compliant with GMP, 21 CFR Part 11, CSV, Annex 11, FDA/EU.
    /// </para>
    /// <remarks>
    /// To add a new role:  
    /// 1. Add it here with XML summary.  
    /// 2. Update the RBAC/permission mapping.  
    /// 3. Sync workflow, audit, and UI code.
    /// </remarks>
    /// </summary>
    public enum UserRole
    {
        /// <summary>Basic technician/operator (assigned jobs only).</summary>
        Technician = 0,
        /// <summary>Supervisor/Team Lead (assigns, approves, escalates, signs off).</summary>
        Supervisor = 1,
        /// <summary>Quality/Audit team (audit, override, forensics).</summary>
        Auditor = 2,
        /// <summary>Department Manager (team, budget, escalation, reporting).</summary>
        Manager = 3,
        /// <summary>Maintenance Planner (schedule, preventive, resources).</summary>
        Planner = 4,
        /// <summary>Storekeeper/Inventory (warehouse, spare parts, logistics).</summary>
        Storekeeper = 5,
        /// <summary>Calibration/Validation Officer (IQ/OQ/PQ, certificates, sign-off).</summary>
        CalibrationOfficer = 6,
        /// <summary>External Contractor/Vendor (own interventions only).</summary>
        Contractor = 7,
        /// <summary>IT/System Admin (security, integration, config, backup).</summary>
        ITAdmin = 8,
        /// <summary>Compliance Manager (SOP, CAPA, risk management).</summary>
        ComplianceManager = 9,
        /// <summary>Lab/QA Specialist (OOS, stability, environmental).</summary>
        LaboratorySpecialist = 10,
        /// <summary>HR/Training Admin (training records, onboarding).</summary>
        TrainingAdmin = 11,
        /// <summary>Read-only (view all, cannot modify).</summary>
        ReadOnly = 12,
        /// <summary>External Inspector/Auditor (regulators, notified bodies).</summary>
        ExternalAuditor = 13,
        /// <summary>QMS Admin (config, process, master data).</summary>
        QmsAdmin = 14,
        // === Reserved for advanced/future roles ===
        /// <summary>API/Integration Bot (automation, system integration, IoT).</summary>
        IntegrationBot = 20,
        /// <summary>Super Administrator (cannot be deleted/demoted, full rights).</summary>
        SuperAdmin = 99
    }
}
