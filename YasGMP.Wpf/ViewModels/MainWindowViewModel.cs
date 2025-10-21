using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.ViewModels;

/// <summary>
/// Root view-model that orchestrates the docked workspace.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private const string ReadyStatusKey = "Shell.Status.Ready";
    private const string LayoutResetStatusKey = "Shell.Status.LayoutReset";
    private const string SmokeDisabledStatusKey = "Shell.Status.Smoke.Disabled";
    private const string SmokeRunningStatusKey = "Shell.Status.Smoke.Running";
    private const string SmokeFailedStatusKey = "Shell.Status.Smoke.Failed";

    private readonly IModuleRegistry _moduleRegistry;
    private readonly ShellInteractionService _shellInteraction;
    private readonly DebugSmokeTestService _smokeTestService;
    private readonly ILocalizationService _localization;
    private readonly IShellAlertService _alertService;
    private string _statusText = string.Empty;
    private string? _statusResourceKey;
    private object?[]? _statusResourceArguments;

    [ObservableProperty]
    private DocumentViewModel? _activeDocument;
    /// <summary>
    /// Initializes a new instance of the MainWindowViewModel class.
    /// </summary>

    public MainWindowViewModel(
        IModuleRegistry moduleRegistry,
        ModulesPaneViewModel modulesPane,
        InspectorPaneViewModel inspectorPane,
        ShellStatusBarViewModel statusBar,
        ShellInteractionService shellInteraction,
        DebugSmokeTestService smokeTestService,
        ILocalizationService localization,
        IShellAlertService alertService)
    {
        _moduleRegistry = moduleRegistry;
        _shellInteraction = shellInteraction;
        _smokeTestService = smokeTestService;
        _localization = localization;
        _alertService = alertService;
        ModulesPane = modulesPane;
        InspectorPane = inspectorPane;
        StatusBar = statusBar;
        Documents = new ObservableCollection<DocumentViewModel>();
        WindowCommands = new WindowMenuViewModel(this);
        RunSmokeTestCommand = new AsyncRelayCommand(RunSmokeTestAsync, () => _smokeTestService.IsEnabled);
        Toasts = alertService?.Toasts
            ?? new ReadOnlyObservableCollection<ToastNotificationViewModel>(new ObservableCollection<ToastNotificationViewModel>());

        _localization.LanguageChanged += OnLanguageChanged;

        _shellInteraction.Configure(OpenModuleInternal, ActivateInternal, UpdateStatusInternal, InspectorPane.Update, OpenDocumentInternal, CloseDocumentInternal);
        RefreshShellContext();
        SetStatusFromResource(ReadyStatusKey);
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

    /// <summary>Runs the debug smoke test harness surfaced on the Tools ribbon tab.</summary>
    public IAsyncRelayCommand RunSmokeTestCommand { get; }

    /// <summary>Toast notifications rendered in the top-right corner of the shell.</summary>
    public ReadOnlyObservableCollection<ToastNotificationViewModel> Toasts { get; }

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
        SetStatusFromResource(LayoutResetStatusKey);
    }

    /// <summary>Re-evaluates and pushes connection/session metadata into the status bar.</summary>
    public void RefreshShellContext()
    {
        StatusBar.RefreshMetadata();
    }

    /// <summary>Updates the shell status bar using a localization resource key.</summary>
    /// <param name="resourceKey">Localization resource key resolved through the injected service.</param>
    /// <param name="arguments">Optional format arguments applied with the current culture.</param>
    public void UpdateStatusFromResource(string resourceKey, params object?[] arguments)
    {
        SetStatusFromResource(resourceKey, arguments);
    }

    /// <summary>Updates the shell status bar using a pre-localized message while tracking the resource key.</summary>
    /// <param name="resourceKey">Localization resource key associated with the message.</param>
    /// <param name="localizedMessage">Localized message previously resolved by the caller.</param>
    /// <param name="arguments">Optional format arguments to replay when languages change.</param>
    public void UpdateStatusFromResource(string resourceKey, string localizedMessage, params object?[] arguments)
    {
        SetStatusFromResourceWithMessage(resourceKey, localizedMessage, arguments);
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

    private void ActivateInternal(DocumentViewModel document)
    {
        ActiveDocument = document;
        StatusBar.ActiveModule = document.Title;
    }

    private DocumentViewModel OpenDocumentInternal(DocumentViewModel document, bool activate)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var existing = Documents.FirstOrDefault(d => string.Equals(d.ContentId, document.ContentId, StringComparison.Ordinal));
        if (existing is null)
        {
            Documents.Add(document);
            existing = document;
        }

        if (activate)
        {
            ActivateInternal(existing);
        }

        return existing;
    }

    private void CloseDocumentInternal(DocumentViewModel document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var wasActive = ReferenceEquals(ActiveDocument, document);
        if (!Documents.Remove(document))
        {
            return;
        }

        if (wasActive)
        {
            var fallback = Documents.LastOrDefault();
            if (fallback is not null)
            {
                ActivateInternal(fallback);
            }
            else
            {
                ActiveDocument = null;
                StatusBar.ActiveModule = string.Empty;
            }
        }
    }

    private void UpdateStatusInternal(string message)
    {
        SetStatusRaw(message);
    }

    private async Task RunSmokeTestAsync()
    {
        if (!_smokeTestService.IsEnabled)
        {
            SetStatusFromResource(SmokeDisabledStatusKey, DebugSmokeTestService.EnvironmentToggleName);
            RunSmokeTestCommand.NotifyCanExecuteChanged();
            return;
        }

        try
        {
            SetStatusFromResource(SmokeRunningStatusKey);
            var result = await _smokeTestService.RunAsync();
            if (!string.IsNullOrWhiteSpace(result.SummaryResourceKey))
            {
                var args = result.SummaryResourceArguments?.ToArray() ?? Array.Empty<object?>();
                SetStatusFromResource(result.SummaryResourceKey!, args);
            }
            else
            {
                SetStatusRaw(result.Summary);
            }
        }
        catch (Exception ex)
        {
            SetStatusFromResource(SmokeFailedStatusKey, ex.Message);
        }
        finally
        {
            RunSmokeTestCommand.NotifyCanExecuteChanged();
        }
    }

    private void SetStatusFromResource(string resourceKey, params object?[] arguments)
    {
        var args = arguments is { Length: > 0 } ? arguments.ToArray() : Array.Empty<object?>();
        _statusResourceKey = resourceKey;
        _statusResourceArguments = args;
        StatusText = _localization.GetString(resourceKey, args);
    }

    private void SetStatusFromResourceWithMessage(string resourceKey, string localizedMessage, params object?[] arguments)
    {
        var args = arguments is { Length: > 0 } ? arguments.ToArray() : Array.Empty<object?>();
        _statusResourceKey = resourceKey;
        _statusResourceArguments = args;
        StatusText = localizedMessage;
    }

    private void SetStatusRaw(string message)
    {
        _statusResourceKey = null;
        _statusResourceArguments = null;
        StatusText = message;
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        if (_statusResourceKey is null)
        {
            return;
        }

        var args = _statusResourceArguments ?? Array.Empty<object?>();
        StatusText = _localization.GetString(_statusResourceKey, args);
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
