using System;
using System.Collections.Generic;
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

/// <summary>
/// Projects the MAUI <see cref="AuditLogViewModel"/> onto the WPF shell while preserving toolbar/status semantics.
/// </summary>
public sealed partial class AuditLogDocumentViewModel : ModuleDocumentViewModel
{
    public const string ModuleKey = "Audit";

    public AuditLogDocumentViewModel(
        AuditService auditService,
        ExportService exportService,
        AuditLogViewModel auditLogViewModel,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation,
        ILocalizationService localization)
        : base(ModuleKey, localization.GetString("Module.Title.AuditTrail"), localization, cflDialogService, shellInteraction, navigation)
    {
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        _ = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _ = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _auditLogViewModel = auditLogViewModel ?? throw new ArgumentNullException(nameof(auditLogViewModel));

        _auditLogViewModel.FilteredEvents.CollectionChanged += OnFilteredEventsChanged;
        _auditLogViewModel.PropertyChanged += OnAuditLogPropertyChanged;

        _applyFilterCommand = new AsyncRelayCommand(ExecuteApplyFilterAsync, CanExecuteNonBusyCommand);
        _refreshCommand = new AsyncRelayCommand(ExecuteRefreshAsync, CanExecuteNonBusyCommand);
        _exportCsvCommand = new AsyncRelayCommand(() => ExecuteExportAsync(_auditLogViewModel.ExportCsvCommand, "CSV"), CanExecuteExport);
        _exportXlsxCommand = new AsyncRelayCommand(() => ExecuteExportAsync(_auditLogViewModel.ExportXlsxCommand, "Excel"), CanExecuteExport);
        _exportPdfCommand = new AsyncRelayCommand(() => ExecuteExportAsync(_auditLogViewModel.ExportPdfCommand, "PDF"), CanExecuteExport);

        var refreshEntry = Toolbar
            .Select((command, index) => (Command: command, Index: index))
            .FirstOrDefault(tuple => string.Equals(tuple.Command.CaptionKey, "Module.Toolbar.Command.Refresh.Content", StringComparison.Ordinal));

        if (refreshEntry.Command is not null)
        {
            Toolbar.Remove(refreshEntry.Command);
            Toolbar.Insert(refreshEntry.Index, new ModuleToolbarCommand(
                "Module.Toolbar.Command.Refresh.Content",
                _refreshCommand,
                _localization,
                "Module.Toolbar.Command.Refresh.ToolTip",
                "Module.Toolbar.Command.Refresh.AutomationName",
                "Module.Toolbar.Command.Refresh.AutomationId"));
        }
        else
        {
            Toolbar.Add(new ModuleToolbarCommand(
                "Module.Toolbar.Command.Refresh.Content",
                _refreshCommand,
                _localization,
                "Module.Toolbar.Command.Refresh.ToolTip",
                "Module.Toolbar.Command.Refresh.AutomationName",
                "Module.Toolbar.Command.Refresh.AutomationId"));
        }

        Toolbar.Add(new ModuleToolbarCommand(
            "Audit.Toolbar.Command.ApplyFilter.Content",
            _applyFilterCommand,
            _localization,
            "Audit.Toolbar.Command.ApplyFilter.ToolTip",
            "Audit.Toolbar.Command.ApplyFilter.AutomationName",
            "Audit.Toolbar.Command.ApplyFilter.AutomationId"));
        Toolbar.Add(new ModuleToolbarCommand(
            "Audit.Toolbar.Command.ExportCsv.Content",
            _exportCsvCommand,
            _localization,
            "Audit.Toolbar.Command.ExportCsv.ToolTip",
            "Audit.Toolbar.Command.ExportCsv.AutomationName",
            "Audit.Toolbar.Command.ExportCsv.AutomationId"));
        Toolbar.Add(new ModuleToolbarCommand(
            "Audit.Toolbar.Command.ExportExcel.Content",
            _exportXlsxCommand,
            _localization,
            "Audit.Toolbar.Command.ExportExcel.ToolTip",
            "Audit.Toolbar.Command.ExportExcel.AutomationName",
            "Audit.Toolbar.Command.ExportExcel.AutomationId"));
        Toolbar.Add(new ModuleToolbarCommand(
            "Audit.Toolbar.Command.ExportPdf.Content",
            _exportPdfCommand,
            _localization,
            "Audit.Toolbar.Command.ExportPdf.ToolTip",
            "Audit.Toolbar.Command.ExportPdf.AutomationName",
            "Audit.Toolbar.Command.ExportPdf.AutomationId"));

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
                EventTime = now.AddMinutes(-42),
                EventType = "UPDATE",
                TableName = "work_orders",
                RecordId = 1204,
                UserId = 17,
                Description = "Adjusted planned start",
                DeviceInfo = "Win10 | MAINT-02",
                SourceIp = "10.0.5.12"
            })
        };
    }

    protected override string FormatLoadedStatus(int count)
    {
        if (!string.IsNullOrWhiteSpace(_auditLogViewModel.StatusMessage))
        {
            return _auditLogViewModel.StatusMessage!;
        }

        return count switch
        {
            <= 0 => "No audit events match the current filters.",
            1 => "Loaded 1 audit event.",
            _ => $"Loaded {count} audit events."
        };
    }

    [ObservableProperty]
    private bool _hasResults;

    [ObservableProperty]
    private bool _hasError;

    private readonly ILocalizationService _localization;
    private readonly AuditLogViewModel _auditLogViewModel;
    private readonly AsyncRelayCommand _applyFilterCommand;
    private readonly AsyncRelayCommand _refreshCommand;
    private readonly AsyncRelayCommand _exportCsvCommand;
    private readonly AsyncRelayCommand _exportXlsxCommand;
    private readonly AsyncRelayCommand _exportPdfCommand;
    private bool _suppressProjection;

    private async Task ExecuteApplyFilterAsync()
    {
        if (!CanExecuteNonBusyCommand())
        {
            return;
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

    private async Task ExecuteExportAsync(System.Windows.Input.ICommand? mauiCommand, string formatLabel)
    {
        if (!CanExecuteExport())
        {
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = $"Exporting audit log to {formatLabel}...";

            switch (mauiCommand)
            {
                case IAsyncRelayCommand asyncRelay:
                    await asyncRelay.ExecuteAsync(null).ConfigureAwait(false);
                    break;
                case IRelayCommand relay:
                    relay.Execute(null);
                    break;
                default:
                    mauiCommand?.Execute(null);
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
        => !IsBusy && (_auditLogViewModel.FilteredEvents?.Count ?? 0) > 0;

    private List<ModuleRecord> ProjectRecords()
        => _auditLogViewModel.FilteredEvents.Select(MapToRecord).ToList();

    private void ProjectRecordsFromEvents()
    {
        if (_suppressProjection)
        {
            return;
        }

        var previousKey = SelectedRecord?.Key;
        var snapshot = ProjectRecords();

        Records.Clear();
        foreach (var record in snapshot)
        {
            Records.Add(record);
        }

        RecordsView.Refresh();

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
        _applyFilterCommand.NotifyCanExecuteChanged();
        _refreshCommand.NotifyCanExecuteChanged();
        _exportCsvCommand.NotifyCanExecuteChanged();
        _exportXlsxCommand.NotifyCanExecuteChanged();
        _exportPdfCommand.NotifyCanExecuteChanged();
        base.RefreshCommand.NotifyCanExecuteChanged();
    }
}
