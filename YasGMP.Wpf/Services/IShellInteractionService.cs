using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Services;

/// <summary>Exposes status/inspector updates for docked module documents.</summary>
public interface IShellInteractionService
{
    void UpdateStatus(string message);

    void UpdateInspector(InspectorContext context);

    // Navigation helpers exposed for convenience (also on IModuleNavigationService)
    ModuleDocumentViewModel OpenModule(string moduleKey, object? parameter = null);

    void Activate(ModuleDocumentViewModel document);
}

/// <summary>Navigation contract used by golden-arrow buttons to activate related modules.</summary>
public interface IModuleNavigationService
{
    ModuleDocumentViewModel OpenModule(string moduleKey, object? parameter = null);

    void Activate(ModuleDocumentViewModel document);
}
