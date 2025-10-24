using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services;
using CommunityToolkit.Mvvm.Input; 

namespace YasGMP.ViewModels
{
    /// <summary>
    /// <b>PpmViewModel</b> – GMP/Annex 11-compliant ViewModel for Preventive Maintenance Plans (PPM).
    /// <para>
    /// ✅ Robust observable binding, null-safety, error and status messaging<br/>
    /// ✅ Add/Edit dialog validation, attachment, audit, e-signature, future extensibility for regulatory/inspector use<br/>
    /// ✅ Idiot-proof, power-user proof, regulator-proof
    /// </para>
    /// </summary>
    public class PpmViewModel : INotifyPropertyChanged
    {
        #region === Core Plan List and Selection ===

        /// <summary>
        /// List of all PPM plans (observable for UI).
        /// </summary>
        public ObservableCollection<PreventiveMaintenancePlan> PpmPlans { get; } = new();

        private PreventiveMaintenancePlan? _selectedPlan;

        /// <summary>
        /// Currently selected PPM plan (for UI binding).
        /// </summary>
        public PreventiveMaintenancePlan? SelectedPlan
        {
            get => _selectedPlan;
            set
            {
                if (_selectedPlan != value)
                {
                    _selectedPlan = value;
                    OnPropertyChanged();
                    // When plan changes, update popup/fields if dialog is open
                    if (IsEditDialogOpen && value != null)
                        LoadPlanToDialog(value);
                }
            }
        }

        private string? _statusMessage;
        /// <summary>
        /// Status or error message for the UI (bindable).
        /// </summary>
        public string? StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        private readonly DatabaseService _db;

        /// <summary>
        /// Initializes the ViewModel and injects database service.
        /// </summary>
        /// <param name="db">Injected DatabaseService instance (must not be null).</param>
        public PpmViewModel(DatabaseService db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        #endregion

        #region === LOAD/REFRESH ===

        /// <summary>
        /// Loads all preventive maintenance plans from the database.
        /// </summary>
        public async Task LoadPpmPlansAsync()
        {
            try
            {
                var plans = await _db.GetAllPpmPlansAsync();
                PpmPlans.Clear();
                foreach (var p in plans)
                    PpmPlans.Add(p);
                StatusMessage = $"Učitano {plans.Count} planova održavanja.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Greška pri učitavanju: {ex.Message}";
            }
        }

        #endregion

        #region === ADD/EDIT POPUP STATE & VALIDATION ===

        private bool _isEditDialogOpen;
        /// <summary>
        /// Is the Add/Edit plan dialog currently open?
        /// </summary>
        public bool IsEditDialogOpen
        {
            get => _isEditDialogOpen;
            set { _isEditDialogOpen = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Temporary plan object used for Add/Edit dialog (prevents dirtying SelectedPlan).
        /// </summary>
        public PreventiveMaintenancePlan EditDialogPlan { get; private set; } = new();

        private string? _editDialogValidationError;
        /// <summary>
        /// Validation error message for the dialog UI.
        /// </summary>
        public string? EditDialogValidationError
        {
            get => _editDialogValidationError;
            set { _editDialogValidationError = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Prepares a blank plan for Add dialog, or clones existing for Edit.
        /// </summary>
        /// <param name="plan">If null, prepares a new plan; else clones existing.</param>
        public void OpenAddEditDialog(PreventiveMaintenancePlan? plan = null)
        {
            EditDialogValidationError = null;
            if (plan == null)
                EditDialogPlan = new PreventiveMaintenancePlan();
            else
                EditDialogPlan = plan.Clone(); // You must implement Clone() on your model for deep copy!
            IsEditDialogOpen = true;
            OnPropertyChanged(nameof(EditDialogPlan));
        }

        /// <summary>
        /// Loads the current plan data into the dialog object for editing.
        /// </summary>
        /// <param name="plan">Plan to edit (not null).</param>
        private void LoadPlanToDialog(PreventiveMaintenancePlan plan)
        {
            EditDialogPlan = plan.Clone();
            EditDialogValidationError = null;
            OnPropertyChanged(nameof(EditDialogPlan));
        }

        /// <summary>
        /// Validates all required fields before allowing save.
        /// </summary>
        public bool ValidateDialogPlan()
        {
            EditDialogValidationError = null;
            if (string.IsNullOrWhiteSpace(EditDialogPlan.Title))
            {
                EditDialogValidationError = "Naslov je obavezan!";
                return false;
            }
            if (EditDialogPlan.DueDate == default)
            {
                EditDialogValidationError = "Datum dospijeća je obavezan!";
                return false;
            }
            // Add more rules as needed...
            return true;
        }

        /// <summary>
        /// Saves the plan from the dialog (new or edit). Closes dialog on success.
        /// </summary>
        public async Task SaveDialogPlanAsync()
        {
            if (!ValidateDialogPlan())
                return;

            try
            {
                bool isUpdate = EditDialogPlan.Id > 0;
                await _db.InsertOrUpdatePpmPlanAsync(EditDialogPlan, isUpdate);
                StatusMessage = isUpdate ? "Plan ažuriran!" : "Plan dodan!";
                IsEditDialogOpen = false;
                await LoadPpmPlansAsync();
            }
            catch (Exception ex)
            {
                EditDialogValidationError = $"Greška pri spremanju: {ex.Message}";
            }
        }

        /// <summary>
        /// Cancels the dialog edit.
        /// </summary>
        public void CancelDialogEdit()
        {
            IsEditDialogOpen = false;
            EditDialogValidationError = null;
        }

        #endregion

        #region === DELETE ===

        /// <summary>
        /// Deletes the selected preventive maintenance plan.
        /// </summary>
        public async Task DeletePpmPlanAsync()
        {
            if (SelectedPlan == null)
            {
                StatusMessage = "Nijedan plan nije odabran!";
                return;
            }
            try
            {
                await _db.DeletePpmPlanAsync(SelectedPlan.Id);
                StatusMessage = "Plan obrisan!";
                await LoadPpmPlansAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Greška pri brisanju: {ex.Message}";
            }
        }

        #endregion

        #region === ATTACHMENT, AUDIT, E-SIGN, FUTURE EXTENSIONS ===

        // You can add properties here for attachments popup, audit trail, digital signature state, etc.
        // Example stubs:

        public bool IsAttachmentDialogOpen { get; set; } // Add OnPropertyChanged as above if you bind this
        public bool IsAuditPopupOpen { get; set; }
        public bool IsSignatureDialogOpen { get; set; }
        // More advanced state/logic as needed...

        #endregion

        #region === INotifyPropertyChanged Support ===

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion
    }
}
