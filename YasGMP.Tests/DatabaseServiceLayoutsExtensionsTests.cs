using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using MySqlConnector;
using Xunit;
using YasGMP.Services;

namespace YasGMP.Tests;

public class DatabaseServiceLayoutsExtensionsTests
{
    private const string ConnectionString = "Server=localhost;User Id=test;Password=test;Database=test;";

    [Fact]
    public async Task GetUserWindowLayoutAsync_EmitsExpectedSql_AndMapsNullableGeometry()
    {
        var db = new DatabaseService(ConnectionString);
        string? capturedSql = null;
        MySqlParameter[]? capturedParameters = null;
        var now = DateTime.UtcNow;

        var table = new DataTable();
        table.Columns.Add("layout_xml", typeof(string));
        table.Columns.Add("pos_x", typeof(double));
        table.Columns.Add("pos_y", typeof(double));
        table.Columns.Add("width", typeof(double));
        table.Columns.Add("height", typeof(double));
        table.Columns.Add("saved_at", typeof(DateTime));
        table.Columns.Add("created_at", typeof(DateTime));
        table.Columns.Add("updated_at", typeof(DateTime));
        table.Rows.Add(DBNull.Value, 15.5, DBNull.Value, 800.0, DBNull.Value, now, now.AddDays(-1), now.AddMinutes(-30));

        db.ExecuteSelectOverride = (sql, parameters, _) =>
        {
            capturedSql = sql;
            capturedParameters = parameters?.OfType<MySqlParameter>().ToArray();
            return Task.FromResult(table);
        };

        try
        {
            var snapshot = await db.GetUserWindowLayoutAsync(7, "YasGmp.Wpf.Shell").ConfigureAwait(false);

            Assert.Equal(
                "SELECT layout_xml, pos_x, pos_y, width, height, saved_at, created_at, updated_at\nFROM user_window_layouts WHERE user_id=@u AND page_type=@p LIMIT 1;",
                capturedSql);

            Assert.NotNull(capturedParameters);
            Assert.Collection(
                capturedParameters!,
                p =>
                {
                    Assert.Equal("@u", p.ParameterName);
                    Assert.Equal(7, p.Value);
                },
                p =>
                {
                    Assert.Equal("@p", p.ParameterName);
                    Assert.Equal("YasGmp.Wpf.Shell", p.Value);
                });

            Assert.NotNull(snapshot);
            Assert.Null(snapshot!.Value.LayoutXml);
            Assert.Equal(15.5, snapshot.Value.Geometry.Left);
            Assert.Null(snapshot.Value.Geometry.Top);
            Assert.Equal(800.0, snapshot.Value.Geometry.Width);
            Assert.Null(snapshot.Value.Geometry.Height);
            Assert.Equal(now, snapshot.Value.SavedAt);
            Assert.Equal(now.AddDays(-1), snapshot.Value.CreatedAt);
            Assert.Equal(now.AddMinutes(-30), snapshot.Value.UpdatedAt);
        }
        finally
        {
            db.ResetTestOverrides();
        }
    }

    [Fact]
    public async Task GetUserWindowLayoutAsync_ReturnsNullWhenNoRows()
    {
        var db = new DatabaseService(ConnectionString);
        db.ExecuteSelectOverride = (_, _, _) =>
        {
            var empty = new DataTable();
            empty.Columns.Add("layout_xml", typeof(string));
            empty.Columns.Add("pos_x", typeof(double));
            empty.Columns.Add("pos_y", typeof(double));
            empty.Columns.Add("width", typeof(double));
            empty.Columns.Add("height", typeof(double));
            empty.Columns.Add("saved_at", typeof(DateTime));
            empty.Columns.Add("created_at", typeof(DateTime));
            empty.Columns.Add("updated_at", typeof(DateTime));
            return Task.FromResult(empty);
        };

        try
        {
            var snapshot = await db.GetUserWindowLayoutAsync(99, "Shell").ConfigureAwait(false);
            Assert.Null(snapshot);
        }
        finally
        {
            db.ResetTestOverrides();
        }
    }

