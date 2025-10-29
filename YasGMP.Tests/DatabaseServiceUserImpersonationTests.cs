using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using MySqlConnector;
using Xunit;
using YasGMP.Services;

namespace YasGMP.Tests;

public class DatabaseServiceUserImpersonationTests
{
    private const string ConnectionString = "Server=localhost;User Id=test;Password=test;Database=test;";

    [Fact]
    public async Task BeginImpersonationSessionAsync_UsesPreferredSchemaAndReturnsId()
    {
        var db = new DatabaseService(ConnectionString);
        var commands = new List<(string Sql, MySqlParameter[] Parameters)>();

        db.ExecuteNonQueryOverride = (sql, parameters, _) =>
        {
            commands.Add((sql, parameters?.OfType<MySqlParameter>().ToArray() ?? Array.Empty<MySqlParameter>()));
            return Task.FromResult(1);
        };

        db.ExecuteScalarOverride = (sql, _, _) =>
        {
            Assert.Equal("SELECT LAST_INSERT_ID();", sql);
            return Task.FromResult<object?>(42);
        };

        try
        {
            var startedAt = new DateTime(2024, 01, 02, 12, 00, 00, DateTimeKind.Utc);
            var id = await db.BeginImpersonationSessionAsync(
                actorUserId: 11,
                targetUserId: 25,
                startedAtUtc: startedAt,
                sessionId: "sess-1",
                ip: "10.0.0.5",
                deviceInfo: "Surface",
                reason: "Audit trail",
                notes: "Follow-up",
                signatureId: 77,
                signatureHash: "hash-77",
                signatureMethod: "password",
                signatureStatus: "valid",
                signatureNote: "note-77").ConfigureAwait(false);

            Assert.Equal(42, id);
            var command = Assert.Single(commands);
            const string expectedSql = @"
INSERT INTO session_log
    (user_id, impersonated_by_id, is_impersonated, login_time, session_id, session_token,
     ip_address, device_info, reason, note, digital_signature_id, digital_signature,
     signature_method, signature_status, signature_note, created_at, updated_at)
VALUES
    (@target, @actor, 1, @login, @sessionId, @sessionToken,
     @ip, @device, @reason, @note, @sigId, @sigHash,
     @sigMethod, @sigStatus, @sigNote, @created, @updated);";
            Assert.Equal(expectedSql, command.Sql);
            Assert.Collection(
                command.Parameters,
                p => Assert.Equal(("@target", 25), (p.ParameterName, Convert.ToInt32(p.Value, CultureInfo.InvariantCulture))),
                p => Assert.Equal(("@actor", 11), (p.ParameterName, Convert.ToInt32(p.Value, CultureInfo.InvariantCulture))),
                p => Assert.Equal(("@login", startedAt), (p.ParameterName, (DateTime)p.Value!)),
                p => Assert.Equal(("@sessionId", "sess-1"), (p.ParameterName, (string)p.Value!)),
                p => Assert.Equal(("@sessionToken", "sess-1"), (p.ParameterName, (string)p.Value!)),
                p => Assert.Equal(("@ip", "10.0.0.5"), (p.ParameterName, (string)p.Value!)),
                p => Assert.Equal(("@device", "Surface"), (p.ParameterName, (string)p.Value!)),
                p => Assert.Equal(("@reason", "Audit trail"), (p.ParameterName, (string)p.Value!)),
                p => Assert.Equal(("@note", "Follow-up"), (p.ParameterName, (string)p.Value!)),
                p => Assert.Equal(("@sigId", 77), (p.ParameterName, Convert.ToInt32(p.Value, CultureInfo.InvariantCulture))),
                p => Assert.Equal(("@sigHash", "hash-77"), (p.ParameterName, (string)p.Value!)),
                p => Assert.Equal(("@sigMethod", "password"), (p.ParameterName, (string)p.Value!)),
                p => Assert.Equal(("@sigStatus", "valid"), (p.ParameterName, (string)p.Value!)),
                p => Assert.Equal(("@sigNote", "note-77"), (p.ParameterName, (string)p.Value!)),
                p => Assert.Equal(("@created", startedAt), (p.ParameterName, (DateTime)p.Value!)),
                p => Assert.Equal(("@updated", startedAt), (p.ParameterName, (DateTime)p.Value!)));
        }
        finally
        {
            db.ResetTestOverrides();
        }
    }

