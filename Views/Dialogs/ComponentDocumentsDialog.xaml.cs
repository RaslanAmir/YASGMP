using System;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using YasGMP.Services;

namespace YasGMP.Views.Dialogs
{
    public partial class ComponentDocumentsDialog : ContentPage
    {
        private readonly TaskCompletionSource<bool> _tcs = new();
        public Task<bool> Result => _tcs.Task;

        private readonly DatabaseService _db;
        private readonly int _componentId;

        public ObservableCollection<DocRow> Documents { get; } = new();

        public ComponentDocumentsDialog(DatabaseService db, int componentId)
        {
            InitializeComponent();
            _db = db; _componentId = componentId;
            DocsList.ItemsSource = Documents;
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                var dt = await _db.GetDocumentsForEntityAsync("Component", _componentId).ConfigureAwait(false);
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Documents.Clear();
                    foreach (DataRow r in dt.Rows)
                    {
                        var id = r.Table.Columns.Contains("id") && r["id"] != DBNull.Value ? Convert.ToInt32(r["id"]) : 0;
                        var name = r["file_name"]?.ToString() ?? string.Empty;
                        var path = r["storage_path"]?.ToString() ?? string.Empty;
                        Documents.Add(new DocRow(_db, _componentId, id, name, path, OnChanged));
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
                    var docs = new DocumentService(_db);
                    foreach (var f in files)
                    {
                        using var fs = File.OpenRead(f.FullPath);
                        await docs.SaveAsync(fs, Path.GetFileName(f.FullPath), null, "Component", _componentId).ConfigureAwait(false);
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
            private readonly DatabaseService _db;
            private readonly int _componentId;
            public int DocumentId { get; }
            public string FileName { get; }
            public string StoragePath { get; }
            public Command OpenCommand { get; }
            public Command RemoveCommand { get; }
            private readonly Func<Task> _onChanged;

            public DocRow(DatabaseService db, int componentId, int docId, string name, string path, Func<Task> onChanged)
            {
                _db = db; _componentId = componentId; DocumentId = docId; FileName = name; StoragePath = path; _onChanged = onChanged;
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
                    await _db.DeleteDocumentLinkAsync("Component", _componentId, DocumentId).ConfigureAwait(false);
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
