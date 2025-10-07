using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Common;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Models;

namespace YasGMP.Views.Dialogs
{
    /// <summary>
    /// Dialog listing documents linked to a machine with download actions.
    /// </summary>
    public partial class MachineDocumentsDialog : ContentPage
    {
        private readonly TaskCompletionSource<bool> _tcs = new();
        /// <summary>
        /// Gets or sets the result.
        /// </summary>
        public Task<bool> Result => _tcs.Task;

        private readonly DatabaseService _db;
        private readonly IAttachmentService _attachments;
        private readonly AuthService _auth;
        private readonly int _machineId;
        /// <summary>
        /// Gets or sets the documents.
        /// </summary>

        public ObservableCollection<DocRow> Documents { get; } = new();
        /// <summary>
        /// Initializes a new instance of the MachineDocumentsDialog class.
        /// </summary>

        public MachineDocumentsDialog(DatabaseService db, int machineId, IAttachmentService? attachmentService = null, AuthService? authService = null)
        {
            InitializeComponent();
            _db = db;
            _machineId = machineId;
            _attachments = attachmentService ?? ServiceLocator.GetRequiredService<IAttachmentService>();
            _auth = authService ?? ServiceLocator.GetRequiredService<AuthService>();
            DocsList.ItemsSource = Documents;
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                var rows = await _attachments.GetLinksForEntityAsync("Machine", _machineId).ConfigureAwait(false);
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Documents.Clear();
                    foreach (var row in rows)
                    {
                        Documents.Add(new DocRow(_attachments, _auth, row, OnChanged));
                    }
                });
            }
            catch (Exception ex)
            {
                await DisplayAlert("Greška", ex.Message, "OK");
            }
        }

        private async Task OnChanged()
        {
            await LoadAsync().ConfigureAwait(false);
        }

        private async void OnAddClicked(object? sender, EventArgs e)
        {
            try
            {
                var files = await FilePicker.PickMultipleAsync();
                if (files != null)
                {
                    foreach (var f in files)
                    {
                        await using var stream = await f.OpenReadAsync().ConfigureAwait(false);
                        var upload = await _attachments.UploadAsync(stream, new AttachmentUploadRequest
                        {
                            FileName = f.FileName,
                            ContentType = f.ContentType,
                            EntityType = "Machine",
                            EntityId = _machineId,
                            UploadedById = _auth.CurrentUser?.Id,
                            Notes = "Machine document",
                            Reason = "machine-doc-upload",
                            SourceIp = _auth.CurrentIpAddress,
                            SourceHost = _auth.CurrentDeviceInfo
                        }).ConfigureAwait(false);

                        if (IsIntegrityAlarm(upload.Attachment))
                        {
                            await MainThread.InvokeOnMainThreadAsync(async () =>
                            {
                                await DisplayAlert(
                                    "Integrity warning",
                                    $"Attachment '{upload.Attachment.FileName}' reported an integrity alarm after upload.",
                                    "OK");
                            }).ConfigureAwait(false);
                        }
                    }
                    await LoadAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Greška", ex.Message, "OK");
                });
            }
        }

        private async void OnCloseClicked(object? sender, EventArgs e)
        {
            _tcs.TrySetResult(true);
            await Navigation.PopModalAsync();
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
        /// Represents the Doc Row.
        /// </summary>

        public sealed class DocRow
        {
            private readonly IAttachmentService _attachments;
            private readonly AuthService _auth;
            private readonly Func<Task> _onChanged;
            /// <summary>
            /// Gets or sets the link id.
            /// </summary>

            public int LinkId { get; }
            /// <summary>
            /// Gets or sets the attachment id.
            /// </summary>
            public int AttachmentId { get; }
            /// <summary>
            /// Gets or sets the file name.
            /// </summary>
            public string FileName { get; }
            /// <summary>
            /// Gets or sets the size display.
            /// </summary>
            public string SizeDisplay { get; }
            /// <summary>
            /// Gets or sets the retention summary.
            /// </summary>
            public string RetentionSummary { get; }
            /// <summary>
            /// Gets or sets the has legal hold.
            /// </summary>
            public bool HasLegalHold { get; }
            /// <summary>
            /// Gets or sets the open command.
            /// </summary>
            public IAsyncRelayCommand OpenCommand { get; }
            /// <summary>
            /// Gets or sets the remove command.
            /// </summary>
            public IAsyncRelayCommand RemoveCommand { get; }
            /// <summary>
            /// Initializes a new instance of the DocRow class.
            /// </summary>

            public DocRow(IAttachmentService attachments, AuthService auth, AttachmentLinkWithAttachment row, Func<Task> onChanged)
            {
                _attachments = attachments;
                _auth = auth;
                _onChanged = onChanged;
                LinkId = row.Link.Id;
                AttachmentId = row.Attachment.Id;
                FileName = row.Attachment.FileName;
                SizeDisplay = FormatBytes(row.Attachment.FileSize);
                RetentionSummary = BuildRetentionSummary(row.Attachment);
                HasLegalHold = row.Attachment.RetentionLegalHold;

                OpenCommand = new AsyncRelayCommand(OpenAsync);
                RemoveCommand = new AsyncRelayCommand(RemoveAsync);
            }

            private async Task OpenAsync()
            {
                string? tempPath = null;
                try
                {
                    var cacheDir = FileSystem.CacheDirectory;
                    Directory.CreateDirectory(cacheDir);

                    var safeName = Path.GetFileName(string.IsNullOrWhiteSpace(FileName)
                        ? $"attachment_{AttachmentId}.dat"
                        : FileName);
                    tempPath = Path.Combine(cacheDir, $"attachment_{AttachmentId}_{Guid.NewGuid():N}_{safeName}");

                    AttachmentStreamResult result;
                    await using (var fs = File.Create(tempPath))
                    {
                        var request = new AttachmentReadRequest
                        {
                            Reason = "machine-doc-open",
                            SourceHost = _auth.CurrentDeviceInfo,
                            SourceIp = _auth.CurrentIpAddress,
                            RequestedById = _auth.CurrentUser?.Id
                        };
                        result = await _attachments.StreamContentAsync(AttachmentId, fs, request).ConfigureAwait(false);
                    }

                    var auditMessage = $"Streamed {FormatBytes(result.BytesWritten)} of {FormatBytes(result.TotalLength)}. Status: {result.Attachment.Status ?? "OK"}.";
                    await SafeNavigator.ShowAlertAsync("Audit", auditMessage).ConfigureAwait(false);

                    if (IsIntegrityAlarm(result.Attachment))
                    {
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            var page = Application.Current?.MainPage;
                            if (page != null)
                                await page.DisplayAlert("Integrity warning", $"Attachment '{FileName}' reported a hash mismatch during streaming.", "OK").ConfigureAwait(false);
                        }).ConfigureAwait(false);
                    }

                    await Launcher.OpenAsync(new OpenFileRequest
                    {
                        File = new ReadOnlyFile(tempPath)
                    }).ConfigureAwait(false);

                    string pathToDelete = tempPath;
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(TimeSpan.FromMinutes(5)).ConfigureAwait(false);
                        try
                        {
                            if (File.Exists(pathToDelete))
                                File.Delete(pathToDelete);
                        }
                        catch
                        {
                            // Best-effort cleanup; swallow exceptions.
                        }
                    });
                }
                catch (Exception ex)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        var page = Application.Current?.MainPage;
                        if (page != null)
                            await page.DisplayAlert("Greška", ex.Message, "OK").ConfigureAwait(false);
                    }).ConfigureAwait(false);

                    if (!string.IsNullOrWhiteSpace(tempPath))
                    {
                        try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
                    }
                }
            }

            private async Task RemoveAsync()
            {
                try
                {
                    await _attachments.RemoveLinkAsync(LinkId).ConfigureAwait(false);
                    await _onChanged().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await Application.Current?.MainPage?.DisplayAlert("Greška", ex.Message, "OK");
                }
            }

            private static string BuildRetentionSummary(Attachment attachment)
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(attachment.RetentionPolicyName))
                    parts.Add(attachment.RetentionPolicyName!);
                if (attachment.RetainUntil.HasValue)
                {
                    parts.Add(string.Format(
                        CultureInfo.CurrentCulture,
                        "do {0}",
                        attachment.RetainUntil.Value.ToLocalTime().ToString("d", CultureInfo.CurrentCulture)));
                }
                if (attachment.RetentionLegalHold)
                    parts.Add("legal hold");
                if (attachment.RetentionReviewRequired)
                    parts.Add("review");
                if (!string.IsNullOrWhiteSpace(attachment.RetentionNotes))
                    parts.Add(attachment.RetentionNotes!);

                return parts.Count == 0 ? "N/A" : string.Join(" • ", parts);
            }

            private static string FormatBytes(long? bytes)
            {
                if (!bytes.HasValue || bytes.Value <= 0)
                    return "N/A";

                string[] units = { "B", "KB", "MB", "GB", "TB" };
                double value = bytes.Value;
                int unit = 0;
                while (value >= 1024 && unit < units.Length - 1)
                {
                    value /= 1024;
                    unit++;
                }

                return string.Format(CultureInfo.CurrentCulture, "{0:0.##} {1}", value, units[unit]);
            }
        }
    }
}

