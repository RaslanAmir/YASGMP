using System;
using System.Collections.Generic;
using Xunit;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Tests;

public sealed class ModulesPaneViewModelTests : IDisposable
{
    private readonly LocalizationService _localization = new();
    private readonly string _originalLanguage;

    public ModulesPaneViewModelTests()
    {
        _originalLanguage = _localization.CurrentLanguage;
        _localization.SetLanguage("en");
    }

    [Fact]
    public void BuildGroups_ComposesLocalizedAutomationMetadata()
    {
        var viewModel = CreateViewModel();

        Assert.Equal(_localization.GetString("Dock.Modules.ToolTip"), viewModel.HelpText);

        var group = Assert.Single(viewModel.Groups);
        Assert.Equal(_localization.GetString("ModuleTree.Category.Cockpit.ToolTip"), group.ToolTip);
        Assert.Equal(_localization.GetString("ModuleTree.Category.Cockpit.AutomationName"), group.AutomationName);
        Assert.Equal(_localization.GetString("ModuleTree.Category.Cockpit.AutomationId"), group.AutomationId);

        var module = Assert.Single(group.Modules);
        Assert.Equal(_localization.GetString("ModuleTree.Node.Cockpit.Dashboard.ToolTip"), module.ToolTip);
        Assert.Equal(_localization.GetString("ModuleTree.Node.Cockpit.Dashboard.AutomationName"), module.AutomationName);
        Assert.Equal(_localization.GetString("ModuleTree.Node.Cockpit.Dashboard.AutomationId"), module.AutomationId);
    }

    [Fact]
    public void LanguageChange_RefreshesAutomationMetadata()
    {
        var viewModel = CreateViewModel();

        _localization.SetLanguage("hr");

        Assert.Equal(_localization.GetString("Dock.Modules.ToolTip"), viewModel.HelpText);

        var group = Assert.Single(viewModel.Groups);
        Assert.Equal(_localization.GetString("ModuleTree.Category.Cockpit.ToolTip"), group.ToolTip);
        Assert.Equal(_localization.GetString("ModuleTree.Category.Cockpit.AutomationName"), group.AutomationName);
        Assert.Equal(_localization.GetString("ModuleTree.Category.Cockpit.AutomationId"), group.AutomationId);

        var module = Assert.Single(group.Modules);
        Assert.Equal(_localization.GetString("ModuleTree.Node.Cockpit.Dashboard.ToolTip"), module.ToolTip);
        Assert.Equal(_localization.GetString("ModuleTree.Node.Cockpit.Dashboard.AutomationName"), module.AutomationName);
        Assert.Equal(_localization.GetString("ModuleTree.Node.Cockpit.Dashboard.AutomationId"), module.AutomationId);
    }

    public void Dispose() => _localization.SetLanguage(_originalLanguage);

    private ModulesPaneViewModel CreateViewModel()
    {
        var metadata = new ModuleMetadata(
            key: "Dashboard",
            title: "Dashboard",
            category: "Cockpit",
            description: "Operations overview and KPIs.");

        var registry = new StubModuleRegistry(metadata);
        var navigation = new StubNavigationService();

        return new ModulesPaneViewModel(registry, navigation, _localization);
    }

    private sealed class StubModuleRegistry : IModuleRegistry
    {
        private readonly ModuleMetadata[] _modules;

        public StubModuleRegistry(params ModuleMetadata[] modules)
        {
            _modules = modules;
        }

        public IReadOnlyList<ModuleMetadata> Modules => _modules;

        public ModuleDocumentViewModel CreateModule(string moduleKey)
            => throw new NotSupportedException();
    }

    private sealed class StubNavigationService : IModuleNavigationService
    {
        public ModuleDocumentViewModel OpenModule(string moduleKey, object? parameter = null)
            => throw new NotSupportedException();

        public void Activate(ModuleDocumentViewModel document)
        {
            throw new NotSupportedException();
        }
    }
}
