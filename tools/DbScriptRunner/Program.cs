using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using MySqlConnector;

class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            var cs = Environment.GetEnvironmentVariable("YASGMP_MYSQL_CS")
                     ?? "Server=localhost;Database=yasgmp;User=root;Password=Jasenka1;Charset=utf8mb4;Allow User Variables=true;";
            var scriptPath = args.Length > 0 ? args[0] : Path.Combine("scripts", "align_and_smoke.sql");
            if (!File.Exists(scriptPath))
            {
                Console.Error.WriteLine($"Script not found: {scriptPath}");
                return 2;
            }
            var sql = await File.ReadAllTextAsync(scriptPath, Encoding.UTF8);

            await using var conn = new MySqlConnection(cs);
            await conn.OpenAsync();

            int executed = 0;
            foreach (var stmt in SplitStatements(sql))
            {
                var s = stmt.Trim();
                if (string.IsNullOrWhiteSpace(s)) continue;
                await using var cmd = new MySqlCommand(s, conn);
                await cmd.ExecuteNonQueryAsync();
                executed++;
            }

            Console.WriteLine($"Executed {executed} statements against {conn.Database}.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
            return 1;
        }
    }

    static IEnumerable<string> SplitStatements(string script)
    {
        var reader = new StringReader(script);
        var sb = new StringBuilder();
        var delimiter = ";"; // default
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("DELIMITER", StringComparison.OrdinalIgnoreCase))
            {
                // Set new delimiter
                var parts = trimmed.Split(new[]{' ','\t'}, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                    delimiter = parts[1];
                continue;
            }

            sb.AppendLine(line);

            // Check for delimiter at the end (ignoring trailing whitespace)
            if (EndsWithDelimiter(sb, delimiter))
            {
                // Remove delimiter
                RemoveTrailingDelimiter(sb, delimiter);
                yield return sb.ToString();
                sb.Clear();
            }
        }

        if (sb.Length > 0)
            yield return sb.ToString();
    }

    static bool EndsWithDelimiter(StringBuilder sb, string delimiter)
    {
        int i = sb.Length - 1;
        while (i >= 0 && char.IsWhiteSpace(sb[i])) i--;
        if (i < 0) return false;
        if (delimiter.Length == 0) return false;
        if (i - delimiter.Length + 1 < 0) return false;
        for (int d = delimiter.Length - 1; d >= 0; d--)
        {
            if (sb[i - (delimiter.Length - 1 - d)] != delimiter[d])
                return false;
        }
        return true;
    }

    static void RemoveTrailingDelimiter(StringBuilder sb, string delimiter)
    {
        // Remove trailing whitespace and delimiter
        int i = sb.Length - 1;
        while (i >= 0 && char.IsWhiteSpace(sb[i])) i--;
        if (i < 0) { sb.Clear(); return; }
        int start = i - delimiter.Length + 1;
        if (start < 0) return;
        sb.Remove(start, delimiter.Length);
    }
}
