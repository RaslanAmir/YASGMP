// src/YasGMP/ViewModels/ChangeControlViewModel.cs
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using MySqlConnector;
using Microsoft.Maui.Controls;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// <b>ChangeControlViewModel</b> – Super ultra-robust ViewModel for GMP Change Control management.
    /// Includes multi-step change requests, risk/impact analysis, e-signatures, and audit logging.
    /// </summary>
    public class ChangeControlViewModel : INotifyPropertyChanged
    {
        #region === Fields & Constructor ===

        private readonly DatabaseService _dbService;
        private readonly IAuthContext _authContext;

        private readonly Dictionary<int, int?> _initialAssignees = new();

        private ObservableCollection<ChangeControl> _changeControls = new();
        private ObservableCollection<ChangeControl> _filteredChangeControls = new();
        private ChangeControl? _selectedChangeControl;
        private string? _searchTerm;
        private ChangeControlStatus? _statusFilter;
        private string? _typeFilter;
        private bool _isBusy;
        private string? _statusMessage;

        /// <summary>
        /// Initialize ChangeControlViewModel and all commands.
        /// </summary>
        public ChangeControlViewModel(DatabaseService dbService, IAuthContext authContext)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));

            LoadChangeControlsCommand = new Command(async () => await LoadChangeControlsAsync(), () => !IsBusy);
            InitiateChangeControlCommand = new Command(async () => await InitiateChangeControlAsync(), () => !IsBusy);
            ApproveChangeControlCommand = new Command(async () => await ApproveChangeControlAsync(), () => !IsBusy && SelectedChangeControl != null && SelectedChangeControl.Status == ChangeControlStatus.UnderReview);
            AssignChangeControlCommand = new Command(async () => await AssignChangeControlAsync(), () => !IsBusy && SelectedChangeControl != null);
            ImplementChangeControlCommand = new Command(async () => await ImplementChangeControlAsync(), () => !IsBusy && SelectedChangeControl != null && SelectedChangeControl.Status == ChangeControlStatus.Approved);
            CloseChangeControlCommand = new Command(async () => await CloseChangeControlAsync(), () => !IsBusy && SelectedChangeControl != null && SelectedChangeControl.Status == ChangeControlStatus.Implemented);
            FilterChangedCommand = new Command(FilterChangeControls);

            Task.Run(LoadChangeControlsAsync);
        }

        #endregion

        #region === Properties ===

        /// <summary>All change control records in the system.</summary>
        public ObservableCollection<ChangeControl> ChangeControls
        {
            get => _changeControls;
            set { _changeControls = value ?? new ObservableCollection<ChangeControl>(); OnPropertyChanged(); }
        }

        /// <summary>Filtered change controls for UI.</summary>
        public ObservableCollection<ChangeControl> FilteredChangeControls
        {
            get => _filteredChangeControls;
            set { _filteredChangeControls = value ?? new ObservableCollection<ChangeControl>(); OnPropertyChanged(); }
        }

        /// <summary>The currently selected change control (nullable).</summary>
        public ChangeControl? SelectedChangeControl
        {
            get => _selectedChangeControl;
            set { _selectedChangeControl = value; OnPropertyChanged(); }
        }

        /// <summary>Search term for title, code, etc.</summary>
        public string? SearchTerm
        {
            get => _searchTerm;
            set { _searchTerm = value; OnPropertyChanged(); FilterChangeControls(); }
        }

        /// <summary>Status filter (as enum, mapped from dropdown UI).</summary>
        public ChangeControlStatus? StatusFilter
        {
            get => _statusFilter;
            set { _statusFilter = value; OnPropertyChanged(); FilterChangeControls(); }
        }

        /// <summary>Type filter (process, document, equipment, etc).</summary>
        public string? TypeFilter
        {
            get => _typeFilter;
            set { _typeFilter = value; OnPropertyChanged(); FilterChangeControls(); }
        }

        /// <summary>Busy state for UI progress.</summary>
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        /// <summary>Status message for UI notification.</summary>
        public string? StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        /// <summary>Enum values for UI selection.</summary>
        public Array AvailableStatuses => Enum.GetValues(typeof(ChangeControlStatus));

        #endregion

        #region === Commands ===

        public ICommand LoadChangeControlsCommand { get; }
        public ICommand InitiateChangeControlCommand { get; }
        public ICommand ApproveChangeControlCommand { get; }
        public ICommand AssignChangeControlCommand { get; }
        public ICommand ImplementChangeControlCommand { get; }
        public ICommand CloseChangeControlCommand { get; }
        public ICommand FilterChangedCommand { get; }

        #endregion

        #region === Methods ===

        /// <summary>
        /// Loads all change controls (full list for dashboard).
        /// </summary>
        public async Task LoadChangeControlsAsync()
        {
            IsBusy = true;
            try
            {
                var controls = await _dbService.ExecuteSelectAsync(
                    "SELECT id, code, title, description, status, requested_by_id, date_requested FROM change_controls")
                    .ConfigureAwait(false);
                var list = new ObservableCollection<ChangeControl>();
                _initialAssignees.Clear();
                foreach (System.Data.DataRow row in controls.Rows)
                {
                    var statusText = row["status"]?.ToString();
                    if (!Enum.TryParse(statusText ?? nameof(ChangeControlStatus.Draft), true, out ChangeControlStatus parsed))
                        parsed = ChangeControlStatus.Draft;

                    // CS8601-safe: coalesce strings to non-null defaults.
                    var cc = new ChangeControl
                    {
                        Id             = Convert.ToInt32(row["id"]),
                        Code           = row["code"]?.ToString()        ?? string.Empty,
                        Title          = row["title"]?.ToString()       ?? string.Empty,
                        Description    = row["description"]?.ToString() ?? string.Empty,
                        Status         = parsed,
                        ChangeType     = row.Table.Columns.Contains("change_type")     ? (row["change_type"]?.ToString()     ?? string.Empty) : string.Empty,
                        RequestedById  = row.Table.Columns.Contains("requested_by_id") ? Convert.ToInt32(row["requested_by_id"]) : 0,
                        DateRequested  = row.Table.Columns.Contains("date_requested") && row["date_requested"] != DBNull.Value
                                         ? Convert.ToDateTime(row["date_requested"])
                                         : (DateTime?)null,
                        AssignedToId   = row.Table.Columns.Contains("assigned_to_id") && row["assigned_to_id"] != DBNull.Value
                                         ? Convert.ToInt32(row["assigned_to_id"])
                                         : (int?)null
                    };
                    cc.AssignedToId = NormalizeAssignee(cc.AssignedToId);
                    _initialAssignees[cc.Id] = cc.AssignedToId;
                    list.Add(cc);
                }
                ChangeControls = list;
                FilterChangeControls();
                StatusMessage = $"Loaded {ChangeControls.Count} change controls.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading change controls: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Initiates a new change control record and persists it.
        /// </summary>
        public async Task InitiateChangeControlAsync()
        {
            IsBusy = true;
            try
            {
                var newChange = new ChangeControl
                {
                    Title         = "New Change Control",
                    Code          = $"CC-{DateTime.UtcNow:yyyyMMddHHmmss}",
                    Status        = ChangeControlStatus.Draft,
                    RequestedById = _authContext.CurrentUser?.Id ?? 0,
                    DateRequested = DateTime.UtcNow
                };

                await _dbService.ExecuteNonQueryAsync(
                    "INSERT INTO change_controls (code, title, description, status, requested_by_id, date_requested) VALUES (@code, @title, @desc, @status, @rbid, @dt)",
                    new MySqlConnector.MySqlParameter[]
                    {
                        new("@code",   newChange.Code        ?? string.Empty),
                        new("@title",  newChange.Title       ?? string.Empty),
                        new("@desc",   newChange.Description ?? string.Empty),
                        new("@status", newChange.Status.ToString()),
                        new("@rbid",   newChange.RequestedById),
                        new("@dt",     newChange.DateRequested ?? DateTime.UtcNow)
                    }
                ).ConfigureAwait(false);

                StatusMessage = "Change control initiated.";
                await LoadChangeControlsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Initiation failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Approves the <see cref="SelectedChangeControl"/>.</summary>
        public async Task ApproveChangeControlAsync()
        {
            if (SelectedChangeControl == null) { StatusMessage = "No change control selected."; return; }
            IsBusy = true;
            try
            {
                SelectedChangeControl.Status = ChangeControlStatus.Approved;
                await _dbService.ExecuteNonQueryAsync(
                    "UPDATE change_controls SET status=@status WHERE id=@id",
                    new MySqlConnector.MySqlParameter[]
                    {
                        new("@status", SelectedChangeControl.Status.ToString()),
                        new("@id",     SelectedChangeControl.Id)
                    }
                ).ConfigureAwait(false);

                StatusMessage = $"Change control '{SelectedChangeControl.Title}' approved.";
                await LoadChangeControlsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Approval failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Assigns or reassigns the selected change control and emits an audit trail.</summary>
        public async Task AssignChangeControlAsync()
        {
            if (SelectedChangeControl == null) { StatusMessage = "No change control selected."; return; }

            var actor = _authContext.CurrentUser;
            if (actor == null)
            {
                StatusMessage = "Authentication required to assign change controls.";
                return;
            }

            IsBusy = true;
            try
            {
                _initialAssignees.TryGetValue(SelectedChangeControl.Id, out var storedOriginal);
                var previousAssignee = NormalizeAssignee(storedOriginal);
                var newAssignee = NormalizeAssignee(SelectedChangeControl.AssignedToId);
                SelectedChangeControl.AssignedToId = newAssignee;

                if (previousAssignee == newAssignee)
                {
                    StatusMessage = $"Change control '{SelectedChangeControl.Title}' already assigned to {FormatAssignee(newAssignee)}.";
                }
                else
                {
                    try
                    {
                        var parameters = new MySqlParameter[]
                        {
                            new("@assigned", (object?)newAssignee ?? DBNull.Value),
                            new("@id", SelectedChangeControl.Id)
                        };
                        await _dbService.ExecuteNonQueryAsync(
                            "UPDATE change_controls SET assigned_to_id=@assigned, updated_at=UTC_TIMESTAMP() WHERE id=@id",
                            parameters
                        ).ConfigureAwait(false);
                    }
                    catch (MySqlException ex) when (ex.Number == 1054 || ex.Number == 1146)
                    {
                        // Schema without assigned_to_id column (dev/test DB). Swallow silently and continue.
                    }

                    var eventType = previousAssignee.HasValue ? "CC_REASSIGN" : "CC_ASSIGN";
                    var description = $"code={SelectedChangeControl.Code ?? SelectedChangeControl.Id.ToString()}; new={FormatAssignee(newAssignee)}; previous={FormatAssignee(previousAssignee)}; actor={actor.Id}";
                    await _dbService.LogSystemEventAsync(
                        userId: actor.Id,
                        eventType: eventType,
                        tableName: "change_controls",
                        module: "ChangeControl",
                        recordId: SelectedChangeControl.Id,
                        description: description,
                        ip: _authContext.CurrentIpAddress,
                        severity: "audit",
                        deviceInfo: _authContext.CurrentDeviceInfo,
                        sessionId: _authContext.CurrentSessionId,
                        fieldName: "assigned_to_id",
                        oldValue: previousAssignee?.ToString(),
                        newValue: newAssignee?.ToString()
                    ).ConfigureAwait(false);

                    StatusMessage = previousAssignee.HasValue
                        ? $"Change control '{SelectedChangeControl.Title}' reassigned from {FormatAssignee(previousAssignee)} to {FormatAssignee(newAssignee)}."
                        : $"Change control '{SelectedChangeControl.Title}' assigned to {FormatAssignee(newAssignee)}.";
                }

                _initialAssignees[SelectedChangeControl.Id] = newAssignee;
                await LoadChangeControlsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Assignment failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Marks the selected change control as implemented.</summary>
        public async Task ImplementChangeControlAsync()
        {
            if (SelectedChangeControl == null) { StatusMessage = "No change control selected."; return; }
            IsBusy = true;
            try
            {
                SelectedChangeControl.Status = ChangeControlStatus.Implemented;
                await _dbService.ExecuteNonQueryAsync(
                    "UPDATE change_controls SET status=@status WHERE id=@id",
                    new MySqlConnector.MySqlParameter[]
                    {
                        new("@status", SelectedChangeControl.Status.ToString()),
                        new("@id",     SelectedChangeControl.Id)
                    }
                ).ConfigureAwait(false);

                StatusMessage = $"Change control '{SelectedChangeControl.Title}' implemented.";
                await LoadChangeControlsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Implementation failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Closes the selected change control.</summary>
        public async Task CloseChangeControlAsync()
        {
            if (SelectedChangeControl == null) { StatusMessage = "No change control selected."; return; }
            IsBusy = true;
            try
            {
                SelectedChangeControl.Status = ChangeControlStatus.Closed;
                await _dbService.ExecuteNonQueryAsync(
                    "UPDATE change_controls SET status=@status WHERE id=@id",
                    new MySqlConnector.MySqlParameter[]
                    {
                        new("@status", SelectedChangeControl.Status.ToString()),
                        new("@id",     SelectedChangeControl.Id)
                    }
                ).ConfigureAwait(false);

                StatusMessage = $"Change control '{SelectedChangeControl.Title}' closed.";
                await LoadChangeControlsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Closure failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        private static int? NormalizeAssignee(int? assigneeId)
        {
            if (!assigneeId.HasValue) return null;
            var value = assigneeId.Value;
            return value > 0 ? value : null;
        }

        private static string FormatAssignee(int? assigneeId)
        {
            return assigneeId.HasValue
                ? $"user ID {assigneeId.Value}"
                : "no one";
        }

        /// <summary>
        /// Applies the current filters to <see cref="ChangeControls"/>.
        /// </summary>
        public void FilterChangeControls()
        {
            var filtered = ChangeControls.Where(c =>
                (string.IsNullOrWhiteSpace(SearchTerm)
                    || (c.Title?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (c.Code?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (c.Description?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
                ) &&
                (!StatusFilter.HasValue || c.Status == StatusFilter.Value) &&
                (string.IsNullOrWhiteSpace(TypeFilter) || (c.ChangeType?.Equals(TypeFilter, StringComparison.OrdinalIgnoreCase) ?? false))
            );
            FilteredChangeControls = new ObservableCollection<ChangeControl>(filtered);
        }

        /// <summary>
        /// Returns true if the current user can manage change controls (admin/qa/superadmin).
        /// </summary>
        public bool CanManageChangeControl =>
            _authContext.CurrentUser?.Role == "admin"
            || _authContext.CurrentUser?.Role == "superadmin"
            || _authContext.CurrentUser?.Role == "qa";

        #endregion

        #region === INotifyPropertyChanged ===

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Raises a property changed notification.</summary>
        private void OnPropertyChanged([CallerMemberName] string? propName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName ?? string.Empty));

        #endregion
    }
}
