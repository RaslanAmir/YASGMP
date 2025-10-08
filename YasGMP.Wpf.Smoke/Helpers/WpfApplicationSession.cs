using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using FlaUI.UIA3;

namespace YasGMP.Wpf.Smoke.Helpers;

internal sealed class WpfApplicationSession : IAsyncDisposable
{
    private static readonly object BuildSync = new();
    private static bool _isBuilt;

    private readonly Application _application;
    private readonly UIA3Automation _automation;

    private WpfApplicationSession(Application application, UIA3Automation automation, Window mainWindow)
    {
        _application = application;
        _automation = automation;
        MainWindow = mainWindow;
    }

    public Window MainWindow { get; }

    public UIA3Automation Automation => _automation;

    public static async Task<WpfApplicationSession> LaunchAsync()
    {
        EnsureBuilt();

        var exePath = RepositoryPaths.ResolveWpfExecutable();
        if (!File.Exists(exePath))
        {
            throw new FileNotFoundException($"WPF executable not found at '{exePath}'.", exePath);
        }

        var processStart = new ProcessStartInfo(exePath)
        {
            WorkingDirectory = Path.GetDirectoryName(exePath) ?? RepositoryPaths.Root,
            UseShellExecute = false,
        };

        var application = Application.Launch(processStart);
        application.WaitWhileMainHandleIsMissing(TimeSpan.FromSeconds(30));

        var automation = new UIA3Automation();
        var mainWindow = Retry.WhileNull(
                () => application.GetMainWindow(automation),
                timeout: TimeSpan.FromSeconds(30),
                throwOnTimeout: true)
            .Result;

        Wait.UntilInputIsProcessed(automation, TimeSpan.FromSeconds(2));

        return new WpfApplicationSession(application, automation, mainWindow);
    }

    public void WaitForIdle() => Wait.UntilInputIsProcessed(_automation, TimeSpan.FromSeconds(2));

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (!MainWindow.IsClosed)
            {
                MainWindow.Close();
            }
        }
        catch
        {
            // Ignore shutdown exceptions so cleanup always completes.
        }

        await Task.Delay(250).ConfigureAwait(false);
        _automation.Dispose();
        _application.Close();
    }

    private static void EnsureBuilt()
    {
        if (_isBuilt)
        {
            return;
        }

        lock (BuildSync)
        {
            if (_isBuilt)
            {
                return;
            }

            var root = RepositoryPaths.Root;
            var start = new ProcessStartInfo("dotnet", "build YasGMP.Wpf/YasGMP.Wpf.csproj -c Debug -f net9.0-windows")
            {
                WorkingDirectory = root,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            try
            {
                using var process = Process.Start(start);
                if (process is null)
                {
                    throw new InvalidOperationException("Failed to start dotnet build process.");
                }

                process.WaitForExit();
                _isBuilt = true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to build YasGMP.Wpf before launching automation.", ex);
            }
        }
    }
}
