// C:\Projects\YasGMP\ViewModels\PartViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using System.Collections.Generic;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Helpers;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Models.DTO;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// <b>PartViewModel</b> — ViewModel for spare part inventory management in a .NET MAUI MVVM application.
    /// <para>
    /// Features:
    /// <list type="bullet">
    ///   <item><description>Async CRUD operations with audit-ready parameter flow (actor/session/IP/device).</description></item>
    ///   <item><description>Digital signature generation for Part 11 traceability.</description></item>
    ///   <item><description>Real-time filtering by search, category, and status.</description></item>
    ///   <item><description>Export of filtered view.</description></item>
    ///   <item><description>Safe, explicit <see cref="INotifyPropertyChanged"/> implementation with full XML docs.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public class PartViewModel : INotifyPropertyChanged
    {
        #region === Fields & Constructor =======================================================

        private readonly DatabaseService _dbService;
        private readonly AuthService _authService;

        private ObservableCollection<Part> _parts = new();
        private ObservableCollection<Part> _filteredParts = new();

        // CS8618 fixes: make reference types nullable where they are not initialized in the ctor.
        private Part? _selectedPart;
        private string? _searchTerm;
        private string? _categoryFilter;
        private string? _statusFilter;

        private bool _isBusy;

        // CS8618 fix: initialize to non-null.
        private string _statusMessage = string.Empty;

        // Coalesce auth context to non-null strings to satisfy typical DB parameter contracts.
        private readonly string _currentSessionId;
        private readonly string _currentDeviceInfo;
        private readonly string _currentIpAddress;

        /// <summary>
        /// Initializes a new instance of the <see cref="PartViewModel"/> class.
        /// </summary>
        /// <param name="dbService">Concrete database service used for persistence and exports.</param>
        /// <param name="authService">Authentication/context service providing current user and session info.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="dbService"/> or <paramref name="authService"/> is <c>null</c>.</exception>
        public PartViewModel(DatabaseService dbService, AuthService authService)
        {
            _dbService   = dbService   ?? throw new ArgumentNullException(nameof(dbService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            _currentSessionId = _authService.CurrentSessionId  ?? string.Empty;
            _currentDeviceInfo = _authService.CurrentDeviceInfo ?? string.Empty;
            _currentIpAddress  = _authService.CurrentIpAddress  ?? string.Empty;

            // Commands (kept consistent with original CanExecute semantics)
            LoadPartsCommand    = new AsyncRelayCommand(LoadPartsAsync);
            AddPartCommand      = new AsyncRelayCommand(AddPartAsync,      () => !IsBusy);
            UpdatePartCommand   = new AsyncRelayCommand(UpdatePartAsync,   () => !IsBusy && SelectedPart != null);
            DeletePartCommand   = new AsyncRelayCommand(DeletePartAsync,   () => !IsBusy && SelectedPart != null);
            RollbackPartCommand = new AsyncRelayCommand(RollbackPartAsync, () => !IsBusy && SelectedPart != null);
            ExportPartsCommand  = new AsyncRelayCommand(ExportPartsAsync,  () => !IsBusy);
            FilterChangedCommand = new RelayCommand(FilterParts);

            // Initial load (fire-and-forget; errors surface through StatusMessage)
            _ = LoadPartsAsync();
        }

        #endregion

        #region === Properties ================================================================

        /// <summary>
        /// Gets or sets the complete list of parts retrieved from the database.
        /// </summary>
        public ObservableCollection<Part> Parts
        {
            get => _parts;
            set { _parts = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Gets or sets the filtered view of <see cref="Parts"/> for UI binding.
        /// </summary>
        public ObservableCollection<Part> FilteredParts
        {
            get => _filteredParts;
            set { _filteredParts = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Gets or sets the currently selected part in the UI (may be <c>null</c>).
        /// </summary>
        public Part? SelectedPart
        {
            get => _selectedPart;
            set
            {
                _selectedPart = value;
                OnPropertyChanged();
                // Update command CanExecute for commands that depend on a selection.
                (UpdatePartCommand   as AsyncRelayCommand)?.NotifyCanExecuteChanged();
                (DeletePartCommand   as AsyncRelayCommand)?.NotifyCanExecuteChanged();
                (RollbackPartCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            }
        }

        /// <summary>
        /// Gets or sets the free-text search term applied to Name/Code/Description.
        /// </summary>
        public string? SearchTerm
        {
            get => _searchTerm;
            set { _searchTerm = value; OnPropertyChanged(); FilterParts(); }
        }

        /// <summary>
        /// Gets or sets the category filter. When set, items must match <see cref="Part.Category"/>.
        /// </summary>
        public string? CategoryFilter
        {
            get => _categoryFilter;
            set { _categoryFilter = value; OnPropertyChanged(); FilterParts(); }
        }

        /// <summary>
        /// Gets or sets the status filter. When set, items must match <see cref="Part.Status"/>.
        /// </summary>
        public string? StatusFilter
        {
            get => _statusFilter;
            set { _statusFilter = value; OnPropertyChanged(); FilterParts(); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether an async operation is in progress.
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged();
                // Update command CanExecute for all commands that depend on IsBusy.
                (LoadPartsCommand    as AsyncRelayCommand)?.NotifyCanExecuteChanged();
                (AddPartCommand      as AsyncRelayCommand)?.NotifyCanExecuteChanged();
                (UpdatePartCommand   as AsyncRelayCommand)?.NotifyCanExecuteChanged();
                (DeletePartCommand   as AsyncRelayCommand)?.NotifyCanExecuteChanged();
                (RollbackPartCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
                (ExportPartsCommand  as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            }
        }

        /// <summary>
        /// Gets or sets the latest status or error message for UI display.
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Gets the list of available statuses for UI filtering.
        /// </summary>
        public string[] AvailableStatuses => new[] { "active", "inactive", "obsolete" };

        /// <summary>
        /// Gets a value indicating whether the current user can edit parts (admin / superadmin).
        /// </summary>
        public bool CanEditParts => _authService.CurrentUser?.Role == "admin" || _authService.CurrentUser?.Role == "superadmin";

        #endregion

        #region === Commands ==================================================================

        /// <summary>Command: Loads all parts from the database.</summary>
        public ICommand LoadPartsCommand { get; }

        /// <summary>Command: Adds a new part (uses current selection as buffer).</summary>
        public ICommand AddPartCommand { get; }

        /// <summary>Command: Updates the selected part.</summary>
        public ICommand UpdatePartCommand { get; }

        /// <summary>Command: Deletes the selected part.</summary>
        public ICommand DeletePartCommand { get; }

        /// <summary>Command: Rolls back the selected part (if supported by service).</summary>
        public ICommand RollbackPartCommand { get; }

        /// <summary>Command: Exports the filtered list of parts.</summary>
        public ICommand ExportPartsCommand { get; }

        /// <summary>Command: Explicitly triggers filtering (useful for UI events).</summary>
        public ICommand FilterChangedCommand { get; }

        #endregion

        #region === Methods ===================================================================

        /// <summary>
        /// Asynchronously loads all parts and applies current filters.
        /// </summary>
        /// <returns>A running <see cref="Task"/> that completes when loading finishes.</returns>
        public async Task LoadPartsAsync()
        {
            IsBusy = true;
            try
            {
                var parts = await _dbService.GetAllPartsAsync();
                Parts = new ObservableCollection<Part>(parts ?? Enumerable.Empty<Part>());
                FilterParts();
                StatusMessage = $"Loaded {Parts.Count} parts.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading parts: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Adds a new part. The selected part is used as the input buffer.
        /// </summary>
        public async Task AddPartAsync()
        {
            if (SelectedPart == null) { StatusMessage = "No part selected."; return; }
            IsBusy = true;
            try
            {
                // Generate a signature hash (if your DB layer stores/validates it, pass along as needed).
                string signatureHash = DigitalSignatureHelper.GenerateSignatureHash(SelectedPart, _currentSessionId, _currentDeviceInfo);
                _ = signatureHash; // Prepared for audit usage where applicable.

                int actorUserId = _authService.CurrentUser?.Id ?? 1;
                await _dbService.AddPartAsync(SelectedPart, actorUserId, _currentIpAddress, _currentDeviceInfo);

                StatusMessage = $"Part '{SelectedPart.Name}' added.";
                await LoadPartsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Add failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Updates the selected part with full audit compatibility.
        /// </summary>
        public async Task UpdatePartAsync()
        {
            if (SelectedPart == null) { StatusMessage = "No part selected."; return; }
            IsBusy = true;
            try
            {
                string signatureHash = DigitalSignatureHelper.GenerateSignatureHash(SelectedPart, _currentSessionId, _currentDeviceInfo);
                _ = signatureHash;

                int actorUserId = _authService.CurrentUser?.Id ?? 1;
                await _dbService.UpdatePartAsync(SelectedPart, actorUserId, _currentIpAddress, _currentDeviceInfo);

                StatusMessage = $"Part '{SelectedPart.Name}' updated.";
                await LoadPartsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Update failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Deletes the selected part and records a corresponding audit entry server-side.
        /// </summary>
        public async Task DeletePartAsync()
        {
            if (SelectedPart == null) { StatusMessage = "No part selected."; return; }
            IsBusy = true;
            try
            {
                int actorUserId = _authService.CurrentUser?.Id ?? 1;
                await _dbService.DeletePartAsync(SelectedPart.Id, actorUserId, _currentIpAddress, _currentDeviceInfo);

                StatusMessage = $"Part '{SelectedPart.Name}' deleted.";
                await LoadPartsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Delete failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Rolls back the selected part (server-side decides how snapshot/versioning is applied).
        /// </summary>
        public async Task RollbackPartAsync()
        {
            if (SelectedPart == null) { StatusMessage = "No part selected."; return; }
            IsBusy = true;
            try
            {
                int actorUserId = _authService.CurrentUser?.Id ?? 1;
                await _dbService.RollbackPartAsync(SelectedPart.Id, actorUserId, _currentIpAddress, _currentDeviceInfo, _currentSessionId);

                StatusMessage = $"Rollback completed for part '{SelectedPart.Name}'.";
                await LoadPartsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Rollback failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Exports the current filtered list to a file using a chosen format, with audit logging.
        /// </summary>
        public async Task ExportPartsAsync()
        {
            IsBusy = true;
            try
            {
                int actorUserId = _authService.CurrentUser?.Id ?? 1;
                var fmt = await YasGMP.Helpers.ExportFormatPrompt.PromptAsync();
                await _dbService.ExportPartsAsync(FilteredParts.ToList(), fmt, actorUserId, _currentIpAddress, _currentDeviceInfo, _currentSessionId);
                StatusMessage = "Parts exported successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Applies <see cref="SearchTerm"/>, <see cref="CategoryFilter"/>, and <see cref="StatusFilter"/> to <see cref="Parts"/>.
        /// </summary>
        public void FilterParts()
        {
            var filtered = Parts.Where(p =>
                (string.IsNullOrWhiteSpace(SearchTerm)
                    || (!string.IsNullOrEmpty(p.Name)        && p.Name.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                    || (!string.IsNullOrEmpty(p.Code)        && p.Code.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                    || (!string.IsNullOrEmpty(p.Description) && p.Description.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)))
                && (string.IsNullOrWhiteSpace(CategoryFilter) || string.Equals(p.Category, CategoryFilter, StringComparison.OrdinalIgnoreCase))
                && (string.IsNullOrWhiteSpace(StatusFilter)   || string.Equals(p.Status,   StatusFilter,   StringComparison.OrdinalIgnoreCase))
            );

            FilteredParts = new ObservableCollection<Part>(filtered);
        }

        /// <summary>
        /// Loads audit history for a specific part via the generic entity audit API.
        /// </summary>
        /// <param name="partId">Primary key of the part.</param>
        /// <returns>Observable collection of <see cref="AuditEntryDto"/> items.</returns>
        public async Task<ObservableCollection<AuditEntryDto>> LoadPartAuditAsync(int partId)
        {
            var audits = await _dbService.GetAuditLogForEntityAsync("parts", partId);
            return new ObservableCollection<AuditEntryDto>(audits ?? new List<AuditEntryDto>());
        }

        #endregion

        #region === INotifyPropertyChanged ====================================================

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged; // CS8612/CS8618 fixes: nullable event delegate.

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for the specified property name.
        /// </summary>
        /// <param name="propName">The name of the property that changed (optional).</param>
        private void OnPropertyChanged([CallerMemberName] string? propName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName ?? string.Empty)); // CS8625 fix.

        #endregion
    }
}
