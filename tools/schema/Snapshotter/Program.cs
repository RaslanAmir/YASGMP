using System.Text;
using System.Text.Json;
using MySqlConnector;

static string Arg(string name, string? def = null)
{
    for (int i = 0; i < Environment.GetCommandLineArgs().Length; i++)
    {
        var a = Environment.GetCommandLineArgs()[i];
        if (a.Equals(name, StringComparison.OrdinalIgnoreCase) && i + 1 < Environment.GetCommandLineArgs().Length)
            return Environment.GetCommandLineArgs()[i + 1];
    }
    return def ?? string.Empty;
}

static string Q(string ident)
{
    // Quote MySQL identifier with backticks, escaping backticks
    return "`" + ident.Replace("`", "``") + "`";
}

var host = Arg("--host", "127.0.0.1");
var port = int.TryParse(Arg("--port", "3306"), out var p) ? p : 3306;
var user = Arg("--user", "root");
var pass = Arg("--password", string.Empty);
var outDir = Arg("--out", Path.Combine(Directory.GetCurrentDirectory(), "..", "snapshots"));
Directory.CreateDirectory(outDir);

var csb = new MySqlConnectionStringBuilder
{
    Server = host,
    Port = (uint)port,
    UserID = user,
    Password = pass,
    SslMode = MySqlSslMode.None,
    AllowUserVariables = true,
    AllowPublicKeyRetrieval = true,
    ConnectionTimeout = 6,
    DefaultCommandTimeout = 30
};

using var conn = new MySqlConnection(csb.ConnectionString);
await conn.OpenAsync();

static async Task<string?> ScalarStringAsync(MySqlConnection c, string sql)
{
    using var cmd = new MySqlCommand(sql, c);
    var o = await cmd.ExecuteScalarAsync();
    return o?.ToString();
}

var dbName = await ScalarStringAsync(conn,
    "SELECT SCHEMA_NAME FROM information_schema.schemata WHERE SCHEMA_NAME IN ('yasgmp','yasmp') ORDER BY SCHEMA_NAME='yasgmp' DESC LIMIT 1;");
if (string.IsNullOrWhiteSpace(dbName))
    throw new Exception("Neither yasgmp nor yasmp database found.");

Console.WriteLine($"Using database: {dbName}");

async Task<List<string>> ListAsync(string sql)
{
    var list = new List<string>();
    using var cmd = new MySqlCommand(sql, conn);
    using var rdr = await cmd.ExecuteReaderAsync();
    while (await rdr.ReadAsync())
        list.Add(rdr.GetString(0));
    return list;
}

async Task<string> ShowCreateAsync(string kind, string name)
{
    var sql = $"SHOW CREATE {kind} {Q(dbName!)}.{Q(name)};";
    using var cmd = new MySqlCommand(sql, conn);
    using var rdr = await cmd.ExecuteReaderAsync();
    if (!await rdr.ReadAsync()) return string.Empty;
    string expected = kind.ToUpperInvariant() switch
    {
        "TABLE" => "Create Table",
        "VIEW" => "Create View",
        "TRIGGER" => "SQL Original Statement",
        "PROCEDURE" => "Create Procedure",
        "FUNCTION" => "Create Function",
        _ => string.Empty
    };
    if (!string.IsNullOrEmpty(expected))
    {
        for (int i = 0; i < rdr.FieldCount; i++)
        {
            if (string.Equals(rdr.GetName(i), expected, StringComparison.OrdinalIgnoreCase))
                return rdr.GetString(i);
        }
    }
    // Fallback: find the first string column that looks like DDL
    for (int i = 0; i < rdr.FieldCount; i++)
    {
        if (!await rdr.IsDBNullAsync(i) && rdr.GetFieldType(i) == typeof(string))
        {
            var val = rdr.GetString(i);
            if (val.StartsWith("CREATE", StringComparison.OrdinalIgnoreCase))
                return val;
        }
    }
    return string.Empty;
}

