using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using YasGMP.Models;
using YasGMP.Models.DTO; // DTOs for audit log mapping
using YasGMP.Services;
using YasGMP.Helpers;
using CommunityToolkit.Mvvm.Input;
using System.Linq;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// <b>CapaCaseViewModel</b> – ViewModel for Corrective and Preventive Action (CAPA) management.
    /// Provides async CRUD, approval, escalation, export, digital signatures, audit trail mapping,
    /// advanced filtering, and status color helpers in a GMP/Annex 11/21 CFR Part 11 context.
    /// </summary>
    public partial class CapaCaseViewModel : INotifyPropertyChanged
    {
        #region === Fields & Constructor ===

        private readonly DatabaseService _dbService;
        private readonly AuthService _authService;

        private ObservableCollection<CapaCase> _capaCases = new();
        private ObservableCollection<CapaCase> _filteredCapaCases = new();
        private CapaCase? _selectedCapaCase;
        private string? _searchTerm;
        private string? _statusFilter;
        private string? _riskFilter;
        private bool _isBusy;
        private string? _statusMessage;

        // Session info (used for audit/logging) - ensure non-null by coalescing on assignment
        private readonly string _currentSessionId;
        private readonly string _currentDeviceInfo;
        private readonly string _currentIpAddress;

        /// <summary>
        /// Initializes a new instance of the <see cref="CapaCaseViewModel"/> class.
        /// </summary>
        public CapaCaseViewModel(DatabaseService dbService, AuthService authService)
        {
            _dbService    = dbService  ?? throw new ArgumentNullException(nameof(dbService));
            _authService  = authService?? throw new ArgumentNullException(nameof(authService));

            // CS8601 fix: coalesce possibly-null auth properties into safe, non-null strings.
            _currentSessionId  = _authService.CurrentSessionId  ?? string.Empty;
            _currentDeviceInfo = _authService.CurrentDeviceInfo ?? string.Empty;
            _currentIpAddress  = _authService.CurrentIpAddress  ?? string.Empty;

            LoadCapaCasesCommand     = new AsyncRelayCommand(LoadCapaCasesAsync);
            AddCapaCaseCommand       = new AsyncRelayCommand(AddCapaCaseAsync,      CanPerformCapaAction);
            UpdateCapaCaseCommand    = new AsyncRelayCommand(UpdateCapaCaseAsync,   CanPerformCapaAction);
            DeleteCapaCaseCommand    = new AsyncRelayCommand(DeleteCapaCaseAsync,   CanPerformCapaAction);
            RollbackCapaCaseCommand  = new AsyncRelayCommand(RollbackCapaCaseAsync, CanPerformCapaAction);
            ExportCapaCasesCommand   = new AsyncRelayCommand(ExportCapaCasesAsync,  () => !IsBusy && FilteredCapaCases.Any());
            EscalateCapaCaseCommand  = new AsyncRelayCommand(EscalateCapaCaseAsync, CanPerformCapaAction);
            ApproveCapaCaseCommand   = new AsyncRelayCommand(ApproveCapaCaseAsync,  CanPerformCapaAction);
            RejectCapaCaseCommand    = new AsyncRelayCommand(RejectCapaCaseAsync,   CanPerformCapaAction);
            FilterChangedCommand     = new RelayCommand(FilterCapaCases);

            _ = Task.Run(LoadCapaCasesAsync);
        }

        #endregion

        #region === Properties ===

        /// <summary>All CAPA cases loaded from the database.</summary>
        public ObservableCollection<CapaCase> CapaCases
        {
            get => _capaCases;
            set { _capaCases = value ?? new(); OnPropertyChanged(); }
        }

        /// <summary>CAPA cases after applying current filters.</summary>
        public ObservableCollection<CapaCase> FilteredCapaCases
        {
            get => _filteredCapaCases;
            set { _filteredCapaCases = value ?? new(); OnPropertyChanged(); }
        }

        /// <summary>The currently selected CAPA case in the UI (nullable).</summary>
        public CapaCase? SelectedCapaCase
        {
            get => _selectedCapaCase;
            set { _selectedCapaCase = value; OnPropertyChanged(); }
        }

        /// <summary>Free-text search term filter.</summary>
        public string? SearchTerm
        {
            get => _searchTerm;
            set { _searchTerm = value; OnPropertyChanged(); FilterCapaCases(); }
        }

        /// <summary>Status filter; e.g., open, in_review, approved, closed, escalated, rejected.</summary>
        public string? StatusFilter
        {
            get => _statusFilter;
            set { _statusFilter = value; OnPropertyChanged(); FilterCapaCases(); }
        }

        /// <summary>Risk filter; e.g., low, medium, high, critical.</summary>
        public string? RiskFilter
        {
            get => _riskFilter;
            set { _riskFilter = value; OnPropertyChanged(); FilterCapaCases(); }
        }

        /// <summary>Busy state for async operations.</summary>
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        /// <summary>Status message for UI (errors, operations, etc.).</summary>
        public string? StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        /// <summary>Allowed statuses for UI pickers.</summary>
        public string[] AvailableStatuses => new[] { "open", "in_review", "approved", "closed", "escalated", "rejected" };

        /// <summary>Allowed risk labels for UI pickers.</summary>
        public string[] AvailableRisks => new[] { "low", "medium", "high", "critical" };

        /// <summary>Maps status to a recommended HEX color for quick UI tags.</summary>
        public static string GetStatusColor(string status) =>
            status switch
            {
                "open"       => "#2196F3",
                "in_review"  => "#FFB300",
                "approved"   => "#43A047",
                "closed"     => "#616161",
                "escalated"  => "#D32F2F",
                "rejected"   => "#757575",
                _            => "#BDBDBD"
            };

        #endregion

        #region === Commands ===

        public IAsyncRelayCommand LoadCapaCasesCommand { get; }
        public IAsyncRelayCommand AddCapaCaseCommand { get; }
        public IAsyncRelayCommand UpdateCapaCaseCommand { get; }
        public IAsyncRelayCommand DeleteCapaCaseCommand { get; }
        public IAsyncRelayCommand RollbackCapaCaseCommand { get; }
        public IAsyncRelayCommand ExportCapaCasesCommand { get; }
        public IAsyncRelayCommand EscalateCapaCaseCommand { get; }
        public IAsyncRelayCommand ApproveCapaCaseCommand { get; }
        public IAsyncRelayCommand RejectCapaCaseCommand { get; }
        public ICommand            FilterChangedCommand { get; }

        #endregion

        #region === Main Methods ===

        /// <summary>Loads all CAPA cases from the database and applies current filters.</summary>
        public async Task LoadCapaCasesAsync()
        {
            IsBusy = true;
            try
            {
                var capaCases = await _dbService.GetAllCapaCasesAsync().ConfigureAwait(false);
                CapaCases = new ObservableCollection<CapaCase>(capaCases ?? new List<CapaCase>()); // CS8601-safe
                FilterCapaCases();
                StatusMessage = $"Loaded {CapaCases.Count} CAPA cases.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading CAPA cases: {ex.Message} {(ex.InnerException?.Message ?? string.Empty)}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Adds the currently selected CAPA case using digital signature metadata.</summary>
        public async Task AddCapaCaseAsync()
        {
            if (SelectedCapaCase == null)
            {
                StatusMessage = "No CAPA case selected.";
                return;
            }

            IsBusy = true;
            try
            {
                string signatureHash = DigitalSignatureHelper.GenerateSignatureHash(SelectedCapaCase, _currentSessionId, _currentDeviceInfo);
                await _dbService.AddCapaCaseAsync(
                    SelectedCapaCase,
                    signatureHash,
                    _currentIpAddress,
                    _currentDeviceInfo,
                    _currentSessionId
                ).ConfigureAwait(false);

                await _dbService.LogCapaCaseAuditAsync(
                    SelectedCapaCase.Id,
                    "CREATE",
                    _authService.CurrentUser?.Id ?? 1,
                    _currentIpAddress,
                    _currentDeviceInfo,
                    _currentSessionId,
                    $"CAPA '{SelectedCapaCase.CapaCode}' created."
                ).ConfigureAwait(false);

                StatusMessage = $"CAPA case '{SelectedCapaCase.CapaCode}' added.";
                await LoadCapaCasesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Add failed: {ex.Message} {(ex.InnerException?.Message ?? string.Empty)}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Updates the currently selected CAPA case and writes an audit entry.</summary>
        public async Task UpdateCapaCaseAsync()
        {
            if (SelectedCapaCase == null)
            {
                StatusMessage = "No CAPA case selected.";
                return;
            }

            IsBusy = true;
            try
            {
                string signatureHash = DigitalSignatureHelper.GenerateSignatureHash(SelectedCapaCase, _currentSessionId, _currentDeviceInfo);
                await _dbService.UpdateCapaCaseAsync(
                    SelectedCapaCase,
                    signatureHash,
                    _currentIpAddress,
                    _currentDeviceInfo,
                    _currentSessionId
                ).ConfigureAwait(false);

                await _dbService.LogCapaCaseAuditAsync(
                    SelectedCapaCase.Id,
                    "UPDATE",
                    _authService.CurrentUser?.Id ?? 1,
                    _currentIpAddress,
                    _currentDeviceInfo,
                    _currentSessionId,
                    $"CAPA '{SelectedCapaCase.CapaCode}' updated."
                ).ConfigureAwait(false);

                StatusMessage = $"CAPA case '{SelectedCapaCase.CapaCode}' updated.";
                await LoadCapaCasesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Update failed: {ex.Message} {(ex.InnerException?.Message ?? string.Empty)}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Deletes the currently selected CAPA case and writes an audit entry.</summary>
        public async Task DeleteCapaCaseAsync()
        {
            if (SelectedCapaCase == null)
            {
                StatusMessage = "No CAPA case selected.";
                return;
            }

            IsBusy = true;
            try
            {
                await _dbService.DeleteCapaCaseAsync(
                    SelectedCapaCase.Id,
                    _authService.CurrentUser?.Id ?? 1,
                    _currentIpAddress,
                    _currentDeviceInfo
                ).ConfigureAwait(false);

                await _dbService.LogCapaCaseAuditAsync(
                    SelectedCapaCase.Id,
                    "DELETE",
                    _authService.CurrentUser?.Id ?? 1,
                    _currentIpAddress,
                    _currentDeviceInfo,
                    _currentSessionId,
                    $"CAPA '{SelectedCapaCase.CapaCode}' deleted."
                ).ConfigureAwait(false);

                StatusMessage = $"CAPA case '{SelectedCapaCase.CapaCode}' deleted.";
                await LoadCapaCasesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Delete failed: {ex.Message} {(ex.InnerException?.Message ?? string.Empty)}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Rolls back the selected CAPA case to a previous snapshot.</summary>
        public async Task RollbackCapaCaseAsync()
        {
            if (SelectedCapaCase == null)
            {
                StatusMessage = "No CAPA case selected.";
                return;
            }

            IsBusy = true;
            try
            {
                await _dbService.RollbackCapaCaseAsync(
                    SelectedCapaCase.Id,
                    "{}",
                    _authService.CurrentUser?.Id ?? 1,
                    _currentIpAddress,
                    _currentDeviceInfo
                ).ConfigureAwait(false);

                StatusMessage = $"Rollback completed for CAPA case '{SelectedCapaCase.CapaCode}'.";
                await LoadCapaCasesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Rollback failed: {ex.Message} {(ex.InnerException?.Message ?? string.Empty)}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Exports filtered CAPA cases using a chosen format and logs the action.</summary>
        public async Task ExportCapaCasesAsync()
        {
            IsBusy = true;
            try
            {
                var fmt = await YasGMP.Helpers.ExportFormatPrompt.PromptAsync();
                await _dbService.ExportCapaCasesAsync(
                    FilteredCapaCases.ToList(),
                    fmt
                ).ConfigureAwait(false);

                await _dbService.LogCapaCaseAuditAsync(
                    0,
                    "EXPORT",
                    _authService.CurrentUser?.Id ?? 1,
                    _currentIpAddress,
                    _currentDeviceInfo,
                    _currentSessionId,
                    "CAPA cases exported."
                ).ConfigureAwait(false);

                StatusMessage = "CAPA cases exported successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message} {(ex.InnerException?.Message ?? string.Empty)}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Escalates the selected CAPA case and logs the action.</summary>
        public async Task EscalateCapaCaseAsync()
        {
            if (SelectedCapaCase == null)
            {
                StatusMessage = "No CAPA case selected.";
                return;
            }

            IsBusy = true;
            try
            {
                await _dbService.EscalateCapaCaseAsync(
                    SelectedCapaCase.Id,
                    _authService.CurrentUser?.Id ?? 1,
                    "Escalated by user",
                    _currentIpAddress,
                    _currentDeviceInfo
                ).ConfigureAwait(false);

                await _dbService.LogCapaCaseAuditAsync(
                    SelectedCapaCase.Id,
                    "ESCALATE",
                    _authService.CurrentUser?.Id ?? 1,
                    _currentIpAddress,
                    _currentDeviceInfo,
                    _currentSessionId,
                    $"CAPA '{SelectedCapaCase.CapaCode}' escalated."
                ).ConfigureAwait(false);

                StatusMessage = $"CAPA case '{SelectedCapaCase.CapaCode}' escalated.";
                await LoadCapaCasesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Escalation failed: {ex.Message} {(ex.InnerException?.Message ?? string.Empty)}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Approves the selected CAPA case with a digital signature and logs the action.</summary>
        public async Task ApproveCapaCaseAsync()
        {
            if (SelectedCapaCase == null)
            {
                StatusMessage = "No CAPA case selected.";
                return;
            }

            IsBusy = true;
            try
            {
                string signatureHash = DigitalSignatureHelper.GenerateSignatureHash(SelectedCapaCase, _currentSessionId, _currentDeviceInfo);
                await _dbService.ApproveCapaCaseAsync(
                    SelectedCapaCase.Id,
                    _authService.CurrentUser?.Id ?? 1,
                    _currentIpAddress,
                    _currentDeviceInfo,
                    signatureHash
                ).ConfigureAwait(false);

                await _dbService.LogCapaCaseAuditAsync(
                    SelectedCapaCase.Id,
                    "APPROVE",
                    _authService.CurrentUser?.Id ?? 1,
                    _currentIpAddress,
                    _currentDeviceInfo,
                    _currentSessionId,
                    $"CAPA '{SelectedCapaCase.CapaCode}' approved."
                ).ConfigureAwait(false);

                StatusMessage = $"CAPA case '{SelectedCapaCase.CapaCode}' approved.";
                await LoadCapaCasesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Approval failed: {ex.Message} {(ex.InnerException?.Message ?? string.Empty)}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Rejects the selected CAPA case with a digital signature and logs the action.</summary>
        public async Task RejectCapaCaseAsync()
        {
            if (SelectedCapaCase == null)
            {
                StatusMessage = "No CAPA case selected.";
                return;
            }

            IsBusy = true;
            try
            {
                string signatureHash = DigitalSignatureHelper.GenerateSignatureHash(SelectedCapaCase, _currentSessionId, _currentDeviceInfo);
                await _dbService.RejectCapaCaseAsync(
                    SelectedCapaCase.Id,
                    _authService.CurrentUser?.Id ?? 1,
                    _currentIpAddress,
                    _currentDeviceInfo,
                    signatureHash
                ).ConfigureAwait(false);

                await _dbService.LogCapaCaseAuditAsync(
                    SelectedCapaCase.Id,
                    "REJECT",
                    _authService.CurrentUser?.Id ?? 1,
                    _currentIpAddress,
                    _currentDeviceInfo,
                    _currentSessionId,
                    $"CAPA '{SelectedCapaCase.CapaCode}' rejected."
                ).ConfigureAwait(false);

                StatusMessage = $"CAPA case '{SelectedCapaCase.CapaCode}' rejected.";
                await LoadCapaCasesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Rejection failed: {ex.Message} {(ex.InnerException?.Message ?? string.Empty)}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Applies all filters to the CAPA case list.</summary>
        public void FilterCapaCases()
        {
            var filtered = CapaCases?.Where(c =>
                (string.IsNullOrWhiteSpace(SearchTerm) ||
                    (c.CapaCode   ?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.Title      ?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.Description?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (string.IsNullOrWhiteSpace(StatusFilter) || c.Status     == StatusFilter) &&
                (string.IsNullOrWhiteSpace(RiskFilter)   || c.RiskRating == RiskFilter)
            ) ?? Enumerable.Empty<CapaCase>();

            FilteredCapaCases = new ObservableCollection<CapaCase>(filtered);
        }

        /// <summary>Returns true if the current user can edit CAPA cases.</summary>
        public bool CanEditCapaCases =>
            string.Equals(_authService.CurrentUser?.Role, "admin", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(_authService.CurrentUser?.Role, "superadmin", StringComparison.OrdinalIgnoreCase);

        private bool CanPerformCapaAction() =>
            !IsBusy && SelectedCapaCase != null && CanEditCapaCases;

        #endregion

        #region === Audit/Auxiliary ===

        /// <summary>Maps audit DTOs to model and returns the audit trail for a CAPA case.</summary>
        public async Task<ObservableCollection<AuditLogEntry>> LoadCapaCaseAuditAsync(int capaCaseId)
        {
            var auditDtos = await _dbService.GetAuditLogForEntityAsync("capa_cases", capaCaseId).ConfigureAwait(false)
                         ?? new List<AuditEntryDto>();

            var audits = auditDtos.Select(dto => new AuditLogEntry
            {
                Id               = dto.Id ?? 0,
                EntityType       = dto.Entity ?? string.Empty,
                EntityId         = int.TryParse(dto.EntityId, out int eid) ? eid : 0,
                PerformedBy      = dto.Username ?? (dto.UserId?.ToString() ?? string.Empty),
                Action           = dto.Action ?? string.Empty,
                OldValue         = dto.OldValue ?? string.Empty,
                NewValue         = dto.NewValue ?? string.Empty,
                ChangedAt        = dto.Timestamp,
                DeviceInfo       = dto.DeviceInfo ?? string.Empty,
                IpAddress        = dto.IpAddress ?? string.Empty,
                SessionId        = dto.SessionId ?? string.Empty,
                DigitalSignature = dto.DigitalSignature ?? string.Empty,
                Note             = dto.Note ?? string.Empty
            }).ToList();

            return new ObservableCollection<AuditLogEntry>(audits);
        }

        #endregion

        #region === INotifyPropertyChanged ===

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Raises <see cref="PropertyChanged"/> for MVVM binding updates.</summary>
        private void OnPropertyChanged([CallerMemberName] string? propName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName ?? string.Empty));

        #endregion

        #region === Extensibility Hooks ===

        partial void OnCapaCaseAdded(CapaCase newCapaCase);
        partial void OnCapaCaseDeleted(int capaCaseId);
        partial void OnCapaCaseApproved(int capaCaseId);

        #endregion
    }
}
