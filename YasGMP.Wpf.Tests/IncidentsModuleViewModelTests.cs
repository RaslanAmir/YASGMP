using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Tests;

public class IncidentsModuleViewModelTests
{
    [Fact]
    public async Task OnSaveAsync_AddMode_PersistsIncidentThroughAdapter()
    {
        var database = new DatabaseService();
        var incidents = new FakeIncidentCrudService();
        var auth = new TestAuthContext { CurrentUser = new User { Id = 5, FullName = "QA Manager" } };
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new IncidentsModuleViewModel(database, incidents, auth, filePicker, attachments, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.Mode = FormMode.Add;
        viewModel.Editor.Title = "Temperature deviation";
        viewModel.Editor.Description = "Fridge deviated from range.";
        viewModel.Editor.Type = "Deviation";
        viewModel.Editor.Priority = "High";
        viewModel.Editor.AssignedInvestigator = "QA";
        viewModel.Editor.DetectedAt = DateTime.UtcNow;
        viewModel.Editor.Status = "REPORTED";

        var saved = await InvokeSaveAsync(viewModel);

        Assert.True(saved);
        var persisted = Assert.Single(incidents.Saved);
        Assert.Equal("Temperature deviation", persisted.Title);
        Assert.Equal("Deviation", persisted.Type);
        Assert.False(viewModel.IsDirty);
    }

    [Fact]
    public async Task AttachEvidenceCommand_UploadsEvidenceViaAttachmentService()
    {
        var database = new DatabaseService();
        database.Incidents.Add(new Incident
        {
            Id = 3,
            Title = "Audit trail alert",
            Description = "Multiple failed logins detected",
            Type = "Security",
            Priority = "Medium",
            DetectedAt = DateTime.UtcNow.AddHours(-2),
            Status = "REPORTED"
        });

        var incidents = new FakeIncidentCrudService();
        incidents.Saved.Add(new Incident
        {
            Id = 3,
            Title = "Audit trail alert",
            Description = "Multiple failed logins detected",
            Type = "Security",
            Priority = "Medium",
            DetectedAt = database.Incidents[0].DetectedAt,
            Status = "REPORTED"
        });

        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 4, FullName = "Auditor" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "10.0.0.5"
        };
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();

        var bytes = Encoding.UTF8.GetBytes("incident evidence");
        filePicker.Files = new[]
        {
            new PickedFile("evidence.txt", "text/plain", () => Task.FromResult<Stream>(new MemoryStream(bytes, writable: false)), bytes.Length)
        };

        var viewModel = new IncidentsModuleViewModel(database, incidents, auth, filePicker, attachments, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        Assert.True(viewModel.AttachEvidenceCommand.CanExecute(null));
        await viewModel.AttachEvidenceCommand.ExecuteAsync(null);

        var upload = Assert.Single(attachments.Uploads);
        Assert.Equal("incidents", upload.EntityType);
        Assert.Equal(3, upload.EntityId);
        Assert.Equal("evidence.txt", upload.FileName);
    }

    private static Task<bool> InvokeSaveAsync(IncidentsModuleViewModel viewModel)
    {
        var method = typeof(IncidentsModuleViewModel)
            .GetMethod("OnSaveAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(IncidentsModuleViewModel), "OnSaveAsync");
        return (Task<bool>)method.Invoke(viewModel, null)!;
    }
}
