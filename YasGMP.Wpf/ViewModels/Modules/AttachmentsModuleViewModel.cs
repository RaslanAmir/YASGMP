using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
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

        UploadCommand = new AsyncRelayCommand(UploadAsync, CanUpload);
        DownloadCommand = new AsyncRelayCommand(DownloadAsync, CanDownload);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, CanDelete);
        Toolbar.Add(new ModuleToolbarCommand("Upload", UploadCommand));
        Toolbar.Add(new ModuleToolbarCommand("Download", DownloadCommand));
        Toolbar.Add(new ModuleToolbarCommand("Delete", DeleteCommand));

        PropertyChanged += OnPropertyChanged;

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

    public IAsyncRelayCommand UploadCommand { get; }

    public IAsyncRelayCommand DownloadCommand { get; }

    public IAsyncRelayCommand DeleteCommand { get; }

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var attachments = await _databaseService.GetAttachmentsFilteredAsync(null, null, null).ConfigureAwait(false);

        var rows = new List<AttachmentRowViewModel>();
        var records = new List<ModuleRecord>();

        foreach (var attachment in attachments)
        {
            rows.Add(new AttachmentRowViewModel(attachment));
            records.Add(ToRecord(attachment));
        }

        ApplyAttachmentRows(rows);

        return records;
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

        return new ModuleRecord(
            attachment.Id.ToString(CultureInfo.InvariantCulture),
            string.IsNullOrWhiteSpace(attachment.Name) ? attachment.FileName : attachment.Name,
            attachment.FileName,
            attachment.Status,
            attachment.Notes ?? attachment.Note,
            CreateRecordInspectorFields(attachment),
            moduleKey,
            attachment.EntityId);
    }

    private static IReadOnlyList<InspectorField> CreateRecordInspectorFields(Attachment attachment)
    {
        var entity = string.IsNullOrWhiteSpace(attachment.EntityType)
            ? "-"
            : attachment.EntityType;

        var linkedRecordId = attachment.EntityId?.ToString(CultureInfo.InvariantCulture) ?? "-";
        var sha256 = string.IsNullOrWhiteSpace(attachment.Sha256) ? "-" : attachment.Sha256;
        var status = string.IsNullOrWhiteSpace(attachment.Status) ? "-" : attachment.Status;

        return new List<InspectorField>
        {
            new("Entity/Table", entity),
            new("Linked Record Id", linkedRecordId),
            new("SHA-256", sha256),
            new("Status", status)
        };
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

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(IsBusy), StringComparison.Ordinal))
        {
            UpdateAttachmentCommands();
        }
    }

    private bool CanUpload()
        => !IsBusy && HasAttachmentWorkflow;

    private bool CanDownload()
        => !IsBusy && SelectedAttachment is not null && HasAttachmentWorkflow;

    private bool CanDelete()
        => !IsBusy && SelectedAttachment is not null;

    private async Task UploadAsync()
    {
        if (!HasAttachmentWorkflow)
        {
            StatusMessage = "Attachment workflow unavailable.";
            return;
        }

        var files = await _filePicker
            .PickFilesAsync(new FilePickerRequest(AllowMultiple: true, Title: "Select attachments to upload"))
            .ConfigureAwait(false);

        if (files is null || files.Count == 0)
        {
            StatusMessage = "Attachment upload cancelled.";
            return;
        }

        try
        {
            IsBusy = true;
            UpdateAttachmentCommands();

            var uploaded = 0;
            var duplicates = 0;
            var failed = 0;
            var notes = new List<string>();

            foreach (var file in files)
            {
                var staged = CreateStagedUpload(file);
                StagedUploads.Add(staged);

                var tempDirectory = Path.Combine(Path.GetTempPath(), "YasGMP", "uploads", Guid.NewGuid().ToString("N"));
                var tempPath = Path.Combine(tempDirectory, file.FileName);

                try
                {
                    Directory.CreateDirectory(tempDirectory);

                    await using (var source = await file.OpenReadAsync().ConfigureAwait(false))
                    await using (var destination = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 128 * 1024, useAsync: true))
                    using (var sha = SHA256.Create())
                    {
                        var buffer = new byte[128 * 1024];
                        long total = 0;
                        int read;
                        while ((read = await source.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                        {
                            await destination.WriteAsync(buffer.AsMemory(0, read)).ConfigureAwait(false);
                            sha.TransformBlock(buffer, 0, read, null, 0);
                            total += read;
                        }

                        sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                        var hash = sha.Hash is null || sha.Hash.Length == 0
                            ? string.Empty
                            : Convert.ToHexString(sha.Hash);

                        if (!string.IsNullOrWhiteSpace(hash))
                        {
                            var existing = await _attachmentService
                                .FindByHashAndSizeAsync(hash, total)
                                .ConfigureAwait(false);

                            if (existing is not null)
                            {
                                duplicates++;
                                notes.Add($"Skipped '{file.FileName}' (duplicate of #{existing.Id}).");
                                continue;
                            }

                            staged.Notes = hash;
                        }

                        await destination.FlushAsync().ConfigureAwait(false);
                    }

                    await using var uploadStream = new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.Read, 128 * 1024, useAsync: true);
                    var entityType = string.IsNullOrWhiteSpace(staged.EntityType) ? "attachments" : staged.EntityType;
                    var request = new AttachmentUploadRequest
                    {
                        FileName = file.FileName,
                        ContentType = staged.ContentType,
                        EntityType = entityType,
                        EntityId = staged.EntityId,
                        Notes = staged.Notes,
                        Reason = $"wpf:{ModuleKey}:upload",
                        SourceHost = Environment.MachineName,
                        SourceIp = "ui:wpf"
                    };

                    var uploadResult = await _attachmentService
                        .UploadAsync(uploadStream, request)
                        .ConfigureAwait(false);

                    var proxy = new AttachmentServiceUploadProxy(_attachmentService, uploadResult);
                    await _databaseService
                        .AddAttachmentAsync(
                            tempPath,
                            entityType,
                            staged.EntityId,
                            uploadResult.Attachment.UploadedById ?? 0,
                            "ui:wpf",
                            Environment.MachineName,
                            $"wpf:{ModuleKey}",
                            request.Reason,
                            proxy)
                        .ConfigureAwait(false);

                    uploaded++;
                    notes.Add($"Uploaded '{file.FileName}'.");
                }
                catch (Exception ex)
                {
                    failed++;
                    notes.Add($"Failed '{file.FileName}': {ex.Message}");
                }
                finally
                {
                    StagedUploads.Remove(staged);
                    TryDeleteTempDirectory(Path.GetDirectoryName(tempPath));
                }
            }

            StatusMessage = BuildUploadStatus(uploaded, duplicates, failed, notes);

            if (uploaded > 0)
            {
                await RefreshAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Attachment upload failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            UpdateAttachmentCommands();
        }
    }

    private async Task DownloadAsync()
    {
        if (SelectedAttachment is null)
        {
            StatusMessage = "Select an attachment to download.";
            return;
        }

        if (!HasAttachmentWorkflow)
        {
            StatusMessage = "Attachment workflow unavailable.";
            return;
        }

        var dialog = new SaveFileDialog
        {
            FileName = SelectedAttachment.FileName,
            Title = $"Save {SelectedAttachment.FileName}",
            Filter = "All files (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
        {
            StatusMessage = "Download cancelled.";
            return;
        }

        try
        {
            IsBusy = true;
            UpdateAttachmentCommands();
            StatusMessage = $"Downloading '{SelectedAttachment.FileName}'...";

            await using var destination = new FileStream(dialog.FileName, FileMode.Create, FileAccess.Write, FileShare.None, 128 * 1024, useAsync: true);
            var request = new AttachmentReadRequest
            {
                Reason = $"wpf:{ModuleKey}:download",
                SourceHost = Environment.MachineName,
                SourceIp = "ui:wpf"
            };

            var result = await _attachmentService
                .StreamContentAsync(SelectedAttachment.Id, destination, request)
                .ConfigureAwait(false);

            var inspectorFields = BuildInspectorFields(SelectedAttachment);
            _shellInteractionService.UpdateInspector(new InspectorContext(Title, SelectedAttachment.DisplayName, inspectorFields));

            StatusMessage = $"Downloaded {FormatBytes(result.BytesWritten)} of {FormatBytes(result.TotalLength)} to '{dialog.FileName}'.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Download failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            UpdateAttachmentCommands();
        }
    }

    private async Task DeleteAsync()
    {
        if (SelectedAttachment is null)
        {
            StatusMessage = "Select an attachment to delete.";
            return;
        }

        try
        {
            IsBusy = true;
            UpdateAttachmentCommands();

            await _databaseService
                .DeleteAttachmentAsync(SelectedAttachment.Id)
                .ConfigureAwait(false);

            StatusMessage = $"Attachment '{SelectedAttachment.FileName}' deleted.";
            await RefreshAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Delete failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            UpdateAttachmentCommands();
        }
    }

    private void UpdateAttachmentCommands()
    {
        UploadCommand.NotifyCanExecuteChanged();
        DownloadCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
    }

    private StagedAttachmentUploadViewModel CreateStagedUpload(PickedFile file)
    {
        var entityType = SelectedAttachment?.Model.EntityType;
        var entityId = SelectedAttachment?.Model.EntityId ?? 0;

        return new StagedAttachmentUploadViewModel
        {
            FileName = file.FileName,
            ContentType = file.ContentType,
            EntityType = string.IsNullOrWhiteSpace(entityType) ? "attachments" : entityType!,
            EntityId = entityId,
            Notes = SelectedAttachment?.Model.Notes
        };
    }

    private static string BuildUploadStatus(int uploaded, int duplicates, int failed, IReadOnlyList<string> notes)
    {
        var parts = new List<string>
        {
            $"Uploaded {uploaded} file(s)"
        };

        if (duplicates > 0)
        {
            parts.Add($"skipped {duplicates} duplicate(s)");
        }

        if (failed > 0)
        {
            parts.Add($"{failed} failed");
        }

        var summary = string.Join(", ", parts) + ".";

        if (notes.Count > 0)
        {
            summary += " " + string.Join(" ", notes);
        }

        return summary;
    }

    private static string FormatBytes(long value)
    {
        if (value <= 0)
        {
            return "0 B";
        }

        const double kilo = 1024d;
        const double mega = kilo * 1024d;

        if (value < kilo)
        {
            return value + " B";
        }

        if (value < mega)
        {
            return (value / kilo).ToString("F1", CultureInfo.CurrentCulture) + " KB";
        }

        return (value / mega).ToString("F1", CultureInfo.CurrentCulture) + " MB";
    }

    private static void TryDeleteTempDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup failures; temporary files will be removed by the OS later.
        }
    }

    partial void OnSelectedAttachmentChanged(AttachmentRowViewModel? value)
    {
        UpdateAttachmentCommands();
    }

    private sealed class AttachmentServiceUploadProxy : IAttachmentService
    {
        private readonly IAttachmentService _inner;
        private readonly AttachmentUploadResult _result;

        public AttachmentServiceUploadProxy(IAttachmentService inner, AttachmentUploadResult result)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _result = result ?? throw new ArgumentNullException(nameof(result));
        }

        public Task<AttachmentUploadResult> UploadAsync(Stream content, AttachmentUploadRequest request, CancellationToken token = default)
            => Task.FromResult(_result);

        public Task<Attachment?> FindByHashAsync(string sha256, CancellationToken token = default)
            => _inner.FindByHashAsync(sha256, token);

        public Task<Attachment?> FindByHashAndSizeAsync(string sha256, long fileSize, CancellationToken token = default)
            => _inner.FindByHashAndSizeAsync(sha256, fileSize, token);

        public Task<AttachmentStreamResult> StreamContentAsync(int attachmentId, Stream destination, AttachmentReadRequest? request = null, CancellationToken token = default)
            => _inner.StreamContentAsync(attachmentId, destination, request, token);

        public Task<IReadOnlyList<AttachmentLinkWithAttachment>> GetLinksForEntityAsync(string entityType, int entityId, CancellationToken token = default)
            => _inner.GetLinksForEntityAsync(entityType, entityId, token);

        public Task RemoveLinkAsync(int linkId, CancellationToken token = default)
            => _inner.RemoveLinkAsync(linkId, token);

        public Task RemoveLinkAsync(string entityType, int entityId, int attachmentId, CancellationToken token = default)
            => _inner.RemoveLinkAsync(entityType, entityId, attachmentId, token);
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
