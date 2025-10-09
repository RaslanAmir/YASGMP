using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using AvalonDock;
using AvalonDock.Layout;
using AvalonDock.Layout.Serialization;
using MySqlConnector;
using Xunit;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.Tests;

public class DockLayoutPersistenceServiceTests
{
    private const string ConnectionString = "Server=localhost;Database=test;Uid=test;Pwd=test;";

    [Fact]
    public async Task LoadAsync_DelegatesToDatabaseService()
    {
        var database = new DatabaseService(ConnectionString);
        var session = new StubUserSession(42);
        string? capturedSql = null;
        MySqlParameter[]? capturedParameters = null;

        SetExecuteSelectOverride(database, (sql, parameters, _) =>
        {
            capturedSql = sql;
            capturedParameters = parameters?.OfType<MySqlParameter>().ToArray();

            var table = new DataTable();
            table.Columns.Add("layout_xml", typeof(string));
            table.Columns.Add("pos_x", typeof(double));
            table.Columns.Add("pos_y", typeof(double));
            table.Columns.Add("width", typeof(double));
            table.Columns.Add("height", typeof(double));
            table.Columns.Add("saved_at", typeof(DateTime));
            table.Columns.Add("created_at", typeof(DateTime));
            table.Columns.Add("updated_at", typeof(DateTime));
            table.Rows.Add("<layout />", 10.0, 20.0, 1024.0, 768.0, DateTime.UtcNow, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);
            return Task.FromResult(table);
        });

        try
        {
            var auth = new StubAuthContext();
            var service = new DockLayoutPersistenceService(database, session, auth);
            var snapshot = await service.LoadAsync("Shell", CancellationToken.None).ConfigureAwait(false);

            Assert.NotNull(snapshot);
            Assert.Equal("<layout />", snapshot!.Value.LayoutXml);
            Assert.Equal(10.0, snapshot.Value.Left);
            Assert.Equal(20.0, snapshot.Value.Top);
            Assert.Equal(1024.0, snapshot.Value.Width);
            Assert.Equal(768.0, snapshot.Value.Height);

            Assert.Equal(
                "SELECT layout_xml, pos_x, pos_y, width, height, saved_at, created_at, updated_at\nFROM user_window_layouts WHERE user_id=@u AND page_type=@p LIMIT 1;",
                capturedSql);
            Assert.Collection(
                capturedParameters ?? Array.Empty<MySqlParameter>(),
                p =>
                {
                    Assert.Equal("@u", p.ParameterName);
                    Assert.Equal(42, p.Value);
                },
                p =>
                {
                    Assert.Equal("@p", p.ParameterName);
                    Assert.Equal("Shell", p.Value);
                });
        }
        finally
        {
            ResetOverrides(database);
        }
    }

