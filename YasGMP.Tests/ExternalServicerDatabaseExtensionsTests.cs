using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.Tests
{
    public class ExternalServicerDatabaseExtensionsTests
    {
        private const string ConnectionString = "Server=localhost;Database=test;Uid=root;Pwd=secret;";

        [Fact]
        public async Task InsertOrUpdateExternalServicerAsync_Insert_UsesExternalContractorsTable()
        {
            var db = new DatabaseService(ConnectionString);
            var executedSql = new List<string>();
            Dictionary<string, object?>? capturedParams = null;

            db.ExecuteNonQueryOverride = (sql, parameters, _) =>
            {
                executedSql.Add(sql);
                capturedParams = parameters?.ToDictionary(p => p.ParameterName, p => p.Value);
                return Task.FromResult(1);
            };

            db.ExecuteScalarOverride = (sql, _, _) =>
            {
                executedSql.Add(sql);
                return Task.FromResult<object?>(42);
            };

            var servicer = new ExternalServicer
            {
                Name = "ACME Labs",
                ExtraNotes = "Note",
                Comment = "Comment"
            };

            try
            {
                var id = await db.InsertOrUpdateExternalServicerAsync(servicer, update: false).ConfigureAwait(false);

                Assert.Equal(42, id);
                Assert.Equal(42, servicer.Id);
                Assert.Contains(executedSql, sql => sql.Contains("INSERT INTO external_contractors", StringComparison.OrdinalIgnoreCase));
                Assert.NotNull(capturedParams);
                Assert.Equal("Note", capturedParams!["@note"]);
                Assert.Equal("Comment", capturedParams!["@comm"]);
            }
            finally
            {
                db.ResetTestOverrides();
            }
        }

        [Fact]
        public async Task InsertOrUpdateExternalServicerAsync_Update_UsesExternalContractorsTable()
        {
            var db = new DatabaseService(ConnectionString);
            var executedSql = new List<string>();
            Dictionary<string, object?>? capturedParams = null;

            db.ExecuteNonQueryOverride = (sql, parameters, _) =>
            {
                executedSql.Add(sql);
                capturedParams = parameters?.ToDictionary(p => p.ParameterName, p => p.Value);
                return Task.FromResult(1);
            };

            var servicer = new ExternalServicer
            {
                Id = 7,
                Name = "ACME Labs",
                ExtraNotes = "Note",
                Comment = "Comment"
            };

            try
            {
                await db.InsertOrUpdateExternalServicerAsync(servicer, update: true).ConfigureAwait(false);

                var updateSql = Assert.Single(executedSql);
                Assert.Contains("UPDATE external_contractors", updateSql, StringComparison.OrdinalIgnoreCase);
                Assert.NotNull(capturedParams);
                Assert.Equal(7, capturedParams!["@id"]);
                Assert.Equal("Note", capturedParams!["@note"]);
                Assert.Equal("Comment", capturedParams!["@comm"]);
            }
            finally
            {
                db.ResetTestOverrides();
            }
        }

        [Fact]
        public async Task DeleteExternalServicerAsync_UsesExternalContractorsTable()
        {
            var db = new DatabaseService(ConnectionString);
            var executedSql = new List<string>();
            Dictionary<string, object?>? capturedParams = null;

            db.ExecuteNonQueryOverride = (sql, parameters, _) =>
            {
                executedSql.Add(sql);
                capturedParams = parameters?.ToDictionary(p => p.ParameterName, p => p.Value);
                return Task.FromResult(1);
            };

            try
            {
                await db.DeleteExternalServicerAsync(9).ConfigureAwait(false);

                var deleteSql = Assert.Single(executedSql);
                Assert.Contains("DELETE FROM external_contractors", deleteSql, StringComparison.OrdinalIgnoreCase);
                Assert.NotNull(capturedParams);
                Assert.Equal(9, capturedParams!["@id"]);
            }
            finally
            {
                db.ResetTestOverrides();
            }
        }
    }
}
