using System;
using System.Threading.Tasks;
using Xunit;

namespace YasGMP.Wpf.Smoke;

public class SmokeHarnessTests
{
    [SmokeFact]
    public Task Placeholder_LaunchAndNavigate_Modules()
    {
        // Placeholder test; actual FlaUI automation to be added in a later increment.
        // We deliberately pass to validate the conditional test activation pipeline.
        return Task.CompletedTask;
    }

    [Fact]
    public void SmokeToggle_ReportsDisabledByDefault()
    {
        var value = Environment.GetEnvironmentVariable("YASGMP_SMOKE");
        Assert.True(string.IsNullOrWhiteSpace(value) || value.Equals("0", StringComparison.OrdinalIgnoreCase));
    }
}

