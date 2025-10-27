using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Xunit;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.ViewModels;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.Tests.TestDoubles;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Tests;

public class QualificationsModuleViewModelTests
{
    [Fact]
    public async Task InitializeAsync_LoadsQualificationsAndProjectsRecords()
    {
        var database = CreateDatabase();
        SeedQualifications(database);
        var (module, shared) = CreateModule(database);

        await module.InitializeAsync(null).ConfigureAwait(false);

        Assert.Equal(shared.FilteredQualifications.Count, module.Records.Count);
        Assert.Equal(shared.StatusMessage, module.StatusMessage);
        var selected = Assert.NotNull(module.SelectedRecord);
        var firstQualification = shared.FilteredQualifications.First();

        Assert.Equal(firstQualification.Id.ToString(CultureInfo.InvariantCulture), selected.Key);
        Assert.Equal(string.Format(CultureInfo.CurrentCulture, "{0} — {1}", "Installation Qualification", "Filling Line 1"), selected.Title);
        Assert.Equal(firstQualification.Code, selected.Code);
        Assert.Contains(selected.InspectorFields, field => field.Value == "Filling Line 1");
        Assert.Contains(selected.InspectorFields, field => field.Value == "IQ");
        Assert.Contains(selected.InspectorFields, field => field.Value == "QA Lead");
        Assert.Contains(selected.InspectorFields, field => field.Value == "Quality Director");

        module.Dispose();
    }

    [Fact]
    public async Task SearchStatusTypeFilters_StayInSyncWithSharedViewModel()
    {
        var database = CreateDatabase();
        SeedQualifications(database);
        var (module, shared) = CreateModule(database);

        await module.InitializeAsync(null).ConfigureAwait(false);

        module.SearchText = "autoclave";
        Assert.Equal("autoclave", shared.SearchTerm);

        shared.SearchTerm = "centrifuge";
        Assert.Equal("centrifuge", module.SearchText);

        module.StatusFilter = "valid";
        Assert.Equal("valid", shared.StatusFilter);
        Assert.All(shared.FilteredQualifications, qualification => Assert.Equal("valid", qualification.Status));
        Assert.All(module.Records, record => Assert.Equal("Valid", record.Status));

        shared.TypeFilter = "PQ";
        Assert.Equal("PQ", module.TypeFilter);

        var expectedKeys = shared.FilteredQualifications
            .Select(item => item.Id.ToString(CultureInfo.InvariantCulture))
            .ToList();
        var actualKeys = module.Records.Select(record => record.Key).ToList();
        Assert.Equal(expectedKeys, actualKeys);

        module.Dispose();
    }

