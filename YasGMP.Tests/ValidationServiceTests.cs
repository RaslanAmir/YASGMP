using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using Xunit;
using YasGMP.Models;
using YasGMP.Models.Enums;
using YasGMP.Services;
using YasGMP.Services.Interfaces;

namespace YasGMP.Tests
{
    public class ValidationServiceTests
    {
        [Fact]
        public async Task CreateAsync_PreservesExistingDigitalSignature()
        {
            var db = new DatabaseService("Server=localhost;Database=test;Uid=root;Pwd=test;");
            var parameters = new Dictionary<string, object?>();

            db.ExecuteNonQueryOverride = CaptureParameters(parameters);
            db.ExecuteScalarOverride = (_, _, _) => Task.FromResult<object?>(123);

            var service = new ValidationService(db, new StubValidationAuditService());
            var validation = CreateValidation();
            validation.DigitalSignature = "adapter-hash";
            validation.SourceIp = "adapter-ip";
            validation.SessionId = "adapter-session";

            await service.CreateAsync(validation, userId: 7);

            Assert.Equal("adapter-hash", validation.DigitalSignature);
            Assert.Equal("adapter-hash", parameters["@sig"]);
        }

        [Fact]
        public async Task UpdateAsync_PreservesExistingDigitalSignature()
        {
            var db = new DatabaseService("Server=localhost;Database=test;Uid=root;Pwd=test;");
            var parameters = new Dictionary<string, object?>();

            db.ExecuteNonQueryOverride = CaptureParameters(parameters);

            var service = new ValidationService(db, new StubValidationAuditService());
            var validation = CreateValidation();
            validation.Id = 77;
            validation.DigitalSignature = "adapter-update-hash";
            validation.SourceIp = "adapter-ip";
            validation.SessionId = "adapter-session";

            await service.UpdateAsync(validation, userId: 11);

            Assert.Equal("adapter-update-hash", validation.DigitalSignature);
            Assert.Equal("adapter-update-hash", parameters["@sig"]);
        }

        [Fact]
        public async Task CreateAsync_PassesSourceMetadataToDatabase()
        {
            var db = new DatabaseService("Server=localhost;Database=test;Uid=root;Pwd=test;");
            var parameters = new Dictionary<string, object?>();

            db.ExecuteNonQueryOverride = CaptureParameters(parameters);
            db.ExecuteScalarOverride = (_, _, _) => Task.FromResult<object?>(321);

            var service = new ValidationService(db, new StubValidationAuditService());
            var validation = CreateValidation();
            validation.SourceIp = "10.1.2.3";
            validation.SessionId = "sess-create";

            await service.CreateAsync(validation, userId: 3);

            Assert.Equal("10.1.2.3", validation.SourceIp);
            Assert.Equal("sess-create", validation.SessionId);
            Assert.Equal("10.1.2.3", parameters["@source_ip"]);
            Assert.Equal("sess-create", parameters["@session"]);
        }

        [Fact]
        public async Task UpdateAsync_PassesSourceMetadataToDatabase()
        {
            var db = new DatabaseService("Server=localhost;Database=test;Uid=root;Pwd=test;");
            var parameters = new Dictionary<string, object?>();

            db.ExecuteNonQueryOverride = CaptureParameters(parameters);

            var service = new ValidationService(db, new StubValidationAuditService());
            var validation = CreateValidation();
            validation.Id = 55;
            validation.SourceIp = "10.4.5.6";
            validation.SessionId = "sess-update";

            await service.UpdateAsync(validation, userId: 9);

            Assert.Equal("10.4.5.6", validation.SourceIp);
            Assert.Equal("sess-update", validation.SessionId);
            Assert.Equal("10.4.5.6", parameters["@source_ip"]);
            Assert.Equal("sess-update", parameters["@session"]);
        }

        private static Func<string, IEnumerable<MySqlParameter>?, CancellationToken, Task<int>> CaptureParameters(Dictionary<string, object?> store)
        {
            return (_, parameters, _) =>
            {
                store.Clear();
                if (parameters != null)
                {
                    foreach (var p in parameters)
                    {
                        store[p.ParameterName] = p.Value;
                    }
                }
                return Task.FromResult(1);
            };
        }

        private static Validation CreateValidation() => new()
        {
            Code = "VAL-001",
            Type = "PQ",
            MachineId = 42,
            Status = "DRAFT",
            DateStart = DateTime.UtcNow
        };

        private sealed class StubValidationAuditService : IValidationAuditService
        {
            public Task CreateAsync(ValidationAudit audit) => Task.CompletedTask;

            public Task LogAsync(int validationId, int userId, ValidationActionType action, string details)
                => Task.CompletedTask;
        }
    }
}
