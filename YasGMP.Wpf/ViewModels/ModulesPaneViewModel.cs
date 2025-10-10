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

    public ModulesPaneViewModel(IModuleRegistry moduleRegistry, IModuleNavigationService navigationService)
    {
        _moduleRegistry = moduleRegistry;
        _navigationService = navigationService;
        Title = TryGetString("Label_Modules", "Modules");
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

    private static string TryGetString(string key, string fallback)
    {
        try
        {
            var app = System.Windows.Application.Current;
            if (app?.Resources.Contains(key) == true)
            {
                var value = app.Resources[key] as string;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value!;
                }
            }
        }
        catch
        {
            // ignore and use fallback
        }

        return fallback;
    }
}

/// <summary>Grouping of modules by category.</summary>
public partial class ModuleGroupViewModel : ObservableObject
{
    public ModuleGroupViewModel(string name)
    {
        Name = name;
        Modules = new ObservableCollection<ModuleLinkViewModel>();
    }

    public string Name { get; }

    public ObservableCollection<ModuleLinkViewModel> Modules { get; }
}

/// <summary>Represents a single module entry in the pane.</summary>
public partial class ModuleLinkViewModel : ObservableObject
{
    public ModuleLinkViewModel(ModuleMetadata metadata, RelayCommand<ModuleLinkViewModel> openCommand)
    {
        Metadata = metadata;
        OpenCommand = openCommand;
    }

    public ModuleMetadata Metadata { get; }

    public RelayCommand<ModuleLinkViewModel> OpenCommand { get; }
}