    [Fact]
    public async Task ToolbarCommands_InvokeUnderlyingDatabaseWorkflow()
    {
        var database = CreateDatabase();
        SeedQualifications(database);
        var (module, shared) = CreateModule(database, sessionId: "session-qualifications-tests");

        await module.InitializeAsync(null).ConfigureAwait(false);

        var newQualification = new Qualification
        {
            QualificationType = "DQ",
            Type = "DQ",
            Code = "DQ-4004",
            Description = "Design qualification for HVAC system",
            Status = "scheduled",
            Date = DateTime.Today.AddDays(-2),
            ExpiryDate = DateTime.Today.AddMonths(6),
            CertificateNumber = "CERT-4004",
            Machine = new Machine { Name = "HVAC System" },
            QualifiedBy = new User { Id = 91, FullName = "Validation Engineer", Username = "validation" },
            ApprovedBy = new User { Id = 92, FullName = "Quality Reviewer", Username = "qa.reviewer" },
            ApprovedAt = DateTime.Today.AddDays(-1)
        };
        shared.SelectedQualification = newQualification;

        await module.AddCommand.ExecuteAsync(null).ConfigureAwait(false);
        var addCall = Assert.Single(database.QualificationAddCalls);
        Assert.Equal("DQ-4004", addCall.Qualification.Code);
        Assert.Contains(database.QualificationAuditEntries, entry => entry.Action == "CREATE");
        Assert.Equal(SeededQualificationCount + 1, database.Qualifications.Count);

        var updateTarget = shared.Qualifications.First(q => q.Status == "valid");
        shared.SelectedQualification = updateTarget;
        shared.SelectedQualification.Status = "expired";
        shared.SelectedQualification.CertificateNumber = "CERT-UPDATED";

        await module.UpdateCommand.ExecuteAsync(null).ConfigureAwait(false);
        var updateCall = Assert.Single(database.QualificationUpdateCalls);
        Assert.Equal(updateTarget.Id, updateCall.Qualification.Id);
        Assert.Contains(database.QualificationAuditEntries, entry => entry.Action == "UPDATE");
        Assert.Equal("expired", database.Qualifications.Single(q => q.Id == updateTarget.Id).Status);

        var deleteTarget = shared.Qualifications.Last();
        shared.SelectedQualification = deleteTarget;

        await module.DeleteCommand.ExecuteAsync(null).ConfigureAwait(false);
        var deleteCall = Assert.Single(database.QualificationDeleteCalls);
        Assert.Equal(deleteTarget.Id, deleteCall.QualificationId);
        Assert.Contains(database.QualificationAuditEntries, entry => entry.Action == "DELETE");
        Assert.DoesNotContain(database.Qualifications, qualification => qualification.Id == deleteTarget.Id);

        var rollbackTarget = shared.Qualifications.First();
        shared.SelectedQualification = rollbackTarget;

        await module.RollbackCommand.ExecuteAsync(null).ConfigureAwait(false);
        var rollbackCall = Assert.Single(database.QualificationRollbackCalls);
        Assert.Equal(rollbackTarget.Id, rollbackCall.QualificationId);

        await module.ExportCommand.ExecuteAsync(null).ConfigureAwait(false);
        var exportCall = Assert.Single(database.QualificationExportCalls);
        Assert.Equal(shared.FilteredQualifications.Count, exportCall.Items.Count);
        Assert.Contains(database.QualificationAuditEntries, entry => entry.Action == "EXPORT");
        Assert.Equal(shared.StatusMessage, module.StatusMessage);

        module.Dispose();
    }

    [Fact]
    public async Task StatusMessage_PropagatesSuccessAndFailureFromShared()
    {
        var database = CreateDatabase();
        SeedQualifications(database);
        var (module, shared) = CreateModule(database);

        await module.InitializeAsync(null).ConfigureAwait(false);

        await module.ExportCommand.ExecuteAsync(null).ConfigureAwait(false);
        Assert.Equal("Qualifications exported successfully.", shared.StatusMessage);
        Assert.Equal(shared.StatusMessage, module.StatusMessage);

        database.QualificationsWorkflowException = new InvalidOperationException("workflow blocked");

        await module.ExportCommand.ExecuteAsync(null).ConfigureAwait(false);
        Assert.Equal("Export failed: workflow blocked", shared.StatusMessage);
        Assert.Equal(shared.StatusMessage, module.StatusMessage);

        database.QualificationsWorkflowException = null;
        module.Dispose();
    }

