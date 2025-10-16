using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Adapter that exposes <see cref="YasGMP.Services.PreventiveMaintenancePlanService"/> through a
/// WPF-friendly interface so docked modules can execute the same plan lifecycle as MAUI.
/// </summary>
public sealed class PreventiveMaintenancePlanServiceAdapter : IPreventiveMaintenancePlanService
{
    private readonly PreventiveMaintenancePlanService _service;

    public PreventiveMaintenancePlanServiceAdapter(PreventiveMaintenancePlanService service)
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
    public Task DeleteAsync(int planId, int userId)
        => _service.DeleteAsync(planId, userId);

    /// <inheritdoc />
    public async Task<IReadOnlyList<PreventiveMaintenancePlan>> GetOverduePlansAsync()
        => await _service.GetOverduePlansAsync().ConfigureAwait(false);

    /// <inheritdoc />
    public Task MarkExecutedAsync(int planId, int userId)
        => _service.MarkExecutedAsync(planId, userId);

    /// <inheritdoc />
    public DateTime CalculateNextDue(PreventiveMaintenancePlan plan)
        => _service.CalculateNextDue(plan);
}
