using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Abstraction that lets the WPF shell reuse the AppCore preventive maintenance plan service
/// while keeping dependencies mockable for view-model tests.
/// </summary>
public interface IPreventiveMaintenancePlanService
{
    Task<IReadOnlyList<PreventiveMaintenancePlan>> GetAllAsync();

    Task<PreventiveMaintenancePlan> GetByIdAsync(int id);

    Task CreateAsync(PreventiveMaintenancePlan plan, int userId);

    Task UpdateAsync(PreventiveMaintenancePlan plan, int userId);

    Task DeleteAsync(int planId, int userId);

    Task<IReadOnlyList<PreventiveMaintenancePlan>> GetOverduePlansAsync();

    Task MarkExecutedAsync(int planId, int userId);

    DateTime CalculateNextDue(PreventiveMaintenancePlan plan);
}
