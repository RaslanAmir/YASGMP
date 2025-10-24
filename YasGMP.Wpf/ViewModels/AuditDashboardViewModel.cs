using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Wpf.Helpers;
using YasGMP.Models.DTO;
using YasGMP.Services;

namespace YasGMP.Wpf.ViewModels;

/// <summary>
/// Lightweight dashboard view-model used by the WPF shell. Mirrors the MAUI shape
/// without depending on Microsoft.Maui.* so it can be constructed inside the desktop host.
/// </summary>
public partial class AuditDashboardViewModel : ObservableObject
{
    private static readonly string[] DefaultActions =
    {
        "All",
        "CREATE",
        "UPDATE",
        "DELETE",
        "SIGN",
        "ROLLBACK",
        "EXPORT"
    };

    private readonly AuditService _auditService;
    private readonly ExportService _exportService;

    private string? _filterUser;
    private string? _filterEntity;
    private string? _selectedAction;
    private DateTime _filterFrom;
    private DateTime _filterTo;

    public AuditDashboardViewModel(AuditService auditService, ExportService exportService)
    {
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));

        _filterFrom = DateTime.Today.AddDays(-30);
        _filterTo = DateTime.Today;

        ApplyFilterCommand = new AsyncRelayCommand(LoadAuditsAsync);
        ExportPdfCommand = new AsyncRelayCommand(ExecuteExportPdfAsync, () => FilteredAudits.Count > 0);
        ExportExcelCommand = new AsyncRelayCommand(ExecuteExportExcelAsync, () => FilteredAudits.Count > 0);
        FilteredAudits.CollectionChanged += (_, __) =>
        {
            UiCommandHelper.NotifyCanExecuteOnUi(ExportPdfCommand);
            UiCommandHelper.NotifyCanExecuteOnUi(ExportExcelCommand);
        };
    }

    public ObservableCollection<AuditEntryDto> FilteredAudits { get; } = new();

    public string? FilterUser
    {
        get => _filterUser;
        set => SetProperty(ref _filterUser, value);
    }

    public string? FilterEntity
    {
        get => _filterEntity;
        set => SetProperty(ref _filterEntity, value);
    }

    public string? SelectedAction
    {
        get => _selectedAction;
        set => SetProperty(ref _selectedAction, value);
    }

    public DateTime FilterFrom
    {
        get => _filterFrom;
        set => SetProperty(ref _filterFrom, value);
    }

    public DateTime FilterTo
    {
        get => _filterTo;
        set => SetProperty(ref _filterTo, value);
    }

    public IReadOnlyList<string> ActionTypes => DefaultActions;

    public IAsyncRelayCommand ApplyFilterCommand { get; }

    public IAsyncRelayCommand ExportPdfCommand { get; }

    public IAsyncRelayCommand ExportExcelCommand { get; }

    private async Task LoadAuditsAsync()
    {
        var audits = await _auditService
            .GetFilteredAudits(
                Normalize(FilterUser),
                Normalize(FilterEntity),
                NormalizeAction(SelectedAction),
                FilterFrom,
                FilterTo)
            .ConfigureAwait(false);

        FilteredAudits.Clear();
        foreach (var entry in audits ?? Enumerable.Empty<AuditEntryDto>())
        {
            FilteredAudits.Add(entry);
        }

        UiCommandHelper.NotifyCanExecuteOnUi(ExportPdfCommand);
        UiCommandHelper.NotifyCanExecuteOnUi(ExportExcelCommand);
    }

    private async Task ExecuteExportPdfAsync()
    {
        await _exportService.ExportAuditToPdf(FilteredAudits, BuildFilterDescription()).ConfigureAwait(false);
    }

    private async Task ExecuteExportExcelAsync()
    {
        await _exportService.ExportAuditToExcel(FilteredAudits, BuildFilterDescription()).ConfigureAwait(false);
    }

    private string BuildFilterDescription()
    {
        var user = string.IsNullOrWhiteSpace(FilterUser) ? "All Users" : FilterUser.Trim();
        var entity = string.IsNullOrWhiteSpace(FilterEntity) ? "All Entities" : FilterEntity.Trim();
        var action = string.IsNullOrWhiteSpace(SelectedAction) || string.Equals(SelectedAction, "All", StringComparison.OrdinalIgnoreCase)
            ? "All Actions"
            : SelectedAction.Trim();

        return $"User: {user}; Entity: {entity}; Action: {action}; Range: {FilterFrom:d} - {FilterTo:d}";
    }

    private static string Normalize(string? value) => value?.Trim() ?? string.Empty;

    private static string NormalizeAction(string? action)
        => string.IsNullOrWhiteSpace(action) || string.Equals(action, "All", StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : action.Trim();
}
