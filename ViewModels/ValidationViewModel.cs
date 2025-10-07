using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using YasGMP.Models;
using YasGMP.Services;
using CommunityToolkit.Mvvm.Input; 

namespace YasGMP.ViewModels
{
    /// <summary>
    /// Ultra-robust ViewModel for managing equipment/system validations.
    /// Distinct from qualification: covers process/software/CSV validation, protocol execution, etc.
    /// GMP/Annex 11/21 CFR Part 11 ready: async CRUD, full audit, digital signature, rollback/versioning,
    /// advanced filtering, protocol document links, and extensibility for future validation cycles.
    /// </summary>
    public class ValidationViewModel : INotifyPropertyChanged
    {
        #region Fields & Constructor

        private readonly DatabaseService _dbService;
        private readonly AuthService _authService;

        private ObservableCollection<Validation> _validations = new();
        private ObservableCollection<Validation> _filteredValidations = new();

        // Nullable: selections/filters can be absent
        private Validation? _selectedValidation;
        private string? _searchTerm;
        private string? _statusFilter;
        private string? _typeFilter;

        private bool _isBusy;
        private string? _statusMessage;

        private readonly string _currentSessionId;
        private readonly string _currentDeviceInfo;
        private readonly string _currentIpAddress;

        /// <summary>
        /// Initializes the ValidationViewModel with all commands and service dependencies.
        /// </summary>
        public ValidationViewModel(DatabaseService dbService, AuthService authService)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            // Canonical non-null context strings (prevents CS8618/CS8601 at field init)
            _currentSessionId = _authService.CurrentSessionId ?? string.Empty;
            _currentDeviceInfo = _authService.CurrentDeviceInfo ?? "ui";
            _currentIpAddress  = _authService.CurrentIpAddress  ?? "0.0.0.0";

            LoadValidationsCommand     = new AsyncRelayCommand(LoadValidationsAsync);
            AddValidationCommand       = new AsyncRelayCommand(AddValidationAsync,      () => !IsBusy);
            UpdateValidationCommand    = new AsyncRelayCommand(UpdateValidationAsync,   () => !IsBusy && SelectedValidation is not null);
            DeleteValidationCommand    = new AsyncRelayCommand(DeleteValidationAsync,   () => !IsBusy && SelectedValidation is not null);
            RollbackValidationCommand  = new AsyncRelayCommand(RollbackValidationAsync, () => !IsBusy && SelectedValidation is not null);
            ExportValidationsCommand   = new AsyncRelayCommand(ExportValidationsAsync,  () => !IsBusy);
            FilterChangedCommand       = new RelayCommand(FilterValidations);

            // Load data at startup
            _ = LoadValidationsAsync();
        }

        #endregion

        #region Properties

        /// <summary>All validations (process, CSV, software, etc.).</summary>
        public ObservableCollection<Validation> Validations
        {
            get => _validations;
            set { _validations = value; OnPropertyChanged(); }
        }

        /// <summary>Filtered validations for UI.</summary>
        public ObservableCollection<Validation> FilteredValidations
        {
            get => _filteredValidations;
            set { _filteredValidations = value; OnPropertyChanged(); }
        }

        /// <summary>The currently selected validation (or <c>null</c>).</summary>
        public Validation? SelectedValidation
        {
            get => _selectedValidation;
            set { _selectedValidation = value; OnPropertyChanged(); }
        }

        /// <summary>Search term for validation target, protocol, etc.</summary>
        public string? SearchTerm
        {
            get => _searchTerm;
            set { _searchTerm = value; OnPropertyChanged(); FilterValidations(); }
        }

        /// <summary>Status filter (valid, expired, scheduled, etc).</summary>
        public string? StatusFilter
        {
            get => _statusFilter;
            set { _statusFilter = value; OnPropertyChanged(); FilterValidations(); }
        }

        /// <summary>Validation type filter (process, software, CSV, PQ, etc).</summary>
        public string? TypeFilter
        {
            get => _typeFilter;
            set { _typeFilter = value; OnPropertyChanged(); FilterValidations(); }
        }

        /// <summary>Is an operation in progress?</summary>
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        /// <summary>UI status message.</summary>
        public string? StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        /// <summary>Available statuses for filtering.</summary>
        public string[] AvailableStatuses => new[] { "valid", "expired", "scheduled", "in_progress", "rejected" };

        /// <summary>Available validation types.</summary>
        public string[] AvailableTypes => new[] { "Process", "Software", "CSV", "PQ", "Other" };

        #endregion

        #region Commands
        /// <summary>
        /// Gets or sets the load validations command.
        /// </summary>

        public ICommand LoadValidationsCommand { get; }
        /// <summary>
        /// Gets or sets the add validation command.
        /// </summary>
        public ICommand AddValidationCommand { get; }
        /// <summary>
        /// Gets or sets the update validation command.
        /// </summary>
        public ICommand UpdateValidationCommand { get; }
        /// <summary>
        /// Gets or sets the delete validation command.
        /// </summary>
        public ICommand DeleteValidationCommand { get; }
        /// <summary>
        /// Gets or sets the rollback validation command.
        /// </summary>
        public ICommand RollbackValidationCommand { get; }
        /// <summary>
        /// Gets or sets the export validations command.
        /// </summary>
        public ICommand ExportValidationsCommand { get; }
        /// <summary>
        /// Gets or sets the filter changed command.
        /// </summary>
        public ICommand FilterChangedCommand { get; }

        #endregion

        #region Methods

