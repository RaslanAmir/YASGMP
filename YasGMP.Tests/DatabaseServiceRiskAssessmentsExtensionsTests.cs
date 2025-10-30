using System;
using System.Data;
using System.Threading.Tasks;
using Xunit;
using YasGMP.Services;

namespace YasGMP.Tests;

public class DatabaseServiceRiskAssessmentsExtensionsTests
{
    private const string ConnectionString = "Server=localhost;User Id=test;Password=test;Database=test;";

    [Fact]
    public async Task GetAllRiskAssessmentsFullAsync_MapsPreferredProjection_WithJoinedUsers()
    {
        var db = new DatabaseService(ConnectionString);
        var reviewDate = new DateTime(2025, 3, 4, 0, 0, 0, DateTimeKind.Utc);

        var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("code", typeof(string));
        table.Columns.Add("title", typeof(string));
        table.Columns.Add("status", typeof(string));
        table.Columns.Add("category", typeof(string));
        table.Columns.Add("risk_score", typeof(int));
        table.Columns.Add("risk_level", typeof(string));
        table.Columns.Add("review_date", typeof(DateTime));
        table.Columns.Add("owner_id", typeof(int));
        table.Columns.Add("owner_username", typeof(string));
        table.Columns.Add("owner_full_name", typeof(string));
        table.Columns.Add("approved_by_id", typeof(int));
        table.Columns.Add("approved_by_username", typeof(string));
        table.Columns.Add("approved_by_full_name", typeof(string));

        var row = table.NewRow();
        row["id"] = 42;
        row["code"] = "RA-2024-004";
        row["title"] = "Sterilization Cycle Review";
        row["status"] = "pending_review";
        row["category"] = "Quality";
        row["risk_score"] = 64;
        row["risk_level"] = "High";
        row["review_date"] = reviewDate;
        row["owner_id"] = 77;
        row["owner_username"] = "owner.user";
        row["owner_full_name"] = "Owner Person";
        row["approved_by_id"] = 88;
        row["approved_by_username"] = "approver.user";
        row["approved_by_full_name"] = "Approver Person";
        table.Rows.Add(row);

        db.ExecuteSelectOverride = (_, _, _) => Task.FromResult(table);

        try
        {
            var results = await db.GetAllRiskAssessmentsFullAsync().ConfigureAwait(false);

            var risk = Assert.Single(results);
            Assert.Equal(42, risk.Id);
            Assert.Equal("RA-2024-004", risk.Code);
            Assert.Equal("Sterilization Cycle Review", risk.Title);
            Assert.Equal("pending_review", risk.Status);
            Assert.Equal("Quality", risk.Category);
            Assert.Equal(64, risk.RiskScore);
            Assert.Equal("High", risk.RiskLevel);
            Assert.Equal(reviewDate, risk.ReviewDate);

            Assert.Equal(77, risk.OwnerId);
            Assert.Equal("owner.user", risk.OwnerUsername);
            Assert.Equal("Owner Person", risk.OwnerFullName);
            Assert.NotNull(risk.Owner);
            Assert.Equal(77, risk.Owner!.Id);
            Assert.Equal("Owner Person", risk.Owner.FullName);
            Assert.Equal("owner.user", risk.Owner.Username);

            Assert.Equal(88, risk.ApprovedById);
            Assert.Equal("approver.user", risk.ApprovedByUsername);
            Assert.Equal("Approver Person", risk.ApprovedByFullName);
            Assert.NotNull(risk.ApprovedBy);
            Assert.Equal(88, risk.ApprovedBy!.Id);
            Assert.Equal("Approver Person", risk.ApprovedBy.FullName);
            Assert.Equal("approver.user", risk.ApprovedBy.Username);
        }
        finally
        {
            db.ResetTestOverrides();
        }
    }

    [Fact]
    public async Task GetAllRiskAssessmentsFullAsync_AllowsLegacyProjection_WhenNewColumnsMissing()
    {
        var db = new DatabaseService(ConnectionString);

        var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("code", typeof(string));
        table.Columns.Add("title", typeof(string));
        table.Columns.Add("status", typeof(string));
        table.Columns.Add("category", typeof(string));
        table.Columns.Add("mitigation", typeof(string));
        table.Columns.Add("action_plan", typeof(string));
        table.Columns.Add("digital_signature", typeof(string));
        table.Columns.Add("note", typeof(string));
        table.Columns.Add("device_info", typeof(string));
        table.Columns.Add("session_id", typeof(string));
        table.Columns.Add("ip_address", typeof(string));

        var row = table.NewRow();
        row["id"] = 7;
        row["code"] = "RA-LEGACY";
        row["title"] = "Legacy Projection";
        row["status"] = "closed";
        row["category"] = "Equipment";
        row["mitigation"] = "Completed";
        row["action_plan"] = "No additional actions";
        row["digital_signature"] = "sig";
        row["note"] = "historical";
        row["device_info"] = "device";
        row["session_id"] = "session";
        row["ip_address"] = "127.0.0.1";
        table.Rows.Add(row);

        db.ExecuteSelectOverride = (_, _, _) => Task.FromResult(table);

        try
        {
            var results = await db.GetAllRiskAssessmentsFullAsync().ConfigureAwait(false);

            var risk = Assert.Single(results);
            Assert.Equal(7, risk.Id);
            Assert.Equal("RA-LEGACY", risk.Code);
            Assert.Equal("Legacy Projection", risk.Title);
            Assert.Equal("closed", risk.Status);
            Assert.Equal("Equipment", risk.Category);
            Assert.Null(risk.RiskScore);
            Assert.Equal(string.Empty, risk.RiskLevel);
            Assert.Null(risk.ReviewDate);
            Assert.Null(risk.OwnerId);
            Assert.Null(risk.Owner);
            Assert.Null(risk.OwnerUsername);
            Assert.Null(risk.OwnerFullName);
            Assert.Null(risk.ApprovedById);
            Assert.Null(risk.ApprovedBy);
            Assert.Null(risk.ApprovedByUsername);
            Assert.Null(risk.ApprovedByFullName);
        }
        finally
        {
            db.ResetTestOverrides();
        }
    }
}