    [Fact]
    public async Task Dispose_UnsubscribesEventHandlersAndCollections()
    {
        var database = CreateDatabase();
        SeedQualifications(database);
        var (module, shared) = CreateModule(database);

        await module.InitializeAsync(null).ConfigureAwait(false);

        var addHandlers = GetCanExecuteChangedHandlers(shared.AddQualificationCommand);
        var updateHandlers = GetCanExecuteChangedHandlers(shared.UpdateQualificationCommand);
        var deleteHandlers = GetCanExecuteChangedHandlers(shared.DeleteQualificationCommand);
        var rollbackHandlers = GetCanExecuteChangedHandlers(shared.RollbackQualificationCommand);
        var exportHandlers = GetCanExecuteChangedHandlers(shared.ExportQualificationsCommand);

        Assert.Contains(addHandlers, handler => ReferenceEquals(handler.Target, module));
        Assert.Contains(updateHandlers, handler => ReferenceEquals(handler.Target, module));
        Assert.Contains(deleteHandlers, handler => ReferenceEquals(handler.Target, module));
        Assert.Contains(rollbackHandlers, handler => ReferenceEquals(handler.Target, module));
        Assert.Contains(exportHandlers, handler => ReferenceEquals(handler.Target, module));

        var collectionHandlers = GetCollectionChangedHandlers(shared.FilteredQualifications);
        Assert.Contains(collectionHandlers, handler => ReferenceEquals(handler.Target, module));

        module.Dispose();

        addHandlers = GetCanExecuteChangedHandlers(shared.AddQualificationCommand);
        updateHandlers = GetCanExecuteChangedHandlers(shared.UpdateQualificationCommand);
        deleteHandlers = GetCanExecuteChangedHandlers(shared.DeleteQualificationCommand);
        rollbackHandlers = GetCanExecuteChangedHandlers(shared.RollbackQualificationCommand);
        exportHandlers = GetCanExecuteChangedHandlers(shared.ExportQualificationsCommand);
        collectionHandlers = GetCollectionChangedHandlers(shared.FilteredQualifications);

        Assert.DoesNotContain(addHandlers, handler => ReferenceEquals(handler.Target, module));
        Assert.DoesNotContain(updateHandlers, handler => ReferenceEquals(handler.Target, module));
        Assert.DoesNotContain(deleteHandlers, handler => ReferenceEquals(handler.Target, module));
        Assert.DoesNotContain(rollbackHandlers, handler => ReferenceEquals(handler.Target, module));
        Assert.DoesNotContain(exportHandlers, handler => ReferenceEquals(handler.Target, module));
        Assert.DoesNotContain(collectionHandlers, handler => ReferenceEquals(handler.Target, module));
    }

    private const int SeededQualificationCount = 3;

    private static (QualificationsModuleViewModel Module, QualificationViewModel Shared) CreateModule(
        DatabaseService database,
        string? sessionId = null)
    {
        var auditService = new AuditService(database);
        var user = new User
        {
            Id = 501,
            FullName = "Quality Admin",
            Username = "quality.admin",
            Role = "admin"
        };

        var authService = AuthServiceTestHelper.CreateAuthenticatedAuthService(
            database,
            auditService,
            user: user,
            sessionId: sessionId ?? "session-qualifications-module-tests");

        var shared = new QualificationViewModel(database, authService);
        var localization = CreateLocalizationService();
        var module = new QualificationsModuleViewModel(
            shared,
            localization,
            new StubCflDialogService(),
            new StubShellInteractionService(),
            new StubModuleNavigationService());

        return (module, shared);
    }

    private static DatabaseService CreateDatabase() => new();

    private static void SeedQualifications(DatabaseService database)
    {
        database.Qualifications.Clear();
        database.Qualifications.AddRange(new[]
        {
            new Qualification
            {
                Id = 3101,
                QualificationType = "IQ",
                Type = "IQ",
                Code = "IQ-3101",
                Description = "Installation qualification for filling line",
                Status = "valid",
                Date = DateTime.Today.AddDays(-10),
                ExpiryDate = DateTime.Today.AddMonths(18),
                CertificateNumber = "CERT-3101",
                Machine = new Machine { Name = "Filling Line 1" },
                QualifiedBy = new User { Id = 201, FullName = "QA Lead", Username = "qa.lead" },
                ApprovedBy = new User { Id = 202, FullName = "Quality Director", Username = "qa.director" },
                ApprovedAt = DateTime.Today.AddDays(-5)
            },
            new Qualification
            {
                Id = 3102,
                QualificationType = "OQ",
                Type = "OQ",
                Code = "OQ-3102",
                Description = "Operational qualification for autoclave",
                Status = "expired",
                Date = DateTime.Today.AddYears(-1),
                ExpiryDate = DateTime.Today.AddDays(-30),
                CertificateNumber = "CERT-3102",
                Supplier = new Supplier { Id = 301, Name = "Sterilization Co." },
                QualifiedBy = new User { Id = 203, FullName = "Validation Specialist", Username = "validation.specialist" },
                ApprovedBy = new User { Id = 204, FullName = "Quality Supervisor", Username = "qa.supervisor" },
                ApprovedAt = DateTime.Today.AddYears(-1).AddDays(2)
            },
            new Qualification
            {
                Id = 3103,
                QualificationType = "PQ",
                Type = "PQ",
                Code = "PQ-3103",
                Description = "Performance qualification for centrifuge",
                Status = "scheduled",
                Date = DateTime.Today.AddMonths(-2),
                ExpiryDate = DateTime.Today.AddMonths(12),
                CertificateNumber = "CERT-3103",
                Machine = new Machine { Name = "Production Centrifuge" },
                QualifiedBy = new User { Id = 205, FullName = "Process Engineer", Username = "process.engineer" },
                ApprovedBy = new User { Id = 206, FullName = "Quality Manager", Username = "qa.manager" },
                ApprovedAt = DateTime.Today.AddMonths(-1)
            }
        });
    }

