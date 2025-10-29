using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using YasGMP.Diagnostics;
using YasGMP.Services;

namespace YasGMP.Wpf.Runtime
{
    internal static class DatabaseTestHookBootstrapper
    {
        internal const string FlagName = "YASGMP_WPF_TESTHOOKS";
        internal const string FixturePathVariable = "YASGMP_WPF_FIXTURE_PATH";
        internal const string FixtureJsonVariable = "YASGMP_WPF_FIXTURE_JSON";
        private const string Category = "Wpf.TestHooks";

        public static void Configure(DatabaseService database, IServiceProvider services)
        {
            var trace = services.GetService<ITrace>();
            var accessor = DatabaseServiceTestHookAccessor.TryCreate(database, trace);
            if (accessor == null)
            {
                return;
            }

            if (!IsFlagEnabled())
            {
                accessor.Reset();
                trace?.Log(DiagLevel.Info, Category, nameof(Configure),
                    "DatabaseService test hooks disabled (flag not set).");
                return;
            }

            try
            {
                var fixture = DatabaseFixture.LoadFromEnvironment(trace);
                var runtime = new DatabaseFixtureRuntime(fixture, trace);

                accessor.SetExecuteNonQueryOverride(runtime.ExecuteNonQueryAsync);
                accessor.SetExecuteScalarOverride(runtime.ExecuteScalarAsync);
                accessor.SetExecuteSelectOverride(runtime.ExecuteSelectAsync);

                trace?.Log(DiagLevel.Info, Category, nameof(Configure),
                    $"DatabaseService test hooks enabled using fixture '{fixture.Name}'.");
            }
            catch (Exception ex)
            {
                accessor.Reset();
                trace?.Log(DiagLevel.Error, Category, nameof(Configure),
                    "Failed to initialize DatabaseService test hooks.", ex);
            }
        }

        private static bool IsFlagEnabled()
        {
            var flag = Environment.GetEnvironmentVariable(FlagName);
            if (string.IsNullOrWhiteSpace(flag))
            {
                return false;
            }

            return flag.Equals("1", StringComparison.OrdinalIgnoreCase)
                || flag.Equals("true", StringComparison.OrdinalIgnoreCase)
                || flag.Equals("yes", StringComparison.OrdinalIgnoreCase)
                || flag.Equals("on", StringComparison.OrdinalIgnoreCase);
        }

        private sealed class DatabaseFixtureRuntime
        {
            private readonly DatabaseFixture _fixture;
            private readonly ITrace? _trace;
            private readonly ConcurrentDictionary<string, byte> _loggedMisses = new(StringComparer.OrdinalIgnoreCase);

            public DatabaseFixtureRuntime(DatabaseFixture fixture, ITrace? trace)
            {
                _fixture = fixture;
                _trace = trace;
            }

            public Task<int> ExecuteNonQueryAsync(
                string sql,
                IEnumerable<MySqlParameter>? parameters,
                CancellationToken token)
            {
                var (matched, result, descriptor) = _fixture.ResolveNonQuery(sql);
                if (!matched)
                {
                    LogMiss("non-query", sql);
                }
                else
                {
                    LogHit("non-query", descriptor);
                }

                return Task.FromResult(result);
            }

            public Task<object?> ExecuteScalarAsync(
                string sql,
                IEnumerable<MySqlParameter>? parameters,
                CancellationToken token)
            {
                var (matched, result, descriptor) = _fixture.ResolveScalar(sql);
                if (!matched)
                {
                    LogMiss("scalar", sql);
                }
                else
                {
                    LogHit("scalar", descriptor);
                }

                return Task.FromResult(result);
            }

            public Task<DataTable> ExecuteSelectAsync(
                string sql,
                IEnumerable<MySqlParameter>? parameters,
                CancellationToken token)
            {
                var (matched, table, descriptor) = _fixture.ResolveSelect(sql);
                if (!matched)
                {
                    LogMiss("select", sql);
                }
                else
                {
                    LogHit("select", descriptor);
                }

                return Task.FromResult(table);
            }

            private void LogMiss(string type, string sql)
            {
                if (string.IsNullOrWhiteSpace(sql))
                {
                    return;
                }

                var snippet = TrimSql(sql);
                var key = $"{type}:{snippet}";
                if (_loggedMisses.TryAdd(key, 0))
                {
                    _trace?.Log(DiagLevel.Warning, Category, "FixtureMiss",
                        $"No {type} fixture matched SQL snippet: {snippet}");
                }
            }

