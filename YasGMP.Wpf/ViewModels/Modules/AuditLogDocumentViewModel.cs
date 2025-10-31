using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Services;
using YasGMP.ViewModels;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>Projects the MAUI <see cref="AuditLogViewModel"/> into the WPF shell with SAP B1 tooling.</summary>
/// <remarks>
/// Form Modes: Operates in Find/View modes to query audit history; Add/Update remain present for parity but the log is immutable.
/// Audit &amp; Logging: Reads the consolidated audit log through the underlying MAUI view model and export services without generating new entries.
/// Localization: Uses inline button captions such as `"Apply Filter"`, `"Export CSV"`, and propagated status text; resource keys are pending.
/// Navigation: ModuleKey `Audit` integrates with Golden Arrow routing when other modules reference audit trails, and status strings stay aligned with the shell's status bar.
/// </remarks>
public sealed partial class AuditLogDocumentViewModel : ModuleDocumentViewModel
{
    public new const string ModuleKey = "Audit";

    private readonly AuditLogViewModel _auditLogViewModel;
    private readonly AsyncRelayCommand _applyFilterCommand;
    private readonly AsyncRelayCommand _refreshCommand;
    private readonly AsyncRelayCommand _exportCsvCommand;
    private readonly AsyncRelayCommand _exportXlsxCommand;
    private readonly AsyncRelayCommand _exportPdfCommand;
    private bool _suppressProjection;

    [ObservableProperty]
    private bool _hasResults;

    [ObservableProperty]
    private bool _hasError;

    public AuditLogDocumentViewModel(
        AuditService auditService,
        ExportService exportService,
        AuditLogViewModel auditLogViewModel,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "Audit Trail", cflDialogService, shellInteraction, navigation)
    {
        _ = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _ = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _auditLogViewModel = auditLogViewModel ?? throw new ArgumentNullException(nameof(auditLogViewModel));

        _auditLogViewModel.FilteredEvents.CollectionChanged += OnFilteredEventsChanged;
        _auditLogViewModel.PropertyChanged += OnAuditLogPropertyChanged;

        _applyFilterCommand = new AsyncRelayCommand(_ => ExecuteApplyFilterAsync(), CanExecuteNonBusyCommand);
        _refreshCommand = new AsyncRelayCommand(_ => ExecuteRefreshAsync(), CanExecuteNonBusyCommand);
        _exportCsvCommand = new AsyncRelayCommand(_ => ExecuteExportAsync(_auditLogViewModel.ExportCsvCommand, "CSV"), CanExecuteExport);
        _exportXlsxCommand = new AsyncRelayCommand(_ => ExecuteExportAsync(_auditLogViewModel.ExportXlsxCommand, "Excel"), CanExecuteExport);
        _exportPdfCommand = new AsyncRelayCommand(_ => ExecuteExportAsync(_auditLogViewModel.ExportPdfCommand, "PDF"), CanExecuteExport);

        var refreshEntry = Toolbar
            .Select((command, index) => (Command: command, Index: index))
            .FirstOrDefault(tuple => string.Equals(tuple.Command.Caption, "Refresh", StringComparison.OrdinalIgnoreCase));

        if (refreshEntry.Command is not null)
        {
            Toolbar.Remove(refreshEntry.Command);
            Toolbar.Insert(refreshEntry.Index, new ModuleToolbarCommand("Refresh", _refreshCommand));
        }
        else
        {
            Toolbar.Add(new ModuleToolbarCommand("Refresh", _refreshCommand));
        }

        Toolbar.Add(new ModuleToolbarCommand("Apply Filter", _applyFilterCommand));
        Toolbar.Add(new ModuleToolbarCommand("Export CSV", _exportCsvCommand));
        Toolbar.Add(new ModuleToolbarCommand("Export Excel", _exportXlsxCommand));
        Toolbar.Add(new ModuleToolbarCommand("Export PDF", _exportPdfCommand));

        PropertyChanged += OnSelfPropertyChanged;
    }

    public AuditLogViewModel AuditLog => _auditLogViewModel;

    public IAsyncRelayCommand ApplyFilterCommand => _applyFilterCommand;

    public new IAsyncRelayCommand RefreshCommand => _refreshCommand;

    public IAsyncRelayCommand ExportCsvCommand => _exportCsvCommand;

    public IAsyncRelayCommand ExportXlsxCommand => _exportXlsxCommand;

    public IAsyncRelayCommand ExportPdfCommand => _exportPdfCommand;

