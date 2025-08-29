using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Helpers;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// <b>ComponentViewModel</b> – Ultra-robust ViewModel for machine component management.
    /// Handles async CRUD, filtering, export (placeholder), and audit mapping with full null-safety.
    /// </summary>
    public class ComponentViewModel : INotifyPropertyChanged
    {
        #region === FIELDS & DEPENDENCY INJECTION ===

        private readonly DatabaseService _dbService;
        private readonly AuthService _authService;

        private ObservableCollection<MachineComponent> _components = new();
        private ObservableCollection<MachineComponent> _filteredComponents = new();
        private MachineComponent? _selectedComponent;
        private string? _searchTerm;
        private string? _statusFilter;
        private bool _isBusy;
        private string? _statusMessage;

        // Session context (nullable-safe storage; callers coalesce as needed)
        private readonly string? _currentSessionId;
        private readonly string? _currentDeviceInfo;
        private readonly string? _currentIpAddress;

        #endregion

        #region === CONSTRUCTOR ===

        /// <summary>
        /// Initializes <see cref="ComponentViewModel"/> and wires up commands/dependencies.
        /// </summary>
        /// <param name="dbService">Database access service (DI).</param>
        /// <param name="authService">Authentication/session service (DI).</param>
        /// <exception cref="ArgumentNullException">Thrown if any dependency is null.</exception>
        public ComponentViewModel(DatabaseService dbService, AuthService authService)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            _currentSessionId = _authService.CurrentSessionId;
            _currentDeviceInfo = _authService.CurrentDeviceInfo;
            _currentIpAddress = _authService.CurrentIpAddress;

            LoadComponentsCommand = new AsyncRelayCommand(LoadComponentsAsync);
            AddComponentCommand = new AsyncRelayCommand(AddComponentAsync, () => !IsBusy && SelectedComponent != null);
            UpdateComponentCommand = new AsyncRelayCommand(UpdateComponentAsync, () => !IsBusy && SelectedComponent != null);
            DeleteComponentCommand = new AsyncRelayCommand(DeleteComponentAsync, () => !IsBusy && SelectedComponent != null);
            ExportComponentsCommand = new AsyncRelayCommand(ExportComponentsAsync, () => !IsBusy);
            FilterChangedCommand = new RelayCommand(FilterComponents);

            _ = LoadComponentsAsync();
        }

        #endregion

        #region === PROPERTIES ===

        /// <summary>
        /// All components in the system.
        /// </summary>
        public ObservableCollection<MachineComponent> Components
        {
            get => _components;
            set { _components = value ?? new ObservableCollection<MachineComponent>(); OnPropertyChanged(); }
        }

        /// <summary>
        /// Filtered components for UI display.
        /// </summary>
        public ObservableCollection<MachineComponent> FilteredComponents
        {
            get => _filteredComponents;
            set { _filteredComponents = value ?? new ObservableCollection<MachineComponent>(); OnPropertyChanged(); }
        }

        /// <summary>
        /// The component currently selected in the UI (nullable).
        /// </summary>
        public MachineComponent? SelectedComponent
        {
            get => _selectedComponent;
            set { _selectedComponent = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Search term (component name, code, supplier, etc).
        /// </summary>
        public string? SearchTerm
        {
            get => _searchTerm;
            set { _searchTerm = value; OnPropertyChanged(); FilterComponents(); }
        }

        /// <summary>
        /// Component status filter (active, inactive, under_maintenance, etc).
        /// </summary>
        public string? StatusFilter
        {
            get => _statusFilter;
            set { _statusFilter = value; OnPropertyChanged(); FilterComponents(); }
        }

        /// <summary>
        /// Indicates if an operation is in progress.
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// UI status message (success/error/info).
        /// </summary>
        public string? StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Available component statuses.
        /// </summary>
        public string[] AvailableStatuses => new[] { "active", "inactive", "under_maintenance", "retired", "pending", "spare" };

        #endregion

        #region === COMMANDS ===

        /// <summary>Loads components from the database.</summary>
        public ICommand LoadComponentsCommand { get; }

        /// <summary>Adds the <see cref="SelectedComponent"/>.</summary>
        public ICommand AddComponentCommand { get; }

        /// <summary>Updates the <see cref="SelectedComponent"/>.</summary>
        public ICommand UpdateComponentCommand { get; }

        /// <summary>Deletes the <see cref="SelectedComponent"/>.</summary>
        public ICommand DeleteComponentCommand { get; }

        /// <summary>Exports filtered components (placeholder).</summary>
        public ICommand ExportComponentsCommand { get; }

        /// <summary>Signals filter changes from the UI.</summary>
        public ICommand FilterChangedCommand { get; }

        #endregion

        #region === CRUD METHODS ===

        /// <summary>
        /// Loads all components as <see cref="MachineComponent"/> from the database.
        /// </summary>
        public async Task LoadComponentsAsync()
        {
            IsBusy = true;
            try
            {
                // CS8601-safe: ensure non-null list when assigning to non-nullable variable.
                var componentList = (await _dbService.GetAllComponentsAsync().ConfigureAwait(false)) ?? new List<MachineComponent>();
                Components = new ObservableCollection<MachineComponent>(componentList);
                FilterComponents();
                StatusMessage = $"Loaded {Components.Count} components.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading components: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Adds a new component using <see cref="SelectedComponent"/>.
        /// </summary>
        public async Task AddComponentAsync()
        {
            if (SelectedComponent == null)
            {
                StatusMessage = "No component selected.";
                return;
            }
            IsBusy = true;
            try
            {
                await _dbService.InsertOrUpdateComponentAsync(
                    SelectedComponent,
                    isUpdate: false,
                    _authService.CurrentUser?.Id ?? 1,
                    _currentIpAddress ?? string.Empty,
                    _currentDeviceInfo ?? string.Empty,
                    default
                ).ConfigureAwait(false);

                StatusMessage = $"Component '{SelectedComponent.Name}' added.";
                await LoadComponentsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Add failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Updates the selected component in the data store.
        /// </summary>
        public async Task UpdateComponentAsync()
        {
            if (SelectedComponent == null)
            {
                StatusMessage = "No component selected.";
                return;
            }
            IsBusy = true;
            try
            {
                await _dbService.InsertOrUpdateComponentAsync(
                    SelectedComponent,
                    isUpdate: true,
                    _authService.CurrentUser?.Id ?? 1,
                    _currentIpAddress ?? string.Empty,
                    _currentDeviceInfo ?? string.Empty,
                    default
                ).ConfigureAwait(false);

                StatusMessage = $"Component '{SelectedComponent.Name}' updated.";
                await LoadComponentsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Update failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Deletes the selected component from the data store.
        /// </summary>
        public async Task DeleteComponentAsync()
        {
            if (SelectedComponent == null)
            {
                StatusMessage = "No component selected.";
                return;
            }
            IsBusy = true;
            try
            {
                await _dbService.DeleteComponentAsync(
                    SelectedComponent.Id,
                    _authService.CurrentUser?.Id ?? 1,
                    _currentIpAddress ?? string.Empty,
                    _currentDeviceInfo ?? string.Empty,
                    default
                ).ConfigureAwait(false);

                StatusMessage = $"Component '{SelectedComponent.Name}' deleted.";
                await LoadComponentsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Delete failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Exports all filtered components (placeholder hook; wire to real export service when available).
        /// </summary>
        public async Task ExportComponentsAsync()
        {
            IsBusy = true;
            try
            {
                await Task.Yield();
                StatusMessage = "Components exported successfully (placeholder).";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        #endregion

        #region === FILTERING & VIEWS ===

        /// <summary>
        /// Real-time filtering by search and status.
        /// </summary>
        public void FilterComponents()
        {
            var filtered = Components.Where(c =>
                (string.IsNullOrWhiteSpace(SearchTerm) ||
                    (c.Name?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.Code?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.Supplier?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (string.IsNullOrWhiteSpace(StatusFilter) || string.Equals(c.Status, StatusFilter, StringComparison.OrdinalIgnoreCase))
            );
            FilteredComponents = new ObservableCollection<MachineComponent>(filtered);
        }

        /// <summary>
        /// Whether the current user can perform edit operations (admin/superadmin).
        /// </summary>
        public bool CanEditComponents =>
            _authService.CurrentUser?.Role == "admin" || _authService.CurrentUser?.Role == "superadmin";

        #endregion

        #region === AUDIT ===

        /// <summary>
        /// Loads audit trail entries for a specific component and maps DTOs to domain model.
        /// </summary>
        /// <param name="componentId">Component identifier.</param>
        /// <returns>Observable collection of <see cref="AuditLogEntry"/>.</returns>
        public async Task<ObservableCollection<AuditLogEntry>> LoadComponentAuditAsync(int componentId)
        {
            var dtos = await _dbService.GetAuditLogForEntityAsync("components", componentId).ConfigureAwait(false)
                       ?? new List<YasGMP.Models.DTO.AuditEntryDto>();

            var mapped = dtos.Select(dto => new AuditLogEntry
            {
                Id               = dto.Id ?? 0,
                EntityType       = dto.Entity ?? string.Empty,
                EntityId         = int.TryParse(dto.EntityId, out var eid) ? eid : 0,
                PerformedBy      = !string.IsNullOrWhiteSpace(dto.Username) ? dto.Username : (dto.UserId?.ToString() ?? "Unknown"),
                Action           = dto.Action ?? string.Empty,
                OldValue         = dto.OldValue ?? string.Empty,
                NewValue         = dto.NewValue ?? string.Empty,
                ChangedAt        = dto.Timestamp,
                DeviceInfo       = dto.DeviceInfo ?? string.Empty,
                IpAddress        = dto.IpAddress ?? string.Empty,
                SessionId        = dto.SessionId ?? string.Empty,
                DigitalSignature = dto.DigitalSignature ?? string.Empty,
                Note             = dto.Note ?? string.Empty
            });

            return new ObservableCollection<AuditLogEntry>(mapped);
        }

        #endregion

        #region === INotifyPropertyChanged ===

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raises <see cref="PropertyChanged"/> with the given property name (or all when null/empty).
        /// </summary>
        /// <param name="propName">Property name (optional via CallerMemberName).</param>
        private void OnPropertyChanged([CallerMemberName] string? propName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName ?? string.Empty));

        #endregion
    }
}
