using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.ViewModels;

namespace YasGMP.Wpf.ViewModels;

/// <summary>
/// Anchorable pane that surfaces the shared notification analytics inside the WPF shell.
/// </summary>
public sealed partial class NotificationsPaneViewModel : AnchorableViewModel
{
    private readonly INotificationAnalyticsViewModel _analytics;
    private readonly ExportService _exportService;
    private readonly ILocalizationService _localization;
    private readonly AsyncRelayCommand _exportPdfCommand;
    private readonly AsyncRelayCommand _exportExcelCommand;
    private readonly AsyncRelayCommand _refreshCommand;
    private ObservableCollection<Notification>? _attachedCollection;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationsPaneViewModel"/> class.
    /// </summary>
    public NotificationsPaneViewModel(
        INotificationAnalyticsViewModel analytics,
        ExportService exportService,
        ILocalizationService localization)
    {
        _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));

        Title = _localization.GetString("Dock.Notifications.Title");
        AutomationId = _localization.GetString("Dock.Notifications.AutomationId");
        ContentId = "YasGmp.Shell.Notifications";

        _analytics.PropertyChanged += OnAnalyticsPropertyChanged;

        _exportPdfCommand = new AsyncRelayCommand(ExportToPdfAsync, CanExport);
        _exportExcelCommand = new AsyncRelayCommand(ExportToExcelAsync, CanExport);
        _refreshCommand = new AsyncRelayCommand(ExecuteRefreshAsync, () => !IsBusy);

        AttachNotifications(_analytics.FilteredNotifications);
        Notifications = _analytics.FilteredNotifications;
        StatusMessage = _analytics.StatusMessage;
    }

    /// <summary>Shared analytics view-model.</summary>
    public INotificationAnalyticsViewModel Analytics => _analytics;

    /// <summary>Filtered notifications projected into the pane.</summary>
    [ObservableProperty]
    private ObservableCollection<Notification> _notifications = new();

    /// <summary>Indicates whether an asynchronous export or refresh is running.</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>Tracks whether the last operation failed.</summary>
    [ObservableProperty]
    private bool _hasError;

    /// <summary>Localized status or error message.</summary>
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>Command that exports notifications to PDF.</summary>
    public IAsyncRelayCommand ExportToPdfCommand => _exportPdfCommand;

    /// <summary>Command that exports notifications to Excel.</summary>
    public IAsyncRelayCommand ExportToExcelCommand => _exportExcelCommand;

    /// <summary>Command that triggers the shared view-model to reload notifications.</summary>
    public IAsyncRelayCommand RefreshCommand => _refreshCommand;

    private async Task ExecuteRefreshAsync()
    {
        if (_analytics.LoadNotificationsCommand is IAsyncRelayCommand asyncCommand)
        {
            await asyncCommand.ExecuteAsync(null).ConfigureAwait(false);
        }
        else
        {
            await Task.Run(() => _analytics.LoadNotificationsCommand.Execute(null)).ConfigureAwait(false);
        }

        AttachNotifications(_analytics.FilteredNotifications);
        Notifications = _analytics.FilteredNotifications;
    }

    private async Task ExportToPdfAsync()
    {
        if (!CanExport())
        {
            StatusMessage = _localization.GetString("Notifications.Status.ExportUnavailable");
            return;
        }

        IsBusy = true;
        try
        {
            var snapshot = _analytics.FilteredNotifications.ToList();
            var path = await _exportService.ExportNotificationsToPdfAsync(snapshot).ConfigureAwait(false);
            StatusMessage = _localization.GetString("Notifications.Status.ExportPdfSuccess", path);
            HasError = false;
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = _localization.GetString("Notifications.Status.ExportPdfFailed", ex.Message);
        }
        finally
        {
            IsBusy = false;
            UpdateCommandStates();
        }
    }

    private async Task ExportToExcelAsync()
    {
        if (!CanExport())
        {
            StatusMessage = _localization.GetString("Notifications.Status.ExportUnavailable");
            return;
        }

        IsBusy = true;
        try
        {
            var snapshot = _analytics.FilteredNotifications.ToList();
            var path = await _exportService.ExportNotificationsToExcelAsync(snapshot).ConfigureAwait(false);
            StatusMessage = _localization.GetString("Notifications.Status.ExportExcelSuccess", path);
            HasError = false;
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = _localization.GetString("Notifications.Status.ExportExcelFailed", ex.Message);
        }
        finally
        {
            IsBusy = false;
            UpdateCommandStates();
        }
    }

    private void OnAnalyticsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(INotificationAnalyticsViewModel.FilteredNotifications), StringComparison.Ordinal))
        {
            AttachNotifications(_analytics.FilteredNotifications);
            Notifications = _analytics.FilteredNotifications;
        }
        else if (string.Equals(e.PropertyName, nameof(INotificationAnalyticsViewModel.StatusMessage), StringComparison.Ordinal))
        {
            StatusMessage = _analytics.StatusMessage;
        }
        else if (string.Equals(e.PropertyName, nameof(INotificationAnalyticsViewModel.IsBusy), StringComparison.Ordinal))
        {
            IsBusy = _analytics.IsBusy;
        }
    }

    private void OnNotificationsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateCommandStates();
    }

    private bool CanExport() => Notifications?.Count > 0 && !IsBusy;

    private void UpdateCommandStates()
    {
        _exportPdfCommand.NotifyCanExecuteChanged();
        _exportExcelCommand.NotifyCanExecuteChanged();
    }

    private void AttachNotifications(ObservableCollection<Notification> collection)
    {
        if (_attachedCollection is not null)
        {
            _attachedCollection.CollectionChanged -= OnNotificationsCollectionChanged;
        }

        _attachedCollection = collection;

        if (_attachedCollection is not null)
        {
            _attachedCollection.CollectionChanged += OnNotificationsCollectionChanged;
        }

        UpdateCommandStates();
    }

    partial void OnNotificationsChanged(ObservableCollection<Notification> value)
        => UpdateCommandStates();
}
