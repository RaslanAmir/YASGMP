using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using YasGMP.Common;
using YasGMP.Services;
using YasGMP.Services.Interfaces;

namespace YasGMP.Views.Dialogs
{
    public partial class MachineDocumentsDialog : ContentPage
    {
        private readonly TaskCompletionSource<bool> _tcs = new();
        public Task<bool> Result => _tcs.Task;

        private readonly DatabaseService _db;
        private readonly IAttachmentService _attachments;
        private readonly int _machineId;

        public ObservableCollection<DocRow> Documents { get; } = new();

        public MachineDocumentsDialog(DatabaseService db, int machineId, IAttachmentService? attachmentService = null)
        {
            InitializeComponent();
            _db = db;
            _machineId = machineId;
            _attachments = attachmentService ?? ServiceLocator.GetRequiredService<IAttachmentService>();
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
                        Documents.Add(new DocRow(_attachments, row, OnChanged));
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
                        using var fs = File.OpenRead(f.FullPath);
                        await _attachments.UploadAsync(fs, new AttachmentUploadRequest
                        {
                            FileName = Path.GetFileName(f.FullPath),
                            ContentType = f.ContentType,
                            EntityType = "Machine",
                            EntityId = _machineId,
                            UploadedById = null,
                            Notes = "Machine document"
                        }).ConfigureAwait(false);
                    }
                    await LoadAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Greška", ex.Message, "OK");
            }
        }

        private async void OnCloseClicked(object? sender, EventArgs e)
        {
            _tcs.TrySetResult(true);
            await Navigation.PopModalAsync();
        }

        public sealed class DocRow
        {
            private readonly IAttachmentService _attachments;
            public int LinkId { get; }
            public int AttachmentId { get; }
            public string FileName { get; }
            public string StoragePath { get; }
            public Command OpenCommand { get; }
            public Command RemoveCommand { get; }
            private readonly Func<Task> _onChanged;

            public DocRow(IAttachmentService attachments, AttachmentLinkWithAttachment row, Func<Task> onChanged)
            {
                _attachments = attachments;
                _onChanged = onChanged;
                LinkId = row.Link.Id;
                AttachmentId = row.Attachment.Id;
                FileName = row.Attachment.FileName;
                StoragePath = row.Attachment.FilePath;
                OpenCommand = new Command(async () => await OpenAsync());
                RemoveCommand = new Command(async () => await RemoveAsync());
            }

            private async Task OpenAsync()
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(StoragePath) && File.Exists(StoragePath))
                    {
                        await Launcher.OpenAsync(new OpenFileRequest
                        {
                            File = new ReadOnlyFile(StoragePath)
                        });
                    }
                }
                catch { }
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
        }
    }
}

