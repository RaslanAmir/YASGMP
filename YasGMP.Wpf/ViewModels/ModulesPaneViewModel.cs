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
        foreach (var group in Groups)
        {
            group.Dispose();
        }

        Groups.Clear();

        foreach (var category in _moduleRegistry.Modules.GroupBy(m => m.Category))
        {
            var categoryKey = category.Key;
            var group = new ModuleGroupViewModel(
                categoryKey,
                $"ModuleTree.Category.{categoryKey}.Title",
                $"ModuleTree.Category.{categoryKey}.ToolTip",
                $"ModuleTree.Category.{categoryKey}.AutomationName",
                $"ModuleTree.Category.{categoryKey}.AutomationId",
                _localization);

            foreach (var metadata in category)
            {
                var nodePrefix = $"ModuleTree.Node.{categoryKey}.{metadata.Key}";
                group.Modules.Add(
                    new ModuleLinkViewModel(
                        metadata,
                        nodePrefix + ".Title",
                        nodePrefix + ".ToolTip",
                        nodePrefix + ".AutomationName",
                        nodePrefix + ".AutomationId",
                        _localization,
                        OpenModuleCommand));
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
        foreach (var group in Groups)
        {
            group.RefreshLocalization();
        }
    }
}

/// <summary>Grouping of modules by category.</summary>
public partial class ModuleGroupViewModel : ObservableObject, IDisposable
{
    private readonly string _categoryKey;
    private readonly string _titleKey;
    private readonly string _toolTipKey;
    private readonly string _automationNameKey;
    private readonly string _automationIdKey;
    private readonly ILocalizationService _localization;
    private string _header = string.Empty;
    private string _toolTip = string.Empty;
    private string _automationName = string.Empty;
    private string _automationId = string.Empty;

    /// <summary>
    /// Initializes a new instance of the ModuleGroupViewModel class.
    /// </summary>
    public ModuleGroupViewModel(
        string name,
        string titleKey,
        string toolTipKey,
        string automationNameKey,
        string automationIdKey,
        ILocalizationService localization)
    {
        _categoryKey = name;
        _titleKey = titleKey;
        _toolTipKey = toolTipKey;
        _automationNameKey = automationNameKey;
        _automationIdKey = automationIdKey;
        _localization = localization;
        Name = name;
        Modules = new ObservableCollection<ModuleLinkViewModel>();
        _localization.LanguageChanged += OnLanguageChanged;
        RefreshLocalization();
    }
    /// <summary>
    /// Gets or sets the name.
    /// </summary>

    public string Name { get; }
    /// <summary>
    /// Gets or sets the modules.
    /// </summary>

    public ObservableCollection<ModuleLinkViewModel> Modules { get; }

    public string Header
    {
        get => _header;
        private set => SetProperty(ref _header, value);
    }

    public string ToolTip
    {
        get => _toolTip;
        private set => SetProperty(ref _toolTip, value);
    }

    public string AutomationName
    {
        get => _automationName;
        private set => SetProperty(ref _automationName, value);
    }

    public string AutomationId
    {
        get => _automationId;
        private set => SetProperty(ref _automationId, value);
    }

    public void RefreshLocalization()
    {
        Header = ResolveString(_titleKey, _categoryKey);
        ToolTip = ResolveString(_toolTipKey, Header);
        AutomationName = ResolveString(_automationNameKey, Header);
        AutomationId = ResolveAutomationId();

        foreach (var module in Modules)
        {
            module.RefreshLocalization();
        }
    }

    public void Dispose()
    {
        _localization.LanguageChanged -= OnLanguageChanged;
        foreach (var module in Modules)
        {
            module.Dispose();
        }
    }

    private void OnLanguageChanged(object? sender, EventArgs e) => RefreshLocalization();

    private string ResolveString(string key, string fallback)
    {
        var localized = _localization.GetString(key);
        return string.Equals(localized, key, StringComparison.Ordinal) ? fallback : localized;
    }

    private string ResolveAutomationId()
    {
        var localized = _localization.GetString(_automationIdKey);
        return string.Equals(localized, _automationIdKey, StringComparison.Ordinal)
            ? $"ModuleTree.Category.{_categoryKey}"
            : localized;
    }
}

/// <summary>Represents a single module entry in the pane.</summary>
public partial class ModuleLinkViewModel : ObservableObject, IDisposable
{
    private readonly string _titleKey;
    private readonly string _toolTipKey;
    private readonly string _automationNameKey;
    private readonly string _automationIdKey;
    private readonly ILocalizationService _localization;
    private string _title = string.Empty;
    private string _toolTip = string.Empty;
    private string _automationName = string.Empty;
    private string _automationId = string.Empty;

    /// <summary>
    /// Initializes a new instance of the ModuleLinkViewModel class.
    /// </summary>
    public ModuleLinkViewModel(
        ModuleMetadata metadata,
        string titleKey,
        string toolTipKey,
        string automationNameKey,
        string automationIdKey,
        ILocalizationService localization,
        RelayCommand<ModuleLinkViewModel> openCommand)
    {
        Metadata = metadata;
        _titleKey = titleKey;
        _toolTipKey = toolTipKey;
        _automationNameKey = automationNameKey;
        _automationIdKey = automationIdKey;
        _localization = localization;
        OpenCommand = openCommand;
        _localization.LanguageChanged += OnLanguageChanged;
        RefreshLocalization();
    }
    /// <summary>
    /// Gets or sets the metadata.
    /// </summary>

    public ModuleMetadata Metadata { get; }
    /// <summary>
    /// Gets or sets the open command.
    /// </summary>

    public RelayCommand<ModuleLinkViewModel> OpenCommand { get; }

    public string Title
    {
        get => _title;
        private set => SetProperty(ref _title, value);
    }

    public string ToolTip
    {
        get => _toolTip;
        private set => SetProperty(ref _toolTip, value);
    }

    public string AutomationName
    {
        get => _automationName;
        private set => SetProperty(ref _automationName, value);
    }

    public string AutomationId
    {
        get => _automationId;
        private set => SetProperty(ref _automationId, value);
    }

    public void RefreshLocalization()
    {
        Title = ResolveString(_titleKey, Metadata.Title);
        ToolTip = ResolveString(_toolTipKey, Metadata.Description);
        AutomationName = ResolveString(_automationNameKey, Title);
        AutomationId = ResolveAutomationId();
    }

    public void Dispose() => _localization.LanguageChanged -= OnLanguageChanged;

    private void OnLanguageChanged(object? sender, EventArgs e) => RefreshLocalization();

    private string ResolveString(string key, string fallback)
    {
        var localized = _localization.GetString(key);
        return string.Equals(localized, key, StringComparison.Ordinal) ? fallback : localized;
    }

    private string ResolveAutomationId()
    {
        var localized = _localization.GetString(_automationIdKey);
        return string.Equals(localized, _automationIdKey, StringComparison.Ordinal)
            ? $"ModuleTree.Node.{Metadata.Category}.{Metadata.Key}"
            : localized;
    }
}
