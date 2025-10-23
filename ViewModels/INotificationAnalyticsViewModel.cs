using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using YasGMP.Models;

namespace YasGMP.ViewModels;

/// <summary>
/// Contract that exposes notification analytics to desktop shells without duplicating MAUI logic.
/// </summary>
public interface INotificationAnalyticsViewModel : INotifyPropertyChanged
{
    /// <summary>Unfiltered notification cache.</summary>
    ObservableCollection<Notification> Notifications { get; }

    /// <summary>Filtered notifications presented to the UI.</summary>
    ObservableCollection<Notification> FilteredNotifications { get; set; }

    /// <summary>Currently selected notification entry.</summary>
    Notification? SelectedNotification { get; set; }

    /// <summary>Free-text search filter.</summary>
    string? SearchTerm { get; set; }

    /// <summary>Logical type filter.</summary>
    string? TypeFilter { get; set; }

    /// <summary>Linked entity filter.</summary>
    string? EntityFilter { get; set; }

    /// <summary>Status filter (new/delivered/read/etc.).</summary>
    string? StatusFilter { get; set; }

    /// <summary>Catalog of available notification types.</summary>
    IReadOnlyList<string> AvailableTypes { get; }

    /// <summary>Indicates whether an asynchronous operation is running.</summary>
    bool IsBusy { get; set; }

    /// <summary>Localized status or error message.</summary>
    string StatusMessage { get; set; }

    /// <summary>Loads notifications from persistence.</summary>
    ICommand LoadNotificationsCommand { get; }

    /// <summary>Exports the filtered notifications through the shared pipeline.</summary>
    ICommand ExportNotificationsCommand { get; }

    /// <summary>Re-evaluates in-memory filters.</summary>
    ICommand FilterChangedCommand { get; }

    /// <summary>Triggers acknowledgement workflow for the selected notification.</summary>
    ICommand AcknowledgeNotificationCommand { get; }

    /// <summary>Triggers mute workflow for the selected notification.</summary>
    ICommand MuteNotificationCommand { get; }

    /// <summary>Triggers delete workflow for the selected notification.</summary>
    ICommand DeleteNotificationCommand { get; }

    /// <summary>Allows shells to await export completion when executing custom commands.</summary>
    Task ExportNotificationsAsync();

    /// <summary>Applies in-memory filters immediately.</summary>
    void FilterNotifications();
}
