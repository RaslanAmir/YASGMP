using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Services.Interfaces
{
    /// <summary>
    /// Interface for managing GMP-compliant calibration audit logs.
    /// </summary>
    public interface ICalibrationAuditService
    {
        Task CreateAsync(CalibrationAudit audit);
        // Add more methods if you have them (GetByCalibrationIdAsync, etc)
    }
}
