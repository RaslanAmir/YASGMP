using System;
using System.Data;
using System.Threading.Tasks;
using Xunit;
using YasGMP.Services;

namespace YasGMP.Tests;

public class DatabaseServiceQualificationsExtensionsTests
{
    private const string ConnectionString = "Server=localhost;User Id=test;Password=test;Database=test;";

    [Fact]
    public async Task GetAllQualificationsAsync_MapsPreferredProjection_WithJoinedMetadata()
    {
        var db = new DatabaseService(ConnectionString);
        var qualificationDate = new DateTime(2024, 11, 5, 0, 0, 0, DateTimeKind.Utc);
        var expiryDate = new DateTime(2025, 11, 5, 0, 0, 0, DateTimeKind.Utc);

        var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("component_id", typeof(int));
        table.Columns.Add("machine_id", typeof(int));
        table.Columns.Add("supplier_id", typeof(int));
        table.Columns.Add("qualification_type", typeof(string));
        table.Columns.Add("type", typeof(string));
        table.Columns.Add("status", typeof(string));
        table.Columns.Add("qualification_date", typeof(DateTime));
        table.Columns.Add("expiry_date", typeof(DateTime));
        table.Columns.Add("next_due", typeof(DateTime));
        table.Columns.Add("certificate_number", typeof(string));
        table.Columns.Add("qualified_by_id", typeof(int));
        table.Columns.Add("qualified_by_username", typeof(string));
        table.Columns.Add("qualified_by_full_name", typeof(string));
        table.Columns.Add("approved_by_id", typeof(int));
        table.Columns.Add("approved_at", typeof(DateTime));
        table.Columns.Add("approved_by_username", typeof(string));
        table.Columns.Add("approved_by_full_name", typeof(string));
        table.Columns.Add("component_code", typeof(string));
        table.Columns.Add("component_name", typeof(string));
        table.Columns.Add("machine_code", typeof(string));
        table.Columns.Add("machine_name", typeof(string));
        table.Columns.Add("supplier_name", typeof(string));

        var row = table.NewRow();
        row["id"] = 5;
        row["component_id"] = 10;
        row["machine_id"] = 20;
        row["supplier_id"] = 30;
        row["qualification_type"] = "Process Equipment Review";
        row["type"] = " process equipment ";
        row["status"] = "In Progress";
        row["qualification_date"] = qualificationDate;
        row["expiry_date"] = expiryDate;
        row["next_due"] = expiryDate.AddMonths(6);
        row["certificate_number"] = "CERT-123";
        row["qualified_by_id"] = 40;
        row["qualified_by_username"] = "qual.user";
        row["qualified_by_full_name"] = "Qualification User";
        row["approved_by_id"] = 50;
        row["approved_at"] = expiryDate;
        row["approved_by_username"] = "approver.user";
        row["approved_by_full_name"] = "Approver User";
        row["component_code"] = "COMP-10";
        row["component_name"] = "Component Ten";
        row["machine_code"] = "MACH-20";
        row["machine_name"] = "Machine Twenty";
        row["supplier_name"] = "Supplier Thirty";
        table.Rows.Add(row);

        db.ExecuteSelectOverride = (_, _, _) => Task.FromResult(table);

        try
        {
            var results = await db.GetAllQualificationsAsync().ConfigureAwait(false);

            var qualification = Assert.Single(results);
            Assert.Equal(5, qualification.Id);
            Assert.Equal("CERT-123", qualification.Code);
            Assert.Equal("PROCESS_EQUIPMENT", qualification.Type);
            Assert.Equal("Process Equipment Review", qualification.Description);
            Assert.Equal("in_progress", qualification.Status);
            Assert.Equal(qualificationDate, qualification.Date);
            Assert.Equal(expiryDate, qualification.ExpiryDate);

            Assert.NotNull(qualification.Machine);
            Assert.Equal(20, qualification.Machine!.Id);
            Assert.Equal("MACH-20", qualification.Machine.Code);
            Assert.Equal("Machine Twenty", qualification.Machine.Name);

            Assert.NotNull(qualification.Component);
            Assert.Equal(10, qualification.Component!.Id);
            Assert.Equal("COMP-10", qualification.Component.Code);
            Assert.Equal("Component Ten", qualification.Component.Name);

            Assert.NotNull(qualification.Supplier);
            Assert.Equal(30, qualification.Supplier!.Id);
            Assert.Equal("Supplier Thirty", qualification.Supplier.Name);

            Assert.NotNull(qualification.QualifiedBy);
            Assert.Equal(40, qualification.QualifiedBy!.Id);
            Assert.Equal("qual.user", qualification.QualifiedBy.Username);
            Assert.Equal("Qualification User", qualification.QualifiedBy.FullName);

            Assert.NotNull(qualification.ApprovedBy);
            Assert.Equal(50, qualification.ApprovedBy!.Id);
            Assert.Equal("approver.user", qualification.ApprovedBy.Username);
            Assert.Equal("Approver User", qualification.ApprovedBy.FullName);
        }
        finally
        {
            db.ResetTestOverrides();
        }
    }

    [Fact]
    public async Task GetAllQualificationsAsync_AllowsLegacyProjection_WhenNewColumnsMissing()
    {
        var db = new DatabaseService(ConnectionString);

        var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("component_id", typeof(int));
        table.Columns.Add("supplier_id", typeof(int));
        table.Columns.Add("qualification_date", typeof(DateTime));
        table.Columns.Add("status", typeof(string));
        table.Columns.Add("certificate_number", typeof(string));

        var row = table.NewRow();
        row["id"] = 9;
        row["component_id"] = DBNull.Value;
        row["supplier_id"] = DBNull.Value;
        row["qualification_date"] = new DateTime(2023, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        row["status"] = "Active";
        row["certificate_number"] = "LEG-001";
        table.Rows.Add(row);

        db.ExecuteSelectOverride = (_, _, _) => Task.FromResult(table);

        try
        {
            var results = await db.GetAllQualificationsAsync().ConfigureAwait(false);

            var qualification = Assert.Single(results);
            Assert.Equal(9, qualification.Id);
            Assert.Equal("LEG-001", qualification.Code);
            Assert.Equal("COMPONENT", qualification.Type);
            Assert.Equal(string.Empty, qualification.Description);
            Assert.Equal("active", qualification.Status);
            Assert.Null(qualification.ExpiryDate);
            Assert.Null(qualification.Machine);
            Assert.Null(qualification.Component);
            Assert.Null(qualification.Supplier);
            Assert.Null(qualification.QualifiedBy);
            Assert.Null(qualification.ApprovedBy);
        }
        finally
        {
            db.ResetTestOverrides();
        }
    }
}
