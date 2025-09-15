using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.IO;
using System.Threading.Tasks;
using MySqlConnector;

static class Program
{
    static async Task<int> Main()
    {
        try
        {
            var cs = Environment.GetEnvironmentVariable("YASGMP_MYSQL_CS") ?? ReadConnFromAppSettings();
            if (string.IsNullOrWhiteSpace(cs))
            {
                Console.Error.WriteLine("Connection string missing. Set YASGMP_MYSQL_CS or appsettings.json:ConnectionStrings:MySqlDb");
                return 2;
            }

            await using var conn = new MySqlConnection(cs);
            await conn.OpenAsync();

            Console.WriteLine($"Connected. DB={conn.Database} Server={conn.DataSource}");

            // Print counts for key tables
            var tables = new[]
            {
                "machines","machine_types","manufacturers","locations","responsible_parties",
                "work_orders","work_order_parts","calibrations","validation_sets","preventive_maintenance_plans",
                "parts","stock_levels","documents","document_links"
            };
            foreach (var t in tables)
            {
                try
                {
                    await using var cmd = new MySqlCommand($"SELECT COUNT(*) FROM `{t}`", conn);
                    var c = await cmd.ExecuteScalarAsync();
                    Console.WriteLine($"{t,-32} : {c}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{t,-32} : (missing) {ex.Message}");
                }
            }

            // Describe machines columns (map)
            Console.WriteLine("\n[machines] columns:");
            try
            {
                await using var cmd = new MySqlCommand("SHOW COLUMNS FROM `machines`", conn);
                await using var rdr = await cmd.ExecuteReaderAsync();
                while (await rdr.ReadAsync())
                {
                    var name = rdr.GetString("Field");
                    var type = rdr.GetString("Type");
                    Console.WriteLine($" - {name} : {type}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SHOW COLUMNS failed: {ex.Message}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
            return 1;
        }
    }

    static string? ReadConnFromAppSettings()
    {
        try
        {
            // Try repo root appsettings.json
            var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "appsettings.json");
            path = Path.GetFullPath(path);
            if (File.Exists(path))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                if (doc.RootElement.TryGetProperty("ConnectionStrings", out var cs) &&
                    cs.TryGetProperty("MySqlDb", out var v) &&
                    v.ValueKind == JsonValueKind.String)
                {
                    return v.GetString();
                }
            }
        }
        catch { }
        return null;
    }
}

