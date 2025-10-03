using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

public sealed partial class AttachmentsModuleViewModel : ModuleDocumentViewModel
{
    public const string ModuleKey = "Attachments";

    public AttachmentsModuleViewModel(
        DatabaseService databaseService,
        IAttachmentService attachmentService,
        IFilePicker filePicker,
        IElectronicSignatureDialogService signatureDialogService,
        AuditService auditService,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "Attachments", cflDialogService, shellInteraction, navigation)
    {
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        _attachmentService = attachmentService ?? throw new ArgumentNullException(nameof(attachmentService));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _signatureDialogService = signatureDialogService ?? throw new ArgumentNullException(nameof(signatureDialogService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _cflDialogService = cflDialogService ?? throw new ArgumentNullException(nameof(cflDialogService));
        _shellInteractionService = shellInteraction ?? throw new ArgumentNullException(nameof(shellInteraction));
        _navigationService = navigation ?? throw new ArgumentNullException(nameof(navigation));

        HasAttachmentWorkflow = _attachmentService is not null
            && _filePicker is not null
            && _signatureDialogService is not null
            && _auditService is not null;

        HasShellIntegration = _cflDialogService is not null
            && _shellInteractionService is not null
            && _navigationService is not null;

        AttachmentRows = new ObservableCollection<AttachmentRowViewModel>();
        StagedUploads = new ObservableCollection<StagedAttachmentUploadViewModel>();
        StagedUploads.CollectionChanged += OnStagedUploadsChanged;

        if (IsInDesignMode())
        {
            Records.Clear();
            foreach (var record in CreateDesignTimeRecords())
            {
                Records.Add(record);
            }

            SelectedRecord = Records.Count > 0 ? Records[0] : null;
            StatusMessage = FormatLoadedStatus(Records.Count);
        }
        else
        {
            ResetAttachmentState(clearStagedUploads: true, clearAttachmentRows: true);
        }
    }

    public bool HasAttachmentWorkflow { get; }

    public bool HasShellIntegration { get; }

    public ObservableCollection<AttachmentRowViewModel> AttachmentRows { get; }

    public ObservableCollection<StagedAttachmentUploadViewModel> StagedUploads { get; }

    public bool HasStagedUploads => StagedUploads.Count > 0;

    [ObservableProperty]
    private AttachmentRowViewModel? _selectedAttachment;

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var attachments = await _databaseService.GetAttachmentsFilteredAsync(null, null, null).ConfigureAwait(false);
        var rows = attachments.Select(attachment => new AttachmentRowViewModel(attachment)).ToList();
        ApplyAttachmentRows(rows);
        return attachments.Select(ToRecord).ToList();
    }

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
    {
        var rows = new List<AttachmentRowViewModel>
        {
            new(new Attachment
            {
                Id = 1,
                FileName = "certificate.pdf",
                Name = "Calibration Certificate",
                EntityType = "calibrations",
                EntityId = 100,
                Status = "Approved",
                Notes = "Calibration PDF",
                FileType = "pdf",
                FileSize = 256_000,
                UploadedAt = DateTime.Now.AddDays(-3),
                Sha256 = "ABC123",
                RetentionPolicyName = "Calibration",
                RetainUntil = DateTime.Now.AddYears(1)
            }),
            new(new Attachment
            {
                Id = 2,
                FileName = "photo.jpg",
                Name = "Work Order Photo",
                EntityType = "work_orders",
                EntityId = 1001,
                Status = "Pending",
                Notes = "Machine photo",
                FileType = "jpg",
                FileSize = 512_000,
                UploadedAt = DateTime.Now.AddDays(-1),
                Sha256 = "XYZ789"
            })
        };

        ApplyAttachmentRows(rows);

        return rows
            .Select(row => ToRecord(row.Model))
            .ToList();
    }

    private static ModuleRecord ToRecord(Attachment attachment)
    {
        var moduleKey = attachment.EntityType?.ToLowerInvariant() switch
        {
            "calibrations" => CalibrationModuleViewModel.ModuleKey,
            "work_orders" => WorkOrdersModuleViewModel.ModuleKey,
            "capa_cases" => CapaModuleViewModel.ModuleKey,
            "users" => SecurityModuleViewModel.ModuleKey,
            _ => null
        };

        var fields = new List<InspectorField>
        {
            new("Table", attachment.EntityType ?? "-"),
            new("Record", attachment.EntityId?.ToString(CultureInfo.InvariantCulture) ?? "-"),
            new("File Type", attachment.FileType ?? "-"),
            new("Created", attachment.UploadedAt.ToString("g", CultureInfo.CurrentCulture)),
            new("Status", attachment.Status ?? "-"),
        };

        return new ModuleRecord(
            attachment.Id.ToString(CultureInfo.InvariantCulture),
            string.IsNullOrWhiteSpace(attachment.Name) ? attachment.FileName : attachment.Name,
            attachment.FileName,
            attachment.Status,
            attachment.Notes ?? attachment.Note,
            fields,
            moduleKey,
            attachment.EntityId);
    }

    protected override Task OnRecordSelectedAsync(ModuleRecord? record)
    {
        if (record is null)
        {
            ResetAttachmentState(clearStagedUploads: false, clearAttachmentRows: false);
            _shellInteractionService.UpdateInspector(
                new InspectorContext(Title, "No attachment selected", Array.Empty<InspectorField>()));
            return Task.CompletedTask;
        }

        if (!int.TryParse(record.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var attachmentId))
        {
            ResetAttachmentState(clearStagedUploads: false, clearAttachmentRows: false);
            _shellInteractionService.UpdateInspector(new InspectorContext(Title, record.Title, record.InspectorFields));
            return Task.CompletedTask;
        }

        var attachment = AttachmentRows.FirstOrDefault(row => row.Id == attachmentId);
        if (attachment is null)
        {
            ResetAttachmentState(clearStagedUploads: false, clearAttachmentRows: false);
            _shellInteractionService.UpdateInspector(new InspectorContext(Title, record.Title, record.InspectorFields));
            return Task.CompletedTask;
        }

        SelectedAttachment = attachment;

        var inspectorFields = BuildInspectorFields(attachment);
        _shellInteractionService.UpdateInspector(
            new InspectorContext(Title, attachment.DisplayName, inspectorFields));

        return Task.CompletedTask;
    }

    private IReadOnlyList<InspectorField> BuildInspectorFields(AttachmentRowViewModel attachment)
    {
        var fields = new List<InspectorField>
        {
            new("Attachment Id", attachment.Id.ToString(CultureInfo.InvariantCulture)),
            new("File Name", attachment.FileName),
            new("Display Name", attachment.DisplayName),
            new("Status", attachment.Status ?? "-"),
            new("Entity", attachment.EntityDisplayName),
            new("Uploaded", attachment.UploadedAt.ToString("g", CultureInfo.CurrentCulture)),
            new("File Type", attachment.FileType ?? "-"),
            new("File Size", attachment.FileSizeDisplay),
            new("SHA-256", attachment.Sha256 ?? "-"),
            new("Notes", attachment.Notes ?? string.Empty)
        };

        if (!string.IsNullOrWhiteSpace(attachment.RetentionPolicyName) || attachment.RetainUntil is not null)
        {
            fields.Add(new InspectorField("Retention Policy", attachment.RetentionPolicyName ?? "-"));
            fields.Add(new InspectorField(
                "Retain Until",
                attachment.RetainUntil?.ToString("g", CultureInfo.CurrentCulture) ?? "-"));
        }

        if (attachment.RetentionLegalHold)
        {
            fields.Add(new InspectorField("Legal Hold", "Enabled"));
        }

        if (attachment.RetentionReviewRequired)
        {
            fields.Add(new InspectorField("Manual Review", "Required"));
        }

        if (!string.IsNullOrWhiteSpace(attachment.RetentionNotes))
        {
            fields.Add(new InspectorField("Retention Notes", attachment.RetentionNotes));
        }

        return fields;
    }

    private void ApplyAttachmentRows(IReadOnlyList<AttachmentRowViewModel> rows)
    {
        AttachmentRows.Clear();
        foreach (var row in rows)
        {
            AttachmentRows.Add(row);
        }

        if (AttachmentRows.Count == 0)
        {
            SelectedAttachment = null;
        }
    }

    private void ResetAttachmentState(bool clearStagedUploads, bool clearAttachmentRows)
    {
        if (clearAttachmentRows && AttachmentRows.Count > 0)
        {
            AttachmentRows.Clear();
        }

        SelectedAttachment = null;

        if (clearStagedUploads && StagedUploads.Count > 0)
        {
            StagedUploads.Clear();
        }
    }

    private void OnStagedUploadsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasStagedUploads));
    }

    private static bool IsInDesignMode()
        => DesignerProperties.GetIsInDesignMode(new DependencyObject());

    private readonly DatabaseService _databaseService;
    private readonly IAttachmentService _attachmentService;
    private readonly IFilePicker _filePicker;
    private readonly IElectronicSignatureDialogService _signatureDialogService;
    private readonly AuditService _auditService;
    private readonly ICflDialogService _cflDialogService;
    private readonly IShellInteractionService _shellInteractionService;
    private readonly IModuleNavigationService _navigationService;

    public sealed class AttachmentRowViewModel
    {
        public AttachmentRowViewModel(Attachment model)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public Attachment Model { get; }

        public int Id => Model.Id;

        public string FileName => string.IsNullOrWhiteSpace(Model.FileName) ? "(unknown)" : Model.FileName;

        public string DisplayName => string.IsNullOrWhiteSpace(Model.Name) ? FileName : Model.Name;

        public string? Status => Model.Status;

        public string EntityDisplayName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Model.EntityType))
                {
                    return "-";
                }

                return Model.EntityId is null
                    ? Model.EntityType!
                    : $"{Model.EntityType}/{Model.EntityId}";
            }
        }

        public string? FileType => Model.FileType;

        public string FileSizeDisplay
        {
            get
            {
                if (Model.FileSize is null || Model.FileSize.Value <= 0)
                {
                    return "-";
                }

                var size = Model.FileSize.Value;
                if (size < 1024)
                {
                    return size + " B";
                }

                if (size < 1024 * 1024)
                {
                    return (size / 1024d).ToString("F1", CultureInfo.CurrentCulture) + " KB";
                }

                return (size / 1024d / 1024d).ToString("F1", CultureInfo.CurrentCulture) + " MB";
            }
        }

        public string? Sha256 => Model.Sha256;

        public string? Notes => string.IsNullOrWhiteSpace(Model.Notes) ? Model.Note : Model.Notes;

        public DateTime UploadedAt => Model.UploadedAt;

        public string? RetentionPolicyName => Model.RetentionPolicyName;

        public DateTime? RetainUntil => Model.RetainUntil;

        public bool RetentionLegalHold => Model.RetentionLegalHold;

        public bool RetentionReviewRequired => Model.RetentionReviewRequired;

        public string? RetentionNotes => Model.RetentionNotes;
    }

    public sealed class StagedAttachmentUploadViewModel : ObservableObject
    {
        private string _fileName = string.Empty;
        private string? _contentType;
        private string _entityType = string.Empty;
        private int _entityId;
        private string? _notes;

        public string FileName
        {
            get => _fileName;
            set => SetProperty(ref _fileName, value);
        }

        public string? ContentType
        {
            get => _contentType;
            set => SetProperty(ref _contentType, value);
        }

        public string EntityType
        {
            get => _entityType;
            set => SetProperty(ref _entityType, value);
        }

        public int EntityId
        {
            get => _entityId;
            set => SetProperty(ref _entityId, value);
        }

        public string? Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }
    }
}
