using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Contract surfaced to WPF view models for preventive maintenance orchestration.
/// Wraps the shared <see cref="YasGMP.Services.PreventiveMaintenanceService"/> so the
/// shell can schedule, approve, and execute plans without duplicating AppCore logic.
/// </summary>
public interface IPreventiveMaintenanceService
{
    Task<IReadOnlyList<PreventiveMaintenancePlan>> GetAllAsync();

    Task<PreventiveMaintenancePlan> GetByIdAsync(int id);

    Task CreateAsync(PreventiveMaintenancePlan plan, int userId);

    Task UpdateAsync(PreventiveMaintenancePlan plan, int userId);

    Task DeleteAsync(int ppmId, int userId);

    Task<IReadOnlyList<PreventiveMaintenancePlan>> GetOverduePlansAsync();

    Task MarkExecutedAsync(int ppmId, int userId);

    Task<double> PredictFailureRiskAsync(int planId);

    Task<string> GetIoTStatusAsync(int machineId);
}
