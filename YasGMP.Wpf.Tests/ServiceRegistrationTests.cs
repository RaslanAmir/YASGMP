using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using YasGMP.AppCore.DependencyInjection;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Modules;
using YasGMP.Models;
using YasGMP.Models.Enums;

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