    [Fact]
    public async Task GetUserWindowLayoutAsync_PropagatesDatabaseFailures()
    {
        var db = new DatabaseService(ConnectionString);
        var expected = new DataException("select fail");

        db.ExecuteSelectOverride = (_, _, _) => Task.FromException<DataTable>(expected);

        try
        {
            var exception = await Assert.ThrowsAsync<DataException>(() =>
                db.GetUserWindowLayoutAsync(1, "Shell")).ConfigureAwait(false);

            Assert.Same(expected, exception);
        }
        finally
        {
            db.ResetTestOverrides();
        }
    }

    [Fact]
    public async Task SaveUserWindowLayoutAsync_EmitsExpectedSql_AndParameters()
    {
        var db = new DatabaseService(ConnectionString);
        var commands = new List<(string Sql, MySqlParameter[] Parameters)>();

        db.ExecuteNonQueryOverride = (sql, parameters, _) =>
        {
            commands.Add((sql, parameters?.OfType<MySqlParameter>().ToArray() ?? Array.Empty<MySqlParameter>()));
            return Task.FromResult(1);
        };

        var geometry = new DatabaseServiceLayoutsExtensions.UserWindowLayoutGeometry(null, 20.0, null, 720.0);
        var audit = new DatabaseServiceLayoutsExtensions.UserWindowLayoutAuditContext(
            "10.0.0.1",
            "Device-1",
            "sess-123",
            77,
            "hash-abc");

        try
        {
            await db.SaveUserWindowLayoutAsync(11, "Shell", null, geometry, audit).ConfigureAwait(false);

            const string expectedSql = "INSERT INTO user_window_layouts (user_id, page_type, layout_xml, pos_x, pos_y, width, height, saved_at, created_at, updated_at)\nVALUES (@u, @p, @layout, @x, @y, @w, @h, UTC_TIMESTAMP(), UTC_TIMESTAMP(), UTC_TIMESTAMP())\nON DUPLICATE KEY UPDATE\n    layout_xml = VALUES(layout_xml),\n    pos_x = VALUES(pos_x),\n    pos_y = VALUES(pos_y),\n    width = VALUES(width),\n    height = VALUES(height),\n    saved_at = UTC_TIMESTAMP(),\n    updated_at = UTC_TIMESTAMP(),\n    created_at = COALESCE(created_at, VALUES(created_at));";
            Assert.Equal(2, commands.Count);

            var upsert = commands[0];
            Assert.Equal(expectedSql, upsert.Sql);
            Assert.Collection(
                upsert.Parameters,
                p =>
                {
                    Assert.Equal("@u", p.ParameterName);
                    Assert.Equal(11, p.Value);
                },
                p =>
                {
                    Assert.Equal("@p", p.ParameterName);
                    Assert.Equal("Shell", p.Value);
                },
                p =>
                {
                    Assert.Equal("@layout", p.ParameterName);
                    Assert.Equal(DBNull.Value, p.Value);
                },
                p =>
                {
                    Assert.Equal("@x", p.ParameterName);
                    Assert.Equal(DBNull.Value, p.Value);
                },
                p =>
                {
                    Assert.Equal("@y", p.ParameterName);
                    Assert.Equal(20.0, p.Value);
                },
                p =>
                {
                    Assert.Equal("@w", p.ParameterName);
                    Assert.Equal(DBNull.Value, p.Value);
                },
                p =>
                {
                    Assert.Equal("@h", p.ParameterName);
                    Assert.Equal(720.0, p.Value);
                });

            var auditCommand = commands[1];
            Assert.StartsWith("INSERT INTO system_event_log", auditCommand.Sql, StringComparison.Ordinal);
            Assert.Contains(auditCommand.Parameters, p => p.ParameterName == "@etype" && (string?)p.Value == "LAYOUT_SAVE");
            Assert.Contains(auditCommand.Parameters, p => p.ParameterName == "@sid" && (string?)p.Value == "sess-123");
            Assert.Contains(auditCommand.Parameters, p => p.ParameterName == "@ip" && (string?)p.Value == "10.0.0.1");
            Assert.Contains(auditCommand.Parameters, p => p.ParameterName == "@dev" && (string?)p.Value == "Device-1");
            Assert.Contains(auditCommand.Parameters, p => p.ParameterName == "@sev" && (string?)p.Value == "audit");
            Assert.Contains(auditCommand.Parameters, p => p.ParameterName == "@sigId" && (object?)p.Value == 77);
            Assert.Contains(auditCommand.Parameters, p => p.ParameterName == "@sigHash" && (string?)p.Value == "hash-abc");
            var descParam = Assert.Single(auditCommand.Parameters, p => p.ParameterName == "@desc");
            Assert.Contains("page=Shell", descParam.Value?.ToString());
        }
        finally
        {
            db.ResetTestOverrides();
        }
    }

