using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Basic in-memory implementation of <see cref="ISecurityImpersonationWorkflowService"/> that records
/// the active impersonation session and surfaces candidate accounts through <see cref="IUserCrudService"/>.
/// The workflow is intentionally lightweight; downstream audit and persistence hooks can decorate this
/// service when full impersonation support arrives.
/// </summary>
public sealed class SecurityImpersonationWorkflowService : ISecurityImpersonationWorkflowService
{
    private readonly IUserCrudService _userCrudService;
    private ImpersonationContext? _activeContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityImpersonationWorkflowService"/> class.
    /// </summary>
    public SecurityImpersonationWorkflowService(
        IUserCrudService userService)
    {
        _userCrudService = userService ?? throw new ArgumentNullException(nameof(userService));
    }

    /// <inheritdoc />
    public bool IsImpersonating => _activeContext.HasValue;

    /// <inheritdoc />
    public int? ImpersonatedUserId => _activeContext?.TargetUserId;

    /// <inheritdoc />
    public ImpersonationContext? ActiveContext => _activeContext;

    /// <inheritdoc />
    public Task<IReadOnlyList<User>> GetImpersonationCandidatesAsync()
        => _userCrudService.GetAllAsync();

    /// <inheritdoc />
    public async Task<ImpersonationContext> BeginImpersonationAsync(int userId, UserCrudContext context)
    {
        if (IsImpersonating)
        {
            throw new InvalidOperationException("An impersonation session is already active.");
        }

        var impersonation = await _userCrudService
            .BeginImpersonationAsync(userId, context)
            .ConfigureAwait(false);

        if (impersonation is null)
        {
            throw new InvalidOperationException("Impersonation request was denied by the server.");
        }

        _activeContext = impersonation;
        return impersonation;
    }

    /// <inheritdoc />
    public async Task EndImpersonationAsync(UserCrudContext auditContext)
    {
        if (!_activeContext.HasValue)
        {
            return;
        }

        await _userCrudService.EndImpersonationAsync(_activeContext.Value, auditContext).ConfigureAwait(false);
        _activeContext = null;
    }
}
