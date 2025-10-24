using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Helpers;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// <b>WorkOrderViewModel</b> � Ultra-robust MVVM for GMP/Annex 11/21 CFR Part 11 compliant Work Order Management.
    /// <para>
    /// ? Async CRUD, full workflow, audit trail, digital signature, rollback/versioning<br/>
    /// ? Attachments, comments, asset/part/supplier linkage, timeline, escalation, dashboard stats<br/>
    /// ? Advanced filtering, KPI, extensibility, sign-off, mass actions, future AI/ML hooks
    /// </para>
    /// </summary>
    public sealed class WorkOrderViewModel : INotifyPropertyChanged
    {
        #region === Fields & Constructor ===

        private readonly DatabaseService _dbService;
        private readonly AuthService _authService;

        private ObservableCollection<WorkOrder> _workOrders = new();
        private ObservableCollection<WorkOrder> _filteredWorkOrders = new();

        // Nullable backing fields to respect real UX state (no selection, empty filters)
        private WorkOrder? _selectedWorkOrder = null;
        private string? _searchTerm = null;
        private string? _statusFilter = null;
        private string? _assetFilter = null;
        private string? _priorityFilter = null;
        private int? _photosMin;
        private int? _partsMin;
        // New: additional filters
        private int? _idFilter = null;
        private DateTime? _openFrom = null;
        private DateTime? _openTo = null;
        private DateTime? _closeFrom = null;
        private DateTime? _closeTo = null;
        private bool _isBusy;
        private string? _statusMessage = null;

        private readonly string _currentSessionId;
        private readonly string _currentDeviceInfo;
        private readonly string _currentIpAddress;

        /// <summary>Initializes the WorkOrderViewModel and all commands for robust GMP work order management.</summary>
        /// <param name="dbService">Data access service.</param>
        /// <param name="authService">Authentication/context service.</param>
        /// <exception cref="ArgumentNullException">Thrown when dependencies are <c>null</c>.</exception>
        public WorkOrderViewModel(DatabaseService dbService, AuthService authService)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            _currentSessionId = _authService.CurrentSessionId;
            _currentDeviceInfo = _authService.CurrentDeviceInfo;
            _currentIpAddress = _authService.CurrentIpAddress;

            LoadWorkOrdersCommand     = new AsyncRelayCommand(LoadWorkOrdersAsync);
            AddWorkOrderCommand       = new AsyncRelayCommand(AddWorkOrderAsync,       () => !IsBusy);
            UpdateWorkOrderCommand    = new AsyncRelayCommand(UpdateWorkOrderAsync,    () => !IsBusy && SelectedWorkOrder != null);
            DeleteWorkOrderCommand    = new AsyncRelayCommand(DeleteWorkOrderAsync,    () => !IsBusy && SelectedWorkOrder != null);
            RollbackWorkOrderCommand  = new AsyncRelayCommand(RollbackWorkOrderAsync,  () => !IsBusy && SelectedWorkOrder != null);
            ExportWorkOrdersCommand   = new AsyncRelayCommand(ExportWorkOrdersAsync,   () => !IsBusy);
            ApproveWorkOrderCommand   = new AsyncRelayCommand(ApproveWorkOrderAsync,   () => !IsBusy && SelectedWorkOrder != null);
            CloseWorkOrderCommand     = new AsyncRelayCommand(CloseWorkOrderAsync,     () => !IsBusy && SelectedWorkOrder != null);
            EscalateWorkOrderCommand  = new AsyncRelayCommand(EscalateWorkOrderAsync,  () => !IsBusy && SelectedWorkOrder != null);

            // Important for CS8622: the delegate type for AsyncRelayCommand<T> is Func<T?,Task>
            AddCommentCommand         = new AsyncRelayCommand<string?>(AddCommentAsync,
                (comment) => !IsBusy && SelectedWorkOrder != null && !string.IsNullOrWhiteSpace(comment));

            FilterChangedCommand      = new RelayCommand(FilterWorkOrders);

            _ = LoadWorkOrdersAsync();
        }

        #endregion

        #region === Properties ===

        /// <summary>All work orders in the system (raw list, for advanced dashboard stats).</summary>
        public ObservableCollection<WorkOrder> WorkOrders
        {
            get => _workOrders;
            set { _workOrders = value; OnPropertyChanged(); }
        }

        /// <summary>Filtered work orders for UI.</summary>
        public ObservableCollection<WorkOrder> FilteredWorkOrders
        {
            get => _filteredWorkOrders;
            set { _filteredWorkOrders = value; OnPropertyChanged(); }
        }

        /// <summary>The currently selected work order (for editing/details). <c>null</c> when nothing selected.</summary>
        public WorkOrder? SelectedWorkOrder
        {
            get => _selectedWorkOrder;
            set
            {
                _selectedWorkOrder = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsWorkOrderSelected));
                NotifyCommandsCanExecuteChanged();
            }
        }

        /// <summary>Returns true if any Work Order is currently selected.</summary>
        public bool IsWorkOrderSelected => SelectedWorkOrder != null;

        /// <summary>Search term for asset, title/code, description.</summary>
        public string? SearchTerm
        {
            get => _searchTerm;
            set { _searchTerm = value; OnPropertyChanged(); FilterWorkOrders(); }
        }

        /// <summary>Status filter (active, closed, overdue, pending, escalated).</summary>
        public string? StatusFilter
        {
            get => _statusFilter;
            set { _statusFilter = value; OnPropertyChanged(); FilterWorkOrders(); }
        }

        /// <summary>Filter by asset/machine name.</summary>
        public string? AssetFilter
        {
            get => _assetFilter;
            set { _assetFilter = value; OnPropertyChanged(); FilterWorkOrders(); }
        }

        /// <summary>Filter by work order priority (critical, high, normal, low).</summary>
        public string? PriorityFilter
        {
            get => _priorityFilter;
            set { _priorityFilter = value; OnPropertyChanged(); FilterWorkOrders(); }
        }

        public int? PhotosMin
        {
            get => _photosMin;
            set { _photosMin = value; OnPropertyChanged(); FilterWorkOrders(); }
        }

        public int? PartsMin
        {
            get => _partsMin;
            set { _partsMin = value; OnPropertyChanged(); FilterWorkOrders(); }
        }

        // New filters: ID and date ranges
        public int? IdFilter
        {
            get => _idFilter;
            set { _idFilter = value; OnPropertyChanged(); FilterWorkOrders(); }
        }

        public DateTime? OpenFrom
        {
            get => _openFrom;
            set { _openFrom = value; OnPropertyChanged(); FilterWorkOrders(); }
        }

        public DateTime? OpenTo
        {
            get => _openTo;
            set { _openTo = value; OnPropertyChanged(); FilterWorkOrders(); }
        }

        public DateTime? CloseFrom
        {
            get => _closeFrom;
            set { _closeFrom = value; OnPropertyChanged(); FilterWorkOrders(); }
        }

        public DateTime? CloseTo
        {
            get => _closeTo;
            set { _closeTo = value; OnPropertyChanged(); FilterWorkOrders(); }
        }

        /// <summary>Busy flag for async UI operations.</summary>
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        /// <summary>Status message for UI (nullable to allow �no error/info�).</summary>
        public string? StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        /// <summary>Available status options for work orders.</summary>
        public string[] AvailableStatuses => new[] { "active", "closed", "overdue", "pending", "escalated" };

        /// <summary>Available priority options.</summary>
        public string[] AvailablePriorities => new[] { "critical", "high", "normal", "low" };

        /// <summary>Open work order count (dashboard KPI).</summary>
        public int CountOpen => WorkOrders.Count(o => string.Equals(o.Status, "active", StringComparison.OrdinalIgnoreCase)
                                                   || string.Equals(o.Status, "otvoren", StringComparison.OrdinalIgnoreCase));

        /// <summary>Critical priority count (dashboard KPI).</summary>
        public int CountCritical => WorkOrders.Count(o => string.Equals(o.Priority, "critical", StringComparison.OrdinalIgnoreCase)
                                                        || string.Equals(o.Priority, "kritican", StringComparison.OrdinalIgnoreCase));

        /// <summary>Overdue work order count (dashboard KPI).</summary>
        public int CountOverdue => WorkOrders.Count(o =>
            (string.Equals(o.Status, "active", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(o.Status, "otvoren", StringComparison.OrdinalIgnoreCase))
            && o.DateClose.HasValue && o.DateClose.Value.ToUniversalTime() < DateTime.UtcNow);

        /// <summary>Can the current user edit work orders? (admin/superadmin)</summary>
        public bool CanEditWorkOrders => _authService.CurrentUser?.Role == "admin" || _authService.CurrentUser?.Role == "superadmin";

        #endregion

        #region === Commands ===

        public ICommand LoadWorkOrdersCommand { get; }
        public ICommand AddWorkOrderCommand { get; }
        public ICommand UpdateWorkOrderCommand { get; }
        public ICommand DeleteWorkOrderCommand { get; }
        public ICommand RollbackWorkOrderCommand { get; }
        public ICommand ExportWorkOrdersCommand { get; }
        public ICommand ApproveWorkOrderCommand { get; }
        public ICommand CloseWorkOrderCommand { get; }
        public ICommand EscalateWorkOrderCommand { get; }
        public ICommand AddCommentCommand { get; }
        public ICommand FilterChangedCommand { get; }

        #endregion

        #region === Methods ===

        /// <summary>Loads all work orders, including full KPI refresh and filtering.</summary>
        public async Task LoadWorkOrdersAsync()
        {
            IsBusy = true;
            try
            {
                var workOrders = await _dbService.GetAllWorkOrdersFullAsync().ConfigureAwait(false) ?? new System.Collections.Generic.List<WorkOrder>();
                WorkOrders = new ObservableCollection<WorkOrder>(workOrders);
                FilterWorkOrders();
                StatusMessage = $"Loaded {WorkOrders.Count} work orders.";
                OnPropertyChanged(nameof(CountOpen));
                OnPropertyChanged(nameof(CountCritical));
                OnPropertyChanged(nameof(CountOverdue));
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading work orders: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Adds a new work order, digitally signed and logged.</summary>
        public async Task AddWorkOrderAsync()
        {
            if (SelectedWorkOrder == null) { StatusMessage = "No work order selected."; return; }
            IsBusy = true;
            try
            {
                var actorId = _authService.CurrentUser?.Id ?? 0;
                SelectedWorkOrder.DigitalSignature = ComputeSignature(SelectedWorkOrder, _currentSessionId, _currentDeviceInfo);

                await _dbService.AddWorkOrderAsync(SelectedWorkOrder, actorId, _currentIpAddress, _currentDeviceInfo, _currentSessionId).ConfigureAwait(false);
                await _dbService.LogWorkOrderAuditAsync(SelectedWorkOrder.Id, actorId, "CREATE", null, _currentIpAddress, _currentDeviceInfo).ConfigureAwait(false);

                StatusMessage = $"Work order '{(SelectedWorkOrder.Title ?? $"#{SelectedWorkOrder.Id}")}' added.";
                await LoadWorkOrdersAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Add failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Updates a work order (full audit, status, signature, versioning).</summary>
        public async Task UpdateWorkOrderAsync()
        {
            if (SelectedWorkOrder == null) { StatusMessage = "No work order selected."; return; }
            IsBusy = true;
            try
            {
                var actorId = _authService.CurrentUser?.Id ?? 0;
                SelectedWorkOrder.DigitalSignature = ComputeSignature(SelectedWorkOrder, _currentSessionId, _currentDeviceInfo);

                await _dbService.InsertOrUpdateWorkOrderAsync(SelectedWorkOrder, update: true, actorUserId: actorId, ip: _currentIpAddress, deviceInfo: _currentDeviceInfo, sessionId: _currentSessionId, signatureMetadata: null).ConfigureAwait(false);
                await _dbService.LogWorkOrderAuditAsync(SelectedWorkOrder.Id, actorId, "UPDATE", null, _currentIpAddress, _currentDeviceInfo).ConfigureAwait(false);

                StatusMessage = $"Work order '{(SelectedWorkOrder.Title ?? $"#{SelectedWorkOrder.Id}")}' updated.";
                await LoadWorkOrdersAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Update failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Deletes a work order, with full audit/forensics.</summary>
        public async Task DeleteWorkOrderAsync()
        {
            if (SelectedWorkOrder == null) { StatusMessage = "No work order selected."; return; }
            IsBusy = true;
            try
            {
                var actorId = _authService.CurrentUser?.Id ?? 0;

                await _dbService.DeleteWorkOrderAsync(SelectedWorkOrder.Id, actorId, _currentIpAddress, _currentDeviceInfo, _currentSessionId).ConfigureAwait(false);
                await _dbService.LogWorkOrderAuditAsync(SelectedWorkOrder.Id, actorId, "DELETE", null, _currentIpAddress, _currentDeviceInfo).ConfigureAwait(false);

                StatusMessage = $"Work order '{(SelectedWorkOrder.Title ?? $"#{SelectedWorkOrder.Id}")}' deleted.";
                await LoadWorkOrdersAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Delete failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Records a rollback request for the selected work order (server-side restore may be separate).</summary>
        public async Task RollbackWorkOrderAsync()
        {
            if (SelectedWorkOrder == null) { StatusMessage = "No work order selected."; return; }
            IsBusy = true;
            try
            {
                var actorId = _authService.CurrentUser?.Id ?? 0;
                await _dbService.LogWorkOrderAuditAsync(SelectedWorkOrder.Id, actorId, "ROLLBACK", "Rollback requested", _currentIpAddress, _currentDeviceInfo).ConfigureAwait(false);

                StatusMessage = $"Rollback recorded for work order '{(SelectedWorkOrder.Title ?? $"#{SelectedWorkOrder.Id}")}'.";
                await LoadWorkOrdersAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Rollback failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Exports filtered work orders to file, with full audit.</summary>
        public async Task ExportWorkOrdersAsync()
        {
            IsBusy = true;
            try
            {
                var actorId = _authService.CurrentUser?.Id ?? 0;

                await _dbService.ExportWorkOrdersAsync(FilteredWorkOrders.ToList(), actorId, _currentIpAddress, _currentDeviceInfo, _currentSessionId).ConfigureAwait(false);
                await _dbService.LogWorkOrderAuditAsync(0, actorId, "EXPORT", null, _currentIpAddress, _currentDeviceInfo).ConfigureAwait(false);
                StatusMessage = "Work orders exported successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Approves a work order in workflow (digital signature, audit).</summary>
        public async Task ApproveWorkOrderAsync()
        {
            if (SelectedWorkOrder == null) { StatusMessage = "No work order selected."; return; }
            IsBusy = true;
            try
            {
                var actorId = _authService.CurrentUser?.Id ?? 0;

                await _dbService.ApproveWorkOrderAsync(SelectedWorkOrder.Id, actorId, note: "", ip: _currentIpAddress, device: _currentDeviceInfo).ConfigureAwait(false);
                await _dbService.LogWorkOrderAuditAsync(SelectedWorkOrder.Id, actorId, "APPROVE", null, _currentIpAddress, _currentDeviceInfo).ConfigureAwait(false);

                StatusMessage = $"Work order '{(SelectedWorkOrder.Title ?? $"#{SelectedWorkOrder.Id}")}' approved.";
                await LoadWorkOrdersAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Approval failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Closes a work order in workflow (digital signature, audit).</summary>
        public async Task CloseWorkOrderAsync()
        {
            if (SelectedWorkOrder == null) { StatusMessage = "No work order selected."; return; }
            IsBusy = true;
            try
            {
                var actorId = _authService.CurrentUser?.Id ?? 0;

                await _dbService.CloseWorkOrderAsync(SelectedWorkOrder.Id, actorId, note: "", ip: _currentIpAddress, device: _currentDeviceInfo).ConfigureAwait(false);
                await _dbService.LogWorkOrderAuditAsync(SelectedWorkOrder.Id, actorId, "CLOSE", null, _currentIpAddress, _currentDeviceInfo).ConfigureAwait(false);

                StatusMessage = $"Work order '{(SelectedWorkOrder.Title ?? $"#{SelectedWorkOrder.Id}")}' closed.";
                await LoadWorkOrdersAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Closure failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Escalates a work order for overdue or special handling.</summary>
        public async Task EscalateWorkOrderAsync()
        {
            if (SelectedWorkOrder == null) { StatusMessage = "No work order selected."; return; }
            IsBusy = true;
            try
            {
                var actorId = _authService.CurrentUser?.Id ?? 0;

                await _dbService.EscalateWorkOrderAsync(SelectedWorkOrder.Id, actorId, note: "", ip: _currentIpAddress, device: _currentDeviceInfo).ConfigureAwait(false);
                await _dbService.LogWorkOrderAuditAsync(SelectedWorkOrder.Id, actorId, "ESCALATE", null, _currentIpAddress, _currentDeviceInfo).ConfigureAwait(false);

                StatusMessage = $"Work order '{(SelectedWorkOrder.Title ?? $"#{SelectedWorkOrder.Id}")}' escalated.";
                await LoadWorkOrdersAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Escalation failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Adds a comment to the selected work order, logs audit.</summary>
        /// <param name="comment">Comment text (nullable to match command delegate); ignored if null/empty.</param>
        public async Task AddCommentAsync(string? comment)
        {
            if (SelectedWorkOrder == null || string.IsNullOrWhiteSpace(comment))
            {
                StatusMessage = "No work order or empty comment.";
                return;
            }
            IsBusy = true;
            try
            {
                var actorId = _authService.CurrentUser?.Id ?? 0;
                await _dbService.AddWorkOrderCommentAsync(SelectedWorkOrder.Id, actorId, comment).ConfigureAwait(false);
                await _dbService.LogWorkOrderAuditAsync(SelectedWorkOrder.Id, actorId, "COMMENT", comment, _currentIpAddress, _currentDeviceInfo).ConfigureAwait(false);

                StatusMessage = "Comment added.";
                await LoadWorkOrdersAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Comment failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Real-time filter for work orders (by machine name, title, priority, status).</summary>
                public void FilterWorkOrders()
        {
            var filtered = WorkOrders.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                filtered = filtered.Where(w =>
                    (w.Machine?.Name?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (w.Title?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (w.Description?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            if (!string.IsNullOrWhiteSpace(StatusFilter))
                filtered = filtered.Where(w => string.Equals(w.Status, StatusFilter, StringComparison.Ordinal));
            if (!string.IsNullOrWhiteSpace(AssetFilter))
                filtered = filtered.Where(w => string.Equals(w.Machine?.Name, AssetFilter, StringComparison.Ordinal));
            if (!string.IsNullOrWhiteSpace(PriorityFilter))
                filtered = filtered.Where(w => string.Equals(w.Priority, PriorityFilter, StringComparison.Ordinal));

            if (IdFilter.HasValue) filtered = filtered.Where(w => w.Id == IdFilter.Value);
            if (OpenFrom.HasValue) filtered = filtered.Where(w => w.DateOpen >= OpenFrom.Value);
            if (OpenTo.HasValue)   filtered = filtered.Where(w => w.DateOpen <= OpenTo.Value);
            if (CloseFrom.HasValue) filtered = filtered.Where(w => w.DateClose.HasValue && w.DateClose.Value >= CloseFrom.Value);
            if (CloseTo.HasValue)   filtered = filtered.Where(w => w.DateClose.HasValue && w.DateClose.Value <= CloseTo.Value);

            if (PhotosMin.HasValue) filtered = filtered.Where(w => w.PhotosCount >= PhotosMin.Value);
            if (PartsMin.HasValue)  filtered = filtered.Where(w => w.PartsCount >= PartsMin.Value);

            FilteredWorkOrders = new ObservableCollection<WorkOrder>(filtered);
        }
        #endregion

        #region === Audit/Auxiliary ===

        /// <summary>Loads audit history for a specific work order and maps DTO → model if needed.</summary>
        public async Task<ObservableCollection<AuditLogEntry>> LoadWorkOrderAuditAsync(int workOrderId)
        {
            var raw = await _dbService.GetAuditLogForEntityAsync("work_orders", workOrderId).ConfigureAwait(false);
            var mapped = raw?.Select(MapAuditEntryToModel) ?? Enumerable.Empty<AuditLogEntry>();
            return new ObservableCollection<AuditLogEntry>(mapped);
        }

        private static AuditLogEntry MapAuditEntryToModel(object src)
        {
            if (src is AuditLogEntry a) return a;

            object? Get(string n1, string? n2 = null, string? n3 = null)
            {
                var t = src.GetType();
                var p = t.GetProperty(n1, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                        ?? (n2 != null ? t.GetProperty(n2, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase) : null)
                        ?? (n3 != null ? t.GetProperty(n3, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase) : null);
                return p?.GetValue(src);
            }

            T? Cast<T>(object? o)
            {
                if (o is null || o is DBNull) return default;
                try { return (T)Convert.ChangeType(o, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T)); }
                catch { return default; }
            }

            return new AuditLogEntry
            {
                Id          = Cast<int>(Get("Id")),
                TableName   = Cast<string>(Get("Table", "TableName", "EntityTable")) ?? "work_orders",
                EntityId    = Cast<int?>(Get("EntityId", "RecordId", "TargetId")),
                Action      = Cast<string>(Get("Action", "EventType")) ?? string.Empty,
                Description = Cast<string>(Get("Description", "Details", "Message")),
                Timestamp   = Cast<DateTime?>(Get("Timestamp", "ChangedAt", "CreatedAt")) ?? DateTime.UtcNow,
                UserId      = Cast<int?>(Get("UserId", "ActorUserId", "ChangedBy")),
                SourceIp    = Cast<string>(Get("SourceIp", "Ip", "IPAddress")),
                DeviceInfo  = Cast<string>(Get("DeviceInfo", "Device")),
                SessionId   = Cast<string>(Get("SessionId", "Session"))
            };
        }

        /// <summary>
        /// Computes a GMP-grade digital signature for any entity using a canonical,
        /// reflection-based snapshot salted with session/device info. SHA-256, hex.
        /// </summary>
        private static string ComputeSignature(object entity, string sessionId, string deviceInfo)
        {
            if (entity == null) return string.Empty;

            var sb = new StringBuilder();
            sb.Append("SID=").Append(sessionId ?? "").Append('|');
            sb.Append("DEV=").Append(deviceInfo ?? "").Append('|');

            var props = entity.GetType()
                              .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                              .Where(p => p.CanRead)
                              .OrderBy(p => p.Name, StringComparer.Ordinal);

            foreach (var p in props)
            {
                object? val;
                try { val = p.GetValue(entity); }
                catch { continue; }

                var str = val switch
                {
                    null => "",
                    DateTime dt => dt.ToUniversalTime().ToString("O"),
                    DateTimeOffset dto => dto.ToUniversalTime().ToString("O"),
                    byte[] bytes => Convert.ToBase64String(bytes),
                    _ => val.ToString() ?? ""
                };

                sb.Append(p.Name).Append('=').Append(str).Append(';');
            }

            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
            var hex = new StringBuilder(hash.Length * 2);
            foreach (var b in hash) hex.Append(b.ToString("X2"));
            return hex.ToString();
        }

        #endregion

        #region === INotifyPropertyChanged & helpers ===

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

        /// <summary>Notifies all AsyncRelayCommands that depend on selection/busy state.</summary>
        private void NotifyCommandsCanExecuteChanged()
        {
            (LoadWorkOrdersCommand      as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (AddWorkOrderCommand        as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (UpdateWorkOrderCommand     as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (DeleteWorkOrderCommand     as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (RollbackWorkOrderCommand   as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (ExportWorkOrdersCommand    as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (ApproveWorkOrderCommand    as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (CloseWorkOrderCommand      as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (EscalateWorkOrderCommand   as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (AddCommentCommand          as AsyncRelayCommand<string?>)?.NotifyCanExecuteChanged();
        }

        #endregion
    }
}





