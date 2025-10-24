using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using YasGMP.Models;
using YasGMP.Models.Enums;
using System.Collections.Generic;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// ViewModel for robust, GMP/CSV/21 CFR Part 11-compliant editing/creation of Work Orders.
    /// Handles validation, rollback, digital signature, audit, forensics, attachments, escalation, and extensibility.
    /// </summary>
    public sealed class WorkOrderEditDialogViewModel : INotifyPropertyChanged
    {
        #region === Core Properties ===

        /// <summary>Deep-copied WorkOrder instance for safe, auditable editing.</summary>
        public WorkOrder WorkOrder { get; set; }

        /// <summary>Supported work order types (bind to picker).</summary>
        public ObservableCollection<string> TypeOptions { get; } = new()
        {
            "preventivni", "korektivni", "vanredni", "inspekcija", "validacija", "kalibracija"
        };

        /// <summary>Supported priorities (bind to picker).</summary>
        public ObservableCollection<string> PriorityOptions { get; } = new()
        {
            "normalno", "visoko", "kritican"
        };

        /// <summary>Supported statuses (bind to picker).</summary>
        public ObservableCollection<string> StatusOptions { get; } = new()
        {
            "otvoren", "u tijeku", "zavrsen", "odbijen"
        };

        /// <summary>All machines/assets (for selection).</summary>
        public ObservableCollection<Machine> Machines { get; set; } = new();

        /// <summary>All users (for assignment).</summary>
        public ObservableCollection<User> Users { get; set; } = new();

        /// <summary>All components for the selected machine (optional selection).</summary>
        public ObservableCollection<MachineComponent> Components { get; set; } = new();

        /// <summary>Selected component (optional).</summary>
        public MachineComponent? SelectedComponent
        {
            get => Components?.FirstOrDefault(c => c.Id == (WorkOrder.ComponentId ?? 0));
            set { WorkOrder.ComponentId = value?.Id; OnPropertyChanged(); }
        }

        /// <summary>All CAPA cases (optional linkage).</summary>
        public ObservableCollection<CapaCase> CapaCases { get; set; } = new();

        /// <summary>All incidents (optional linkage).</summary>
        public ObservableCollection<Incident> Incidents { get; set; } = new();

        // Bound editable fields (null-safe)
        public string Type
        {
            get => WorkOrder.Type ?? string.Empty;
            set { WorkOrder.Type = value ?? string.Empty; OnPropertyChanged(); }
        }

        public string Priority
        {
            get => WorkOrder.Priority ?? string.Empty;
            set { WorkOrder.Priority = value ?? string.Empty; OnPropertyChanged(); }
        }

        public string Status
        {
            get => WorkOrder.Status ?? string.Empty;
            set { WorkOrder.Status = value ?? string.Empty; OnPropertyChanged(); }
        }

        public Machine? SelectedMachine
        {
            get => Machines?.FirstOrDefault(m => m.Id == WorkOrder.MachineId);
            set { WorkOrder.MachineId = value?.Id ?? 0; OnPropertyChanged(); }
        }

        public User? SelectedUser
        {
            get => Users?.FirstOrDefault(u => u.Id == WorkOrder.AssignedToId);
            set { WorkOrder.AssignedToId = value?.Id ?? 0; OnPropertyChanged(); }
        }

        public CapaCase? SelectedCapaCase
        {
            get => CapaCases?.FirstOrDefault(c => c.Id == WorkOrder.CapaCaseId);
            set { WorkOrder.CapaCaseId = value?.Id ?? 0; OnPropertyChanged(); }
        }

        public Incident? SelectedIncident
        {
            get => Incidents?.FirstOrDefault(i => i.Id == WorkOrder.IncidentId);
            set { WorkOrder.IncidentId = value?.Id ?? 0; OnPropertyChanged(); }
        }

        public string TaskDescription
        {
            get => WorkOrder.TaskDescription ?? string.Empty;
            set { WorkOrder.TaskDescription = value ?? string.Empty; OnPropertyChanged(); }
        }

        public DateTime DateOpen
        {
            get => WorkOrder.DateOpen;
            set { WorkOrder.DateOpen = value; OnPropertyChanged(); }
        }

        public DateTime? DateClose
        {
            get => WorkOrder.DateClose;
            set { WorkOrder.DateClose = value; OnPropertyChanged(); }
        }

        public string Result
        {
            get => WorkOrder.Result ?? string.Empty;
            set { WorkOrder.Result = value ?? string.Empty; OnPropertyChanged(); }
        }

        /// <summary>Audit note — required by GMP for every edit.</summary>
        private string _auditNote = string.Empty;
        public string AuditNote
        {
            get => _auditNote;
            set { _auditNote = value ?? string.Empty; OnPropertyChanged(); }
        }

        /// <summary>Forensics / digital signature display (readonly, auto-filled).</summary>
        public string? DigitalSignature => WorkOrder.DigitalSignature;
        public string? DeviceInfo => WorkOrder.DeviceInfo;
        public string? IpAddress => WorkOrder.SourceIp;

        /// <summary>Dialog title, context-sensitive.</summary>
        public string DialogTitle => WorkOrder.Id == 0 ? "Novi Radni Nalog" : $"Uredi Radni Nalog #{WorkOrder.Id}";

        #endregion

        #region === Advanced Collections ===

        public ObservableCollection<Photo> Photos { get; set; } = new();
        public ObservableCollection<WorkOrderPart> UsedParts { get; set; } = new();
        public ObservableCollection<WorkOrderComment> Comments { get; set; } = new();
        public ObservableCollection<WorkOrderStatusLog> StatusTimeline { get; set; } = new();
        public ObservableCollection<WorkOrderSignature> Signatures { get; set; } = new();
        public ObservableCollection<WorkOrderAudit> AuditTrail { get; set; } = new();

        #endregion

        #region === State ===

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }

        private string? _statusMessage = null;
        public string? StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }

        public bool HasError => !string.IsNullOrWhiteSpace(StatusMessage);

        #endregion

        #region === Commands ===

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        #endregion

        #region === DialogResult Event ===

        /// <summary>Event triggered when dialog completes; true/false for success/cancel, returns the (deep-copied) WorkOrder or null if canceled.</summary>
        public event Action<bool, WorkOrder?>? DialogResult;

        #endregion

        #region === Constructors ===

        /// <summary>
        /// Parameterless constructor required by XAML. Initializes a new WorkOrder with defaults.
        /// </summary>
        public WorkOrderEditDialogViewModel() : this((WorkOrder?)null)
        {
        }

        /// <summary>
        /// Initializes the dialog ViewModel: deep-copies the order, sets up lookups, collections, state and commands.
        /// </summary>
        public WorkOrderEditDialogViewModel(WorkOrder? workOrder)
        {
            // Deep copy for safe editing (audit/rollback proof)
            WorkOrder = workOrder != null
                ? workOrder.DeepCopy()
                : new WorkOrder { DateOpen = DateTime.Now, Status = "otvoren" };

            // Populate bindable collections
            Photos         = new ObservableCollection<Photo>(WorkOrder.Photos?.ToList() ?? new System.Collections.Generic.List<Photo>());
            UsedParts      = new ObservableCollection<WorkOrderPart>(WorkOrder.UsedParts?.ToList() ?? new System.Collections.Generic.List<WorkOrderPart>());
            Comments       = new ObservableCollection<WorkOrderComment>(WorkOrder.Comments?.ToList() ?? new System.Collections.Generic.List<WorkOrderComment>());
            StatusTimeline = new ObservableCollection<WorkOrderStatusLog>(WorkOrder.StatusTimeline?.ToList() ?? new System.Collections.Generic.List<WorkOrderStatusLog>());
            Signatures     = new ObservableCollection<WorkOrderSignature>(WorkOrder.Signatures?.ToList() ?? new System.Collections.Generic.List<WorkOrderSignature>());
            AuditTrail     = new ObservableCollection<WorkOrderAudit>(WorkOrder.AuditTrail?.ToList() ?? new System.Collections.Generic.List<WorkOrderAudit>());

            SaveCommand   = new Command(OnSave, CanSave);
            CancelCommand = new Command(OnCancel);
        }

        #endregion

        #region === Validation & Save/Cancel Logic ===

        /// <summary>Returns true if all mandatory fields are valid (GMP/CSV/21CFR compliance).</summary>
        private bool CanSave()
        {
            StatusMessage = null;
            if (string.IsNullOrWhiteSpace(Type)) { StatusMessage = "Tip naloga je obavezan."; return false; }
            if (SelectedMachine == null) { StatusMessage = "Stroj/oprema je obavezna."; return false; }
            if (string.IsNullOrWhiteSpace(TaskDescription)) { StatusMessage = "Opis zadatka je obavezan."; return false; }
            if (SelectedUser == null) { StatusMessage = "Dodijeljeni korisnik je obavezan."; return false; }
            if (string.IsNullOrWhiteSpace(Status)) { StatusMessage = "Status je obavezan."; return false; }
            if (Status == "zavrsen" && (Signatures == null || Signatures.Count == 0))
            {
                StatusMessage = "Za zatvaranje naloga potreban je digitalni potpis.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(AuditNote))
            {
                StatusMessage = "Razlog izmjene (audit note) je obavezan (GMP zahtjev).";
                return false;
            }
            return true;
        }

        /// <summary>Save handler: validates, logs audit, and returns result if valid.</summary>
        private void OnSave()
        {
            if (!CanSave()) return;

            AuditTrail.Add(new WorkOrderAudit
            {
                WorkOrderId = WorkOrder.Id,
                UserId      = GetEffectiveUserId(),
                Action      = WorkOrderActionType.Update,
                OldValue    = string.Empty,                 // non-null to satisfy model nullability
                NewValue    = WorkOrder.ToString() ?? string.Empty,
                ChangedAt   = DateTime.Now,
                Note        = AuditNote
            });

            DialogResult?.Invoke(true, WorkOrder);
        }

        /// <summary>Cancel handler: logs audit and cancels dialog.</summary>
        private void OnCancel()
        {
            AuditTrail.Add(new WorkOrderAudit
            {
                WorkOrderId = WorkOrder.Id,
                UserId      = GetEffectiveUserId(),
                Action      = WorkOrderActionType.Custom,
                OldValue    = WorkOrder.ToString() ?? string.Empty,
                NewValue    = string.Empty,                 // non-null to satisfy model nullability
                ChangedAt   = DateTime.Now,
                Note        = "User canceled edit"
            });

            DialogResult?.Invoke(false, null);
        }

        /// <summary>
        /// Returns a non-null user id for audit rows.
        /// Works whether the model exposes int or int? properties.
        /// </summary>
        private int GetEffectiveUserId()
        {
            // If properties are nullable: pattern matches only when HasValue
            // If properties are non-nullable int: pattern always matches and binds the value
            if (WorkOrder.LastModifiedById is int lm && lm > 0) return lm;
            if (WorkOrder.CreatedById     is int cr && cr > 0) return cr;
            return 0;
        }

        #endregion

        #region === INotifyPropertyChanged ===

        public event PropertyChangedEventHandler? PropertyChanged;

        // NOTE: sealed type → keep private to avoid “protected member on sealed type” diagnostics.
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion
    }
}
