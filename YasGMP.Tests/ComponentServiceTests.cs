using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using Xunit;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.Tests
{
    public class ComponentServiceTests
    {
        private const string ConnectionString = "Server=localhost;User Id=test;Password=test;Database=test;";

        [Fact]
        public async Task CreateAsync_UsesAdapterProvidedSignatureAndContext()
        {
            var db = new DatabaseService(ConnectionString);
            var capturedCommands = new List<(string Sql, IReadOnlyDictionary<string, object?> Parameters)>();

            db.ExecuteNonQueryOverride = (sql, parameters, _) =>
            {
                var snapshot = parameters?.ToDictionary(p => p.ParameterName, p => p.Value)
                               ?? new Dictionary<string, object?>();
                capturedCommands.Add((sql, snapshot));
                return Task.FromResult(1);
            };
            db.ExecuteScalarOverride = (sql, _, _) =>
            {
                if (sql.Contains("LAST_INSERT_ID", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult<object?>(1);
                }

                return Task.FromResult<object?>(0);
            };

            var audit = new NoOpAuditService(db);
            var service = new ComponentService(db, audit);

            var component = new Component
            {
                MachineId = 7,
                Code = "CMP-001",
                Name = "Filter Assembly",
                SopDoc = "SOP-FILTER-001"
            };

            var context = ComponentSaveContext.Create(
                signatureHash: "hash-from-adapter",
                ipAddress: "10.0.0.12",
                deviceInfo: "UnitTestRig",
                sessionId: "session-xyz");

            try
            {
                await service.CreateAsync(component, userId: 42, context, CancellationToken.None).ConfigureAwait(false);
            }
            finally
            {
                db.ResetTestOverrides();
            }

            Assert.Equal("hash-from-adapter", component.DigitalSignature);
            Assert.Equal("10.0.0.12", component.SourceIp);

            var insert = capturedCommands.Single(entry =>
                entry.Sql.Contains("machine_components", StringComparison.OrdinalIgnoreCase));

            Assert.Equal("hash-from-adapter", insert.Parameters["@sig"]);
            Assert.Equal("10.0.0.12", insert.Parameters["@ip"]);
            Assert.Equal("UnitTestRig", insert.Parameters["@device"]);
            Assert.Equal("session-xyz", insert.Parameters["@session"]);
        }

        [Fact]
        public async Task UpdateAsync_RetainsExistingSignatureWhenContextMissing()
        {
            var db = new DatabaseService(ConnectionString);
            var capturedCommands = new List<(string Sql, IReadOnlyDictionary<string, object?> Parameters)>();

            db.ExecuteNonQueryOverride = (sql, parameters, _) =>
            {
                var snapshot = parameters?.ToDictionary(p => p.ParameterName, p => p.Value)
                               ?? new Dictionary<string, object?>();
                capturedCommands.Add((sql, snapshot));
                return Task.FromResult(1);
            };
            db.ExecuteScalarOverride = (sql, _, _) => Task.FromResult<object?>(0);

            var audit = new NoOpAuditService(db);
            var service = new ComponentService(db, audit);

            var component = new Component
            {
                Id = 12,
                MachineId = 5,
                Code = "CMP-012",
                Name = "Pump",
                SopDoc = "SOP-PUMP-001",
                DigitalSignature = "existing-hash",
                SourceIp = "192.168.1.10"
            };

            try
            {
                await service.UpdateAsync(component, userId: 99, context: null, CancellationToken.None).ConfigureAwait(false);
            }
            finally
            {
                db.ResetTestOverrides();
            }

            Assert.Equal("existing-hash", component.DigitalSignature);

            var update = capturedCommands.Single(entry =>
                entry.Sql.Contains("UPDATE machine_components", StringComparison.OrdinalIgnoreCase));

            Assert.Equal("existing-hash", update.Parameters["@sig"]);
            Assert.Equal("192.168.1.10", update.Parameters["@ip"]);
        }

        private sealed class NoOpAuditService : AuditService
        {
            public NoOpAuditService(DatabaseService db) : base(db)
            {
            }

            public override Task LogEntityAuditAsync(string? tableName, int entityId, string? action, string? details)
                => Task.CompletedTask;
        }
    }
}

