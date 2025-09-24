using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.ViewModels;

/// <summary>
/// Root view-model that orchestrates the docked workspace.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly IModuleRegistry _moduleRegistry;
    private readonly ShellInteractionService _shellInteraction;
    private string _statusText = "Ready";

    [ObservableProperty]
    private DocumentViewModel? _activeDocument;

    public MainWindowViewModel(
        IModuleRegistry moduleRegistry,
        ModulesPaneViewModel modulesPane,
        InspectorPaneViewModel inspectorPane,
        ShellStatusBarViewModel statusBar,
        ShellInteractionService shellInteraction)
    {
        _moduleRegistry = moduleRegistry;
        _shellInteraction = shellInteraction;
        ModulesPane = modulesPane;
        InspectorPane = inspectorPane;
        StatusBar = statusBar;
        Documents = new ObservableCollection<DocumentViewModel>();
        WindowCommands = new WindowMenuViewModel(this);

        _shellInteraction.Configure(OpenModuleInternal, ActivateInternal, UpdateStatusInternal, InspectorPane.Update);
        StatusBar.StatusText = _statusText;
    }

    /// <summary>Left-hand navigation pane listing modules.</summary>
    public ModulesPaneViewModel ModulesPane { get; }

    /// <summary>Inspector pane rendered along the right side of the shell.</summary>
    public InspectorPaneViewModel InspectorPane { get; }

    /// <summary>Status bar data context.</summary>
    public ShellStatusBarViewModel StatusBar { get; }

    /// <summary>Collection of open docked documents.</summary>
    public ObservableCollection<DocumentViewModel> Documents { get; }

    /// <summary>Command surface for Window menu/backstage.</summary>
    public WindowMenuViewModel WindowCommands { get; }

    /// <summary>Gets or sets the status text exposed for legacy bindings.</summary>
    public string StatusText
    {
        get => _statusText;
        set
        {
            if (_statusText != value)
            {
                _statusText = value;
                StatusBar.StatusText = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Initialises the workspace with the default dashboard document.</summary>
    public void InitializeWorkspace()
    {
        if (!Documents.OfType<ModuleDocumentViewModel>().Any())
        {
            OpenModule(DashboardModuleViewModel.ModuleKey);
        }
    }

    /// <summary>Opens a module by key and activates it.</summary>
    public ModuleDocumentViewModel OpenModule(string moduleKey, object? parameter = null)
    {
        var document = OpenModuleInternal(moduleKey, parameter);
        ActivateInternal(document);
        return document;
    }

    /// <summary>Ensures a document exists for a persisted content id.</summary>
    public DocumentViewModel EnsureDocumentForId(string contentId)
    {
        var existing = Documents.FirstOrDefault(d => d.ContentId == contentId);
        if (existing != null)
        {
            return existing;
        }

        var moduleKey = TryParseModuleKey(contentId) ?? DashboardModuleViewModel.ModuleKey;
        var document = CreateModuleInstance(moduleKey, contentId);
        Documents.Add(document);
        _ = document.InitializeAsync(null);
        return document;
    }

    /// <summary>Prepares state for layout deserialization.</summary>
    public void PrepareForLayoutImport()
    {
        Documents.Clear();
        ActiveDocument = null;
        StatusBar.ActiveModule = string.Empty;
    }

    /// <summary>Resets the workspace back to a single dashboard module.</summary>
    public void ResetWorkspace()
    {
        Documents.Clear();
        ActiveDocument = null;
        StatusBar.ActiveModule = string.Empty;
        OpenModule(DashboardModuleViewModel.ModuleKey);
        StatusText = "Layout reset to default";
    }

    private ModuleDocumentViewModel OpenModuleInternal(string moduleKey, object? parameter)
    {
        var existing = Documents.OfType<ModuleDocumentViewModel>()
            .FirstOrDefault(m => string.Equals(m.ModuleKey, moduleKey, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            _ = existing.InitializeAsync(parameter);
            return existing;
        }

        var document = CreateModuleInstance(moduleKey, null);
        Documents.Add(document);
        _ = document.InitializeAsync(parameter);
        return document;
    }

    private ModuleDocumentViewModel CreateModuleInstance(string moduleKey, string? contentIdOverride)
    {
        var vm = _moduleRegistry.CreateModule(moduleKey);
        if (contentIdOverride is not null)
        {
            vm.ContentId = contentIdOverride;
        }
        return vm;
    }

    private void ActivateInternal(ModuleDocumentViewModel document)
    {
        ActiveDocument = document;
        StatusBar.ActiveModule = document.Title;
    }

    private void UpdateStatusInternal(string message)
    {
        StatusText = message;
    }

    private static string? TryParseModuleKey(string contentId)
    {
        if (string.IsNullOrWhiteSpace(contentId))
        {
            return null;
        }

        var parts = contentId.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 4 && string.Equals(parts[2], "Module", StringComparison.OrdinalIgnoreCase))
        {
            return parts[3];
        }

        return null;
    }
}
