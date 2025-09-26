using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using YasGMP.AppCore.DependencyInjection;
using YasGMP.Services;

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
}