// schema.sql (tables)
var tables = await ListAsync($"SELECT TABLE_NAME FROM information_schema.tables WHERE table_schema='{dbName}' AND TABLE_TYPE='BASE TABLE' ORDER BY TABLE_NAME;");
var schemaSqlPath = Path.Combine(outDir, "schema.sql");
await File.WriteAllTextAsync(schemaSqlPath, $"-- Schema snapshot for {dbName} (tables) - {DateTime.UtcNow:o}\n");
foreach (var t in tables)
{
    var ddl = await ShowCreateAsync("TABLE", t);
    await File.AppendAllTextAsync(schemaSqlPath, $"\n-- {t}\n{ddl};\n");
}
Console.WriteLine($"Wrote: {schemaSqlPath}");

// views.sql
var views = await ListAsync($"SELECT TABLE_NAME FROM information_schema.views WHERE table_schema='{dbName}' ORDER BY TABLE_NAME;");
var viewsSqlPath = Path.Combine(outDir, "views.sql");
await File.WriteAllTextAsync(viewsSqlPath, $"-- Views snapshot for {dbName} - {DateTime.UtcNow:o}\n");
foreach (var v in views)
{
    var ddl = await ShowCreateAsync("VIEW", v);
    await File.AppendAllTextAsync(viewsSqlPath, $"\n-- {v}\n{ddl};\n");
}
Console.WriteLine($"Wrote: {viewsSqlPath}");

// triggers.sql
var triggers = await ListAsync($"SELECT TRIGGER_NAME FROM information_schema.triggers WHERE trigger_schema='{dbName}' ORDER BY TRIGGER_NAME;");
var trgSqlPath = Path.Combine(outDir, "triggers.sql");
await File.WriteAllTextAsync(trgSqlPath, $"-- Triggers snapshot for {dbName} - {DateTime.UtcNow:o}\n");
foreach (var g in triggers)
{
    var ddl = await ShowCreateAsync("TRIGGER", g);
    await File.AppendAllTextAsync(trgSqlPath, $"\n-- {g}\n{ddl};\n");
}
Console.WriteLine($"Wrote: {trgSqlPath}");

// routines.sql
var routines = new List<(string name, string type)>();
using (var cmd = new MySqlCommand($"SELECT ROUTINE_NAME, ROUTINE_TYPE FROM information_schema.routines WHERE ROUTINE_SCHEMA='{dbName}' ORDER BY ROUTINE_NAME;", conn))
using (var rdr = await cmd.ExecuteReaderAsync())
{
    while (await rdr.ReadAsync())
        routines.Add((rdr.GetString(0), rdr.GetString(1)));
}
var rtnSqlPath = Path.Combine(outDir, "routines.sql");
await File.WriteAllTextAsync(rtnSqlPath, $"-- Routines snapshot for {dbName} - {DateTime.UtcNow:o}\n");
foreach (var (name, type) in routines)
{
    var kind = type.Equals("FUNCTION", StringComparison.OrdinalIgnoreCase) ? "FUNCTION" : "PROCEDURE";
    var ddl = await ShowCreateAsync(kind, name);
    await File.AppendAllTextAsync(rtnSqlPath, $"\n-- {kind} {name}\n{ddl};\n");
}
Console.WriteLine($"Wrote: {rtnSqlPath}");

// schema.json (columns, pks, fks, indexes)
var colSql = $@"SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, COLUMN_TYPE, IS_NULLABLE, COLUMN_DEFAULT, EXTRA, COLUMN_KEY,
       CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE, DATETIME_PRECISION, COLLATION_NAME, COLUMN_COMMENT
