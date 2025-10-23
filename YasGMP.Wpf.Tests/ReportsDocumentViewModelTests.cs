using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Xunit;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.ViewModels;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Modules;
using YasGMP.Wpf.Tests.TestDoubles;

namespace YasGMP.Wpf.Tests;

public class ReportsDocumentViewModelTests
{
    [Fact]
    public async Task ExportToPdfCommand_WhenReportsAvailable_InvokesExportServiceAndClearsError()
    {
        var localization = CreateLocalization();
        var analytics = new TestReportAnalyticsViewModel
        {
            FromDate = new DateTime(2024, 1, 1),
            ToDate = new DateTime(2024, 12, 31),
            SelectedReportType = "Audit",
            SelectedEntityType = "WorkOrder",
            EntityIdText = "42",
            StatusFilter = "Completed",
            SearchTerm = "batch"
        };
        analytics.Reports.Add(new Report
        {
            Id = 5,
            Title = "Monthly Audit",
            ReportType = "pdf",
            Status = "Completed",
            LinkedEntityType = "WorkOrder",
            LinkedEntityId = 42,
            GeneratedOn = new DateTime(2024, 1, 15, 8, 0, 0, DateTimeKind.Utc)
        });

        var exportService = new RecordingExportService
        {
            ReportsPdfPath = @"C:\\Exports\\Reports_202401150800.pdf"
        };

        var viewModel = CreateViewModel(localization, analytics, exportService);
        await viewModel.InitializeAsync();

        Assert.True(viewModel.ExportToPdfCommand.CanExecute(null));

        await viewModel.ExportToPdfCommand.ExecuteAsync(null);

        Assert.Equal(1, exportService.ReportsPdfCalls);
        Assert.Equal("from=2024-01-01, to=2024-12-31, type=Audit, entity=WorkOrder, entityId=42, status=Completed, search=batch", exportService.LastReportsFilter);
        Assert.False(viewModel.HasError);
        Assert.Equal(string.Format(CultureInfo.CurrentCulture, "PDF exported to {0}", exportService.ReportsPdfPath), viewModel.StatusMessage);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task ExportToExcelCommand_WhenServiceThrows_SetsHasErrorAndStatus()
    {
        var localization = CreateLocalization();
        var analytics = new TestReportAnalyticsViewModel();
        analytics.Reports.Add(new Report { Id = 1, Title = "Validation", ReportType = "excel" });

        var exportService = new RecordingExportService
        {
            ThrowOnReportsExcel = true,
            ReportsExcelException = new InvalidOperationException("disk full")
        };

        var viewModel = CreateViewModel(localization, analytics, exportService);
        await viewModel.InitializeAsync();

        Assert.True(viewModel.ExportToExcelCommand.CanExecute(null));

        await viewModel.ExportToExcelCommand.ExecuteAsync(null);

        Assert.True(viewModel.HasError);
        Assert.Equal(string.Format(CultureInfo.CurrentCulture, "Excel export failed: {0}", "disk full"), viewModel.StatusMessage);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task ExportCommands_DisabledWhenNoResults()
    {
        var localization = CreateLocalization();
        var analytics = new TestReportAnalyticsViewModel();
        var exportService = new RecordingExportService();

        var viewModel = CreateViewModel(localization, analytics, exportService);
        await viewModel.InitializeAsync();

        Assert.False(viewModel.ExportToPdfCommand.CanExecute(null));
        Assert.False(viewModel.ExportToExcelCommand.CanExecute(null));
    }

    private static ReportsDocumentViewModel CreateViewModel(
        ILocalizationService localization,
        TestReportAnalyticsViewModel analytics,
        RecordingExportService exportService)
    {
        var cfl = new StubCflDialogService();
        var shell = new StubShellInteractionService();
        var navigation = new StubModuleNavigationService();
        return new ReportsDocumentViewModel(analytics, exportService, cfl, shell, navigation, localization);
    }

    private static ILocalizationService CreateLocalization()
    {
        var resources = new Dictionary<string, IDictionary<string, string>>
        {
            ["en"] = new Dictionary<string, string>
            {
                ["Module.Title.Reports"] = "Reports",
                ["Reports.Status.ExportPdfSuccess"] = "PDF exported to {0}",
                ["Reports.Status.ExportPdfFailed"] = "PDF export failed: {0}",
                ["Reports.Status.ExportExcelSuccess"] = "Excel exported to {0}",
                ["Reports.Status.ExportExcelFailed"] = "Excel export failed: {0}",
                ["Reports.Status.ExportUnavailable"] = "Export is unavailable"
            }
        };

        return new FakeLocalizationService(resources, "en");
    }

    private sealed class RecordingExportService : ExportService
    {
        public RecordingExportService()
            : base(new DatabaseService("Server=localhost;User Id=test;Password=test;Database=test;"))
        {
        }

        public string ReportsPdfPath { get; set; } = "reports.pdf";
        public string ReportsExcelPath { get; set; } = "reports.xlsx";
        public bool ThrowOnReportsExcel { get; set; }
        public Exception? ReportsExcelException { get; set; }
        public int ReportsPdfCalls { get; private set; }
        public int ReportsExcelCalls { get; private set; }
        public string LastReportsFilter { get; private set; } = string.Empty;

        public override Task<string> ExportReportsToPdfAsync(IEnumerable<Report> reports, string filterUsed = "")
        {
            ReportsPdfCalls++;
            LastReportsFilter = filterUsed;
            return Task.FromResult(ReportsPdfPath);
        }

        public override Task<string> ExportReportsToExcelAsync(IEnumerable<Report> reports, string filterUsed = "")
        {
            ReportsExcelCalls++;
            LastReportsFilter = filterUsed;
            if (ThrowOnReportsExcel)
            {
                throw ReportsExcelException ?? new InvalidOperationException("export failed");
            }

            return Task.FromResult(ReportsExcelPath);
        }
    }

    private sealed class TestReportAnalyticsViewModel : ObservableObject, IReportAnalyticsViewModel
    {
        private bool _isBusy;
        private string? _statusMessage;
        private DateTime? _fromDate;
        private DateTime? _toDate;
        private string? _selectedReportType;
        private string? _selectedEntityType;
        private string? _entityIdText;
        private string? _searchTerm;
        private string? _statusFilter;

        public TestReportAnalyticsViewModel()
        {
            Reports = new ObservableCollection<Report>();
            AvailableReportTypes = Array.Empty<string>();
            AvailableEntityTypes = Array.Empty<string>();
            AvailableStatuses = Array.Empty<string>();
            LoadReportsCommand = new AsyncRelayCommand(async () => await Task.CompletedTask);
            GenerateReportsCommand = new AsyncRelayCommand(async () => await Task.CompletedTask);
            ExportPdfCommand = new AsyncRelayCommand(async () => await Task.CompletedTask);
            ExportExcelCommand = new AsyncRelayCommand(async () => await Task.CompletedTask);
            ApplyFiltersCommand = new RelayCommand(() => { });
            ClearFiltersCommand = new RelayCommand(() => { });
        }

        public ObservableCollection<Report> Reports { get; }

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public string? StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public DateTime? FromDate
        {
            get => _fromDate;
            set => SetProperty(ref _fromDate, value);
        }

        public DateTime? ToDate
        {
            get => _toDate;
            set => SetProperty(ref _toDate, value);
        }

        public string? SelectedReportType
        {
            get => _selectedReportType;
            set => SetProperty(ref _selectedReportType, value);
        }

        public string? SelectedEntityType
        {
            get => _selectedEntityType;
            set => SetProperty(ref _selectedEntityType, value);
        }

        public string? EntityIdText
        {
            get => _entityIdText;
            set => SetProperty(ref _entityIdText, value);
        }

        public string? SearchTerm
        {
            get => _searchTerm;
            set => SetProperty(ref _searchTerm, value);
        }

        public string? StatusFilter
        {
            get => _statusFilter;
            set => SetProperty(ref _statusFilter, value);
        }

        public IReadOnlyList<string> AvailableReportTypes { get; }
        public IReadOnlyList<string> AvailableEntityTypes { get; }
        public IReadOnlyList<string> AvailableStatuses { get; }
        public IAsyncRelayCommand LoadReportsCommand { get; }
        public IAsyncRelayCommand GenerateReportsCommand { get; }
        public IAsyncRelayCommand ExportPdfCommand { get; }
        public IAsyncRelayCommand ExportExcelCommand { get; }
        public IRelayCommand ApplyFiltersCommand { get; }
        public IRelayCommand ClearFiltersCommand { get; }

        public void ApplyFilters()
        {
        }
    }
}
