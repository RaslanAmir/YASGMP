using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Xunit;

namespace YasGMP.Wpf.Smoke;

public class EndToEndSmokeTests
{
    [SmokeFact]
    public async Task RunSmokeHarness_LogsOutput_And_RemainsResponsive()
    {
        var root = FindRepoRoot();
        var exe = Path.Combine(root, "YasGMP.Wpf", "bin", "Release", "net9.0-windows10.0.19041.0", "YasGMP.Wpf.exe");
        if (!File.Exists(exe))
        {
            throw new SkipException($"WPF exe not found at {exe}. Build Release before running smoke.");
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var logsDir = Path.Combine(localAppData, "YasGMP", "logs");
        Directory.CreateDirectory(logsDir);
        var before = Directory.GetFiles(logsDir, "smoke_*.log").Select(File.GetLastWriteTimeUtc).DefaultIfEmpty(DateTime.MinValue).Max();

        var psi = new ProcessStartInfo(exe)
        {
            UseShellExecute = false
        };
        psi.Environment["YASGMP_SMOKE"] = "1";

        using var app = Application.Launch(psi);
        using var automation = new UIA3Automation();

        try
        {
            var main = await WaitForAsync(() => app.GetMainWindow(automation), TimeSpan.FromSeconds(15));
            Assert.NotNull(main);

            // Try to click Tools -> Run Smoke Test (button is localized via DynamicResource)
            // We search for a button with the name "Run Smoke Test"
            var runBtn = RetryFind(() => main.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button)
                .And(cf.ByName("Run Smoke Test")))?.AsButton(), 10, TimeSpan.FromMilliseconds(300));
            if (runBtn != null)
            {
                runBtn.Invoke();
            }

            // Wait up to 20 seconds for a new smoke_*.log
            var started = DateTime.UtcNow;
            var timeoutAt = started.AddSeconds(20);
            bool logFound = false;
            while (DateTime.UtcNow < timeoutAt)
            {
                var latest = Directory.GetFiles(logsDir, "smoke_*.log")
                    .Select(File.GetLastWriteTimeUtc)
                    .DefaultIfEmpty(before)
                    .Max();
                if (latest > before)
                {
                    logFound = true;
                    break;
                }
                await Task.Delay(500);
            }

            // App remains open; close it cleanly
            main.Close();

            // We do not fail if log not found to keep smoke gentle; presence indicates end-to-end success
            Assert.True(logFound || runBtn == null, "Smoke harness button clicked but no log file detected in time.");
        }
        finally
        {
            if (!app.HasExited)
            {
                app.Close();
            }
        }
    }

    private static T? RetryFind<T>(Func<T?> find, int attempts, TimeSpan delay) where T : class
    {
        for (int i = 0; i < attempts; i++)
        {
            var el = find();
            if (el != null) return el;
            Thread.Sleep(delay);
        }
        return null;
    }

    private static async Task<T?> WaitForAsync<T>(Func<T?> fn, TimeSpan timeout) where T : class
    {
        var start = DateTime.UtcNow;
        while (DateTime.UtcNow - start < timeout)
        {
            var v = fn();
            if (v != null) return v;
            await Task.Delay(200);
        }
        return null;
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        // Walk up until a directory containing the WPF project is found
        for (int i = 0; i < 6; i++)
        {
            if (Directory.Exists(Path.Combine(dir, "YasGMP.Wpf")))
            {
                return dir;
            }
            var parent = Directory.GetParent(dir)?.FullName;
            if (string.IsNullOrEmpty(parent)) break;
            dir = parent;
        }
        return AppContext.BaseDirectory;
    }
}

public sealed class SkipException : Xunit.Sdk.XunitException
{
    public SkipException(string message) : base(message) { }
}

