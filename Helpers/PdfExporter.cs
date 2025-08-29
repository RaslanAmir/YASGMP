using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace YasGMP.Helpers
{
    /// <summary>
    /// Minimal PDF (1.4) text exporter with no external libraries.
    /// Generates a single-page PDF with monospaced text lines.
    /// </summary>
    public static class PdfExporter
    {
        /// <summary>
        /// Writes a simple one-page PDF with a title and multiple text lines.
        /// </summary>
        /// <param name="path">Destination file path (.pdf).</param>
        /// <param name="title">Document title (metadata & header).</param>
        /// <param name="lines">Lines of text to draw (monospaced, left aligned).</param>
        public static void WriteSimpleTextPdf(string path, string title, IList<string> lines)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            // Basic page size: A4 portrait in points (72 dpi)
            const int pageWidth = 595;
            const int pageHeight = 842;
            const int marginLeft = 40;
            const int marginTop = 40;
            const int lineHeight = 14;
            const int startY = pageHeight - marginTop;

            var objects = new List<string>();
            var xref = new List<long>();

            // 1. Catalog
            objects.Add("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

            // 2. Pages
            objects.Add("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");

            // 3. Page
            objects.Add($"3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {pageWidth} {pageHeight}] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>\nendobj\n");

            // 4. Contents stream (built later with actual text)
            var content = new StringBuilder();
            content.Append("BT\n/F1 10 Tf\n");

            // Title
            content.AppendFormat(CultureInfo.InvariantCulture, "{0} {1} {2} {3}\n",
                "1 0 0 1", marginLeft, startY, "Tm"); // set text matrix
            content.Append($"({PdfEsc(title)}) Tj\n");

            int y = startY - (lineHeight * 2);
            foreach (var line in lines)
            {
                if (y < marginTop + lineHeight) break; // single-page limiter
                content.AppendFormat(CultureInfo.InvariantCulture, "{0} {1} {2} {3}\n",
                    "1 0 0 1", marginLeft, y, "Tm");
                content.Append($"({PdfEsc(line)}) Tj\n");
                y -= lineHeight;
            }

            content.Append("ET\n");
            var contentBytes = Encoding.ASCII.GetBytes(content.ToString());
            objects.Add($"4 0 obj\n<< /Length {contentBytes.Length} >>\nstream\n{content}\nendstream\nendobj\n");

            // 5. Font
            objects.Add("5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>\nendobj\n");

            // 6. Info
            var now = DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            objects.Add($"6 0 obj\n<< /Title ({PdfEsc(title)}) /Creator (YasGMP) /Producer (YasGMP PdfExporter) /CreationDate (D:{now}) >>\nendobj\n");

            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            using var bw = new BinaryWriter(fs, Encoding.ASCII);

            // Header
            bw.Write(Encoding.ASCII.GetBytes("%PDF-1.4\n"));

            // Objects with xref positions
            foreach (var obj in objects)
            {
                xref.Add(fs.Position);
                bw.Write(Encoding.ASCII.GetBytes(obj));
            }

            // XRef
            long xrefPos = fs.Position;
            bw.Write(Encoding.ASCII.GetBytes($"xref\n0 {objects.Count + 1}\n"));
            bw.Write(Encoding.ASCII.GetBytes("0000000000 65535 f \n"));
            foreach (var pos in xref)
            {
                bw.Write(Encoding.ASCII.GetBytes($"{pos.ToString("D10", CultureInfo.InvariantCulture)} 00000 n \n"));
            }

            // Trailer
            bw.Write(Encoding.ASCII.GetBytes($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R /Info 6 0 R >>\nstartxref\n{xrefPos}\n%%EOF"));
        }

        private static string PdfEsc(string s)
        {
            if (s == null) return string.Empty;
            return s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)")
                    .Replace("\r", " ").Replace("\n", " ");
        }
    }
}
