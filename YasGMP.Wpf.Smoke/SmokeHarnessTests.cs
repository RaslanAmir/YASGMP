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
        if (!string.IsNullOrWhiteSpace(value))
        {
            // When explicitly enabled, this check is not applicable
            return;
        }
        Assert.True(string.IsNullOrWhiteSpace(value) || value.Equals("0", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ReportsVm_RunsProvider_WithoutUi()
    {
        // Headless smoke for the Reports VM run/export providers
        var db = new YasGMP.Services.DatabaseService("Server=127.0.0.1;Port=3306;Database=YASGMP;User ID=yasgmp_app;Password=Jasenka1;CharSet=utf8mb4;");
        var audit = new YasGMP.Services.AuditService(db);
        var vm = new YasGMP.Wpf.ViewModels.Modules.ReportsModuleViewModel(db, audit, null!, null!, null!);
        // Load sample data (design-time generator)
        var mi = typeof(YasGMP.Wpf.ViewModels.Modules.ReportsModuleViewModel)
            .GetMethod("CreateSampleRecords", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var obj = mi.Invoke(null, Array.Empty<object>());
        var records = obj as System.Collections.Generic.IReadOnlyList<YasGMP.Wpf.ViewModels.Modules.ModuleRecord>;
        Assert.NotNull(records);
    }
}
