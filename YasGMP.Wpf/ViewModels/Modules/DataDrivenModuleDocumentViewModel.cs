using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>Base class for modules backed by <see cref="DatabaseService"/> queries.</summary>
public abstract class DataDrivenModuleDocumentViewModel : ModuleDocumentViewModel
{
    protected DataDrivenModuleDocumentViewModel(
        string key,
        string title,
        DatabaseService databaseService,
        ILocalizationService localization,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation,
        AuditService? auditService = null)
        : base(key, title, localization, cflDialogService, shellInteraction, navigation)
    {
        Database = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        Audit = auditService;
    }

    protected DatabaseService Database { get; }

    /// <summary>
    /// Optional audit logging service shared by module implementations.
    /// </summary>
    protected AuditService? Audit { get; }

    protected async Task<IReadOnlyList<ModuleRecord>> ExecuteSafeAsync<T>(
        Func<DatabaseService, Task<IEnumerable<T>>> loader,
        Func<IEnumerable<T>, IEnumerable<ModuleRecord>> projector)
    {
        try
        {
            var data = await loader(Database).ConfigureAwait(false);
            return projector(data).ToList();
        }
        catch
        {
            throw;
        }
    }

    /// <summary>
    /// Executes an audit logging delegate while shielding the caller from exceptions.
    /// </summary>
    /// <param name="auditAction">Callback that performs the audit write against the shared <see cref="AuditService"/>.</param>
    /// <param name="failureStatusMessage">Optional status message applied when logging fails.</param>
    protected async Task LogAuditAsync(Func<AuditService, Task> auditAction, string? failureStatusMessage = null)
    {
        if (auditAction is null)
        {
            throw new ArgumentNullException(nameof(auditAction));
        }

        if (Audit is null)
        {
            return;
        }

        try
        {
            await auditAction(Audit).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            StatusMessage = failureStatusMessage ?? $"Audit logging failed: {ex.Message}";
        }
    }
}
