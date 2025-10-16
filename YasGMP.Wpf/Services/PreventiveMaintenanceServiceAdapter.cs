using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Desktop adapter around <see cref="YasGMP.Services.PreventiveMaintenanceService"/> so WPF modules
/// invoke the shared preventive maintenance logic without depending on MAUI infrastructure.
/// </summary>
public sealed class PreventiveMaintenanceServiceAdapter : IPreventiveMaintenanceService
{
    private readonly PreventiveMaintenanceService _service;

    public PreventiveMaintenanceServiceAdapter(PreventiveMaintenanceService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PreventiveMaintenancePlan>> GetAllAsync()
        => await _service.GetAllAsync().ConfigureAwait(false);

    /// <inheritdoc />
    public Task<PreventiveMaintenancePlan> GetByIdAsync(int id)
        => _service.GetByIdAsync(id);

    /// <inheritdoc />
    public Task CreateAsync(PreventiveMaintenancePlan plan, int userId)
        => _service.CreateAsync(plan, userId);

    /// <inheritdoc />
    public Task UpdateAsync(PreventiveMaintenancePlan plan, int userId)
        => _service.UpdateAsync(plan, userId);

    /// <inheritdoc />
    public Task DeleteAsync(int ppmId, int userId)
        => _service.DeleteAsync(ppmId, userId);

    /// <inheritdoc />
    public async Task<IReadOnlyList<PreventiveMaintenancePlan>> GetOverduePlansAsync()
        => await _service.GetOverduePlansAsync().ConfigureAwait(false);

    /// <inheritdoc />
    public Task MarkExecutedAsync(int ppmId, int userId)
        => _service.MarkExecutedAsync(ppmId, userId);

    /// <inheritdoc />
    public Task<double> PredictFailureRiskAsync(int planId)
        => _service.PredictFailureRiskAsync(planId);

    /// <inheritdoc />
    public Task<string> GetIoTStatusAsync(int machineId)
        => _service.GetIoTStatusAsync(machineId);
}
