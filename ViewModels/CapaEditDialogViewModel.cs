using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// <b>CapaEditDialogViewModel</b> — ViewModel for the CAPA Add/Edit dialog (MAUI).
    /// Manages a <see cref="CapaCase"/>, validation rules, and save/cancel commands.
    /// All UI alerts are routed through <see cref="SafeNavigator"/> to ensure UI-thread execution
    /// and avoid WinUI <c>COMException 0x8001010E</c> when invoked from background threads.
    /// </summary>
    public class CapaEditDialogViewModel : BindableObject
    {
        private CapaCase? _capaCase;

        /// <summary>
        /// Gets or sets the CAPA object currently being edited.
        /// Never <see langword="null"/> after construction.
        /// </summary>
        public CapaCase CapaCase
        {
            get => _capaCase!;
            set
            {
                if (!ReferenceEquals(_capaCase, value))
                {
                    _capaCase = value;
                    OnPropertyChanged(nameof(CapaCase));
                }
            }
        }

        /// <summary>
        /// Status list for the UI picker (localized).
        /// </summary>
        public ObservableCollection<string> StatusList { get; } = new ObservableCollection<string>
        {
            "otvoren", "u tijeku", "zatvoren", "poništen", "odgođen"
        };

        /// <summary>Command to validate and save the entry.</summary>
        public ICommand SaveCommand { get; }

        /// <summary>Command to cancel the entry.</summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// Dialog result set by the view model:
        /// <list type="bullet">
        /// <item><description><see langword="true"/> — saved successfully</description></item>
        /// <item><description><see langword="false"/> — canceled</description></item>
        /// <item><description><see langword="null"/> — pending/undecided</description></item>
        /// </list>
        /// The hosting view should observe this property and close the modal when set.
        /// </summary>
        public bool? DialogResult { get; private set; }

        /// <summary>
        /// Initializes a new instance of <see cref="CapaEditDialogViewModel"/>.
        /// </summary>
        /// <param name="capaCase">Existing case or <c>null</c> for a new entry.</param>
        public CapaEditDialogViewModel(CapaCase? capaCase)
        {
            // Clone incoming entity to avoid mutating the original instance before Save.
            CapaCase = capaCase != null
                ? new CapaCase
                {
                    Id               = capaCase.Id,
                    Title            = capaCase.Title,
                    DateOpen         = capaCase.DateOpen,
                    DateClose        = capaCase.DateClose,
                    Reason           = capaCase.Reason,
                    Actions          = capaCase.Actions,
                    Status           = capaCase.Status,
                    DigitalSignature = capaCase.DigitalSignature
                }
                : new CapaCase
                {
                    DateOpen         = DateTime.Today,
                    Status           = "otvoren",
                    DigitalSignature = (((App?)Application.Current)?.LoggedUser?.FullName) ?? "Nepoznat korisnik"
                };

            // Ensure signature is always populated for audit trails.
            if (string.IsNullOrWhiteSpace(CapaCase.DigitalSignature))
                CapaCase.DigitalSignature = (((App?)Application.Current)?.LoggedUser?.FullName) ?? "Nepoznat korisnik";

            SaveCommand   = new Command(async () => await OnSave());
            CancelCommand = new Command(OnCancel);
        }

        /// <summary>
        /// Validates input and sets the dialog result on success.
        /// Uses <see cref="SafeNavigator"/> to display messages on the UI thread.
        /// </summary>
        private async System.Threading.Tasks.Task OnSave()
        {
            // Trim/normalize text inputs
            CapaCase.Title  = (CapaCase.Title  ?? string.Empty).Trim();
            CapaCase.Reason = (CapaCase.Reason ?? string.Empty).Trim();
            CapaCase.Actions= (CapaCase.Actions?? string.Empty).Trim();
            CapaCase.Status = (CapaCase.Status ?? string.Empty).Trim();

            // === Validation ===
            if (string.IsNullOrWhiteSpace(CapaCase.Title))
            {
                await SafeNavigator.ShowAlertAsync("Greška", "Naslov je obavezan.", "OK");
                return;
            }

            if (CapaCase.Title.Length > 200)
            {
                await SafeNavigator.ShowAlertAsync("Greška", "Naslov je predugačak (maks. 200 znakova).", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(CapaCase.Status))
            {
                await SafeNavigator.ShowAlertAsync("Greška", "Status je obavezan.", "OK");
                return;
            }

            // If status list is used in UI, also enforce it here for consistency.
            if (!StatusList.Contains(CapaCase.Status))
            {
                await SafeNavigator.ShowAlertAsync("Greška", "Nevažeća vrijednost statusa.", "OK");
                return;
            }

            if (CapaCase.DateOpen == default)
            {
                await SafeNavigator.ShowAlertAsync("Greška", "Datum otvaranja je obavezan.", "OK");
                return;
            }

            if (CapaCase.DateClose != default && CapaCase.DateClose < CapaCase.DateOpen)
            {
                await SafeNavigator.ShowAlertAsync("Greška", "Datum zatvaranja ne može biti prije datuma otvaranja.", "OK");
                return;
            }

            if (CapaCase.Reason.Length > 2000)
            {
                await SafeNavigator.ShowAlertAsync("Greška", "Opis uzroka je predugačak (maks. 2000 znakova).", "OK");
                return;
            }

            if (CapaCase.Actions.Length > 4000)
            {
                await SafeNavigator.ShowAlertAsync("Greška", "Akcije su predugačke (maks. 4000 znakova).", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(CapaCase.DigitalSignature))
                CapaCase.DigitalSignature = (((App?)Application.Current)?.LoggedUser?.FullName) ?? "Nepoznat korisnik";

            // Mark as success; view should close the dialog upon observing this change.
            DialogResult = true;
            OnPropertyChanged(nameof(DialogResult));
        }

        /// <summary>
        /// Sets the dialog result to cancelled.
        /// The hosting view should close the modal when this property changes.
        /// </summary>
        private void OnCancel()
        {
            DialogResult = false;
            OnPropertyChanged(nameof(DialogResult));
        }
    }
}
