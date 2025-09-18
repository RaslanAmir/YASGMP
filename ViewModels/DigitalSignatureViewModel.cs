using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input; // AsyncRelayCommand / RelayCommand
using YasGMP.Helpers;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// Robust ViewModel for digital/electronic signatures.
    /// Compliant with EU GMP Annex 11 &amp; 21 CFR Part 11.
    /// </summary>
    public sealed class DigitalSignatureViewModel : INotifyPropertyChanged
    {
        #region Fields

        private readonly DatabaseService _dbService;
        private readonly AuthService _authService;

        private ObservableCollection<DigitalSignature> _signatures = new();
        private DigitalSignature? _selectedSignature;

        private string? _searchTerm;
        private bool _isBusy;
        private string? _statusMessage;

        private readonly string _currentSessionId;
        private readonly string _currentDeviceInfo;
        private readonly string _currentIpAddress;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalSignatureViewModel"/> class.
        /// </summary>
        /// <param name="dbService">Data access service.</param>
        /// <param name="authService">Authentication/session context service.</param>
        public DigitalSignatureViewModel(DatabaseService dbService, AuthService authService)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            _currentSessionId = _authService.CurrentSessionId;
            _currentDeviceInfo = _authService.CurrentDeviceInfo;
            _currentIpAddress = _authService.CurrentIpAddress;

            LoadSignaturesCommand   = new AsyncRelayCommand(LoadSignaturesAsync);
            AddSignatureCommand     = new AsyncRelayCommand(AddSignatureAsync,     () => !IsBusy);
            RevokeSignatureCommand  = new AsyncRelayCommand(RevokeSignatureAsync,  () => !IsBusy && SelectedSignature != null);
            VerifySignatureCommand  = new AsyncRelayCommand(VerifySignatureAsync,  () => !IsBusy && SelectedSignature != null);
            ExportSignaturesCommand = new AsyncRelayCommand(ExportSignaturesAsync, () => !IsBusy);
            FilterChangedCommand    = new RelayCommand(FilterSignatures);

            _ = LoadSignaturesAsync();
        }

        #endregion

        #region Bindable Properties

        /// <summary>All signature rows.</summary>
        public ObservableCollection<DigitalSignature> Signatures
        {
            get => _signatures;
            private set { _signatures = value; OnPropertyChanged(); }
        }

        /// <summary>Selected signature (nullable).</summary>
        public DigitalSignature? SelectedSignature
        {
            get => _selectedSignature;
            set { _selectedSignature = value; OnPropertyChanged(); }
        }

        /// <summary>Search term used for filtering.</summary>
        public string? SearchTerm
        {
            get => _searchTerm;
            set { _searchTerm = value; OnPropertyChanged(); FilterSignatures(); }
        }

        /// <summary>Indicates long-running operation in progress.</summary>
        public bool IsBusy
        {
            get => _isBusy;
            private set { _isBusy = value; OnPropertyChanged(); }
        }

        /// <summary>UI status text.</summary>
        public string? StatusMessage
        {
            get => _statusMessage;
            private set { _statusMessage = value; OnPropertyChanged(); }
        }

        #endregion

        #region Commands

        /// <summary>Loads signatures.</summary>
        public ICommand LoadSignaturesCommand { get; }
        /// <summary>Adds a new signature.</summary>
        public ICommand AddSignatureCommand { get; }
        /// <summary>Revokes the selected signature.</summary>
        public ICommand RevokeSignatureCommand { get; }
        /// <summary>Verifies the selected signature.</summary>
        public ICommand VerifySignatureCommand { get; }
        /// <summary>Exports the filtered list.</summary>
        public ICommand ExportSignaturesCommand { get; }
        /// <summary>Reapplies client-side filtering.</summary>
        public ICommand FilterChangedCommand { get; }

        #endregion

        #region Data Loading

        /// <summary>
        /// Loads all signatures using the canonical DB APIs from Region 17.
        /// </summary>
        public async Task LoadSignaturesAsync()
        {
            IsBusy = true;
            try
            {
                var signatures = await _dbService.GetAllSignaturesFullAsync().ConfigureAwait(false);
                Signatures = new ObservableCollection<DigitalSignature>(signatures);
                FilterSignatures();
                StatusMessage = $"Loaded {Signatures.Count} signature(s).";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading signatures: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        #endregion

        #region Actions

        /// <summary>
        /// Adds a new digital signature (schema-tolerant, aligned with DatabaseService Region 17).
        /// Region 17 already performs system-event logging internally.
        /// </summary>
        public async Task AddSignatureAsync()
        {
            if (_authService.CurrentUser == null)
            {
                StatusMessage = "No authenticated user.";
                return;
            }

            IsBusy = true;
            try
            {
                string session = _currentSessionId ?? string.Empty;
                string device  = _currentDeviceInfo ?? string.Empty;
                var timestamp = DateTime.UtcNow;
                var signatureContext = DigitalSignatureHelper.ComputeUserContextSignature(
                    _authService.CurrentUser.Id,
                    session,
                    device,
                    timestamp);

                var sig = new DigitalSignature
                {
                    TableName     = "generic", // adapt as needed from UI context
                    RecordId      = 0,
                    UserId        = _authService.CurrentUser.Id,
                    SignatureHash = signatureContext.Hash,
                    Method        = "pin",
                    Status        = "valid",
                    IpAddress     = _currentIpAddress,
                    DeviceInfo    = _currentDeviceInfo,
                    SessionId     = _currentSessionId,
                    Note          = "User signed from UI",
                    SignedAt      = timestamp
                    // UserName not set here (your User model does not expose UserName)
                };

                // Canonical insert method (internally logs to system_event_log):
                _ = await _dbService.InsertDigitalSignatureAsync(sig).ConfigureAwait(false);

                StatusMessage = "Signature added.";
                await LoadSignaturesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Add failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Revokes the selected signature (Region 17 logs internally).
        /// </summary>
        public async Task RevokeSignatureAsync()
        {
            if (SelectedSignature == null)
            {
                StatusMessage = "No signature selected.";
                return;
            }

            IsBusy = true;
            try
            {
                await _dbService.RevokeSignatureAsync(SelectedSignature.Id, "Revoked by user/admin")
                                .ConfigureAwait(false);

                StatusMessage = "Signature revoked.";
                await LoadSignaturesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Revoke failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Verifies the selected signature using the canonical <c>VerifySignatureAsync(int)</c>.
        /// </summary>
        public async Task VerifySignatureAsync()
        {
            if (SelectedSignature == null)
            {
                StatusMessage = "No signature selected.";
                return;
            }

            IsBusy = true;
            try
            {
                var ok = await _dbService.VerifySignatureAsync(SelectedSignature.Id).ConfigureAwait(false);
                StatusMessage = ok ? "Signature is valid." : "Signature is invalid or revoked.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Verify failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Exports the current list of signatures. Region 17 handles audit logging internally.
        /// </summary>
        public async Task ExportSignaturesAsync()
        {
            IsBusy = true;
            try
            {
                await _dbService.ExportSignaturesAsync(
                    rows: Signatures.ToList(),
                    format: "csv",
                    actorUserId: _authService.CurrentUser?.Id ?? 1,
                    ip: _currentIpAddress,
                    deviceInfo: _currentDeviceInfo,
                    sessionId: _currentSessionId
                ).ConfigureAwait(false);

                StatusMessage = "Signatures exported.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        #endregion

        #region Filtering

        /// <summary>
        /// Client-side filter that uses properties guaranteed by the model + convenience not-mapped props.
        /// (No dependency on a <c>SessionId</c> property to match your current model.)
        /// </summary>
        public void FilterSignatures()
        {
            var q = _signatures.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim();
                q = q.Where(s =>
                    (s.UserName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (s.Method?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (s.Status?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (s.PublicKey?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (s.DeviceInfo?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (s.IpAddress?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (s.SignatureHash?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                );
            }

            Signatures = new ObservableCollection<DigitalSignature>(q);
        }

        #endregion

        #region INotifyPropertyChanged

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion
    }
}
