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
/// Projects the shared <see cref="QualificationViewModel"/> into the WPF shell.
/// </summary>
public sealed partial class QualificationsModuleViewModel : ModuleDocumentViewModel, IDisposable
{
    /// <summary>Stable module key consumed across the shell.</summary>
    public const string ModuleKey = "Qualifications";

    private readonly QualificationViewModel _qualifications;
    private readonly ILocalizationService _localization;
    private readonly AsyncRelayCommand _addCommand;
    private readonly AsyncRelayCommand _updateCommand;
    private readonly AsyncRelayCommand _deleteCommand;
    private readonly AsyncRelayCommand _rollbackCommand;
    private readonly AsyncRelayCommand _exportCommand;
    private readonly Dictionary<string, string> _statusLookup;
    private readonly Dictionary<string, string> _typeLookup;
    private bool _suppressSearchSync;
    private bool _suppressSelectionSync;
    private INotifyCollectionChanged? _filteredSubscription;
    private EventHandler? _addCanExecuteHandler;
    private EventHandler? _updateCanExecuteHandler;
    private EventHandler? _deleteCanExecuteHandler;
    private EventHandler? _rollbackCanExecuteHandler;
    private EventHandler? _exportCanExecuteHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="QualificationsModuleViewModel"/> class.
    /// </summary>
    public QualificationsModuleViewModel(
        QualificationViewModel qualifications,
        ILocalizationService localization,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, localization.GetString("Module.Title.Qualifications"), localization, cflDialogService, shellInteraction, navigation)
    {
        _qualifications = qualifications ?? throw new ArgumentNullException(nameof(qualifications));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));

        _statusLookup = _qualifications.AvailableStatuses
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToDictionary(status => status, FormatStatusInternal, StringComparer.OrdinalIgnoreCase);

        _typeLookup = _qualifications.AvailableTypes
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToDictionary(type => type, FormatTypeInternal, StringComparer.OrdinalIgnoreCase);

        _qualifications.PropertyChanged += OnQualificationsPropertyChanged;
        SubscribeToFiltered(_qualifications.FilteredQualifications);

        _addCommand = CreateDelegatedCommand(_qualifications.AddQualificationCommand, ExecuteAddAsync, out _addCanExecuteHandler);
        _updateCommand = CreateDelegatedCommand(_qualifications.UpdateQualificationCommand, ExecuteUpdateAsync, out _updateCanExecuteHandler);
        _deleteCommand = CreateDelegatedCommand(_qualifications.DeleteQualificationCommand, ExecuteDeleteAsync, out _deleteCanExecuteHandler);
        _rollbackCommand = CreateDelegatedCommand(_qualifications.RollbackQualificationCommand, ExecuteRollbackAsync, out _rollbackCanExecuteHandler);
        _exportCommand = CreateDelegatedCommand(_qualifications.ExportQualificationsCommand, ExecuteExportAsync, out _exportCanExecuteHandler);

        PropertyChanged += OnSelfPropertyChanged;

        if (!string.IsNullOrWhiteSpace(_qualifications.SearchTerm))
        {
            _suppressSearchSync = true;
            try
            {
                SearchText = _qualifications.SearchTerm;
            }
            finally
            {
                _suppressSearchSync = false;
            }
        }

        if (_qualifications.SelectedQualification is not null)
        {
            SyncSelectionFromShared();
        }

        if (!string.IsNullOrWhiteSpace(_qualifications.StatusMessage))
        {
            StatusMessage = _qualifications.StatusMessage!;
        }

        if (IsBusy != _qualifications.IsBusy)
        {
            IsBusy = _qualifications.IsBusy;
        }
    }

    /// <summary>Shared qualification view-model surfaced for bindings.</summary>
    public QualificationViewModel Qualifications => _qualifications;

    /// <summary>Available status filters mirrored from the shared view-model.</summary>
    public IReadOnlyList<string> StatusOptions => _qualifications.AvailableStatuses;

    /// <summary>Available type filters mirrored from the shared view-model.</summary>
    public IReadOnlyList<string> TypeOptions => _qualifications.AvailableTypes;

    /// <summary>Command that adds a qualification.</summary>
    public IAsyncRelayCommand AddCommand => _addCommand;

    /// <summary>Command that updates the selected qualification.</summary>
    public IAsyncRelayCommand UpdateCommand => _updateCommand;

    /// <summary>Command that deletes the selected qualification.</summary>
    public IAsyncRelayCommand DeleteCommand => _deleteCommand;

    /// <summary>Command that rolls back the selected qualification.</summary>
    public IAsyncRelayCommand RollbackCommand => _rollbackCommand;

    /// <summary>Command that exports the current filter set.</summary>
    public IAsyncRelayCommand ExportCommand => _exportCommand;

    /// <summary>Status filter forwarded to the shared view-model.</summary>
    public string? StatusFilter
    {
        get => _qualifications.StatusFilter;
        set
        {
            if (!string.Equals(_qualifications.StatusFilter, value, StringComparison.Ordinal))
            {
                _qualifications.StatusFilter = value;
                OnPropertyChanged();
                ProjectRecordsIntoShell();
            }
        }
    }

    /// <summary>Type filter forwarded to the shared view-model.</summary>
    public string? TypeFilter
    {
        get => _qualifications.TypeFilter;
        set
        {
            if (!string.Equals(_qualifications.TypeFilter, value, StringComparison.Ordinal))
            {
                _qualifications.TypeFilter = value;
                OnPropertyChanged();
                ProjectRecordsIntoShell();
            }
        }
    }

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        await _qualifications.LoadQualificationsAsync().ConfigureAwait(false);
        return ProjectRecords();
    }

    /// <inheritdoc />
    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
    {
        var today = DateTime.Today;
        return new List<ModuleRecord>
        {
            CreateRecord(new Qualification
            {
                Id = 2001,
                Code = "IQ-2026-001",
                Type = "IQ",
                Description = "Initial qualification for filling line",
                Status = "valid",
                Date = today.AddDays(-14),
                ExpiryDate = today.AddYears(1),
                CertificateNumber = "CERT-2026-01",
                Machine = new Machine { Name = "Filling Line 1" },
                QualifiedBy = new User { FullName = "QA Lead" },
                ApprovedBy = new User { FullName = "Quality Director" },
                ApprovedAt = today.AddDays(-7)
            }),
            CreateRecord(new Qualification
            {
                Id = 2002,
                Code = "OQ-2026-004",
                Type = "OQ",
                Description = "Operational qualification for autoclave",
                Status = "expired",
                Date = today.AddYears(-2),
                ExpiryDate = today.AddDays(-10),
                CertificateNumber = "CERT-2024-19",
                Machine = new Machine { Name = "Autoclave A" },
                QualifiedBy = new User { FullName = "Validation Engineer" },
                ApprovedBy = new User { FullName = "QA Manager" },
                ApprovedAt = today.AddYears(-2).AddDays(3)
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
                _qualifications.SelectedQualification = null;
            }
            else
            {
                var match = TryFindQualification(record.Key);
                _qualifications.SelectedQualification = match;
            }
        }
        finally
        {
            _suppressSelectionSync = false;
            UpdateWorkflowCommandStates();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        PropertyChanged -= OnSelfPropertyChanged;
        _qualifications.PropertyChanged -= OnQualificationsPropertyChanged;

        UnsubscribeFromFiltered();
        DetachCommand(_qualifications.AddQualificationCommand, _addCanExecuteHandler);
        DetachCommand(_qualifications.UpdateQualificationCommand, _updateCanExecuteHandler);
        DetachCommand(_qualifications.DeleteQualificationCommand, _deleteCanExecuteHandler);
        DetachCommand(_qualifications.RollbackQualificationCommand, _rollbackCanExecuteHandler);
        DetachCommand(_qualifications.ExportQualificationsCommand, _exportCanExecuteHandler);
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

    private Task ExecuteAddAsync() => ExecuteDelegatedAsync(_qualifications.AddQualificationCommand);

    private Task ExecuteUpdateAsync() => ExecuteDelegatedAsync(_qualifications.UpdateQualificationCommand);

    private Task ExecuteDeleteAsync() => ExecuteDelegatedAsync(_qualifications.DeleteQualificationCommand);

    private Task ExecuteRollbackAsync() => ExecuteDelegatedAsync(_qualifications.RollbackQualificationCommand);

    private Task ExecuteExportAsync() => ExecuteDelegatedAsync(_qualifications.ExportQualificationsCommand);

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
        if (!string.Equals(e.PropertyName, nameof(SearchText), StringComparison.Ordinal))
        {
            return;
        }

        if (_suppressSearchSync)
        {
            return;
        }

        _suppressSearchSync = true;
        try
        {
            _qualifications.SearchTerm = SearchText;
        }
        finally
        {
            _suppressSearchSync = false;
        }
    }

    private void OnQualificationsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(QualificationViewModel.FilteredQualifications):
                SubscribeToFiltered(_qualifications.FilteredQualifications);
                ProjectRecordsIntoShell();
                break;
            case nameof(QualificationViewModel.SelectedQualification):
                SyncSelectionFromShared();
                UpdateWorkflowCommandStates();
                break;
            case nameof(QualificationViewModel.SearchTerm):
                if (_suppressSearchSync)
                {
                    break;
                }

                if (!string.Equals(SearchText, _qualifications.SearchTerm, StringComparison.Ordinal))
                {
                    _suppressSearchSync = true;
                    try
                    {
                        SearchText = _qualifications.SearchTerm;
                    }
                    finally
                    {
                        _suppressSearchSync = false;
                    }
                }

                break;
            case nameof(QualificationViewModel.StatusFilter):
                OnPropertyChanged(nameof(StatusFilter));
                ProjectRecordsIntoShell();
                break;
            case nameof(QualificationViewModel.TypeFilter):
                OnPropertyChanged(nameof(TypeFilter));
                ProjectRecordsIntoShell();
                break;
            case nameof(QualificationViewModel.StatusMessage):
                if (!string.Equals(StatusMessage, _qualifications.StatusMessage, StringComparison.Ordinal))
                {
                    StatusMessage = _qualifications.StatusMessage ?? string.Empty;
                }

                break;
            case nameof(QualificationViewModel.IsBusy):
                if (IsBusy != _qualifications.IsBusy)
                {
                    IsBusy = _qualifications.IsBusy;
                }

                UpdateWorkflowCommandStates();
                break;
        }
    }

    private void SubscribeToFiltered(ObservableCollection<Qualification>? collection)
    {
        UnsubscribeFromFiltered();
        _filteredSubscription = collection;

        if (_filteredSubscription is not null)
        {
            _filteredSubscription.CollectionChanged += OnFilteredQualificationsChanged;
        }
    }

    private void UnsubscribeFromFiltered()
    {
        if (_filteredSubscription is not null)
        {
            _filteredSubscription.CollectionChanged -= OnFilteredQualificationsChanged;
            _filteredSubscription = null;
        }
    }

    private void OnFilteredQualificationsChanged(object? sender, NotifyCollectionChangedEventArgs e)
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
        => _qualifications.FilteredQualifications
            .Select(CreateRecord)
            .ToList();

    private ModuleRecord CreateRecord(Qualification qualification)
    {
        var key = qualification.Id.ToString(CultureInfo.InvariantCulture);
        var title = BuildTitle(qualification);
        var inspector = new List<InspectorField>
        {
            InspectorField.Create(ModuleKey, Title, key, title, _localization.GetString("Module.Qualifications.Field.Equipment"), qualification.EquipmentName),
            InspectorField.Create(ModuleKey, Title, key, title, _localization.GetString("Module.Qualifications.Field.Type"), FormatType(qualification.Type)),
            InspectorField.Create(ModuleKey, Title, key, title, _localization.GetString("Module.Qualifications.Field.Certificate"), qualification.CertificateNumber),
            InspectorField.Create(ModuleKey, Title, key, title, _localization.GetString("Module.Qualifications.Field.EffectiveDate"), FormatDate(qualification.Date)),
            InspectorField.Create(ModuleKey, Title, key, title, _localization.GetString("Module.Qualifications.Field.DueDate"), FormatDate(qualification.ExpiryDate)),
            InspectorField.Create(ModuleKey, Title, key, title, _localization.GetString("Module.Qualifications.Field.QualifiedBy"), qualification.QualifiedBy?.FullName ?? qualification.QualifiedBy?.Username),
            InspectorField.Create(ModuleKey, Title, key, title, _localization.GetString("Module.Qualifications.Field.ApprovedBy"), qualification.ApprovedBy?.FullName ?? qualification.ApprovedBy?.Username),
            InspectorField.Create(ModuleKey, Title, key, title, _localization.GetString("Module.Qualifications.Field.Status"), FormatStatus(qualification.Status))
        };

        return new ModuleRecord(
            key,
            title,
            qualification.Code,
            FormatStatus(qualification.Status),
            qualification.Description,
            inspector);
    }

    private Qualification? TryFindQualification(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        if (int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
        {
            return _qualifications.Qualifications.FirstOrDefault(q => q.Id == id);
        }

        return _qualifications.Qualifications.FirstOrDefault(q => string.Equals(q.Code, key, StringComparison.OrdinalIgnoreCase));
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
            if (_qualifications.SelectedQualification is null)
            {
                SelectedRecord = null;
                return;
            }

            var key = _qualifications.SelectedQualification.Id.ToString(CultureInfo.InvariantCulture);
            var record = Records.FirstOrDefault(r => string.Equals(r.Key, key, StringComparison.Ordinal));

            if (record is null)
            {
                record = CreateRecord(_qualifications.SelectedQualification);
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
        _addCommand.NotifyCanExecuteChanged();
        _updateCommand.NotifyCanExecuteChanged();
        _deleteCommand.NotifyCanExecuteChanged();
        _rollbackCommand.NotifyCanExecuteChanged();
        _exportCommand.NotifyCanExecuteChanged();
    }

    private string BuildTitle(Qualification qualification)
    {
        var equipment = string.IsNullOrWhiteSpace(qualification.EquipmentName)
            ? _localization.GetString("Module.Qualifications.Title.UnknownEquipment")
            : qualification.EquipmentName;

        return string.Format(
            CultureInfo.CurrentCulture,
            "{0} — {1}",
            FormatType(qualification.Type),
            equipment);
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

        return FormatStatusInternal(status);
    }

    private string FormatType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return string.Empty;
        }

        if (_typeLookup.TryGetValue(type, out var localized))
        {
            return localized;
        }

        return FormatTypeInternal(type);
    }

    private string FormatStatusInternal(string status)
    {
        var key = status switch
        {
            "valid" => "Module.Qualifications.Status.Valid",
            "expired" => "Module.Qualifications.Status.Expired",
            "scheduled" => "Module.Qualifications.Status.Scheduled",
            "in_progress" => "Module.Qualifications.Status.InProgress",
            "rejected" => "Module.Qualifications.Status.Rejected",
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(key))
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(status.Replace('_', ' '));
        }

        return _localization.GetString(key);
    }

    private string FormatTypeInternal(string type)
    {
        var key = type.ToUpperInvariant() switch
        {
            "IQ" => "Module.Qualifications.Type.IQ",
            "OQ" => "Module.Qualifications.Type.OQ",
            "PQ" => "Module.Qualifications.Type.PQ",
            "DQ" => "Module.Qualifications.Type.DQ",
            "VQ" => "Module.Qualifications.Type.VQ",
            "SAT" => "Module.Qualifications.Type.SAT",
            "FAT" => "Module.Qualifications.Type.FAT",
            "REQUALIFICATION" => "Module.Qualifications.Type.Requalification",
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(key))
        {
            return type;
        }

        return _localization.GetString(key);
    }

    private static string FormatDate(DateTime? value)
        => value.HasValue ? value.Value.ToString("d", CultureInfo.CurrentCulture) : string.Empty;

    private static void DetachCommand(System.Windows.Input.ICommand command, EventHandler? handler)
    {
        if (handler is null)
        {
            return;
        }

        command.CanExecuteChanged -= handler;
    }
}
