using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.ViewModels;
using YasGMP.Views.Dialogs;

namespace YasGMP.Tests;

public class MachineDialogTests
{
    [Fact]
    public void MachineEditDialog_XamlContainsCodeAndQrElements()
    {
        var document = LoadMachineEditDialogXaml();
        XNamespace ns = "http://schemas.microsoft.com/dotnet/2021/maui";

        var codeEntry = document
            .Descendants(ns + "Entry")
            .FirstOrDefault(element => string.Equals("{Binding Code}", (string?)element.Attribute("Text"), StringComparison.Ordinal));
        Assert.NotNull(codeEntry);

        var autoCodeButton = document
            .Descendants(ns + "Button")
            .FirstOrDefault(element => string.Equals("OnAutoCodeClicked", (string?)element.Attribute("Clicked"), StringComparison.Ordinal));
        Assert.NotNull(autoCodeButton);

        var qrButton = document
            .Descendants(ns + "Button")
            .FirstOrDefault(element => string.Equals("OnOpenQrClicked", (string?)element.Attribute("Clicked"), StringComparison.Ordinal));
        Assert.NotNull(qrButton);
    }

    [Fact]
    public void MachineViewModel_EnsureCodeAndQr_ProducesMauiFormattedOutputs()
    {
        var database = new DatabaseService("Server=localhost;User Id=test;Password=test;Database=test;");
        database.ExecuteSelectOverride = (_, _, _) => Task.FromResult(new DataTable());
        TestPlatformService? platform = null;
        Machine? machine = null;
        try
        {
            var audit = new AuditService(database);
            var rbac = new StubRbacService();
            var userService = new UserService(database, audit, rbac);
            var auth = new AuthService(userService, audit);
            platform = new TestPlatformService();
            var viewModel = new MachineViewModel(database, auth, new CodeGeneratorService(), new QRCodeService(), platform);

            machine = new Machine
            {
                Name = "Granulator",
                Manufacturer = "Contoso"
            };

            var method = typeof(MachineViewModel).GetMethod("EnsureCodeAndQr", BindingFlags.NonPublic | BindingFlags.Instance)
                         ?? throw new MissingMethodException(nameof(MachineViewModel), "EnsureCodeAndQr");
            method.Invoke(viewModel, new object[] { machine });

            Assert.Matches("^[A-Z0-9]{3}-[A-Z0-9]{3}-\\d{14}$", machine.Code);
            Assert.False(string.IsNullOrWhiteSpace(machine.QrCode));
            Assert.Equal(platform.Directory, Path.GetDirectoryName(machine.QrCode));
            Assert.Equal($"{machine.Code}.png", Path.GetFileName(machine.QrCode));
            Assert.True(File.Exists(machine.QrCode));
        }
        finally
        {
            database.ResetTestOverrides();
            if (!string.IsNullOrWhiteSpace(machine?.QrCode) && File.Exists(machine.QrCode))
            {
                File.Delete(machine.QrCode);
            }

            if (platform is not null && Directory.Exists(platform.Directory))
            {
                Directory.Delete(platform.Directory, recursive: true);
            }
        }
    }

    private static XDocument LoadMachineEditDialogXaml()
    {
        var mauiAssembly = typeof(MachineEditDialog).GetTypeInfo().Assembly;
        var resourceName = mauiAssembly
            .GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith("MachineEditDialog.xaml", StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(resourceName))
        {
            using var stream = mauiAssembly.GetManifestResourceStream(resourceName);
            if (stream is not null)
            {
                return XDocument.Load(stream);
            }
        }

        var testAssembly = typeof(MachineDialogTests).GetTypeInfo().Assembly;
        var linkedResourceName = testAssembly
            .GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith("Views.Dialogs.MachineEditDialog.xaml", StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(linkedResourceName))
        {
            using var stream = testAssembly.GetManifestResourceStream(linkedResourceName);
            if (stream is not null)
            {
                return XDocument.Load(stream);
            }
        }

        throw new InvalidOperationException("MachineEditDialog.xaml could not be loaded from the MAUI or test assemblies.");
    }


    private sealed class TestPlatformService : IPlatformService
    {
        public string Directory { get; } = Path.Combine(Path.GetTempPath(), "YasGMP", "MachineDialogTests", Guid.NewGuid().ToString("N"));

        public TestPlatformService()
        {
            Directory.CreateDirectory(Directory);
        }

        public string GetLocalIpAddress() => "127.0.0.1";
        public string GetOsVersion() => "UnitTestOS";
        public string GetHostName() => "UnitTestHost";
        public string GetUserName() => "UnitTester";

        public string GetAppDataDirectory()
            => Directory;
    }

    private sealed class StubRbacService : IRBACService
    {
        public Task AssertPermissionAsync(int userId, string permissionCode) => Task.CompletedTask;
        public Task<bool> HasPermissionAsync(int userId, string permissionCode) => Task.FromResult(false);
        public Task<List<string>> GetAllUserPermissionsAsync(int userId) => Task.FromResult(new List<string>());
        public Task GrantRoleAsync(int userId, int roleId, int grantedBy, DateTime? expiresAt = null, string reason = "") => Task.CompletedTask;
        public Task RevokeRoleAsync(int userId, int roleId, int revokedBy, string reason = "") => Task.CompletedTask;
        public Task<List<Role>> GetRolesForUserAsync(int userId) => Task.FromResult(new List<Role>());
        public Task<List<Role>> GetAvailableRolesForUserAsync(int userId) => Task.FromResult(new List<Role>());
        public Task GrantPermissionAsync(int userId, string permissionCode, int grantedBy, DateTime? expiresAt = null, string reason = "") => Task.CompletedTask;
        public Task RevokePermissionAsync(int userId, string permissionCode, int revokedBy, string reason = "") => Task.CompletedTask;
        public Task DelegatePermissionAsync(int fromUserId, int toUserId, string permissionCode, int grantedBy, DateTime expiresAt, string reason = "") => Task.CompletedTask;
        public Task RevokeDelegatedPermissionAsync(int delegatedPermissionId, int revokedBy, string reason = "") => Task.CompletedTask;
        public Task<List<Role>> GetAllRolesAsync() => Task.FromResult(new List<Role>());
        public Task<List<Permission>> GetAllPermissionsAsync() => Task.FromResult(new List<Permission>());
        public Task<List<Permission>> GetPermissionsForRoleAsync(int roleId) => Task.FromResult(new List<Permission>());
        public Task<List<Permission>> GetPermissionsNotInRoleAsync(int roleId) => Task.FromResult(new List<Permission>());
        public Task AddPermissionToRoleAsync(int roleId, int permissionId, int adminUserId, string reason = "") => Task.CompletedTask;
        public Task RemovePermissionFromRoleAsync(int roleId, int permissionId, int adminUserId, string reason = "") => Task.CompletedTask;
        public Task<int> CreateRoleAsync(Role role, int adminUserId) => Task.FromResult(0);
        public Task UpdateRoleAsync(Role role, int adminUserId) => Task.CompletedTask;
        public Task DeleteRoleAsync(int roleId, int adminUserId, string reason = "") => Task.CompletedTask;
        public Task<int> RequestPermissionAsync(int userId, string permissionCode, string reason) => Task.FromResult(0);
        public Task ApprovePermissionRequestAsync(int requestId, int approvedBy, string comment) => Task.CompletedTask;
        public Task DenyPermissionRequestAsync(int requestId, int deniedBy, string comment) => Task.CompletedTask;
    }
}
