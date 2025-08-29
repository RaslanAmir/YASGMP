using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// <b>CalibrationEditDialogViewModel</b> – ViewModel for adding/editing calibrations
    /// with rich validation, audit metadata, and dialog completion callbacks.
    /// Designed for GMP/Annex 11/21 CFR Part 11 compliant flows.
    /// </summary>
    public class CalibrationEditDialogViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CalibrationEditDialogViewModel"/>.
        /// </summary>
        /// <param name="calibration">The calibration to edit/add (required).</param>
        /// <param name="components">All available components (nullable allowed; replaced by empty list).</param>
        /// <param name="suppliers">All available suppliers (nullable allowed; replaced by empty list).</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="calibration"/> is null.</exception>
        public CalibrationEditDialogViewModel(
            Calibration calibration,
            List<MachineComponent>? components,
            List<Supplier>? suppliers)
        {
            Calibration = calibration ?? throw new ArgumentNullException(nameof(calibration));
            Components  = components ?? new List<MachineComponent>();
            Suppliers   = suppliers  ?? new List<Supplier>();

            // Preselect, tolerating "no match" (null).
            _selectedComponent = Components.FirstOrDefault(c => c.Id == Calibration.ComponentId);
            _selectedSupplier  = Suppliers.FirstOrDefault(s => s.Id == Calibration.SupplierId);

            // Ensure we always have a signature string.
            Calibration.DigitalSignature = string.IsNullOrWhiteSpace(Calibration.DigitalSignature)
                ? (((App?)Application.Current)?.LoggedUser?.FullName ?? "Nepoznat korisnik")
                : Calibration.DigitalSignature;

            SaveCommand   = new Command(OnSave);
            CancelCommand = new Command(OnCancel);
        }

        #region === Bindable Model & Lookups ===

        /// <summary>
        /// Gets or sets the <see cref="Calibration"/> entity being edited.
        /// Never null after construction.
        /// </summary>
        public Calibration Calibration { get; set; }

        /// <summary>
        /// Gets the list of all available machine components for the picker binding.
        /// </summary>
        public List<MachineComponent> Components { get; }

        /// <summary>
        /// Gets the list of all available suppliers for the picker binding.
        /// </summary>
        public List<Supplier> Suppliers { get; }

        private MachineComponent? _selectedComponent;
        /// <summary>
        /// Gets or sets the currently selected machine component in the dialog.
        /// Nullable to allow the UI to represent "no selection".
        /// Updates <see cref="Calibration.ComponentId"/> when set.
        /// </summary>
        public MachineComponent? SelectedComponent
        {
            get => _selectedComponent;
            set
            {
                if (!ReferenceEquals(_selectedComponent, value))
                {
                    _selectedComponent = value;
                    Calibration.ComponentId = value?.Id ?? 0; // keep 0-as-unset pattern used elsewhere
                    OnPropertyChanged();
                }
            }
        }

        private Supplier? _selectedSupplier;
        /// <summary>
        /// Gets or sets the currently selected supplier in the dialog.
        /// Nullable to allow the UI to represent "no selection".
        /// Updates <see cref="Calibration.SupplierId"/> when set.
        /// </summary>
        public Supplier? SelectedSupplier
        {
            get => _selectedSupplier;
            set
            {
                if (!ReferenceEquals(_selectedSupplier, value))
                {
                    _selectedSupplier = value;
                    Calibration.SupplierId = value?.Id ?? 0; // keep 0-as-unset pattern used elsewhere
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region === Commands & Dialog Result ===

        /// <summary>Gets the command that validates and saves the calibration.</summary>
        public ICommand SaveCommand { get; }

        /// <summary>Gets the command that cancels the dialog without saving.</summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// Raised when the dialog completes.
        /// <list type="bullet">
        /// <item><description><c>bool</c> — <c>true</c> if saved, <c>false</c> if cancelled.</description></item>
        /// <item><description><see cref="Calibration"/> — the saved entity; <c>null</c> when cancelled.</description></item>
        /// </list>
        /// </summary>
        public event Action<bool, Calibration?>? DialogResult;

        #endregion

        #region === Save / Cancel ===

        /// <summary>
        /// Performs full validation, sets audit metadata, and completes the dialog on save.
        /// All UI dialogs are invoked through <see cref="SafeNavigator"/> to guarantee UI-thread execution,
        /// preventing WinUI <c>COMException 0x8001010E</c>.
        /// </summary>
        private async void OnSave()
        {
            // Sanitize string fields (trim, limit length where appropriate).
            Calibration.CertDoc = (Calibration.CertDoc ?? string.Empty).Trim();
            Calibration.Result  = (Calibration.Result  ?? string.Empty).Trim();
            Calibration.Comment = (Calibration.Comment ?? string.Empty).Trim();

            // --- Validation ---

            if (Calibration.ComponentId <= 0)
            {
                await SafeNavigator.ShowAlertAsync("Greška", "Odaberite komponentu.", "U redu");
                return;
            }

            if (Calibration.SupplierId <= 0)
            {
                await SafeNavigator.ShowAlertAsync("Greška", "Odaberite servisera ili laboratorij.", "U redu");
                return;
            }

            if (Calibration.CalibrationDate == default)
            {
                await SafeNavigator.ShowAlertAsync("Greška", "Unesite datum kalibracije.", "U redu");
                return;
            }

            if (Calibration.NextDue <= Calibration.CalibrationDate)
            {
                var proceed = await SafeNavigator.ConfirmAsync(
                    "Upozorenje",
                    "Rok sljedeće kalibracije je isti ili prije datuma kalibracije. Želite li nastaviti?",
                    "Da", "Ne");
                if (!proceed) return;
            }

            if (Calibration.CertDoc.Length > 128)
            {
                await SafeNavigator.ShowAlertAsync("Greška", "Broj certifikata je predugačak (max 128 znakova).", "U redu");
                return;
            }

            // --- Audit / metadata ---
            Calibration.LastModified     = DateTime.Now;
            Calibration.LastModifiedById = (((App?)Application.Current)?.LoggedUser?.Id) ?? 0;
            Calibration.SourceIp         = DependencyService.Get<IPlatformService>()?.GetLocalIpAddress() ?? string.Empty;
            Calibration.DigitalSignature = (((App?)Application.Current)?.LoggedUser?.FullName) ?? "Nepoznat korisnik";

            // Complete dialog (success).
            DialogResult?.Invoke(true, Calibration);
        }

        /// <summary>
        /// Cancels the dialog and notifies the parent.
        /// </summary>
        private void OnCancel() => DialogResult?.Invoke(false, null);

        #endregion

        #region === INotifyPropertyChanged ===

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raises <see cref="PropertyChanged"/> for data binding updates.
        /// </summary>
        /// <param name="name">The name of the changed property (optional due to <see cref="CallerMemberNameAttribute"/>).</param>
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        #endregion
    }
}
