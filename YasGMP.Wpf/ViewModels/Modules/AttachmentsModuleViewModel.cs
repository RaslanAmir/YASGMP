using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Diagnostics;
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
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>Manages database-backed attachments and their signatures within the WPF shell.</summary>
/// <remarks>
/// Form Modes: Find lists attachments with filtering, Add and Update enable staging uploads plus metadata edits, and View locks the grid for read-only inspection.
/// Audit &amp; Logging: Commits staged uploads only after capturing an electronic signature and records an entity-level audit entry per file via <see cref="AuditService.LogEntityAuditAsync"/>.
/// Localization: Uses inline strings for captions and status text (e.g. `"Attachments"`, `"Upload"`, `"Staged uploads pending signature"`); resource keys will replace these literals once available.
/// Navigation: ModuleKey `Attachments` registers the document with the shell; `ModuleRecord` entries populate related module keys so Golden Arrow launches entity-specific editors while status messages keep the ribbon informed.
/// </remarks>
    public sealed partial class AttachmentsModuleViewModel : ModuleDocumentViewModel
{
    /// <summary>Shell registration key that binds Attachments into the docking catalog.</summary>
    /// <remarks>Execution: Resolved during module composition so ribbon tabs and layout persistence can route to this view. Form Mode: Applies to all modes as a neutral identifier. Localization: Currently paired with the inline caption literal "Attachments" until `Modules_Attachments_Title` exists.</remarks>
    public new const string ModuleKey = "Attachments";

    /// <summary>Initializes the attachments surface with persistence, signature, and navigation services.</summary>
    /// <remarks>Execution: Invoked at module activation when the shell resolves dependencies. Form Mode: Ensures Find/View primes lists while Add/Update wire staging plus e-signature prompts. Localization: Uses inline labels such as "Attachments" and "Upload" pending resource bindings.</remarks>
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
        _signatureDialog = signatureDialogService ?? throw new ArgumentNullException(nameof(signatureDialogService));
        _audit = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _cflDialogService = cflDialogService ?? throw new ArgumentNullException(nameof(cflDialogService));
        _shellInteractionService = shellInteraction ?? throw new ArgumentNullException(nameof(shellInteraction));
        _navigationService = navigation ?? throw new ArgumentNullException(nameof(navigation));

        HasAttachmentWorkflow = _attachmentService is not null
            && _filePicker is not null
            && _signatureDialog is not null
            && _audit is not null;

        HasShellIntegration = _cflDialogService is not null
            && _shellInteractionService is not null
            && _navigationService is not null;

        AttachmentRows = new ObservableCollection<AttachmentRowViewModel>();
        StagedUploads = new ObservableCollection<StagedAttachmentUploadViewModel>();
        StagedUploads.CollectionChanged += OnStagedUploadsChanged;

