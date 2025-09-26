using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Wpf.Services
{
    /// <summary>
    /// Adapter-friendly abstraction that exposes user and role CRUD functionality to the WPF shell
    /// without binding directly to <see cref="YasGMP.Services.UserService"/> or <see cref="YasGMP.Services.RBACService"/>.
    /// </summary>
    public interface IUserCrudService
    {
        Task<IReadOnlyList<User>> GetAllAsync();

        Task<User?> TryGetByIdAsync(int id);

        Task<IReadOnlyList<Role>> GetAllRolesAsync();

        Task<int> CreateAsync(User user, string password, UserCrudContext context);

        Task UpdateAsync(User user, string? password, UserCrudContext context);

        Task UpdateRoleAssignmentsAsync(int userId, IReadOnlyCollection<int> roleIds, UserCrudContext context);

        Task DeactivateAsync(int userId, UserCrudContext context);

        void Validate(User user);
    }

    /// <summary>
    /// Ambient metadata captured when persisting security changes so audit trails include the actor and origin details.
    /// </summary>
    /// <param name="UserId">Authenticated operator identifier.</param>
    /// <param name="Ip">Source IP address captured from the session.</param>
    /// <param name="DeviceInfo">Device or workstation fingerprint.</param>
    /// <param name="SessionId">Logical session identifier for traceability.</param>
    public readonly record struct UserCrudContext(int UserId, string Ip, string DeviceInfo, string? SessionId)
    {
        public static UserCrudContext Create(int userId, string? ip, string? deviceInfo, string? sessionId)
            => new(
                userId <= 0 ? 1 : userId,
                string.IsNullOrWhiteSpace(ip) ? "unknown" : ip!,
                string.IsNullOrWhiteSpace(deviceInfo) ? "WPF" : deviceInfo!,
                string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString("N") : sessionId);
    }
}
