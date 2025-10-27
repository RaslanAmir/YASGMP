using System;
using System.Collections.Generic;
using YasGMP.Models;

namespace YasGMP.Services
{
    /// <summary>
    /// Shared payload passed to <see cref="IDialogService"/> implementations when opening the
    /// user editor dialog from either shell.
    /// </summary>
    public sealed class UserEditDialogRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserEditDialogRequest"/> class.
        /// </summary>
        /// <param name="mode">Dialog mode requested by the caller.</param>
        /// <param name="user">Existing user to edit or <c>null</c> for create operations.</param>
        /// <param name="roles">Available role definitions used to populate the editor.</param>
        /// <param name="impersonationCandidates">Users that may be impersonated from the dialog.</param>
        public UserEditDialogRequest(
            UserEditDialogMode mode,
            User? user,
            IEnumerable<Role>? roles = null,
            IEnumerable<User>? impersonationCandidates = null)
        {
            Mode = mode;
            User = user;
            Roles = roles;
            ImpersonationCandidates = impersonationCandidates;
        }

        /// <summary>Gets the dialog mode requested by the caller.</summary>
        public UserEditDialogMode Mode { get; }

        /// <summary>Gets the user being edited, if any.</summary>
        public User? User { get; }

        /// <summary>Gets the available role definitions.</summary>
        public IEnumerable<Role>? Roles { get; }

        /// <summary>Gets the impersonation candidates surfaced to the dialog.</summary>
        public IEnumerable<User>? ImpersonationCandidates { get; }
    }

    /// <summary>
    /// High-level mode flags surfaced to both shells when invoking the user editor dialog.
    /// Mirrors SAP Business One form modes so MAUI and WPF code share a single request contract.
    /// </summary>
    public enum UserEditDialogMode
    {
        /// <summary>Query/filter mode – typically unused when launching the dialog.</summary>
        Find,

        /// <summary>Allows inserting a new user.</summary>
        Add,

        /// <summary>Read-only preview of an existing user.</summary>
        View,

        /// <summary>Editing an existing user.</summary>
        Update,
    }
}
