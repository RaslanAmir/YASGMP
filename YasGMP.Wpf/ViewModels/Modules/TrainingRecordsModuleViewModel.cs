using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Models;
using YasGMP.ViewModels;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>
/// Projects the shared <see cref="TrainingRecordViewModel"/> into the WPF shell with SAP B1 form-mode semantics.
/// </summary>
public sealed partial class TrainingRecordsModuleViewModel : ModuleDocumentViewModel, IDisposable
{
    /// <summary>Stable registry key consumed by the module tree, inspector, and golden arrow navigation.</summary>
    public const string ModuleKey = "TrainingRecords";

    private readonly TrainingRecordViewModel _trainingRecords;
    private readonly ILocalizationService _localization;
    private readonly AsyncRelayCommand _initiateCommand;
    private readonly AsyncRelayCommand _assignCommand;
    private readonly AsyncRelayCommand _approveCommand;
    private readonly AsyncRelayCommand _completeCommand;
    private readonly AsyncRelayCommand _closeCommand;
    private readonly AsyncRelayCommand _exportCommand;
    private INotifyCollectionChanged? _filteredRecordsSubscription;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrainingRecordsModuleViewModel"/> class.
    /// </summary>
    public TrainingRecordsModuleViewModel(
        TrainingRecordViewModel trainingRecords,
        ILocalizationService localization,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, localization.GetString("Module.Title.TrainingRecords"), localization, cflDialogService, shellInteraction, navigation)
    {
        _trainingRecords = trainingRecords ?? throw new ArgumentNullException(nameof(trainingRecords));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));

        _trainingRecords.PropertyChanged += OnTrainingRecordsPropertyChanged;
        UpdateFilteredRecordsSubscription(_trainingRecords.FilteredTrainingRecords);

        _initiateCommand = new AsyncRelayCommand(ExecuteInitiateAsync, CanInitiate);
        _assignCommand = new AsyncRelayCommand(ExecuteAssignAsync, CanAssign);
        _approveCommand = new AsyncRelayCommand(ExecuteApproveAsync, CanApprove);
        _completeCommand = new AsyncRelayCommand(ExecuteCompleteAsync, CanComplete);
        _closeCommand = new AsyncRelayCommand(ExecuteCloseAsync, CanClose);
        _exportCommand = new AsyncRelayCommand(ExecuteExportAsync, CanExport);

        PropertyChanged += OnSelfPropertyChanged;

        Editor = TrainingRecordEditor.CreateEmpty(OnEditorChanged);
        IsEditorEnabled = false;
    }

    /// <summary>Shared training records view-model surfaced for bindings.</summary>
    public TrainingRecordViewModel TrainingRecords => _trainingRecords;

    /// <summary>Command that initiates a new training record from the current editor.</summary>
    public IAsyncRelayCommand InitiateCommand => _initiateCommand;

    /// <summary>Command that assigns the selected training record.</summary>
    public IAsyncRelayCommand AssignCommand => _assignCommand;

    /// <summary>Command that approves the selected training record.</summary>
    public IAsyncRelayCommand ApproveCommand => _approveCommand;

    /// <summary>Command that marks the selected training record as completed.</summary>
    public IAsyncRelayCommand CompleteCommand => _completeCommand;

    /// <summary>Command that closes the selected training record after effectiveness checks.</summary>
    public IAsyncRelayCommand CloseCommand => _closeCommand;

    /// <summary>Command that exports the filtered snapshot to PDF/Excel.</summary>
    public IAsyncRelayCommand ExportCommand => _exportCommand;

    /// <summary>Status options mirrored from the shared view-model.</summary>
    public IReadOnlyList<string> StatusOptions => _trainingRecords.AvailableStatuses;

    /// <summary>Training type options mirrored from the shared view-model.</summary>
    public IReadOnlyList<string> TypeOptions => _trainingRecords.AvailableTypes;

    /// <summary>Active status filter forwarded to the shared MAUI view-model.</summary>
    public string? StatusFilter
    {
        get => _trainingRecords.StatusFilter;
        set
        {
            if (!string.Equals(_trainingRecords.StatusFilter, value, StringComparison.Ordinal))
            {
                _trainingRecords.StatusFilter = value;
                OnPropertyChanged();
                ProjectRecordsIntoShell();
            }
        }
    }

    /// <summary>Active type filter forwarded to the shared MAUI view-model.</summary>
    public string? TypeFilter
    {
        get => _trainingRecords.TypeFilter;
        set
        {
            if (!string.Equals(_trainingRecords.TypeFilter, value, StringComparison.Ordinal))
            {
                _trainingRecords.TypeFilter = value;
                OnPropertyChanged();
                ProjectRecordsIntoShell();
            }
        }
    }

    /// <summary>Indicates whether editor fields are enabled for Add/Update operations.</summary>
    [ObservableProperty]
    private bool _isEditorEnabled;

    /// <summary>Projection of the selected training record used for Add/Update modes.</summary>
    [ObservableProperty]
    private TrainingRecordEditor _editor;

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        await _trainingRecords.LoadTrainingRecordsAsync().ConfigureAwait(false);
        return ProjectRecords();
    }

    /// <inheritdoc />
    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
    {
        var today = DateTime.Today;
        return new List<ModuleRecord>
        {
            CreateRecord(new TrainingRecord
            {
                Id = 1001,
                Code = "TR-001",
                Title = "GMP Awareness",
                TrainingType = "GMP",
                Status = "planned",
                AssignedToName = "Ava Martin",
                DueDate = today.AddDays(30),
                TrainingDate = today.AddDays(7),
                ExpiryDate = today.AddMonths(12)
            }),
            CreateRecord(new TrainingRecord
            {
                Id = 1002,
                Code = "TR-002",
                Title = "SOP-123 Revision",
                TrainingType = "SOP",
                Status = "pending_approval",
                AssignedToName = "Luka Horvat",
                DueDate = today.AddDays(10),
                TrainingDate = today.AddDays(-1),
                ExpiryDate = today.AddMonths(6)
            })
        };
    }

    /// <inheritdoc />
    protected override Task OnModeChangedAsync(FormMode mode)
    {
        switch (mode)
        {
            case FormMode.Add:
                Editor = TrainingRecordEditor.CreateEmpty(OnEditorChanged);
                IsEditorEnabled = true;
                break;
            case FormMode.Update:
                if (_trainingRecords.SelectedTrainingRecord is TrainingRecord record)
                {
                    Editor = TrainingRecordEditor.FromEntity(record, OnEditorChanged);
                    IsEditorEnabled = true;
                }
                else
                {
                    Editor = TrainingRecordEditor.CreateEmpty(OnEditorChanged);
                    IsEditorEnabled = false;
                }
                break;
            default:
                if (_trainingRecords.SelectedTrainingRecord is TrainingRecord current)
                {
                    Editor = TrainingRecordEditor.FromEntity(current, OnEditorChanged);
                }
                IsEditorEnabled = false;
                break;
        }

        UpdateWorkflowCommandStates();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override async Task<bool> OnSaveAsync()
    {
        if (Mode == FormMode.Add)
        {
            var draft = Editor.ToEntity();
            await _trainingRecords.InitiateTrainingRecordAsync(draft, Editor.Note).ConfigureAwait(false);
            StatusMessage = _localization.GetString("Module.TrainingRecords.Status.Initiated", draft.Title);
            return true;
        }

        if (Mode == FormMode.Update)
        {
            var selected = _trainingRecords.SelectedTrainingRecord;
            if (selected is null)
            {
                return false;
            }

            Editor.ApplyTo(selected);
            var nextAction = DetermineWorkflowAction(selected.Status);
            switch (nextAction)
            {
                case WorkflowAction.Assign:
                    await ExecuteAssignAsync().ConfigureAwait(false);
                    StatusMessage = _localization.GetString("Module.TrainingRecords.Status.Assigned", selected.Title);
                    return true;
                case WorkflowAction.Approve:
                    await ExecuteApproveAsync().ConfigureAwait(false);
                    StatusMessage = _localization.GetString("Module.TrainingRecords.Status.Approved", selected.Title);
                    return true;
                case WorkflowAction.Complete:
                    await ExecuteCompleteAsync().ConfigureAwait(false);
                    StatusMessage = _localization.GetString("Module.TrainingRecords.Status.Completed", selected.Title);
                    return true;
                case WorkflowAction.Close:
                    await ExecuteCloseAsync().ConfigureAwait(false);
                    StatusMessage = _localization.GetString("Module.TrainingRecords.Status.Closed", selected.Title);
                    return true;
                default:
                    return false;
            }
        }

        return false;
    }

    /// <inheritdoc />
    protected override void OnCancel()
    {
        if (Mode == FormMode.Add)
        {
            Editor = TrainingRecordEditor.CreateEmpty(OnEditorChanged);
        }
        else if (_trainingRecords.SelectedTrainingRecord is TrainingRecord record)
        {
            Editor = TrainingRecordEditor.FromEntity(record, OnEditorChanged);
        }

        UpdateWorkflowCommandStates();
    }

    /// <inheritdoc />
    protected override Task OnRecordSelectedAsync(ModuleRecord? record)
    {
        if (record is null)
        {
            _trainingRecords.SelectedTrainingRecord = null;
            Editor = TrainingRecordEditor.CreateEmpty(OnEditorChanged);
            UpdateWorkflowCommandStates();
            return Task.CompletedTask;
        }

        if (int.TryParse(record.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
        {
            var entity = _trainingRecords.TrainingRecords.FirstOrDefault(t => t.Id == id);
            _trainingRecords.SelectedTrainingRecord = entity;
            if (entity is not null)
            {
                Editor = TrainingRecordEditor.FromEntity(entity, OnEditorChanged);
            }
        }

        UpdateWorkflowCommandStates();
        return Task.CompletedTask;
    }

    private void OnEditorChanged()
    {
        if (Mode is FormMode.Add or FormMode.Update)
        {
            MarkDirty();
        }
    }

    private async Task ExecuteInitiateAsync()
    {
        if (!CanInitiate())
        {
            return;
        }

        var draft = Editor.ToEntity();
        await _trainingRecords.InitiateTrainingRecordAsync(draft, Editor.Note).ConfigureAwait(false);
        StatusMessage = _localization.GetString("Module.TrainingRecords.Status.Initiated", draft.Title);
        await RefreshCommand.ExecuteAsync(null).ConfigureAwait(false);
    }

    private bool CanInitiate() => !IsBusy;

    private async Task ExecuteAssignAsync()
    {
        if (!CanAssign())
        {
            return;
        }

        await ExecuteAsync(_trainingRecords.AssignTrainingRecordCommand).ConfigureAwait(false);
        await RefreshCommand.ExecuteAsync(null).ConfigureAwait(false);
    }

    private bool CanAssign()
        => !IsBusy && _trainingRecords.SelectedTrainingRecord is TrainingRecord record
           && string.Equals(record.Status, "planned", StringComparison.OrdinalIgnoreCase);

    private async Task ExecuteApproveAsync()
    {
        if (!CanApprove())
        {
            return;
        }

        await ExecuteAsync(_trainingRecords.ApproveTrainingRecordCommand).ConfigureAwait(false);
        await RefreshCommand.ExecuteAsync(null).ConfigureAwait(false);
    }

    private bool CanApprove()
        => !IsBusy && _trainingRecords.SelectedTrainingRecord is TrainingRecord record
           && string.Equals(record.Status, "pending_approval", StringComparison.OrdinalIgnoreCase);

    private async Task ExecuteCompleteAsync()
    {
        if (!CanComplete())
        {
            return;
        }

        await ExecuteAsync(_trainingRecords.CompleteTrainingRecordCommand).ConfigureAwait(false);
        await RefreshCommand.ExecuteAsync(null).ConfigureAwait(false);
    }

    private bool CanComplete()
        => !IsBusy && _trainingRecords.SelectedTrainingRecord is TrainingRecord record
           && string.Equals(record.Status, "assigned", StringComparison.OrdinalIgnoreCase);

    private async Task ExecuteCloseAsync()
    {
        if (!CanClose())
        {
            return;
        }

        await ExecuteAsync(_trainingRecords.CloseTrainingRecordCommand).ConfigureAwait(false);
        await RefreshCommand.ExecuteAsync(null).ConfigureAwait(false);
    }

    private bool CanClose()
        => !IsBusy && _trainingRecords.SelectedTrainingRecord is TrainingRecord record
           && string.Equals(record.Status, "completed", StringComparison.OrdinalIgnoreCase);

    private async Task ExecuteExportAsync()
    {
        if (!CanExport())
        {
            return;
        }

        await ExecuteAsync(_trainingRecords.ExportTrainingRecordsCommand).ConfigureAwait(false);
        StatusMessage = _trainingRecords.StatusMessage ?? string.Empty;
    }

    private bool CanExport() => !IsBusy && _trainingRecords.FilteredTrainingRecords.Count > 0;

    private void OnTrainingRecordsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(TrainingRecordViewModel.StatusMessage), StringComparison.Ordinal))
        {
            if (!string.IsNullOrWhiteSpace(_trainingRecords.StatusMessage))
            {
                StatusMessage = _trainingRecords.StatusMessage!;
            }
        }
        else if (string.Equals(e.PropertyName, nameof(TrainingRecordViewModel.IsBusy), StringComparison.Ordinal))
        {
            IsBusy = _trainingRecords.IsBusy;
            RefreshCommandStates();
            UpdateWorkflowCommandStates();
        }
        else if (string.Equals(e.PropertyName, nameof(TrainingRecordViewModel.SelectedTrainingRecord), StringComparison.Ordinal))
        {
            if (_trainingRecords.SelectedTrainingRecord is TrainingRecord record)
            {
                Editor = TrainingRecordEditor.FromEntity(record, OnEditorChanged);
            }
            UpdateWorkflowCommandStates();
        }
        else if (string.Equals(e.PropertyName, nameof(TrainingRecordViewModel.FilteredTrainingRecords), StringComparison.Ordinal))
        {
            UpdateFilteredRecordsSubscription(_trainingRecords.FilteredTrainingRecords);
            ProjectRecordsIntoShell();
        }
    }

    private void OnFilteredTrainingRecordsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ProjectRecordsIntoShell();
    }

    private void OnSelfPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(SearchText), StringComparison.Ordinal))
        {
            _trainingRecords.SearchTerm = SearchText;
        }
    }

    private void ProjectRecordsIntoShell()
    {
        var snapshot = ProjectRecords();
        var previousKey = SelectedRecord?.Key;

        Records.Clear();
        foreach (var record in snapshot)
        {
            Records.Add(record);
        }

        RecordsView.Refresh();

        if (previousKey is not null)
        {
            SelectedRecord = Records.FirstOrDefault(r => string.Equals(r.Key, previousKey, StringComparison.Ordinal));
        }
        else if (Records.Count > 0)
        {
            SelectedRecord = Records[0];
        }
        else
        {
            SelectedRecord = null;
        }

        UpdateWorkflowCommandStates();
    }

    private IReadOnlyList<ModuleRecord> ProjectRecords()
        => _trainingRecords.FilteredTrainingRecords
            .Select(CreateRecord)
            .ToList();

    private ModuleRecord CreateRecord(TrainingRecord record)
    {
        var inspector = new List<InspectorField>
        {
            InspectorField.Create(ModuleKey, Title, record.Id.ToString(CultureInfo.InvariantCulture), record.Title, _localization.GetString("Module.TrainingRecords.Field.Type"), record.TrainingType ?? record.Type),
            InspectorField.Create(ModuleKey, Title, record.Id.ToString(CultureInfo.InvariantCulture), record.Title, _localization.GetString("Module.TrainingRecords.Field.AssignedTo"), record.AssignedToName),
            InspectorField.Create(ModuleKey, Title, record.Id.ToString(CultureInfo.InvariantCulture), record.Title, _localization.GetString("Module.TrainingRecords.Field.DueDate"), FormatDate(record.DueDate)),
            InspectorField.Create(ModuleKey, Title, record.Id.ToString(CultureInfo.InvariantCulture), record.Title, _localization.GetString("Module.TrainingRecords.Field.TrainingDate"), FormatDate(record.TrainingDate)),
            InspectorField.Create(ModuleKey, Title, record.Id.ToString(CultureInfo.InvariantCulture), record.Title, _localization.GetString("Module.TrainingRecords.Field.ExpiryDate"), FormatDate(record.ExpiryDate))
        };

        return new ModuleRecord(
            record.Id.ToString(CultureInfo.InvariantCulture),
            record.Title,
            record.Code,
            record.Status,
            record.Description,
            inspector);
    }

    private static string FormatDate(DateTime? value)
        => value.HasValue ? value.Value.ToString("d", CultureInfo.CurrentCulture) : string.Empty;

    private static string FormatDate(DateTime value)
        => value.ToString("d", CultureInfo.CurrentCulture);

    private void UpdateFilteredRecordsSubscription(INotifyCollectionChanged? next)
    {
        if (_filteredRecordsSubscription is not null)
        {
            _filteredRecordsSubscription.CollectionChanged -= OnFilteredTrainingRecordsChanged;
        }

        _filteredRecordsSubscription = next;

        if (_filteredRecordsSubscription is not null)
        {
            _filteredRecordsSubscription.CollectionChanged += OnFilteredTrainingRecordsChanged;
        }
    }

    private static Task ExecuteAsync(ICommand command)
    {
        if (command is IAsyncRelayCommand asyncRelay)
        {
            return asyncRelay.ExecuteAsync(null);
        }

        command.Execute(null);
        return Task.CompletedTask;
    }

    private WorkflowAction DetermineWorkflowAction(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return WorkflowAction.None;
        }

        return status.ToLowerInvariant() switch
        {
            "planned" or "scheduled" => WorkflowAction.Assign,
            "pending_approval" => WorkflowAction.Approve,
            "assigned" => WorkflowAction.Complete,
            "completed" => WorkflowAction.Close,
            _ => WorkflowAction.None
        };
    }

    private void UpdateWorkflowCommandStates()
    {
        _initiateCommand.NotifyCanExecuteChanged();
        _assignCommand.NotifyCanExecuteChanged();
        _approveCommand.NotifyCanExecuteChanged();
        _completeCommand.NotifyCanExecuteChanged();
        _closeCommand.NotifyCanExecuteChanged();
        _exportCommand.NotifyCanExecuteChanged();
    }

    private enum WorkflowAction
    {
        None,
        Assign,
        Approve,
        Complete,
        Close
    }

    /// <summary>Editor projection used to bind Add/Update fields in XAML.</summary>
    public sealed partial class TrainingRecordEditor : ObservableObject
    {
        private readonly Action _onChanged;

        private TrainingRecordEditor(Action onChanged)
        {
            _onChanged = onChanged;
        }

        public static TrainingRecordEditor CreateEmpty(Action onChanged)
            => FromEntity(new TrainingRecord
            {
                Title = string.Empty,
                TrainingType = "GMP",
                Status = "planned",
                AssignedToName = string.Empty,
                DueDate = DateTime.Today.AddDays(30),
                TrainingDate = DateTime.Today,
                ExpiryDate = DateTime.Today.AddMonths(12),
                Description = string.Empty,
                Note = string.Empty,
                EffectivenessCheck = false
            }, onChanged);

        public static TrainingRecordEditor FromEntity(TrainingRecord record, Action onChanged)
        {
            if (record is null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            return new TrainingRecordEditor(onChanged)
            {
                Code = record.Code,
                Title = record.Title,
                TrainingType = record.TrainingType ?? record.Type,
                Status = record.Status,
                AssignedToName = record.AssignedToName,
                DueDate = record.DueDate,
                TrainingDate = record.TrainingDate,
                ExpiryDate = record.ExpiryDate,
                Description = record.Description,
                Note = record.Note,
                EffectivenessCheck = record.EffectivenessCheck
            };
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayStatus))]
        private string _code = string.Empty;

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _trainingType = string.Empty;

        [ObservableProperty]
        private string _status = string.Empty;

        [ObservableProperty]
        private string _assignedToName = string.Empty;

        [ObservableProperty]
        private DateTime? _dueDate;

        [ObservableProperty]
        private DateTime _trainingDate = DateTime.Today;

        [ObservableProperty]
        private DateTime? _expiryDate;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private string _note = string.Empty;

        [ObservableProperty]
        private bool _effectivenessCheck;

        public string DisplayStatus => string.IsNullOrWhiteSpace(Status) ? "-" : Status;

        partial void OnCodeChanged(string value) => _onChanged();
        partial void OnTitleChanged(string value) => _onChanged();
        partial void OnTrainingTypeChanged(string value) => _onChanged();
        partial void OnStatusChanged(string value) => _onChanged();
        partial void OnAssignedToNameChanged(string value) => _onChanged();
        partial void OnDueDateChanged(DateTime? value) => _onChanged();
        partial void OnTrainingDateChanged(DateTime value) => _onChanged();
        partial void OnExpiryDateChanged(DateTime? value) => _onChanged();
        partial void OnDescriptionChanged(string value) => _onChanged();
        partial void OnNoteChanged(string value) => _onChanged();
        partial void OnEffectivenessCheckChanged(bool value) => _onChanged();

        public TrainingRecord ToEntity()
            => new()
            {
                Code = Code,
                Title = Title,
                TrainingType = TrainingType,
                Status = Status,
                AssignedToName = AssignedToName,
                DueDate = DueDate,
                TrainingDate = TrainingDate,
                ExpiryDate = ExpiryDate,
                Description = Description,
                Note = Note,
                EffectivenessCheck = EffectivenessCheck
            };

        public void ApplyTo(TrainingRecord target)
        {
            if (target is null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            target.Code = Code;
            target.Title = Title;
            target.TrainingType = TrainingType;
            target.Status = Status;
            target.AssignedToName = AssignedToName;
            target.DueDate = DueDate;
            target.TrainingDate = TrainingDate;
            target.ExpiryDate = ExpiryDate;
            target.Description = Description;
            target.Note = Note;
            target.EffectivenessCheck = EffectivenessCheck;
        }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        PropertyChanged -= OnSelfPropertyChanged;
        _trainingRecords.PropertyChanged -= OnTrainingRecordsPropertyChanged;
        UpdateFilteredRecordsSubscription(null);
    }