    private static ILocalizationService CreateLocalizationService()
    {
        var resources = new Dictionary<string, IDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Module.Title.Qualifications"] = "Qualifications",
                ["Module.Description.Qualifications"] = "Manage equipment and supplier qualifications",
                ["Module.Qualifications.Field.Equipment"] = "Equipment",
                ["Module.Qualifications.Field.Type"] = "Type",
                ["Module.Qualifications.Field.Certificate"] = "Certificate",
                ["Module.Qualifications.Field.EffectiveDate"] = "Effective Date",
                ["Module.Qualifications.Field.DueDate"] = "Due Date",
                ["Module.Qualifications.Field.QualifiedBy"] = "Qualified By",
                ["Module.Qualifications.Field.ApprovedBy"] = "Approved By",
                ["Module.Qualifications.Field.Status"] = "Status",
                ["Module.Qualifications.Status.Valid"] = "Valid",
                ["Module.Qualifications.Status.Expired"] = "Expired",
                ["Module.Qualifications.Status.Scheduled"] = "Scheduled",
                ["Module.Qualifications.Status.InProgress"] = "In Progress",
                ["Module.Qualifications.Status.Rejected"] = "Rejected",
                ["Module.Qualifications.Type.IQ"] = "Installation Qualification",
                ["Module.Qualifications.Type.OQ"] = "Operational Qualification",
                ["Module.Qualifications.Type.PQ"] = "Performance Qualification",
                ["Module.Qualifications.Type.DQ"] = "Design Qualification",
                ["Module.Qualifications.Type.VQ"] = "Vendor Qualification",
                ["Module.Qualifications.Type.SAT"] = "Site Acceptance Test",
                ["Module.Qualifications.Type.FAT"] = "Factory Acceptance Test",
                ["Module.Qualifications.Type.Requalification"] = "Requalification",
                ["Module.Qualifications.Title.UnknownEquipment"] = "Unknown Equipment"
            }
        };

        return new FakeLocalizationService(resources, "en");
    }

    private static IEnumerable<Delegate> GetCanExecuteChangedHandlers(System.Windows.Input.ICommand command)
    {
        if (command is null)
        {
            return Array.Empty<Delegate>();
        }

        var type = command.GetType();
        var field = type.GetField("CanExecuteChanged", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? type.GetField("_canExecuteChanged", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? type.GetField("canExecuteChanged", BindingFlags.Instance | BindingFlags.NonPublic);

        var value = field?.GetValue(command) as Delegate;
        return value?.GetInvocationList() ?? Array.Empty<Delegate>();
    }

    private static IEnumerable<Delegate> GetCollectionChangedHandlers(INotifyCollectionChanged collection)
    {
        if (collection is null)
        {
            return Array.Empty<Delegate>();
        }

        var type = collection.GetType();
        var field = type.GetField("CollectionChanged", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? typeof(ObservableCollection<Qualification>).GetField("CollectionChanged", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? typeof(INotifyCollectionChanged).GetField("CollectionChanged", BindingFlags.Instance | BindingFlags.NonPublic);

        var value = field?.GetValue(collection) as Delegate;
        return value?.GetInvocationList() ?? Array.Empty<Delegate>();
    }
}
