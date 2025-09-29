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
    private readonly ICflDialogService _cflDialogService;
    private readonly IShellInteractionService _shellInteraction;
    private readonly IModuleNavigationService _moduleNavigation;

    protected B1FormDocumentViewModel(
        string moduleKey,
        string title,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService moduleNavigation)
    {
        ModuleKey = moduleKey ?? throw new ArgumentNullException(nameof(moduleKey));
        Title = title ?? throw new ArgumentNullException(nameof(title));
        ContentId = $"YasGmp.Shell.Module.{moduleKey}.{Guid.NewGuid():N}";
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
            new("Find", EnterFindModeCommand),
            new("Add", EnterAddModeCommand),
            new("View", EnterViewModeCommand),
            new("Update", EnterUpdateModeCommand),
            new("Save", SaveCommand),
            new("Cancel", CancelCommand),
            new("Refresh", RefreshCommand)
        };
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

    public IAsyncRelayCommand EnterFindModeCommand { get; }

    public IAsyncRelayCommand EnterAddModeCommand { get; }

    public IAsyncRelayCommand EnterViewModeCommand { get; }

    public IAsyncRelayCommand EnterUpdateModeCommand { get; }

    public IAsyncRelayCommand SaveCommand { get; }

    public IRelayCommand CancelCommand { get; }

    public IAsyncRelayCommand RefreshCommand { get; }

    public IAsyncRelayCommand ShowCflCommand { get; }

    public IRelayCommand GoldenArrowCommand { get; }

    [ObservableProperty]
    private FormMode _mode = FormMode.View;

    [ObservableProperty]
    private bool _isBusy;

    /// <summary>Monotonically increasing counter used to detect status updates.</summary>
    private int _statusMessageVersion;

    private string _statusMessage = "Ready";

    /// <summary>
    /// Gets or sets the status message shown in the shell. The version counter increments on every
    /// assignment so callers can detect programmatic updates even when the text itself is unchanged.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessageVersion++;
            var coercedValue = value ?? string.Empty;

            if (!string.Equals(_statusMessage, coercedValue, StringComparison.Ordinal))
            {
                SetProperty(ref _statusMessage, coercedValue);
            }
            else
            {
                OnPropertyChanged(nameof(StatusMessage));
            }

            _shellInteraction.UpdateStatus(coercedValue);
        }
    }

    [ObservableProperty]
    private string? _searchText;

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

        return $"Loaded {count} record(s).";
    }

    partial void OnModeChanged(FormMode value)
    {
        foreach (var button in Toolbar)
        {
            button.IsChecked = string.Equals(button.Caption, value.ToString(), StringComparison.OrdinalIgnoreCase);
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

    partial void OnSearchTextChanged(string? value)
    {
        RecordsView.Refresh();
    }

    partial void OnSelectedRecordChanged(ModuleRecord? value)
    {
        GoldenArrowCommand.NotifyCanExecuteChanged();
        if (value is null)
        {
            _shellInteraction.UpdateInspector(new InspectorContext(Title, "No record selected", Array.Empty<InspectorField>()));
            _ = OnRecordSelectedAsync(null);
            return;
        }

        _shellInteraction.UpdateInspector(new InspectorContext(Title, value.Title, value.InspectorFields));
        _ = OnRecordSelectedAsync(value);
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
            StatusMessage = $"Loading {Title} records...";
            var records = await LoadAsync(parameter).ConfigureAwait(false);
            ApplyRecords(records);
            StatusMessage = FormatLoadedStatus(Records.Count);
        }
        catch (Exception ex)
        {
            ApplyRecords(CreateDesignTimeRecords());
            StatusMessage = $"Offline data loaded because: {ex.Message}";
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
        => !IsBusy && (Mode != mode || mode == FormMode.Find);

    private async Task<bool> SaveAsync()
    {
        if (IsBusy)
        {
            return false;
        }

        if (!IsInEditMode)
        {
            StatusMessage = $"{Title} is not in Add/Update mode.";
            return false;
        }

        try
        {
            IsBusy = true;
            var validation = await ValidateAsync().ConfigureAwait(false);
            ApplyValidation(validation);
            if (validation.Count > 0)
            {
                StatusMessage = $"{Title} has {validation.Count} validation issue(s).";
                return false;
            }

            var previousMessage = StatusMessage;
            var previousMessageVersion = _statusMessageVersion;
            var saved = await OnSaveAsync().ConfigureAwait(false);
            var statusCustomized = _statusMessageVersion != previousMessageVersion;
            if (saved)
            {
                if (!statusCustomized && (string.IsNullOrWhiteSpace(StatusMessage) || StatusMessage == previousMessage))
                {
                    StatusMessage = $"{Title} saved successfully.";
                }

                ResetDirty();
                Mode = FormMode.View;
                await RefreshAsync().ConfigureAwait(false);
            }
            else
            {
                if (!statusCustomized && (string.IsNullOrWhiteSpace(StatusMessage) || StatusMessage == previousMessage))
                {
                    StatusMessage = $"No changes to save for {Title}.";
                }
            }

            return saved;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to save {Title}: {ex.Message}";
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
        StatusMessage = $"{Title} changes cancelled.";
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
            return;
        }

        ValidationMessages.Clear();
    }

    /// <summary>Pushes validation errors to the observable collection.</summary>
    protected void ApplyValidation(IEnumerable<string> errors)
    {
        ValidationMessages.Clear();
        if (errors is null)
        {
            return;
        }

        foreach (var error in errors)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                ValidationMessages.Add(error);
            }
        }
    }

    private void OnValidationMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasValidationErrors));
        RefreshCommandStates();
    }
}
