using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySqlConnector;
using Xunit;
using YasGMP.Models;
using YasGMP.Models.Enums;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.Tests;

public class DeviationCrudServiceAdapterTests
{
    [Fact]
    public async Task CreateAsync_PersistsContextMetadataAndReturnsMatchingSignature()
    {
        var insertParameters = new Dictionary<string, object?>();
        var eventParameters = new Dictionary<string, object?>();
        var db = CreateDatabaseService(insertParameters, eventParameters);
        db.ExecuteScalarOverride = (_, _, _) => Task.FromResult<object?>(321);

        var adapter = new DeviationCrudServiceAdapter(new DeviationService(db, new StubDeviationAuditService()));
        var deviation = new Deviation
        {
            Title = "Line deviation",
            Description = "Observed variance",
            Severity = "major",
            Status = "draft"
        };

        var context = new DeviationCrudContext(
            userId: 77,
            ip: "10.0.0.5",
            deviceInfo: "QA-Lab",
            sessionId: "sess-999",
            signatureId: 41,
            signatureHash: "sig-hash-dev",
            signatureMethod: "pin",
            signatureStatus: "accepted",
            signatureNote: "approved");

        var result = await adapter.CreateAsync(deviation, context).ConfigureAwait(false);

        Assert.Equal("sig-hash-dev", deviation.DigitalSignature);
        Assert.Equal("10.0.0.5", deviation.SourceIp);
        Assert.Equal(77, deviation.LastModifiedById);
        Assert.Equal("sig-hash-dev", insertParameters["@sig"]);
        Assert.Equal("10.0.0.5", insertParameters["@ip"]);
        Assert.Equal("QA-Lab", eventParameters["@dev"]);
        Assert.Equal("sess-999", eventParameters["@sid"]);
        Assert.Equal(41, eventParameters["@sigId"]);
        Assert.Equal("sig-hash-dev", eventParameters["@sigHash"]);

        Assert.NotNull(result.SignatureMetadata);
        Assert.Equal(context.SignatureId, result.SignatureMetadata?.Id);
        Assert.Equal("sig-hash-dev", result.SignatureMetadata?.Hash);
        Assert.Equal("pin", result.SignatureMetadata?.Method);
        Assert.Equal("accepted", result.SignatureMetadata?.Status);
        Assert.Equal("approved", result.SignatureMetadata?.Note);
        Assert.Equal("10.0.0.5", result.SignatureMetadata?.IpAddress);
        Assert.Equal("sess-999", result.SignatureMetadata?.Session);
        Assert.Equal("QA-Lab", result.SignatureMetadata?.Device);
    }

    [Fact]
    public async Task UpdateAsync_PropagatesSignatureAndAuditContext()
    {
        var updateParameters = new Dictionary<string, object?>();
        var eventParameters = new Dictionary<string, object?>();
        var db = CreateDatabaseService(updateParameters, eventParameters);

        var adapter = new DeviationCrudServiceAdapter(new DeviationService(db, new StubDeviationAuditService()));
        var deviation = new Deviation
        {
            Id = 512,
            Title = "Existing deviation",
            Description = "Needs follow-up",
            Severity = "critical",
            Status = "investigation",
            DigitalSignature = "old-sig",
            SourceIp = "192.168.0.2"
        };

        var context = new DeviationCrudContext(
            userId: 88,
            ip: "10.10.10.10",
            deviceInfo: "Prod-Terminal",
            sessionId: "session-42",
            signatureId: 77,
            signatureHash: "sig-update",
            signatureMethod: "token",
            signatureStatus: "valid",
            signatureNote: "update ok");

        var result = await adapter.UpdateAsync(deviation, context).ConfigureAwait(false);

        Assert.Equal("sig-update", deviation.DigitalSignature);
        Assert.Equal("10.10.10.10", deviation.SourceIp);
        Assert.Equal(88, deviation.LastModifiedById);
        Assert.Equal("sig-update", updateParameters["@sig"]);
        Assert.Equal("10.10.10.10", updateParameters["@ip"]);
        Assert.Equal("Prod-Terminal", eventParameters["@dev"]);
        Assert.Equal("session-42", eventParameters["@sid"]);
        Assert.Equal(77, eventParameters["@sigId"]);
        Assert.Equal("sig-update", eventParameters["@sigHash"]);

        Assert.NotNull(result.SignatureMetadata);
        Assert.Equal(context.SignatureId, result.SignatureMetadata?.Id);
        Assert.Equal("sig-update", result.SignatureMetadata?.Hash);
        Assert.Equal("token", result.SignatureMetadata?.Method);
        Assert.Equal("valid", result.SignatureMetadata?.Status);
        Assert.Equal("update ok", result.SignatureMetadata?.Note);
        Assert.Equal("10.10.10.10", result.SignatureMetadata?.IpAddress);
        Assert.Equal("session-42", result.SignatureMetadata?.Session);
        Assert.Equal("Prod-Terminal", result.SignatureMetadata?.Device);
    }

