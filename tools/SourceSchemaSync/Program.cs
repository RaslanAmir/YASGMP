using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MySqlConnector;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("YASGMP_MYSQL_CS") ?? "Server=localhost;Database=yasgmp;User=root;Password=Jasenka1;Charset=utf8mb4;Allow User Variables=true;AllowPublicKeyRetrieval=True;SslMode=None;";
        var repoRoot = Directory.GetCurrentDirectory();
        var modelsDir = Path.Combine(repoRoot, "Models");
        var outFile = args.FirstOrDefault() ?? Path.Combine(repoRoot, "db execute", "03_schema_sync.sql");

        if (!Directory.Exists(modelsDir))
        {
            Console.Error.WriteLine($"Models directory not found: {modelsDir}");
            return 2;
        }

        await using var conn = new MySqlConnection(cs);
        await conn.OpenAsync();

        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var columns = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        await using (var cmd = new MySqlCommand("SELECT table_name, column_name FROM information_schema.columns WHERE table_schema = DATABASE()", conn))
        await using (var r = await cmd.ExecuteReaderAsync())
        {
            while (await r.ReadAsync())
            {
                var t = r.GetString(0);
                var c = r.GetString(1);
                tables.Add(t);
                if (!columns.TryGetValue(t, out var set))
                    columns[t] = set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                set.Add(c);
            }
        }

        var addSql = new List<string>();

        foreach (var file in Directory.GetFiles(modelsDir, "*.cs", SearchOption.AllDirectories))
        {
            var lines = await File.ReadAllLinesAsync(file);
            string? pendingTable = null;
            string? currentClass = null;
            string? currentTable = null;
            bool notMappedNext = false;
            List<string> pendingAttrs = new();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (line.StartsWith("[Table(", StringComparison.OrdinalIgnoreCase))
                {
                    var m = Regex.Match(line, @"Table\(""(?<t>[^""]+)""\)");
                    if (m.Success) pendingTable = m.Groups["t"].Value;
                    continue;
                }

                if (line.StartsWith("public class ") || line.StartsWith("public sealed class "))
                {
                    var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    var idx = Array.IndexOf(parts, "class");
                    if (idx >= 0 && idx + 1 < parts.Length)
                        currentClass = parts[idx + 1].Trim('{');

                    currentTable = pendingTable;
                    if (string.IsNullOrWhiteSpace(currentTable))
                    {
                        // guess by snake/plural
                        var snake = ToSnake(currentClass ?? "");
                        var guess = new[] { snake, snake + "s", snake + "es" };
                        currentTable = guess.FirstOrDefault(t => tables.Contains(t));
                    }
                    pendingTable = null;
                    notMappedNext = false;
                    pendingAttrs.Clear();
                    continue;
                }

                if (line.StartsWith("["))
                {
                    pendingAttrs.Add(line);
                    if (Regex.IsMatch(line, "\\[\\s*NotMapped", RegexOptions.IgnoreCase))
                        notMappedNext = true;
                    continue;
                }

                if (line.StartsWith("public ") && line.Contains("{") && line.Contains("}"))
                {
                    if (string.IsNullOrWhiteSpace(currentTable) || !tables.Contains(currentTable))
                    { pendingAttrs.Clear(); notMappedNext = false; continue; }

                    if (notMappedNext)
                    { pendingAttrs.Clear(); notMappedNext = false; continue; }

                    // Parse property: public <type> <Name> { get; set; }
                    var propParts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (propParts.Length < 3) { pendingAttrs.Clear(); continue; }
                    var propType = propParts[1];
                    var propName = propParts[2];

                    // Attributes
                    string? colName = null;
                    var attrJoined = string.Join(" ", pendingAttrs);
                    var mCol = Regex.Match(attrJoined, "Column\\(\\s*\\\"(?<c>[^\\\"]+)\\\"", RegexOptions.IgnoreCase);
                    if (mCol.Success) colName = mCol.Groups["c"].Value;
                    colName ??= ToSnake(propName);

                    if (!columns.TryGetValue(currentTable, out var tableCols))
                    { pendingAttrs.Clear(); continue; }

                    if (!tableCols.Contains(colName))
                    {
                        var sqlType = GuessSqlType(propType, attrJoined);
                        addSql.Add($"ALTER TABLE `{currentTable}` ADD COLUMN `{colName}` {sqlType} NULL;");
                        tableCols.Add(colName); // avoid duplicates
                    }

                    pendingAttrs.Clear();
                    notMappedNext = false;
                }
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outFile)!);
        if (addSql.Count == 0)
        {
            await File.WriteAllTextAsync(outFile, "-- No missing columns detected.\n");
            Console.WriteLine($"No changes. Wrote {outFile}");
            return 0;
        }

        await File.WriteAllLinesAsync(outFile, addSql);
        Console.WriteLine($"Wrote {addSql.Count} ALTERs to {outFile}");
        return 0;
    }

    static string ToSnake(string name)
        => Regex.Replace(name, "([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant();

    static string GuessSqlType(string propType, string attrs)
    {
        var typeAttr = Regex.Match(attrs, "Column\\([^\\)]*TypeName\\s*=\\s*\\\"(?<tn>[^\\\"]+)\\\"", RegexOptions.IgnoreCase);
        if (typeAttr.Success) return typeAttr.Groups["tn"].Value;

        var maxLen = 0;
        var ml = Regex.Match(attrs, "MaxLength\\s*\\(\\s*(?<n>\\d+)\\s*\\)");
        if (ml.Success && int.TryParse(ml.Groups["n"].Value, out var n)) maxLen = n;

        var t = propType.TrimEnd('?');
        return t switch
        {
            "int" or "Int32" or "short" or "Int16" => "int",
            "long" or "Int64" => "bigint",
            "bool" or "Boolean" => "tinyint(1)",
            "DateTime" => "datetime",
            "decimal" or "Decimal" or "double" or "Double" or "float" or "Single" => "decimal(10,2)",
            _ => maxLen > 0 ? $"varchar({Math.Clamp(maxLen,1,65535)})" : "varchar(255)",
        };
    }
}
