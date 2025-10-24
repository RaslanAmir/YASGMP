using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Common;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Helpers;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// <b>AttachmentViewModel</b> – GMP/Annex 11/21 CFR Part 11 ViewModel for managing attachments.
    /// Provides filtering, CRUD helpers, approvals, export logging, and complete INotifyPropertyChanged support.
    /// All properties are nullability-annotated and event signatures are compatible with <see cref="INotifyPropertyChanged"/>.
    /// </summary>
    public class AttachmentViewModel : INotifyPropertyChanged
    {
        #region === Fields & Constructor ===

        private readonly DatabaseService _dbService;
        private readonly AuthService _authService;

        private ObservableCollection<Attachment> _attachments = new();
        private ObservableCollection<Attachment> _filteredAttachments = new();
        private Attachment? _selectedAttachment;
        private string? _searchTerm;
        private string? _entityFilter;
        private string? _typeFilter;
        private bool _isBusy;
        private string? _statusMessage;

        /// <summary>
        /// Initializes a new instance of <see cref="AttachmentViewModel"/> with required services.
        /// </summary>
        /// <param name="dbService">Database abstraction for attachment operations.</param>
        /// <param name="authService">Authentication/session context provider.</param>
        /// <exception cref="ArgumentNullException">Thrown if any dependency is <c>null</c>.</exception>
        public AttachmentViewModel(DatabaseService dbService, AuthService authService)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            LoadAttachmentsCommand    = new AsyncRelayCommand(LoadAttachmentsAsync);
            AddAttachmentCommand      = new AsyncRelayCommand<string?>(AddAttachmentAsync,      _ => !IsBusy && CanManageAttachments);
            DownloadAttachmentCommand = new AsyncRelayCommand<string?>(DownloadAttachmentAsync, _ => !IsBusy && SelectedAttachment != null);
            DeleteAttachmentCommand   = new AsyncRelayCommand(DeleteAttachmentAsync,            () => !IsBusy && SelectedAttachment != null && CanManageAttachments);
            RollbackAttachmentCommand = new AsyncRelayCommand(RollbackAttachmentAsync,          () => !IsBusy && SelectedAttachment != null && CanManageAttachments);
            ExportAttachmentsCommand  = new AsyncRelayCommand(ExportAttachmentsAsync,           () => !IsBusy);
            ApproveAttachmentCommand  = new AsyncRelayCommand(ApproveAttachmentAsync,           () => !IsBusy && SelectedAttachment != null && CanManageAttachments);
            FilterChangedCommand      = new RelayCommand(FilterAttachments);

            _ = LoadAttachmentsAsync();
        }

        #endregion

        #region === Properties ===

        /// <summary>All currently loaded attachments (raw list from the database).</summary>
        public ObservableCollection<Attachment> Attachments
        {
            get => _attachments;
            set { _attachments = value ?? new ObservableCollection<Attachment>(); OnPropertyChanged(); }
        }

        /// <summary>Filtered view over <see cref="Attachments"/> for the UI.</summary>
        public ObservableCollection<Attachment> FilteredAttachments
        {
            get => _filteredAttachments;
            set { _filteredAttachments = value ?? new ObservableCollection<Attachment>(); OnPropertyChanged(); }
        }

        /// <summary>The currently selected attachment in the UI (nullable).</summary>
        public Attachment? SelectedAttachment
        {
            get => _selectedAttachment;
            set
            {
                if (_selectedAttachment == value)
                    return;

                _selectedAttachment = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedAttachmentSizeDisplay));
                OnPropertyChanged(nameof(SelectedAttachmentRetentionSummary));
                OnPropertyChanged(nameof(SelectedAttachmentHasLegalHold));
                OnPropertyChanged(nameof(SelectedAttachmentRequiresReview));
            }
        }

        /// <summary>Free-text search term (nullable). Triggers <see cref="FilterAttachments"/> on change.</summary>
        public string? SearchTerm
        {
            get => _searchTerm;
            set { _searchTerm = value; OnPropertyChanged(); FilterAttachments(); }
        }

        /// <summary>Entity/table filter (nullable). Triggers <see cref="FilterAttachments"/> on change.</summary>
        public string? EntityFilter
        {
            get => _entityFilter;
            set { _entityFilter = value; OnPropertyChanged(); FilterAttachments(); }
        }

        /// <summary>File type filter (nullable). Triggers <see cref="FilterAttachments"/> on change.</summary>
        public string? TypeFilter
        {
            get => _typeFilter;
            set { _typeFilter = value; OnPropertyChanged(); FilterAttachments(); }
        }

        /// <summary>Indicates whether a background operation is in progress.</summary>
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        /// <summary>Status message for user feedback (nullable).</summary>
        public string? StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        /// <summary>Human readable summary of the selected attachment's size.</summary>
        public string SelectedAttachmentSizeDisplay
        {
            get
            {
                var size = SelectedAttachment?.FileSize;
                return size.HasValue && size.Value > 0
                    ? FormatBytes(size.Value)
                    : "N/A";
            }
        }

        /// <summary>Summary of the latest retention metadata for the selected attachment.</summary>
        public string SelectedAttachmentRetentionSummary
        {
            get
            {
                var attachment = SelectedAttachment;
                if (attachment is null)
                    return "N/A";

                var segments = new List<string>();
                if (!string.IsNullOrWhiteSpace(attachment.RetentionPolicyName))
                    segments.Add(attachment.RetentionPolicyName!);

                if (attachment.RetainUntil.HasValue)
                {
                    segments.Add(string.Format(
                        CultureInfo.CurrentCulture,
                        "do {0}",
                        attachment.RetainUntil.Value.ToLocalTime().ToString("d", CultureInfo.CurrentCulture)));
                }

                if (attachment.RetentionLegalHold)
                    segments.Add("legal hold");

                if (attachment.RetentionReviewRequired)
                    segments.Add("review");

                if (!string.IsNullOrWhiteSpace(attachment.RetentionNotes))
                    segments.Add(attachment.RetentionNotes!);

                return segments.Count == 0 ? "N/A" : string.Join(" • ", segments);
            }
        }

        /// <summary>True when a legal hold prevents purge of the selected attachment.</summary>
        public bool SelectedAttachmentHasLegalHold => SelectedAttachment?.RetentionLegalHold ?? false;

        /// <summary>True when manual review is required before retention purge.</summary>
        public bool SelectedAttachmentRequiresReview => SelectedAttachment?.RetentionReviewRequired ?? false;

        /// <summary>Available attachment types for quick filtering.</summary>
        public string[] AvailableTypes => new[] { "pdf", "photo", "certificate", "doc", "report", "other" };

        #endregion

        #region === Commands ===

        public ICommand LoadAttachmentsCommand { get; }
        public ICommand AddAttachmentCommand { get; }
        public ICommand DownloadAttachmentCommand { get; }
        public ICommand DeleteAttachmentCommand { get; }
        public ICommand RollbackAttachmentCommand { get; }
        public ICommand ExportAttachmentsCommand { get; }
        public ICommand ApproveAttachmentCommand { get; }
        public ICommand FilterChangedCommand { get; }

        #endregion

        #region === Methods ===

        /// <summary>
        /// Loads attachments using server-side filtering (entity, type, search) and updates the filtered view.
        /// </summary>
        public async Task LoadAttachmentsAsync()
        {
            IsBusy = true;
            try
            {
                var rows = await _dbService.GetAttachmentsFilteredAsync(
                    relatedTable: string.IsNullOrWhiteSpace(EntityFilter) ? null : EntityFilter,
                    relatedId: null,
                    fileType: string.IsNullOrWhiteSpace(TypeFilter) ? null : TypeFilter,
                    search: string.IsNullOrWhiteSpace(SearchTerm) ? null : SearchTerm
                ).ConfigureAwait(false);

                var list = rows ?? new List<Attachment>();
                Attachments = new ObservableCollection<Attachment>(list);
                FilterAttachments();
                StatusMessage = $"Loaded {Attachments.Count} attachments.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading attachments: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Adds a new attachment by uploading a file located at <paramref name="filePath"/>.
        /// Signature and audit logging are handled in the <see cref="DatabaseService"/>.
        /// </summary>
        /// <param name="filePath">Absolute path to the file to be attached (nullable).</param>
        public async Task AddAttachmentAsync(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                StatusMessage = "Invalid file path.";
                return;
            }

            IsBusy = true;
            try
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser == null)
                {
                    StatusMessage = "No authenticated user.";
                    return;
                }

                var relatedTable = string.IsNullOrWhiteSpace(EntityFilter) ? "attachments" : EntityFilter!;
                var relatedId = SelectedAttachment?.EntityId ?? 0;
                var fileName = Path.GetFileName(filePath);

                await _dbService.AddAttachmentAsync(
                    relatedTable: relatedTable,
                    relatedId: relatedId,
                    fileName: fileName,
                    filePath: filePath
                ).ConfigureAwait(false);

                StatusMessage = "Attachment uploaded successfully.";
                await LoadAttachmentsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Upload failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Downloads the currently selected attachment to <paramref name="destinationPath"/>.
        /// </summary>
        /// <param name="destinationPath">Path where the file should be placed (nullable).</param>
        public async Task DownloadAttachmentAsync(string? destinationPath)
        {
            if (SelectedAttachment == null)
            {
                StatusMessage = "No attachment selected.";
                return;
            }
            if (string.IsNullOrWhiteSpace(destinationPath))
            {
                StatusMessage = "Destination path not specified.";
                return;
            }

            IsBusy = true;
            try
            {
                // Placeholder: hook up to the actual binary/document storage implementation.
                var attachmentService = ServiceLocator.GetRequiredService<IAttachmentService>();
                var currentUser = _authService.CurrentUser;

                var directory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                var request = new AttachmentReadRequest
                {
                    RequestedById = currentUser?.Id,
                    Reason = $"attachment-download:{SelectedAttachment.EntityType}",
                    SourceIp = "ui",
                    SourceHost = Environment.MachineName
                };

                AttachmentStreamResult result;
                await using (var fs = File.Create(destinationPath))
                {
                    result = await attachmentService.StreamContentAsync(SelectedAttachment.Id, fs, request).ConfigureAwait(false);
                }

                var written = FormatBytes(result.BytesWritten);
                var total = FormatBytes(result.TotalLength);
                var status = string.IsNullOrWhiteSpace(result.Attachment.Status) ? "OK" : result.Attachment.Status;
                StatusMessage = $"Downloaded {written} of {total} to {destinationPath}. Status: {status}. Audit event logged.";

                if (IsIntegrityAlarm(result.Attachment))
                {
                    await SafeNavigator.ShowAlertAsync(
                        "Integrity warning",
                        "The attachment reported a hash or integrity alarm. Please verify the source before distribution.").ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Download failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Deletes the selected attachment.
        /// </summary>
        public async Task DeleteAttachmentAsync()
        {
            if (SelectedAttachment == null)
            {
                StatusMessage = "No attachment selected.";
                return;
            }

            IsBusy = true;
            try
            {
                await _dbService.DeleteAttachmentAsync(SelectedAttachment.Id).ConfigureAwait(false);
                StatusMessage = $"Attachment '{SelectedAttachment.FileName}' deleted.";
                await LoadAttachmentsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Delete failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Rollback is not supported by the current DB service; shows informative message only.
        /// </summary>
        public async Task RollbackAttachmentAsync()
        {
            if (SelectedAttachment == null)
            {
                StatusMessage = "No attachment selected.";
                return;
            }

            IsBusy = true;
            try
            {
                await Task.Delay(50).ConfigureAwait(false);
                StatusMessage = "Rollback is not supported in this build.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Rollback failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Approves (e-signs) the selected attachment, recording the action via the DB service.
        /// </summary>
        public async Task ApproveAttachmentAsync()
        {
            if (SelectedAttachment == null)
            {
                StatusMessage = "No attachment selected.";
                return;
            }
            var currentUser = _authService.CurrentUser;
            if (currentUser == null)
            {
                StatusMessage = "No authenticated user.";
                return;
            }

            IsBusy = true;
            try
            {
                string sigHash;
                var sha256 = SelectedAttachment.Sha256;
                if (!string.IsNullOrWhiteSpace(sha256))
                {
                    try
                    {
                        var bytes = Convert.FromHexString(sha256);
                        sigHash = Convert.ToBase64String(bytes);
                    }
                    catch (FormatException)
                    {
                        sigHash = sha256;
                    }
                }
                else
                {
                    sigHash = DigitalSignatureHelper.GenerateSignatureHash(
                        $"attachment:{SelectedAttachment.Id}:{SelectedAttachment.FileName}:{DateTime.UtcNow:O}");
                }

                await _dbService.ApproveAttachmentAsync(
                    attachmentId: SelectedAttachment.Id,
                    actorUserId: currentUser.Id,
                    ip: _authService.CurrentIpAddress,
                    deviceInfo: _authService.CurrentDeviceInfo,
                    signatureHash: sigHash
                ).ConfigureAwait(false);

                StatusMessage = $"Attachment '{SelectedAttachment.FileName}' approved.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Approval failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Logs an export of the current filtered set of attachments.
        /// </summary>
        public async Task ExportAttachmentsAsync()
        {
            IsBusy = true;
            try
            {
                var format = "csv";
                var exportPath = $"/export/attachments_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{format}";
                var userId = _authService.CurrentUser?.Id ?? 0;

                var filterUsed =
                    $"search={SearchTerm};entity={EntityFilter};type={TypeFilter};count={FilteredAttachments?.Count ?? 0}";

                await _dbService.SaveExportPrintLogAsync(
                    userId: userId,
                    format: format,
                    tableName: "attachments",
                    filterUsed: filterUsed,
                    filePath: exportPath,
                    sourceIp: _authService.CurrentIpAddress,
                    note: "Attachments export").ConfigureAwait(false);

                StatusMessage = $"Attachments export logged: {exportPath}.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Applies client-side filters on <see cref="Attachments"/> into <see cref="FilteredAttachments"/>.
        /// </summary>
        public void FilterAttachments()
        {
            var filtered = Attachments.Where(a =>
                (string.IsNullOrWhiteSpace(SearchTerm) ||
                    (a.FileName?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (a.EntityType?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (a.Notes?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (string.IsNullOrWhiteSpace(EntityFilter) || string.Equals(a.EntityType, EntityFilter, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(TypeFilter) || string.Equals(a.FileType, TypeFilter, StringComparison.OrdinalIgnoreCase))
            );
            FilteredAttachments = new ObservableCollection<Attachment>(filtered);
        }

        private static string FormatBytes(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double value = bytes;
            int unit = 0;

            while (value >= 1024 && unit < units.Length - 1)
            {
                value /= 1024;
                unit++;
            }

            return string.Format(CultureInfo.CurrentCulture, "{0:0.##} {1}", value, units[unit]);
        }

        private static bool IsIntegrityAlarm(Attachment attachment)
        {
            if (attachment == null)
                return false;

            var status = attachment.Status ?? string.Empty;
            return status.Contains("integrity", StringComparison.OrdinalIgnoreCase)
                || status.Contains("hash", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns <c>true</c> if the current user has permission to manage attachments.
        /// </summary>
        public bool CanManageAttachments
        {
            get
            {
                var role = _authService.CurrentUser?.Role;
                return string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(role, "superadmin", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(role, "qa", StringComparison.OrdinalIgnoreCase);
            }
        }

        #endregion

        #region === Audit/Auxiliary ===

        /// <summary>
        /// Loads attachment audit entries. Current build has no server method; returns empty collection.
        /// </summary>
        /// <param name="attachmentId">Attachment identifier.</param>
        /// <returns>Empty collection.</returns>
        public async Task<ObservableCollection<AttachmentAuditLog>> LoadAttachmentAuditAsync(int attachmentId)
        {
            await Task.CompletedTask;
            return new ObservableCollection<AttachmentAuditLog>();
        }

        #endregion

        #region === INotifyPropertyChanged ===

        /// <summary>Raised when a property value changes.</summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Notifies listeners that a property value has changed.
        /// </summary>
        /// <param name="propName">Name of the property that changed. Filled automatically by the compiler.</param>
        protected void OnPropertyChanged([CallerMemberName] string? propName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

        #endregion
    }
}
