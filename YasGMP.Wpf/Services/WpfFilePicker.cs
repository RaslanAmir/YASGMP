using System;
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

            var smoke = Environment.GetEnvironmentVariable("YASGMP_SMOKE");
            var smokeAttach = Environment.GetEnvironmentVariable("YASGMP_SMOKE_ATTACH_FAKE");
            if (!string.IsNullOrWhiteSpace(smoke) || string.Equals(smokeAttach, "1", StringComparison.OrdinalIgnoreCase))
            {
                // In smoke mode, avoid opening the OS dialog. Create a deterministic temp file and return it.
                var smokeFiles = new List<PickedFile>();
                var tempDir = Path.Combine(Path.GetTempPath(), "YasGMP", "Smoke");
                Directory.CreateDirectory(tempDir);
                var tempPath = Path.Combine(tempDir, $"attach-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}.txt");
                File.WriteAllText(tempPath, "YasGMP Smoke Attachment\nGenerated: " + DateTime.UtcNow.ToString("O"));
                var info = new FileInfo(tempPath);
                smokeFiles.Add(new PickedFile(
                    info.Name,
                    "text/plain",
                    () => Task.FromResult<Stream>(File.OpenRead(tempPath)),
                    info.Exists ? info.Length : null));
                return Task.FromResult<IReadOnlyList<PickedFile>>(smokeFiles);
            }

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

