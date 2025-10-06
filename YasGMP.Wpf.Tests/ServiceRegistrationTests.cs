using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using YasGMP.AppCore.DependencyInjection;
using YasGMP.Services;
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
}

