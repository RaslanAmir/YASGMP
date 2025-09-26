using System;
using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Abstraction over the <see cref="YasGMP.Services.WorkOrderService"/> so the WPF shell
/// can execute CRUD operations in a testable manner without connecting to the full
/// database runtime.
/// </summary>
public interface IWorkOrderCrudService
{
    Task<WorkOrder?> TryGetByIdAsync(int id);

    Task<int> CreateAsync(WorkOrder workOrder, WorkOrderCrudContext context);

    Task UpdateAsync(WorkOrder workOrder, WorkOrderCrudContext context);

    void Validate(WorkOrder workOrder);
}

/// <summary>
/// Context metadata captured when persisting work-order edits so audit trails receive
/// consistent identifiers.
/// </summary>
/// <param name="UserId">Authenticated user identifier.</param>
/// <param name="Ip">Source IP captured from the current session.</param>
/// <param name="DeviceInfo">Machine fingerprint or hostname.</param>
/// <param name="SessionId">Logical application session identifier.</param>
public readonly record struct WorkOrderCrudContext(int UserId, string Ip, string DeviceInfo, string? SessionId)
{
    public static WorkOrderCrudContext Create(int userId, string ip, string deviceInfo, string? sessionId)
        => new(
            userId <= 0 ? 1 : userId,
            string.IsNullOrWhiteSpace(ip) ? "unknown" : ip,
            string.IsNullOrWhiteSpace(deviceInfo) ? "WPF" : deviceInfo,
            string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString("N") : sessionId);
}
