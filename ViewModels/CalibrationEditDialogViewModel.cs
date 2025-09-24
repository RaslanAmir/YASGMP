using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using YasGMP.Common;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// View-model for adding or editing calibrations. Performs validation and populates audit metadata.
    /// </summary>
    public sealed class CalibrationEditDialogViewModel : INotifyPropertyChanged
    {
        private readonly IDialogService _dialogService;
        private readonly IUserSession _userSession;
        private readonly IPlatformService _platformService;

        private MachineComponent? _selectedComponent;
        private Supplier? _selectedSupplier;

        public Calibration Calibration { get; }
        public List<MachineComponent> Components { get; }
        public List<Supplier> Suppliers { get; }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action<bool, Calibration?>? DialogResult;

        public CalibrationEditDialogViewModel()
            : this(
                  new Calibration(),
                  new List<MachineComponent>(),
                  new List<Supplier>(),
                  ServiceLocator.GetRequiredService<IUserSession>(),
                  ServiceLocator.GetRequiredService<IDialogService>(),
                  ServiceLocator.GetRequiredService<IPlatformService>())
        {
        }

        public CalibrationEditDialogViewModel(
            Calibration calibration,
            List<MachineComponent> components,
            List<Supplier> suppliers)
            : this(
                  calibration,
                  components,
                  suppliers,
                  ServiceLocator.GetRequiredService<IUserSession>(),
                  ServiceLocator.GetRequiredService<IDialogService>(),
                  ServiceLocator.GetRequiredService<IPlatformService>())
        {
        }

        public CalibrationEditDialogViewModel(
            Calibration calibration,
            List<MachineComponent> components,
            List<Supplier> suppliers,
            IUserSession userSession,
            IDialogService dialogService,
            IPlatformService platformService)
        {
            Calibration = calibration ?? throw new ArgumentNullException(nameof(calibration));
            Components = components ?? new List<MachineComponent>();
            Suppliers = suppliers ?? new List<Supplier>();
            _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _platformService = platformService ?? throw new ArgumentNullException(nameof(platformService));

            _selectedComponent = Components.FirstOrDefault(c => c.Id == Calibration.ComponentId);
            _selectedSupplier = Suppliers.FirstOrDefault(s => s.Id == Calibration.SupplierId);

            if (string.IsNullOrWhiteSpace(Calibration.DigitalSignature))
                Calibration.DigitalSignature = ResolveSignature();

            SaveCommand = new AsyncDelegateCommand(OnSaveAsync);
            CancelCommand = new DelegateCommand(OnCancel);
        }

        public MachineComponent? SelectedComponent
        {
            get => _selectedComponent;
            set
            {
                if (!ReferenceEquals(_selectedComponent, value))
                {
                    _selectedComponent = value;
                    Calibration.ComponentId = value?.Id ?? 0;
                    OnPropertyChanged();
                }
            }
        }

        public Supplier? SelectedSupplier
        {
            get => _selectedSupplier;
            set
            {
                if (!ReferenceEquals(_selectedSupplier, value))
                {
                    _selectedSupplier = value;
                    Calibration.SupplierId = value?.Id ?? 0;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private async Task OnSaveAsync()
        {
            Calibration.CertDoc = (Calibration.CertDoc ?? string.Empty).Trim();
            Calibration.Result = (Calibration.Result ?? string.Empty).Trim();
            Calibration.Comment = (Calibration.Comment ?? string.Empty).Trim();

            if (Calibration.ComponentId <= 0)
            {
                await _dialogService.ShowAlertAsync("Greška", "Odaberite komponentu.", "U redu").ConfigureAwait(false);
                return;
            }

            if (Calibration.SupplierId <= 0)
            {
                await _dialogService.ShowAlertAsync("Greška", "Odaberite servisera ili laboratorij.", "U redu").ConfigureAwait(false);
                return;
            }

            if (Calibration.CalibrationDate == default)
            {
                await _dialogService.ShowAlertAsync("Greška", "Unesite datum kalibracije.", "U redu").ConfigureAwait(false);
                return;
            }

            if (Calibration.NextDue <= Calibration.CalibrationDate)
            {
                var proceed = await _dialogService
                    .ShowConfirmationAsync("Upozorenje", "Rok sljedeće kalibracije je isti ili prije datuma kalibracije. Želite li nastaviti?", "Da", "Ne")
                    .ConfigureAwait(false);
                if (!proceed) return;
            }

            if (Calibration.CertDoc.Length > 128)
            {
                await _dialogService.ShowAlertAsync("Greška", "Broj certifikata je predugačak (max 128 znakova).", "U redu").ConfigureAwait(false);
                return;
            }

            Calibration.LastModified = DateTime.Now;
            Calibration.LastModifiedById = _userSession.UserId ?? 0;
            Calibration.SourceIp = _platformService.GetLocalIpAddress();
            Calibration.DigitalSignature = ResolveSignature();

            DialogResult?.Invoke(true, Calibration);
        }

        private void OnCancel() => DialogResult?.Invoke(false, null);

        private string ResolveSignature() => _userSession.FullName ?? _userSession.Username ?? "Nepoznat korisnik";

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
    }
}
