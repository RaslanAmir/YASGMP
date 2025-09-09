using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Runtime.Loader;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MySqlConnector;

internal static class TypeMap
{
    public static string ToSqlType(PropertyInfo p)
    {
        var colAttr = p.GetCustomAttribute<ColumnAttribute>();
        if (colAttr?.TypeName is string tn && !string.IsNullOrWhiteSpace(tn))
            return tn;

        var t = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
        if (t == typeof(int) || t == typeof(short)) return "int";
        if (t == typeof(long)) return "bigint";
        if (t == typeof(bool)) return "tinyint(1)";
        if (t == typeof(DateTime)) return "datetime";
        if (t == typeof(decimal) || t == typeof(double) || t == typeof(float)) return "decimal(10,2)";
        if (t == typeof(string))
        {
            var max = p.GetCustomAttribute<MaxLengthAttribute>()?.Length;
            if (max.HasValue)
            {
                if (max.Value <= 0) return "text";
                if (max.Value <= 65535) return $"varchar({Math.Clamp(max.Value,1,65535)})";
                return "text";
            }
            return "varchar(255)";
        }
        return "text";
    }
}

internal static class NameConv
{
    private static readonly Regex Upper = new Regex("([a-z0-9])([A-Z])", RegexOptions.Compiled);
    public static string ToSnake(string name)
        => Upper.Replace(name, "$1_$2").ToLowerInvariant();
}

class Program
{
    static async Task<int> Main(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("YASGMP_MYSQL_CS") ?? "Server=localhost;Database=yasgmp;User=root;Password=Jasenka1;Charset=utf8mb4;Allow User Variables=true;AllowPublicKeyRetrieval=True;SslMode=None;";
        var asmPath = args.FirstOrDefault(a => a.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    ?? Path.Combine("bin", "Release", "net8.0-windows10.0.19041.0", "win10-x64", "yasgmp.dll");
        var apply = args.Any(a => a.Equals("--apply", StringComparison.OrdinalIgnoreCase));

        if (!File.Exists(asmPath))
        {
            Console.Error.WriteLine($"Assembly not found: {asmPath}");
            return 2;
        }\r\n            return File.Exists(probe) ? ctx.LoadFromAssemblyPath(probe) : null;
        };
        asmPath = Path.GetFullPath(asmPath);\r\n            return File.Exists(probe) ? ctx.LoadFromAssemblyPath(probe) : null;
        };
        var asm = Assembly.LoadFrom(asmPath);

        var modelTypes = asm.GetTypes()
            .Where(t => t.IsClass && t.Namespace != null && t.Namespace.StartsWith("YasGMP.Models"))
            .ToList();

        await using var conn = new MySqlConnection(cs);
        await conn.OpenAsync();

        // Get all tables in current DB
        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using (var cmd = new MySqlCommand("SELECT table_name FROM information_schema.tables WHERE table_schema=DATABASE()", conn))
        await using (var r = await cmd.ExecuteReaderAsync())
        {
            while (await r.ReadAsync()) tables.Add(r.GetString(0));
        }

        int alters = 0;
        foreach (var type in modelTypes)
        {
            var table = type.GetCustomAttribute<TableAttribute>()?.Name;
            if (string.IsNullOrWhiteSpace(table))
            {
                // Best-effort: try snake plural
                var snake = NameConv.ToSnake(type.Name);
                var candidates = new[] { snake, snake + "s", snake + "es" };
                table = candidates.FirstOrDefault(t => tables.Contains(t));
                if (string.IsNullOrWhiteSpace(table))
                    continue; // skip unmatched models (avoid guessing)
            }
            if (!tables.Contains(table)) continue;

            // Existing columns
            var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using (var cmd = new MySqlCommand("SELECT column_name FROM information_schema.columns WHERE table_schema=DATABASE() AND table_name=@t", conn))
            {
                cmd.Parameters.AddWithValue("@t", table);
                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync()) existing.Add(r.GetString(0));
            }

            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetMethod is not null && p.SetMethod is not null && p.GetCustomAttribute<NotMappedAttribute>() == null);

            foreach (var p in props)
            {
                var col = p.GetCustomAttribute<ColumnAttribute>()?.Name ?? NameConv.ToSnake(p.Name);
                if (existing.Contains(col)) continue;

                var sqlType = TypeMap.ToSqlType(p);
                var alter = $"ALTER TABLE `{table}` ADD COLUMN `{col}` {sqlType} NULL;";
                Console.WriteLine(alter);

                if (apply)
                {
                    await using var cmd = new MySqlCommand(alter, conn);
                    await cmd.ExecuteNonQueryAsync();
                    alters++;
                }
            }
        }

        Console.WriteLine(apply
            ? $"Applied {alters} ADD COLUMN statements."
            : "Dry run. Re-run with --apply to execute.");
        return 0;
    }
}



