using System;
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
        string? capturedSql = null;
        MySqlParameter[]? capturedParameters = null;

        db.ExecuteNonQueryOverride = (sql, parameters, _) =>
        {
            capturedSql = sql;
            capturedParameters = parameters?.OfType<MySqlParameter>().ToArray();
            return Task.FromResult(1);
        };

        var geometry = new DatabaseServiceLayoutsExtensions.UserWindowLayoutGeometry(null, 20.0, null, 720.0);

        try
        {
            await db.SaveUserWindowLayoutAsync(11, "Shell", null, geometry).ConfigureAwait(false);

            const string expectedSql = "INSERT INTO user_window_layouts (user_id, page_type, layout_xml, pos_x, pos_y, width, height, saved_at, created_at, updated_at)\nVALUES (@u, @p, @layout, @x, @y, @w, @h, UTC_TIMESTAMP(), UTC_TIMESTAMP(), UTC_TIMESTAMP())\nON DUPLICATE KEY UPDATE\n    layout_xml = VALUES(layout_xml),\n    pos_x = VALUES(pos_x),\n    pos_y = VALUES(pos_y),\n    width = VALUES(width),\n    height = VALUES(height),\n    saved_at = UTC_TIMESTAMP(),\n    updated_at = UTC_TIMESTAMP(),\n    created_at = COALESCE(created_at, VALUES(created_at));";
            Assert.Equal(expectedSql, capturedSql);

            Assert.NotNull(capturedParameters);
            Assert.Collection(
                capturedParameters!,
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

        db.ExecuteNonQueryOverride = (_, _, _) => Task.FromException<int>(expected);

        try
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                db.SaveUserWindowLayoutAsync(5, "Shell", "<layout />", null)).ConfigureAwait(false);

            Assert.Same(expected, exception);
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
        string? capturedSql = null;
        MySqlParameter[]? capturedParameters = null;

        db.ExecuteNonQueryOverride = (sql, parameters, _) =>
        {
            capturedSql = sql;
            capturedParameters = parameters?.OfType<MySqlParameter>().ToArray();
            return Task.FromResult(1);
        };

        try
        {
            await db.DeleteUserWindowLayoutAsync(3, "Reset.Key").ConfigureAwait(false);

            Assert.Equal("DELETE FROM user_window_layouts WHERE user_id=@u AND page_type=@p;", capturedSql);
            Assert.NotNull(capturedParameters);
            Assert.Collection(
                capturedParameters!,
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

        db.ExecuteNonQueryOverride = (_, _, _) => Task.FromException<int>(expected);

        try
        {
            var exception = await Assert.ThrowsAsync<TimeoutException>(() =>
                db.DeleteUserWindowLayoutAsync(9, "Shell")).ConfigureAwait(false);

            Assert.Same(expected, exception);
        }
        finally
        {
            db.ResetTestOverrides();
        }
    }
}