        UploadCommand = new AsyncRelayCommand(UploadAsync, CanUpload);
        DownloadCommand = new AsyncRelayCommand(DownloadAsync, CanDownload);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, CanDelete);
        IndexEmbeddingCommand = new AsyncRelayCommand(IndexEmbeddingAsync, CanIndexEmbedding);
        FindSimilarCommand = new AsyncRelayCommand(FindSimilarAsync, CanIndexEmbedding);
        OpenSimilarCommand = new RelayCommand<object?>(OpenSimilar);
        Toolbar.Add(new ModuleToolbarCommand("Upload", UploadCommand));
        Toolbar.Add(new ModuleToolbarCommand("Download", DownloadCommand));
        Toolbar.Add(new ModuleToolbarCommand("Delete", DeleteCommand));
        Toolbar.Add(new ModuleToolbarCommand("Index AI Embedding", IndexEmbeddingCommand));
        Toolbar.Add(new ModuleToolbarCommand("Find Similar (AI)", FindSimilarCommand));

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

    /// <summary>Indicates whether upload/download/signature services were provided by the container.</summary>
    /// <remarks>Execution: Evaluated during construction and read whenever commands toggle availability. Form Mode: Checked during Add/Update when attachments become editable; ignored during Find/View. Localization: Drives inline status text ("Staged uploads pending signature") until formal resources land.</remarks>
    public bool HasAttachmentWorkflow { get; }

    /// <summary>Signals that CFL dialogs and shell navigation hooks are available.</summary>
    /// <remarks>Execution: Computed in the constructor before the shell wires Golden Arrow routing. Form Mode: Enables link-outs from Find/View as well as Add/Update inspectors. Localization: Supports inline shell status messages pending `Shell_Status_Attachments_*` resources.</remarks>
    public bool HasShellIntegration { get; }

    /// <summary>Rows presented in the attachments grid backing the document host.</summary>
    /// <remarks>Execution: Populated after `LoadAsync` completes or design-time seed flows. Form Mode: Always visible; only Add/Update enable per-row editing affordances. Localization: Column headers remain inline until grid resources are published.</remarks>
    public ObservableCollection<AttachmentRowViewModel> AttachmentRows { get; }

    /// <summary>Pending upload sessions created during Add/Update flows.</summary>
    /// <remarks>Execution: Mutated by staging commands as users pick files. Form Mode: Only Add/Update push items into the collection; Find/View observe read-only. Localization: Status prompts and badges use literals pending RESX entries.</remarks>
    public ObservableCollection<StagedAttachmentUploadViewModel> StagedUploads { get; }

    /// <summary>Gets a value indicating whether staged uploads exist for signature capture.</summary>
    /// <remarks>Execution: Evaluated on collection change events to update ribbon buttons. Form Mode: Relevant solely to Add/Update transactions. Localization: Tied to inline badge strings until shared resources arrive.</remarks>
    public bool HasStagedUploads => StagedUploads.Count > 0;

    /// <summary>Top-K similar attachments discovered via vector search (AI).</summary>
    public ObservableCollection<SimilarAttachmentItem> SimilarAttachments { get; } = new();

    /// <summary>Lightweight item for the Similar Attachments panel.</summary>
    public sealed class SimilarAttachmentItem
    {
        public int AttachmentId { get; init; }
        public double Score { get; init; }
        public string DisplayName { get; init; } = string.Empty;
        public string Display => $"#{AttachmentId} • {DisplayName} (score={Score:F3})";
    }

    /// <summary>Attachment row currently highlighted in the inspector viewport.</summary>
    /// <remarks>Execution: Setter fires on grid selection change events. Form Mode: Read-only in Find/View; editable metadata appears in Add/Update. Localization: Inspector labels rely on inline literals pending attachments resource dictionary.</remarks>
    [ObservableProperty]
    private AttachmentRowViewModel? _selectedAttachment;

    /// <summary>Command surfaced on the ribbon for staging file uploads.</summary>
    /// <remarks>Execution: Invoked asynchronously when the user taps Upload. Form Mode: Enabled exclusively in Add/Update once `HasAttachmentWorkflow` is true. Localization: Tooltip and label use inline literals pending `Ribbon_Attachments_Upload`.</remarks>
    public IAsyncRelayCommand UploadCommand { get; }

    /// <summary>Command that streams the selected attachment to a local file.</summary>
    /// <remarks>Execution: Runs when the ribbon Download action is triggered. Form Mode: Available in View/Find for read-only access; Add/Update also permitted. Localization: Uses inline caption "Download" pending ribbon resource keys.</remarks>
    public IAsyncRelayCommand DownloadCommand { get; }

    /// <summary>Command responsible for unregistering an attachment link.</summary>
    /// <remarks>Execution: Fired from the ribbon Delete action after confirmation. Form Mode: Restricted to Update mode with proper signature gating; Find/View disable this command. Localization: Inline text "Delete" currently shown until resources exist.</remarks>
    public IAsyncRelayCommand DeleteCommand { get; }

    /// <summary>Creates an embedding vector for the selected attachment (optional RAG index).</summary>
    public IAsyncRelayCommand IndexEmbeddingCommand { get; }
    public IAsyncRelayCommand FindSimilarCommand { get; }
    public IRelayCommand OpenSimilarCommand { get; }

    private bool CanIndexEmbedding()
        => !IsBusy && SelectedRecord != null;

    private async Task IndexEmbeddingAsync()
    {
        try
        {
            if (SelectedRecord == null)
            {
                return;
            }

            var row = AttachmentRows.FirstOrDefault(r => r.Model.Id.ToString() == SelectedRecord.Key);
            if (row == null)
            {
                StatusMessage = "Select an attachment to index.";
                return;
            }

            var locator = YasGMP.Common.ServiceLocator.GetService<YasGMP.Services.AttachmentEmbeddingService>();
            if (locator == null)
            {
                StatusMessage = "Embedding service not available.";
                return;
            }

            IsBusy = true;
            await locator.IndexAttachmentAsync(row.Model).ConfigureAwait(false);
            StatusMessage = $"Indexed embedding for #{row.Model.Id} ({row.Model.FileName}).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Embedding index failed: {ex.Message}";
            throw;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task FindSimilarAsync()
    {
        try
        {
            if (SelectedRecord == null)
            {
                return;
            }

            var row = AttachmentRows.FirstOrDefault(r => r.Model.Id.ToString() == SelectedRecord.Key);
            if (row == null)
            {
                StatusMessage = "Select an attachment to search similar.";
                return;
            }

            var service = YasGMP.Common.ServiceLocator.GetService<YasGMP.Services.AttachmentEmbeddingService>();
            if (service == null)
            {
                StatusMessage = "Embedding search unavailable.";
                return;
            }

            IsBusy = true;
            var matches = await service.FindSimilarAsync(row.Model.Id, 5).ConfigureAwait(false);
            if (matches.Count == 0)
            {
                StatusMessage = "No similar embeddings found. Consider indexing first.";
                SimilarAttachments.Clear();
                return;
            }

            SimilarAttachments.Clear();
            foreach (var m in matches)
            {
                var display = AttachmentRows.FirstOrDefault(a => a.Id == m.AttachmentId)?.DisplayName
                               ?? AttachmentRows.FirstOrDefault(a => a.Model.Id == m.AttachmentId)?.DisplayName
                               ?? "Attachment";
                SimilarAttachments.Add(new SimilarAttachmentItem
                {
                    AttachmentId = m.AttachmentId,
                    Score = m.Score,
                    DisplayName = display
                });
            }

            StatusMessage = $"Found {SimilarAttachments.Count} similar attachment(s).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Similar search failed: {ex.Message}";
            throw;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OpenSimilar(object? parameter)
    {
        try
        {
            if (parameter is null) return;
            int id = parameter is int i ? i : int.TryParse(parameter.ToString(), out var parsed) ? parsed : 0;
            if (id <= 0) return;

            var key = id.ToString(CultureInfo.InvariantCulture);
            var match = Records.FirstOrDefault(r => r.Key == key);
            if (match is null)
            {
                StatusMessage = $"Attachment #{id} not loaded; refresh to see it.";
                return;
            }

            SelectedRecord = match;
            StatusMessage = $"Opened similar attachment #{id}.";

            // Golden Arrow: open related module if available
            var row = AttachmentRows.FirstOrDefault(r => r.Id == id);
            if (row?.Model is Attachment a && !string.IsNullOrWhiteSpace(a.EntityType) && a.EntityId.HasValue)
            {
                var moduleKey = MapEntityTypeToModuleKey(a.EntityType!);
                if (moduleKey != null)
                {
                    var doc = _navigationService.OpenModule(moduleKey, a.EntityId.Value);
                    _navigationService.Activate(doc);
                    return;
                }
            }

            // Fallback: preview file if no related module mapping
            if (row?.Model is Attachment att)
            {
                _ = PreviewAttachmentAsync(att);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Open similar failed: {ex.Message}";
        }
    }

    private static string? MapEntityTypeToModuleKey(string entityType)
    {
        switch (entityType.Trim().ToLowerInvariant())
        {
            case "work_orders": return WorkOrdersModuleViewModel.ModuleKey;
            case "calibrations": return CalibrationModuleViewModel.ModuleKey;
            case "capa_cases": return CapaModuleViewModel.ModuleKey;
            case "users": return SecurityModuleViewModel.ModuleKey;
            case "assets": return AssetsModuleViewModel.ModuleKey;
            case "components": return ComponentsModuleViewModel.ModuleKey;
            case "suppliers": return SuppliersModuleViewModel.ModuleKey;
            case "external_servicers": return ExternalServicersModuleViewModel.ModuleKey;
            default: return null;
        }
    }

    private async Task PreviewAttachmentAsync(Attachment a)
    {
        if (!HasAttachmentWorkflow)
        {
            return;
        }

        try
        {
            IsBusy = true;
            var dir = Path.Combine(Path.GetTempPath(), "YasGMP", "preview", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, string.IsNullOrWhiteSpace(a.FileName) ? $"attachment_{a.Id}" : a.FileName);

            await using (var destination = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, 128 * 1024, useAsync: true))
            {
                var request = new AttachmentReadRequest
                {
                    Reason = $"wpf:{ModuleKey}:preview",
                    SourceHost = Environment.MachineName,
                    SourceIp = "ui:wpf"
                };

                await _attachmentService.StreamContentAsync(a.Id, destination, request).ConfigureAwait(false);
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                };
                Process.Start(psi);
                StatusMessage = $"Preview opened: {path}";
            }
            catch (Exception openEx)
            {
                StatusMessage = $"Preview saved to {path} (unable to open: {openEx.Message})";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Preview failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Loads the attachment catalog from the persistence layer.</summary>
    /// <remarks>Execution: Called by the base class when the module enters Find mode or refreshes. Form Mode: Supplies records for Find/View while Add/Update reuse the same backing list. Localization: Relays status messages using inline strings pending `Status_Attachments_Loaded`.</remarks>
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

    /// <summary>Provides design-time attachments for Blend and preview tooling.</summary>
    /// <remarks>Execution: Invoked only by the design-time branch in the constructor. Form Mode: Mimics Find mode to preview list rendering. Localization: Returns sample data with inline strings for previews.</remarks>
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

    /// <summary>Formats the status message after records are loaded.</summary>
    /// <remarks>Execution: Called by the base module scaffolding after `LoadAsync`. Form Mode: Applies to Find/View to inform ribbon state; Add/Update reuse the last status. Localization: Uses inline fallback strings until shared resource keys are wired.</remarks>
    protected override string FormatLoadedStatus(int count)
    {
        if (!string.IsNullOrWhiteSpace(_pendingStatusMessage))
        {
            var message = _pendingStatusMessage;
            _pendingStatusMessage = null;
            return message!;
        }

        return base.FormatLoadedStatus(count);
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

    /// <summary>Loads editor payloads for the selected Attachments record.</summary>
    /// <remarks>Execution: Triggered when document tabs change or shell routing targets `ModuleKey` "Attachments". Form Mode: Honors Add/Update safeguards to avoid overwriting dirty state. Localization: Inline status/error strings remain until `Status_Attachments` resources are available.</remarks>
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

    /// <summary>Adjusts command enablement and editor state when the form mode changes.</summary>
    /// <remarks>Execution: Fired by the SAP B1 style form state machine when Find/Add/View/Update transitions occur. Form Mode: Governs which controls are writable and which commands are visible. Localization: Mode change prompts use inline strings pending localization resources.</remarks>
    protected override Task OnModeChangedAsync(FormMode mode)
    {
        DiscardStagedUploads();

        if (mode != FormMode.View)
        {
            SelectedRecord = null;
            SelectedAttachment = null;
        }

        IReadOnlyList<InspectorField> inspectorFields;
        string inspectorTitle;

        if (mode == FormMode.View && SelectedAttachment is not null)
        {
            inspectorTitle = SelectedAttachment.DisplayName;
            inspectorFields = BuildInspectorFields(SelectedAttachment);
        }
        else
        {
            inspectorTitle = $"{mode} mode";
            inspectorFields = new List<InspectorField>
            {
                new("Mode", mode.ToString()),
                new("Staged Uploads", StagedUploads.Count.ToString(CultureInfo.CurrentCulture))
            };
        }

        _shellInteractionService.UpdateInspector(new InspectorContext(Title, inspectorTitle, inspectorFields));

        StatusMessage = mode switch
        {
            FormMode.Find => "Enter search text to find attachments.",
            FormMode.Add => "Stage attachments, capture a signature, then save to commit.",
            FormMode.Update => "Stage additional files or remove attachments, then save to commit.",
            _ => FormatLoadedStatus(Records.Count)
        };

        UpdateAttachmentCommands();
        return Task.CompletedTask;
    }

    /// <summary>Persists the current record and coordinates signatures, attachments, and audits.</summary>
    /// <remarks>Execution: Runs after validation when OK/Update is confirmed. Form Mode: Exclusive to Add/Update operations. Localization: Success/failure messaging remains inline pending dedicated resources.</remarks>
    protected override async Task<bool> OnSaveAsync()
    {
        if (!HasAttachmentWorkflow)
        {
            StatusMessage = "Attachment workflow unavailable.";
            return false;
        }

        var stagedItems = StagedUploads.ToList();
        if (stagedItems.Count == 0)
        {
            StatusMessage = "No staged uploads to commit.";
            return false;
        }

        ElectronicSignatureDialogResult? signatureResult;
        try
        {
            signatureResult = await _signatureDialog
                .CaptureSignatureAsync(new ElectronicSignatureContext("attachments", 0))
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Electronic signature failed: {ex.Message}";
            return false;
        }

        if (signatureResult is null)
        {
            StatusMessage = "Electronic signature cancelled. Save aborted.";
            return false;
        }

        if (signatureResult.Signature is null)
        {
            StatusMessage = "Electronic signature was not captured.";
            return false;
        }

        var successes = 0;
        var failures = 0;
        var notes = new List<string>();
        var committedIds = new List<int>();

        foreach (var staged in stagedItems)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(staged.TempPath) || !File.Exists(staged.TempPath))
                {
                    failures++;
                    notes.Add($"Missing staged content for '{staged.FileName}'.");
                    continue;
                }

                var entityType = string.IsNullOrWhiteSpace(staged.EntityType)
                    ? "attachments"
                    : staged.EntityType;

                await using var uploadStream = new FileStream(
                    staged.TempPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    128 * 1024,
                    useAsync: true);

                var request = new AttachmentUploadRequest
                {
                    FileName = staged.FileName,
                    ContentType = staged.ContentType,
                    EntityType = entityType,
                    EntityId = staged.EntityId,
                    Notes = staged.Notes,
                    Reason = $"wpf:{ModuleKey}:{Mode.ToString().ToLowerInvariant()}",
                    SourceHost = Environment.MachineName,
                    SourceIp = "ui:wpf"
                };

                var uploadResult = await _attachmentService
                    .UploadAsync(uploadStream, request)
                    .ConfigureAwait(false);

                var uploadedById = uploadResult.Attachment.UploadedById;
                if (uploadedById is null or 0)
                {
                    uploadedById = signatureResult.Signature.UserId;
                }

                var proxy = new AttachmentServiceUploadProxy(_attachmentService, uploadResult);
                await _databaseService
                    .AddAttachmentAsync(
                        staged.TempPath,
                        entityType,
                        staged.EntityId,
                        uploadedById ?? 0,
                        "ui:wpf",
                        Environment.MachineName,
                        $"wpf:{ModuleKey}",
                        request.Reason,
                        proxy)
                    .ConfigureAwait(false);

                successes++;
                committedIds.Add(uploadResult.Attachment.Id);
                notes.Add($"Committed '{staged.FileName}' (#{uploadResult.Attachment.Id}).");

                try
                {
                    var detailParts = new List<string>
                    {
                        $"file={staged.FileName}",
                        $"entity={entityType}",
                        $"entityId={staged.EntityId}",
                        $"size={staged.FileSize}"
                    };

                    if (!string.IsNullOrWhiteSpace(staged.Sha256))
                    {
                        detailParts.Add($"hash={staged.Sha256}");
                    }

                    detailParts.Add($"reason={signatureResult.ReasonDisplay ?? signatureResult.ReasonCode}");

                    await _audit
                        .LogEntityAuditAsync(
                            "attachments",
                            uploadResult.Attachment.Id,
                            "UPLOAD",
                            string.Join(", ", detailParts))
                        .ConfigureAwait(false);
                }
                catch (Exception auditEx)
                {
                    notes.Add($"Audit logging failed for '{staged.FileName}': {auditEx.Message}");
                }
            }
            catch (Exception ex)
            {
                failures++;
                notes.Add($"Failed '{staged.FileName}': {ex.Message}");
            }
            finally
            {
                StagedUploads.Remove(staged);
                CleanupStagedUpload(staged);
            }
        }

        if (successes == 0)
        {
            StatusMessage = failures > 0
                ? $"Failed to commit staged uploads. {string.Join(" ", notes)}"
                : "No staged uploads were committed.";
            return false;
        }

        var latestId = committedIds.LastOrDefault();
        SignaturePersistenceHelper.ApplyEntityMetadata(
            signatureResult,
            "attachments",
            latestId,
            metadata: null,
            fallbackSignatureHash: signatureResult.Signature.SignatureHash,
            fallbackMethod: signatureResult.Signature.Method,
            fallbackStatus: signatureResult.Signature.Status,
            fallbackNote: signatureResult.Signature.Note,
            signedAt: signatureResult.Signature.SignedAt,
            fallbackDeviceInfo: signatureResult.Signature.DeviceInfo,
            fallbackIpAddress: signatureResult.Signature.IpAddress,
            fallbackSessionId: signatureResult.Signature.SessionId);

        try
        {
            await SignaturePersistenceHelper
                .PersistIfRequiredAsync(_signatureDialog, signatureResult)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to persist electronic signature: {ex.Message}";
            return false;
        }

        var summary = $"Committed {successes} attachment(s).";
        if (failures > 0)
        {
            summary += $" {failures} failure(s).";
        }

        if (notes.Count > 0)
        {
            summary += " " + string.Join(" ", notes);
        }

        _pendingStatusMessage = summary;
        StatusMessage = summary;
        UpdateAttachmentCommands();
        return true;
    }

    /// <summary>Reverts in-flight edits and restores the last committed snapshot.</summary>
    /// <remarks>Execution: Activated when Cancel is chosen mid-edit. Form Mode: Applies to Add/Update; inert elsewhere. Localization: Cancellation prompts use inline text until localized resources exist.</remarks>
    protected override void OnCancel()
    {
        var hadStagedUploads = StagedUploads.Count;
        DiscardStagedUploads();
        var refreshTask = RefreshAsync();

        if (hadStagedUploads > 0)
        {
            var message = $"Discarded {hadStagedUploads} staged upload(s).";
            _pendingStatusMessage = message;
            refreshTask.ContinueWith(
                _ => StatusMessage = message,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.FromCurrentSynchronizationContext());
        }
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

        if (clearStagedUploads)
        {
            DiscardStagedUploads();
        }
    }

    private void OnStagedUploadsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasStagedUploads));
        if (StagedUploads.Count > 0)
        {
            MarkDirty();
        }
        else
        {
            ResetDirty();
        }

        UpdateAttachmentCommands();
    }

    private void DiscardStagedUploads()
    {
        if (StagedUploads.Count == 0)
        {
            return;
        }

        var staged = StagedUploads.ToList();
        StagedUploads.Clear();

        foreach (var upload in staged)
        {
            CleanupStagedUpload(upload);
        }

        UpdateAttachmentCommands();
    }

    private void CleanupStagedUpload(StagedAttachmentUploadViewModel? staged)
    {
        if (staged is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(staged.TempDirectory))
        {
            TryDeleteTempDirectory(staged.TempDirectory);
        }
        else if (!string.IsNullOrWhiteSpace(staged.TempPath))
        {
            TryDeleteTempDirectory(Path.GetDirectoryName(staged.TempPath));
        }

        staged.TempDirectory = null;
        staged.TempPath = null;
        staged.Sha256 = null;
        staged.FileSize = 0;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(IsBusy), StringComparison.Ordinal))
        {
            UpdateAttachmentCommands();
        }
    }

    private bool CanUpload()
        => !IsBusy && HasAttachmentWorkflow && Mode is FormMode.Add or FormMode.Update;

    private bool CanDownload()
        => !IsBusy && SelectedAttachment is not null && HasAttachmentWorkflow && Mode is not FormMode.Add;

    private bool CanDelete()
        => !IsBusy && SelectedAttachment is not null && Mode is FormMode.Update;

    private async Task UploadAsync()
    {
        if (!HasAttachmentWorkflow)
        {
            StatusMessage = "Attachment workflow unavailable.";
            return;
        }

        if (Mode is not (FormMode.Add or FormMode.Update))
        {
            StatusMessage = "Switch to Add or Update mode to stage attachments.";
            return;
        }

        var files = await _filePicker
            .PickFilesAsync(new FilePickerRequest(AllowMultiple: true, Title: "Select attachments to upload"))
            .ConfigureAwait(false);

        if (files is null || files.Count == 0)
        {
            StatusMessage = "Attachment staging cancelled.";
            return;
        }

        try
        {
            IsBusy = true;
            UpdateAttachmentCommands();

            var stagedCount = 0;
            var duplicates = 0;
            var failed = 0;
            var notes = new List<string>();

            foreach (var file in files)
            {
                var staged = CreateStagedUpload(file);

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
                        staged.FileSize = total;
                        staged.TempDirectory = tempDirectory;
                        staged.TempPath = tempPath;

                        if (sha.Hash is { Length: > 0 })
                        {
                            var hash = Convert.ToHexString(sha.Hash);
                            staged.Sha256 = hash;

                            var existing = await _attachmentService
                                .FindByHashAndSizeAsync(hash, total)
                                .ConfigureAwait(false);

                            if (existing is not null)
                            {
                                duplicates++;
                                notes.Add($"Skipped '{file.FileName}' (duplicate of #{existing.Id}).");
                                CleanupStagedUpload(staged);
                                continue;
                            }
                        }

                        await destination.FlushAsync().ConfigureAwait(false);
                    }

                    StagedUploads.Add(staged);
                    stagedCount++;
                    notes.Add($"Staged '{file.FileName}'.");
                }
                catch (Exception ex)
                {
                    failed++;
                    notes.Add($"Failed '{file.FileName}': {ex.Message}");
                    staged.TempDirectory = tempDirectory;
                    staged.TempPath = tempPath;
                    CleanupStagedUpload(staged);
                }
            }

            StatusMessage = BuildStagingStatus(stagedCount, duplicates, failed, notes);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Attachment staging failed: {ex.Message}";
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
        // Ensure CanExecute notifications are raised on the UI dispatcher to avoid
        // cross-thread violations when smoke runs steps off the UI thread.
        YasGMP.Wpf.Helpers.UiCommandHelper.NotifyManyOnUi(UploadCommand, DownloadCommand, DeleteCommand);
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

    private static string BuildStagingStatus(int staged, int duplicates, int failed, IReadOnlyList<string> notes)
    {
        var parts = new List<string>
        {
            $"Staged {staged} file(s)"
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

        if (staged > 0)
        {
            summary += " Use Save to commit staged uploads.";
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

    private readonly DatabaseService _databaseService = null!;
    private readonly IAttachmentService _attachmentService = null!;
    private readonly IFilePicker _filePicker = null!;
    private readonly IElectronicSignatureDialogService _signatureDialog = null!;
    private readonly AuditService _audit = null!;
    private readonly ICflDialogService _cflDialogService = null!;
    private readonly IShellInteractionService _shellInteractionService = null!;
    private readonly IModuleNavigationService _navigationService = null!;
    private string? _pendingStatusMessage;

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
        private string? _tempPath;
        private string? _tempDirectory;
        private long _fileSize;
        private string? _sha256;

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

        public string? TempPath
        {
            get => _tempPath;
            set => SetProperty(ref _tempPath, value);
        }

        public string? TempDirectory
        {
            get => _tempDirectory;
            set => SetProperty(ref _tempDirectory, value);
        }

        public long FileSize
        {
            get => _fileSize;
            set => SetProperty(ref _fileSize, value);
        }

        public string? Sha256
        {
            get => _sha256;
            set => SetProperty(ref _sha256, value);
        }
    }
}
