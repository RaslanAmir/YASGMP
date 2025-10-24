using Microsoft.Maui;                    // MauiWinUIApplication
using Microsoft.Maui.Hosting;            // Microsoft.Maui.Hosting.MauiApp
using Microsoft.Maui.ApplicationModel;   // MainThread
using Microsoft.Maui.Controls;           // Controls Application
using ControlsApp = Microsoft.Maui.Controls.Application;
using System.Linq;

namespace YasGMP.WinUI
{
    /// <summary>
    /// WinUI entry point for the packaged Windows app. Bridges to <see cref="YasGMP.MauiProgram.CreateMauiApp"/>.
    /// Adds a Windows-specific unhandled exception handler to show a safe alert on the UI thread.
    /// </summary>
    public partial class App : MauiWinUIApplication
    {
        /// <summary>Initializes WinUI application resources and installs Windows exception hook.</summary>
        public App()
        {
            InitializeComponent();

            // Windows-specific unhandled exception -> show safe dialog without tearing down the process.
            this.UnhandledException += (s, e) =>
            {
                e.Handled = true;
                _ = MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        var page = ControlsApp.Current?.Windows?.FirstOrDefault()?.Page;
                        if (page != null)
                        {
                            var msg = e.Exception?.ToString() ?? "Unknown Windows exception";
                            await page.DisplayAlert("Windows error", msg, "OK");
                        }
                    }
                    catch { /* swallow */ }
                });
            };
        }

        /// <summary>Creates the MAUI app instance for Windows.</summary>
        protected override Microsoft.Maui.Hosting.MauiApp CreateMauiApp()
            => YasGMP.MauiProgram.CreateMauiApp();
    }
}
