using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using YasGMP.Helpers;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.Tests
{
    public class DatabaseServiceDigitalSignatureTests
    {
        private const string ConnectionString = "Server=localhost;User Id=test;Password=test;Database=test;";

        [Fact]
        public async Task VerifySignatureAsync_ReturnsTrue_ForValidPinSignature()
        {
            var db = new DatabaseService(ConnectionString);
            var sessionId = "sess-001";
            var deviceInfo = "Device-A";
            var machineId = 7;
            var recordUser = 42;

            var canonicalMachine = new Machine
            {
                Id = machineId,
                Code = "MX-7",
                Name = "Mixer",
                SerialNumber = "SN-777"
            };
            var signatureComputation = DigitalSignatureHelper.ComputeSignature(canonicalMachine, sessionId, deviceInfo);

            var signatureTable = CreateSignatureTable();
            signatureTable.Rows.Add(
                1,
                "machines",
                machineId,
                recordUser,
                signatureComputation.Hash,
                "pin",
                "valid",
                DateTime.UtcNow,
                deviceInfo,
                "127.0.0.1",
                "audit",
                DBNull.Value,
                sessionId);

            var machineTable = CreateMachineTable();
            machineTable.Rows.Add(
                machineId,
                canonicalMachine.Code,
                canonicalMachine.Name,
                "Filler",
                "Test machine",
                "Model-X",
                canonicalMachine.SerialNumber,
                "ACME",
                "HQ",
                "QA");

            db.ExecuteSelectOverride = (sql, parameters, _) =>
            {
                if (sql.Contains("digital_signatures", StringComparison.OrdinalIgnoreCase))
                    return Task.FromResult(signatureTable);
                if (sql.Contains("FROM machines", StringComparison.OrdinalIgnoreCase))
                    return Task.FromResult(machineTable);
                throw new InvalidOperationException($"Unexpected SQL: {sql}");
            };

            try
            {
                var result = await db.VerifySignatureAsync(1).ConfigureAwait(false);
                Assert.True(result);
            }
            finally
            {
                db.ResetTestOverrides();
            }
        }

        [Fact]
        public async Task VerifySignatureAsync_ReturnsFalse_WhenHashMismatch()
        {
            var db = new DatabaseService(ConnectionString);
            var sessionId = "sess-002";
            var deviceInfo = "Device-B";
            var machineId = 11;
            var userId = 5;

            var canonicalMachine = new Machine
            {
                Id = machineId,
                Code = "MX-11",
                Name = "Mixer",
                SerialNumber = "SN-111"
            };
            var signatureComputation = DigitalSignatureHelper.ComputeSignature(canonicalMachine, sessionId, deviceInfo);
            var tamperedHash = signatureComputation.Hash[..^1] + (signatureComputation.Hash[^1] == 'A' ? 'B' : 'A');

            var signatureTable = CreateSignatureTable();
            signatureTable.Rows.Add(
                2,
                "machines",
                machineId,
                userId,
                tamperedHash,
                "pin",
                "valid",
                DateTime.UtcNow,
                deviceInfo,
                "10.0.0.2",
                "tampered",
                DBNull.Value,
                sessionId);

            var machineTable = CreateMachineTable();
            machineTable.Rows.Add(
                machineId,
                canonicalMachine.Code,
                canonicalMachine.Name,
                "Packaging",
                "Mixer",
                "Model-Y",
                canonicalMachine.SerialNumber,
                "ACME",
                "Plant",
                "Ops");

            var loggedEvents = new List<Dictionary<string, object?>>();

            db.ExecuteSelectOverride = (sql, parameters, _) =>
            {
                if (sql.Contains("digital_signatures", StringComparison.OrdinalIgnoreCase))
                    return Task.FromResult(signatureTable);
                if (sql.Contains("FROM machines", StringComparison.OrdinalIgnoreCase))
                    return Task.FromResult(machineTable);
                throw new InvalidOperationException($"Unexpected SQL: {sql}");
            };

            db.ExecuteNonQueryOverride = (sql, parameters, _) =>
            {
                var snapshot = new Dictionary<string, object?>();
                if (parameters != null)
                {
                    foreach (var p in parameters)
                        snapshot[p.ParameterName] = p.Value;
                }
                loggedEvents.Add(snapshot);
                return Task.FromResult(1);
            };

            try
            {
                var result = await db.VerifySignatureAsync(2).ConfigureAwait(false);
                Assert.False(result);
                Assert.NotEmpty(loggedEvents);
                Assert.Contains(loggedEvents, entry => entry.TryGetValue("@etype", out var type) && string.Equals(type?.ToString(), "SIG_VERIFY_FAIL", StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                db.ResetTestOverrides();
            }
        }

        [Fact]
        public async Task VerifySignatureAsync_ReturnsFalse_ForRevokedSignature()
        {
            var db = new DatabaseService(ConnectionString);
            var sessionId = "sess-003";
            var deviceInfo = "Device-C";
            var machineId = 21;
            var userId = 9;

            var canonicalMachine = new Machine
            {
                Id = machineId,
                Code = "MX-21",
                Name = "Mixer",
                SerialNumber = "SN-210"
            };
            var signatureComputation = DigitalSignatureHelper.ComputeSignature(canonicalMachine, sessionId, deviceInfo);

            var signatureTable = CreateSignatureTable();
            signatureTable.Rows.Add(
                3,
                "machines",
                machineId,
                userId,
                signatureComputation.Hash,
                "pin",
                "revoked",
                DateTime.UtcNow,
                deviceInfo,
                "10.0.0.3",
                "revoked",
                DBNull.Value,
                sessionId);

            var loggedEvents = new List<Dictionary<string, object?>>();

            db.ExecuteSelectOverride = (sql, parameters, _) =>
            {
                if (sql.Contains("digital_signatures", StringComparison.OrdinalIgnoreCase))
                    return Task.FromResult(signatureTable);
                if (sql.Contains("FROM machines", StringComparison.OrdinalIgnoreCase))
                    return Task.FromResult(CreateMachineTable());
                throw new InvalidOperationException($"Unexpected SQL: {sql}");
            };

            db.ExecuteNonQueryOverride = (sql, parameters, _) =>
            {
                var snapshot = new Dictionary<string, object?>();
                if (parameters != null)
                {
                    foreach (var p in parameters)
                        snapshot[p.ParameterName] = p.Value;
                }
                loggedEvents.Add(snapshot);
                return Task.FromResult(1);
            };

            try
            {
                var result = await db.VerifySignatureAsync(3).ConfigureAwait(false);
                Assert.False(result);
                Assert.NotEmpty(loggedEvents);
            }
            finally
            {
                db.ResetTestOverrides();
            }
        }

        private static DataTable CreateSignatureTable()
        {
            var table = new DataTable();
            table.Columns.Add("id", typeof(int));
            table.Columns.Add("table_name", typeof(string));
            table.Columns.Add("record_id", typeof(int));
            table.Columns.Add("user_id", typeof(int));
            table.Columns.Add("signature_hash", typeof(string));
            table.Columns.Add("method", typeof(string));
            table.Columns.Add("status", typeof(string));
            table.Columns.Add("signed_at", typeof(DateTime));
            table.Columns.Add("device_info", typeof(string));
            table.Columns.Add("ip_address", typeof(string));
            table.Columns.Add("note", typeof(string));
            table.Columns.Add("public_key", typeof(string));
            table.Columns.Add("session_id", typeof(string));
            return table;
        }

        private static DataTable CreateMachineTable()
        {
            var table = new DataTable();
            table.Columns.Add("id", typeof(int));
            table.Columns.Add("code", typeof(string));
            table.Columns.Add("name", typeof(string));
            table.Columns.Add("machine_type", typeof(string));
            table.Columns.Add("description", typeof(string));
            table.Columns.Add("model", typeof(string));
            table.Columns.Add("serial_number", typeof(string));
            table.Columns.Add("manufacturer", typeof(string));
            table.Columns.Add("location", typeof(string));
            table.Columns.Add("responsible_party", typeof(string));
            return table;
        }
    }
}
