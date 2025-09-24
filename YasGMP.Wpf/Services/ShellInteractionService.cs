using System;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Services;

/// <summary>Bridges module documents with the shell window.</summary>
public sealed class ShellInteractionService : IShellInteractionService, IModuleNavigationService
{
    private Func<string, object?, ModuleDocumentViewModel>? _openModule;
    private Action<ModuleDocumentViewModel>? _activate;
    private Action<string>? _statusUpdater;
    private Action<InspectorContext>? _inspectorUpdater;

    public void Configure(
        Func<string, object?, ModuleDocumentViewModel> openModule,
        Action<ModuleDocumentViewModel> activate,
        Action<string> statusUpdater,
        Action<InspectorContext> inspectorUpdater)
    {
        _openModule = openModule ?? throw new ArgumentNullException(nameof(openModule));
        _activate = activate ?? throw new ArgumentNullException(nameof(activate));
        _statusUpdater = statusUpdater ?? throw new ArgumentNullException(nameof(statusUpdater));
        _inspectorUpdater = inspectorUpdater ?? throw new ArgumentNullException(nameof(inspectorUpdater));
    }

    public ModuleDocumentViewModel OpenModule(string moduleKey, object? parameter = null)
    {
        if (_openModule == null)
        {
            throw new InvalidOperationException("Shell interaction service not configured.");
        }

        var document = _openModule(moduleKey, parameter);
        return document;
    }

    public void Activate(ModuleDocumentViewModel document)
    {
        if (_activate == null)
        {
            throw new InvalidOperationException("Shell interaction service not configured.");
        }

        _activate(document);
    }

    public void UpdateStatus(string message)
    {
        if (_statusUpdater == null)
        {
            return;
        }

        _statusUpdater(message);
    }

    public void UpdateInspector(InspectorContext context)
    {
        if (_inspectorUpdater == null)
        {
            return;
        }

        _inspectorUpdater(context);
    }
}
