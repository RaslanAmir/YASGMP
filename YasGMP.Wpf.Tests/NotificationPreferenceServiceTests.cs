using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using Xunit;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.Tests;

public sealed class NotificationPreferenceServiceTests
{
    [Fact]
    public async Task SaveAsync_WhenSettingExists_UpdatesInsteadOfInserting()
    {
        // Arrange
        var operations = new List<(string Operation, string Key)>();
        var database = CreateInMemoryDatabase(operations);
        var session = new StubUserSession { UserId = 42, SessionId = "session-1" };
        var platform = new StubPlatformService();
        var service = new NotificationPreferenceService(database, session, platform);

        var initial = new NotificationPreferences
        {
            ShowStatusBarAlerts = true,
            ShowToastAlerts = false
        };

        var updated = new NotificationPreferences
        {
            ShowStatusBarAlerts = false,
            ShowToastAlerts = true
        };

        // Act
        await service.SaveAsync(initial);
        await service.SaveAsync(updated);

        // Assert
        Assert.Equal(1, CountOperations(operations, "INSERT", "notifications.ui.statusBar"));
        Assert.Equal(1, CountOperations(operations, "UPDATE", "notifications.ui.statusBar"));
        Assert.Equal(1, CountOperations(operations, "INSERT", "notifications.ui.toast"));
        Assert.Equal(1, CountOperations(operations, "UPDATE", "notifications.ui.toast"));
    }

