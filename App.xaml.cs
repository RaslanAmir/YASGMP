using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Extensions.Configuration;
using YasGMP.Models;
using YasGMP.Views;

namespace YasGMP
{
    /// <summary>
    /// Root MAUI <see cref="Application"/> for YasGMP:
    /// configuration bootstrap, session context, initial navigation, and global exception handling to AppData\logs.
    /// </summary>
    public partial class App : Application
    {
        public IConfiguration AppConfig { get; private set; } = default!;
        public User? LoggedUser { get; set; }
        public string SessionId { get; } = Guid.NewGuid().ToString("N");

        public App()
        {
            InitializeComponent();

            AppConfig = LoadConfiguration();
            MainPage = new NavigationPage(new LoginPage());

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

#if ANDROID
            Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += OnAndroidUnhandled;
#endif

            _ = WriteAppLogAsync("info", "AppStart", $"Session {SessionId} started");
        }

        private static IConfiguration LoadConfiguration()
        {
            var builder = new ConfigurationBuilder();
            try
            {
                var appData = FileSystem.AppDataDirectory;
                var path = Path.Combine(appData, "appsettings.json");
                if (File.Exists(path))
                    builder.AddJsonFile(path, optional: true, reloadOnChange: true);

                var exePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
                if (File.Exists(exePath))
                    builder.AddJsonFile(exePath, optional: true, reloadOnChange: true);
            }
            catch { /* never throw from config bootstrap */ }
            return builder.Build();
        }

        #region === Global Exception Handling ===

        private async void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception ?? new Exception("Unknown fatal exception");
            await WriteAppLogAsync("error", "UnhandledException", ex.Message, ex);

            // Always marshal dialog to UI thread; never "await Application.Current?.MainPage?...".
            _ = MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    var page = Application.Current?.MainPage;
                    if (page != null) await page.DisplayAlert("Unexpected error", ex.Message, "OK");
                }
                catch { /* swallow */ }
            });
        }

        private async void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            try { e.SetObserved(); } catch { }
            await WriteAppLogAsync("error", "UnobservedTaskException", e.Exception?.Message ?? "Unknown", e.Exception);

            _ = MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    var page = Application.Current?.MainPage;
                    if (page != null) await page.DisplayAlert("Background task error", e.Exception?.Message ?? "Unknown", "OK");
                }
                catch { /* swallow */ }
            });
        }

#if ANDROID
        private async void OnAndroidUnhandled(object? sender, Android.Runtime.RaiseThrowableEventArgs e)
        {
            await WriteAppLogAsync("fatal", "AndroidUnhandledException", e.Exception.Message, e.Exception);
            e.Handled = true;

            _ = MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    var page = Application.Current?.MainPage;
                    if (page != null) await page.DisplayAlert("Android error", e.Exception.Message, "OK");
                }
                catch { /* swallow */ }
            });
        }
#endif

        #endregion

        #region === JSONL App Log (AppData\logs) ===

        private static string GetLogFilePath()
        {
            var dir = Path.Combine(FileSystem.AppDataDirectory, "logs");
            Directory.CreateDirectory(dir);
            var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
            return Path.Combine(dir, $"{date}_app.log");
        }

        public Task WriteAppLogAsync(string level, string eventName, string message, Exception? ex = null)
        {
            try
            {
                var payload = new
                {
                    ts = DateTime.UtcNow,
                    level,
                    evt = eventName,
                    msg = message,
                    session = SessionId,
                    userId = LoggedUser?.Id,
                    user = LoggedUser?.Username,
                    exception = ex?.ToString()
                };

                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                File.AppendAllText(GetLogFilePath(), json + Environment.NewLine);
            }
            catch { /* swallow logging errors */ }
            return Task.CompletedTask;
        }

        #endregion
    }
}

