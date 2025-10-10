using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>
/// Base document view-model that reproduces SAP Business One toolbar behaviour.
/// </summary>
public abstract partial class B1FormDocumentViewModel : DocumentViewModel
{
    private const string ReadyStatusKey = "Module.Status.Ready";
    private const string LoadingStatusKey = "Module.Status.Loading";
    private const string LoadedStatusKey = "Module.Status.Loaded";
    private const string OfflineFallbackStatusKey = "Module.Status.OfflineFallback";
    private const string NotInEditModeStatusKey = "Module.Status.NotInEditMode";
    private const string ValidationIssuesStatusKey = "Module.Status.ValidationIssues";
    private const string SaveSuccessStatusKey = "Module.Status.SaveSuccess";
    private const string NoChangesStatusKey = "Module.Status.NoChanges";
    private const string SaveFailureStatusKey = "Module.Status.SaveFailure";
    private const string CancelledStatusKey = "Module.Status.Cancelled";

    private readonly ICflDialogService _cflDialogService;
    private readonly IShellInteractionService _shellInteraction;
    private readonly IModuleNavigationService _moduleNavigation;
    private readonly ILocalizationService _localization;
    private string _currentReadyStatus = string.Empty;

    protected B1FormDocumentViewModel(
        string moduleKey,
        string title,
        ILocalizationService localization,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService moduleNavigation)
    {
        ModuleKey = moduleKey ?? throw new ArgumentNullException(nameof(moduleKey));
        Title = title ?? throw new ArgumentNullException(nameof(title));
        ContentId = $"YasGmp.Shell.Module.{moduleKey}.{Guid.NewGuid():N}";
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        _cflDialogService = cflDialogService;
        _shellInteraction = shellInteraction;
        _moduleNavigation = moduleNavigation;

        Records = new ObservableCollection<ModuleRecord>();
        RecordsView = CollectionViewSource.GetDefaultView(Records);
        RecordsView.Filter = FilterRecord;

        EnterFindModeCommand = new AsyncRelayCommand(SetFindModeAsync, () => CanEnterMode(FormMode.Find));
        EnterAddModeCommand = new AsyncRelayCommand(SetAddModeAsync, () => CanEnterMode(FormMode.Add));
        EnterViewModeCommand = new AsyncRelayCommand(SetViewModeAsync, () => CanEnterMode(FormMode.View));
        EnterUpdateModeCommand = new AsyncRelayCommand(SetUpdateModeAsync, () => CanEnterMode(FormMode.Update));
        SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        CancelCommand = new RelayCommand(Cancel, () => !IsBusy && (IsInEditMode || Mode == FormMode.Find));
        RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsBusy);
        ShowCflCommand = new AsyncRelayCommand(ShowCflAsync, () => !IsBusy);
        GoldenArrowCommand = new RelayCommand(NavigateToRelated, () => SelectedRecord?.RelatedModuleKey is not null && !IsBusy);

        ValidationMessages = new ObservableCollection<string>();
        ValidationMessages.CollectionChanged += OnValidationMessagesChanged;

