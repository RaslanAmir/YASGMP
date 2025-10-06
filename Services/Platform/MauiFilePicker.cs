using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using YasGMP.Services;

namespace YasGMP.Services.Platform
{
    /// <summary>MAUI wrapper for the built-in <see cref="FilePicker"/>.</summary>
    public sealed class MauiFilePicker : IFilePicker
    {
        public async Task<IReadOnlyList<PickedFile>> PickFilesAsync(FilePickerRequest request, CancellationToken cancellationToken = default)
        {
            request ??= new FilePickerRequest();

            var options = new PickOptions
            {
                PickerTitle = request.Title
            };

            if (request.FileTypes != null)
            {
                var fileType = ConvertFileTypes(request.FileTypes);
                if (fileType != null)
                    options.FileTypes = fileType;
            }

            if (request.AllowMultiple)
            {
                var results = await FilePicker.Default.PickMultipleAsync(options).ConfigureAwait(false);
                return results?.Select(ToPickedFile).ToList() ?? new List<PickedFile>();
            }
            else
            {
                var result = await FilePicker.Default.PickAsync(options).ConfigureAwait(false);
                if (result == null)
                    return Array.Empty<PickedFile>();

                return new[] { ToPickedFile(result) };
            }
        }

        private static PickedFile ToPickedFile(FileResult result)
        {
            return new PickedFile(
                result.FileName,
                result.ContentType ?? "application/octet-stream",
                () => result.OpenReadAsync(),
                (result.FullPath != null && System.IO.File.Exists(result.FullPath) ? new System.IO.FileInfo(result.FullPath).Length : (long?)null));
        }

        private static FilePickerFileType? ConvertFileTypes(IReadOnlyDictionary<string, string[]> fileTypes)
        {
            if (fileTypes.Count == 0)
                return null;

            var map = new Dictionary<DevicePlatform, IEnumerable<string>>();

            foreach (var pair in fileTypes)
            {
                if (Enum.TryParse<DevicePlatform>(pair.Key, true, out var platform))
                {
                    map[platform] = pair.Value;
                }
            }

            return map.Count == 0 ? null : new FilePickerFileType(map);
        }
    }
}

