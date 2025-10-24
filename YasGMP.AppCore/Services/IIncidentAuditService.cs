using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Services.Interfaces
{
    /// <summary>
    /// Interface for GMP-compliant audit service related to Incident management.
    /// Provides audit logging for all Incident-related actions.
    /// </summary>
    public interface IIncidentAuditService
    {
        /// <summary>
        /// Creates a new audit log entry for an Incident action.
        /// </summary>
        /// <param name="audit">The Incident audit data.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        Task CreateAsync(IncidentAudit audit);
    }
}

