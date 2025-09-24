using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Models.Enums;


namespace YasGMP.Services.Interfaces
{
    /// <summary>
    /// <b>ICAPAService</b> – Interface for GMP-compliant Corrective and Preventive Actions (CAPA) service.
    /// <para>
    /// Defines all core CAPA operations: CRUD, workflow, audit, risk, digital signatures, notifications, and extensibility hooks.
    /// </para>
    /// </summary>
    public interface ICAPAService
    {
        /// <summary>
        /// Gets all CAPA cases.
        /// </summary>
        /// <returns>List of CAPA cases.</returns>
        Task<List<CapaCase>> GetAllAsync();

        /// <summary>
        /// Gets a specific CAPA case by ID.
        /// </summary>
        /// <param name="id">CAPA ID.</param>
        /// <returns>CAPA case if found; otherwise, null.</returns>
        Task<CapaCase> GetByIdAsync(int id);

        /// <summary>
        /// Creates a new CAPA case with audit and risk notification.
        /// </summary>
        /// <param name="capa">CAPA case to create.</param>
        /// <param name="userId">User ID performing the operation.</param>
        Task CreateAsync(CapaCase capa, int userId);

        /// <summary>
        /// Updates an existing CAPA case.
        /// </summary>
        /// <param name="capa">CAPA case with new data.</param>
        /// <param name="userId">User ID performing the update.</param>
        Task UpdateAsync(CapaCase capa, int userId);

        /// <summary>
        /// Deletes a CAPA case.
        /// </summary>
        /// <param name="capaId">CAPA ID to delete.</param>
        /// <param name="userId">User performing deletion.</param>
        Task DeleteAsync(int capaId, int userId);

        /// <summary>
        /// Starts investigation for a CAPA case.
        /// </summary>
        /// <param name="capaId">CAPA ID.</param>
        /// <param name="userId">User starting investigation.</param>
        /// <param name="investigator">Investigator's name.</param>
        Task StartInvestigationAsync(int capaId, int userId, string investigator);

        /// <summary>
        /// Defines action plan for a CAPA case.
        /// </summary>
        /// <param name="capaId">CAPA ID.</param>
        /// <param name="userId">User defining plan.</param>
        /// <param name="actionPlan">Action plan string.</param>
        Task DefineActionPlanAsync(int capaId, int userId, string actionPlan);

        /// <summary>
        /// Approves CAPA action plan.
        /// </summary>
        /// <param name="capaId">CAPA ID.</param>
        /// <param name="approverId">User approving.</param>
        Task ApproveActionPlanAsync(int capaId, int approverId);

        /// <summary>
        /// Marks CAPA actions as executed.
        /// </summary>
        /// <param name="capaId">CAPA ID.</param>
        /// <param name="userId">User executing.</param>
        /// <param name="executionComment">Execution notes.</param>
        Task MarkActionExecutedAsync(int capaId, int userId, string executionComment);

        /// <summary>
        /// Verifies CAPA effectiveness.
        /// </summary>
        /// <param name="capaId">CAPA ID.</param>
        /// <param name="verifierId">Verifier user ID.</param>
        /// <param name="effective">Is it effective?</param>
        Task VerifyEffectivenessAsync(int capaId, int verifierId, bool effective);

        /// <summary>
        /// Closes a CAPA case.
        /// </summary>
        /// <param name="capaId">CAPA ID.</param>
        /// <param name="userId">User closing.</param>
        /// <param name="closureComment">Closure comments.</param>
        Task CloseCapaAsync(int capaId, int userId, string closureComment);

        /// <summary>
        /// Calculates risk score for a CAPA case.
        /// </summary>
        /// <param name="capa">CAPA case.</param>
        /// <returns>Risk score.</returns>
        int CalculateRiskScore(CapaCase capa);

        /// <summary>
        /// Checks risk and triggers notifications if score exceeds thresholds.
        /// </summary>
        /// <param name="capa">CAPA case.</param>
        Task CheckAndNotifyRisk(CapaCase capa);

        /// <summary>
        /// Validates a CAPA case for all business/GMP rules.
        /// </summary>
        /// <param name="capa">CAPA to validate.</param>
        void ValidateCapa(CapaCase capa);

        /// <summary>
        /// Generates a digital signature hash for the CAPA.
        /// </summary>
        /// <param name="capa">CAPA case.</param>
        /// <returns>Signature string.</returns>
        string GenerateDigitalSignature(CapaCase capa);

        /// <summary>
        /// Generates a digital signature hash from a payload.
        /// </summary>
        /// <param name="payload">String payload.</param>
        /// <returns>Signature string.</returns>
        string GenerateDigitalSignature(string payload);

        /// <summary>
        /// GMP/Annex 11 audit log for any CAPA action.
        /// </summary>
        /// <param name="capaId">CAPA ID.</param>
        /// <param name="userId">User ID.</param>
        /// <param name="action">Action type.</param>
        /// <param name="details">Details.</param>
        Task LogAudit(int capaId, int userId, CapaActionType action, string details);
    }
}
