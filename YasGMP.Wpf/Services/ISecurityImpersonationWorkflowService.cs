using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Coordinates impersonation workflow transitions triggered from the security module.
/// Provides access to candidate accounts and tracks whether an impersonation session is active.
/// </summary>
public interface ISecurityImpersonationWorkflowService
{
    /// <summary>Gets a value indicating whether the shell is currently impersonating another user.</summary>
    bool IsImpersonating { get; }

    /// <summary>Gets the identifier of the impersonated account when <see cref="IsImpersonating"/> is <c>true</c>.</summary>
    int? ImpersonatedUserId { get; }

    /// <summary>Retrieves the list of users eligible for impersonation.</summary>
    Task<IReadOnlyList<User>> GetImpersonationCandidatesAsync();

    /// <summary>Begins an impersonation session for the supplied user.</summary>
    Task BeginImpersonationAsync(int userId, string reason, string? notes);

    /// <summary>Ends the active impersonation session, if any.</summary>
    Task EndImpersonationAsync();
}