    [Fact]
    public async Task LoadAsync_PropagatesDatabaseFailures()
    {
        var database = new DatabaseService(ConnectionString);
        var session = new StubUserSession(55);
        var expected = new InvalidOperationException("boom");

        SetExecuteSelectOverride(database, (_, _, _) => Task.FromException<DataTable>(expected));

        try
        {
            var auth = new StubAuthContext();
            var service = new DockLayoutPersistenceService(database, session, auth);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.LoadAsync("Shell", CancellationToken.None)).ConfigureAwait(false);

            Assert.Same(expected, exception);
        }
        finally
        {
            ResetOverrides(database);
        }
    }

    [Fact]
    public async Task LoadAsync_ReturnsNullWhenDatabaseReturnsNoRows()
    {
        var database = new DatabaseService(ConnectionString);
        var session = new StubUserSession(56);

        SetExecuteSelectOverride(database, (_, _, _) =>
        {
            var table = new DataTable();
            table.Columns.Add("layout_xml", typeof(string));
            table.Columns.Add("pos_x", typeof(double));
            table.Columns.Add("pos_y", typeof(double));
            table.Columns.Add("width", typeof(double));
            table.Columns.Add("height", typeof(double));
            table.Columns.Add("saved_at", typeof(DateTime));
            table.Columns.Add("created_at", typeof(DateTime));
            table.Columns.Add("updated_at", typeof(DateTime));
            return Task.FromResult(table);
        });

        try
        {
            var auth = new StubAuthContext();
            var service = new DockLayoutPersistenceService(database, session, auth);
            var snapshot = await service.LoadAsync("Shell", CancellationToken.None).ConfigureAwait(false);

            Assert.Null(snapshot);
        }
        finally
        {
            ResetOverrides(database);
        }
    }

    [Fact]
    public async Task SaveAsync_DelegatesToDatabaseService_WithNullableGeometry()
    {
        var database = new DatabaseService(ConnectionString);
        var session = new StubUserSession(77, "session-77");
        var auth = new StubAuthContext("192.168.1.50", "DeviceInfo-77");
        var commands = new List<(string Sql, MySqlParameter[] Parameters)>();

        SetExecuteNonQueryOverride(database, (sql, parameters, _) =>
        {
            commands.Add((sql, parameters?.OfType<MySqlParameter>().ToArray() ?? Array.Empty<MySqlParameter>()));
            return Task.FromResult(1);
        });

        try
        {
            var service = new DockLayoutPersistenceService(database, session, auth);
            var geometry = new WindowGeometry(null, 64.0, 1400.0, null);
            await service.SaveAsync("Shell", "<layoutXml />", geometry, CancellationToken.None).ConfigureAwait(false);

            const string expectedSql = "INSERT INTO user_window_layouts (user_id, page_type, layout_xml, pos_x, pos_y, width, height, saved_at, created_at, updated_at)\nVALUES (@u, @p, @layout, @x, @y, @w, @h, UTC_TIMESTAMP(), UTC_TIMESTAMP(), UTC_TIMESTAMP())\nON DUPLICATE KEY UPDATE\n    layout_xml = VALUES(layout_xml),\n    pos_x = VALUES(pos_x),\n    pos_y = VALUES(pos_y),\n    width = VALUES(width),\n    height = VALUES(height),\n    saved_at = UTC_TIMESTAMP(),\n    updated_at = UTC_TIMESTAMP(),\n    created_at = COALESCE(created_at, VALUES(created_at));";
            Assert.Equal(2, commands.Count);

            var upsert = commands[0];
            Assert.Equal(expectedSql, upsert.Sql);
            Assert.Collection(
                upsert.Parameters,
                p =>
                {
                    Assert.Equal("@u", p.ParameterName);
                    Assert.Equal(77, p.Value);
                },
                p =>
                {
                    Assert.Equal("@p", p.ParameterName);
                    Assert.Equal("Shell", p.Value);
                },
                p =>
                {
                    Assert.Equal("@layout", p.ParameterName);
                    Assert.Equal("<layoutXml />", p.Value);
                    Assert.Equal(MySqlDbType.LongText, p.MySqlDbType);
                },
                p =>
                {
                    Assert.Equal("@x", p.ParameterName);
                    Assert.Equal(DBNull.Value, p.Value);
                },
                p =>
                {
                    Assert.Equal("@y", p.ParameterName);
                    Assert.Equal(64.0, p.Value);
                },
                p =>
                {
                    Assert.Equal("@w", p.ParameterName);
                    Assert.Equal(1400.0, p.Value);
                },
                p =>
                {
                    Assert.Equal("@h", p.ParameterName);
                    Assert.Equal(DBNull.Value, p.Value);
                });

            var auditCommand = commands[1];
            Assert.StartsWith("INSERT INTO system_event_log", auditCommand.Sql, StringComparison.Ordinal);
            Assert.Contains(auditCommand.Parameters, p => p.ParameterName == "@etype" && (string?)p.Value == "LAYOUT_SAVE");
            Assert.Contains(auditCommand.Parameters, p => p.ParameterName == "@sid" && (string?)p.Value == "session-77");
            Assert.Contains(auditCommand.Parameters, p => p.ParameterName == "@ip" && (string?)p.Value == "192.168.1.50");
            Assert.Contains(auditCommand.Parameters, p => p.ParameterName == "@dev" && (string?)p.Value == "DeviceInfo-77");
            Assert.Contains(auditCommand.Parameters, p => p.ParameterName == "@sev" && (string?)p.Value == "audit");
        }
        finally
        {
            ResetOverrides(database);
        }
    }

    [Fact]
    public async Task SaveAsync_PropagatesDatabaseFailures()
    {
        var database = new DatabaseService(ConnectionString);
        var session = new StubUserSession(78, "session-78");
        var auth = new StubAuthContext();
        var expected = new DataException("save failed");
        int attempts = 0;

        SetExecuteNonQueryOverride(database, (_, _, _) =>
        {
            attempts++;
            return Task.FromException<int>(expected);
        });

        try
        {
            var service = new DockLayoutPersistenceService(database, session, auth);

            var exception = await Assert.ThrowsAsync<DataException>(() =>
                service.SaveAsync("Shell", "<layout />", default(WindowGeometry), CancellationToken.None)).ConfigureAwait(false);

            Assert.Same(expected, exception);
            Assert.Equal(1, attempts);
        }
        finally
        {
            ResetOverrides(database);
        }
    }

    [Fact]
    public async Task ResetAsync_DelegatesToDatabaseService()
    {
        var database = new DatabaseService(ConnectionString);
        var session = new StubUserSession(91, "session-91");
        var auth = new StubAuthContext("10.0.0.5", "DeviceInfo-91");
        var commands = new List<(string Sql, MySqlParameter[] Parameters)>();

        SetExecuteNonQueryOverride(database, (sql, parameters, _) =>
        {
            commands.Add((sql, parameters?.OfType<MySqlParameter>().ToArray() ?? Array.Empty<MySqlParameter>()));
            return Task.FromResult(1);
        });

        try
        {
            var service = new DockLayoutPersistenceService(database, session, auth);
            await service.ResetAsync("Shell", CancellationToken.None).ConfigureAwait(false);

            Assert.Equal(2, commands.Count);
            var deleteCommand = commands[0];
            Assert.Equal("DELETE FROM user_window_layouts WHERE user_id=@u AND page_type=@p;", deleteCommand.Sql);
            Assert.Collection(
                deleteCommand.Parameters,
                p =>
                {
                    Assert.Equal("@u", p.ParameterName);
                    Assert.Equal(91, p.Value);
                },
                p =>
                {
                    Assert.Equal("@p", p.ParameterName);
                    Assert.Equal("Shell", p.Value);
                });

            var auditCommand = commands[1];
            Assert.StartsWith("INSERT INTO system_event_log", auditCommand.Sql, StringComparison.Ordinal);
            Assert.Contains(auditCommand.Parameters, p => p.ParameterName == "@etype" && (string?)p.Value == "LAYOUT_RESET");
            Assert.Contains(auditCommand.Parameters, p => p.ParameterName == "@sid" && (string?)p.Value == "session-91");
            Assert.Contains(auditCommand.Parameters, p => p.ParameterName == "@ip" && (string?)p.Value == "10.0.0.5");
        }
        finally
        {
            ResetOverrides(database);
        }
    }

    [Fact]
    public async Task ResetAsync_PropagatesDatabaseFailures()
    {
        var database = new DatabaseService(ConnectionString);
        var session = new StubUserSession(92, "session-92");
        var auth = new StubAuthContext();
        var expected = new TimeoutException("reset failed");
        int attempts = 0;

        SetExecuteNonQueryOverride(database, (_, _, _) =>
        {
            attempts++;
            return Task.FromException<int>(expected);
        });

        try
        {
            var service = new DockLayoutPersistenceService(database, session, auth);

            var exception = await Assert.ThrowsAsync<TimeoutException>(() =>
                service.ResetAsync("Shell", CancellationToken.None)).ConfigureAwait(false);

            Assert.Same(expected, exception);
            Assert.Equal(1, attempts);
        }
        finally
        {
            ResetOverrides(database);
        }
    }

    [Fact]
    public async Task ShellLayoutController_ResetLayoutAsync_InvokesResetBeforeSave()
    {
        await RunOnStaThread(async () =>
        {
            var database = new DatabaseService(ConnectionString);
            var session = new StubUserSession(101, "session-101");
            var auth = new StubAuthContext();
            var persistence = new DockLayoutPersistenceService(database, session, auth);
            var localization = new LocalizationService();
            var controller = new ShellLayoutController(persistence, localization);

            var dockManager = new DockingManager();
            var layoutRoot = new LayoutRoot();
            var layoutPanel = new LayoutPanel();
            layoutPanel.Children.Add(new LayoutDocumentPane());
            layoutRoot.RootPanel = layoutPanel;
            dockManager.Layout = layoutRoot;

            var serializer = new XmlLayoutSerializer(dockManager);
            string defaultLayout;
            using (var writer = new System.IO.StringWriter())
            {
                serializer.Serialize(writer);
                defaultLayout = writer.ToString();
            }

            SetPrivateField(controller, "_dockManager", dockManager);
            SetPrivateField(controller, "_defaultLayout", defaultLayout);

            var window = new Window
            {
                Width = 1280,
                Height = 720,
                Left = 100,
                Top = 100
            };

            var sqlCalls = new List<string>();
            SetExecuteNonQueryOverride(database, (sql, parameters, _) =>
            {
                sqlCalls.Add(sql);
                return Task.FromResult(1);
            });

            try
            {
                await controller.ResetLayoutAsync(window, CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(4, sqlCalls.Count);
                Assert.Equal("DELETE FROM user_window_layouts WHERE user_id=@u AND page_type=@p;", sqlCalls[0]);
                Assert.StartsWith("INSERT INTO system_event_log", sqlCalls[1], StringComparison.Ordinal);
                Assert.StartsWith("INSERT INTO user_window_layouts", sqlCalls[2], StringComparison.Ordinal);
                Assert.StartsWith("INSERT INTO system_event_log", sqlCalls[3], StringComparison.Ordinal);
            }
            finally
            {
                window.Close();
                ResetOverrides(database);
            }
        });
    }

    private static Task RunOnStaThread(Func<Task> action)
    {
        var tcs = new TaskCompletionSource<object?>();
        var thread = new Thread(() =>
        {
            try
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
                var dispatcher = Dispatcher.CurrentDispatcher;
                dispatcher.InvokeAsync(async () =>
                {
                    try
                    {
                        await action().ConfigureAwait(true);
                        tcs.SetResult(null);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                    finally
                    {
                        dispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
                    }
                });
                Dispatcher.Run();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        })
        {
            IsBackground = true
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return tcs.Task;
    }

    private static void SetExecuteSelectOverride(DatabaseService database, Func<string, IEnumerable<MySqlParameter>?, CancellationToken, Task<DataTable>> factory)
    {
        var property = typeof(DatabaseService).GetProperty("ExecuteSelectOverride", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMemberException(nameof(DatabaseService), "ExecuteSelectOverride");
        property.SetValue(database, factory);
    }

    private static void SetExecuteNonQueryOverride(DatabaseService database, Func<string, IEnumerable<MySqlParameter>?, CancellationToken, Task<int>> factory)
    {
        var property = typeof(DatabaseService).GetProperty("ExecuteNonQueryOverride", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMemberException(nameof(DatabaseService), "ExecuteNonQueryOverride");
        property.SetValue(database, factory);
    }

    private static void ResetOverrides(DatabaseService database)
    {
        var method = typeof(DatabaseService).GetMethod("ResetTestOverrides", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMemberException(nameof(DatabaseService), "ResetTestOverrides");
        method.Invoke(database, null);
    }

    private static void SetPrivateField(object target, string fieldName, object? value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMemberException(target.GetType().Name, fieldName);
        field.SetValue(target, value);
    }

    private sealed class StubUserSession : IUserSession
    {
        public StubUserSession(int userId, string? sessionId = null)
        {
            UserId = userId;
            SessionId = sessionId ?? Guid.NewGuid().ToString("N");
        }

        public User? CurrentUser => null;
        public int? UserId { get; }
        public string? Username => "test";
        public string? FullName => "Test User";
        public string SessionId { get; }
    }

    private sealed class StubAuthContext : IAuthContext
    {
        private readonly string _ip;
        private readonly string _device;

        public StubAuthContext(string? ip = null, string? device = null)
        {
            _ip = ip ?? "127.0.0.1";
            _device = device ?? "Device";
        }

        public User? CurrentUser => null;
        public string CurrentSessionId => Guid.Empty.ToString();
        public string CurrentDeviceInfo => _device;
        public string CurrentIpAddress => _ip;
    }
}
