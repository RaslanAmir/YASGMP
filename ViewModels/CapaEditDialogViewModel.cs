using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using YasGMP.Common;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// View-model backing the CAPA edit dialog. Handles validation and audit metadata.
    /// </summary>
    public sealed class CapaEditDialogViewModel : INotifyPropertyChanged
    {
        private readonly IDialogService _dialogService;
        private readonly IUserSession _userSession;

        private CapaCase _capaCase;

        public ObservableCollection<string> StatusList { get; } = new()
        {
            "otvoren", "u tijeku", "zatvoren", "poništen", "odgođen"
        };

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public bool? DialogResult { get; private set; }

        public CapaCase CapaCase
        {
            get => _capaCase;
            private set
            {
                if (!ReferenceEquals(_capaCase, value))
                {
                    _capaCase = value;
                    OnPropertyChanged();
                }
            }
        }

        public CapaEditDialogViewModel()
            : this(null,
                  ServiceLocator.GetRequiredService<IUserSession>(),
                  ServiceLocator.GetRequiredService<IDialogService>())
        {
        }

        public CapaEditDialogViewModel(CapaCase? capaCase)
            : this(capaCase,
                  ServiceLocator.GetRequiredService<IUserSession>(),
                  ServiceLocator.GetRequiredService<IDialogService>())
        {
        }

        public CapaEditDialogViewModel(CapaCase? capaCase, IUserSession userSession, IDialogService dialogService)
        {
            _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            CapaCase = capaCase != null
                ? new CapaCase
                {
                    Id = capaCase.Id,
                    Title = capaCase.Title,
                    DateOpen = capaCase.DateOpen,
                    DateClose = capaCase.DateClose,
                    Reason = capaCase.Reason,
                    Actions = capaCase.Actions,
                    Status = capaCase.Status,
                    DigitalSignature = capaCase.DigitalSignature
                }
                : new CapaCase
                {
                    DateOpen = DateTime.Today,
                    Status = "otvoren",
                    DigitalSignature = ResolveSignature()
                };

            if (string.IsNullOrWhiteSpace(CapaCase.DigitalSignature))
                CapaCase.DigitalSignature = ResolveSignature();

            SaveCommand = new AsyncDelegateCommand(OnSaveAsync);
            CancelCommand = new DelegateCommand(OnCancel);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private async Task OnSaveAsync()
        {
            CapaCase.Title = (CapaCase.Title ?? string.Empty).Trim();
            CapaCase.Reason = (CapaCase.Reason ?? string.Empty).Trim();
            CapaCase.Actions = (CapaCase.Actions ?? string.Empty).Trim();
            CapaCase.Status = (CapaCase.Status ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(CapaCase.Title))
            {
                await _dialogService.ShowAlertAsync("Greška", "Naslov je obavezan.", "OK").ConfigureAwait(false);
                return;
            }

            if (CapaCase.Title.Length > 200)
            {
                await _dialogService.ShowAlertAsync("Greška", "Naslov je predugačak (maks. 200 znakova).", "OK").ConfigureAwait(false);
                return;
            }

            if (string.IsNullOrWhiteSpace(CapaCase.Status))
            {
                await _dialogService.ShowAlertAsync("Greška", "Status je obavezan.", "OK").ConfigureAwait(false);
                return;
            }

            if (!StatusList.Contains(CapaCase.Status))
            {
                await _dialogService.ShowAlertAsync("Greška", "Nevažeća vrijednost statusa.", "OK").ConfigureAwait(false);
                return;
            }

            if (CapaCase.DateOpen == default)
            {
                await _dialogService.ShowAlertAsync("Greška", "Datum otvaranja je obavezan.", "OK").ConfigureAwait(false);
                return;
            }

            if (CapaCase.DateClose != default && CapaCase.DateClose < CapaCase.DateOpen)
            {
                await _dialogService.ShowAlertAsync("Greška", "Datum zatvaranja ne može biti prije datuma otvaranja.", "OK").ConfigureAwait(false);
                return;
            }

            if (CapaCase.Reason.Length > 2000)
            {
                await _dialogService.ShowAlertAsync("Greška", "Opis uzroka je predugačak (maks. 2000 znakova).", "OK").ConfigureAwait(false);
                return;
            }

            if (CapaCase.Actions.Length > 4000)
            {
                await _dialogService.ShowAlertAsync("Greška", "Akcije su predugačke (maks. 4000 znakova).", "OK").ConfigureAwait(false);
                return;
            }

            if (string.IsNullOrWhiteSpace(CapaCase.DigitalSignature))
                CapaCase.DigitalSignature = ResolveSignature();

            DialogResult = true;
            OnPropertyChanged(nameof(DialogResult));
        }

        private void OnCancel()
        {
            DialogResult = false;
            OnPropertyChanged(nameof(DialogResult));
        }

        private string ResolveSignature()
        {
            return _userSession.FullName ?? _userSession.Username ?? "Nepoznat korisnik";
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
    }
}