    public new async Task InitializeAsync(object? parameter = null)
    {
        ProjectRecordsFromEvents();
        SyncStatusFromAuditLog();
        await base.InitializeAsync(parameter).ConfigureAwait(false);
    }

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        try
        {
            _suppressProjection = true;
            await _auditLogViewModel.LoadAsync().ConfigureAwait(false);
            var records = ProjectRecords();
            HasError = false;
            HasResults = records.Count > 0;
            return ToReadOnlyList(records);
        }
        catch
        {
            HasResults = false;
            HasError = true;
            throw;
        }
        finally
        {
            _suppressProjection = false;
            ProjectRecordsFromEvents();
            SyncStatusFromAuditLog();
        }
    }

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
    {
        var now = DateTime.Now;
        return new List<ModuleRecord>
        {
            MapToRecord(new SystemEvent
            {
                Id = 1,
                EventTime = now.AddMinutes(-10),
                EventType = "LOGIN",
                TableName = "system",
                RecordId = 0,
                UserId = 5,
                Description = "User darko logged in",
                DeviceInfo = "Win11 | QA-WS01",
                SourceIp = "192.168.10.5"
            }),
            MapToRecord(new SystemEvent
            {
                Id = 2,
                EventTime = now.AddMinutes(-4),
                EventType = "UPDATE",
                TableName = "work_orders",
                RecordId = 1204,
                UserId = 5,
                Description = "Updated WO-1204 status from Planned to Released",
                DeviceInfo = "Win11 | QA-WS01",
                SourceIp = "192.168.10.5"
            }),
            MapToRecord(new SystemEvent
            {
                Id = 3,
                EventTime = now.AddMinutes(-1),
                EventType = "SIGN",
                TableName = "calibration_runs",
                RecordId = 88,
                UserId = 7,
                Description = "Calibration run 88 signed by jane",
                DeviceInfo = "Win11 | QA-WS02",
                SourceIp = "192.168.10.6"
            })
        };
    }

    private Task ExecuteApplyFilterAsync()
    {
        if (!CanExecuteNonBusyCommand())
        {
            return Task.CompletedTask;
        }

        try
        {
            IsBusy = true;
            _auditLogViewModel.ApplyFilterCommand?.Execute(null);
            HasError = false;
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = $"Failed to apply audit filters: {ex.Message}";
        }
        finally
        {
            ProjectRecordsFromEvents();
            SyncStatusFromAuditLog();
            IsBusy = false;
            UpdateCommandStates();
        }

        return Task.CompletedTask;
    }

    private async Task ExecuteRefreshAsync()
    {
        if (!CanExecuteNonBusyCommand())
        {
            return;
        }

        try
        {
            await base.RefreshCommand.ExecuteAsync(null).ConfigureAwait(false);
        }
        finally
        {
            SyncStatusFromAuditLog();
            UpdateCommandStates();
        }
    }

    private async Task ExecuteExportAsync(System.Windows.Input.ICommand? sourceCommand, string formatLabel)
    {
        if (!CanExecuteExport())
        {
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = $"Exporting audit log to {formatLabel}...";

            switch (sourceCommand)
            {
                case IAsyncRelayCommand asyncRelay:
                    await asyncRelay.ExecuteAsync(null).ConfigureAwait(false);
                    break;
                case IRelayCommand relay:
                    relay.Execute(null);
                    break;
                default:
                    sourceCommand?.Execute(null);
                    break;
            }

            HasError = false;
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = $"Failed to export audit log to {formatLabel}: {ex.Message}";
        }
        finally
        {
            ProjectRecordsFromEvents();
            SyncStatusFromAuditLog();
            IsBusy = false;
            UpdateCommandStates();
        }
    }

    private bool CanExecuteNonBusyCommand() => !IsBusy;

    private bool CanExecuteExport()
        => CanExecuteNonBusyCommand() && _auditLogViewModel.FilteredEvents.Count > 0 && !_auditLogViewModel.IsBusy;

    private List<ModuleRecord> ProjectRecords()
        => _auditLogViewModel.FilteredEvents.Select(MapToRecord).ToList();

    private static IReadOnlyList<ModuleRecord> ToReadOnlyList(List<ModuleRecord> records)
        => records.AsReadOnly();

    private void ProjectRecordsFromEvents()
    {
        if (_suppressProjection)
        {
            return;
        }

        var previousKey = SelectedRecord?.Key;
        Records.Clear();
        foreach (var record in ProjectRecords())
        {
            Records.Add(record);
        }

        SelectedRecord = previousKey is not null
            ? Records.FirstOrDefault(r => string.Equals(r.Key, previousKey, StringComparison.Ordinal))
            : Records.FirstOrDefault();

        HasResults = Records.Count > 0;
    }

    private static ModuleRecord MapToRecord(SystemEvent auditEvent)
    {
        var timestamp = auditEvent.EventTime == DateTime.MinValue
            ? string.Empty
            : auditEvent.EventTime.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);

        var user = auditEvent.UserId.HasValue
            ? $"User #{auditEvent.UserId.Value.ToString(CultureInfo.InvariantCulture)}"
            : "System";

        var entity = string.IsNullOrWhiteSpace(auditEvent.TableName)
            ? "system"
            : auditEvent.TableName!;

        if (auditEvent.RecordId.HasValue)
        {
            entity += $" #{auditEvent.RecordId.Value.ToString(CultureInfo.InvariantCulture)}";
        }

        var inspector = new List<InspectorField>
        {
            new("Timestamp", timestamp),
            new("User", user),
            new("Entity/Table", entity),
            new("Action", auditEvent.EventType ?? string.Empty),
            new("Device", auditEvent.DeviceInfo ?? string.Empty),
            new("IP Address", auditEvent.SourceIp ?? string.Empty),
            new("Description", auditEvent.Description ?? string.Empty)
        };

        var key = auditEvent.Id.ToString(CultureInfo.InvariantCulture);
        var title = string.IsNullOrWhiteSpace(auditEvent.EventType)
            ? entity
            : $"{auditEvent.EventType}: {entity}";

        var relatedModule = string.IsNullOrWhiteSpace(auditEvent.RelatedModule)
            ? null
            : auditEvent.RelatedModule;

        object? relatedParameter = auditEvent.RecordId.HasValue
            ? auditEvent.RecordId.Value
            : null;

        return new ModuleRecord(
            key,
            title,
            code: auditEvent.RecordId?.ToString(CultureInfo.InvariantCulture),
            status: auditEvent.Severity,
            description: auditEvent.Description,
            inspectorFields: inspector,
            relatedModuleKey: relatedModule,
            relatedParameter: relatedParameter);
    }

    private void SyncStatusFromAuditLog()
    {
        if (HasError)
        {
            if (!string.IsNullOrWhiteSpace(_auditLogViewModel.StatusMessage))
            {
                StatusMessage = _auditLogViewModel.StatusMessage!;
            }

            return;
        }

        if (!string.IsNullOrWhiteSpace(_auditLogViewModel.StatusMessage))
        {
            StatusMessage = _auditLogViewModel.StatusMessage!;
        }
        else
        {
            StatusMessage = FormatLoadedStatus(Records.Count);
        }
    }

    private void OnFilteredEventsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ProjectRecordsFromEvents();
        UpdateCommandStates();
    }

    private void OnAuditLogPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(AuditLogViewModel.StatusMessage), StringComparison.Ordinal))
        {
            SyncStatusFromAuditLog();
        }
        else if (string.Equals(e.PropertyName, nameof(AuditLogViewModel.IsBusy), StringComparison.Ordinal))
        {
            if (IsBusy != _auditLogViewModel.IsBusy)
            {
                IsBusy = _auditLogViewModel.IsBusy;
            }
        }
    }

    private void OnSelfPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(IsBusy), StringComparison.Ordinal)
            || string.Equals(e.PropertyName, nameof(HasResults), StringComparison.Ordinal))
        {
            UpdateCommandStates();
        }
    }

    private void UpdateCommandStates()
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher != null && !dispatcher.CheckAccess())
        {
            dispatcher.BeginInvoke(new Action(UpdateCommandStates));
            return;
        }
        YasGMP.Wpf.Helpers.UiCommandHelper.NotifyCanExecuteOnUi(_applyFilterCommand);
        YasGMP.Wpf.Helpers.UiCommandHelper.NotifyCanExecuteOnUi(_refreshCommand);
        YasGMP.Wpf.Helpers.UiCommandHelper.NotifyCanExecuteOnUi(_exportCsvCommand);
        YasGMP.Wpf.Helpers.UiCommandHelper.NotifyCanExecuteOnUi(_exportXlsxCommand);
        YasGMP.Wpf.Helpers.UiCommandHelper.NotifyCanExecuteOnUi(_exportPdfCommand);
        YasGMP.Wpf.Helpers.UiCommandHelper.NotifyCanExecuteOnUi(base.RefreshCommand);
    }
}

