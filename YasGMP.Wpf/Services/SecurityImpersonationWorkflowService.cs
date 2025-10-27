using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Basic in-memory implementation of <see cref="ISecurityImpersonationWorkflowService"/> that records
/// the active impersonation session and surfaces candidate accounts through <see cref="IUserCrudService"/>.
/// The workflow is intentionally lightweight; downstream audit and persistence hooks can decorate this
/// service when full impersonation support arrives.
/// </summary>
public sealed class SecurityImpersonationWorkflowService : ISecurityImpersonationWorkflowService
{
    private readonly IUserCrudService _userService;
    private int? _impersonatedUserId;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityImpersonationWorkflowService"/> class.
    /// </summary>
    public SecurityImpersonationWorkflowService(IUserCrudService userService)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
    }

    /// <inheritdoc />
    public bool IsImpersonating => _impersonatedUserId.HasValue;

    /// <inheritdoc />
    public int? ImpersonatedUserId => _impersonatedUserId;

    /// <inheritdoc />
    public Task<IReadOnlyList<User>> GetImpersonationCandidatesAsync()
        => _userService.GetAllAsync();

    /// <inheritdoc />
    public Task BeginImpersonationAsync(int userId, string reason, string? notes)
    {
        _impersonatedUserId = userId;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task EndImpersonationAsync()
    {
        _impersonatedUserId = null;
        return Task.CompletedTask;
    }
}
