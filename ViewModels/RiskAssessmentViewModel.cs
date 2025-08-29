using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Models;
using YasGMP.Models.DTO; // audits
using YasGMP.Services;
using YasGMP.Helpers;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// <b>RiskAssessmentViewModel</b> – MVVM ViewModel for ICH Q9/GMP-compliant risk assessments.
    /// <para>
    /// • Async CRUD with full audit trail<br/>
    /// • RPN (Severity × Probability × Detection) auto-scoring<br/>
    /// • Approval / closure workflow commands with traceable forensics (IP, device, session)<br/>
    /// • Advanced filtering and export
    /// </para>
    /// </summary>
    public class RiskAssessmentViewModel : INotifyPropertyChanged
    {
        #region === Fields & Ctor =============================================================

        private readonly DatabaseService _dbService;
        private readonly AuthService _authService;

        private ObservableCollection<RiskAssessment> _riskAssessments = new();
        private ObservableCollection<RiskAssessment> _filteredRiskAssessments = new();

        // Make selection and filters nullable or initialize to avoid CS8618.
        private RiskAssessment? _selectedRiskAssessment;
        private string? _searchTerm;
        private string? _statusFilter;
        private string? _categoryFilter;

        private bool _isBusy;

        // Initialize to empty string to avoid non-nullable ctor exit warnings (CS8618).
        private string _statusMessage = string.Empty;

        // Normalize session/device/IP to non-null strings for DB calls.
        private readonly string _currentSessionId;
        private readonly string _currentDeviceInfo;
        private readonly string _currentIpAddress;

        /// <summary>
        /// Initializes a new instance of <see cref="RiskAssessmentViewModel"/>.
        /// </summary>
        /// <param name="dbService">Data access/audit service.</param>
        /// <param name="authService">Authentication/session context provider.</param>
        /// <exception cref="ArgumentNullException">Thrown if any dependency is null.</exception>
        public RiskAssessmentViewModel(DatabaseService dbService, AuthService authService)
        {
            _dbService   = dbService   ?? throw new ArgumentNullException(nameof(dbService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            _currentSessionId = _authService.CurrentSessionId  ?? string.Empty;
            _currentDeviceInfo = _authService.CurrentDeviceInfo ?? string.Empty;
            _currentIpAddress  = _authService.CurrentIpAddress  ?? string.Empty;

            LoadRiskAssessmentsCommand    = new AsyncRelayCommand(LoadRiskAssessmentsAsync);
            InitiateRiskAssessmentCommand = new AsyncRelayCommand(InitiateRiskAssessmentAsync, () => !IsBusy);
            UpdateRiskAssessmentCommand   = new AsyncRelayCommand(UpdateRiskAssessmentAsync,   () => !IsBusy && SelectedRiskAssessment != null);
            ApproveRiskAssessmentCommand  = new AsyncRelayCommand(ApproveRiskAssessmentAsync,  () => !IsBusy && SelectedRiskAssessment != null && SelectedRiskAssessment.Status == "pending_approval");
            CloseRiskAssessmentCommand    = new AsyncRelayCommand(CloseRiskAssessmentAsync,    () => !IsBusy && SelectedRiskAssessment != null && SelectedRiskAssessment.Status == "effectiveness_check");
            ExportRiskAssessmentsCommand  = new AsyncRelayCommand(ExportRiskAssessmentsAsync,  () => !IsBusy);
            FilterChangedCommand          = new RelayCommand(FilterRiskAssessments);

            // Initial load (fire & forget)
            _ = LoadRiskAssessmentsAsync();
        }

        #endregion

        #region === Properties ================================================================

        /// <summary>All risk assessments loaded from the data source.</summary>
        public ObservableCollection<RiskAssessment> RiskAssessments
        {
            get => _riskAssessments;
            set { _riskAssessments = value; OnPropertyChanged(); }
        }

        /// <summary>Filtered view of <see cref="RiskAssessments"/> for UI binding.</summary>
        public ObservableCollection<RiskAssessment> FilteredRiskAssessments
        {
            get => _filteredRiskAssessments;
            set { _filteredRiskAssessments = value; OnPropertyChanged(); }
        }

        /// <summary>Currently selected risk assessment item.</summary>
        public RiskAssessment? SelectedRiskAssessment
        {
            get => _selectedRiskAssessment;
            set { _selectedRiskAssessment = value; OnPropertyChanged(); }
        }

        /// <summary>Free-text search filter applied to Title, Category, Status and ActionPlan.</summary>
        public string? SearchTerm
        {
            get => _searchTerm;
            set { _searchTerm = value; OnPropertyChanged(); FilterRiskAssessments(); }
        }

        /// <summary>Status filter (e.g., initiated/in_progress/pending_approval/...)</summary>
        public string? StatusFilter
        {
            get => _statusFilter;
            set { _statusFilter = value; OnPropertyChanged(); FilterRiskAssessments(); }
        }

        /// <summary>Category filter (e.g., process/equipment/IT/...)</summary>
        public string? CategoryFilter
        {
            get => _categoryFilter;
            set { _categoryFilter = value; OnPropertyChanged(); FilterRiskAssessments(); }
        }

        /// <summary>Indicates long-running operations.</summary>
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

        /// <summary>Available status values for filter controls.</summary>
        public string[] AvailableStatuses => new[] { "initiated","in_progress","pending_approval","effectiveness_check","closed","rejected" };

        /// <summary>Available category values for filter controls.</summary>
        public string[] AvailableCategories => new[] { "process","equipment","supplier","IT","product","validation","other" };

        /// <summary>Whether the current user can manage risk assessments.</summary>
        public bool CanManageRiskAssessment => _authService.CurrentUser?.Role is "admin" or "superadmin" or "qa";

        #endregion

        #region === Commands ==================================================================

        /// <summary>Loads the collection of risk assessments.</summary>
        public ICommand LoadRiskAssessmentsCommand { get; }

        /// <summary>Initiates a new risk assessment with defaults.</summary>
        public ICommand InitiateRiskAssessmentCommand { get; }

        /// <summary>Updates the selected risk assessment.</summary>
        public ICommand UpdateRiskAssessmentCommand { get; }

        /// <summary>Approves the selected risk assessment (moves to effectiveness check).</summary>
        public ICommand ApproveRiskAssessmentCommand { get; }

        /// <summary>Closes the selected risk assessment (after effectiveness check).</summary>
        public ICommand CloseRiskAssessmentCommand { get; }

        /// <summary>Exports the currently filtered assessments.</summary>
        public ICommand ExportRiskAssessmentsCommand { get; }

        /// <summary>Forces a manual re-filtering.</summary>
        public ICommand FilterChangedCommand { get; }

        #endregion

        #region === Methods ===================================================================

        /// <summary>
        /// Loads all risk assessments using a full, schema-tolerant query.
        /// </summary>
        public async Task LoadRiskAssessmentsAsync()
        {
            IsBusy = true;
            try
            {
                var riskAssessments = await _dbService.GetAllRiskAssessmentsFullAsync();
                RiskAssessments = new ObservableCollection<RiskAssessment>(riskAssessments ?? Enumerable.Empty<RiskAssessment>());
                FilterRiskAssessments();
                StatusMessage = $"Loaded {RiskAssessments.Count} risk assessments.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading risk assessments: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Initiates a new risk assessment with sane defaults and logs an audit entry.
        /// </summary>
        public async Task InitiateRiskAssessmentAsync()
        {
            IsBusy = true;
            try
            {
                var newRisk = new RiskAssessment
                {
                    Title       = "New Risk Assessment",
                    Category    = CategoryFilter ?? "process",
                    Status      = "initiated",
                    AssessedBy  = _authService.CurrentUser?.UserName ?? "system",
                    AssessedAt  = DateTime.UtcNow,
                    Severity    = 1,
                    Probability = 1,
                    Detection   = 5,
                    RiskScore   = 1 * 1 * 5,
                    ActionPlan  = string.Empty,
                    Attachments = new(),
                    WorkflowHistory = new(),
                    DeviceInfo  = _currentDeviceInfo,
                    SessionId   = _currentSessionId,
                    IpAddress   = _currentIpAddress
                };

                await _dbService.InitiateRiskAssessmentAsync(newRisk);
                await _dbService.LogRiskAssessmentAuditAsync(newRisk, "INITIATE", _currentIpAddress, _currentDeviceInfo, _currentSessionId, null);

                StatusMessage = "Risk assessment initiated.";
                await LoadRiskAssessmentsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Initiation failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Updates the selected risk assessment and writes an audit entry.
        /// </summary>
        public async Task UpdateRiskAssessmentAsync()
        {
            if (SelectedRiskAssessment == null) { StatusMessage = "No risk assessment selected."; return; }

            IsBusy = true;
            try
            {
                // Compute RPN
                SelectedRiskAssessment.RiskScore =
                    SelectedRiskAssessment.Severity * SelectedRiskAssessment.Probability * SelectedRiskAssessment.Detection;

                int actorUserId = _authService.CurrentUser?.Id ?? 0;

                await _dbService.UpdateRiskAssessmentAsync(
                    SelectedRiskAssessment,
                    actorUserId,
                    _currentIpAddress,
                    _currentDeviceInfo,
                    _currentSessionId);

                await _dbService.LogRiskAssessmentAuditAsync(
                    SelectedRiskAssessment,
                    "UPDATE",
                    _currentIpAddress,
                    _currentDeviceInfo,
                    _currentSessionId,
                    null);

                StatusMessage = $"Risk assessment '{SelectedRiskAssessment.Title}' updated.";
                await LoadRiskAssessmentsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Update failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Approves the selected assessment (moves status to effectiveness check).
        /// </summary>
        public async Task ApproveRiskAssessmentAsync()
        {
            if (SelectedRiskAssessment == null) { StatusMessage = "No risk assessment selected."; return; }

            IsBusy = true;
            try
            {
                int actorUserId = _authService.CurrentUser?.Id ?? 0;

                await _dbService.ApproveRiskAssessmentAsync(
                    SelectedRiskAssessment.Id,
                    actorUserId,
                    _currentIpAddress,
                    _currentDeviceInfo,
                    _currentSessionId);

                await _dbService.LogRiskAssessmentAuditAsync(
                    SelectedRiskAssessment,
                    "APPROVE",
                    _currentIpAddress,
                    _currentDeviceInfo,
                    _currentSessionId,
                    null);

                StatusMessage = $"Risk assessment '{SelectedRiskAssessment.Title}' approved.";
                await LoadRiskAssessmentsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Approval failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Closes the selected assessment after effectiveness check.
        /// </summary>
        public async Task CloseRiskAssessmentAsync()
        {
            if (SelectedRiskAssessment == null) { StatusMessage = "No risk assessment selected."; return; }

            IsBusy = true;
            try
            {
                int actorUserId = _authService.CurrentUser?.Id ?? 0;

                await _dbService.CloseRiskAssessmentAsync(
                    SelectedRiskAssessment.Id,
                    actorUserId,
                    _currentIpAddress,
                    _currentDeviceInfo,
                    _currentSessionId,
                    "Closed after effectiveness check");

                await _dbService.LogRiskAssessmentAuditAsync(
                    SelectedRiskAssessment,
                    "CLOSE",
                    _currentIpAddress,
                    _currentDeviceInfo,
                    _currentSessionId,
                    null);

                StatusMessage = $"Risk assessment '{SelectedRiskAssessment.Title}' closed.";
                await LoadRiskAssessmentsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Closure failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Exports the filtered list and records an export audit event.
        /// </summary>
        public async Task ExportRiskAssessmentsAsync()
        {
            IsBusy = true;
            try
            {
                await _dbService.ExportRiskAssessmentsAsync(
                    FilteredRiskAssessments.ToList(),
                    _currentIpAddress,
                    _currentDeviceInfo,
                    _currentSessionId);

                await _dbService.LogRiskAssessmentAuditAsync(
                    null,
                    "EXPORT",
                    _currentIpAddress,
                    _currentDeviceInfo,
                    _currentSessionId,
                    null);

                StatusMessage = "Risk assessments exported successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Applies <see cref="SearchTerm"/>, <see cref="StatusFilter"/>, and <see cref="CategoryFilter"/> to the list.
        /// </summary>
        public void FilterRiskAssessments()
        {
            var filtered = RiskAssessments.Where(r =>
                (string.IsNullOrWhiteSpace(SearchTerm) ||
                    (r.Title?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (r.Category?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (r.Status?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (r.ActionPlan?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (string.IsNullOrWhiteSpace(StatusFilter) || r.Status == StatusFilter) &&
                (string.IsNullOrWhiteSpace(CategoryFilter) || r.Category == CategoryFilter));

            FilteredRiskAssessments = new ObservableCollection<RiskAssessment>(filtered);
        }

        /// <summary>
        /// Loads audit history entries for a specific risk assessment.
        /// </summary>
        /// <param name="riskAssessmentId">Primary key of the risk assessment.</param>
        /// <returns>Observable collection of audits for UI binding.</returns>
        public async Task<ObservableCollection<AuditEntryDto>> LoadRiskAssessmentAuditAsync(int riskAssessmentId)
        {
            var audits = await _dbService.GetAuditLogForEntityAsync("risk_assessments", riskAssessmentId);
            return new ObservableCollection<AuditEntryDto>(audits ?? Enumerable.Empty<AuditEntryDto>());
        }

        #endregion

        #region === INotifyPropertyChanged ====================================================

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raises <see cref="PropertyChanged"/> for the specified property.
        /// </summary>
        /// <param name="propName">Property name (optional; inferred by compiler if omitted).</param>
        protected void OnPropertyChanged([CallerMemberName] string? propName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName ?? string.Empty));

        #endregion
    }
}
