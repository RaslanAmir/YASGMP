using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.Tests.TestDoubles;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Tests;

public class SecurityModuleViewModelTests
{
    [Fact]
    public async Task OnSaveAsync_AddMode_CreatesUserAndAssignsRoles()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var audit = new AuditService(database);
        var userService = new FakeUserCrudService();
        userService.SeedRole(new Role { Id = 1, Name = "Administrator", Description = "Admin" });
        userService.SeedRole(new Role { Id = 2, Name = "Quality", Description = "QA" });

        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 42, Username = "admin" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "127.0.0.1"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new SecurityModuleViewModel(database, audit, userService, auth, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.Mode = FormMode.Add;
        viewModel.Editor.Username = "new.user";
        viewModel.Editor.FullName = "New User";
        viewModel.Editor.Email = "new.user@example.com";
        viewModel.Editor.Role = "Administrator";
        viewModel.Editor.DepartmentName = "IT";
        viewModel.Editor.NewPassword = "TempPass123!";
        viewModel.Editor.ConfirmPassword = "TempPass123!";

        var adminRole = viewModel.RoleOptions.First();
        adminRole.IsSelected = true;

        var saved = await InvokeSaveAsync(viewModel);

        Assert.True(saved);
        var created = Assert.Single(userService.CreatedUsers);
        Assert.Equal("new.user", created.Username);
        Assert.Equal("New User", created.FullName);
        Assert.Equal("test-signature", created.DigitalSignature);
        var context = Assert.Single(userService.SavedContexts);
        Assert.Equal("test-signature", context.SignatureHash);
        Assert.Equal("password", context.SignatureMethod);
        Assert.Equal("valid", context.SignatureStatus);
        Assert.Equal("Automated test", context.SignatureNote);
        Assert.Collection(signatureDialog.Requests, ctx =>
        {
            Assert.Equal("users", ctx.TableName);
            Assert.Equal(0, ctx.RecordId);
        });
        Assert.Single(signatureDialog.PersistedResults);
        var persistedSignature = signatureDialog.PersistedResults[0];
        Assert.Equal(created.Id, persistedSignature.Signature.RecordId);
        Assert.Equal(signatureDialog.LastPersistedSignatureId, persistedSignature.Signature.Id);
        Assert.True(persistedSignature.Signature.Id > 0);
        var persistedMetadata = Assert.Single(signatureDialog.PersistedSignatureRecords);
        Assert.Equal(persistedSignature.Signature.Id, persistedMetadata.SignatureId);
        Assert.Equal(created.Id, persistedMetadata.RecordId);
        Assert.Equal("test-signature", persistedMetadata.SignatureHash);
        Assert.Equal("password", persistedMetadata.Method);
        Assert.Equal("valid", persistedMetadata.Status);
        Assert.Equal("Automated test", persistedMetadata.Note);
        var assignment = Assert.Single(userService.RoleAssignments);
        Assert.Equal(created.Id, assignment.UserId);
        Assert.Contains(adminRole.RoleId, assignment.Roles);
    }

    [Fact]
    public async Task OnSaveAsync_UpdateMode_UpdatesExistingUserAndClearsPassword()
    {
        var database = new DatabaseService();
        var userService = new FakeUserCrudService();
        userService.SeedRole(new Role { Id = 1, Name = "Administrator" });
        userService.SeedRole(new Role { Id = 2, Name = "Quality" });
        userService.SeedUser(new User
        {
            Id = 7,
            Username = "existing",
            FullName = "Existing User",
            Email = "existing@example.com",
            Role = "Administrator",
            Active = true,
            RoleIds = new[] { 1 }
        });

        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 99, Username = "admin" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "127.0.0.1"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new SecurityModuleViewModel(database, audit, userService, auth, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.SelectedRecord = viewModel.Records.First();
        viewModel.Mode = FormMode.Update;
        viewModel.Editor.Email = "updated@example.com";
        viewModel.Editor.Role = "Administrator";
        viewModel.Editor.NewPassword = "UpdatedPass!1";
        viewModel.Editor.ConfirmPassword = "UpdatedPass!1";

        var qualityRole = viewModel.RoleOptions.First(r => r.RoleId == 2);
        qualityRole.IsSelected = true;

        var saved = await InvokeSaveAsync(viewModel);

        Assert.True(saved);
        var updated = Assert.Single(userService.UpdatedUsers);
        Assert.Equal("updated@example.com", updated.Email);
        Assert.Equal(7, updated.Id);
        Assert.Equal(string.Empty, viewModel.Editor.NewPassword);
        Assert.Equal(string.Empty, viewModel.Editor.ConfirmPassword);
        var contexts = userService.SavedContexts.ToList();
        Assert.NotEmpty(contexts);
        var context = contexts[^1];
        Assert.Equal("test-signature", context.SignatureHash);
        Assert.Equal("password", context.SignatureMethod);
        Assert.Equal("valid", context.SignatureStatus);
        Assert.Equal("Automated test", context.SignatureNote);
        Assert.Collection(signatureDialog.Requests, ctx =>
        {
            Assert.Equal("users", ctx.TableName);
            Assert.Equal(7, ctx.RecordId);
        });
        Assert.Single(signatureDialog.PersistedResults);
        var persistedSignature = signatureDialog.PersistedResults[0];
        Assert.Equal(7, persistedSignature.Signature.RecordId);
        Assert.Equal(signatureDialog.LastPersistedSignatureId, persistedSignature.Signature.Id);
        Assert.True(persistedSignature.Signature.Id > 0);
        var persistedMetadata = Assert.Single(signatureDialog.PersistedSignatureRecords);
        Assert.Equal(persistedSignature.Signature.Id, persistedMetadata.SignatureId);
        Assert.Equal(7, persistedMetadata.RecordId);
        Assert.Equal("test-signature", persistedMetadata.SignatureHash);
        Assert.Equal("password", persistedMetadata.Method);
        Assert.Equal("valid", persistedMetadata.Status);
        Assert.Equal("Automated test", persistedMetadata.Note);
        var assignment = userService.RoleAssignments.Last();
        Assert.Contains(2, assignment.Roles);
    }

    private static Task<bool> InvokeSaveAsync(SecurityModuleViewModel viewModel)
    {
        var method = typeof(SecurityModuleViewModel)
            .GetMethod("OnSaveAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(SecurityModuleViewModel), "OnSaveAsync");
        return (Task<bool>)method.Invoke(viewModel, null)!;
    }
}
