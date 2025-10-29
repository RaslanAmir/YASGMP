using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;

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
    private readonly IUserService _userService;
    private readonly IAuthContext _authContext;
    private ImpersonationContext? _activeContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityImpersonationWorkflowService"/> class.
    /// </summary>
    public SecurityImpersonationWorkflowService(
        IUserCrudService userService,
        IUserService coreUserService,
        IAuthContext authContext)
    {
        _userCrudService = userService ?? throw new ArgumentNullException(nameof(userService));
        _userService = coreUserService ?? throw new ArgumentNullException(nameof(coreUserService));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
    }

    /// <inheritdoc />
    public bool IsImpersonating => _activeContext.HasValue;

    /// <inheritdoc />
    public int? ImpersonatedUserId => _activeContext?.TargetUserId;

    /// <inheritdoc />
    public Task<IReadOnlyList<User>> GetImpersonationCandidatesAsync()
        => _userCrudService.GetAllAsync();

    /// <inheritdoc />
    public async Task BeginImpersonationAsync(int userId, string reason, string? notes)
    {
        if (IsImpersonating)
        {
            throw new InvalidOperationException("An impersonation session is already active.");
        }

        var actorId = _authContext.CurrentUser?.Id ?? 0;
        var context = UserCrudContext.Create(
            actorId,
            _authContext.CurrentIpAddress,
            _authContext.CurrentDeviceInfo,
            _authContext.CurrentSessionId,
            reason,
            notes);

        var impersonation = await _userService
            .BeginImpersonationAsync(userId, context)
            .ConfigureAwait(false);

        if (impersonation is null)
        {
            throw new InvalidOperationException("Impersonation request was denied by the server.");
        }

        _activeContext = impersonation;
    }

    /// <inheritdoc />
    public async Task EndImpersonationAsync()
    {
        if (!_activeContext.HasValue)
        {
            return;
        }

        var actorId = _authContext.CurrentUser?.Id ?? 0;
        var auditContext = UserCrudContext.Create(
            actorId,
            _authContext.CurrentIpAddress,
            _authContext.CurrentDeviceInfo,
            _authContext.CurrentSessionId,
            _activeContext.Value.Reason,
            _activeContext.Value.Notes);

        await _userService.EndImpersonationAsync(_activeContext.Value, auditContext).ConfigureAwait(false);
        _activeContext = null;
    }
}
