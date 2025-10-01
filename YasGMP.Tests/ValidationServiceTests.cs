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
            string? capturedSignature = null;

            db.ExecuteNonQueryOverride = CaptureSignature(ref capturedSignature);
            db.ExecuteScalarOverride = (_, _, _) => Task.FromResult<object?>(123);

            var service = new ValidationService(db, new StubValidationAuditService());
            var validation = CreateValidation();
            validation.DigitalSignature = "adapter-hash";

            await service.CreateAsync(validation, userId: 7);

            Assert.Equal("adapter-hash", validation.DigitalSignature);
            Assert.Equal("adapter-hash", capturedSignature);
        }

        [Fact]
        public async Task UpdateAsync_PreservesExistingDigitalSignature()
        {
            var db = new DatabaseService("Server=localhost;Database=test;Uid=root;Pwd=test;");
            string? capturedSignature = null;

            db.ExecuteNonQueryOverride = CaptureSignature(ref capturedSignature);

            var service = new ValidationService(db, new StubValidationAuditService());
            var validation = CreateValidation();
            validation.Id = 77;
            validation.DigitalSignature = "adapter-update-hash";

            await service.UpdateAsync(validation, userId: 11);

            Assert.Equal("adapter-update-hash", validation.DigitalSignature);
            Assert.Equal("adapter-update-hash", capturedSignature);
        }

        private static Func<string, IEnumerable<MySqlParameter>?, CancellationToken, Task<int>> CaptureSignature(ref string? captured)
        {
            return (_, parameters, _) =>
            {
                captured = parameters?.FirstOrDefault(p => p.ParameterName == "@sig")?.Value?.ToString();
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
