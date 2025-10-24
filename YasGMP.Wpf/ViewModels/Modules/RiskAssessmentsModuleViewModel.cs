using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Models;
using YasGMP.ViewModels;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>
/// Projects the shared <see cref="RiskAssessmentViewModel"/> into the WPF quality workspace.
/// </summary>
public sealed partial class RiskAssessmentsModuleViewModel : ModuleDocumentViewModel, IDisposable
{
    /// <summary>Stable module key consumed by the registry, navigation, and inspector panes.</summary>
    public const string ModuleKey = "RiskAssessments";

    private readonly RiskAssessmentViewModel _riskAssessments;
    private readonly ILocalizationService _localization;
    private readonly AsyncRelayCommand _initiateCommand;
    private readonly AsyncRelayCommand _updateCommand;
    private readonly AsyncRelayCommand _approveCommand;
    private readonly AsyncRelayCommand _closeCommand;
    private readonly AsyncRelayCommand _exportCommand;
    private readonly Dictionary<string, string> _statusLookup;
    private bool _suppressSearchSync;
    private bool _suppressSelectionSync;
    private INotifyCollectionChanged? _filteredSubscription;
    private EventHandler? _initiateCanExecuteHandler;
    private EventHandler? _updateCanExecuteHandler;
    private EventHandler? _approveCanExecuteHandler;
    private EventHandler? _closeCanExecuteHandler;
    private EventHandler? _exportCanExecuteHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="RiskAssessmentsModuleViewModel"/> class.
    /// </summary>
    public RiskAssessmentsModuleViewModel(
        RiskAssessmentViewModel riskAssessments,
        ILocalizationService localization,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, localization.GetString("Module.Title.RiskAssessments"), localization, cflDialogService, shellInteraction, navigation)
    {
        _riskAssessments = riskAssessments ?? throw new ArgumentNullException(nameof(riskAssessments));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));

        _statusLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["initiated"] = _localization.GetString("Module.RiskAssessments.Status.Initiated"),
            ["in_progress"] = _localization.GetString("Module.RiskAssessments.Status.InProgress"),
            ["pending_approval"] = _localization.GetString("Module.RiskAssessments.Status.PendingApproval"),
            ["effectiveness_check"] = _localization.GetString("Module.RiskAssessments.Status.EffectivenessCheck"),
            ["closed"] = _localization.GetString("Module.RiskAssessments.Status.Closed"),
            ["rejected"] = _localization.GetString("Module.RiskAssessments.Status.Rejected")
        };

        _riskAssessments.PropertyChanged += OnRiskAssessmentsPropertyChanged;
        SubscribeToFiltered(_riskAssessments.FilteredRiskAssessments);

        _initiateCommand = CreateDelegatedCommand(_riskAssessments.InitiateRiskAssessmentCommand, ExecuteInitiateAsync, out _initiateCanExecuteHandler);
        _updateCommand = CreateDelegatedCommand(_riskAssessments.UpdateRiskAssessmentCommand, ExecuteUpdateAsync, out _updateCanExecuteHandler);
        _approveCommand = CreateDelegatedCommand(_riskAssessments.ApproveRiskAssessmentCommand, ExecuteApproveAsync, out _approveCanExecuteHandler);
        _closeCommand = CreateDelegatedCommand(_riskAssessments.CloseRiskAssessmentCommand, ExecuteCloseAsync, out _closeCanExecuteHandler);
        _exportCommand = CreateDelegatedCommand(_riskAssessments.ExportRiskAssessmentsCommand, ExecuteExportAsync, out _exportCanExecuteHandler);

        PropertyChanged += OnSelfPropertyChanged;

        if (!string.IsNullOrWhiteSpace(_riskAssessments.SearchTerm))
        {
            _suppressSearchSync = true;
            try
            {
                SearchText = _riskAssessments.SearchTerm;
            }
            finally
            {
                _suppressSearchSync = false;
            }
        }

        if (_riskAssessments.SelectedRiskAssessment is not null)
        {
            SyncSelectionFromShared();
        }
    }

    /// <summary>Shared risk assessment view-model surfaced for bindings.</summary>
    public RiskAssessmentViewModel RiskAssessments => _riskAssessments;

    /// <summary>Available status filter values mirrored from the shared view-model.</summary>
    public IReadOnlyList<string> StatusOptions => _riskAssessments.AvailableStatuses;

    /// <summary>Available category filter values mirrored from the shared view-model.</summary>
    public IReadOnlyList<string> CategoryOptions => _riskAssessments.AvailableCategories;

    /// <summary>Command that initiates a new risk assessment.</summary>
    public IAsyncRelayCommand InitiateCommand => _initiateCommand;

    /// <summary>Command that updates the selected risk assessment.</summary>
    public IAsyncRelayCommand UpdateCommand => _updateCommand;

    /// <summary>Command that approves the selected risk assessment.</summary>
    public IAsyncRelayCommand ApproveCommand => _approveCommand;

    /// <summary>Command that closes the selected risk assessment.</summary>
    public IAsyncRelayCommand CloseCommand => _closeCommand;

    /// <summary>Command that exports the filtered risk assessments.</summary>
    public IAsyncRelayCommand ExportCommand => _exportCommand;

    /// <summary>Status filter forwarded to the shared MAUI view-model.</summary>
    public string? StatusFilter
    {
        get => _riskAssessments.StatusFilter;
        set
        {
            if (!string.Equals(_riskAssessments.StatusFilter, value, StringComparison.Ordinal))
            {
                _riskAssessments.StatusFilter = value;
                OnPropertyChanged();
                ProjectRecordsIntoShell();
            }
        }
    }

    /// <summary>Category filter forwarded to the shared MAUI view-model.</summary>
    public string? CategoryFilter
    {
        get => _riskAssessments.CategoryFilter;
        set
        {
            if (!string.Equals(_riskAssessments.CategoryFilter, value, StringComparison.Ordinal))
            {
                _riskAssessments.CategoryFilter = value;
                OnPropertyChanged();
                ProjectRecordsIntoShell();
            }
        }
    }

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        await _riskAssessments.LoadRiskAssessmentsAsync().ConfigureAwait(false);
        return ProjectRecords();
    }

    /// <inheritdoc />
    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
    {
        var today = DateTime.Today;
        return new List<ModuleRecord>
        {
            CreateRecord(new RiskAssessment
            {
                Id = 101,
                Code = "RA-2026-001",
                Title = "Deviation risk review",
                Status = "in_progress",
                Category = "process",
                AssessedAt = today.AddDays(-2),
                RiskLevel = "High",
                RiskScore = 84,
                Owner = new User { FullName = "QA Specialist" },
                ReviewDate = today.AddMonths(6),
                Mitigation = "Containment in progress"
            }),
            CreateRecord(new RiskAssessment
            {
                Id = 102,
                Code = "RA-2026-002",
                Title = "Supplier qualification",
                Status = "pending_approval",
                Category = "supplier",
                AssessedAt = today.AddDays(-10),
                RiskLevel = "Medium",
                RiskScore = 45,
                Owner = new User { FullName = "Supply Chain" },
                ReviewDate = today.AddMonths(3),
                Mitigation = "Approval workflow pending"
            })
        };
    }

    /// <inheritdoc />
    protected override Task OnRecordSelectedAsync(ModuleRecord? record)
    {
        if (_suppressSelectionSync)
        {
            return Task.CompletedTask;
        }

        _suppressSelectionSync = true;
        try
        {
            if (record is null)
            {
                _riskAssessments.SelectedRiskAssessment = null;
            }
            else
            {
                var match = TryFindAssessment(record.Key);
                _riskAssessments.SelectedRiskAssessment = match;
            }
        }
        finally
        {
            _suppressSelectionSync = false;
        }

        UpdateWorkflowCommandStates();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        PropertyChanged -= OnSelfPropertyChanged;
        _riskAssessments.PropertyChanged -= OnRiskAssessmentsPropertyChanged;

        UnsubscribeFromFiltered();
        DetachCommand(_riskAssessments.InitiateRiskAssessmentCommand, _initiateCanExecuteHandler);
        DetachCommand(_riskAssessments.UpdateRiskAssessmentCommand, _updateCanExecuteHandler);
        DetachCommand(_riskAssessments.ApproveRiskAssessmentCommand, _approveCanExecuteHandler);
        DetachCommand(_riskAssessments.CloseRiskAssessmentCommand, _closeCanExecuteHandler);
        DetachCommand(_riskAssessments.ExportRiskAssessmentsCommand, _exportCanExecuteHandler);
    }

    /// <inheritdoc />
    protected override bool MatchesSearch(ModuleRecord record, string searchText)
        => base.MatchesSearch(record, searchText)
           || record.InspectorFields.Any(field => field.Value.Contains(searchText, StringComparison.OrdinalIgnoreCase));

    private AsyncRelayCommand CreateDelegatedCommand(
        System.Windows.Input.ICommand source,
        Func<Task> execute,
        out EventHandler? canExecuteHandler)
    {
        var command = new AsyncRelayCommand(execute, () => source?.CanExecute(null) ?? false);
        canExecuteHandler = (s, e) => command.NotifyCanExecuteChanged();
        source.CanExecuteChanged += canExecuteHandler;
        return command;
    }

    private Task ExecuteInitiateAsync() => ExecuteDelegatedAsync(_riskAssessments.InitiateRiskAssessmentCommand);

    private Task ExecuteUpdateAsync() => ExecuteDelegatedAsync(_riskAssessments.UpdateRiskAssessmentCommand);

    private Task ExecuteApproveAsync() => ExecuteDelegatedAsync(_riskAssessments.ApproveRiskAssessmentCommand);

    private Task ExecuteCloseAsync() => ExecuteDelegatedAsync(_riskAssessments.CloseRiskAssessmentCommand);

    private Task ExecuteExportAsync() => ExecuteDelegatedAsync(_riskAssessments.ExportRiskAssessmentsCommand);

    private static Task ExecuteDelegatedAsync(System.Windows.Input.ICommand command)
    {
        if (command is IAsyncRelayCommand asyncRelay)
        {
            return asyncRelay.ExecuteAsync(null);
        }

        if (command.CanExecute(null))
        {
            command.Execute(null);
        }

        return Task.CompletedTask;
    }

    private void OnSelfPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(SearchText), StringComparison.Ordinal))
        {
            if (_suppressSearchSync)
            {
                return;
            }

            _suppressSearchSync = true;
            try
            {
                _riskAssessments.SearchTerm = SearchText;
            }
            finally
            {
                _suppressSearchSync = false;
            }
        }
    }

    private void OnRiskAssessmentsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(RiskAssessmentViewModel.FilteredRiskAssessments):
                SubscribeToFiltered(_riskAssessments.FilteredRiskAssessments);
                ProjectRecordsIntoShell();
                break;
            case nameof(RiskAssessmentViewModel.SelectedRiskAssessment):
                SyncSelectionFromShared();
                UpdateWorkflowCommandStates();
                break;
            case nameof(RiskAssessmentViewModel.SearchTerm):
                if (!_suppressSearchSync && !string.Equals(SearchText, _riskAssessments.SearchTerm, StringComparison.Ordinal))
                {
                    _suppressSearchSync = true;
                    try
                    {
                        SearchText = _riskAssessments.SearchTerm;
                    }
                    finally
                    {
                        _suppressSearchSync = false;
                    }
                }

                break;
            case nameof(RiskAssessmentViewModel.StatusFilter):
                OnPropertyChanged(nameof(StatusFilter));
                ProjectRecordsIntoShell();
                break;
            case nameof(RiskAssessmentViewModel.CategoryFilter):
                OnPropertyChanged(nameof(CategoryFilter));
                ProjectRecordsIntoShell();
                break;
            case nameof(RiskAssessmentViewModel.StatusMessage):
                if (!string.Equals(StatusMessage, _riskAssessments.StatusMessage, StringComparison.Ordinal))
                {
                    StatusMessage = _riskAssessments.StatusMessage ?? string.Empty;
                }

                break;
            case nameof(RiskAssessmentViewModel.IsBusy):
                if (IsBusy != _riskAssessments.IsBusy)
                {
                    IsBusy = _riskAssessments.IsBusy;
                }

                UpdateWorkflowCommandStates();
                break;
        }
    }

    private void SubscribeToFiltered(ObservableCollection<RiskAssessment>? collection)
    {
        UnsubscribeFromFiltered();
        _filteredSubscription = collection;

        if (_filteredSubscription is not null)
        {
            _filteredSubscription.CollectionChanged += OnFilteredRiskAssessmentsChanged;
        }
    }

    private void UnsubscribeFromFiltered()
    {
        if (_filteredSubscription is not null)
        {
            _filteredSubscription.CollectionChanged -= OnFilteredRiskAssessmentsChanged;
            _filteredSubscription = null;
        }
    }

    private void OnFilteredRiskAssessmentsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ProjectRecordsIntoShell();
        UpdateWorkflowCommandStates();
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
    }

    private IReadOnlyList<ModuleRecord> ProjectRecords()
        => _riskAssessments.FilteredRiskAssessments
            .Select(CreateRecord)
            .ToList();

    private ModuleRecord CreateRecord(RiskAssessment assessment)
    {
        var key = assessment.Id.ToString(CultureInfo.InvariantCulture);
        var inspector = new List<InspectorField>
        {
            InspectorField.Create(ModuleKey, Title, key, assessment.Title, _localization.GetString("Module.RiskAssessments.Field.Category"), assessment.Category),
            InspectorField.Create(ModuleKey, Title, key, assessment.Title, _localization.GetString("Module.RiskAssessments.Field.Owner"), assessment.Owner?.FullName ?? assessment.Owner?.Username),
            InspectorField.Create(ModuleKey, Title, key, assessment.Title, _localization.GetString("Module.RiskAssessments.Field.RiskLevel"), assessment.RiskLevel),
            InspectorField.Create(ModuleKey, Title, key, assessment.Title, _localization.GetString("Module.RiskAssessments.Field.RiskScore"), FormatRiskScore(assessment.RiskScore)),
            InspectorField.Create(ModuleKey, Title, key, assessment.Title, _localization.GetString("Module.RiskAssessments.Field.ReviewDate"), FormatDate(assessment.ReviewDate)),
            InspectorField.Create(ModuleKey, Title, key, assessment.Title, _localization.GetString("Module.RiskAssessments.Field.Status"), FormatStatus(assessment.Status)),
            InspectorField.Create(ModuleKey, Title, key, assessment.Title, _localization.GetString("Module.RiskAssessments.Field.AssessedAt"), FormatDateTime(assessment.AssessedAt))
        };

        return new ModuleRecord(
            key,
            assessment.Title,
            assessment.Code,
            FormatStatus(assessment.Status),
            assessment.Description,
            inspector);
    }

    private RiskAssessment? TryFindAssessment(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        if (int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
        {
            return _riskAssessments.RiskAssessments.FirstOrDefault(r => r.Id == id);
        }

        return _riskAssessments.RiskAssessments.FirstOrDefault(r => string.Equals(r.Code, key, StringComparison.OrdinalIgnoreCase));
    }

    private void SyncSelectionFromShared()
    {
        if (_suppressSelectionSync)
        {
            return;
        }

        _suppressSelectionSync = true;
        try
        {
            if (_riskAssessments.SelectedRiskAssessment is null)
            {
                SelectedRecord = null;
                return;
            }

            var key = _riskAssessments.SelectedRiskAssessment.Id.ToString(CultureInfo.InvariantCulture);
            var record = Records.FirstOrDefault(r => string.Equals(r.Key, key, StringComparison.Ordinal));

            if (record is null)
            {
                record = CreateRecord(_riskAssessments.SelectedRiskAssessment);
                Records.Add(record);
                RecordsView.Refresh();
            }

            SelectedRecord = record;
        }
        finally
        {
            _suppressSelectionSync = false;
        }
    }

    private void UpdateWorkflowCommandStates()
    {
        _initiateCommand.NotifyCanExecuteChanged();
        _updateCommand.NotifyCanExecuteChanged();
        _approveCommand.NotifyCanExecuteChanged();
        _closeCommand.NotifyCanExecuteChanged();
        _exportCommand.NotifyCanExecuteChanged();
    }

    private string FormatStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return string.Empty;
        }

        if (_statusLookup.TryGetValue(status, out var localized))
        {
            return localized;
        }

        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(status.Replace('_', ' '));
    }

    private static string FormatRiskScore(int? score)
        => score.HasValue ? score.Value.ToString(CultureInfo.CurrentCulture) : string.Empty;

    private static string FormatDate(DateTime? value)
        => value.HasValue ? value.Value.ToString("d", CultureInfo.CurrentCulture) : string.Empty;

    private static string FormatDateTime(DateTime? value)
        => value.HasValue ? value.Value.ToString("g", CultureInfo.CurrentCulture) : string.Empty;

    private static void DetachCommand(System.Windows.Input.ICommand command, EventHandler? handler)
    {
        if (handler is null)
        {
            return;
        }

        command.CanExecuteChanged -= handler;
    }
}
