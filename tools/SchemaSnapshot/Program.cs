using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MySqlConnector;

class ColumnInfo
{
    public string Name { get; set; } = "";
    public string DataType { get; set; } = "";
    public string ColumnType { get; set; } = "";
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsUnique { get; set; }
    public string? Default { get; set; }
    public string Extra { get; set; } = "";
    public int OrdinalPosition { get; set; }
    public string Comment { get; set; } = "";
}

class TableInfo
{
    public string Name { get; set; } = "";
    public string Engine { get; set; } = "";
    public string TableCollation { get; set; } = "";
    public string TableComment { get; set; } = "";
    public List<ColumnInfo> Columns { get; set; } = new();
}

class Snapshot
{
    public string Database { get; set; } = "";
    public string AliasTried { get; set; } = "";
    public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;
    public List<TableInfo> Tables { get; set; } = new();
}

static class Program
{
    static string? ResolveConnectionString()
    {
        var env = Environment.GetEnvironmentVariable("MYSQL_CS");
        if (!string.IsNullOrWhiteSpace(env)) return env;
        var appSettingsPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"));
        if (File.Exists(appSettingsPath))
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(appSettingsPath));
                if (doc.RootElement.TryGetProperty("ConnectionStrings", out var cs) && cs.TryGetProperty("MySqlDb", out var my))
                    return my.GetString();
                if (doc.RootElement.TryGetProperty("MySqlDb", out var flat))
                    return flat.GetString();
            }
            catch { }
        }
        return null;
    }

    static async Task<int> Main(string[] args)
    {
        var cs = ResolveConnectionString();
        if (string.IsNullOrWhiteSpace(cs))
        {
            Console.Error.WriteLine("MySQL connection string not found (MYSQL_CS or appsettings.json).");
            return 2;
        }

        var outDir = Path.Combine("tools", "schema", "snapshots");
        Directory.CreateDirectory(outDir);
        var outFile = Path.Combine(outDir, "schema.json");

        await using var conn = new MySqlConnection(cs);
        await conn.OpenAsync();

        string dbName;
        await using (var cmd = new MySqlCommand("SELECT DATABASE()", conn))
        {
            dbName = (await cmd.ExecuteScalarAsync())?.ToString() ?? "";
        }

        var snap = new Snapshot { Database = dbName, CapturedAtUtc = DateTime.UtcNow };

        var tables = new List<string>();
        await using (var cmd = new MySqlCommand("SELECT table_name FROM information_schema.tables WHERE table_schema=DATABASE()", conn))
        await using (var r = await cmd.ExecuteReaderAsync())
        {
            while (await r.ReadAsync()) tables.Add(r.GetString(0));
        }

        foreach (var t in tables)
        {
            var ti = new TableInfo { Name = t };

            await using (var cmd = new MySqlCommand(@"SELECT ENGINE, TABLE_COLLATION, IFNULL(TABLE_COMMENT,'') FROM information_schema.tables WHERE table_schema=DATABASE() AND table_name=@t", conn))
            {
                cmd.Parameters.AddWithValue("@t", t);
                await using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    ti.Engine = r.IsDBNull(0) ? "" : r.GetString(0);
                    ti.TableCollation = r.IsDBNull(1) ? "" : r.GetString(1);
                    ti.TableComment = r.IsDBNull(2) ? "" : r.GetString(2);
                }
            }

            await using (var cmd = new MySqlCommand(@"SELECT COLUMN_NAME, DATA_TYPE, COLUMN_TYPE, IS_NULLABLE, COLUMN_DEFAULT, EXTRA, ORDINAL_POSITION, IFNULL(COLUMN_COMMENT,'')
                                                     FROM information_schema.columns
                                                     WHERE table_schema=DATABASE() AND table_name=@t ORDER BY ORDINAL_POSITION", conn))
            {
                cmd.Parameters.AddWithValue("@t", t);
                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    var c = new ColumnInfo
                    {
                        Name = r.GetString(0),
                        DataType = r.GetString(1),
                        ColumnType = r.GetString(2),
                        IsNullable = string.Equals(r.GetString(3), "YES", StringComparison.OrdinalIgnoreCase),
                        Default = r.IsDBNull(4) ? null : r.GetValue(4)?.ToString(),
                        Extra = r.IsDBNull(5) ? "" : r.GetString(5),
                        OrdinalPosition = r.GetInt32(6),
                        Comment = r.GetString(7)
                    };
                    ti.Columns.Add(c);
                }
            }

            // PK and Unique flags
            await using (var cmd = new MySqlCommand(@"SELECT COLUMN_NAME, CONSTRAINT_NAME
                                                     FROM information_schema.key_column_usage
                                                     WHERE table_schema=DATABASE() AND table_name=@t", conn))
            {
                cmd.Parameters.AddWithValue("@t", t);
                var pkCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var uqCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    var col = r.GetString(0);
                    var cons = r.GetString(1);
                    if (string.Equals(cons, "PRIMARY", StringComparison.OrdinalIgnoreCase)) pkCols.Add(col);
                    if (cons.StartsWith("uniq_", StringComparison.OrdinalIgnoreCase) || cons.StartsWith("unique", StringComparison.OrdinalIgnoreCase)) uqCols.Add(col);
                }
                foreach (var c in ti.Columns)
                {
                    c.IsPrimaryKey = pkCols.Contains(c.Name);
                    c.IsUnique = uqCols.Contains(c.Name);
                }
            }

            snap.Tables.Add(ti);
        }

        var opts = new JsonSerializerOptions { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
        var json = JsonSerializer.Serialize(snap, opts);
        await File.WriteAllTextAsync(outFile, json);
        Console.WriteLine($"Wrote {outFile} with {snap.Tables.Count} tables.");
        return 0;
    }
}

