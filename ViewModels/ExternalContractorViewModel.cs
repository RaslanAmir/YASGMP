using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using YasGMP.Models;
using YasGMP.Models.DTO;          // for AuditEntryDto
using YasGMP.Services;
using CommunityToolkit.Mvvm.Input;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// ViewModel za upravljanje vanjskim izvođačima/serviserima.
    /// <para>
    /// • Async CRUD • Pametno filtriranje • Izvoz (preko refleksije – radi i kad metoda ima opcionalne parametre)
    /// • Audit povijest (DTO) • Sigurna obavijest o promjenama (INotifyPropertyChanged).
    /// </para>
    /// <remarks>
    /// Napomene kompatibilnosti:
    /// <list type="bullet">
    ///   <item>
    ///     <description>Model <see cref="ExternalContractor"/> nema svojstvo <c>Contact</c>.
    ///     Umjesto toga koristi se <see cref="ExternalContractor.ContactPerson"/>. Time se uklanja pogreška CS1061.</description>
    ///   </item>
    ///   <item>
    ///     <description>Filtriranje po <c>Status</c>/<c>Rating</c>/<c>ServiceType</c> je tolerantno:
    ///     ako ta svojstva ne postoje u modelu, refleksija vraća <c>null</c> i filter i dalje radi.</description>
    ///   </item>
    ///   <item>
    ///     <description>Reflektirani pozivi (<c>RollbackExternalContractorAsync</c>, <c>ExportExternalContractorsAsync</c>)
    ///     podržavaju varijante potpisa s opcionalnim parametrima; kad su dostupni, prosljeđuju se
    ///     <c>userId</c>, <c>ip</c>, <c>deviceInfo</c>, <c>sessionId</c> i <see cref="CancellationToken.None"/>.</description>
    ///   </item>
    /// </list>
    /// </remarks>
    /// </summary>
    public sealed class ExternalContractorViewModel : INotifyPropertyChanged
    {
        #region Fields & Constructor

        // FIX (CS8618): Initialize with null-forgiving to satisfy NRT analysis across all construction paths.
        private readonly DatabaseService _dbService = null!;
        private readonly AuthService _authService = null!;

        private ObservableCollection<ExternalContractor> _contractors = new();
        private ObservableCollection<ExternalContractor> _filteredContractors = new();
        private ExternalContractor? _selectedContractor;
        private string? _searchTerm;
        private string? _statusFilter;
        private string? _ratingFilter;
        private bool _isBusy;
        private string _statusMessage = string.Empty;

        // Optional DB methods (resolved via reflection; may be missing in some builds)
        private readonly MethodInfo? _miRollback;
        private readonly MethodInfo? _miExport;

        // For richer audit/export context if your AuthService exposes these (gracefully null-safe)
        private readonly string? _currentSessionId;
        private readonly string? _currentDeviceInfo;
        private readonly string? _currentIpAddress;

        /// <summary>
        /// Inicijalizira novi <see cref="ExternalContractorViewModel"/>.
        /// </summary>
        /// <param name="dbService">Servis za pristup bazi.</param>
        /// <param name="authService">Servis za autentikaciju/kontekst korisnika.</param>
        /// <exception cref="ArgumentNullException">Ako je bilo koji servis <c>null</c>.</exception>
        public ExternalContractorViewModel(DatabaseService dbService, AuthService authService)
        {
            _dbService   = dbService  ?? throw new ArgumentNullException(nameof(dbService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            // cache reflection lookups
            _miRollback = _dbService.GetType().GetMethod("RollbackExternalContractorAsync", BindingFlags.Instance | BindingFlags.Public);
            _miExport   = _dbService.GetType().GetMethod("ExportExternalContractorsAsync", BindingFlags.Instance | BindingFlags.Public);

            // capture context (if provided by AuthService; otherwise null-safe)
            _currentSessionId  = _authService?.CurrentSessionId;
            _currentDeviceInfo = _authService?.CurrentDeviceInfo;
            _currentIpAddress  = _authService?.CurrentIpAddress;

            LoadContractorsCommand    = new AsyncRelayCommand(LoadContractorsAsync);
            AddContractorCommand      = new AsyncRelayCommand(AddContractorAsync,      () => !IsBusy);
            UpdateContractorCommand   = new AsyncRelayCommand(UpdateContractorAsync,   () => !IsBusy && SelectedContractor != null);
            DeleteContractorCommand   = new AsyncRelayCommand(DeleteContractorAsync,   () => !IsBusy && SelectedContractor != null);
            RollbackContractorCommand = new AsyncRelayCommand(RollbackContractorAsync, () => !IsBusy && SelectedContractor != null);
            ExportContractorsCommand  = new AsyncRelayCommand(ExportContractorsAsync,  () => !IsBusy);
            FilterChangedCommand      = new RelayCommand(FilterContractors);

            _ = LoadContractorsAsync();
        }

        #endregion

        #region Properties

        /// <summary>Svi izvođači (puna lista).</summary>
        public ObservableCollection<ExternalContractor> Contractors
        {
            get => _contractors;
            set { _contractors = value; OnPropertyChanged(); }
        }

        /// <summary>Filtrirani izvođači (vezani na UI).</summary>
        public ObservableCollection<ExternalContractor> FilteredContractors
        {
            get => _filteredContractors;
            set { _filteredContractors = value; OnPropertyChanged(); }
        }

        /// <summary>Trenutno odabrani izvođač.</summary>
        public ExternalContractor? SelectedContractor
        {
            get => _selectedContractor;
            set
            {
                _selectedContractor = value;
                OnPropertyChanged();
                RefreshCommandStates();
            }
        }

        /// <summary>Tražilica (naziv, kontakt, usluga...).</summary>
        public string? SearchTerm
        {
            get => _searchTerm;
            set { _searchTerm = value; OnPropertyChanged(); FilterContractors(); }
        }

        /// <summary>Filter statusa (npr. active/blocked/...)</summary>
        public string? StatusFilter
        {
            get => _statusFilter;
            set { _statusFilter = value; OnPropertyChanged(); FilterContractors(); }
        }

        /// <summary>Filter rejtinga (A-D).</summary>
        public string? RatingFilter
        {
            get => _ratingFilter;
            set { _ratingFilter = value; OnPropertyChanged(); FilterContractors(); }
        }

        /// <summary>Busy indikator.</summary>
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); RefreshCommandStates(); }
        }

        /// <summary>Status poruka za UI.</summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        /// <summary>Predefinirani statusi (opcionalno korišteni u filteru).</summary>
        public string[] AvailableStatuses => new[] { "active", "blocked", "expired", "pending", "suspended" };

        /// <summary>Predefinirane ocjene (opcionalno korištene u filteru).</summary>
        public string[] AvailableRatings => new[] { "A", "B", "C", "D" };

        #endregion

        #region Commands

        /// <summary>Naredba: učitaj izvođače iz baze.</summary>
        public ICommand LoadContractorsCommand { get; }

        /// <summary>Naredba: dodaj novog izvođača (koristi trenutni buffer).</summary>
        public ICommand AddContractorCommand { get; }

        /// <summary>Naredba: ažuriraj odabranog izvođača.</summary>
        public ICommand UpdateContractorCommand { get; }

        /// <summary>Naredba: obriši odabranog izvođača.</summary>
        public ICommand DeleteContractorCommand { get; }

        /// <summary>Naredba: rollback (ako je dostupno u servisu).</summary>
        public ICommand RollbackContractorCommand { get; }

        /// <summary>Naredba: izvoz filtrirane liste (ako je dostupno u servisu).</summary>
        public ICommand ExportContractorsCommand { get; }

        /// <summary>Naredba: ručno okidanje filtriranja.</summary>
        public ICommand FilterChangedCommand { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Učitava izvođače iz baze i primjenjuje filtere.
        /// </summary>
        public async Task LoadContractorsAsync()
        {
            IsBusy = true;
            try
            {
                var contractors = await _dbService.GetAllExternalContractorsAsync();
                Contractors = new ObservableCollection<ExternalContractor>(contractors ?? Enumerable.Empty<ExternalContractor>());
                FilterContractors();
                StatusMessage = $"Loaded {Contractors.Count} external contractors.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading contractors: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Dodaje novog izvođača (koristi trenutno odabran entitet kao edit buffer).
        /// </summary>
        public async Task AddContractorAsync()
        {
            if (SelectedContractor is null) { StatusMessage = "No contractor selected."; return; }
            IsBusy = true;
            try
            {
                await _dbService.AddExternalContractorAsync(SelectedContractor);
                StatusMessage = $"External contractor '{(SelectedContractor.CompanyName ?? SelectedContractor.Name ?? "N/A")}' added.";
                await LoadContractorsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Add failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Ažurira odabranog izvođača.
        /// </summary>
        public async Task UpdateContractorAsync()
        {
            if (SelectedContractor is null) { StatusMessage = "No contractor selected."; return; }
            IsBusy = true;
            try
            {
                await _dbService.UpdateExternalContractorAsync(SelectedContractor);
                StatusMessage = $"External contractor '{(SelectedContractor.CompanyName ?? SelectedContractor.Name ?? "N/A")}' updated.";
                await LoadContractorsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Update failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Briše odabranog izvođača.
        /// </summary>
        public async Task DeleteContractorAsync()
        {
            if (SelectedContractor is null) { StatusMessage = "No contractor selected."; return; }
            IsBusy = true;
            try
            {
                await _dbService.DeleteExternalContractorAsync(SelectedContractor.Id);
                StatusMessage = $"External contractor '{(SelectedContractor.CompanyName ?? SelectedContractor.Name ?? "N/A")}' deleted.";
                await LoadContractorsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Delete failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Pokreće rollback (ako metoda postoji u trenutnoj verziji servisa). Podržava potpise s opcionalnim parametrima.
        /// </summary>
        public async Task RollbackContractorAsync()
        {
            if (SelectedContractor is null) { StatusMessage = "No contractor selected."; return; }
            IsBusy = true;
            try
            {
                if (_miRollback != null)
                {
                    var pars = _miRollback.GetParameters();
                    object? taskObj;

                    if (pars.Length == 1)
                    {
                        // RollbackExternalContractorAsync(int id)
                        taskObj = _miRollback.Invoke(_dbService, new object?[] { SelectedContractor.Id });
                    }
                    else
                    {
                        // RollbackExternalContractorAsync(int id, int actorUserId, string? ip, string? device, string? sessionId, CancellationToken token)
                        int userId = _authService?.CurrentUser?.Id ?? 0;
                        taskObj = _miRollback.Invoke(_dbService, new object?[]
                        {
                            SelectedContractor.Id,
                            userId,
                            _currentIpAddress,
                            _currentDeviceInfo,
                            _currentSessionId,
                            CancellationToken.None
                        });
                    }

                    if (taskObj is Task t) await t;
                    StatusMessage = $"Rollback completed for contractor '{(SelectedContractor.CompanyName ?? SelectedContractor.Name ?? "N/A")}'.";
                }
                else
                {
                    StatusMessage = "Rollback not supported in current build.";
                }

                await LoadContractorsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Rollback failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Izvozi filtrirane izvođače (ako metoda postoji u trenutnoj verziji servisa).
        /// Podržava i kratki i prošireni potpis metode.
        /// </summary>
        public async Task ExportContractorsAsync()
        {
            IsBusy = true;
            try
            {
                if (_miExport != null)
                {
                    var pars = _miExport.GetParameters();
                    object? taskObj;

                    if (pars.Length == 1)
                    {
                        // ExportExternalContractorsAsync(IEnumerable<ExternalContractor> rows)
                        taskObj = _miExport.Invoke(_dbService, new object?[] { FilteredContractors.ToList() });
                    }
                    else
                    {
                        // ExportExternalContractorsAsync(IEnumerable<ExternalContractor> rows, string format, int actorUserId, string ip, string device, string? sessionId, CancellationToken token)
                        int userId = _authService?.CurrentUser?.Id ?? 0;
                        taskObj = _miExport.Invoke(_dbService, new object?[]
                        {
                            FilteredContractors.ToList(),
                            "csv",
                            userId,
                            _currentIpAddress ?? "system",
                            _currentDeviceInfo ?? "client",
                            _currentSessionId,
                            CancellationToken.None
                        });
                    }

                    if (taskObj is Task t) await t;
                    StatusMessage = "External contractors exported successfully.";
                }
                else
                {
                    StatusMessage = "Export is not available in this build.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Real-time filter by company/contact + optional ServiceType/Status/Rating (tolerantno čak i ako svojstva ne postoje).
        /// </summary>
        public void FilterContractors()
        {
            static string? GetProp(ExternalContractor? c, string prop)
                => c?.GetType().GetProperty(prop)?.GetValue(c)?.ToString();

            var items = Contractors ?? new ObservableCollection<ExternalContractor>();

            var filtered =
                from c in items
                let company = c?.CompanyName ?? c?.Name ?? string.Empty
                // FIX: uklonjen c?.Contact (ne postoji u modelu) -> koristi se samo ContactPerson
                let contact = c?.ContactPerson ?? string.Empty
                let service = GetProp(c, "ServiceType") ?? GetProp(c, "Type") ?? string.Empty
                let status  = GetProp(c, "Status")      ?? string.Empty
                let rating  = GetProp(c, "Rating")      ?? string.Empty
                where
                    (string.IsNullOrWhiteSpace(SearchTerm) ||
                        company.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                        contact.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                        service.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                && (string.IsNullOrWhiteSpace(StatusFilter) || status.Equals(StatusFilter, StringComparison.OrdinalIgnoreCase))
                && (string.IsNullOrWhiteSpace(RatingFilter) || rating.Equals(RatingFilter, StringComparison.OrdinalIgnoreCase))
                select c;

            FilteredContractors = new ObservableCollection<ExternalContractor>(filtered);
        }

        /// <summary>
        /// Osvježava CanExecute stanja relevantnih komandi.
        /// </summary>
        private void RefreshCommandStates()
        {
            (AddContractorCommand      as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (UpdateContractorCommand   as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (DeleteContractorCommand   as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (RollbackContractorCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (ExportContractorsCommand  as AsyncRelayCommand)?.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Dohvaća audit povijest za danog izvođača.
        /// </summary>
        /// <param name="contractorId">ID izvođača.</param>
        /// <returns>Kolekcija <see cref="AuditEntryDto"/> zapisa.</returns>
        public async Task<ObservableCollection<AuditEntryDto>> LoadContractorAuditAsync(int contractorId)
        {
            var audits = await _dbService.GetAuditLogForEntityAsync("external_contractors", contractorId);
            return new ObservableCollection<AuditEntryDto>(audits ?? new System.Collections.Generic.List<AuditEntryDto>());
        }

        #endregion

        #region INotifyPropertyChanged

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Sigurno podiže <see cref="PropertyChanged"/> događaj.
        /// </summary>
        /// <param name="propName">Naziv svojstva koje se promijenilo.</param>
        private void OnPropertyChanged([CallerMemberName] string? propName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName ?? string.Empty));

        #endregion
    }
}
