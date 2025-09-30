using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using Xunit;
using YasGMP.Services;

namespace YasGMP.Tests
{
    public class DatabaseServiceAuditLoggingTests
    {
        [Fact]
        public async Task LogSystemEventAsync_IncludesSignatureIdentifiers_WhenColumnsPresent()
        {
            var db = new DatabaseService("Server=localhost;User Id=test;Password=test;Database=test;");
            string? sql = null;
            IReadOnlyList<MySqlParameter>? parameters = null;

            db.ExecuteNonQueryOverride = (commandText, pars, _) =>
            {
                sql = commandText;
                parameters = pars?.ToList();
                return Task.FromResult(1);
            };

            try
            {
                await db.LogSystemEventAsync(
                    userId: 42,
                    eventType: "TEST_EVENT",
                    tableName: "entities",
                    module: "UnitTests",
                    recordId: 99,
                    description: "unit-test",
                    ip: "127.0.0.1",
                    severity: "audit",
                    deviceInfo: "test-agent",
                    sessionId: "session-1",
                    signatureId: 777,
                    signatureHash: "ABC123",
                    token: CancellationToken.None).ConfigureAwait(false);
            }
            finally
            {
                db.ResetTestOverrides();
            }

            Assert.NotNull(sql);
            Assert.Contains("digital_signature_id", sql!, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("digital_signature", sql!, StringComparison.OrdinalIgnoreCase);

            Assert.NotNull(parameters);
            var sigIdParam = Assert.Single(parameters!.Where(p => p.ParameterName == "@sigId"));
            Assert.Equal(777, sigIdParam.Value);
            var sigHashParam = Assert.Single(parameters!.Where(p => p.ParameterName == "@sigHash"));
            Assert.Equal("ABC123", sigHashParam.Value);

            var descriptionParam = Assert.Single(parameters!.Where(p => p.ParameterName == "@desc"));
            var description = descriptionParam.Value as string;
            Assert.NotNull(description);
            Assert.Contains("sigId=777", description!, StringComparison.Ordinal);
            Assert.Contains("sigHash=ABC123", description!, StringComparison.Ordinal);
        }

        [Fact]
        public async Task LogSystemEventAsync_FallsBack_WhenSignatureColumnsMissing()
        {
            var db = new DatabaseService("Server=localhost;User Id=test;Password=test;Database=test;");
            int attempts = 0;
            var executedSql = new List<string>();
            var capturedParameters = new List<IReadOnlyDictionary<string, object?>>();

            db.ExecuteNonQueryOverride = (commandText, pars, _) =>
            {
                attempts++;
                executedSql.Add(commandText);
                var snapshot = pars?.ToDictionary(p => p.ParameterName, p => p.Value) ??
                               new Dictionary<string, object?>();
                capturedParameters.Add(snapshot);

                if (attempts == 1)
                {
                    return Task.FromException<int>(CreateMySqlException(1054));
                }

                return Task.FromResult(1);
            };

            try
            {
                await db.LogSystemEventAsync(
                    userId: 7,
                    eventType: "TEST_FALLBACK",
                    tableName: "legacy",
                    module: "UnitTests",
                    recordId: 12,
                    description: "fallback",
                    ip: "10.0.0.2",
                    severity: "audit",
                    deviceInfo: "legacy-client",
                    sessionId: "session-2",
                    signatureId: 55,
                    signatureHash: "HASH-55",
                    token: CancellationToken.None).ConfigureAwait(false);
            }
            finally
            {
                db.ResetTestOverrides();
            }

            Assert.Equal(2, attempts);
            Assert.Collection(
                executedSql,
                first => Assert.Contains("digital_signature_id", first, StringComparison.OrdinalIgnoreCase),
                second => Assert.DoesNotContain("digital_signature_id", second, StringComparison.OrdinalIgnoreCase));
            Assert.Contains("digital_signature", executedSql[1], StringComparison.OrdinalIgnoreCase);

            var finalParams = capturedParameters.Last();
            Assert.True(finalParams.ContainsKey("@sigHash"));
            Assert.False(finalParams.ContainsKey("@sigId"));
            Assert.Equal("HASH-55", finalParams["@sigHash"]);

            var description = Assert.IsType<string>(finalParams["@desc"]);
            Assert.Contains("sigId=55", description, StringComparison.Ordinal);
            Assert.Contains("sigHash=HASH-55", description, StringComparison.Ordinal);
        }

        private static MySqlException CreateMySqlException(int number)
        {
            var exception = (MySqlException)FormatterServices.GetUninitializedObject(typeof(MySqlException));
            var numberField = typeof(MySqlException)
                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(f => f.FieldType == typeof(int) && f.Name.Contains("number", StringComparison.OrdinalIgnoreCase));
            numberField?.SetValue(exception, number);
            return exception;
        }
    }
}