FROM information_schema.columns WHERE table_schema = '{dbName}' ORDER BY TABLE_NAME, ORDINAL_POSITION;";
var cols = new List<Dictionary<string, object?>>();
using (var cmd = new MySqlCommand(colSql, conn))
using (var rdr = await cmd.ExecuteReaderAsync())
{
    while (await rdr.ReadAsync())
    {
        cols.Add(new Dictionary<string, object?>
        {
            ["TABLE_NAME"] = rdr.GetString(0),
            ["COLUMN_NAME"] = rdr.GetString(1),
            ["DATA_TYPE"] = rdr.GetString(2),
            ["COLUMN_TYPE"] = rdr.GetString(3),
            ["IS_NULLABLE"] = rdr.GetString(4),
            ["COLUMN_DEFAULT"] = await rdr.IsDBNullAsync(5) ? null : rdr.GetValue(5),
            ["EXTRA"] = rdr.GetString(6),
            ["COLUMN_KEY"] = rdr.GetString(7),
            ["CHARACTER_MAXIMUM_LENGTH"] = await rdr.IsDBNullAsync(8) ? null : rdr.GetValue(8),
            ["NUMERIC_PRECISION"] = await rdr.IsDBNullAsync(9) ? null : rdr.GetValue(9),
            ["NUMERIC_SCALE"] = await rdr.IsDBNullAsync(10) ? null : rdr.GetValue(10),
            ["DATETIME_PRECISION"] = await rdr.IsDBNullAsync(11) ? null : rdr.GetValue(11),
            ["COLLATION_NAME"] = await rdr.IsDBNullAsync(12) ? null : rdr.GetString(12),
            ["COLUMN_COMMENT"] = await rdr.IsDBNullAsync(13) ? null : rdr.GetString(13),
        });
    }
}

var pkSql = $@"SELECT tc.TABLE_NAME, kcu.COLUMN_NAME, kcu.ORDINAL_POSITION
FROM information_schema.table_constraints tc
JOIN information_schema.key_column_usage kcu
  ON tc.CONSTRAINT_SCHEMA = kcu.CONSTRAINT_SCHEMA
 AND tc.TABLE_NAME = kcu.TABLE_NAME
 AND tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
WHERE tc.CONSTRAINT_SCHEMA = '{dbName}' AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
ORDER BY tc.TABLE_NAME, kcu.ORDINAL_POSITION;";
var pks = new List<Dictionary<string, object?>>();
using (var cmd = new MySqlCommand(pkSql, conn))
using (var rdr = await cmd.ExecuteReaderAsync())
{
    while (await rdr.ReadAsync())
    {
        pks.Add(new Dictionary<string, object?>
        {
            ["TABLE_NAME"] = rdr.GetString(0),
            ["COLUMN_NAME"] = rdr.GetString(1),
            ["ORDINAL_POSITION"] = rdr.GetInt32(2)
        });
    }
}

var fkSql = $@"SELECT k.TABLE_NAME, k.CONSTRAINT_NAME, k.COLUMN_NAME, k.ORDINAL_POSITION,
       k.REFERENCED_TABLE_NAME, k.REFERENCED_COLUMN_NAME, rc.UPDATE_RULE, rc.DELETE_RULE
FROM information_schema.key_column_usage k
JOIN information_schema.referential_constraints rc
  ON rc.CONSTRAINT_SCHEMA = k.CONSTRAINT_SCHEMA
 AND rc.CONSTRAINT_NAME = k.CONSTRAINT_NAME
WHERE k.CONSTRAINT_SCHEMA = '{dbName}' AND k.REFERENCED_TABLE_NAME IS NOT NULL
ORDER BY k.TABLE_NAME, k.CONSTRAINT_NAME, k.ORDINAL_POSITION;";
var fks = new List<Dictionary<string, object?>>();
using (var cmd = new MySqlCommand(fkSql, conn))
using (var rdr = await cmd.ExecuteReaderAsync())
{
    while (await rdr.ReadAsync())
    {
        fks.Add(new Dictionary<string, object?>
        {
            ["TABLE_NAME"] = rdr.GetString(0),
            ["CONSTRAINT_NAME"] = rdr.GetString(1),
            ["COLUMN_NAME"] = rdr.GetString(2),
            ["ORDINAL_POSITION"] = rdr.GetInt32(3),
            ["REFERENCED_TABLE_NAME"] = rdr.GetString(4),
            ["REFERENCED_COLUMN_NAME"] = rdr.GetString(5),
            ["ON_UPDATE"] = rdr.GetString(6),
            ["ON_DELETE"] = rdr.GetString(7)
        });
    }
}

