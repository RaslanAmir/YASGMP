using System;
using System.Threading.Tasks;
using YasGMP.Services;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Tests.TestDoubles;

public sealed class StubCflDialogService : ICflDialogService
{
    public Task<CflResult?> ShowAsync(CflRequest request) => Task.FromResult<CflResult?>(null);
}

public sealed class StubShellInteractionService : IShellInteractionService
{
    public InspectorContext? LastContext { get; private set; }
    public string? LastStatus { get; private set; }

    public void UpdateInspector(InspectorContext context) => LastContext = context;

    public void UpdateStatus(string message) => LastStatus = message;
}

public sealed class StubModuleNavigationService : IModuleNavigationService
{
    public void Activate(ModuleDocumentViewModel document) { }
    public ModuleDocumentViewModel OpenModule(string moduleKey, object? parameter = null)
        => throw new NotSupportedException();
}
