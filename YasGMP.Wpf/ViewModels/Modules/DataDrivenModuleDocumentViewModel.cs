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
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(key, title, cflDialogService, shellInteraction, navigation)
    {
        Database = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
    }

    protected DatabaseService Database { get; }

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
}
