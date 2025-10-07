using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using YasGMP.Services;

namespace YasGMP.Wpf.Services
{
    /// <summary>
    /// Wraps the WPF <see cref="OpenFileDialog"/> so MAUI <see cref="IFilePicker"/> consumers keep
    /// baseline file-open parity when running inside the desktop shell.
    /// </summary>
    /// <remarks>
    /// <para><strong>Feature parity:</strong> mirrors MAUI's multi-select behaviour and file type
    /// filtering, but does not implement folder picking or save dialogs yet. Consumers must branch or
    /// provide alternative UX for those workflows.</para>
    /// <para><strong>Known gaps:</strong> MIME types cannot be inferred from Windows filters and default
    /// to <c>application/octet-stream</c>. Folder prompts, file writing, and drag/drop will require
    /// future shell services.</para>
    /// <para><strong>Localization:</strong> callers own dialog titles and filter captions; they should be
    /// sourced from the shared localization dictionaries so WPF and MAUI stay aligned.</para>
    /// </remarks>
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
