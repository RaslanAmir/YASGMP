using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Services.Interfaces
{
    /// <summary>
    /// Interface for GMP-compliant audit service for Supplier actions.
    /// </summary>
    public interface ISupplierAuditService
    {
        Task CreateAsync(SupplierAudit audit);
        // Add other relevant methods as needed
    }
}
