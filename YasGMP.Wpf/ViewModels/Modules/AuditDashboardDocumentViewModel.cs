using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Models.DTO;
using YasGMP.Services;
using YasGMP.ViewModels;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>
/// WPF shell adapter that projects the MAUI audit dashboard onto the B1 form host.
/// </summary>
public sealed partial class AuditDashboardDocumentViewModel : ModuleDocumentViewModel
{
    public const string ModuleKey = "AuditDashboard";

    public AuditDashboardDocumentViewModel(
        AuditService auditService,
        ExportService exportService,
        AuditDashboardViewModel dashboardViewModel,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation,
        ILocalizationService localization,
        Func<AuditDashboardViewModel, Task<IReadOnlyList<AuditEntryDto>>>? loadOverride = null,
        Func<AuditDashboardViewModel, Task<string>>? exportPdfOverride = null,
        Func<AuditDashboardViewModel, Task<string>>? exportExcelOverride = null)
        : base(ModuleKey, "Audit Dashboard", localization, cflDialogService, shellInteraction, navigation)
    {
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _dashboardViewModel = dashboardViewModel ?? throw new ArgumentNullException(nameof(dashboardViewModel));
        _loadOverride = loadOverride;
        _exportPdfOverride = exportPdfOverride;
        _exportExcelOverride = exportExcelOverride;

        _loadAuditsMethod = _dashboardViewModel.GetType()
            .GetMethod("LoadAuditsAsync", BindingFlags.Instance | BindingFlags.NonPublic);

        _dashboardViewModel.FilteredAudits.CollectionChanged += OnFilteredAuditsChanged;

        _applyFilterCommand = new AsyncRelayCommand(ExecuteApplyFilterAsync, () => !IsBusy);
        _exportPdfCommand = new AsyncRelayCommand(ExecuteExportPdfAsync, CanExecuteExport);
        _exportExcelCommand = new AsyncRelayCommand(ExecuteExportExcelAsync, CanExecuteExport);

        Toolbar.Add(new ModuleToolbarCommand(
            "AuditDashboard.Toolbar.Command.ApplyFilters.Content",
            _applyFilterCommand,
            _localization,
            "AuditDashboard.Toolbar.Command.ApplyFilters.ToolTip",
            "AuditDashboard.Toolbar.Command.ApplyFilters.AutomationName",
            "AuditDashboard.Toolbar.Command.ApplyFilters.AutomationId"));
        Toolbar.Add(new ModuleToolbarCommand(
            "AuditDashboard.Toolbar.Command.ExportPdf.Content",
            _exportPdfCommand,
            _localization,
            "AuditDashboard.Toolbar.Command.ExportPdf.ToolTip",
            "AuditDashboard.Toolbar.Command.ExportPdf.AutomationName",
            "AuditDashboard.Toolbar.Command.ExportPdf.AutomationId"));
        Toolbar.Add(new ModuleToolbarCommand(
            "AuditDashboard.Toolbar.Command.ExportExcel.Content",
            _exportExcelCommand,
            _localization,
            "AuditDashboard.Toolbar.Command.ExportExcel.ToolTip",
            "AuditDashboard.Toolbar.Command.ExportExcel.AutomationName",
            "AuditDashboard.Toolbar.Command.ExportExcel.AutomationId"));

        PropertyChanged += OnPropertyChanged;
    }

    public IAsyncRelayCommand ApplyFilterCommand => _applyFilterCommand;

    public IAsyncRelayCommand ExportPdfCommand => _exportPdfCommand;

    public IAsyncRelayCommand ExportExcelCommand => _exportExcelCommand;

    public AuditDashboardViewModel Dashboard => _dashboardViewModel;