            private void LogHit(string type, string descriptor)
            {
                if (string.IsNullOrWhiteSpace(descriptor))
                {
                    return;
                }

                var key = $"hit:{type}:{descriptor}";
                if (_loggedMisses.TryAdd(key, 0))
                {
                    _trace?.Log(DiagLevel.Debug, Category, "FixtureHit",
                        $"{type} matched fixture entry '{descriptor}'.");
                }
            }

            private static string TrimSql(string sql)
            {
                var normalized = sql.Replace('\n', ' ').Replace('\r', ' ');
                normalized = Regex.Replace(normalized, "\\s+", " ");
                return normalized.Length > 160 ? normalized.Substring(0, 160) + "…" : normalized;
            }
        }

        private sealed class DatabaseServiceTestHookAccessor
        {
            private readonly DatabaseService _database;
            private readonly PropertyInfo _nonQuery;
            private readonly PropertyInfo _scalar;
            private readonly PropertyInfo _select;
            private readonly ITrace? _trace;

            private DatabaseServiceTestHookAccessor(
                DatabaseService database,
                PropertyInfo nonQuery,
                PropertyInfo scalar,
                PropertyInfo select,
                ITrace? trace)
            {
                _database = database;
                _nonQuery = nonQuery;
                _scalar = scalar;
                _select = select;
                _trace = trace;
            }

            public static DatabaseServiceTestHookAccessor? TryCreate(DatabaseService database, ITrace? trace)
            {
                var binding = BindingFlags.Instance | BindingFlags.NonPublic;
                var type = database.GetType();

                var nonQuery = type.GetProperty("ExecuteNonQueryOverride", binding);
                var scalar = type.GetProperty("ExecuteScalarOverride", binding);
                var select = type.GetProperty("ExecuteSelectOverride", binding);

                if (nonQuery == null || scalar == null || select == null)
                {
                    trace?.Log(DiagLevel.Warning, Category, nameof(TryCreate),
                        "DatabaseService test hook properties are unavailable; stubs not applied.");
                    return null;
                }

                return new DatabaseServiceTestHookAccessor(database, nonQuery, scalar, select, trace);
            }

            public void SetExecuteNonQueryOverride(
                Func<string, IEnumerable<MySqlParameter>?, CancellationToken, Task<int>> handler)
            {
                _nonQuery.SetValue(_database, handler);
            }

            public void SetExecuteScalarOverride(
                Func<string, IEnumerable<MySqlParameter>?, CancellationToken, Task<object?>> handler)
            {
                _scalar.SetValue(_database, handler);
            }

            public void SetExecuteSelectOverride(
                Func<string, IEnumerable<MySqlParameter>?, CancellationToken, Task<DataTable>> handler)
            {
                _select.SetValue(_database, handler);
            }

            public void Reset()
            {
                _nonQuery.SetValue(_database, null);
                _scalar.SetValue(_database, null);
                _select.SetValue(_database, null);
            }
        }

        private sealed class DatabaseFixture
        {
            private readonly IReadOnlyList<FixtureNonQueryEntry> _nonQueryEntries;
            private readonly IReadOnlyList<FixtureScalarEntry> _scalarEntries;
            private readonly IReadOnlyList<FixtureSelectEntry> _selectEntries;

            private DatabaseFixture(
                string name,
                IReadOnlyList<FixtureNonQueryEntry> nonQueryEntries,
                IReadOnlyList<FixtureScalarEntry> scalarEntries,
                IReadOnlyList<FixtureSelectEntry> selectEntries)
            {
                Name = name;
                _nonQueryEntries = nonQueryEntries;
                _scalarEntries = scalarEntries;
                _selectEntries = selectEntries;
            }

            public string Name { get; }

            public static DatabaseFixture LoadFromEnvironment(ITrace? trace)
            {
                var inline = Environment.GetEnvironmentVariable(FixtureJsonVariable);
                if (!string.IsNullOrWhiteSpace(inline))
                {
                    return ParseFixture(inline, "<inline>", trace);
                }

                var path = Environment.GetEnvironmentVariable(FixturePathVariable);
                if (!string.IsNullOrWhiteSpace(path))
                {
                    try
                    {
                        path = Environment.ExpandEnvironmentVariables(path);
                        if (File.Exists(path))
                        {
                            var json = File.ReadAllText(path);
                            return ParseFixture(json, path, trace);
                        }

                        trace?.Log(DiagLevel.Warning, Category, nameof(LoadFromEnvironment),
                            $"Fixture path '{path}' does not exist. Using empty fixture.");
                    }
                    catch (Exception ex)
                    {
                        trace?.Log(DiagLevel.Warning, Category, nameof(LoadFromEnvironment),
                            $"Unable to read fixture from '{path}'. Using empty fixture.", ex);
                    }
                }

                return CreateEmpty();
            }