var ixSql = $@"SELECT TABLE_NAME, INDEX_NAME, NON_UNIQUE, SEQ_IN_INDEX, COLUMN_NAME, COLLATION, CARDINALITY, SUB_PART, NULLABLE, INDEX_TYPE, COMMENT
FROM information_schema.statistics
WHERE TABLE_SCHEMA = '{dbName}'
ORDER BY TABLE_NAME, INDEX_NAME, SEQ_IN_INDEX;";
var ixs = new List<Dictionary<string, object?>>();
using (var cmd = new MySqlCommand(ixSql, conn))
using (var rdr = await cmd.ExecuteReaderAsync())
{
    while (await rdr.ReadAsync())
    {
        ixs.Add(new Dictionary<string, object?>
        {
            ["TABLE_NAME"] = rdr.IsDBNull(0) ? null : rdr.GetString(0),
            ["INDEX_NAME"] = rdr.IsDBNull(1) ? null : rdr.GetString(1),
            ["NON_UNIQUE"] = rdr.GetInt32(2) != 0,
            ["SEQ_IN_INDEX"] = rdr.GetInt32(3),
            ["COLUMN_NAME"] = rdr.IsDBNull(4) ? null : rdr.GetString(4),
            ["COLLATION"] = rdr.IsDBNull(5) ? null : rdr.GetString(5),
            ["CARDINALITY"] = rdr.IsDBNull(6) ? null : rdr.GetInt64(6),
            ["SUB_PART"] = rdr.IsDBNull(7) ? null : rdr.GetInt32(7),
            ["NULLABLE"] = rdr.IsDBNull(8) ? null : rdr.GetString(8),
            ["INDEX_TYPE"] = rdr.IsDBNull(9) ? null : rdr.GetString(9),
            ["COMMENT"] = rdr.IsDBNull(10) ? null : rdr.GetString(10)
        });
    }
}

var tableNames = new HashSet<string>(tables.Concat(views));
var schema = new Dictionary<string, object?>
{
    ["database"] = dbName,
    ["generated_at_utc"] = DateTime.UtcNow.ToString("o"),
    ["tables"] = tableNames.ToDictionary(
        t => t,
        t => (object?)new Dictionary<string, object?>
        {
            ["columns"] = cols.Where(c => (string)c["TABLE_NAME"]! == t).ToList(),
            ["primaryKey"] = pks.Where(k => (string)k["TABLE_NAME"]! == t).OrderBy(k => (int)k["ORDINAL_POSITION"]!).ToList(),
            ["foreignKeys"] = fks.Where(k => (string)k["TABLE_NAME"]! == t).OrderBy(k => (string)k["CONSTRAINT_NAME"]!).ThenBy(k => (int)k["ORDINAL_POSITION"]!).ToList(),
            ["indexes"] = ixs.Where(x => (string)x["TABLE_NAME"]! == t).OrderBy(x => (string)x["INDEX_NAME"]!).ThenBy(x => (int)x["SEQ_IN_INDEX"]!).ToList()
        })
};

var jsonPath = Path.Combine(outDir, "schema.json");
await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(schema, new JsonSerializerOptions { WriteIndented = false }));
Console.WriteLine($"Wrote: {jsonPath}");

// Append progress log one directory up from snapshots (reports/PROGRESS.log)
var repoRoot = Directory.GetCurrentDirectory();
// Snapshotter runs from its own folder; outDir was passed from caller; we’ll append to reports under workspace
try
{
    var wsRoot = Path.GetFullPath(Path.Combine(outDir, "..", "..", ".."));
    var progressPath = Path.Combine(wsRoot, "reports", "PROGRESS.log");
    Directory.CreateDirectory(Path.GetDirectoryName(progressPath)!);
    await File.AppendAllTextAsync(progressPath, $"[{DateTime.UtcNow:o}] STEP OBS-001 — Schema snapshot captured for {dbName}\n");
}
catch { /* best effort */ }
