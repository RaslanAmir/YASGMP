using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace YasGMP.Services
{
    /// <summary>
    /// Cross-platform abstraction for picking files from the local device.
    /// </summary>
    public interface IFilePicker
    {
        Task<IReadOnlyList<PickedFile>> PickFilesAsync(FilePickerRequest request, CancellationToken cancellationToken = default);
    }

    /// <summary>Parameters that control the file picking UI.</summary>
    public sealed record FilePickerRequest(bool AllowMultiple = false, IReadOnlyDictionary<string, string[]>? FileTypes = null, string? Title = null);

    /// <summary>Represents a user-picked file.</summary>
    public sealed record PickedFile(string FileName, string ContentType, Func<Task<Stream>> OpenReadAsync, long? FileSize = null);
}

