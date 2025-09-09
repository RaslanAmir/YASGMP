using System.Data;
using System.Text.Json;
using MySqlConnector;

static string GetConnectionString()
{
    var env = Environment.GetEnvironmentVariable("YASGMP_ConnectionString");
    if (!string.IsNullOrWhiteSpace(env)) return env!;

    var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "appsettings.json");
    appSettingsPath = Path.GetFullPath(appSettingsPath);
    if (File.Exists(appSettingsPath))
    {
        try
        {
            using var stream = File.OpenRead(appSettingsPath);
            using var doc = JsonDocument.Parse(stream);
            if (doc.RootElement.TryGetProperty("ConnectionStrings", out var cs) && cs.TryGetProperty("MySqlDb", out var val))
            {
                var s = val.GetString();
                if (!string.IsNullOrWhiteSpace(s)) return s!;
            }
        }
        catch { /* ignore and fall back */ }
    }

    // Fallback from MauiProgram.cs default
    return "Server=127.0.0.1;Port=3306;Database=YASGMP;User ID=yasgmp_app;Password=Jasenka1;Character Set=utf8mb4;Connection Timeout=5;Default Command Timeout=30;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=50;";
}

var mappings = new (string aliasName, string baseName)[]
{
    ("warehouse","warehouses"),
    ("role","roles"),
    ("incidents","incident_log"),
    ("units","measurement_units"),
    ("notifications","notification_queue"),
    ("audit_log","system_event_log"),
    ("capa_audit_log","capa_status_history"),
};

var cs = GetConnectionString();
var csb = new MySqlConnectionStringBuilder(cs)
{
    // ensure user variables like @db, @sql are permitted in scripts
    AllowUserVariables = true,
};
Console.WriteLine("[verify-sync] Using connection: " + new MySqlConnectionStringBuilder(csb.ConnectionString) { Password = "***" });

await using var con = new MySqlConnection(csb.ConnectionString);
await con.OpenAsync();

async Task<string?> TableTypeAsync(string name)
{
    var cmd = new MySqlCommand(@"SELECT table_type FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = @n LIMIT 1;", con);
    cmd.Parameters.AddWithValue("@n", name);
    var r = await cmd.ExecuteScalarAsync();
    return r as string; // "BASE TABLE" or "VIEW" or null
}

async Task<string?> ShowCreateViewAsync(string name)
{
    var cmd = new MySqlCommand($"SHOW CREATE VIEW `{name}`;", con);
    try
    {
        await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow);
        if (await reader.ReadAsync())
        {
            // Columns: View, Create View, character_set_client, collation_connection
            return reader.GetString(1);
        }
    }
    catch (MySqlException)
    {
        // Could be unknown table or insufficient SHOW VIEW privilege; treat as unavailable.
        return null;
    }
    return null;
}

async Task<bool> CanSelectAsync(string name)
{
    var cmd = new MySqlCommand($"SELECT 1 FROM `{name}` LIMIT 1;", con);
    try { await cmd.ExecuteScalarAsync(); return true; }
    catch { return false; }
}

Console.WriteLine("[verify-sync] Database: " + await new MySqlCommand("SELECT DATABASE();", con).ExecuteScalarAsync());
Console.WriteLine();
Console.WriteLine("AliasName | AliasType | BaseName | BaseType | ViewOK | SelectOK | Notes");
Console.WriteLine(new string('-', 100));

// optional: apply migration when requested
bool shouldApply = args.Contains("--apply", StringComparer.OrdinalIgnoreCase) ||
                   string.Equals(Environment.GetEnvironmentVariable("YASGMP_APPLY_MIGRATION"), "1", StringComparison.OrdinalIgnoreCase);
if (shouldApply)
{
    async Task ApplyMigrationAsync(string filePath)
    {
        Console.WriteLine($"[verify-sync] Applying migration: {filePath}");
        var text = await File.ReadAllTextAsync(filePath);
        // strip line comments and split on semicolons
        var lines = text.Replace("\r\n", "\n").Split('\n');
        var sb = new System.Text.StringBuilder();
        foreach (var line in lines)
        {
            var t = line.Trim();
            if (t.StartsWith("--")) continue; // skip comment line
            sb.AppendLine(line);
        }
        var script = sb.ToString();
        var statements = script.Split(';');
        int applied = 0;
        foreach (var raw in statements)
        {
            var sql = raw.Trim();
            if (string.IsNullOrWhiteSpace(sql)) continue;
            using var cmd = new MySqlCommand(sql, con);
            await cmd.ExecuteNonQueryAsync();
            applied++;
        }
        Console.WriteLine($"[verify-sync] Applied {applied} statements.");
    }

    // locate migration file (prefer latest *_sync.sql)
    var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    var migrationsDir = Path.Combine(root, "migrations");
    string? migrationFile = null;
    if (Directory.Exists(migrationsDir))
    {
        migrationFile = Directory.GetFiles(migrationsDir, "*_sync.sql").OrderBy(f => Path.GetFileName(f)).LastOrDefault();
    }
    if (migrationFile is null)
    {
        Console.WriteLine("[verify-sync] No migration file found in migrations/*.sql");
    }
    else
    {
        await ApplyMigrationAsync(migrationFile);
        Console.WriteLine();
    }
}

foreach (var (alias, baseName) in mappings)
{
    var aliasType = await TableTypeAsync(alias);
    var baseType = await TableTypeAsync(baseName);

    bool viewOk = false;
    bool selectOk = false;
    var notes = "";

    if (aliasType == "VIEW")
    {
        var ddl = await ShowCreateViewAsync(alias) ?? "";
        if (!string.IsNullOrEmpty(ddl) && ddl.Contains($"`{baseName}`", StringComparison.OrdinalIgnoreCase))
            viewOk = true;
        selectOk = await CanSelectAsync(alias);
    }
    else if (aliasType == "BASE TABLE")
    {
        notes = "alias is base table; migration guarded to no-op";
        selectOk = await CanSelectAsync(alias);
    }
    else
    {
        if (baseType == null)
            notes = "alias missing and base missing (guarded no-op)";
        else
            notes = "alias missing; apply migration to create view";
    }

    Console.WriteLine($"{alias} | {aliasType ?? "<none>"} | {baseName} | {baseType ?? "<none>"} | {(viewOk ? "OK" : "-")} | {(selectOk ? "OK" : "-")} | {notes}");
}

await con.CloseAsync();
Console.WriteLine();
Console.WriteLine("[verify-sync] Done.");
