using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace YasGMP.Helpers
{
    public static class CsvExportHelper
    {
        public static string EnsureExportDirectory()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "YASGMP", "exports");
            Directory.CreateDirectory(dir);
            return dir;
        }

        public static string WriteCsv<T>(IEnumerable<T> rows, string filePrefix, IEnumerable<(string Header, Func<T, object?> Selector)> columns)
        {
            rows ??= Enumerable.Empty<T>();
            var dir = EnsureExportDirectory();
            var path = Path.Combine(dir, $"{filePrefix}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");

            var sb = new StringBuilder();
            sb.AppendLine(string.Join(';', columns.Select(c => Escape(c.Header))));
            foreach (var row in rows)
            {
                var parts = columns.Select(c => Escape(ObjectToString(c.Selector(row))));
                sb.AppendLine(string.Join(';', parts));
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            return path;
        }

        private static string ObjectToString(object? o)
        {
            return o switch
            {
                null => string.Empty,
                DateTime dt => dt.ToString("O"),
                DateTimeOffset dto => dto.ToString("O"),
                _ => o.ToString() ?? string.Empty
            };
        }

        private static string Escape(string s)
        {
            if (s.IndexOfAny(new[] { ';', '"', '\n', '\r' }) >= 0)
            {
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            }
            return s;
        }
    }
}

