using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MySqlConnector;

static string? ResolveConnectionString()
{
    // Priority: env var MYSQL_CS, then appsettings.json keys
    var env = Environment.GetEnvironmentVariable("MYSQL_CS");
    if (!string.IsNullOrWhiteSpace(env)) return env;

    var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "appsettings.json");
    if (!File.Exists(appSettingsPath)) appSettingsPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"));
    if (!File.Exists(appSettingsPath)) return null;
    var json = JsonDocument.Parse(File.ReadAllText(appSettingsPath));
    if (json.RootElement.TryGetProperty("ConnectionStrings", out var cs) && cs.TryGetProperty("MySqlDb", out var my))
        return my.GetString();
    if (json.RootElement.TryGetProperty("MySqlDb", out var flat))
        return flat.GetString();
    return null;
}

static string[] SplitSqlStatements(string sql)
{
    // Naive split on semicolons not inside string literals
    var list = new System.Collections.Generic.List<string>();
    var sb = new StringBuilder();
    bool inSingle = false, inDouble = false;
    for (int i = 0; i < sql.Length; i++)
    {
        char c = sql[i];
        if (c == '\'' && !inDouble) inSingle = !inSingle;
        else if (c == '"' && !inSingle) inDouble = !inDouble;
        if (c == ';' && !inSingle && !inDouble)
        {
            var s = sb.ToString().Trim();
            if (s.Length > 0) list.Add(s);
            sb.Clear();
        }
        else sb.Append(c);
    }
    var last = sb.ToString().Trim();
    if (last.Length > 0) list.Add(last);
    return list.ToArray();
}

async Task<int> MainAsync(string[] args)
{
    if (args.Length == 0)
    {
        Console.Error.WriteLine("Usage: DbPatchApplier <path-to-sql>");
        return 2;
    }
    var path = Path.GetFullPath(args[0]);
    if (!File.Exists(path))
    {
        Console.Error.WriteLine($"SQL file not found: {path}");
        return 2;
    }
    var cs = ResolveConnectionString();
    if (string.IsNullOrWhiteSpace(cs))
    {
        Console.Error.WriteLine("MySQL connection string not found. Set MYSQL_CS or appsettings.json with ConnectionStrings:MySqlDb.");
        return 3;
    }
    Console.WriteLine($"Applying patches from {path} ...");
    var sqlText = await File.ReadAllTextAsync(path);
    var stmts = SplitSqlStatements(sqlText).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
    Console.WriteLine($"Statements: {stmts.Length}");

    await using var conn = new MySqlConnection(cs);
    await conn.OpenAsync();
    await using var tx = await conn.BeginTransactionAsync();
    try
    {
        foreach (var s in stmts)
        {
            await using var cmd = new MySqlCommand(s, conn, tx);
            await cmd.ExecuteNonQueryAsync();
        }
        await tx.CommitAsync();
        Console.WriteLine("Patches applied successfully.");
        return 0;
    }
    catch (Exception ex)
    {
        await tx.RollbackAsync();
        Console.Error.WriteLine($"Error applying patches: {ex.Message}");
        return 1;
    }
}

return await MainAsync(args);

