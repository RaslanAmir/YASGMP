using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

public sealed partial class WorkOrdersModuleViewModel : DataDrivenModuleDocumentViewModel
{
    public new const string ModuleKey = "WorkOrders";

    private readonly IAuthContext _authContext;
    private readonly WorkOrderService _workOrderService;

    private WorkOrder? _loadedEntity;
    private WorkOrderEditor? _snapshot;
    private bool _suppressEditorDirtyNotifications;

    public WorkOrdersModuleViewModel(
        DatabaseService databaseService,
        WorkOrderService workOrderService,
        IAuthContext authContext,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "Work Orders", databaseService, cflDialogService, shellInteraction, navigation)
    {
        _workOrderService = workOrderService ?? throw new ArgumentNullException(nameof(workOrderService));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        Editor = WorkOrderEditor.CreateEmpty();
    }

    [ObservableProperty]
    private WorkOrderEditor _editor;

    [ObservableProperty]
    private bool _isEditorEnabled;

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var workOrders = await Database.GetAllWorkOrdersFullAsync().ConfigureAwait(false);
        return workOrders.Select(ToRecord).ToList();
    }

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
        => new List<ModuleRecord>
        {
            new("WO-1001", "Preventive maintenance - Autoclave", "WO-1001", "In Progress", "Monthly PM",
                new[]
                {
                    new InspectorField("Assigned To", "Technician A"),
                    new InspectorField("Priority", "High"),
                    new InspectorField("Due", DateTime.Now.AddDays(2).ToString("d", CultureInfo.CurrentCulture))
                },
                AssetsModuleViewModel.ModuleKey, 1),
            new("WO-1002", "Calibration - pH meter", "WO-1002", "Open", "Annual calibration",
                new[]
                {
                    new InspectorField("Assigned To", "Technician B"),
                    new InspectorField("Priority", "Medium"),
                    new InspectorField("Due", DateTime.Now.AddDays(5).ToString("d", CultureInfo.CurrentCulture))
                },
                CalibrationModuleViewModel.ModuleKey, 2)
        };

    protected override async Task<CflRequest?> CreateCflRequestAsync()
    {
        var workOrders = await Database.GetAllWorkOrdersFullAsync().ConfigureAwait(false);
        var items = workOrders
            .Select(order =>
            {
                var key = order.Id.ToString(CultureInfo.InvariantCulture);
                var label = string.IsNullOrWhiteSpace(order.Title) ? key : order.Title;
                var descriptionParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(order.Status))
                {
                    descriptionParts.Add(order.Status!);
                }

                if (order.DueDate is not null)
                {
                    descriptionParts.Add(order.DueDate.Value.ToString("d", CultureInfo.CurrentCulture));
                }

                if (!string.IsNullOrWhiteSpace(order.Machine?.Name))
                {
                    descriptionParts.Add(order.Machine!.Name!);
                }

                var description = descriptionParts.Count > 0
                    ? string.Join(" • ", descriptionParts)
                    : null;

                return new CflItem(key, label, description);
            })
            .ToList();

        return new CflRequest("Select Work Order", items);
    }

    protected override Task OnCflSelectionAsync(CflResult result)
    {
        var search = result.Selected.Label;
        var match = Records.FirstOrDefault(r => r.Key == result.Selected.Key);
        if (match is not null)
        {
            SelectedRecord = match;
            search = match.Title;
        }

        SearchText = search;
        StatusMessage = $"Filtered {Title} by \"{search}\".";
        return Task.CompletedTask;
    }

    protected override async Task OnRecordSelectedAsync(ModuleRecord? record)
    {
        if (record is null)
        {
            _loadedEntity = null;
            SetEditor(WorkOrderEditor.CreateEmpty());
            return;
        }

        if (IsInEditMode)
        {
            return;
        }

        if (!int.TryParse(record.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
        {
            return;
        }

        try
        {
            var entity = await _workOrderService.GetByIdAsync(id).ConfigureAwait(false);
            _loadedEntity = entity;
            LoadEditor(entity);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Unable to load {record.Title}: {ex.Message}";
        }
    }

    protected override Task OnModeChangedAsync(FormMode mode)
    {
        IsEditorEnabled = mode is FormMode.Add or FormMode.Update;

        switch (mode)
        {
            case FormMode.Add:
                _snapshot = null;
                SetEditor(WorkOrderEditor.CreateForNew(_authContext));
                break;
            case FormMode.Update:
                _snapshot = Editor.Clone();
                break;
            case FormMode.View:
                _snapshot = null;
                break;
        }

        return Task.CompletedTask;
    }

    protected override async Task<IReadOnlyList<string>> ValidateAsync()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Editor.Title))
        {
            errors.Add("Title is required.");
        }

        if (string.IsNullOrWhiteSpace(Editor.Description))
        {
            errors.Add("Description is required.");
        }

        if (string.IsNullOrWhiteSpace(Editor.Type))
        {
            errors.Add("Type is required.");
        }

        if (string.IsNullOrWhiteSpace(Editor.Priority))
        {
            errors.Add("Priority is required.");
        }

        if (string.IsNullOrWhiteSpace(Editor.Status))
        {
            errors.Add("Status is required.");
        }

        if (Editor.MachineId <= 0)
        {
            errors.Add("Machine selection is required.");
        }

        if (Editor.RequestedById <= 0)
        {
            errors.Add("Requested by user is required.");
        }

        if (Editor.CreatedById <= 0)
        {
            errors.Add("Created by user is required.");
        }

        if (Editor.AssignedToId <= 0)
        {
            errors.Add("Assigned technician is required.");
        }

        if (string.IsNullOrWhiteSpace(Editor.Result))
        {
            errors.Add("Result summary is required.");
        }

        return await Task.FromResult(errors).ConfigureAwait(false);
    }

    protected override async Task<bool> OnSaveAsync()
    {
        if (Editor is null)
        {
            return false;
        }

        var userId = _authContext.CurrentUser?.Id;
        if (userId is null or <= 0)
        {
            userId = 1;
        }

        var entity = Editor.ToEntity(_loadedEntity);
        entity.LastModified = DateTime.UtcNow;
        entity.LastModifiedById = userId;
        entity.DeviceInfo = _authContext.CurrentDeviceInfo;
        entity.SourceIp = _authContext.CurrentIpAddress;
        entity.SessionId = _authContext.CurrentSessionId;

        if (Mode == FormMode.Add)
        {
            entity.CreatedById = Editor.CreatedById > 0 ? Editor.CreatedById : userId.Value;
            entity.RequestedById = Editor.RequestedById > 0 ? Editor.RequestedById : userId.Value;
            entity.AssignedToId = Editor.AssignedToId > 0 ? Editor.AssignedToId : userId.Value;
            entity.DateOpen = Editor.DateOpen == default ? DateTime.UtcNow : Editor.DateOpen;

            await _workOrderService.CreateAsync(entity, userId.Value).ConfigureAwait(false);

            _loadedEntity = entity;
            LoadEditor(entity);
            return true;
        }

        if (Mode == FormMode.Update)
        {
            await _workOrderService.UpdateAsync(entity, userId.Value).ConfigureAwait(false);

            _loadedEntity = entity;
            LoadEditor(entity);
            return true;
        }

        return false;
    }

    protected override void OnCancel()
    {
        if (Mode == FormMode.Add)
        {
            if (_loadedEntity is not null)
            {
                LoadEditor(_loadedEntity);
            }
            else
            {
                SetEditor(WorkOrderEditor.CreateEmpty());
            }
        }
        else if (Mode == FormMode.Update && _snapshot is not null)
        {
            SetEditor(_snapshot.Clone());
        }
    }

    partial void OnEditorChanging(WorkOrderEditor value)
    {
        if (value is null)
        {
            return;
        }

        value.PropertyChanged -= OnEditorPropertyChanged;
    }

    partial void OnEditorChanged(WorkOrderEditor value)
    {
        if (value is null)
        {
            return;
        }

        value.PropertyChanged += OnEditorPropertyChanged;
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

    private void LoadEditor(WorkOrder entity)
    {
        _suppressEditorDirtyNotifications = true;
        SetEditor(WorkOrderEditor.FromEntity(entity));
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
    }

    private void SetEditor(WorkOrderEditor editor)
    {
        _suppressEditorDirtyNotifications = true;
        Editor = editor;
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
    }

    private static ModuleRecord ToRecord(WorkOrder workOrder)
    {
        var fields = new List<InspectorField>
        {
            new("Assigned To", workOrder.AssignedTo?.FullName ?? workOrder.AssignedTo?.Username ?? "-"),
            new("Priority", workOrder.Priority),
            new("Status", workOrder.Status),
            new("Due Date", workOrder.DueDate?.ToString("d", CultureInfo.CurrentCulture) ?? "-"),
            new("Machine", workOrder.Machine?.Name ?? workOrder.MachineId.ToString(CultureInfo.InvariantCulture))
        };

        return new ModuleRecord(
            workOrder.Id.ToString(CultureInfo.InvariantCulture),
            workOrder.Title,
            workOrder.Title,
            workOrder.Status,
            workOrder.Description,
            fields,
            AssetsModuleViewModel.ModuleKey,
            workOrder.MachineId);
    }

    public sealed partial class WorkOrderEditor : ObservableObject
    {
        [ObservableProperty]
        private int _id;

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private string _taskDescription = string.Empty;

        [ObservableProperty]
        private string _type = string.Empty;

        [ObservableProperty]
        private string _priority = string.Empty;

        [ObservableProperty]
        private string _status = string.Empty;

        [ObservableProperty]
        private DateTime _dateOpen = DateTime.UtcNow;

        [ObservableProperty]
        private DateTime? _dueDate;

        [ObservableProperty]
        private DateTime? _dateClose;

        [ObservableProperty]
        private int _requestedById;

        [ObservableProperty]
        private int _createdById;

        [ObservableProperty]
        private int _assignedToId;

        [ObservableProperty]
        private int _machineId;

        [ObservableProperty]
        private int? _componentId;

        [ObservableProperty]
        private string _result = string.Empty;

        [ObservableProperty]
        private string _notes = string.Empty;

        public static WorkOrderEditor CreateEmpty() => new();

        public static WorkOrderEditor CreateForNew(IAuthContext authContext)
        {
            var userId = authContext.CurrentUser?.Id ?? 1;
            return new WorkOrderEditor
            {
                Status = "OPEN",
                Priority = "Medium",
                Type = "MAINTENANCE",
                Result = string.Empty,
                Notes = string.Empty,
                DateOpen = DateTime.UtcNow,
                RequestedById = userId,
                CreatedById = userId,
                AssignedToId = userId
            };
        }

        public static WorkOrderEditor FromEntity(WorkOrder entity)
        {
            return new WorkOrderEditor
            {
                Id = entity.Id,
                Title = entity.Title,
                Description = entity.Description,
                TaskDescription = entity.TaskDescription,
                Type = entity.Type,
                Priority = entity.Priority,
                Status = entity.Status,
                DateOpen = entity.DateOpen,
                DueDate = entity.DueDate,
                DateClose = entity.DateClose,
                RequestedById = entity.RequestedById,
                CreatedById = entity.CreatedById,
                AssignedToId = entity.AssignedToId,
                MachineId = entity.MachineId,
                ComponentId = entity.ComponentId,
                Result = entity.Result,
                Notes = entity.Notes
            };
        }

        public WorkOrderEditor Clone()
            => new()
            {
                Id = Id,
                Title = Title,
                Description = Description,
                TaskDescription = TaskDescription,
                Type = Type,
                Priority = Priority,
                Status = Status,
                DateOpen = DateOpen,
                DueDate = DueDate,
                DateClose = DateClose,
                RequestedById = RequestedById,
                CreatedById = CreatedById,
                AssignedToId = AssignedToId,
                MachineId = MachineId,
                ComponentId = ComponentId,
                Result = Result,
                Notes = Notes
            };

        public WorkOrder ToEntity(WorkOrder? existing)
        {
            var entity = existing ?? new WorkOrder();
            entity.Id = Id;
            entity.Title = Title;
            entity.Description = Description;
            entity.TaskDescription = TaskDescription;
            entity.Type = Type;
            entity.Priority = Priority;
            entity.Status = Status;
            entity.DateOpen = DateOpen;
            entity.DueDate = DueDate;
            entity.DateClose = DateClose;
            entity.RequestedById = RequestedById;
            entity.CreatedById = CreatedById;
            entity.AssignedToId = AssignedToId;
            entity.MachineId = MachineId;
            entity.ComponentId = ComponentId;
            entity.Result = Result;
            entity.Notes = Notes;
            return entity;
        }
    }
}
