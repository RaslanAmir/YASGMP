using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YasGMP.AppCore.Models.Signatures;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;

namespace YasGMP.Wpf.Services
{
    /// <summary>
    /// Routes WPF user-management workflows through the shared MAUI services
    /// <see cref="YasGMP.Services.Interfaces.IUserService"/> and <see cref="YasGMP.Services.Interfaces.IRBACService"/>.
    /// </summary>
    /// <remarks>
    /// Module view models dispatch CRUD/role operations to this adapter, which forwards them to the shared user and RBAC
    /// services so behavior matches the MAUI shell (including downstream auditing via <see cref="YasGMP.Services.AuditService"/>).
    /// Await calls off the dispatcher thread and use <see cref="WpfUiDispatcher"/> for UI updates. Returned
    /// <see cref="CrudSaveResult"/> instances must include identifiers, signature context, and localization-ready status/notes
    /// for processing via <see cref="LocalizationServiceExtensions"/> or <see cref="ILocalizationService"/> before surfacing.
    /// </remarks>
    public sealed class UserCrudServiceAdapter : IUserCrudService
    {
        private readonly IUserService _userService;
        private readonly IRBACService _rbacService;
        /// <summary>
        /// Initializes a new instance of the UserCrudServiceAdapter class.
        /// </summary>

        public UserCrudServiceAdapter(IUserService userService, IRBACService rbacService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _rbacService = rbacService ?? throw new ArgumentNullException(nameof(rbacService));
        }
        /// <summary>
        /// Executes the get all async operation.
        /// </summary>

        public async Task<IReadOnlyList<User>> GetAllAsync()
            => await _userService.GetAllUsersAsync().ConfigureAwait(false);
        /// <summary>
        /// Executes the try get by id async operation.
        /// </summary>

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
        /// <summary>
        /// Executes the get all roles async operation.
        /// </summary>

        public async Task<IReadOnlyList<Role>> GetAllRolesAsync()
        {
            var roles = await _rbacService.GetAllRolesAsync().ConfigureAwait(false);
            return roles.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }
        /// <summary>
        /// Executes the create async operation.
        /// </summary>

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
        /// <summary>
        /// Executes the update async operation.
        /// </summary>

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
        /// <summary>
        /// Executes the update role assignments async operation.
        /// </summary>

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
        /// <summary>
        /// Executes the deactivate async operation.
        /// </summary>

        public Task DeactivateAsync(int userId, UserCrudContext context)
            => _userService.DeactivateUserAsync(userId);

        /// <summary>
        /// Begins an impersonation session for the specified user.
        /// </summary>
        public Task<ImpersonationContext?> BeginImpersonationAsync(int targetUserId, UserCrudContext context)
            => _userService.BeginImpersonationAsync(targetUserId, context);

        /// <summary>
        /// Ends an impersonation session using the supplied audit context.
        /// </summary>
        public Task EndImpersonationAsync(ImpersonationContext context, UserCrudContext auditContext)
            => _userService.EndImpersonationAsync(context, auditContext);
        /// <summary>
        /// Executes the validate operation.
        /// </summary>

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
            user.SourceIp = context.Ip;
            user.DeviceInfo = context.DeviceInfo;
            user.SessionId = context.SessionId ?? user.SessionId;
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