            public (bool matched, int result, string descriptor) ResolveNonQuery(string sql)
            {
                foreach (var entry in _nonQueryEntries)
                {
                    if (entry.Matcher.IsMatch(sql))
                    {
                        return (true, entry.Result, entry.Matcher.Description);
                    }
                }

                return (false, 0, string.Empty);
            }

            public (bool matched, object? result, string descriptor) ResolveScalar(string sql)
            {
                foreach (var entry in _scalarEntries)
                {
                    if (entry.Matcher.IsMatch(sql))
                    {
                        return (true, entry.Result, entry.Matcher.Description);
                    }
                }

                return (false, null, string.Empty);
            }

            public (bool matched, DataTable table, string descriptor) ResolveSelect(string sql)
            {
                foreach (var entry in _selectEntries)
                {
                    if (entry.Matcher.IsMatch(sql))
                    {
                        return (true, BuildDataTable(entry), entry.Matcher.Description);
                    }
                }

                return (false, new DataTable("StubResult"), string.Empty);
            }

            private static DatabaseFixture ParseFixture(string json, string source, ITrace? trace)
            {
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;
                var name = root.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String
                    ? nameElement.GetString() ?? source
                    : source;

                var nonQueryEntries = ParseNonQueryEntries(root);
                var scalarEntries = ParseScalarEntries(root);
                var selectEntries = ParseSelectEntries(root);

                trace?.Log(DiagLevel.Debug, Category, nameof(ParseFixture),
                    $"Loaded fixture '{name}' with {nonQueryEntries.Count} non-query, {scalarEntries.Count} scalar, and {selectEntries.Count} select entries.");

                return new DatabaseFixture(name, nonQueryEntries, scalarEntries, selectEntries);
            }

            private static List<FixtureNonQueryEntry> ParseNonQueryEntries(JsonElement root)
            {
                var entries = new List<FixtureNonQueryEntry>();
                if (root.TryGetProperty("nonQuery", out var array) && array.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in array.EnumerateArray())
                    {
                        var matcher = FixtureMatcher.FromJson(element);
                        if (matcher == null)
                        {
                            continue;
                        }

                        var result = element.TryGetProperty("result", out var value)
                            ? ConvertToInt(value, 0)
                            : 0;
                        entries.Add(new FixtureNonQueryEntry(matcher, result));
                    }
                }

                return entries;
            }

            private static List<FixtureScalarEntry> ParseScalarEntries(JsonElement root)
            {
                var entries = new List<FixtureScalarEntry>();
                if (root.TryGetProperty("scalar", out var array) && array.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in array.EnumerateArray())
                    {
                        var matcher = FixtureMatcher.FromJson(element);
                        if (matcher == null)
                        {
                            continue;
                        }

                        object? result = null;
                        if (element.TryGetProperty("result", out var value))
                        {
                            result = ConvertJsonElement(value);
                        }

                        entries.Add(new FixtureScalarEntry(matcher, result));
                    }
                }

                return entries;
            }

            private static List<FixtureSelectEntry> ParseSelectEntries(JsonElement root)
            {
                var entries = new List<FixtureSelectEntry>();
                if (root.TryGetProperty("select", out var array) && array.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in array.EnumerateArray())
                    {
                        var matcher = FixtureMatcher.FromJson(element);
                        if (matcher == null)
                        {
                            continue;
                        }

                        var rows = new List<IReadOnlyDictionary<string, object?>>();
                        if (element.TryGetProperty("rows", out var rowsElement) && rowsElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var row in rowsElement.EnumerateArray())
                            {
                                if (row.ValueKind != JsonValueKind.Object)
                                {
                                    continue;
                                }

                                var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                                foreach (var property in row.EnumerateObject())
                                {
                                    dict[property.Name] = ConvertJsonElement(property.Value);
                                }

                                rows.Add(dict);
                            }
                        }

                        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        if (element.TryGetProperty("columns", out var columnsElement) && columnsElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var column in columnsElement.EnumerateArray())
                            {
                                if (column.ValueKind == JsonValueKind.String)
                                {
                                    var colName = column.GetString();
                                    if (!string.IsNullOrWhiteSpace(colName))
                                    {
                                        columns.Add(colName);
                                    }
                                }
                            }
                        }

                        foreach (var row in rows)
                        {
                            foreach (var key in row.Keys)
                            {
                                columns.Add(key);
                            }
                        }

                        if (columns.Count == 0)
                        {
                            columns.Add("Value");
                        }