        /// <summary>Loads all validations, including protocol docs and audit trail.</summary>
        public async Task LoadValidationsAsync()
        {
            IsBusy = true;
            try
            {
                var validations = await _dbService.GetAllValidationsAsync(includeAudit: true, includeProtocols: true, includeAttachments: true).ConfigureAwait(false);
                Validations = new ObservableCollection<Validation>(validations ?? new List<Validation>());
                FilterValidations();
                StatusMessage = $"Loaded {Validations.Count} validations.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading validations: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Adds a new validation (digital signature + audit).</summary>
        public async Task AddValidationAsync()
        {
            if (SelectedValidation is null) { StatusMessage = "No validation selected."; return; }
            IsBusy = true;
            try
            {
                string signatureHash = ComputeSignature(SelectedValidation, _currentSessionId, _currentDeviceInfo);

                await DatabaseServiceValidationsExtensions.AddValidationAsync(_dbService, SelectedValidation, signatureHash, _currentIpAddress, _currentDeviceInfo, _currentSessionId).ConfigureAwait(false);
                await _dbService.LogValidationAuditAsync(SelectedValidation, "CREATE", _currentIpAddress, _currentDeviceInfo, _currentSessionId, signatureHash).ConfigureAwait(false);

                StatusMessage = $"Validation '{SelectedValidation.ValidationType}' added.";
                await LoadValidationsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Add failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Updates an existing validation (audit/versioning).</summary>
        public async Task UpdateValidationAsync()
        {
            if (SelectedValidation is null) { StatusMessage = "No validation selected."; return; }
            IsBusy = true;
            try
            {
                string signatureHash = ComputeSignature(SelectedValidation, _currentSessionId, _currentDeviceInfo);

                await DatabaseServiceValidationsExtensions.UpdateValidationAsync(_dbService, SelectedValidation, signatureHash, _currentIpAddress, _currentDeviceInfo, _currentSessionId).ConfigureAwait(false);
                await _dbService.LogValidationAuditAsync(SelectedValidation, "UPDATE", _currentIpAddress, _currentDeviceInfo, _currentSessionId, signatureHash).ConfigureAwait(false);

                StatusMessage = $"Validation '{SelectedValidation.ValidationType}' updated.";
                await LoadValidationsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Update failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Deletes a validation, with audit/forensic info.</summary>
        public async Task DeleteValidationAsync()
        {
            if (SelectedValidation is null) { StatusMessage = "No validation selected."; return; }
            IsBusy = true;
            try
            {
                await DatabaseServiceValidationsExtensions.DeleteValidationAsync(_dbService, SelectedValidation.Id).ConfigureAwait(false);
                await _dbService.LogValidationAuditAsync(SelectedValidation, "DELETE", _currentIpAddress, _currentDeviceInfo, _currentSessionId, null).ConfigureAwait(false);

                StatusMessage = $"Validation '{SelectedValidation.ValidationType}' deleted.";
                await LoadValidationsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Delete failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Rolls back the selected validation using audit/versioning.</summary>
        public async Task RollbackValidationAsync()
        {
            if (SelectedValidation is null) { StatusMessage = "No validation selected."; return; }
            IsBusy = true;
            try
            {
                await DatabaseServiceValidationsExtensions.RollbackValidationAsync(_dbService, SelectedValidation.Id, _currentIpAddress, _currentDeviceInfo, _currentSessionId).ConfigureAwait(false);
                StatusMessage = $"Rollback completed for validation '{SelectedValidation.ValidationType}'.";
                await LoadValidationsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Rollback failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Exports filtered validations to file, with audit.</summary>
        public async Task ExportValidationsAsync()
        {
            IsBusy = true;
            try
            {
                await DatabaseServiceValidationsExtensions.ExportValidationsAsync(_dbService, FilteredValidations?.ToList() ?? new List<Validation>(), _currentIpAddress, _currentDeviceInfo, _currentSessionId).ConfigureAwait(false);
                await _dbService.LogValidationAuditAsync(null, "EXPORT", _currentIpAddress, _currentDeviceInfo, _currentSessionId, null).ConfigureAwait(false);
                StatusMessage = "Validations exported successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Real-time filter for validations (by type, status, search).</summary>
        public void FilterValidations()
        {
            var filtered = Validations.Where(v =>
                (string.IsNullOrWhiteSpace(SearchTerm) ||
                    (v.TargetName?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (v.ValidationType?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (v.ProtocolNumber?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (string.IsNullOrWhiteSpace(StatusFilter) || string.Equals(v.Status, StatusFilter, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(TypeFilter) || string.Equals(v.ValidationType, TypeFilter, StringComparison.OrdinalIgnoreCase))
            );
            FilteredValidations = new ObservableCollection<Validation>(filtered);
        }

        /// <summary>Can the current user edit validations? (admin/superadmin)</summary>
        public bool CanEditValidations =>
            _authService.CurrentUser?.Role == "admin" || _authService.CurrentUser?.Role == "superadmin";

        #endregion

        #region Audit/Auxiliary

        /// <summary>Loads audit history for a specific validation and maps to model type.</summary>
        public async Task<ObservableCollection<AuditLogEntry>> LoadValidationAuditAsync(int validationId)
        {
            var raw = await _dbService.GetAuditLogForEntityAsync("validations", validationId).ConfigureAwait(false);
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
                TableName   = Cast<string>(Get("Table", "TableName", "EntityTable")) ?? "validations",
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

        #region INotifyPropertyChanged

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Safely raises <see cref="PropertyChanged"/> for data binding.</summary>
        private void OnPropertyChanged([CallerMemberName] string? propName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName ?? string.Empty));

        #endregion
    }
}
