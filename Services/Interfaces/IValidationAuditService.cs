using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Models.Enums;

namespace YasGMP.Services.Interfaces
{
    /// <summary>
    /// <b>IValidationAuditService</b> – Interface defining the contract for GMP-compliant validation audit logging.
    /// <para>✔ Ensures traceability of every action related to validations (create, update, execute, delete).</para>
    /// <para>✔ Supports full integration with digital signatures, forensic tracking, and regulatory compliance.</para>
    /// </summary>
    public interface IValidationAuditService
    {
        /// <summary>
        /// Creates an audit record for a validation-related action.
        /// </summary>
        /// <param name="audit">ValidationAudit entity containing all audit details.</param>
        Task CreateAsync(ValidationAudit audit);

        /// <summary>
        /// Optionally logs an audit entry with minimal parameters (for convenience).
        /// </summary>
        /// <param name="validationId">ID of the validation being audited.</param>
        /// <param name="userId">ID of the user performing the action.</param>
        /// <param name="action">Action type (CREATE, UPDATE, EXECUTE, DELETE).</param>
        /// <param name="details">Additional details describing the event.</param>
        Task LogAsync(int validationId, int userId, ValidationActionType action, string details);
    }
}
