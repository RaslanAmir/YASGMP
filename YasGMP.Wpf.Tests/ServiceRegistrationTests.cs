using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using YasGMP.AppCore.DependencyInjection;
using YasGMP.Models;
using YasGMP.Models.Enums;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.ViewModels;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Tests;

public class ServiceRegistrationTests
{
    [Fact]
    public void EnsureAuditServiceSingleton_ReplacesPreviousRegistrations()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new DatabaseService("Server=localhost;Database=unit_test;Uid=test;Pwd=test;"));
        services.AddTransient<AuditService>();

        YasGmpCoreServiceGuards.EnsureAuditServiceSingleton(services);

        var descriptors = services.Where(d => d.ServiceType == typeof(AuditService)).ToList();
        Assert.Single(descriptors);
        Assert.Equal(ServiceLifetime.Singleton, descriptors[0].Lifetime);

        using var provider = services.BuildServiceProvider();
        var first = provider.GetRequiredService<AuditService>();
        var second = provider.GetRequiredService<AuditService>();

        Assert.Same(first, second);
    }

    [Fact]
    public void WpfShellServices_ResolveAuditModuleViewModel()
    {
        const string connection = "Server=localhost;Database=unit_test;Uid=test;Pwd=test;";
        var services = new ServiceCollection();

        services.AddYasGmpCoreServices(core =>
        {
            core.UseConnectionString(connection);
            core.UseDatabaseService<DatabaseService>((_, conn) => new DatabaseService(conn));

            var svc = core.Services;
            svc.AddSingleton<AuditService>();
            svc.AddSingleton<ExportService>();
            svc.AddSingleton<ICflDialogService, StubCflDialogService>();
            svc.AddSingleton<IShellInteractionService, StubShellInteractionService>();
            svc.AddSingleton<IModuleNavigationService, StubModuleNavigationService>();
            svc.AddTransient<AuditModuleViewModel>();
        });

        using var provider = services.BuildServiceProvider();

        var descriptors = services.Where(d => d.ServiceType == typeof(AuditService)).ToList();
        Assert.Single(descriptors);
        Assert.Equal(ServiceLifetime.Singleton, descriptors[0].Lifetime);

        var viewModel = provider.GetRequiredService<AuditModuleViewModel>();
        Assert.NotNull(viewModel);
    }

    [Fact]
    public void WpfShellServices_ResolveApiAuditModuleViewModel()
    {
        const string connection = "Server=localhost;Database=unit_test;Uid=test;Pwd=test;";
        var services = new ServiceCollection();

        services.AddYasGmpCoreServices(core =>
        {
            core.UseConnectionString(connection);
            core.UseDatabaseService<DatabaseService>((_, conn) => new DatabaseService(conn));

            var svc = core.Services;
            svc.AddSingleton<AuditService>();
            svc.AddSingleton<ICflDialogService, StubCflDialogService>();
            svc.AddSingleton<IShellInteractionService, StubShellInteractionService>();
            svc.AddSingleton<IModuleNavigationService, StubModuleNavigationService>();
            svc.AddTransient<ApiAuditModuleViewModel>();
        });

        using var provider = services.BuildServiceProvider();
        var descriptor = services.Single(d => d.ServiceType == typeof(AuditService));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);

        var viewModel = provider.GetRequiredService<ApiAuditModuleViewModel>();
        Assert.NotNull(viewModel);
    }

    [Fact]
    public void WpfShellServices_ResolveDeviationServices()
    {
        const string connection = "Server=localhost;Database=unit_test;Uid=test;Pwd=test;";
        var services = new ServiceCollection();

        services.AddYasGmpCoreServices(core =>
        {
            core.UseConnectionString(connection);
            core.UseDatabaseService<DatabaseService>((_, conn) => new DatabaseService(conn));

            var svc = core.Services;
            svc.AddSingleton<IDeviationAuditService, StubDeviationAuditService>();
            svc.AddTransient<DeviationService>();
            svc.AddTransient<IDeviationCrudService, DeviationCrudServiceAdapter>();
        });

        using var provider = services.BuildServiceProvider();

        var deviationService = provider.GetRequiredService<DeviationService>();
        Assert.NotNull(deviationService);

        var deviationCrudService = provider.GetRequiredService<IDeviationCrudService>();
        Assert.NotNull(deviationCrudService);
    }

    [Fact]
    public void WpfShellServices_ResolveRiskAssessmentModule()
    {
        const string connection = "Server=localhost;Database=unit_test;Uid=test;Pwd=test;";
        var services = new ServiceCollection();

        services.AddYasGmpCoreServices(core =>
        {
            core.UseConnectionString(connection);
            core.UseDatabaseService<DatabaseService>((_, conn) => new DatabaseService(conn));

            var svc = core.Services;
            svc.AddSingleton<AuditService>();
            svc.AddSingleton<IRBACService, StubRbacService>();
            svc.AddSingleton<UserService>();
            svc.AddSingleton<AuthService>();
            svc.AddSingleton<ILocalizationService, StubLocalizationService>();
            svc.AddSingleton<ICflDialogService, StubCflDialogService>();
            svc.AddSingleton<IShellInteractionService, StubShellInteractionService>();
            svc.AddSingleton<IModuleNavigationService, StubModuleNavigationService>();
            svc.AddSingleton<RiskAssessmentViewModel>();
            svc.AddTransient<RiskAssessmentsModuleViewModel>();
        });

        using var provider = services.BuildServiceProvider();

        var shared = provider.GetRequiredService<RiskAssessmentViewModel>();
        Assert.NotNull(shared);
        Assert.Same(shared, provider.GetRequiredService<RiskAssessmentViewModel>());

        var module = provider.GetRequiredService<RiskAssessmentsModuleViewModel>();
        Assert.NotNull(module);
        Assert.Same(shared, module.RiskAssessments);
    }

    private sealed class StubCflDialogService : ICflDialogService
    {
        public Task<CflResult?> ShowAsync(CflRequest request) => Task.FromResult<CflResult?>(null);
    }

    private sealed class StubShellInteractionService : IShellInteractionService
    {
        public void UpdateStatus(string message) { }

        public void UpdateInspector(InspectorContext context) { }
    }

    private sealed class StubModuleNavigationService : IModuleNavigationService
    {
        public ModuleDocumentViewModel OpenModule(string moduleKey, object? parameter = null)
            => throw new NotSupportedException();

        public void Activate(ModuleDocumentViewModel document) { }
    }

    private sealed class StubLocalizationService : ILocalizationService
    {
        public string CurrentLanguage { get; private set; } = "en";

        public event EventHandler? LanguageChanged;

        public string GetString(string key) => key;

        public void SetLanguage(string language)
        {
            CurrentLanguage = language;
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private sealed class StubRbacService : IRBACService
    {
        public Task AssertPermissionAsync(int userId, string permissionCode) => Task.CompletedTask;

        public Task<bool> HasPermissionAsync(int userId, string permissionCode) => Task.FromResult(true);

        public Task<List<string>> GetAllUserPermissionsAsync(int userId)
            => Task.FromResult(new List<string>());

        public Task GrantRoleAsync(int userId, int roleId, int grantedBy, DateTime? expiresAt = null, string reason = "")
            => Task.CompletedTask;

        public Task RevokeRoleAsync(int userId, int roleId, int revokedBy, string reason = "")
            => Task.CompletedTask;

        public Task<List<Role>> GetRolesForUserAsync(int userId)
            => Task.FromResult(new List<Role>());

        public Task<List<Role>> GetAvailableRolesForUserAsync(int userId)
            => Task.FromResult(new List<Role>());

        public Task GrantPermissionAsync(int userId, string permissionCode, int grantedBy, DateTime? expiresAt = null, string reason = "")
            => Task.CompletedTask;

        public Task RevokePermissionAsync(int userId, string permissionCode, int revokedBy, string reason = "")
            => Task.CompletedTask;

        public Task DelegatePermissionAsync(int fromUserId, int toUserId, string permissionCode, int grantedBy, DateTime expiresAt, string reason = "")
            => Task.CompletedTask;

        public Task RevokeDelegatedPermissionAsync(int delegatedPermissionId, int revokedBy, string reason = "")
            => Task.CompletedTask;

        public Task<List<Role>> GetAllRolesAsync() => Task.FromResult(new List<Role>());

        public Task<List<Permission>> GetAllPermissionsAsync() => Task.FromResult(new List<Permission>());

        public Task<List<Permission>> GetPermissionsForRoleAsync(int roleId) => Task.FromResult(new List<Permission>());

        public Task<List<Permission>> GetPermissionsNotInRoleAsync(int roleId) => Task.FromResult(new List<Permission>());

        public Task AddPermissionToRoleAsync(int roleId, int permissionId, int adminUserId, string reason = "")
            => Task.CompletedTask;

        public Task RemovePermissionFromRoleAsync(int roleId, int permissionId, int adminUserId, string reason = "")
            => Task.CompletedTask;

        public Task<int> CreateRoleAsync(Role role, int adminUserId) => Task.FromResult(0);

        public Task UpdateRoleAsync(Role role, int adminUserId) => Task.CompletedTask;

        public Task DeleteRoleAsync(int roleId, int adminUserId, string reason = "")
            => Task.CompletedTask;

        public Task<int> RequestPermissionAsync(int userId, string permissionCode, string reason) => Task.FromResult(0);

        public Task ApprovePermissionRequestAsync(int requestId, int approvedBy, string comment) => Task.CompletedTask;

        public Task DenyPermissionRequestAsync(int requestId, int deniedBy, string comment) => Task.CompletedTask;
    }

    private sealed class StubDeviationAuditService : IDeviationAuditService
    {
        public Task CreateAsync(DeviationAudit audit) => Task.CompletedTask;

        public Task UpdateAsync(DeviationAudit audit) => Task.CompletedTask;

        public Task DeleteAsync(int id) => Task.CompletedTask;

        public Task<DeviationAudit> GetByIdAsync(int id) => Task.FromResult(new DeviationAudit());

        public Task<IReadOnlyList<DeviationAudit>> GetByDeviationIdAsync(int deviationId)
            => Task.FromResult<IReadOnlyList<DeviationAudit>>(Array.Empty<DeviationAudit>());

        public Task<IReadOnlyList<DeviationAudit>> GetByUserIdAsync(int userId)
            => Task.FromResult<IReadOnlyList<DeviationAudit>>(Array.Empty<DeviationAudit>());

        public Task<IReadOnlyList<DeviationAudit>> GetByActionTypeAsync(DeviationActionType actionType)
            => Task.FromResult<IReadOnlyList<DeviationAudit>>(Array.Empty<DeviationAudit>());

        public Task<IReadOnlyList<DeviationAudit>> GetByDateRangeAsync(DateTime from, DateTime to)
            => Task.FromResult<IReadOnlyList<DeviationAudit>>(Array.Empty<DeviationAudit>());

        public bool ValidateIntegrity(DeviationAudit audit) => true;

        public Task<string> ExportAuditLogsAsync(int deviationId, string format = "pdf")
            => Task.FromResult(string.Empty);

        public Task<double> AnalyzeAnomalyAsync(int deviationId) => Task.FromResult(0d);
    }
}