                        entries.Add(new FixtureSelectEntry(matcher, rows, columns.ToList()));
                    }
                }

                return entries;
            }

            private static object? ConvertJsonElement(JsonElement element)
            {
                return element.ValueKind switch
                {
                    JsonValueKind.Null => null,
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Number => ConvertNumber(element),
                    JsonValueKind.String => element.GetString(),
                    JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToList(),
                    JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
                    _ => element.GetRawText(),
                };
            }

            private static int ConvertToInt(JsonElement element, int fallback)
            {
                if (element.ValueKind != JsonValueKind.Number)
                {
                    return fallback;
                }

                if (element.TryGetInt32(out var i32))
                {
                    return i32;
                }

                if (element.TryGetInt64(out var i64))
                {
                    return (int)Math.Clamp(i64, int.MinValue, int.MaxValue);
                }

                if (element.TryGetDouble(out var dbl))
                {
                    return (int)Math.Round(dbl, MidpointRounding.AwayFromZero);
                }

                return fallback;
            }

            private static object ConvertNumber(JsonElement element)
            {
                if (element.TryGetInt64(out var i64))
                {
                    return i64;
                }

                if (element.TryGetDouble(out var dbl))
                {
                    return dbl;
                }

                return element.GetRawText();
            }

            private static DatabaseFixture CreateEmpty()
            {
                return new DatabaseFixture(
                    "empty",
                    Array.Empty<FixtureNonQueryEntry>(),
                    Array.Empty<FixtureScalarEntry>(),
                    Array.Empty<FixtureSelectEntry>());
            }

            private static DataTable BuildDataTable(FixtureSelectEntry entry)
            {
                var table = new DataTable(entry.Matcher.Description)
                {
                    Locale = System.Globalization.CultureInfo.InvariantCulture
                };

                foreach (var column in entry.Columns)
                {
                    var columnName = string.IsNullOrWhiteSpace(column) ? "Column" + table.Columns.Count : column;
                    if (!table.Columns.Contains(columnName))
                    {
                        table.Columns.Add(columnName, typeof(object));
                    }
                }

                foreach (var row in entry.Rows)
                {
                    var dataRow = table.NewRow();
                    foreach (DataColumn column in table.Columns)
                    {
                        if (row.TryGetValue(column.ColumnName, out var value) && value != null)
                        {
                            dataRow[column.ColumnName] = value;
                        }
                        else
                        {
                            dataRow[column.ColumnName] = DBNull.Value;
                        }
                    }

                    table.Rows.Add(dataRow);
                }

                return table;
            }
        }

        private sealed class FixtureNonQueryEntry
        {
            public FixtureNonQueryEntry(FixtureMatcher matcher, int result)
            {
                Matcher = matcher;
                Result = result;
            }

            public FixtureMatcher Matcher { get; }
            public int Result { get; }
        }

        private sealed class FixtureScalarEntry
        {
            public FixtureScalarEntry(FixtureMatcher matcher, object? result)
            {
                Matcher = matcher;
                Result = result;
            }

            public FixtureMatcher Matcher { get; }
            public object? Result { get; }
        }

        private sealed class FixtureSelectEntry
        {
            public FixtureSelectEntry(
                FixtureMatcher matcher,
                IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
                IReadOnlyList<string> columns)
            {
                Matcher = matcher;
                Rows = rows;
                Columns = columns;
            }

            public FixtureMatcher Matcher { get; }
            public IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; }
            public IReadOnlyList<string> Columns { get; }
        }

        private sealed class FixtureMatcher
        {
            private FixtureMatcher(string description, Func<string, bool> predicate)
            {
                Description = description;
                Predicate = predicate;
            }

            public string Description { get; }
            private Func<string, bool> Predicate { get; }

            public bool IsMatch(string sql)
            {
                if (string.IsNullOrWhiteSpace(sql))
                {
                    return false;
                }

                return Predicate(sql);
            }

            public static FixtureMatcher? FromJson(JsonElement element)
            {
                if (!element.TryGetProperty("match", out var matchElement) || matchElement.ValueKind != JsonValueKind.String)
                {
                    return null;
                }

                var matchText = matchElement.GetString();
                if (string.IsNullOrWhiteSpace(matchText))
                {
                    return null;
                }

                var useRegex = element.TryGetProperty("regex", out var regexElement)
                    && regexElement.ValueKind == JsonValueKind.True;

                if (useRegex)
                {
                    var regex = new Regex(matchText, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
                    return new FixtureMatcher($"regex:{matchText}", sql => regex.IsMatch(sql));
                }

                return new FixtureMatcher(matchText, sql =>
                    sql.IndexOf(matchText, StringComparison.OrdinalIgnoreCase) >= 0);
            }
        }
    }
}