        Toolbar = new ObservableCollection<ModuleToolbarCommand>
        {
            new("Module.Toolbar.Toggle.Find.Content", EnterFindModeCommand, _localization,
                "Module.Toolbar.Toggle.Find.ToolTip", "Module.Toolbar.Toggle.Find.AutomationName",
                "Module.Toolbar.Toggle.Find.AutomationId", FormMode.Find),
            new("Module.Toolbar.Toggle.Add.Content", EnterAddModeCommand, _localization,
                "Module.Toolbar.Toggle.Add.ToolTip", "Module.Toolbar.Toggle.Add.AutomationName",
                "Module.Toolbar.Toggle.Add.AutomationId", FormMode.Add),
            new("Module.Toolbar.Toggle.View.Content", EnterViewModeCommand, _localization,
                "Module.Toolbar.Toggle.View.ToolTip", "Module.Toolbar.Toggle.View.AutomationName",
                "Module.Toolbar.Toggle.View.AutomationId", FormMode.View),
            new("Module.Toolbar.Toggle.Update.Content", EnterUpdateModeCommand, _localization,
                "Module.Toolbar.Toggle.Update.ToolTip", "Module.Toolbar.Toggle.Update.AutomationName",
                "Module.Toolbar.Toggle.Update.AutomationId", FormMode.Update),
            new("Module.Toolbar.Command.Save.Content", SaveCommand, _localization,
                "Module.Toolbar.Command.Save.ToolTip", "Module.Toolbar.Command.Save.AutomationName",
                "Module.Toolbar.Command.Save.AutomationId"),
            new("Module.Toolbar.Command.Cancel.Content", CancelCommand, _localization,
                "Module.Toolbar.Command.Cancel.ToolTip", "Module.Toolbar.Command.Cancel.AutomationName",
                "Module.Toolbar.Command.Cancel.AutomationId"),
            new("Module.Toolbar.Command.Refresh.Content", RefreshCommand, _localization,
                "Module.Toolbar.Command.Refresh.ToolTip", "Module.Toolbar.Command.Refresh.AutomationName",
                "Module.Toolbar.Command.Refresh.AutomationId")
        };

