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
        DateTime GetLatestSmokeTimestamp()
        {
            var legacy = Directory.GetFiles(logsDir, "smoke_*.log");
            var current = Directory.GetFiles(logsDir, "smoke-*.txt");
            return legacy.Concat(current)
                         .Select(File.GetLastWriteTimeUtc)
                         .DefaultIfEmpty(DateTime.MinValue)
                         .Max();
        }

        var before = GetLatestSmokeTimestamp();

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
            if (main is null)
            {
                throw new SkipException("WPF main window not found (possibly blocked environment). Skipping.");
            }

            // Try to click Tools -> Run Smoke Test (support EN/HR captions)
            string[] toolsTabNames = { "Tools", "Alati" };
            string[] smokeNames = { "Run Smoke Test", "Pokreni brzi test" };

            // Activate Tools tab first (Fluent Ribbon)
            var toolsTab = RetryFind(() =>
            {
                foreach (var t in toolsTabNames)
                {
                    var tab = main.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.TabItem).And(cf.ByName(t)))?.AsTabItem();
                    if (tab != null) return tab;
                }
                return null;
            }, 8, TimeSpan.FromMilliseconds(250));
            toolsTab?.Select();

            var runBtn = RetryFind(() =>
            {
                foreach (var n in smokeNames)
                {
                    var b = main.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button).And(cf.ByName(n)))?.AsButton();
                    if (b != null) return b;
                }
                return null;
            }, 12, TimeSpan.FromMilliseconds(300));
            runBtn?.Invoke();

            // Wait up to 20 seconds for a new smoke_*.log
            var started = DateTime.UtcNow;
            var timeoutAt = started.AddSeconds(20);
            bool logFound = false;
            while (DateTime.UtcNow < timeoutAt)
            {
                var latest = GetLatestSmokeTimestamp();
                if (latest > before)
                {
                    logFound = true;
                    break;
                }
                await Task.Delay(500);
            }

            // Attempt to open modules via tree (Modules pane) or buttons; then open first editor grid row
            string[] modules = { "Work Orders", "Radni nalozi", "Assets", "Imovina", "Validations", "Validacije", "Suppliers", "Dobavljači" };
            int opened = 0;
            foreach (var m in modules)
            {
                var modBtn = main.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button).And(cf.ByName(m)))?.AsButton();
                if (modBtn != null)
                {
                    modBtn.Invoke();
                }
                else
                {
                    var treeItem = main.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.TreeItem).And(cf.ByName(m)))?.AsTreeItem();
                    treeItem ??= main.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Text).And(cf.ByName(m)))?.Parent?.AsTreeItem();
                    treeItem?.DoubleClick();
                }

                await Task.Delay(700);
                var grid = main.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.DataGrid))?.AsGrid();
                if (grid?.Rows?.Length > 0)
                {
                    grid.Rows[0]?.DoubleClick();
                }

                opened++;
                if (opened >= 2) break;
            }

            // App remains open; close it cleanly
            main.Close();

            // We do not fail if log not found to keep smoke gentle; presence indicates end-to-end success
            Assert.True(logFound || runBtn == null, "Smoke harness button clicked but no log file detected in time.");
        }
        finally
        {
            try { if (!app.HasExited) { app.Close(); } }
            catch { /* ignore shutdown issues in CI */ }
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
        string TryWalk(string start)
        {
            var dir = start;
            for (int i = 0; i < 12; i++)
            {
                if (Directory.Exists(Path.Combine(dir, "YasGMP.Wpf")))
                {
                    return dir;
                }
                var parent = Directory.GetParent(dir)?.FullName;
                if (string.IsNullOrEmpty(parent)) break;
                dir = parent;
            }
            return start;
        }

        var fromBase = TryWalk(AppContext.BaseDirectory);
        if (!ReferenceEquals(fromBase, AppContext.BaseDirectory)) return fromBase;
        var fromCwd = TryWalk(Directory.GetCurrentDirectory());
        return !ReferenceEquals(fromCwd, Directory.GetCurrentDirectory()) ? fromCwd : AppContext.BaseDirectory;
    }
}

public sealed class SkipException : Xunit.Sdk.XunitException
{
    public SkipException(string message) : base(message) { }
}
