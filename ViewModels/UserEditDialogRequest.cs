using System.Collections.Generic;
using YasGMP.Models;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// Payload passed to <see cref="IDialogService"/> when opening the MAUI user edit dialog.
    /// Carries the currently selected user along with lookup collections required to hydrate the
    /// <see cref="UserEditDialogViewModel"/> prior to rendering.
    /// </summary>
    public sealed class UserEditDialogRequest
    {
        /// <summary>Initializes a new instance of the <see cref="UserEditDialogRequest"/> class.</summary>
        /// <param name="user">Existing user to edit, or <c>null</c> when creating a new user.</param>
        /// <param name="roles">Available role definitions that can be assigned within the dialog.</param>
        /// <param name="impersonationCandidates">Users that may be impersonated from the dialog.</param>
        public UserEditDialogRequest(
            User? user,
            IEnumerable<Role>? roles,
            IEnumerable<User>? impersonationCandidates)
        {
            User = user;
            Roles = roles;
            ImpersonationCandidates = impersonationCandidates;
        }

        /// <summary>Gets the user being edited (if any).</summary>
        public User? User { get; }

        /// <summary>Gets the available role definitions.</summary>
        public IEnumerable<Role>? Roles { get; }

        /// <summary>Gets the impersonation candidates list.</summary>
        public IEnumerable<User>? ImpersonationCandidates { get; }
    }
}
