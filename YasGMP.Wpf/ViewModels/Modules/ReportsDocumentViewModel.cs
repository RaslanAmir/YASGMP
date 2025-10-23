using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.ViewModels;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>
/// Projects the shared analytics <see cref="ReportViewModel"/> onto the WPF shell.
/// </summary>
public sealed partial class ReportsDocumentViewModel : ModuleDocumentViewModel
{
    /// <summary>Stable registry key used by the module tree and Golden Arrow navigation.</summary>
    public const string ModuleKey = "Reports";

    private readonly IReportAnalyticsViewModel _analytics;
    private readonly ExportService _exportService;
    private readonly ILocalizationService _localization;
    private readonly AsyncRelayCommand _refreshCommand;
    private readonly AsyncRelayCommand _generateCommand;
    private readonly AsyncRelayCommand _exportPdfCommand;
    private readonly AsyncRelayCommand _exportExcelCommand;
    private bool _suppressProjection;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportsDocumentViewModel"/> class.
    /// </summary>
    public ReportsDocumentViewModel(
        IReportAnalyticsViewModel analytics,
        ExportService exportService,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation,
        ILocalizationService localization)
        : base(ModuleKey, localization.GetString("Module.Title.Reports"), localization, cflDialogService, shellInteraction, navigation)
    {
        _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));

        _analytics.PropertyChanged += OnAnalyticsPropertyChanged;
        _analytics.Reports.CollectionChanged += OnReportsCollectionChanged;

        _refreshCommand = new AsyncRelayCommand(ExecuteRefreshAsync, () => !IsBusy);
        _generateCommand = new AsyncRelayCommand(ExecuteGenerateAsync, () => !IsBusy);
        _exportPdfCommand = new AsyncRelayCommand(ExportToPdfAsync, CanExecuteExport);
        _exportExcelCommand = new AsyncRelayCommand(ExportToExcelAsync, CanExecuteExport);

        ReplaceRefreshCommand();
        AppendToolbarCommands();

        PropertyChanged += OnSelfPropertyChanged;
    }

    /// <summary>Exposes the shared analytics view-model so XAML can bind to filters and commands.</summary>
    public IReportAnalyticsViewModel Analytics => _analytics;

    /// <summary>Command that reloads reports from the shared analytics pipeline.</summary>
    public new IAsyncRelayCommand RefreshCommand => _refreshCommand;

    /// <summary>Command that triggers report generation while respecting busy gating.</summary>
    public IAsyncRelayCommand GenerateCommand => _generateCommand;

    /// <summary>Exports the filtered snapshot to PDF via <see cref="ExportService"/>.</summary>
    public IAsyncRelayCommand ExportToPdfCommand => _exportPdfCommand;

    /// <summary>Exports the filtered snapshot to Excel via <see cref="ExportService"/>.</summary>
    public IAsyncRelayCommand ExportToExcelCommand => _exportExcelCommand;

    [ObservableProperty]
    private bool _hasResults;

    [ObservableProperty]
    private bool _hasError;

    /// <summary>
    /// Synchronizes the initial projection before delegating to the base initialization pipeline.
    /// </summary>
    public new async Task InitializeAsync(object? parameter = null)
    {
        ProjectRecordsFromAnalytics();
        SyncStatusFromAnalytics();
        await base.InitializeAsync(parameter).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        try
        {
            _suppressProjection = true;
            await _analytics.LoadReportsCommand.ExecuteAsync(null).ConfigureAwait(false);
            var records = ProjectRecords();
            HasResults = records.Count > 0;
            HasError = false;
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
            ProjectRecordsFromAnalytics();
            SyncStatusFromAnalytics();
        }
    }

    private async Task ExecuteRefreshAsync()
    {
        await _analytics.LoadReportsCommand.ExecuteAsync(null).ConfigureAwait(false);
        ProjectRecordsFromAnalytics();
    }

    private async Task ExecuteGenerateAsync()
    {
        await _analytics.GenerateReportsCommand.ExecuteAsync(null).ConfigureAwait(false);
        ProjectRecordsFromAnalytics();
    }

    private async Task ExportToPdfAsync()
    {
        if (!CanExecuteExport())
        {
            StatusMessage = _localization.GetString("Reports.Status.ExportUnavailable");
            return;
        }

        IsBusy = true;
        try
        {
            var snapshot = _analytics.Reports.ToList();
            var path = await _exportService.ExportReportsToPdfAsync(snapshot, BuildFilterDescription()).ConfigureAwait(false);
            StatusMessage = _localization.GetString("Reports.Status.ExportPdfSuccess", path);
            HasError = false;
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = _localization.GetString("Reports.Status.ExportPdfFailed", ex.Message);
        }
        finally
        {
            IsBusy = false;
            UpdateExportCommandStates();
        }
    }

    private async Task ExportToExcelAsync()
    {
        if (!CanExecuteExport())
        {
            StatusMessage = _localization.GetString("Reports.Status.ExportUnavailable");
            return;
        }

        IsBusy = true;
        try
        {
            var snapshot = _analytics.Reports.ToList();
            var path = await _exportService.ExportReportsToExcelAsync(snapshot, BuildFilterDescription()).ConfigureAwait(false);
            StatusMessage = _localization.GetString("Reports.Status.ExportExcelSuccess", path);
            HasError = false;
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = _localization.GetString("Reports.Status.ExportExcelFailed", ex.Message);
        }
        finally
        {
            IsBusy = false;
            UpdateExportCommandStates();
        }
    }

    private void ReplaceRefreshCommand()
    {
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
    }

    private void AppendToolbarCommands()
    {
        Toolbar.Add(new ModuleToolbarCommand(
            "Reports.Toolbar.Command.Generate.Content",
            _generateCommand,
            _localization,
            "Reports.Toolbar.Command.Generate.ToolTip",
            "Reports.Toolbar.Command.Generate.AutomationName",
            "Reports.Toolbar.Command.Generate.AutomationId"));

        Toolbar.Add(new ModuleToolbarCommand(
            "Reports.Toolbar.Command.ExportPdf.Content",
            _exportPdfCommand,
            _localization,
            "Reports.Toolbar.Command.ExportPdf.ToolTip",
            "Reports.Toolbar.Command.ExportPdf.AutomationName",
            "Reports.Toolbar.Command.ExportPdf.AutomationId"));

        Toolbar.Add(new ModuleToolbarCommand(
            "Reports.Toolbar.Command.ExportExcel.Content",
            _exportExcelCommand,
            _localization,
            "Reports.Toolbar.Command.ExportExcel.ToolTip",
            "Reports.Toolbar.Command.ExportExcel.AutomationName",
            "Reports.Toolbar.Command.ExportExcel.AutomationId"));
    }

    private void OnReportsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ProjectRecordsFromAnalytics();
    }

    private void OnAnalyticsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(IReportAnalyticsViewModel.IsBusy), StringComparison.Ordinal))
        {
            IsBusy = _analytics.IsBusy;
        }
        else if (string.Equals(e.PropertyName, nameof(IReportAnalyticsViewModel.StatusMessage), StringComparison.Ordinal))
        {
            if (!string.IsNullOrWhiteSpace(_analytics.StatusMessage))
            {
                StatusMessage = _analytics.StatusMessage;
            }
        }
    }

    private void OnSelfPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(HasResults), StringComparison.Ordinal) ||
            string.Equals(e.PropertyName, nameof(IsBusy), StringComparison.Ordinal))
        {
            UpdateExportCommandStates();
        }
    }

    private void ProjectRecordsFromAnalytics()
    {
        if (_suppressProjection)
        {
            return;
        }

        var records = ProjectRecords();

        Records.Clear();
        foreach (var record in records)
        {
            Records.Add(record);
        }

        HasResults = Records.Count > 0;
    }

    private List<ModuleRecord> ProjectRecords()
    {
        var records = new List<ModuleRecord>(_analytics.Reports.Count);

        foreach (var report in _analytics.Reports)
        {
            records.Add(MapReport(report));
        }

        return records;
    }

    private ModuleRecord MapReport(Report report)
    {
        var key = string.IsNullOrWhiteSpace(report.FilePath)
            ? $"Report-{report.Id}"
            : report.FilePath;

        var generatedOn = report.GeneratedOn.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);

        var inspectorFields = new List<InspectorField>
        {
            CreateInspectorField(key, report.Title, _localization.GetString("Reports.Inspector.GeneratedOn"), generatedOn),
            CreateInspectorField(key, report.Title, _localization.GetString("Reports.Inspector.ReportType"), report.ReportType ?? string.Empty),
            CreateInspectorField(key, report.Title, _localization.GetString("Reports.Inspector.Status"), report.Status ?? string.Empty),
            CreateInspectorField(key, report.Title, _localization.GetString("Reports.Inspector.Entity"), report.LinkedEntityType ?? string.Empty),
            CreateInspectorField(key, report.Title, _localization.GetString("Reports.Inspector.EntityId"), report.LinkedEntityId?.ToString(CultureInfo.CurrentCulture) ?? string.Empty),
            CreateInspectorField(key, report.Title, _localization.GetString("Reports.Inspector.GeneratedBy"), report.GeneratedById.ToString(CultureInfo.CurrentCulture)),
            CreateInspectorField(key, report.Title, _localization.GetString("Reports.Inspector.Device"), report.DeviceInfo ?? string.Empty),
            CreateInspectorField(key, report.Title, _localization.GetString("Reports.Inspector.Session"), report.SessionId ?? string.Empty),
            CreateInspectorField(key, report.Title, _localization.GetString("Reports.Inspector.Signature"), report.DigitalSignature ?? string.Empty)
        };

        return new ModuleRecord(
            key,
            report.Title,
            report.ReportType,
            report.Status,
            report.Description,
            inspectorFields);
    }

    private void SyncStatusFromAnalytics()
    {
        IsBusy = _analytics.IsBusy;
        if (!string.IsNullOrWhiteSpace(_analytics.StatusMessage))
        {
            StatusMessage = _analytics.StatusMessage;
        }
    }

    private string BuildFilterDescription()
    {
        var parts = new List<string>();

        if (_analytics.FromDate.HasValue)
        {
            parts.Add($"from={_analytics.FromDate.Value:yyyy-MM-dd}");
        }

        if (_analytics.ToDate.HasValue)
        {
            parts.Add($"to={_analytics.ToDate.Value:yyyy-MM-dd}");
        }

        if (!string.IsNullOrWhiteSpace(_analytics.SelectedReportType))
        {
            parts.Add($"type={_analytics.SelectedReportType}");
        }

        if (!string.IsNullOrWhiteSpace(_analytics.SelectedEntityType))
        {
            parts.Add($"entity={_analytics.SelectedEntityType}");
        }

        if (!string.IsNullOrWhiteSpace(_analytics.EntityIdText))
        {
            parts.Add($"entityId={_analytics.EntityIdText}");
        }

        if (!string.IsNullOrWhiteSpace(_analytics.StatusFilter))
        {
            parts.Add($"status={_analytics.StatusFilter}");
        }

        if (!string.IsNullOrWhiteSpace(_analytics.SearchTerm))
        {
            parts.Add($"search={_analytics.SearchTerm}");
        }

        return parts.Count == 0 ? "none" : string.Join(", ", parts);
    }

    private bool CanExecuteExport() => HasResults && !IsBusy;

    private void UpdateExportCommandStates()
    {
        _exportPdfCommand.NotifyCanExecuteChanged();
        _exportExcelCommand.NotifyCanExecuteChanged();
    }
}
