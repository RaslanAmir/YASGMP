using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Wpf.Services
{
    /// <summary>
    /// Adapter-friendly abstraction around <see cref="YasGMP.Services.MachineService"/>
    /// so the WPF shell can be unit-tested without the full database infrastructure.
    /// </summary>
    public interface IMachineCrudService
    {
        Task<IReadOnlyList<Machine>> GetAllAsync();

        Task<Machine?> TryGetByIdAsync(int id);

        Task<int> CreateAsync(Machine machine, MachineCrudContext context);

        Task UpdateAsync(Machine machine, MachineCrudContext context);

        void Validate(Machine machine);

        string NormalizeStatus(string? status);
    }

    /// <summary>
    /// Ambient metadata required for audit logging when persisting machines.
    /// </summary>
    /// <param name="UserId">Authenticated user identifier.</param>
    /// <param name="Ip">Source IP captured by the auth context.</param>
    /// <param name="DeviceInfo">Device fingerprint (Workstation name, etc.).</param>
    /// <param name="SessionId">Logical session identifier.</param>
    public readonly record struct MachineCrudContext(int UserId, string Ip, string DeviceInfo, string? SessionId)
    {
        public static MachineCrudContext Create(int userId, string ip, string deviceInfo, string? sessionId)
            => new(userId <= 0 ? 1 : userId,
                   string.IsNullOrWhiteSpace(ip) ? "unknown" : ip,
                   string.IsNullOrWhiteSpace(deviceInfo) ? "WPF" : deviceInfo,
                   string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString("N") : sessionId);
    }
}