    [Fact]
    public void Validate_ThrowsWhenRequiredFieldsMissing()
    {
        var adapter = new DeviationCrudServiceAdapter(new DeviationService(new DatabaseService("Server=localhost;Uid=root;Pwd=test;"), new StubDeviationAuditService()));

        Assert.Throws<ArgumentNullException>(() => adapter.Validate(null!));

        var missingTitle = new Deviation { Description = "desc", Severity = "major" };
        Assert.Throws<InvalidOperationException>(() => adapter.Validate(missingTitle));

        var missingDescription = new Deviation { Title = "Title", Severity = "major" };
        Assert.Throws<InvalidOperationException>(() => adapter.Validate(missingDescription));

        var missingSeverity = new Deviation { Title = "Title", Description = "desc" };
        Assert.Throws<InvalidOperationException>(() => adapter.Validate(missingSeverity));
    }

    [Fact]
    public void Validate_NormalizesWhitespaceAndDefaultsReportedAt()
    {
        var db = new DatabaseService("Server=localhost;Uid=root;Pwd=test;");
        var adapter = new DeviationCrudServiceAdapter(new DeviationService(db, new StubDeviationAuditService()));
        var deviation = new Deviation
        {
            Title = "  title  ",
            Description = "  description  ",
            Severity = "  major  ",
            ReportedAt = null
        };

        adapter.Validate(deviation);

        Assert.Equal("title", deviation.Title);
        Assert.Equal("description", deviation.Description);
        Assert.Equal("MAJOR", deviation.Severity);
        Assert.NotNull(deviation.ReportedAt);
    }

    [Theory]
    [InlineData(null, "OPEN")]
    [InlineData("", "OPEN")]
    [InlineData(" \t", "OPEN")]
    [InlineData("open", "OPEN")]
    [InlineData("Closed", "CLOSED")]
    [InlineData("custom", "CUSTOM")]
    public void NormalizeStatus_ProducesExpectedValues(string? input, string expected)
    {
        var db = new DatabaseService("Server=localhost;Uid=root;Pwd=test;");
        var adapter = new DeviationCrudServiceAdapter(new DeviationService(db, new StubDeviationAuditService()));

        var actual = adapter.NormalizeStatus(input);

        Assert.Equal(expected, actual);
    }

    private static DatabaseService CreateDatabaseService(
        IDictionary<string, object?> persistence,
        IDictionary<string, object?> events)
    {
        var db = new DatabaseService("Server=localhost;Database=test;Uid=root;Pwd=test;");
        db.ExecuteNonQueryOverride = (sql, parameters, _) =>
        {
            if (sql.Contains("deviations", StringComparison.OrdinalIgnoreCase))
            {
                Capture(parameters, persistence);
                return Task.FromResult(1);
            }

            if (sql.Contains("system_event_log", StringComparison.OrdinalIgnoreCase))
            {
                Capture(parameters, events);
                return Task.FromResult(1);
            }

            return Task.FromResult(0);
        };

        return db;
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

    private sealed class StubDeviationAuditService : IDeviationAuditService
    {
        public Task CreateAsync(DeviationAudit audit) => Task.CompletedTask;

        public Task UpdateAsync(DeviationAudit audit) => Task.CompletedTask;

        public Task<DeviationAudit> GetByIdAsync(int id) => Task.FromResult(new DeviationAudit());

        public Task<IReadOnlyList<DeviationAudit>> GetByDeviationIdAsync(int deviationId)
            => Task.FromResult<IReadOnlyList<DeviationAudit>>(Array.Empty<DeviationAudit>());

        public Task<IReadOnlyList<DeviationAudit>> GetByUserIdAsync(int userId)
            => Task.FromResult<IReadOnlyList<DeviationAudit>>(Array.Empty<DeviationAudit>());

        public Task<IReadOnlyList<DeviationAudit>> GetByActionTypeAsync(DeviationActionType actionType)
            => Task.FromResult<IReadOnlyList<DeviationAudit>>(Array.Empty<DeviationAudit>());

        public Task<IReadOnlyList<DeviationAudit>> GetByDateRangeAsync(DateTime from, DateTime to)
            => Task.FromResult<IReadOnlyList<DeviationAudit>>(Array.Empty<DeviationAudit>());

        public bool ValidateIntegrity(DeviationAudit audit) => true;
    }
}
