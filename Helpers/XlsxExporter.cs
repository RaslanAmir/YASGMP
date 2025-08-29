using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace YasGMP.Helpers
{
    /// <summary>
    /// Minimal OpenXML .xlsx writer with no external dependencies.
    /// Produces a single-sheet workbook with string cells.
    /// </summary>
    public static class XlsxExporter
    {
        /// <summary>
        /// Writes a single-sheet .xlsx file.
        /// </summary>
        /// <param name="path">Destination file path (.xlsx).</param>
        /// <param name="sheetName">Worksheet name.</param>
        /// <param name="header">Header row values (text).</param>
        /// <param name="rows">Data rows (each row is an array of text cells).</param>
        public static void WriteSingleSheet(string path, string sheetName, string[] header, List<string[]> rows)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrWhiteSpace(sheetName)) sheetName = "Sheet1";
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            using var zip = new ZipArchive(fs, ZipArchiveMode.Create);

            // [Content_Types].xml
            AddEntry(zip, "[Content_Types].xml",
                @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">
  <Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>
  <Default Extension=""xml"" ContentType=""application/xml""/>
  <Override PartName=""/xl/workbook.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml""/>
  <Override PartName=""/xl/worksheets/sheet1.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml""/>
  <Override PartName=""/xl/sharedStrings.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml""/>
</Types>");

            // _rels/.rels
            AddEntry(zip, "_rels/.rels",
                @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""xl/workbook.xml""/>
</Relationships>");

            // xl/_rels/workbook.xml.rels
            AddEntry(zip, "xl/_rels/workbook.xml.rels",
                @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"" Target=""worksheets/sheet1.xml""/>
  <Relationship Id=""rId2"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings"" Target=""sharedStrings.xml""/>
</Relationships>");

            // xl/workbook.xml
            AddEntry(zip, "xl/workbook.xml",
                $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main""
          xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">
  <sheets>
    <sheet name=""{XmlEsc(sheetName)}"" sheetId=""1"" r:id=""rId1""/>
  </sheets>
</workbook>");

            // shared strings
            var strings = new List<string[]>();
            var sst = BuildSharedStrings(header, rows, out strings);

            AddEntry(zip, "xl/sharedStrings.xml", sst);

            // sheet1
            var sheetXml = BuildSheetXml(strings);
            AddEntry(zip, "xl/worksheets/sheet1.xml", sheetXml);
        }

        private static void AddEntry(ZipArchive zip, string name, string xml)
        {
            var e = zip.CreateEntry(name, CompressionLevel.Optimal);
            using var s = e.Open();
            var buf = Encoding.UTF8.GetBytes(xml);
            s.Write(buf, 0, buf.Length);
        }

        private static string BuildSharedStrings(string[] header, List<string[]> rows, out List<string[]> outRows)
        {
            var all = new List<string[]>();
            all.Add(header);
            all.AddRange(rows);
            outRows = all;

            var sb = new StringBuilder();
            int count = 0, unique = 0;
            var dict = new Dictionary<string, int>(StringComparer.Ordinal);
            var flat = new List<string>();

            foreach (var row in all)
            {
                foreach (var cell in row)
                {
                    string v = cell ?? string.Empty;
                    count++;
                    if (!dict.ContainsKey(v))
                    {
                        dict[v] = unique++;
                        flat.Add(v);
                    }
                }
            }

            sb.Append(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
            sb.Append($@"<sst xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" count=""{count}"" uniqueCount=""{unique}"">");
            foreach (var s in flat)
                sb.Append($@"<si><t>{XmlEsc(s)}</t></si>");
            sb.Append("</sst>");
            return sb.ToString();
        }

        private static string BuildSheetXml(List<string[]> rows)
        {
            var sb = new StringBuilder();
            sb.Append(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
            sb.Append(@"<worksheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">");
            sb.Append("<sheetData>");

            // Build rows: r=1..N; cells c=@SST index
            int r = 1;
            int sstIndex = 0; // since we emit sharedStrings in flat order, we map by incremental indices
            foreach (var row in rows)
            {
                sb.Append($@"<row r=""{r}"">");
                for (int c = 0; c < row.Length; c++)
                {
                    var cellRef = ColName(c + 1) + r.ToString();
                    sb.Append($@"<c r=""{cellRef}"" t=""s""><v>{sstIndex}</v></c>");
                    sstIndex++;
                }
                sb.Append("</row>");
                r++;
            }

            sb.Append("</sheetData>");
            sb.Append("</worksheet>");
            return sb.ToString();
        }

        private static string ColName(int index)
        {
            // 1->A, 26->Z, 27->AA...
            var sb = new StringBuilder();
            while (index > 0)
            {
                index--;
                sb.Insert(0, (char)('A' + (index % 26)));
                index /= 26;
            }
            return sb.ToString();
        }

        private static string XmlEsc(string s) =>
            s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
    }
}
