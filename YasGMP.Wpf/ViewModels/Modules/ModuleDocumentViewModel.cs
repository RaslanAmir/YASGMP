using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>Base class for YasGMP modules hosted inside the WPF shell document dock.</summary>
/// <remarks>
/// Form Modes: Relies on the SAP B1 Find/Add/View/Update cycle implemented by <see cref="B1FormDocumentViewModel"/> so derived modules only override CRUD hooks.
/// Audit &amp; Logging: Provides helper utilities (`ToReadOnlyList`, `InitializeAsync`) that downstream modules pair with their own audit trails while keeping `StatusMessage` updates aligned with shell expectations.
/// Localization: Defers to derived types for RESX-backed titles or inline strings; no resource keys are consumed directly at this layer.
/// Navigation: Exposes the `ModuleKey` inherited from the base constructor so shell navigation, Golden Arrow routing, and status strings remain consistent across modules.
/// </remarks>
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



