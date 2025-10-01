using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using Xunit;
using YasGMP.Models;
using YasGMP.Models.Enums;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.Tests;

public class ValidationCrudServiceAdapterTests
{
    [Fact]
    public async Task CreateAsync_PersistsContextMetadataAndReturnsMatchingSignature()
    {
        var captured = new Dictionary<string, object?>();
        var db = new DatabaseService("Server=localhost;Database=test;Uid=root;Pwd=test;");
        db.ExecuteNonQueryOverride = (sql, parameters, _) =>
        {
            if (sql.Contains("INSERT INTO validations", StringComparison.OrdinalIgnoreCase))
            {
                Capture(parameters, captured);
            }

            return Task.FromResult(1);
        };
        db.ExecuteScalarOverride = (_, _, _) => Task.FromResult<object?>(321);

        var adapter = new ValidationCrudServiceAdapter(new ValidationService(db, new StubValidationAuditService()));
        var validation = new Validation
        {
            Code = "VAL-CTX",
            Type = "PQ",
            MachineId = 5,
            Status = "DRAFT",
            DateStart = DateTime.UtcNow
        };

        var context = new ValidationCrudContext(
            userId: 17,
            ip: "172.20.0.77",
            deviceInfo: "TestHarness",
            sessionId: "session-ctx",
            signatureId: 44,
            signatureHash: "sig-hash-ctx",
            signatureMethod: "password",
            signatureStatus: "valid",
            signatureNote: "ok");

        var result = await adapter.CreateAsync(validation, context).ConfigureAwait(false);

        Assert.Equal("sig-hash-ctx", validation.DigitalSignature);
        Assert.Equal("172.20.0.77", validation.SourceIp);
        Assert.Equal("session-ctx", validation.SessionId);

        Assert.Equal("sig-hash-ctx", captured["@sig"]);
        Assert.Equal("172.20.0.77", captured["@source_ip"]);
        Assert.Equal("session-ctx", captured["@session"]);

        Assert.NotNull(result.SignatureMetadata);
        Assert.Equal(validation.DigitalSignature, result.SignatureMetadata?.Hash);
        Assert.Equal(validation.SourceIp, result.SignatureMetadata?.IpAddress);
        Assert.Equal(validation.SessionId, result.SignatureMetadata?.Session);
        Assert.Equal(context.SignatureId, result.SignatureMetadata?.Id);
    }

    private static void Capture(IEnumerable<MySqlParameter>? parameters, IDictionary<string, object?> store)
    {
        store.Clear();
        if (parameters == null)
        {
            return;
        }

        foreach (var parameter in parameters)
        {
            store[parameter.ParameterName] = parameter.Value;
        }
    }

    private sealed class StubValidationAuditService : IValidationAuditService
    {
        public Task CreateAsync(ValidationAudit audit) => Task.CompletedTask;

        public Task LogAsync(int validationId, int userId, ValidationActionType action, string details)
            => Task.CompletedTask;
    }
}
