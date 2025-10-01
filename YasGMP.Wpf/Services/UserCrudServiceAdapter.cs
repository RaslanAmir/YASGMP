using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YasGMP.AppCore.Models.Signatures;
using YasGMP.Models;
using YasGMP.Services.Interfaces;

namespace YasGMP.Wpf.Services
{
    /// <summary>
    /// Default <see cref="IUserCrudService"/> implementation that forwards calls to the shared
    /// <see cref="UserService"/> and <see cref="IRBACService"/> infrastructure while keeping the WPF shell
    /// decoupled for unit testing.
    /// </summary>
    public sealed class UserCrudServiceAdapter : IUserCrudService
    {
        private readonly IUserService _userService;
        private readonly IRBACService _rbacService;

        public UserCrudServiceAdapter(IUserService userService, IRBACService rbacService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _rbacService = rbacService ?? throw new ArgumentNullException(nameof(rbacService));
        }

        public async Task<IReadOnlyList<User>> GetAllAsync()
            => await _userService.GetAllUsersAsync().ConfigureAwait(false);

        public async Task<User?> TryGetByIdAsync(int id)
        {
            if (id <= 0) return null;
            try
            {
                return await _userService.GetUserByIdAsync(id).ConfigureAwait(false);
            }
            catch
            {
                return null;
            }
        }

        public async Task<IReadOnlyList<Role>> GetAllRolesAsync()
        {
            var roles = await _rbacService.GetAllRolesAsync().ConfigureAwait(false);
            return roles.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public async Task<CrudSaveResult> CreateAsync(User user, string password, UserCrudContext context)
        {
            if (user is null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("Password is required when creating a new user.");
            }

            user.PasswordHash = password;
            var signature = ApplyContext(user, context);
            await _userService.CreateUserAsync(user, context.UserId).ConfigureAwait(false);
            return new CrudSaveResult(user.Id, CreateMetadata(context, signature));
        }

        public async Task<CrudSaveResult> UpdateAsync(User user, string? password, UserCrudContext context)
        {
            if (user is null) throw new ArgumentNullException(nameof(user));

            if (!string.IsNullOrWhiteSpace(password))
            {
                user.PasswordHash = _userService.HashPassword(password);
            }

            var signature = ApplyContext(user, context);
            await _userService.UpdateUserAsync(user, context.UserId).ConfigureAwait(false);
            return new CrudSaveResult(user.Id, CreateMetadata(context, signature));
        }

        public async Task UpdateRoleAssignmentsAsync(int userId, IReadOnlyCollection<int> roleIds, UserCrudContext context)
        {
            var desired = new HashSet<int>(roleIds ?? Array.Empty<int>());
            var current = await _rbacService.GetRolesForUserAsync(userId).ConfigureAwait(false);
            var currentIds = current.Select(r => r.Id).ToHashSet();

            foreach (var toAdd in desired.Except(currentIds))
            {
                await _rbacService.GrantRoleAsync(userId, toAdd, context.UserId).ConfigureAwait(false);
            }

            foreach (var toRemove in currentIds.Except(desired))
            {
                await _rbacService.RevokeRoleAsync(userId, toRemove, context.UserId, "WPF shell update").ConfigureAwait(false);
            }
        }

        public Task DeactivateAsync(int userId, UserCrudContext context)
            => _userService.DeactivateUserAsync(userId);

        public void Validate(User user)
        {
            if (user is null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(user.Username))
            {
                throw new InvalidOperationException("Username is required.");
            }

            if (string.IsNullOrWhiteSpace(user.FullName))
            {
                throw new InvalidOperationException("Full name is required.");
            }

            if (string.IsNullOrWhiteSpace(user.Role))
            {
                throw new InvalidOperationException("Primary role is required.");
            }
        }

        private static string ApplyContext(User user, UserCrudContext context)
        {
            var signature = context.SignatureHash ?? user.DigitalSignature ?? string.Empty;
            user.DigitalSignature = signature;
            user.LastChangeSignature = signature;
            user.LastModifiedById = context.UserId;
            user.LastModified = DateTime.UtcNow;
            return signature;
        }

        private static SignatureMetadataDto CreateMetadata(UserCrudContext context, string signature)
            => new()
            {
                Id = context.SignatureId,
                Hash = string.IsNullOrWhiteSpace(signature) ? context.SignatureHash : signature,
                Method = context.SignatureMethod,
                Status = context.SignatureStatus,
                Note = context.SignatureNote,
                Session = context.SessionId,
                Device = context.DeviceInfo,
                IpAddress = context.Ip
            };
    }
}
