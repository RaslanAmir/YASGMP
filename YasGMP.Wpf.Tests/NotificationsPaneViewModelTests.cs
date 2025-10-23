using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Xunit;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.ViewModels;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels;
using YasGMP.Wpf.Tests.TestDoubles;

namespace YasGMP.Wpf.Tests;

public class NotificationsPaneViewModelTests
{
    [Fact]
    public async Task ExportToPdfCommand_WhenNotificationsExist_CompletesSuccessfully()
    {
        var localization = CreateLocalization();
        var analytics = new TestNotificationAnalyticsViewModel();
        analytics.FilteredNotifications.Add(new Notification { Id = 7, Title = "Calibration overdue" });

        var exportService = new RecordingExportService
        {
            NotificationsPdfPath = @"C:\\Exports\\Notifications.pdf"
        };

        var viewModel = new NotificationsPaneViewModel(analytics, exportService, localization);

        Assert.True(viewModel.ExportToPdfCommand.CanExecute(null));

        await viewModel.ExportToPdfCommand.ExecuteAsync(null);

        Assert.Equal(1, exportService.NotificationsPdfCalls);
        Assert.False(viewModel.HasError);
        Assert.Equal(string.Format(CultureInfo.CurrentCulture, "Notifications exported to PDF: {0}", exportService.NotificationsPdfPath), viewModel.StatusMessage);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task ExportToExcelCommand_WhenExportFails_SetsError()
    {
        var localization = CreateLocalization();
        var analytics = new TestNotificationAnalyticsViewModel();
        analytics.FilteredNotifications.Add(new Notification { Id = 3, Title = "Work order pending" });

        var exportService = new RecordingExportService
        {
            ThrowOnNotificationsExcel = true,
            NotificationsExcelException = new InvalidOperationException("permission denied")
        };

        var viewModel = new NotificationsPaneViewModel(analytics, exportService, localization);

        Assert.True(viewModel.ExportToExcelCommand.CanExecute(null));

        await viewModel.ExportToExcelCommand.ExecuteAsync(null);

        Assert.True(viewModel.HasError);
        Assert.Equal(string.Format(CultureInfo.CurrentCulture, "Notifications Excel export failed: {0}", "permission denied"), viewModel.StatusMessage);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task RefreshCommand_DelegatesToAnalyticsAndUpdatesCollection()
    {
        var localization = CreateLocalization();
        var analytics = new TestNotificationAnalyticsViewModel();
        analytics.FilteredNotifications.Add(new Notification { Id = 1, Title = "Initial" });
        analytics.NextFilteredNotifications = new ObservableCollection<Notification>
        {
            new Notification { Id = 2, Title = "Updated" }
        };

        var exportService = new RecordingExportService();
        var viewModel = new NotificationsPaneViewModel(analytics, exportService, localization);

        await viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.Equal(1, analytics.LoadInvocationCount);
        Assert.Single(viewModel.Notifications);
        Assert.Equal(2, viewModel.Notifications[0].Id);
    }

    private static ILocalizationService CreateLocalization()
    {
        var resources = new Dictionary<string, IDictionary<string, string>>
        {
            ["en"] = new Dictionary<string, string>
            {
                ["Dock.Notifications.Title"] = "Notifications",
                ["Dock.Notifications.AutomationId"] = "Dock.Notifications",
                ["Notifications.Status.ExportPdfSuccess"] = "Notifications exported to PDF: {0}",
                ["Notifications.Status.ExportPdfFailed"] = "Notifications PDF export failed: {0}",
                ["Notifications.Status.ExportExcelSuccess"] = "Notifications exported to Excel: {0}",
                ["Notifications.Status.ExportExcelFailed"] = "Notifications Excel export failed: {0}",
                ["Notifications.Status.ExportUnavailable"] = "Notifications export unavailable"
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

        public string NotificationsPdfPath { get; set; } = "notifications.pdf";
        public string NotificationsExcelPath { get; set; } = "notifications.xlsx";
        public bool ThrowOnNotificationsExcel { get; set; }
        public Exception? NotificationsExcelException { get; set; }
        public int NotificationsPdfCalls { get; private set; }
        public int NotificationsExcelCalls { get; private set; }

        public override Task<string> ExportNotificationsToPdfAsync(IEnumerable<Notification> notifications, string filterUsed = "")
        {
            NotificationsPdfCalls++;
            return Task.FromResult(NotificationsPdfPath);
        }

        public override Task<string> ExportNotificationsToExcelAsync(IEnumerable<Notification> notifications, string filterUsed = "")
        {
            NotificationsExcelCalls++;
            if (ThrowOnNotificationsExcel)
            {
                throw NotificationsExcelException ?? new InvalidOperationException("export failed");
            }

            return Task.FromResult(NotificationsExcelPath);
        }
    }

    private sealed class TestNotificationAnalyticsViewModel : ObservableObject, INotificationAnalyticsViewModel
    {
        private ObservableCollection<Notification> _filteredNotifications;
        private Notification? _selectedNotification;
        private string? _searchTerm;
        private string? _typeFilter;
        private string? _entityFilter;
        private string? _statusFilter;
        private bool _isBusy;
        private string _statusMessage = string.Empty;

        public TestNotificationAnalyticsViewModel()
        {
            Notifications = new ObservableCollection<Notification>();
            _filteredNotifications = new ObservableCollection<Notification>();
            AvailableTypes = Array.Empty<string>();
            LoadNotificationsCommand = new AsyncRelayCommand(async () =>
            {
                LoadInvocationCount++;
                if (NextFilteredNotifications is not null)
                {
                    FilteredNotifications = NextFilteredNotifications;
                    NextFilteredNotifications = null;
                }

                await Task.CompletedTask;
            });
            ExportNotificationsCommand = new RelayCommand(() => { });
            FilterChangedCommand = new RelayCommand(() => { });
            AcknowledgeNotificationCommand = new RelayCommand(() => { });
            MuteNotificationCommand = new RelayCommand(() => { });
            DeleteNotificationCommand = new RelayCommand(() => { });
        }

        public ObservableCollection<Notification> Notifications { get; }

        public ObservableCollection<Notification> FilteredNotifications
        {
            get => _filteredNotifications;
            set => SetProperty(ref _filteredNotifications, value);
        }

        public Notification? SelectedNotification
        {
            get => _selectedNotification;
            set => SetProperty(ref _selectedNotification, value);
        }

        public string? SearchTerm
        {
            get => _searchTerm;
            set => SetProperty(ref _searchTerm, value);
        }

        public string? TypeFilter
        {
            get => _typeFilter;
            set => SetProperty(ref _typeFilter, value);
        }

        public string? EntityFilter
        {
            get => _entityFilter;
            set => SetProperty(ref _entityFilter, value);
        }

        public string? StatusFilter
        {
            get => _statusFilter;
            set => SetProperty(ref _statusFilter, value);
        }

        public IReadOnlyList<string> AvailableTypes { get; }

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public IAsyncRelayCommand LoadNotificationsCommand { get; }
        public ICommand ExportNotificationsCommand { get; }
        public ICommand FilterChangedCommand { get; }
        public ICommand AcknowledgeNotificationCommand { get; }
        public ICommand MuteNotificationCommand { get; }
        public ICommand DeleteNotificationCommand { get; }
        public int LoadInvocationCount { get; private set; }
        public ObservableCollection<Notification>? NextFilteredNotifications { get; set; }

        public Task ExportNotificationsAsync() => Task.CompletedTask;

        public void FilterNotifications()
        {
        }
    }
}
