using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace YasGMP.Services
{
    /// <summary>
    /// Abstraction for extracting plain text from binary content, e.g. PDFs.
    /// </summary>
    public interface ITextExtractor
    {
        Task<string?> ExtractTextAsync(Stream content, string? contentType, string? fileName, CancellationToken token = default);
    }
}

