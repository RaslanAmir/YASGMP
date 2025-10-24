using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

namespace YasGMP.Services
{
    /// <summary>
    /// Basic PDF text extractor using iText7; returns up to ~200k chars to cap memory.
    /// </summary>
    public sealed class PdfTextExtractor : ITextExtractor
    {
        private const int MaxChars = 200_000;

        public Task<string?> ExtractTextAsync(Stream content, string? contentType, string? fileName, CancellationToken token = default)
        {
            try
            {
                using var reader = new PdfReader(content);
                using var pdf = new PdfDocument(reader);
                var sb = new StringBuilder();

                for (int i = 1; i <= pdf.GetNumberOfPages(); i++)
                {
                    token.ThrowIfCancellationRequested();
                    var page = pdf.GetPage(i);
                    var strategy = new SimpleTextExtractionStrategy();
                    var text = iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor.GetTextFromPage(page, strategy);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        if (sb.Length + text.Length > MaxChars)
                        {
                            sb.Append(text.AsSpan(0, Math.Max(0, MaxChars - sb.Length)));
                            break;
                        }
                        sb.Append(text);
                        sb.Append('\n');
                    }
                }

                return Task.FromResult<string?>(sb.Length == 0 ? null : sb.ToString());
            }
            catch
            {
                return Task.FromResult<string?>(null);
            }
        }
    }
}

