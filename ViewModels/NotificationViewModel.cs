using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Models.DTO;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// <b>NotificationViewModel</b> – Robust MVVM ViewModel for notifications, reminders, and escalations.
    /// <para>
    /// • GMP/Annex 11/21 CFR Part 11 ready: audit, acknowledgements, device/IP/session context<br/>
    /// • Supports in-app, email, SMS, push; export; mute/snooze; batch operations<br/>
    /// • Integrates with <see cref="DatabaseService"/> and <see cref="AuthService"/> for persistence and context
    /// </para>
    /// </summary>
    public sealed class NotificationViewModel : INotifyPropertyChanged
    {
        #region === Fields & Constructor =======================================================

        private readonly DatabaseService _dbService;
        private readonly AuthService _authService;

        private ObservableCollection<Notification> _notifications = new();
        private ObservableCollection<Notification> _filteredNotifications = new();

        // FIX (CS8618): Allow null selection initially
        private Notification? _selectedNotification;

        // FIX (CS8618): Make filters nullable or initialize to empty as needed
        private string? _searchTerm;
        private string? _typeFilter;
        private string? _entityFilter;
        private string? _statusFilter;

        private bool _isBusy;
        private string _statusMessage = string.Empty; // initialized to avoid nulls

        // Coalesce context strings
        private readonly string _currentSessionId;
        private readonly string _currentDeviceInfo;
        private readonly string _currentIpAddress;

        /// <summary>
        /// Default mute duration (hours) used by <see cref="MuteNotificationAsync"/> when no UX value is provided.
        /// </summary>
        private const int DefaultMuteHours = 24;

        /// <summary>
        /// Creates a new instance of <see cref="NotificationViewModel"/>.
        /// </summary>
        /// <param name="dbService">Persistence/audit service.</param>
        /// <param name="authService">Auth/session context provider.</param>
        /// <exception cref="ArgumentNullException">Thrown if any dependency is null.</exception>
        public NotificationViewModel(DatabaseService dbService, AuthService authService)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            _currentSessionId  = _authService.CurrentSessionId  ?? string.Empty;
            _currentDeviceInfo = _authService.CurrentDeviceInfo ?? string.Empty;
            _currentIpAddress  = _authService.CurrentIpAddress  ?? string.Empty;

            LoadNotificationsCommand    = new AsyncRelayCommand(LoadNotificationsAsync);
            SendNotificationCommand     = new AsyncRelayCommand(SendNotificationAsync,     () => !IsBusy);
            AcknowledgeNotificationCommand = new AsyncRelayCommand(AcknowledgeNotificationAsync, () => !IsBusy && SelectedNotification != null);
            MuteNotificationCommand     = new AsyncRelayCommand(MuteNotificationAsync,     () => !IsBusy && SelectedNotification != null);
            DeleteNotificationCommand   = new AsyncRelayCommand(DeleteNotificationAsync,   () => !IsBusy && SelectedNotification != null);
            ExportNotificationsCommand  = new AsyncRelayCommand(ExportNotificationsAsync,  () => !IsBusy);
            FilterChangedCommand        = new RelayCommand(FilterNotifications);

            // Initial load (fire & forget; errors surface via StatusMessage)
            _ = LoadNotificationsAsync();
        }

        #endregion

        #region === Properties ================================================================

        /// <summary>All notifications (unfiltered).</summary>
        public ObservableCollection<Notification> Notifications
        {
            get => _notifications;
            set { _notifications = value; OnPropertyChanged(); }
        }

        /// <summary>Filtered view of <see cref="Notifications"/>.</summary>
        public ObservableCollection<Notification> FilteredNotifications
        {
            get => _filteredNotifications;
            set { _filteredNotifications = value; OnPropertyChanged(); }
        }

        /// <summary>Notification currently selected in the UI.</summary>
        public Notification? SelectedNotification
        {
            get => _selectedNotification;
            set { _selectedNotification = value; OnPropertyChanged(); }
        }

        /// <summary>Free-text search applied to title, message, entity, sender, and status.</summary>
        public string? SearchTerm
        {
            get => _searchTerm;
            set { _searchTerm = value; OnPropertyChanged(); FilterNotifications(); }
        }

        /// <summary>Type filter (e.g., alert, reminder, escalation).</summary>
        public string? TypeFilter
        {
            get => _typeFilter;
            set { _typeFilter = value; OnPropertyChanged(); FilterNotifications(); }
        }

        /// <summary>Entity filter (e.g., work_orders, incidents, calibrations).</summary>
        public string? EntityFilter
        {
            get => _entityFilter;
            set { _entityFilter = value; OnPropertyChanged(); FilterNotifications(); }
        }

        /// <summary>Status filter (e.g., new, delivered, read, acknowledged, muted).</summary>
        public string? StatusFilter
        {
            get => _statusFilter;
            set { _statusFilter = value; OnPropertyChanged(); FilterNotifications(); }
        }

        /// <summary>Indicates a long-running operation.</summary>
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        /// <summary>Last status or error message for the UI.</summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        /// <summary>Available type values for filters.</summary>
        public string[] AvailableTypes => new[] { "alert", "reminder", "escalation", "info", "overdue", "workflow", "custom" };

        /// <summary>True for admin/superadmin/QA users.</summary>
        public bool CanSendNotifications =>
            _authService.CurrentUser?.Role == "admin" ||
            _authService.CurrentUser?.Role == "superadmin" ||
            _authService.CurrentUser?.Role == "qa";

        #endregion

        #region === Commands ==================================================================
        /// <summary>
        /// Gets or sets the load notifications command.
        /// </summary>

        public ICommand LoadNotificationsCommand { get; }
        /// <summary>
        /// Gets or sets the send notification command.
        /// </summary>
        public ICommand SendNotificationCommand { get; }
        /// <summary>
        /// Gets or sets the acknowledge notification command.
        /// </summary>
        public ICommand AcknowledgeNotificationCommand { get; }
        /// <summary>
        /// Gets or sets the mute notification command.
        /// </summary>
        public ICommand MuteNotificationCommand { get; }
        /// <summary>
        /// Gets or sets the delete notification command.
        /// </summary>
        public ICommand DeleteNotificationCommand { get; }
        /// <summary>
        /// Gets or sets the export notifications command.
        /// </summary>
        public ICommand ExportNotificationsCommand { get; }
        /// <summary>
        /// Gets or sets the filter changed command.
        /// </summary>
        public ICommand FilterChangedCommand { get; }

        #endregion

        #region === Methods ===================================================================

        /// <summary>Loads all notifications (newest first).</summary>
        public async Task LoadNotificationsAsync()
        {
            IsBusy = true;
            try
            {
                var notifications = await _dbService.GetAllNotificationsFullAsync();
                Notifications = new ObservableCollection<Notification>(notifications ?? Enumerable.Empty<Notification>());
                FilterNotifications();
                StatusMessage = $"Loaded {Notifications.Count} notifications.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading notifications: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Sends a new notification and writes audit entries using the extended context (IP, device, session).
        /// <remarks>
        /// Recipients are not auto-derived from entity/type at DB level; this uses any existing selection from
        /// <see cref="SelectedNotification"/> or leaves <see cref="Notification.Recipients"/> empty.
        /// </remarks>
        /// </summary>
        public async Task SendNotificationAsync()
        {
            if (_authService.CurrentUser == null)
            {
                StatusMessage = "No user authenticated for sending notifications.";
                return;
            }

            IsBusy = true;
            try
            {
                int actorUserId = _authService.CurrentUser.Id;

                var notification = new Notification
                {
                    SenderUserId  = actorUserId,
                    SenderUserName= _authService.CurrentUser.UserName,
                    Title         = "Notification",
                    Message       = "Details here...",
                    Type          = string.IsNullOrWhiteSpace(TypeFilter) ? "alert" : TypeFilter,
                    Entity        = string.IsNullOrWhiteSpace(EntityFilter) ? null : EntityFilter,
                    EntityId      = SelectedNotification?.EntityId,
                    Status        = "new",
                    Recipients    = SelectedNotification?.Recipients,
                    CreatedAt     = DateTime.UtcNow,
                    DeviceInfo    = _currentDeviceInfo,
                    SessionId     = _currentSessionId,
                    IpAddress     = _currentIpAddress
                };

                int newId = await _dbService.SendNotificationAsync(
                    notification,
                    actorUserId,
                    _currentIpAddress,
                    _currentDeviceInfo,
                    _currentSessionId
                );

                await _dbService.LogNotificationAuditAsync(
                    newId,
                    actorUserId,
                    action: "SEND",
                    ip: _currentIpAddress,
                    deviceInfo: _currentDeviceInfo,
                    sessionId: _currentSessionId,
                    note: null
                );

                StatusMessage = "Notification sent successfully.";
                await LoadNotificationsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Send failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Marks the selected notification as acknowledged and writes audit entries.
        /// </summary>
        public async Task AcknowledgeNotificationAsync()
        {
            if (SelectedNotification == null) { StatusMessage = "No notification selected."; return; }
            if (_authService.CurrentUser == null) { StatusMessage = "No authenticated user."; return; }

            IsBusy = true;
            try
            {
                int actorUserId = _authService.CurrentUser.Id;

                await _dbService.AcknowledgeNotificationAsync(
                    SelectedNotification.Id,
                    actorUserId,
                    _currentIpAddress,
                    _currentDeviceInfo,
                    _currentSessionId
                );

                await _dbService.LogNotificationAuditAsync(
                    SelectedNotification.Id,
                    actorUserId,
                    action: "ACK",
                    ip: _currentIpAddress,
                    deviceInfo: _currentDeviceInfo,
                    sessionId: _currentSessionId,
                    note: null
                );

                StatusMessage = "Notification acknowledged.";
                await LoadNotificationsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Acknowledge failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Mutes (snoozes) the selected notification for a default duration and writes audit entries.
        /// <remarks>Assumption: default mute = 24 hours.</remarks>
        /// </summary>
        public async Task MuteNotificationAsync()
        {
            if (SelectedNotification == null) { StatusMessage = "No notification selected."; return; }
            if (_authService.CurrentUser == null) { StatusMessage = "No authenticated user."; return; }

            IsBusy = true;
            try
            {
                int actorUserId = _authService.CurrentUser.Id;
                DateTime mutedUntilUtc = DateTime.UtcNow.AddHours(DefaultMuteHours);

                await _dbService.MuteNotificationAsync(
                    notificationId: SelectedNotification.Id,
                    mutedUntilUtc: mutedUntilUtc,
                    actorUserId: actorUserId,
                    ip: _currentIpAddress,
                    deviceInfo: _currentDeviceInfo,
                    sessionId: _currentSessionId
                );

                await _dbService.LogNotificationAuditAsync(
                    SelectedNotification.Id,
                    actorUserId,
                    action: "MUTE",
                    ip: _currentIpAddress,
                    deviceInfo: _currentDeviceInfo,
                    sessionId: _currentSessionId,
                    note: $"Muted until {mutedUntilUtc:u}"
                );

                StatusMessage = "Notification muted.";
                await LoadNotificationsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Mute failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Deletes the selected notification (soft delete if supported) with full audit.
        /// </summary>
        public async Task DeleteNotificationAsync()
        {
            if (SelectedNotification == null) { StatusMessage = "No notification selected."; return; }
            if (_authService.CurrentUser == null) { StatusMessage = "No authenticated user."; return; }

            IsBusy = true;
            try
            {
                int actorUserId = _authService.CurrentUser.Id;

                await _dbService.DeleteNotificationAsync(
                    notificationId: SelectedNotification.Id,
                    actorUserId: actorUserId,
                    ip: _currentIpAddress,
                    deviceInfo: _currentDeviceInfo,
                    sessionId: _currentSessionId
                );

                await _dbService.LogNotificationAuditAsync(
                    SelectedNotification.Id,
                    actorUserId,
                    action: "DELETE",
                    ip: _currentIpAddress,
                    deviceInfo: _currentDeviceInfo,
                    sessionId: _currentSessionId,
                    note: null
                );

                StatusMessage = "Notification deleted.";
                await LoadNotificationsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Delete failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Exports the current filtered set of notifications and writes an audit entry.
        /// </summary>
        public async Task ExportNotificationsAsync()
        {
            IsBusy = true;
            try
            {
                int actorUserId = _authService.CurrentUser?.Id ?? 0;

                _ = await _dbService.ExportNotificationsAsync(
                    rows: FilteredNotifications.ToList(),
                    actorUserId: actorUserId,
                    ip: _currentIpAddress,
                    deviceInfo: _currentDeviceInfo,
                    sessionId: _currentSessionId
                );

                await _dbService.LogNotificationAuditAsync(
                    notificationId: 0,
                    userId: actorUserId,
                    action: "EXPORT",
                    ip: _currentIpAddress,
                    deviceInfo: _currentDeviceInfo,
                    sessionId: _currentSessionId,
                    note: "Notifications export from view model."
                );

                StatusMessage = "Notifications exported successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Applies <see cref="SearchTerm"/>, <see cref="TypeFilter"/>, <see cref="EntityFilter"/>, and <see cref="StatusFilter"/> to <see cref="Notifications"/>.
        /// </summary>
        public void FilterNotifications()
        {
            var filtered = Notifications.Where(n =>
                (string.IsNullOrWhiteSpace(SearchTerm) ||
                    (n.Title?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (n.Message?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (n.Entity?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (n.Status?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (n.SenderUserName?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (string.IsNullOrWhiteSpace(TypeFilter)   || string.Equals(n.Type,   TypeFilter,   StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(EntityFilter) || string.Equals(n.Entity, EntityFilter, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(StatusFilter) || string.Equals(n.Status, StatusFilter, StringComparison.OrdinalIgnoreCase))
            );

            FilteredNotifications = new ObservableCollection<Notification>(filtered);
        }

        /// <summary>
        /// Loads audit history entries for a specific notification ID.
        /// </summary>
        /// <param name="notificationId">Notification primary key.</param>
        /// <returns>Collection of audit DTOs for UI binding.</returns>
        public async Task<ObservableCollection<AuditEntryDto>> LoadNotificationAuditAsync(int notificationId)
        {
            var audits = await _dbService.GetAuditLogForEntityAsync("notifications", notificationId);
            return new ObservableCollection<AuditEntryDto>(audits ?? Enumerable.Empty<AuditEntryDto>());
        }

        #endregion

        #region === INotifyPropertyChanged ====================================================

        /// <inheritdoc/>
        // FIX (CS8612): Use nullable delegate type to match the interface.
        /// <summary>
        /// Occurs when property changed event handler is raised.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Raises <see cref="PropertyChanged"/>.</summary>
        // FIX (CS8625): Accept nullable property name with default null.
        private void OnPropertyChanged([CallerMemberName] string? propName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName ?? string.Empty));

        #endregion
    }
}