    [Fact]
    public async Task BeginImpersonationSessionAsync_FallsBackWhenColumnsMissing()
    {
        var db = new DatabaseService(ConnectionString);
        var commands = new List<string>();
        var invocation = 0;

        db.ExecuteNonQueryOverride = (sql, _, _) =>
        {
            commands.Add(sql);
            invocation++;
            if (invocation == 1)
            {
                return Task.FromException<int>(CreateMySqlException(1054));
            }

            return Task.FromResult(1);
        };

        db.ExecuteScalarOverride = (_, _, _) => Task.FromResult<object?>(5);

        try
        {
            var id = await db.BeginImpersonationSessionAsync(
                actorUserId: 2,
                targetUserId: 3,
                startedAtUtc: DateTime.UtcNow,
                sessionId: null,
                ip: null,
                deviceInfo: null,
                reason: "Escalation",
                notes: null,
                signatureId: null,
                signatureHash: null,
                signatureMethod: null,
                signatureStatus: null,
                signatureNote: null).ConfigureAwait(false);

            Assert.Equal(5, id);
            Assert.Equal(2, commands.Count);
            Assert.Contains("signature_method", commands[0]);
            Assert.DoesNotContain("signature_method", commands[1]);
        }
        finally
        {
            db.ResetTestOverrides();
        }
    }

    [Fact]
    public async Task EndImpersonationSessionAsync_UsesPreferredSchema()
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
            var endedAt = new DateTime(2024, 04, 05, 08, 30, 00, DateTimeKind.Utc);
            var id = await db.EndImpersonationSessionAsync(
                sessionLogId: 9,
                actorUserId: 11,
                endedAtUtc: endedAt,
                notes: "Completed",
                signatureId: 12,
                signatureHash: "hash-12",
                signatureMethod: "pin",
                signatureStatus: "valid",
                signatureNote: "note-12").ConfigureAwait(false);

            Assert.Equal(9, id);
            var command = Assert.Single(commands);
            const string expectedSql = @"
UPDATE session_log
SET logout_time=@logout,
    logout_at=@logout,
    updated_at=@updated,
    is_terminated=1,
    terminated_by=@actor,
    note=COALESCE(@note, note),
    digital_signature_id=@sigId,
    digital_signature=@sigHash,
    signature_method=@sigMethod,
    signature_status=@sigStatus,
    signature_note=@sigNote
WHERE id=@id;";
            Assert.Equal(expectedSql, command.Sql);
            Assert.Collection(
                command.Parameters,
                p => Assert.Equal(("@id", 9), (p.ParameterName, Convert.ToInt32(p.Value, CultureInfo.InvariantCulture))),
                p => Assert.Equal(("@actor", 11), (p.ParameterName, Convert.ToInt32(p.Value, CultureInfo.InvariantCulture))),
                p => Assert.Equal(("@logout", endedAt), (p.ParameterName, (DateTime)p.Value!)),
                p => Assert.Equal(("@note", "Completed"), (p.ParameterName, (string)p.Value!)),
                p => Assert.Equal(("@sigId", 12), (p.ParameterName, Convert.ToInt32(p.Value, CultureInfo.InvariantCulture))),
                p => Assert.Equal(("@sigHash", "hash-12"), (p.ParameterName, (string)p.Value!)),
                p => Assert.Equal(("@sigMethod", "pin"), (p.ParameterName, (string)p.Value!)),
                p => Assert.Equal(("@sigStatus", "valid"), (p.ParameterName, (string)p.Value!)),
                p => Assert.Equal(("@sigNote", "note-12"), (p.ParameterName, (string)p.Value!)),
                p => Assert.Equal(("@updated", endedAt), (p.ParameterName, (DateTime)p.Value!)));
        }
        finally
        {
            db.ResetTestOverrides();
        }
    }

    [Fact]
    public async Task EndImpersonationSessionAsync_FallsBackWhenColumnsMissing()
    {
        var db = new DatabaseService(ConnectionString);
        var commands = new List<string>();
        var invocation = 0;

        db.ExecuteNonQueryOverride = (sql, _, _) =>
        {
            commands.Add(sql);
            invocation++;
            if (invocation <= 2)
            {
                return Task.FromException<int>(CreateMySqlException(1054));
            }

            return Task.FromResult(1);
        };

        try
        {
            var id = await db.EndImpersonationSessionAsync(
                sessionLogId: 33,
                actorUserId: 44,
                endedAtUtc: DateTime.UtcNow,
                notes: null,
                signatureId: null,
                signatureHash: null,
                signatureMethod: null,
                signatureStatus: null,
                signatureNote: null).ConfigureAwait(false);

            Assert.Equal(33, id);
            Assert.Equal(3, commands.Count);
            Assert.Contains("signature_method", commands[0]);
            Assert.Contains("digital_signature_id", commands[1]);
            Assert.DoesNotContain("digital_signature_id", commands[2]);
        }
        finally
        {
            db.ResetTestOverrides();
        }
    }

    private static MySqlException CreateMySqlException(int number)
    {
        var exception = (MySqlException)FormatterServices.GetUninitializedObject(typeof(MySqlException));
        var numberField = typeof(MySqlException)
            .GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .FirstOrDefault(f => f.FieldType == typeof(int) && f.Name.Contains("number", StringComparison.OrdinalIgnoreCase));
        numberField?.SetValue(exception, number);
        return exception;
    }
}
