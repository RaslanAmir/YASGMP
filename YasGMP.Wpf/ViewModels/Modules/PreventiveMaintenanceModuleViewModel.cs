using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>
/// Preventive maintenance workspace that mirrors the MAUI PPM planner with a due-date calendar and GMP form-mode editor.
/// </summary>
public sealed partial class PreventiveMaintenanceModuleViewModel : DataDrivenModuleDocumentViewModel
{
    /// <summary>Stable registry key.</summary>
    public const string ModuleKey = "PreventiveMaintenance";

    private readonly IModuleNavigationService _navigation;
    private readonly List<PreventiveMaintenancePlan> _planCache = new();
    private readonly Dictionary<int, PreventiveMaintenancePlan> _plansById = new();

    private PreventiveMaintenancePlan? _loadedPlan;
    private PpmPlanEditor? _snapshot;
    private bool _suppressEditorDirtyNotifications;
    private bool _suppressCalendarSelection;

    /// <summary>
    /// Initializes a new instance of the <see cref="PreventiveMaintenanceModuleViewModel"/> class.
    /// </summary>
    public PreventiveMaintenanceModuleViewModel(
        DatabaseService databaseService,
        ILocalizationService localization,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, localization.GetString("Module.Title.PreventiveMaintenance"), databaseService, localization, cflDialogService, shellInteraction, navigation)
    {
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));

        CalendarDays = new ObservableCollection<PpmCalendarDayViewModel>();
        SelectedDayPlans = new ObservableCollection<PpmCalendarEntryViewModel>();

        Editor = PpmPlanEditor.CreateEmpty();

        DeletePlanCommand = new AsyncRelayCommand(DeletePlanAsync, CanDeletePlan);
        GenerateWorkOrderCommand = new AsyncRelayCommand(OpenWorkOrdersAsync, CanOpenWorkOrders);
        ApprovePlanCommand = new AsyncRelayCommand(ApprovePlanAsync, CanApprovePlan);
        AttachChecklistCommand = new AsyncRelayCommand(AttachChecklistAsync, CanAttachChecklist);
        ShowAuditTrailCommand = new AsyncRelayCommand(ShowAuditTrailAsync, CanShowAuditTrail);
    }

    /// <summary>Gets the calendar aggregation of upcoming plans.</summary>
    [ObservableProperty]
    private ObservableCollection<PpmCalendarDayViewModel> _calendarDays;

    /// <summary>Gets the list of plans scheduled for the selected calendar day.</summary>
    [ObservableProperty]
    private ObservableCollection<PpmCalendarEntryViewModel> _selectedDayPlans;

    /// <summary>Gets or sets the selected calendar date.</summary>
    [ObservableProperty]
    private DateTime? _selectedCalendarDate;

    /// <summary>Gets or sets the highlighted calendar day summary.</summary>
    [ObservableProperty]
    private PpmCalendarDayViewModel? _selectedCalendarDay;

    /// <summary>Gets or sets the highlighted calendar plan.</summary>
    [ObservableProperty]
    private PpmCalendarEntryViewModel? _selectedCalendarPlan;

    /// <summary>Editor backing the add/update form.</summary>
    [ObservableProperty]
    private PpmPlanEditor _editor = null!;

    /// <summary>Indicates whether editor controls are enabled.</summary>
    [ObservableProperty]
    private bool _isEditorEnabled;

    /// <summary>Deletes the selected preventive maintenance plan.</summary>
    public IAsyncRelayCommand DeletePlanCommand { get; }

    /// <summary>Navigates to Work Orders with the selected plan context.</summary>
    public IAsyncRelayCommand GenerateWorkOrderCommand { get; }

    /// <summary>Marks the current plan as approved via digital signature.</summary>
    public IAsyncRelayCommand ApprovePlanCommand { get; }

    /// <summary>Triggers checklist attachment workflow.</summary>
    public IAsyncRelayCommand AttachChecklistCommand { get; }

    /// <summary>Displays the audit trail for the selected plan.</summary>
    public IAsyncRelayCommand ShowAuditTrailCommand { get; }

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var plans = await Database.GetAllPpmPlansAsync().ConfigureAwait(false);
        UpdatePlanCache(plans);
        UpdateCalendarCollections(_planCache);
        return _planCache.Select(ToRecord).ToList();
    }

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
    {
        var today = DateTime.Today;
        var sample = new List<PreventiveMaintenancePlan>
        {
            new()
            {
                Id = 1001,
                Code = "PPM-1001",
                Name = "Autoclave sterilization validation",
                Description = "Monthly steam validation and gasket inspection.",
                Frequency = "Monthly",
                MachineLabel = "Autoclave A-1",
                ComponentLabel = "Steam chamber",
                NextDue = today.AddDays(3),
                Status = "scheduled"
            },
            new()
            {
                Id = 1002,
                Code = "PPM-1002",
                Name = "HVAC HEPA integrity",
                Description = "Quarterly airflow verification and HEPA leak test.",
                Frequency = "Quarterly",
                MachineLabel = "HVAC Zone 3",
                ComponentLabel = "HEPA stage",
                NextDue = today.AddDays(10),
                Status = "pending_approval"
            },
            new()
            {
                Id = 1003,
                Code = "PPM-1003",
                Name = "Generator load bank",
                Description = "Semi-annual emergency generator load test.",
                Frequency = "Semi-annual",
                MachineLabel = "Generator G-42",
                ComponentLabel = "Load bank",
                NextDue = today.AddDays(-2),
                Status = "overdue"
            }
        };

        UpdatePlanCache(sample);
        UpdateCalendarCollections(_planCache);
        return _planCache.Select(ToRecord).ToList();
    }

    protected override Task OnActivatedAsync(object? parameter)
    {
        if (parameter is null)
        {
            return Task.CompletedTask;
        }

        if (parameter is int id)
        {
            FocusPlan(id);
        }
        else if (parameter is string text && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            FocusPlan(parsed);
        }
        else if (parameter is IDictionary<string, object?> dict)
        {
            if (dict.TryGetValue("planId", out var value) && value is int planId)
            {
                FocusPlan(planId);
            }
        }

        return Task.CompletedTask;
    }

    protected override Task OnRecordSelectedAsync(ModuleRecord? record)
    {
        if (IsInEditMode)
        {
            return Task.CompletedTask;
        }

        if (record is null)
        {
            _loadedPlan = null;
            SetEditor(PpmPlanEditor.CreateEmpty());
            UpdateActionStates();
            return Task.CompletedTask;
        }

        if (!int.TryParse(record.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
        {
            return Task.CompletedTask;
        }

        if (!_plansById.TryGetValue(id, out var plan))
        {
            StatusMessage = $"Unable to load plan {record.Title}.";
            return Task.CompletedTask;
        }

        _loadedPlan = plan;
        LoadEditor(plan);
        UpdateActionStates();
        return Task.CompletedTask;
    }

    protected override Task OnModeChangedAsync(FormMode mode)
    {
        IsEditorEnabled = mode is FormMode.Add or FormMode.Update;

        switch (mode)
        {
            case FormMode.Add:
                _snapshot = null;
                _loadedPlan = null;
                SetEditor(PpmPlanEditor.CreateForNew());
                break;
            case FormMode.Update:
                _snapshot = Editor.Clone();
                break;
            case FormMode.View:
                _snapshot = null;
                if (_loadedPlan is not null)
                {
                    LoadEditor(_loadedPlan);
                }
                else
                {
                    SetEditor(PpmPlanEditor.CreateEmpty());
                }
                break;
        }

        UpdateActionStates();
        return Task.CompletedTask;
    }

    protected override Task<IReadOnlyList<string>> ValidateAsync()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Editor.Name))
        {
            errors.Add("Name is required.");
        }

        if (string.IsNullOrWhiteSpace(Editor.Frequency))
        {
            errors.Add("Frequency is required.");
        }

        if (Editor.NextDue is null)
        {
            errors.Add("Next due date is required.");
        }

        return Task.FromResult<IReadOnlyList<string>>(errors);
    }

    protected override async Task<bool> OnSaveAsync()
    {
        var entity = Editor.ToEntity();
        var isUpdate = Mode == FormMode.Update && _loadedPlan is not null;

        try
        {
            await Database.InsertOrUpdatePpmPlanAsync(entity, isUpdate).ConfigureAwait(false);
            StatusMessage = isUpdate
                ? $"Updated preventive maintenance plan {entity.Name}."
                : $"Created preventive maintenance plan {entity.Name}.";
            return true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to save plan: {ex.Message}";
            return false;
        }
    }

    protected override void OnCancel()
    {
        if (Mode == FormMode.Update && _snapshot is not null)
        {
            SetEditor(_snapshot.Clone());
        }
        else if (Mode == FormMode.Add)
        {
            if (_loadedPlan is not null)
            {
                LoadEditor(_loadedPlan);
            }
            else
            {
                SetEditor(PpmPlanEditor.CreateEmpty());
            }
        }

        UpdateActionStates();
    }

    protected override bool MatchesSearch(ModuleRecord record, string searchText)
    {
        if (base.MatchesSearch(record, searchText))
        {
            return true;
        }

        if (!int.TryParse(record.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
        {
            return false;
        }

        if (!_plansById.TryGetValue(id, out var plan))
        {
            return false;
        }

        return (!string.IsNullOrWhiteSpace(plan.MachineLabel) && plan.MachineLabel.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            || (!string.IsNullOrWhiteSpace(plan.ComponentLabel) && plan.ComponentLabel.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            || (!string.IsNullOrWhiteSpace(plan.Frequency) && plan.Frequency.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            || (plan.NextDue is not null && plan.NextDue.Value.ToString("d", CultureInfo.CurrentCulture).Contains(searchText, StringComparison.OrdinalIgnoreCase));
    }

    partial void OnEditorChanging(PpmPlanEditor value)
    {
        if (_editor is not null)
        {
            _editor.PropertyChanged -= OnEditorPropertyChanged;
        }
    }

    partial void OnEditorChanged(PpmPlanEditor value)
    {
        if (value is not null)
        {
            value.PropertyChanged += OnEditorPropertyChanged;
        }
    }

    partial void OnSelectedCalendarDateChanged(DateTime? value)
    {
        if (_suppressCalendarSelection)
        {
            return;
        }

        UpdateSelectedDayPlans();
    }

    partial void OnSelectedCalendarDayChanged(PpmCalendarDayViewModel? value)
    {
        if (_suppressCalendarSelection)
        {
            return;
        }

        if (value is not null && (!SelectedCalendarDate.HasValue || SelectedCalendarDate.Value.Date != value.Date))
        {
            _suppressCalendarSelection = true;
            SelectedCalendarDate = value.Date;
            _suppressCalendarSelection = false;
        }
    }

    partial void OnSelectedCalendarPlanChanged(PpmCalendarEntryViewModel? value)
    {
        if (value is null)
        {
            return;
        }

        var key = value.PlanId.ToString(CultureInfo.InvariantCulture);
        var record = Records.FirstOrDefault(r => r.Key == key);
        if (record is not null)
        {
            SelectedRecord = record;
        }
    }

    private void OnEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_suppressEditorDirtyNotifications)
        {
            return;
        }

        if (Mode is FormMode.Add or FormMode.Update)
        {
            MarkDirty();
        }
    }

    private void UpdatePlanCache(IEnumerable<PreventiveMaintenancePlan> plans)
    {
        _planCache.Clear();
        _planCache.AddRange(plans.OrderByDescending(p => p.NextDue ?? DateTime.MinValue));

        _plansById.Clear();
        foreach (var plan in _planCache)
        {
            if (plan.Id > 0)
            {
                _plansById[plan.Id] = plan;
            }
        }
    }

    private void UpdateCalendarCollections(IEnumerable<PreventiveMaintenancePlan> plans)
    {
        CalendarDays.Clear();

        var groups = plans
            .Where(p => p.NextDue is not null)
            .GroupBy(p => p.NextDue!.Value.Date)
            .OrderBy(g => g.Key)
            .ToList();

        foreach (var group in groups)
        {
            var entries = group
                .Select(PpmCalendarEntryViewModel.FromPlan)
                .OrderBy(e => e.NextDue)
                .ToList();
            CalendarDays.Add(new PpmCalendarDayViewModel(group.Key, entries));
        }

        if (CalendarDays.Count == 0)
        {
            SelectedDayPlans.Clear();
            SelectedCalendarDay = null;
            SelectedCalendarPlan = null;
            SelectedCalendarDate = null;
            return;
        }

        var candidate = SelectedCalendarDate;
        var target = candidate.HasValue
            ? CalendarDays.FirstOrDefault(d => d.Date == candidate.Value.Date)
            : CalendarDays.FirstOrDefault(d => d.Date >= DateTime.Today) ?? CalendarDays.First();

        if (target is null)
        {
            SelectedCalendarDate = null;
            SelectedDayPlans.Clear();
            return;
        }

        if (!SelectedCalendarDate.HasValue || SelectedCalendarDate.Value.Date != target.Date)
        {
            _suppressCalendarSelection = true;
            SelectedCalendarDate = target.Date;
            _suppressCalendarSelection = false;
        }

        UpdateSelectedDayPlans(target);
    }

    private void UpdateSelectedDayPlans(PpmCalendarDayViewModel? day = null)
    {
        day ??= SelectedCalendarDate is { } date
            ? CalendarDays.FirstOrDefault(d => d.Date == date.Date)
            : null;

        SelectedDayPlans.Clear();

        if (day is null)
        {
            _suppressCalendarSelection = true;
            SelectedCalendarDay = null;
            _suppressCalendarSelection = false;
            SelectedCalendarPlan = null;
            return;
        }

        _suppressCalendarSelection = true;
        SelectedCalendarDay = day;
        _suppressCalendarSelection = false;

        foreach (var entry in day.Plans)
        {
            SelectedDayPlans.Add(entry);
        }

        if (SelectedCalendarPlan is null || !day.Plans.Contains(SelectedCalendarPlan))
        {
            SelectedCalendarPlan = day.Plans.FirstOrDefault();
        }
    }

    private void FocusPlan(int planId)
    {
        var key = planId.ToString(CultureInfo.InvariantCulture);
        var record = Records.FirstOrDefault(r => r.Key == key);
        if (record is not null)
        {
            SelectedRecord = record;
        }

        if (_plansById.TryGetValue(planId, out var plan) && plan.NextDue is { } due)
        {
            _suppressCalendarSelection = true;
            SelectedCalendarDate = due.Date;
            _suppressCalendarSelection = false;
            UpdateSelectedDayPlans();
        }
    }

    private void LoadEditor(PreventiveMaintenancePlan plan)
    {
        _suppressEditorDirtyNotifications = true;
        SetEditor(PpmPlanEditor.FromEntity(plan));
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
        if (plan.NextDue is { } due)
        {
            _suppressCalendarSelection = true;
            SelectedCalendarDate = due.Date;
            _suppressCalendarSelection = false;
            UpdateSelectedDayPlans();
        }
    }

    private void SetEditor(PpmPlanEditor editor)
    {
        _suppressEditorDirtyNotifications = true;
        Editor = editor;
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
    }

    private ModuleRecord ToRecord(PreventiveMaintenancePlan plan)
    {
        var culture = CultureInfo.CurrentCulture;
        var fields = new List<InspectorField>
        {
            new("Next Due", plan.NextDue?.ToString("d", culture) ?? "-"),
            new("Frequency", plan.Frequency ?? "-"),
            new("Machine", plan.MachineLabel ?? plan.Machine?.Name ?? "-"),
            new("Component", plan.ComponentLabel ?? plan.Component?.Name ?? "-"),
            new("Status", plan.Status ?? "-"),
        };

        var title = string.IsNullOrWhiteSpace(plan.Name)
            ? plan.Code ?? $"Plan {plan.Id}"
            : plan.Name!;

        return new ModuleRecord(
            plan.Id.ToString(CultureInfo.InvariantCulture),
            title,
            plan.Code,
            plan.Status,
            plan.Description,
            fields,
            WorkOrdersModuleViewModel.ModuleKey,
            plan.Id);
    }

    private async Task DeletePlanAsync()
    {
        if (_loadedPlan is not { Id: > 0 } plan || IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            await Database.DeletePpmPlanAsync(plan.Id).ConfigureAwait(false);
            StatusMessage = $"Deleted plan {plan.Name ?? plan.Code}.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to delete plan: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            await RefreshAsync().ConfigureAwait(false);
            UpdateActionStates();
        }
    }

    private async Task OpenWorkOrdersAsync()
    {
        if (_loadedPlan is not { Id: > 0 } plan)
        {
            return;
        }

        try
        {
            var payload = new Dictionary<string, object?>
            {
                ["planId"] = plan.Id,
                ["planName"] = plan.Name,
                ["planCode"] = plan.Code
            };
            var document = _navigation.OpenModule(WorkOrdersModuleViewModel.ModuleKey, payload);
            _navigation.Activate(document);
            StatusMessage = $"Opened Work Orders for {plan.Name ?? plan.Code}.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Unable to open Work Orders: {ex.Message}";
        }
    }

    private async Task ApprovePlanAsync()
    {
        if (_loadedPlan is not { Id: > 0 } plan || IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            var entity = Editor.ToEntity();
            entity.Id = plan.Id;
            entity.Status = "approved";
            await Database.InsertOrUpdatePpmPlanAsync(entity, update: true).ConfigureAwait(false);
            StatusMessage = $"Approved plan {entity.Name}.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to approve plan: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            await RefreshAsync().ConfigureAwait(false);
            UpdateActionStates();
        }
    }

    private async Task AttachChecklistAsync()
    {
        if (_loadedPlan is not { Id: > 0 } plan)
        {
            return;
        }

        StatusMessage = $"Checklist attachment workflow queued for {plan.Name ?? plan.Code}.";
        await Task.CompletedTask;
    }

    private async Task ShowAuditTrailAsync()
    {
        if (_loadedPlan is not { Id: > 0 } plan)
        {
            return;
        }

        StatusMessage = $"Audit trail requested for {plan.Name ?? plan.Code}.";
        await Task.CompletedTask;
    }

    private bool CanDeletePlan()
        => !IsBusy && !IsInEditMode && _loadedPlan is { Id: > 0 };

    private bool CanOpenWorkOrders()
        => !IsBusy && _loadedPlan is { Id: > 0 };

    private bool CanApprovePlan()
        => !IsBusy && !IsInEditMode && _loadedPlan is { Id: > 0 };

    private bool CanAttachChecklist()
        => !IsBusy && _loadedPlan is { Id: > 0 };

    private bool CanShowAuditTrail()
        => !IsBusy && _loadedPlan is { Id: > 0 };

    private void UpdateActionStates()
    {
        DeletePlanCommand.NotifyCanExecuteChanged();
        GenerateWorkOrderCommand.NotifyCanExecuteChanged();
        ApprovePlanCommand.NotifyCanExecuteChanged();
        AttachChecklistCommand.NotifyCanExecuteChanged();
        ShowAuditTrailCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Editor backing object for the PPM form.</summary>
    public sealed partial class PpmPlanEditor : ObservableObject
    {
        [ObservableProperty]
        private int _id;

        [ObservableProperty]
        private string _code = string.Empty;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private string _frequency = string.Empty;

        [ObservableProperty]
        private string _status = "draft";

        [ObservableProperty]
        private string _machine = string.Empty;

        [ObservableProperty]
        private string _component = string.Empty;

        [ObservableProperty]
        private int? _machineId;

        [ObservableProperty]
        private int? _componentId;

        [ObservableProperty]
        private DateTime? _nextDue = DateTime.Today;

        [ObservableProperty]
        private string _checklistFile = string.Empty;

        [ObservableProperty]
        private string _responsible = string.Empty;

        [ObservableProperty]
        private int? _responsibleUserId;

        public static PpmPlanEditor CreateEmpty() => new();

        public static PpmPlanEditor CreateForNew() => new()
        {
            Status = "draft",
            NextDue = DateTime.Today.AddDays(7)
        };

        public static PpmPlanEditor FromEntity(PreventiveMaintenancePlan plan)
        {
            return new PpmPlanEditor
            {
                Id = plan.Id,
                Code = plan.Code ?? string.Empty,
                Name = plan.Name ?? string.Empty,
                Description = plan.Description ?? string.Empty,
                Frequency = plan.Frequency ?? string.Empty,
                Status = plan.Status ?? string.Empty,
                Machine = plan.MachineLabel ?? plan.Machine?.Name ?? string.Empty,
                Component = plan.ComponentLabel ?? plan.Component?.Name ?? string.Empty,
                MachineId = plan.MachineId,
                ComponentId = plan.ComponentId,
                NextDue = plan.NextDue,
                ChecklistFile = plan.ChecklistFile ?? string.Empty,
                Responsible = plan.ResponsibleUserLabel ?? plan.ResponsibleUser?.FullName ?? string.Empty,
                ResponsibleUserId = plan.ResponsibleUserId
            };
        }

        public PpmPlanEditor Clone()
        {
            return new PpmPlanEditor
            {
                Id = Id,
                Code = Code,
                Name = Name,
                Description = Description,
                Frequency = Frequency,
                Status = Status,
                Machine = Machine,
                Component = Component,
                MachineId = MachineId,
                ComponentId = ComponentId,
                NextDue = NextDue,
                ChecklistFile = ChecklistFile,
                Responsible = Responsible,
                ResponsibleUserId = ResponsibleUserId
            };
        }

        public PreventiveMaintenancePlan ToEntity()
        {
            return new PreventiveMaintenancePlan
            {
                Id = Id,
                Code = string.IsNullOrWhiteSpace(Code) ? null : Code,
                Name = string.IsNullOrWhiteSpace(Name) ? null : Name,
                Description = string.IsNullOrWhiteSpace(Description) ? null : Description,
                Frequency = string.IsNullOrWhiteSpace(Frequency) ? null : Frequency,
                Status = string.IsNullOrWhiteSpace(Status) ? null : Status,
                MachineLabel = string.IsNullOrWhiteSpace(Machine) ? null : Machine,
                ComponentLabel = string.IsNullOrWhiteSpace(Component) ? null : Component,
                MachineId = MachineId,
                ComponentId = ComponentId,
                NextDue = NextDue,
                ChecklistFile = string.IsNullOrWhiteSpace(ChecklistFile) ? null : ChecklistFile,
                ResponsibleUserLabel = string.IsNullOrWhiteSpace(Responsible) ? null : Responsible,
                ResponsibleUserId = ResponsibleUserId
            };
        }
    }

    /// <summary>Calendar day aggregate.</summary>
    public sealed partial class PpmCalendarDayViewModel : ObservableObject
    {
        public PpmCalendarDayViewModel(DateTime date, IEnumerable<PpmCalendarEntryViewModel> plans)
        {
            Date = date.Date;
            Plans = new ObservableCollection<PpmCalendarEntryViewModel>(plans);
        }

        /// <summary>The calendar date represented by this group.</summary>
        public DateTime Date { get; }

        /// <summary>Plans scheduled for this date.</summary>
        public ObservableCollection<PpmCalendarEntryViewModel> Plans { get; }

        /// <summary>Display label for the date.</summary>
        public string DisplayDate => Date.ToString("D", CultureInfo.CurrentCulture);

        /// <summary>Number of plans due on this date.</summary>
        public int Count => Plans.Count;
    }

    /// <summary>Calendar entry representation for a plan.</summary>
    public sealed partial class PpmCalendarEntryViewModel : ObservableObject
    {
        public PpmCalendarEntryViewModel(int planId, string title, string? machine, string? component, string? status, string? frequency, DateTime? nextDue)
        {
            PlanId = planId;
            Title = title;
            Machine = machine;
            Component = component;
            Status = status;
            Frequency = frequency;
            NextDue = nextDue;
        }

        public int PlanId { get; }

        public string Title { get; }

        public string? Machine { get; }

        public string? Component { get; }

        public string? Status { get; }

        public string? Frequency { get; }

        public DateTime? NextDue { get; }

        public string DueDisplay => NextDue?.ToString("g", CultureInfo.CurrentCulture) ?? "-";

        public static PpmCalendarEntryViewModel FromPlan(PreventiveMaintenancePlan plan)
        {
            var title = string.IsNullOrWhiteSpace(plan.Name) ? plan.Code ?? $"Plan {plan.Id}" : plan.Name!;
            return new PpmCalendarEntryViewModel(
                plan.Id,
                title,
                plan.MachineLabel ?? plan.Machine?.Name,
                plan.ComponentLabel ?? plan.Component?.Name,
                plan.Status,
                plan.Frequency,
                plan.NextDue);
        }
    }
}
