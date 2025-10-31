using System.Text;
using MySqlConnector;

return Run(args);

static int Run(string[] args)
{
    try
    {
        var (scriptPath, connStr) = ResolveInputs(args);
        Console.WriteLine($"[DbMigrator] Using script: {scriptPath}");
        Console.WriteLine($"[DbMigrator] Target: {MaskConnectionString(connStr)}\n");

        var statements = ParseSqlWithDelimiters(File.ReadAllText(scriptPath, Encoding.UTF8));
        Console.WriteLine($"[DbMigrator] {statements.Count} statement(s) to execute\n");

        using var conn = new MySqlConnection(connStr);
        conn.Open();
        using var tx = conn.BeginTransaction();
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandTimeout = 60;

        foreach (var sql in statements)
        {
            if (string.IsNullOrWhiteSpace(sql)) continue;
            // Skip client-only directives if any slipped through
            if (sql.StartsWith("DELIMITER", StringComparison.OrdinalIgnoreCase)) continue;
            Console.WriteLine($"[DbMigrator] >> {Truncate(sql, 120)}");
            cmd.CommandText = sql;
            cmd.Parameters.Clear();
            cmd.ExecuteNonQuery();
        }

        tx.Commit();
        Console.WriteLine("\n[DbMigrator] Migration completed successfully.");
        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[DbMigrator] ERROR: {ex.Message}\n{ex}");
        return 1;
    }
}

static (string scriptPath, string connectionString) ResolveInputs(string[] args)
{
    string script = "sql/migrations/2025-10-31_add_source_ip_session_device.sql";
    for (int i = 0; i < args.Length; i++)
    {
        if (args[i] == "--script" && i + 1 < args.Length)
        {
            script = args[i + 1];
            i++;
        }
    }
    if (!Path.IsPathRooted(script))
    {
        // Resolve relative to solution root (repository root where appsettings.json lives)
        var appsettingsFile = FindFileUpwards(Directory.GetCurrentDirectory(), "appsettings.json");
        var root = appsettingsFile != null ? Path.GetDirectoryName(appsettingsFile)! : Directory.GetCurrentDirectory();
        script = Path.GetFullPath(Path.Combine(root, script));
    }
    if (!File.Exists(script))
        throw new FileNotFoundException("SQL script not found", script);

    // Prefer env var, fall back to appsettings.json ConnectionStrings:MySqlDb
    var env = Environment.GetEnvironmentVariable("YASGMP_ConnectionString");
    if (!string.IsNullOrWhiteSpace(env))
        return (script, env);

    var appsettingsFilePath = FindFileUpwards(Directory.GetCurrentDirectory(), "appsettings.json");
    if (appsettingsFilePath == null)
        throw new InvalidOperationException("Could not locate appsettings.json to read ConnectionStrings:MySqlDb");

    var json = File.ReadAllText(appsettingsFilePath, Encoding.UTF8);
    var conn = ExtractConnectionString(json) ?? throw new InvalidOperationException("ConnectionStrings:MySqlDb not found in appsettings.json");
    // Ensure we can use @var user variables in script
    if (!conn.Contains("Allow User Variables=", StringComparison.OrdinalIgnoreCase))
    {
        conn = conn.TrimEnd(';') + ";Allow User Variables=true";
    }
    return (script, conn);
}

static string? FindFileUpwards(string startDir, string fileName)
{
    var dir = new DirectoryInfo(startDir);
    while (dir != null)
    {
        var candidate = Path.Combine(dir.FullName, fileName);
        if (File.Exists(candidate)) return candidate;
        dir = dir.Parent;
    }
    return null;
}

static string? ExtractConnectionString(string json)
{
    // ultra-lightweight extract to avoid adding JSON libs; assume well-formed and small file
    // Look for "ConnectionStrings": { ... "MySqlDb": "..." ... }
    var idx = json.IndexOf("\"ConnectionStrings\"", StringComparison.OrdinalIgnoreCase);
    if (idx < 0) return null;
    var brace = json.IndexOf('{', idx);
    if (brace < 0) return null;
    int depth = 1; int i = brace + 1;
    var sb = new StringBuilder();
    while (i < json.Length && depth > 0)
    {
        var ch = json[i++];
        sb.Append(ch);
        if (ch == '{') depth++;
        else if (ch == '}') depth--;
    }
    var block = sb.ToString();
    var key = "\"MySqlDb\"";
    var keyIdx = block.IndexOf(key, StringComparison.OrdinalIgnoreCase);
    if (keyIdx < 0) return null;
    var colon = block.IndexOf(':', keyIdx);
    if (colon < 0) return null;
    var firstQuote = block.IndexOf('"', colon + 1);
    if (firstQuote < 0) return null;
    var secondQuote = block.IndexOf('"', firstQuote + 1);
    if (secondQuote < 0) return null;
    return block.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
}

static string Truncate(string value, int max)
    => value.Length <= max ? value : value.Substring(0, max) + "...";

static string MaskConnectionString(string conn)
{
    try
    {
        var parts = conn.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            var kv = parts[i].Split('=', 2);
            if (kv.Length == 2 && (kv[0].Equals("Password", StringComparison.OrdinalIgnoreCase) || kv[0].Equals("Pwd", StringComparison.OrdinalIgnoreCase)))
            {
                parts[i] = kv[0] + "=****";
            }
        }
        return string.Join(';', parts);
    }
    catch { return conn; }
}

static List<string> ParseSqlWithDelimiters(string sql)
{
    var result = new List<string>();
    var sb = new StringBuilder();
    var currentDelimiter = ";";
    using var reader = new StringReader(sql);
    while (true)
    {
        var line = reader.ReadLine();
        if (line == null) break;
        var trimmed = line.Trim();

        if (trimmed.StartsWith("DELIMITER ", StringComparison.OrdinalIgnoreCase))
        {
            // flush pending buffer if any
            if (sb.Length > 0)
            {
                var pending = sb.ToString().Trim();
                if (pending.Length > 0) result.Add(pending);
                sb.Clear();
            }
            currentDelimiter = trimmed.Substring("DELIMITER ".Length);
            continue;
        }

        sb.AppendLine(line);

        // Check if buffer ends with delimiter on a line by itself or suffixed
        if (sb.ToString().TrimEnd().EndsWith(currentDelimiter, StringComparison.Ordinal))
        {
            var text = sb.ToString();
            // Remove the trailing delimiter
            var cut = text.LastIndexOf(currentDelimiter, StringComparison.Ordinal);
            if (cut >= 0)
            {
                text = text.Substring(0, cut);
            }
            if (!string.IsNullOrWhiteSpace(text))
                result.Add(text.Trim());
            sb.Clear();
        }
    }

    if (sb.Length > 0)
    {
        var leftover = sb.ToString().Trim();
        if (leftover.Length > 0) result.Add(leftover);
    }
    return result;
}
