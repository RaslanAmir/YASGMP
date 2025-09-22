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
    /// <summary>
    /// Modal dialog that lists and manages documents linked to a component.
    /// </summary>
    public partial class ComponentDocumentsDialog : ContentPage
    {
        private readonly TaskCompletionSource<bool> _tcs = new();
        public Task<bool> Result => _tcs.Task;

        private readonly DatabaseService _db;
        private readonly IAttachmentService _attachments;
        private readonly int _componentId;

        public ObservableCollection<DocRow> Documents { get; } = new();

        public ComponentDocumentsDialog(DatabaseService db, int componentId, IAttachmentService? attachmentService = null)
        {
            InitializeComponent();
            _db = db;
            _componentId = componentId;
            _attachments = attachmentService ?? ServiceLocator.GetRequiredService<IAttachmentService>();
            DocsList.ItemsSource = Documents;
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                var rows = await _attachments.GetLinksForEntityAsync("Component", _componentId).ConfigureAwait(false);
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

        private async Task OnChanged() => await LoadAsync().ConfigureAwait(false);

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
                            EntityType = "Component",
                            EntityId = _componentId,
                            UploadedById = null,
                            Notes = "Component document",
                            Reason = "component-doc-upload",
                            SourceIp = "ui",
                            SourceHost = Environment.MachineName
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
                OpenCommand = new Command(async () => await OpenAsync());
                RemoveCommand = new Command(async () => await RemoveAsync());
            }

            private async Task OpenAsync()
            {
                try
                {
                    var cacheDir = FileSystem.CacheDirectory;
                    Directory.CreateDirectory(cacheDir);
                    var tempPath = Path.Combine(cacheDir, $"attachment_{AttachmentId}_{Guid.NewGuid():N}_{FileName}");

                    await using (var fs = File.Create(tempPath))
                    {
                        var request = new AttachmentReadRequest
                        {
                            Reason = "component-doc-open",
                            SourceHost = Environment.MachineName,
                            SourceIp = null,
                            RequestedById = null
                        };
                        await _attachments.StreamContentAsync(AttachmentId, fs, request).ConfigureAwait(false);
                    }

                    await Launcher.OpenAsync(new OpenFileRequest
                    {
                        File = new ReadOnlyFile(tempPath)
                    });
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
