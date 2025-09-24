using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using YasGMP.Services;

namespace YasGMP.Wpf.Services
{
    /// <summary>WPF implementation of <see cref="IFilePicker"/> using <see cref="OpenFileDialog"/>.</summary>
    public sealed class WpfFilePicker : IFilePicker
    {
        public Task<IReadOnlyList<PickedFile>> PickFilesAsync(FilePickerRequest request, CancellationToken cancellationToken = default)
        {
            request ??= new FilePickerRequest();

            var dialog = new OpenFileDialog
            {
                Multiselect = request.AllowMultiple,
                Title = request.Title
            };

            if (request.FileTypes != null && request.FileTypes.TryGetValue("Windows", out var patterns))
            {
                dialog.Filter = string.Join("|", patterns);
            }

            var result = dialog.ShowDialog();
            if (result != true)
            {
                return Task.FromResult<IReadOnlyList<PickedFile>>(new List<PickedFile>());
            }

            var files = new List<PickedFile>();
            foreach (var file in dialog.FileNames)
            {
                var info = new FileInfo(file);
                files.Add(new PickedFile(
                    info.Name,
                    "application/octet-stream",
                    () => Task.FromResult<Stream>(File.OpenRead(file)),
                    info.Exists ? info.Length : null));
            }

            return Task.FromResult<IReadOnlyList<PickedFile>>(files);
        }
    }
}