    [Fact]
    public async Task SaveUserWindowLayoutAsync_PropagatesDatabaseFailures()
    {
        var db = new DatabaseService(ConnectionString);
        var expected = new InvalidOperationException("non query fail");

        int attempt = 0;
        db.ExecuteNonQueryOverride = (_, _, _) =>
        {
            attempt++;
            return Task.FromException<int>(expected);
        };

        try
        {
            var geometry = new DatabaseServiceLayoutsExtensions.UserWindowLayoutGeometry(1, 2, 3, 4);
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                db.SaveUserWindowLayoutAsync(5, "Shell", "<layout />", geometry)).ConfigureAwait(false);

            Assert.Same(expected, exception);
            Assert.Equal(1, attempt);
        }
        finally
        {
            db.ResetTestOverrides();
        }
    }

    [Fact]
    public async Task DeleteUserWindowLayoutAsync_EmitsExpectedSql_AndParameters()
    {
        var db = new DatabaseService(ConnectionString);
        var commands = new List<(string Sql, MySqlParameter[] Parameters)>();

        db.ExecuteNonQueryOverride = (sql, parameters, _) =>
        {
            commands.Add((sql, parameters?.OfType<MySqlParameter>().ToArray() ?? Array.Empty<MySqlParameter>()));
            return Task.FromResult(1);
        };

        try
        {
            var audit = new DatabaseServiceLayoutsExtensions.UserWindowLayoutAuditContext("127.0.0.1", "Device", "sess-1");
            await db.DeleteUserWindowLayoutAsync(3, "Reset.Key", audit).ConfigureAwait(false);

            Assert.Equal(2, commands.Count);
            var deleteCommand = commands[0];
            Assert.Equal("DELETE FROM user_window_layouts WHERE user_id=@u AND page_type=@p;", deleteCommand.Sql);
            Assert.Collection(
                deleteCommand.Parameters,
                p =>
                {
                    Assert.Equal("@u", p.ParameterName);
                    Assert.Equal(3, p.Value);
                },
                p =>
                {
                    Assert.Equal("@p", p.ParameterName);
                    Assert.Equal("Reset.Key", p.Value);
                });

            var auditCommand = commands[1];
            Assert.StartsWith("INSERT INTO system_event_log", auditCommand.Sql, StringComparison.Ordinal);
            Assert.Contains(auditCommand.Parameters, p => p.ParameterName == "@etype" && (string?)p.Value == "LAYOUT_RESET");
            Assert.Contains(auditCommand.Parameters, p => p.ParameterName == "@sid" && (string?)p.Value == "sess-1");
        }
        finally
        {
            db.ResetTestOverrides();
        }
    }

    [Fact]
    public async Task DeleteUserWindowLayoutAsync_PropagatesDatabaseFailures()
    {
        var db = new DatabaseService(ConnectionString);
        var expected = new TimeoutException("delete fail");

        int attempt = 0;
        db.ExecuteNonQueryOverride = (_, _, _) =>
        {
            attempt++;
            return Task.FromException<int>(expected);
        };

        try
        {
            var exception = await Assert.ThrowsAsync<TimeoutException>(() =>
                db.DeleteUserWindowLayoutAsync(9, "Shell")).ConfigureAwait(false);

            Assert.Same(expected, exception);
            Assert.Equal(1, attempt);
        }
        finally
        {
            db.ResetTestOverrides();
        }
    }
}