    public new async Task InitializeAsync(object? parameter = null)
    {
        ProjectRecordsFromDashboard();
        await base.InitializeAsync(parameter).ConfigureAwait(false);
    }

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        try
        {
            _suppressProjection = true;
            if (_loadOverride is not null)
            {
                var audits = await _loadOverride(_dashboardViewModel).ConfigureAwait(false);
                _dashboardViewModel.FilteredAudits.Clear();
                if (audits is not null)
                {
                    foreach (var entry in audits)
                    {
                        _dashboardViewModel.FilteredAudits.Add(entry);
                    }
                }
            }
            else
            {
                await InvokeDashboardLoadAsync().ConfigureAwait(false);
            }
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
            ProjectRecordsFromDashboard();
        }
    }

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
    {
        var now = DateTime.Now;
        return new List<ModuleRecord>
        {
            MapToRecord(new AuditEntryDto
            {
                Id = 1,
                Timestamp = now.AddMinutes(-12),
                Username = "qa",
                UserId = 42,
                Entity = "work_orders",
                EntityId = "1001",
                Action = "UPDATE",
                DeviceInfo = "Win11 | HQ-WS01",
                Note = "Adjusted planned start"
            }),
            MapToRecord(new AuditEntryDto
            {
                Id = 2,
                Timestamp = now.AddMinutes(-35),
                Username = "compliance",
                UserId = 7,
                Entity = "validation_records",
                EntityId = "VAL-2024-09",
                Action = "SIGN",
                DeviceInfo = "Win10 | QA-LAP02",
                Note = "Approved IQ protocol"
            })
        };
    }

    protected override string FormatLoadedStatus(int count)
        => count switch
        {
            <= 0 => "No audit events match the current filters.",
            1 => "Loaded 1 audit event.",
            _ => $"Loaded {count} audit events."
        };

    [ObservableProperty]
    private bool _hasResults;

    [ObservableProperty]
    private bool _hasError;

    private readonly ILocalizationService _localization;
    private readonly AuditService _auditService;
    private readonly ExportService _exportService;
    private readonly AuditDashboardViewModel _dashboardViewModel;
    private readonly AsyncRelayCommand _applyFilterCommand;
    private readonly AsyncRelayCommand _exportPdfCommand;
    private readonly AsyncRelayCommand _exportExcelCommand;
    private readonly MethodInfo? _loadAuditsMethod;
    private readonly Func<AuditDashboardViewModel, Task<IReadOnlyList<AuditEntryDto>>>? _loadOverride;
    private readonly Func<AuditDashboardViewModel, Task<string>>? _exportPdfOverride;
    private readonly Func<AuditDashboardViewModel, Task<string>>? _exportExcelOverride;
    private bool _suppressProjection;

    private async Task ExecuteApplyFilterAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            await RefreshAsync().ConfigureAwait(false);
            HasError = false;
        }
        finally
        {
            UpdateCommandStates();
        }
    }

    private async Task ExecuteExportPdfAsync()
        => await ExecuteExportAsync(_dashboardViewModel.ExportPdfCommand as IAsyncRelayCommand, "PDF",
            () => _exportService.ExportAuditToPdf(_dashboardViewModel.FilteredAudits)).ConfigureAwait(false);

    private async Task ExecuteExportExcelAsync()
        => await ExecuteExportAsync(_dashboardViewModel.ExportExcelCommand as IAsyncRelayCommand, "Excel",
            () => _exportService.ExportAuditToExcel(_dashboardViewModel.FilteredAudits)).ConfigureAwait(false);

    private async Task ExecuteExportAsync(IAsyncRelayCommand? mauiCommand, string formatLabel, Func<Task<string>> fallback)
    {
        if (!CanExecuteExport())
        {
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = $"Exporting audit dashboard to {formatLabel}...";

            if (mauiCommand is not null)
            {
                await mauiCommand.ExecuteAsync(null).ConfigureAwait(false);
                StatusMessage = $"Audit dashboard exported to {formatLabel}.";
            }
            else if (string.Equals(formatLabel, "PDF", StringComparison.OrdinalIgnoreCase)
                && _exportPdfOverride is not null)
            {
                var path = await _exportPdfOverride(_dashboardViewModel).ConfigureAwait(false);
                StatusMessage = $"Audit dashboard exported to {formatLabel}: {path}";
            }
            else if (string.Equals(formatLabel, "Excel", StringComparison.OrdinalIgnoreCase)
                && _exportExcelOverride is not null)
            {
                var path = await _exportExcelOverride(_dashboardViewModel).ConfigureAwait(false);
                StatusMessage = $"Audit dashboard exported to {formatLabel}: {path}";
            }
            else
            {
                var path = await fallback().ConfigureAwait(false);
                StatusMessage = $"Audit dashboard exported to {formatLabel}: {path}";
            }

            HasError = false;
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = $"Failed to export audit dashboard to {formatLabel}: {ex.Message}";
            throw;
        }
        finally
        {
            IsBusy = false;
            UpdateCommandStates();
        }
    }

    private bool CanExecuteExport() => HasResults && !IsBusy;

    private async Task InvokeDashboardLoadAsync()
    {
        if (_loadAuditsMethod is not null)
        {
            var task = _loadAuditsMethod.Invoke(_dashboardViewModel, Array.Empty<object?>()) as Task;
            if (task is not null)
            {
                await task.ConfigureAwait(false);
                return;
            }
        }

        var audits = await _auditService
            .GetFilteredAudits(
                _dashboardViewModel.FilterUser ?? string.Empty,
                _dashboardViewModel.FilterEntity ?? string.Empty,
                _dashboardViewModel.SelectedAction ?? string.Empty,
                _dashboardViewModel.FilterFrom,
                _dashboardViewModel.FilterTo)
            .ConfigureAwait(false);

        _dashboardViewModel.FilteredAudits.Clear();
        foreach (var entry in audits ?? Enumerable.Empty<AuditEntryDto>())
        {
            _dashboardViewModel.FilteredAudits.Add(entry);
        }
    }

    private List<ModuleRecord> ProjectRecords()
        => _dashboardViewModel.FilteredAudits.Select(MapToRecord).ToList();

    private void ProjectRecordsFromDashboard()
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

    private static ModuleRecord MapToRecord(AuditEntryDto entry)
    {
        var timestamp = entry.Timestamp == DateTime.MinValue
            ? string.Empty
            : entry.Timestamp.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);

        var user = !string.IsNullOrWhiteSpace(entry.Username)
            ? entry.Username
            : entry.UserId?.ToString(CultureInfo.InvariantCulture) ?? "-";

        if (entry.UserId.HasValue && !string.IsNullOrWhiteSpace(entry.Username))
        {
            user = $"{entry.Username} (#{entry.UserId.Value.ToString(CultureInfo.InvariantCulture)})";
        }

        var entity = string.IsNullOrWhiteSpace(entry.Entity) ? "system" : entry.Entity!;
        if (!string.IsNullOrWhiteSpace(entry.EntityId))
        {
            entity += $" #{entry.EntityId}";
        }

        var inspector = new List<InspectorField>
        {
            new("Timestamp", timestamp),
            new("User", user),
            new("Entity", entity),
            new("Action", entry.Action ?? string.Empty),
            new("Device", entry.DeviceInfo ?? string.Empty),
            new("Description", entry.Note ?? string.Empty)
        };

        var key = entry.Id?.ToString(CultureInfo.InvariantCulture)
            ?? $"{entry.Timestamp:O}|{entry.Entity}|{entry.Action}";

        return new ModuleRecord(
            key,
            entity,
            entry.Action,
            status: null,
            description: entry.Note,
            inspectorFields: inspector);
    }

    private void OnFilteredAuditsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => ProjectRecordsFromDashboard();

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
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
        _exportPdfCommand.NotifyCanExecuteChanged();
        _exportExcelCommand.NotifyCanExecuteChanged();
    }
}
