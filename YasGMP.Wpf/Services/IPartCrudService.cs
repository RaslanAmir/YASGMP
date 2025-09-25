using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Wpf.Services
{
    /// <summary>
    /// Adapter abstraction that surfaces Parts CRUD operations to the WPF shell
    /// without binding the UI to the concrete infrastructure implementation.
    /// </summary>
    public interface IPartCrudService
    {
        Task<IReadOnlyList<Part>> GetAllAsync();

        Task<Part?> TryGetByIdAsync(int id);

        Task<int> CreateAsync(Part part, PartCrudContext context);

        Task UpdateAsync(Part part, PartCrudContext context);

        void Validate(Part part);

        string NormalizeStatus(string? status);
    }

    /// <summary>
    /// Metadata required when persisting parts for audit purposes.
    /// </summary>
    /// <param name="UserId">Authenticated operator identifier.</param>
    /// <param name="Ip">Source IP captured by the auth context.</param>
    /// <param name="DeviceInfo">Device fingerprint or hostname.</param>
    /// <param name="SessionId">Logical session identifier.</param>
    public readonly record struct PartCrudContext(int UserId, string Ip, string DeviceInfo, string? SessionId)
    {
        public static PartCrudContext Create(int userId, string ip, string deviceInfo, string? sessionId)
            => new(userId <= 0 ? 1 : userId,
                   string.IsNullOrWhiteSpace(ip) ? "unknown" : ip,
                   string.IsNullOrWhiteSpace(deviceInfo) ? "WPF" : deviceInfo,
                   string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString("N") : sessionId);
    }
}
