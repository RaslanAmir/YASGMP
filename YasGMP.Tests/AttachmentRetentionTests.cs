using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using YasGMP.Data;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.Tests
{
    public class AttachmentRetentionTests
    {
        [Fact]
        public void AttachmentSeedData_AssignsDefaults_WhenRetentionMissing()
        {
            var options = new DbContextOptionsBuilder<YasGmpDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new YasGmpDbContext(options);
            context.Attachments.Add(new Attachment
            {
                Id = 1,
                Name = "Doc",
                FileName = "doc.pdf",
                UploadedAt = DateTime.UtcNow
            });
            context.SaveChanges();

            AttachmentSeedData.EnsureSeeded(context);

            var policy = Assert.Single(context.RetentionPolicies);
            Assert.Equal("legacy-default", policy.PolicyName);
            Assert.False(policy.LegalHold);
            Assert.False(policy.ReviewRequired);
            Assert.Equal("soft", policy.DeleteMode);
        }

        [Fact]
        public async Task AttachmentRetentionEnforcer_SoftDeletesAndLogs()
        {
            var db = new DatabaseService("Server=localhost;User Id=test;Password=test;Database=test;");

            var table = new DataTable();
            table.Columns.Add("policy_id", typeof(int));
            table.Columns.Add("attachment_id", typeof(int));
            table.Columns.Add("retain_until", typeof(DateTime));
            table.Columns.Add("min_retain_days", typeof(int));
            table.Columns.Add("max_retain_days", typeof(int));
            table.Columns.Add("legal_hold", typeof(bool));
            table.Columns.Add("delete_mode", typeof(string));
            table.Columns.Add("review_required", typeof(bool));
            table.Columns.Add("file_name", typeof(string));
            table.Columns.Add("status", typeof(string));
            table.Columns.Add("is_deleted", typeof(bool));
            table.Columns.Add("soft_deleted_at", typeof(DateTime));
            table.Columns.Add("uploaded_at", typeof(DateTime));
            table.Columns.Add("tenant_id", typeof(int));
            table.Columns.Add("tenant_code", typeof(string));

            table.Rows.Add(
                7,
                42,
                DateTime.UtcNow.AddDays(-3),
                DBNull.Value,
                DBNull.Value,
                false,
                "soft",
                false,
                "report.pdf",
                "uploaded",
                false,
                DBNull.Value,
                DateTime.UtcNow.AddDays(-90),
                DBNull.Value,
                "QA"
            );

            var executed = new List<string>();

            db.ExecuteSelectOverride = (sql, parameters, _) =>
            {
                if (sql.IndexOf("retention_policies", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return Task.FromResult(table);
                }

                return Task.FromResult(new DataTable());
            };

            db.ExecuteNonQueryOverride = (sql, parameters, _) =>
            {
                executed.Add(sql);
                return Task.FromResult(1);
            };

            try
            {
                var enforcer = new AttachmentRetentionEnforcer(db);
                var result = await enforcer.RunOnceAsync().ConfigureAwait(false);

                Assert.Equal(1, result.SoftDeletes);
                Assert.Equal(0, result.HardPurges);
                Assert.Contains(executed, sql => sql.Contains("UPDATE attachments", StringComparison.OrdinalIgnoreCase));
                Assert.Contains(executed, sql => sql.Contains("INSERT INTO system_event_log", StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                db.ResetTestOverrides();
            }
        }
    }
}