        StatusMessage = _localization.GetString(ReadyStatusKey);
        _currentReadyStatus = StatusMessage;
        _localization.LanguageChanged += OnLocalizationLanguageChanged;
    }

    /// <summary>Stable module key registered inside <see cref="IModuleRegistry"/>.</summary>
    public string ModuleKey { get; }

    /// <summary>Collection view used for filtering/search.</summary>
    public ICollectionView RecordsView { get; }

    /// <summary>Backing list of module records.</summary>
    public ObservableCollection<ModuleRecord> Records { get; }

    /// <summary>Toolbar buttons surfaced in the view.</summary>
    public ObservableCollection<ModuleToolbarCommand> Toolbar { get; }

    /// <summary>Whether the view-model completed its initial data load.</summary>
    public bool IsInitialized { get; private set; }

    /// <summary>Validation errors surfaced to the UI when saving fails.</summary>
    public ObservableCollection<string> ValidationMessages { get; }

    /// <summary>Indicates whether <see cref="ValidationMessages"/> contains any entries.</summary>
    public bool HasValidationErrors => ValidationMessages.Count > 0;

    /// <summary>Whether the underlying editor contains unsaved changes.</summary>
    [ObservableProperty]
    private bool _isDirty;

    /// <summary>
    /// Command that transitions the document into SAP B1 style Find mode so derived modules can query records without mutating data.
    /// </summary>
    /// <remarks>
    /// Find mode keeps the editor read-only, allows re-entry even when already active, and does not trigger audit hooks until a record is loaded into View mode.
    /// </remarks>
    public IAsyncRelayCommand EnterFindModeCommand { get; }

    /// <summary>
    /// Command that enters Add mode, enabling editor fields for new record entry following SAP B1 toolbar semantics.
    /// </summary>
    /// <remarks>
    /// While Add mode is active the busy gate prevents parallel operations, <see cref="IsDirty"/> is expected to track edits, and audit status updates do not occur until a successful save returns to View mode.
    /// </remarks>
    public IAsyncRelayCommand EnterAddModeCommand { get; }

    /// <summary>
    /// Command that returns the form to View mode for read-only inspection of the selected record.
    /// </summary>
    /// <remarks>
    /// View mode resets edit state, clears validation errors, and is the mode in which status messages reflect the last persisted audit state.
    /// </remarks>
    public IAsyncRelayCommand EnterViewModeCommand { get; }

    /// <summary>
    /// Command that switches the document into Update mode so the current record can be edited in-place.
    /// </summary>
    /// <remarks>
    /// Update mode mirrors SAP B1 behaviour by reusing the selected record, gating execution while <see cref="IsBusy"/> is <c>true</c>, and deferring audit journal updates until <see cref="SaveCommand"/> completes.
    /// </remarks>
    public IAsyncRelayCommand EnterUpdateModeCommand { get; }

    /// <summary>
    /// Command that persists Add/Update changes, drives validation, and propagates audit/status messages back to the shell.
    /// </summary>
    /// <remarks>
    /// The save pipeline invokes <see cref="OnSaveAsync"/>, refreshes data, flips the mode back to View, and updates the status banner so derived modules can hook e-signature or audit logging during the transition.
    /// </remarks>
    public IAsyncRelayCommand SaveCommand { get; }

    /// <summary>
    /// Command that abandons the current edit session, clears validation, and reverts toolbar state without touching persisted data.
    /// </summary>
    /// <remarks>
    /// Cancel is disabled while <see cref="IsBusy"/> is <c>true</c>; when executed it raises <see cref="OnCancel"/>, resets audit messaging to a cancelled status, and returns the form to View mode if necessary.
    /// </remarks>
    public IRelayCommand CancelCommand { get; }

    /// <summary>
    /// Command that reloads module records, updating the busy state and localized status text as SAP B1 toolbars do after a find or save.
    /// </summary>
    /// <remarks>
    /// Refresh is blocked while <see cref="IsBusy"/> is <c>true</c> to ensure audit/status transitions remain ordered and derived modules can safely rehydrate their data sources.
    /// </remarks>
    public IAsyncRelayCommand RefreshCommand { get; }

    /// <summary>
    /// Command that opens the Choose-From-List dialog, allowing derived modules to select related master data while respecting busy gating.
    /// </summary>
    /// <remarks>
    /// The command is disabled while <see cref="IsBusy"/> is <c>true</c> so that audit prompts or status updates originating from the CFL do not overlap long-running operations.
    /// </remarks>
    public IAsyncRelayCommand ShowCflCommand { get; }

    /// <summary>
    /// Command that triggers Golden Arrow navigation into the related module tied to <see cref="SelectedRecord"/>.
    /// </summary>
    /// <remarks>
    /// Navigation is only available when a record exposes <see cref="ModuleRecord.RelatedModuleKey"/> and the view-model is not busy, ensuring audit chains reflect the originating record before transitioning.
    /// </remarks>
    public IRelayCommand GoldenArrowCommand { get; }

    /// <summary>
    /// Current SAP B1 form mode that drives toolbar toggles, editor enablement, and audit lifecycle hooks.
    /// </summary>
    /// <remarks>
    /// Changing the mode invokes <see cref="OnModeChangedAsync(FormMode)"/>, resets dirty state when leaving edit scenarios, and synchronizes status messages with the shell.
    /// </remarks>
    [ObservableProperty]
    private FormMode _mode = FormMode.View;

    /// <summary>
    /// Indicates whether the view-model is performing an asynchronous operation, disabling toolbar commands until completion.
    /// </summary>
    /// <remarks>
    /// Derived modules should check this flag before triggering long-running work so that audit/status updates maintain the same ordering expected in SAP B1.
    /// </remarks>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>
    /// Localized status text surfaced in the shell status bar to reflect audit outcomes and busy states.
    /// </summary>
    /// <remarks>
    /// Updates propagate to <see cref="IShellInteractionService"/> which mirrors SAP B1 behaviour by keeping the status bar synchronized with find/add/view/update transitions.
    /// </remarks>
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>
    /// Free-form text entered by the user to filter the records list while in Find or View mode.
    /// </summary>
    /// <remarks>
    /// Changing this property refreshes <see cref="RecordsView"/> and lets derived modules extend search semantics via <see cref="MatchesSearch(ModuleRecord, string)"/>.
    /// </remarks>
    [ObservableProperty]
    private string? _searchText;

    /// <summary>
    /// Record currently highlighted in the results grid, used for inspector details, Golden Arrow navigation, and update operations.
    /// </summary>
    /// <remarks>
    /// Assigning a value updates the inspector, raises <see cref="OnRecordSelectedAsync(ModuleRecord?)"/>, and determines whether edit/audit commands operate on an existing entity or a placeholder.
    /// </remarks>
    [ObservableProperty]
    private ModuleRecord? _selectedRecord;

    /// <summary>Returns <c>true</c> when the current form is in Add or Update mode.</summary>
    public bool IsInEditMode => Mode is FormMode.Add or FormMode.Update;

    /// <summary>
    /// Loads data when the document is opened or activated through golden arrow navigation.
    /// </summary>
    public async Task InitializeAsync(object? parameter)
    {
        if (IsInitialized)
        {
            await OnActivatedAsync(parameter).ConfigureAwait(false);
            return;
        }

        await RefreshAsync(parameter).ConfigureAwait(false);
        IsInitialized = true;
    }

    /// <summary>Triggers the refresh command programmatically.</summary>
    public Task RefreshAsync() => RefreshAsync(null);

    /// <summary>Overrides provide the actual data fetch implementation.</summary>
    protected abstract Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter);

    /// <summary>Derived classes can generate fallback/offline records when the live query fails.</summary>
    protected abstract IReadOnlyList<ModuleRecord> CreateDesignTimeRecords();

    /// <summary>Hook invoked when the view-model becomes active again.</summary>
    protected virtual Task OnActivatedAsync(object? parameter) => Task.CompletedTask;

    /// <summary>Hook invoked when entering a specific mode.</summary>
    protected virtual Task OnModeChangedAsync(FormMode mode) => Task.CompletedTask;

    /// <summary>Hook invoked prior to saving.</summary>
    protected virtual Task<bool> OnSaveAsync() => Task.FromResult(false);

    /// <summary>Hook invoked when cancelling edits.</summary>
    protected virtual void OnCancel() { }

    /// <summary>Allows derived classes to extend CFL behaviour.</summary>
    protected virtual Task<CflRequest?> CreateCflRequestAsync() => Task.FromResult<CflRequest?>(null);

    /// <summary>Allows derived classes to handle CFL selection.</summary>
    protected virtual Task OnCflSelectionAsync(CflResult result) => Task.CompletedTask;

    /// <summary>Override to support custom search filters.</summary>
    protected virtual bool MatchesSearch(ModuleRecord record, string searchText)
        => record.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase)
           || (!string.IsNullOrWhiteSpace(record.Code) && record.Code.Contains(searchText, StringComparison.OrdinalIgnoreCase))
           || (!string.IsNullOrWhiteSpace(record.Description) && record.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase));

    /// <summary>Formats the status message after records have been loaded.</summary>
    protected virtual string FormatLoadedStatus(int count)
    {
        if (count < 0)
        {
            count = 0;
        }

        return _localization.GetString(LoadedStatusKey, count);
    }

    partial void OnModeChanged(FormMode value)
    {
        foreach (var button in Toolbar)
        {
            button.IsChecked = button.AssociatedMode is not null && button.AssociatedMode == value;
        }

        _shellInteraction.UpdateStatus($"{Title}: {value} mode");
        OnPropertyChanged(nameof(IsInEditMode));

        if (value is not (FormMode.Add or FormMode.Update))
        {
            ResetDirty();
        }

        ClearValidationMessages();
        _ = OnModeChangedAsync(value);
        RefreshCommandStates();
    }

    partial void OnIsBusyChanged(bool value)
    {
        RefreshCommandStates();
    }

    partial void OnIsDirtyChanged(bool value)
    {
        RefreshCommandStates();
    }

    partial void OnStatusMessageChanged(string value)
    {
        _shellInteraction.UpdateStatus(value);
    }

    private void OnLocalizationLanguageChanged(object? sender, EventArgs e)
    {
        var ready = _localization.GetString(ReadyStatusKey);
        if (string.IsNullOrWhiteSpace(StatusMessage) || StatusMessage == _currentReadyStatus)
        {
            StatusMessage = ready;
        }

        _currentReadyStatus = ready;
    }

    partial void OnSearchTextChanged(string? value)
    {
        RecordsView.Refresh();
    }

    partial void OnSelectedRecordChanged(ModuleRecord? value)
    {
        GoldenArrowCommand.NotifyCanExecuteChanged();
        if (value is null)
        {
            _shellInteraction.UpdateInspector(new InspectorContext(ModuleKey, Title, null, "No record selected", Array.Empty<InspectorField>()));
            _ = OnRecordSelectedAsync(null);
            RefreshCommandStates();
            return;
        }

        _shellInteraction.UpdateInspector(new InspectorContext(ModuleKey, Title, value.Key, value.Title, value.InspectorFields));
        _ = OnRecordSelectedAsync(value);
        RefreshCommandStates();
    }

    private Task SetFindModeAsync()
    {
        Mode = FormMode.Find;
        return Task.CompletedTask;
    }

    private Task SetAddModeAsync()
    {
        Mode = FormMode.Add;
        return Task.CompletedTask;
    }

    private Task SetViewModeAsync()
    {
        Mode = FormMode.View;
        return Task.CompletedTask;
    }

    private Task SetUpdateModeAsync()
    {
        Mode = FormMode.Update;
        return Task.CompletedTask;
    }

    private async Task RefreshAsync(object? parameter)
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = _localization.GetString(LoadingStatusKey, Title);
            var records = await LoadAsync(parameter).ConfigureAwait(false);
            ApplyRecords(records);
            StatusMessage = FormatLoadedStatus(Records.Count);
        }
        catch (Exception ex)
        {
            ApplyRecords(CreateDesignTimeRecords());
            StatusMessage = _localization.GetString(OfflineFallbackStatusKey, ex.Message);
        }
        finally
        {
            IsBusy = false;
            RefreshCommandStates();
        }
    }

    private void ApplyRecords(IReadOnlyList<ModuleRecord> records)
    {
        Records.Clear();
        foreach (var record in records)
        {
            Records.Add(record);
        }

        RecordsView.Refresh();
        if (Records.Count > 0)
        {
            SelectedRecord = Records[0];
        }
        else
        {
            SelectedRecord = null;
        }
    }

    private bool FilterRecord(object obj)
    {
        if (obj is not ModuleRecord record)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        return MatchesSearch(record, SearchText!);
    }

    private bool CanEnterMode(FormMode mode)
    {
        if (IsBusy)
        {
            return false;
        }

        if (mode == FormMode.Find)
        {
            return true;
        }

        if (Mode == mode)
        {
            return false;
        }

        return mode switch
        {
            FormMode.View or FormMode.Update => SelectedRecord is not null,
            FormMode.Add => !IsDirty && !HasValidationErrors,
            _ => true
        };
    }

    private async Task<bool> SaveAsync()
    {
        if (IsBusy)
        {
            return false;
        }

        if (!IsInEditMode)
        {
            StatusMessage = _localization.GetString(NotInEditModeStatusKey, Title);
            return false;
        }

        try
        {
            IsBusy = true;
            var validation = await ValidateAsync().ConfigureAwait(false);
            ApplyValidation(validation);
            if (validation.Count > 0)
            {
                StatusMessage = _localization.GetString(ValidationIssuesStatusKey, Title, validation.Count);
                return false;
            }

            var previousMessage = StatusMessage;
            var saved = await OnSaveAsync().ConfigureAwait(false);
            if (saved)
            {
                if (string.IsNullOrWhiteSpace(StatusMessage) || StatusMessage == previousMessage)
                {
                    StatusMessage = _localization.GetString(SaveSuccessStatusKey, Title);
                }

                ResetDirty();
                Mode = FormMode.View;
                await RefreshAsync().ConfigureAwait(false);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(StatusMessage) || StatusMessage == previousMessage)
                {
                    StatusMessage = _localization.GetString(NoChangesStatusKey, Title);
                }
            }

            return saved;
        }
        catch (Exception ex)
        {
            StatusMessage = _localization.GetString(SaveFailureStatusKey, Title, ex.Message);
            return false;
        }
        finally
        {
            IsBusy = false;
            RefreshCommandStates();
        }
    }

    private void Cancel()
    {
        if (IsBusy)
        {
            return;
        }

        OnCancel();
        ResetDirty();
        ClearValidationMessages();
        if (Mode is FormMode.Add or FormMode.Update)
        {
            Mode = FormMode.View;
        }
        StatusMessage = _localization.GetString(CancelledStatusKey, Title);
        RefreshCommandStates();
    }

    private async Task ShowCflAsync()
    {
        var request = await CreateCflRequestAsync().ConfigureAwait(false);
        if (request is null)
        {
            return;
        }

        var result = await _cflDialogService.ShowAsync(request).ConfigureAwait(false);
        if (result is null)
        {
            return;
        }

        await OnCflSelectionAsync(result).ConfigureAwait(false);
    }

    private void NavigateToRelated()
    {
        if (SelectedRecord?.RelatedModuleKey is null)
        {
            return;
        }

        var document = _moduleNavigation.OpenModule(SelectedRecord.RelatedModuleKey, SelectedRecord.RelatedParameter);
        _moduleNavigation.Activate(document);
    }

    private bool CanSave() => !IsBusy && IsInEditMode;

    private void RefreshCommandStates()
    {
        EnterFindModeCommand.NotifyCanExecuteChanged();
        EnterAddModeCommand.NotifyCanExecuteChanged();
        EnterViewModeCommand.NotifyCanExecuteChanged();
        EnterUpdateModeCommand.NotifyCanExecuteChanged();
        SaveCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
        RefreshCommand.NotifyCanExecuteChanged();
        ShowCflCommand.NotifyCanExecuteChanged();
        GoldenArrowCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Allows derived classes to react when the selection changes.</summary>
    protected virtual Task OnRecordSelectedAsync(ModuleRecord? record) => Task.CompletedTask;

    /// <summary>Allows derived classes to provide validation before save.</summary>
    protected virtual Task<IReadOnlyList<string>> ValidateAsync() => Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

    /// <summary>Marks the current editor as dirty so command enablement updates.</summary>
    protected void MarkDirty()
    {
        if (!IsDirty)
        {
            IsDirty = true;
        }
    }

    /// <summary>Resets the dirty flag without triggering additional refresh operations.</summary>
    protected void ResetDirty()
    {
        if (IsDirty)
        {
            IsDirty = false;
        }
    }

    /// <summary>Clears any validation errors previously recorded.</summary>
    protected void ClearValidationMessages()
    {
        if (ValidationMessages.Count == 0)
        {
            RefreshCommandStates();
            return;
        }

        ValidationMessages.Clear();
        RefreshCommandStates();
    }

    /// <summary>Pushes validation errors to the observable collection.</summary>
    protected void ApplyValidation(IEnumerable<string> errors)
    {
        ValidationMessages.Clear();
        if (errors is null)
        {
            RefreshCommandStates();
            return;
        }

        foreach (var error in errors)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                ValidationMessages.Add(error);
            }
        }

        RefreshCommandStates();
    }

    private void OnValidationMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasValidationErrors));
        RefreshCommandStates();
    }

    /// <summary>
    /// Creates an <see cref="InspectorField"/> using the module context and supplied record metadata.
    /// </summary>
    /// <param name="recordKey">Stable key that identifies the record.</param>
    /// <param name="recordTitle">Display title associated with the record.</param>
    /// <param name="label">Inspector label.</param>
    /// <param name="value">Inspector value.</param>
    /// <returns>A populated <see cref="InspectorField"/> scoped to the module.</returns>
    protected InspectorField CreateInspectorField(string? recordKey, string? recordTitle, string label, string? value)
        => InspectorField.Create(ModuleKey, Title, recordKey, recordTitle, label, value);
}
