using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;                 // ← needed for CancellationToken
using System.Threading.Tasks;
using System.Windows.Input;
using YasGMP.Models;
using YasGMP.Services;
using System.Linq;
using Microsoft.Maui.Controls;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// ViewModel for managing CAPA workflows (GMP/Annex 11/21 CFR Part 11 compliant).
    /// Handles load, create, update, approve, escalate, close, export, and advanced filtering.
    /// </summary>
    public class CAPAWorkflowViewModel : INotifyPropertyChanged
    {
        #region === Fields & Constructor ===

        private readonly DatabaseService _dbService;
        private readonly AuthService _authService;

        private ObservableCollection<CAPA> _capas = new();
        private ObservableCollection<CAPA> _filteredCapas = new();
        private CAPA? _selectedCAPA;
        private string? _searchTerm;
        private string? _statusFilter;
        private string? _typeFilter;
        private bool _isBusy;
        private string? _statusMessage;

        // Session context (nullable in storage; coalesce at use sites)
        private readonly string? _currentSessionId;
        private readonly string? _currentDeviceInfo;
        private readonly string? _currentIpAddress;

        /// <summary>
        /// Initializes a new instance of the <see cref="CAPAWorkflowViewModel"/> class.
        /// </summary>
        public CAPAWorkflowViewModel(DatabaseService dbService, AuthService authService)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            _currentSessionId = _authService.CurrentSessionId;
            _currentDeviceInfo = _authService.CurrentDeviceInfo;
            _currentIpAddress = _authService.CurrentIpAddress;

            LoadCAPAsCommand = new Command(async () => await LoadCAPAsAsync(), () => !IsBusy);
            InitiateCAPACommand = new Command(async () => await InitiateCAPAAsync(), () => !IsBusy);
            ApproveCAPACommand = new Command(async () => await ApproveCAPAAsync(), () => !IsBusy && SelectedCAPA != null && SelectedCAPA.Status == "pending_approval");
            AssignCAPACommand = new Command(async () => await AssignCAPAAsync(), () => !IsBusy && SelectedCAPA != null);
            UpdateCAPACommand = new Command(async () => await UpdateCAPAAsync(), () => !IsBusy && SelectedCAPA != null);
            EscalateCAPACommand = new Command(async () => await EscalateCAPAAsync(), () => !IsBusy && SelectedCAPA != null);
            CloseCAPACommand = new Command(async () => await CloseCAPAAsync(), () => !IsBusy && SelectedCAPA != null && SelectedCAPA.Status == "effectiveness_check");
            ExportCAPAsCommand = new Command(async () => await ExportCAPAsAsync(), () => !IsBusy);
            FilterChangedCommand = new Command(FilterCAPAs);

            Task.Run(LoadCAPAsAsync);
        }

        #endregion

        #region === Properties ===
        /// <summary>
        /// Represents the cap as value.
        /// </summary>

        public ObservableCollection<CAPA> CAPAs
        {
            get => _capas;
            set { _capas = value ?? new(); OnPropertyChanged(); }
        }
        /// <summary>
        /// Represents the filtered cap as value.
        /// </summary>

        public ObservableCollection<CAPA> FilteredCAPAs
        {
            get => _filteredCapas;
            set { _filteredCapas = value ?? new(); OnPropertyChanged(); }
        }
        /// <summary>
        /// Represents the selected capa value.
        /// </summary>

        public CAPA? SelectedCAPA
        {
            get => _selectedCAPA;
            set { _selectedCAPA = value; OnPropertyChanged(); }
        }
        /// <summary>
        /// Represents the search term value.
        /// </summary>

        public string? SearchTerm
        {
            get => _searchTerm;
            set { _searchTerm = value; OnPropertyChanged(); FilterCAPAs(); }
        }
        /// <summary>
        /// Represents the status filter value.
        /// </summary>

        public string? StatusFilter
        {
            get => _statusFilter;
            set { _statusFilter = value; OnPropertyChanged(); FilterCAPAs(); }
        }
        /// <summary>
        /// Represents the type filter value.
        /// </summary>

        public string? TypeFilter
        {
            get => _typeFilter;
            set { _typeFilter = value; OnPropertyChanged(); FilterCAPAs(); }
        }
        /// <summary>
        /// Represents the is busy value.
        /// </summary>

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }
        /// <summary>
        /// Represents the status message value.
        /// </summary>

        public string? StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }
        /// <summary>
        /// Gets or sets the available statuses.
        /// </summary>

        public string[] AvailableStatuses => new[] { "initiated", "in_progress", "pending_approval", "escalated", "effectiveness_check", "closed", "rejected" };
        /// <summary>
        /// Gets or sets the available types.
        /// </summary>
        public string[] AvailableTypes => new[] { "corrective", "preventive", "supplier", "internal", "external" };

        #endregion

        #region === Commands ===
        /// <summary>
        /// Gets or sets the load cap as command.
        /// </summary>

        public ICommand LoadCAPAsCommand { get; }
        /// <summary>
        /// Gets or sets the initiate capa command.
        /// </summary>
        public ICommand InitiateCAPACommand { get; }
        /// <summary>
        /// Gets or sets the approve capa command.
        /// </summary>
        public ICommand ApproveCAPACommand { get; }
        /// <summary>
        /// Gets or sets the assign capa command.
        /// </summary>
        public ICommand AssignCAPACommand { get; }
        /// <summary>
        /// Gets or sets the update capa command.
        /// </summary>
        public ICommand UpdateCAPACommand { get; }
        /// <summary>
        /// Gets or sets the escalate capa command.
        /// </summary>
        public ICommand EscalateCAPACommand { get; }
        /// <summary>
        /// Gets or sets the close capa command.
        /// </summary>
        public ICommand CloseCAPACommand { get; }
        /// <summary>
        /// Gets or sets the export cap as command.
        /// </summary>
        public ICommand ExportCAPAsCommand { get; }
        /// <summary>
        /// Gets or sets the filter changed command.
        /// </summary>
        public ICommand FilterChangedCommand { get; }

        #endregion

        #region === Methods ===

        /// <summary>Loads CAPAs from the database and applies filters.</summary>
        public async Task LoadCAPAsAsync()
        {
            IsBusy = true;
            try
            {
                var capaCases = await _dbService.GetAllCapaCasesAsync().ConfigureAwait(false);
                var capas = capaCases.Select(MapCapaCaseToCAPA).Where(c => c != null).Select(c => c!).ToList();
                CAPAs = new ObservableCollection<CAPA>(capas);
                FilterCAPAs();
                StatusMessage = $"Loaded {CAPAs.Count} CAPAs.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading CAPAs: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Creates a new CAPA shell and persists a mapped CAPA case.</summary>
        public async Task InitiateCAPAAsync()
        {
            IsBusy = true;
            try
            {
                var userId = _authService.CurrentUser?.Id ?? 1;

                var newCapa = new CAPA
                {
                    Title       = "New CAPA",
                    CAPAType    = TypeFilter ?? "corrective",
                    Status      = "initiated",
                    InitiatedBy = userId,
                    InitiatedAt = DateTime.UtcNow,
                    DeviceInfo  = _currentDeviceInfo ?? string.Empty,
                    SessionId   = _currentSessionId  ?? string.Empty,
                    IpAddress   = _currentIpAddress  ?? string.Empty
                };

                var capaCase = MapCAPAToCapaCase(newCapa);
                if (capaCase != null)
                {
                    await _dbService.AddCapaCaseAsync(
                        capaCase,
                        "signature_hash",
                        _currentIpAddress ?? string.Empty,
                        _currentDeviceInfo ?? string.Empty,
                        _currentSessionId ?? string.Empty,
                        userId
                    ).ConfigureAwait(false);
                }
                StatusMessage = "CAPA initiated.";
                await LoadCAPAsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Initiation failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Approves the selected CAPA.</summary>
        public async Task ApproveCAPAAsync()
        {
            if (SelectedCAPA == null) { StatusMessage = "No CAPA selected."; return; }
            IsBusy = true;
            try
            {
                await _dbService.ApproveCapaCaseAsync(
                    SelectedCAPA.Id,
                    _authService.CurrentUser?.Id ?? 1,
                    _currentIpAddress ?? string.Empty,
                    _currentDeviceInfo ?? string.Empty,
                    "signature_hash"
                ).ConfigureAwait(false);
                StatusMessage = $"CAPA '{SelectedCAPA.Title}' approved.";
                await LoadCAPAsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Approval failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Assigns or updates the selected CAPA.</summary>
        public async Task AssignCAPAAsync()
        {
            if (SelectedCAPA == null) { StatusMessage = "No CAPA selected."; return; }
            IsBusy = true;
            try
            {
                var cc = MapCAPAToCapaCase(SelectedCAPA);
                if (cc != null)
                {
                    await _dbService.UpdateCapaCaseAsync(
                        cc,
                        "signature_hash",
                        _currentIpAddress ?? string.Empty,
                        _currentDeviceInfo ?? string.Empty,
                        _currentSessionId ?? string.Empty,
                        _authService.CurrentUser?.Id ?? 1
                    ).ConfigureAwait(false);
                }
                StatusMessage = $"CAPA '{SelectedCAPA.Title}' assigned/updated.";
                await LoadCAPAsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Assignment failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Updates the selected CAPA.</summary>
        public async Task UpdateCAPAAsync()
        {
            if (SelectedCAPA == null) { StatusMessage = "No CAPA selected."; return; }
            IsBusy = true;
            try
            {
                var cc = MapCAPAToCapaCase(SelectedCAPA);
                if (cc != null)
                {
                    await _dbService.UpdateCapaCaseAsync(
                        cc,
                        "signature_hash",
                        _currentIpAddress ?? string.Empty,
                        _currentDeviceInfo ?? string.Empty,
                        _currentSessionId ?? string.Empty,
                        _authService.CurrentUser?.Id ?? 1
                    ).ConfigureAwait(false);
                }
                StatusMessage = $"CAPA '{SelectedCAPA.Title}' updated.";
                await LoadCAPAsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Update failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Escalates the selected CAPA.</summary>
        public async Task EscalateCAPAAsync()
        {
            if (SelectedCAPA == null) { StatusMessage = "No CAPA selected."; return; }
            IsBusy = true;
            try
            {
                await _dbService.EscalateCapaCaseAsync(
                    SelectedCAPA.Id,
                    _authService.CurrentUser?.Id ?? 1,
                    "Escalation triggered",
                    _currentIpAddress ?? string.Empty,
                    _currentDeviceInfo ?? string.Empty
                ).ConfigureAwait(false);
                StatusMessage = $"CAPA '{SelectedCAPA.Title}' escalated.";
                await LoadCAPAsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Escalation failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Closes the selected CAPA.</summary>
        public async Task CloseCAPAAsync()
        {
            if (SelectedCAPA == null) { StatusMessage = "No CAPA selected."; return; }
            IsBusy = true;
            try
            {
                SelectedCAPA.Status = "closed";
                var cc = MapCAPAToCapaCase(SelectedCAPA);
                if (cc != null)
                {
                    await _dbService.UpdateCapaCaseAsync(
                        cc,
                        "signature_hash",
                        _currentIpAddress ?? string.Empty,
                        _currentDeviceInfo ?? string.Empty,
                        _currentSessionId ?? string.Empty,
                        _authService.CurrentUser?.Id ?? 1
                    ).ConfigureAwait(false);
                }
                StatusMessage = $"CAPA '{SelectedCAPA.Title}' closed.";
                await LoadCAPAsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Closure failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Exports the filtered CAPAs.</summary>
        public async Task ExportCAPAsAsync()
        {
            IsBusy = true;
            try
            {
                var capaCases = FilteredCAPAs.Select(MapCAPAToCapaCase).Where(cc => cc != null).Select(cc => cc!).ToList();
                await _dbService.ExportCapaCasesAsync(capaCases, "csv").ConfigureAwait(false);
                StatusMessage = "CAPAs exported successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Applies UI filters.</summary>
        public void FilterCAPAs()
        {
            var filtered = CAPAs.Where(c =>
                (string.IsNullOrWhiteSpace(SearchTerm) ||
                    (c.Title?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.CAPAType?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.Status?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.RootCause?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (string.IsNullOrWhiteSpace(StatusFilter) || c.Status == StatusFilter) &&
                (string.IsNullOrWhiteSpace(TypeFilter) || c.CAPAType == TypeFilter)
            );
            FilteredCAPAs = new ObservableCollection<CAPA>(filtered);
        }

        /// <summary>True if the current user can manage CAPAs.</summary>
        public bool CanManageCAPA =>
            (_authService.CurrentUser?.Role == "admin" ||
             _authService.CurrentUser?.Role == "superadmin" ||
             _authService.CurrentUser?.Role == "qa");

        #endregion

        #region === Mapping Helpers ===

        /// <summary>Maps a <see cref="CapaCase"/> to a view-model level <see cref="CAPA"/>.</summary>
        public static CAPA? MapCapaCaseToCAPA(CapaCase cc)
        {
            if (cc == null) return null;

            double? riskScore = cc.RiskScore;
            if (!riskScore.HasValue && cc.RiskScore is not null && double.TryParse(cc.RiskScore.ToString(), out var parsedScore))
                riskScore = parsedScore;

            return new CAPA
            {
                Id = cc.Id,
                CAPAType = cc.CAPAType,
                Title = cc.Title,
                Status = cc.Status,
                RootCause = cc.RootCause ?? cc.Reason,
                Actions = cc.Actions,
                RiskScore = riskScore,
                InitiatedBy = cc.InitiatedBy,
                InitiatedAt = cc.InitiatedAt ?? cc.OpenedAt,
                ClosedAt = cc.ClosedAt,
                DeviceInfo = cc.DeviceInfo,
                SessionId = cc.SessionId,
                IpAddress = cc.IpAddress ?? cc.SourceIp,
                DigitalSignature = cc.DigitalSignature,
                Attachments = cc.Attachments ?? new List<string>(),
                WorkflowHistory = cc.WorkflowHistory ?? string.Empty
            };
        }

        /// <summary>Maps a view-model level <see cref="CAPA"/> back to <see cref="CapaCase"/>.</summary>
        public static CapaCase? MapCAPAToCapaCase(CAPA capa)
        {
            if (capa == null) return null;

            return new CapaCase
            {
                Id = capa.Id,
                CAPAType = capa.CAPAType,
                Title = capa.Title,
                Status = capa.Status,
                RootCause = capa.RootCause,
                Reason = capa.RootCause,
                Actions = capa.Actions,
                RiskScore = capa.RiskScore,
                InitiatedBy = capa.InitiatedBy,
                InitiatedAt = capa.InitiatedAt,
                OpenedAt = capa.InitiatedAt ?? DateTime.UtcNow,
                ClosedAt = capa.ClosedAt,
                DeviceInfo = capa.DeviceInfo,
                SessionId = capa.SessionId,
                SourceIp = capa.IpAddress,
                DigitalSignature = capa.DigitalSignature,
                Attachments = capa.Attachments ?? new List<string>(),
                WorkflowHistory = capa.WorkflowHistory ?? string.Empty
            };
        }

        #endregion

        #region === INotifyPropertyChanged ===

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Raises property change notification.</summary>
        private void OnPropertyChanged([CallerMemberName] string? propName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName ?? string.Empty));

        #endregion
    }
}
