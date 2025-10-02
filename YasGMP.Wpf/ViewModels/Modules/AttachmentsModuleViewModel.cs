using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

public sealed class AttachmentsModuleViewModel : ModuleDocumentViewModel
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
    }

    public bool HasAttachmentWorkflow { get; }

    public bool HasShellIntegration { get; }

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var attachments = await _databaseService.GetAttachmentsFilteredAsync(null, null, null).ConfigureAwait(false);
        return attachments.Select(ToRecord).ToList();
    }

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
        => new List<ModuleRecord>
        {
            new("ATT-001", "Calibration Certificate", "certificate.pdf", "Approved", "Calibration PDF",
                new[]
                {
                    new InspectorField("Table", "calibrations"),
                    new InspectorField("Record", "100"),
                    new InspectorField("Uploaded", System.DateTime.Now.AddDays(-3).ToString("g", CultureInfo.CurrentCulture))
                },
                CalibrationModuleViewModel.ModuleKey, 100),
            new("ATT-002", "Work Order Photo", "photo.jpg", "Pending", "Machine photo",
                new[]
                {
                    new InspectorField("Table", "work_orders"),
                    new InspectorField("Record", "1001"),
                    new InspectorField("Uploaded", System.DateTime.Now.AddDays(-1).ToString("g", CultureInfo.CurrentCulture))
                },
                WorkOrdersModuleViewModel.ModuleKey, 1001)
        };

    private static ModuleRecord ToRecord(Attachment attachment)
    {
        var moduleKey = attachment.EntityTable?.ToLowerInvariant() switch
        {
            "calibrations" => CalibrationModuleViewModel.ModuleKey,
            "work_orders" => WorkOrdersModuleViewModel.ModuleKey,
            "capa_cases" => CapaModuleViewModel.ModuleKey,
            "users" => SecurityModuleViewModel.ModuleKey,
            _ => null
        };

        var fields = new List<InspectorField>
        {
            new("Table", attachment.EntityTable ?? "-"),
            new("Record", attachment.EntityId?.ToString(CultureInfo.InvariantCulture) ?? "-"),
            new("File Type", attachment.FileType ?? "-"),
            new("Created", attachment.CreatedAt.ToString("g", CultureInfo.CurrentCulture)),
            new("Status", attachment.Status ?? "-"),
        };

        return new ModuleRecord(
            attachment.Id.ToString(CultureInfo.InvariantCulture),
            attachment.FileName,
            attachment.FileName,
            attachment.Status,
            attachment.Description,
            fields,
            moduleKey,
            attachment.EntityId);
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
}
