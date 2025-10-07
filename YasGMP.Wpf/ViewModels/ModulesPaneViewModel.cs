using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.ViewModels;

/// <summary>Anchorable view-model that lists available cockpit modules.</summary>
public partial class ModulesPaneViewModel : AnchorableViewModel
{
    private readonly IModuleRegistry _moduleRegistry;
    private readonly IModuleNavigationService _navigationService;

    private readonly ILocalizationService _localization;
    /// <summary>
    /// Initializes a new instance of the ModulesPaneViewModel class.
    /// </summary>

    public ModulesPaneViewModel(
        IModuleRegistry moduleRegistry,
        IModuleNavigationService navigationService,
        ILocalizationService localization)
    {
        _moduleRegistry = moduleRegistry;
        _navigationService = navigationService;
        _localization = localization;
        Title = _localization.GetString("Dock.Modules.Title");
        AutomationId = _localization.GetString("Dock.Modules.AutomationId");
        _localization.LanguageChanged += OnLanguageChanged;
        ContentId = "YasGmp.Shell.Modules";
        Groups = new ObservableCollection<ModuleGroupViewModel>();
        OpenModuleCommand = new RelayCommand<ModuleLinkViewModel>(OpenModule);
        BuildGroups();
    }

    /// <summary>Top-level module groupings.</summary>
    public ObservableCollection<ModuleGroupViewModel> Groups { get; }

    /// <summary>Command triggered when the user double clicks a module entry.</summary>
    public RelayCommand<ModuleLinkViewModel> OpenModuleCommand { get; }

    private void BuildGroups()
    {
        Groups.Clear();
        foreach (var category in _moduleRegistry.Modules.GroupBy(m => m.Category))
        {
            var group = new ModuleGroupViewModel(category.Key);
            foreach (var metadata in category)
            {
                group.Modules.Add(new ModuleLinkViewModel(metadata, OpenModuleCommand));
            }

            Groups.Add(group);
        }
    }

    private void OpenModule(ModuleLinkViewModel? link)
    {
        if (link is null)
        {
            return;
        }

        var document = _navigationService.OpenModule(link.Metadata.Key);
        _navigationService.Activate(document);
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        Title = _localization.GetString("Dock.Modules.Title");
        AutomationId = _localization.GetString("Dock.Modules.AutomationId");
    }
}

/// <summary>Grouping of modules by category.</summary>
public partial class ModuleGroupViewModel : ObservableObject
{
    /// <summary>
    /// Initializes a new instance of the ModuleGroupViewModel class.
    /// </summary>
    public ModuleGroupViewModel(string name)
    {
        Name = name;
        Modules = new ObservableCollection<ModuleLinkViewModel>();
    }
    /// <summary>
    /// Gets or sets the name.
    /// </summary>

    public string Name { get; }
    /// <summary>
    /// Gets or sets the modules.
    /// </summary>

    public ObservableCollection<ModuleLinkViewModel> Modules { get; }
}

/// <summary>Represents a single module entry in the pane.</summary>
public partial class ModuleLinkViewModel : ObservableObject
{
    /// <summary>
    /// Initializes a new instance of the ModuleLinkViewModel class.
    /// </summary>
    public ModuleLinkViewModel(ModuleMetadata metadata, RelayCommand<ModuleLinkViewModel> openCommand)
    {
        Metadata = metadata;
        OpenCommand = openCommand;
    }
    /// <summary>
    /// Gets or sets the metadata.
    /// </summary>

    public ModuleMetadata Metadata { get; }
    /// <summary>
    /// Gets or sets the open command.
    /// </summary>

    public RelayCommand<ModuleLinkViewModel> OpenCommand { get; }
}
