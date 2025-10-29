using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.AppCore.Models.Signatures;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Services
{
    /// <summary>
    /// Shared contract that orchestrates user and role workflows through
    /// <see cref="YasGMP.Services.Interfaces.IUserService"/> and <see cref="YasGMP.Services.Interfaces.IRBACService"/> so MAUI and WPF share behavior.
    /// </summary>
    /// <remarks>
    /// Module view models call these members on the dispatcher thread. Implementations forward work to the shared MAUI
    /// services (<see cref="YasGMP.Services.Interfaces.IUserService"/>, <see cref="YasGMP.Services.Interfaces.IRBACService"/>, and the downstream
    /// <see cref="YasGMP.Services.AuditService"/>) while callers marshal UI updates via <see cref="WpfUiDispatcher"/> after awaiting the tasks.
    /// Returned <see cref="CrudSaveResult"/> objects must include identifiers, signature context, and localization-ready status/notes so they can be translated with
    /// <see cref="LocalizationServiceExtensions"/> or <see cref="ILocalizationService"/> and maintain parity with the MAUI shell.
    /// </remarks>
    public interface IUserCrudService
    {
        Task<IReadOnlyList<User>> GetAllAsync();

        Task<User?> TryGetByIdAsync(int id);

        Task<IReadOnlyList<Role>> GetAllRolesAsync();

        /// <summary>
        /// Persists a new user and returns the saved identifier with signature metadata.
        /// </summary>
        Task<CrudSaveResult> CreateAsync(User user, string password, UserCrudContext context);

        /// <summary>
        /// Updates an existing user and returns the signature metadata captured during persistence.
        /// </summary>
        Task<CrudSaveResult> UpdateAsync(User user, string? password, UserCrudContext context);

        Task UpdateRoleAssignmentsAsync(int userId, IReadOnlyCollection<int> roleIds, UserCrudContext context);

        Task DeactivateAsync(int userId, UserCrudContext context);

        Task<ImpersonationContext?> BeginImpersonationAsync(int targetUserId, UserCrudContext context);

        Task EndImpersonationAsync(ImpersonationContext context, UserCrudContext auditContext);

        void Validate(User user);
    }

}
