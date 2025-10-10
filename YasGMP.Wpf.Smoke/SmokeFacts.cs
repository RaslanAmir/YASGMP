using System;
using System.Reflection;
using Xunit;

namespace YasGMP.Wpf.Smoke;

/// <summary>
/// Conditional xUnit fact that only runs when both the YASGMP_SMOKE env var is enabled
/// and FlaUI.UIA3 is available on the machine. Otherwise, the test is skipped by default.
/// </summary>
public sealed class SmokeFactAttribute : FactAttribute
{
    public SmokeFactAttribute()
    {
        if (!IsSmokeEnabled())
        {
            Skip = "Smoke disabled. Set YASGMP_SMOKE=1 and ensure FlaUI.UIA3 is installed.";
            return;
        }

        if (!IsFlaUiAvailable())
        {
            Skip = "FlaUI.UIA3 not available on this environment.";
        }
    }

    private static bool IsSmokeEnabled()
    {
        var value = Environment.GetEnvironmentVariable("YASGMP_SMOKE");
        if (string.IsNullOrWhiteSpace(value)) return false;
        value = value.Trim().ToLowerInvariant();
        return value is "1" or "true" or "yes" or "y" or "on" or "enable" or "enabled";
    }

    private static bool IsFlaUiAvailable()
    {
        try
        {
            // Attempt to resolve FlaUI.UIA3 types
            _ = Type.GetType("FlaUI.UIA3.UIA3Automation, FlaUI.UIA3", throwOnError: true);
            _ = Type.GetType("FlaUI.Core.Application, FlaUI.Core", throwOnError: true);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

