using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>Base class for domain-specific modules surfaced inside the WPF cockpit.</summary>
public abstract class ModuleDocumentViewModel : B1FormDocumentViewModel
{
    protected ModuleDocumentViewModel(
        string moduleKey,
        string title,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(moduleKey, title, cflDialogService, shellInteraction, navigation)
    {
    }

    /// <summary>Called after the module has been opened or reactivated.</summary>
    public new Task InitializeAsync(object? parameter = null)
        => base.InitializeAsync(parameter);

    /// <summary>
    /// Helper used by derived classes to convert a list into a read-only payload.
    /// </summary>
    protected static IReadOnlyList<ModuleRecord> ToReadOnlyList(IEnumerable<ModuleRecord> source)
        => source is IReadOnlyList<ModuleRecord> list ? list : source.ToList();
}