    private static int CountOperations(IEnumerable<(string Operation, string Key)> operations, string operation, string key)
        => operations.Count(entry => string.Equals(entry.Operation, operation, StringComparison.OrdinalIgnoreCase)
                                      && string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase));

    private static DatabaseService CreateInMemoryDatabase(List<(string Operation, string Key)> operations)
    {
        var database = new DatabaseService("Server=localhost;Database=test;Uid=root;Pwd=pass;");
        var settings = new Dictionary<string, Setting>(StringComparer.OrdinalIgnoreCase);
        var nextId = 1;
        var lastInsertId = 0;

        SetOverride(database, "ExecuteSelectOverride",
            new Func<string, IEnumerable<MySqlParameter>?, CancellationToken, Task<DataTable>>((sql, _, _) =>
            {
                if (sql.Contains("FROM settings", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(BuildSettingsTable(settings.Values));
                }

                return Task.FromResult(new DataTable());
            }));

        SetOverride(database, "ExecuteNonQueryOverride",
            new Func<string, IEnumerable<MySqlParameter>?, CancellationToken, Task<int>>((sql, parameters, _) =>
            {
                var lookup = ToDictionary(parameters);
                var key = lookup.TryGetValue("@key", out var keyValue)
                    ? Convert.ToString(keyValue, CultureInfo.InvariantCulture) ?? string.Empty
                    : string.Empty;

                if (sql.StartsWith("INSERT INTO settings", StringComparison.OrdinalIgnoreCase))
                {
                    operations.Add(("INSERT", key));
                    var setting = CreateSettingFromParameters(lookup);
                    setting.Id = nextId++;
                    if (!string.IsNullOrWhiteSpace(setting.Key))
                    {
                        settings[setting.Key] = setting;
                    }
                    lastInsertId = setting.Id;
                }
                else if (sql.StartsWith("UPDATE settings", StringComparison.OrdinalIgnoreCase))
                {
                    operations.Add(("UPDATE", key));
                    var setting = CreateSettingFromParameters(lookup);
                    if (lookup.TryGetValue("@id", out var idValue))
                    {
                        setting.Id = Convert.ToInt32(idValue, CultureInfo.InvariantCulture);
                    }

                    if (!string.IsNullOrWhiteSpace(setting.Key))
                    {
                        var existing = settings.FirstOrDefault(entry => entry.Value.Id == setting.Id);
                        if (!string.IsNullOrEmpty(existing.Key)
                            && !string.Equals(existing.Key, setting.Key, StringComparison.OrdinalIgnoreCase))
                        {
                            settings.Remove(existing.Key);
                        }
                        settings[setting.Key] = setting;
                    }
                    else
                    {
                        var existing = settings.FirstOrDefault(entry => entry.Value.Id == setting.Id);
                        if (!string.IsNullOrEmpty(existing.Key))
                        {
                            settings[existing.Key] = setting;
                        }
                    }
                }

                return Task.FromResult(1);
            }));

        SetOverride(database, "ExecuteScalarOverride",
            new Func<string, IEnumerable<MySqlParameter>?, CancellationToken, Task<object?>>((sql, _, _) =>
            {
                if (sql.StartsWith("SELECT LAST_INSERT_ID()", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult<object?>(lastInsertId);
                }

                return Task.FromResult<object?>(null);
            }));

        return database;
    }

    private static Dictionary<string, object?> ToDictionary(IEnumerable<MySqlParameter>? parameters)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (parameters is null)
        {
            return dict;
        }

        foreach (var parameter in parameters)
        {
            dict[parameter.ParameterName] = parameter.Value;
        }

        return dict;
    }

    private static Setting CreateSettingFromParameters(IReadOnlyDictionary<string, object?> parameters)
    {
        return new Setting
        {
            Key = GetString(parameters, "@key"),
            Value = GetString(parameters, "@val"),
            DefaultValue = GetString(parameters, "@def"),
            ValueType = GetString(parameters, "@type"),
            MinValue = GetString(parameters, "@min"),
            MaxValue = GetString(parameters, "@max"),
            Description = GetString(parameters, "@desc"),
            Category = GetString(parameters, "@cat"),
            Subcategory = GetString(parameters, "@sub"),
            IsSensitive = GetBool(parameters, "@sens"),
            IsGlobal = GetBool(parameters, "@glob"),
            Status = GetString(parameters, "@status"),
            UpdatedById = GetNullableInt(parameters, "@updby"),
        };
    }

    private static string GetString(IReadOnlyDictionary<string, object?> parameters, string key)
        => parameters.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty : string.Empty;

    private static bool GetBool(IReadOnlyDictionary<string, object?> parameters, string key)
    {
        if (!parameters.TryGetValue(key, out var value) || value is null)
        {
            return false;
        }

        return value switch
        {
            bool b => b,
            string s when bool.TryParse(s, out var parsed) => parsed,
            _ => Convert.ToBoolean(value, CultureInfo.InvariantCulture)
        };
    }

    private static int? GetNullableInt(IReadOnlyDictionary<string, object?> parameters, string key)
    {
        if (!parameters.TryGetValue(key, out var value) || value is null || value is DBNull)
        {
            return null;
        }

        return Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    private static DataTable BuildSettingsTable(IEnumerable<Setting> settings)
    {
        var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("key", typeof(string));
        table.Columns.Add("value", typeof(string));
        table.Columns.Add("default_value", typeof(string));
        table.Columns.Add("value_type", typeof(string));
        table.Columns.Add("min_value", typeof(string));
        table.Columns.Add("max_value", typeof(string));
        table.Columns.Add("description", typeof(string));
        table.Columns.Add("category", typeof(string));
        table.Columns.Add("subcategory", typeof(string));
        table.Columns.Add("is_sensitive", typeof(bool));
        table.Columns.Add("is_global", typeof(bool));
        var userId = table.Columns.Add("user_id", typeof(int));
        userId.AllowDBNull = true;
        var roleId = table.Columns.Add("role_id", typeof(int));
        roleId.AllowDBNull = true;
        var approvedBy = table.Columns.Add("approved_by_id", typeof(int));
        approvedBy.AllowDBNull = true;
        var approvedAt = table.Columns.Add("approved_at", typeof(DateTime));
        approvedAt.AllowDBNull = true;
        table.Columns.Add("digital_signature", typeof(string));
        table.Columns.Add("status", typeof(string));
        var updatedAt = table.Columns.Add("updated_at", typeof(DateTime));
        updatedAt.AllowDBNull = true;
        var updatedBy = table.Columns.Add("updated_by_id", typeof(int));
        updatedBy.AllowDBNull = true;
        var expiryDate = table.Columns.Add("expiry_date", typeof(DateTime));
        expiryDate.AllowDBNull = true;

        foreach (var setting in settings)
        {
            var row = table.NewRow();
            row["id"] = setting.Id;
            row["key"] = setting.Key ?? string.Empty;
            row["value"] = setting.Value ?? string.Empty;
            row["default_value"] = setting.DefaultValue ?? string.Empty;
            row["value_type"] = setting.ValueType ?? string.Empty;
            row["min_value"] = setting.MinValue ?? string.Empty;
            row["max_value"] = setting.MaxValue ?? string.Empty;
            row["description"] = setting.Description ?? string.Empty;
            row["category"] = setting.Category ?? string.Empty;
            row["subcategory"] = setting.Subcategory ?? string.Empty;
            row["is_sensitive"] = setting.IsSensitive;
            row["is_global"] = setting.IsGlobal;
            row["user_id"] = setting.UserId.HasValue ? setting.UserId.Value : DBNull.Value;
            row["role_id"] = setting.RoleId.HasValue ? setting.RoleId.Value : DBNull.Value;
            row["approved_by_id"] = setting.ApprovedById.HasValue ? setting.ApprovedById.Value : DBNull.Value;
            row["approved_at"] = setting.ApprovedAt.HasValue ? setting.ApprovedAt.Value : DBNull.Value;
            row["digital_signature"] = setting.DigitalSignature ?? string.Empty;
            row["status"] = setting.Status ?? string.Empty;
            row["updated_at"] = setting.UpdatedAt ?? DateTime.UtcNow;
            row["updated_by_id"] = setting.UpdatedById.HasValue ? setting.UpdatedById.Value : DBNull.Value;
            row["expiry_date"] = setting.ExpiryDate.HasValue ? setting.ExpiryDate.Value : DBNull.Value;
            table.Rows.Add(row);
        }

        return table;
    }

    private static void SetOverride(DatabaseService database, string propertyName, object value)
    {
        var property = typeof(DatabaseService).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMemberException(nameof(DatabaseService), propertyName);
        property.SetValue(database, value);
    }

    private sealed class StubUserSession : IUserSession
    {
        public User? CurrentUser => null;
        public int? UserId { get; init; }
        public string? Username => null;
        public string? FullName => null;
        public string SessionId { get; init; } = string.Empty;
    }

    private sealed class StubPlatformService : IPlatformService
    {
        public string GetLocalIpAddress() => "127.0.0.1";
        public string GetOsVersion() => "TestOS";
        public string GetHostName() => "test-host";
        public string GetUserName() => "tester";
    }
}
