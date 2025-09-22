using System;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using YasGMP.Services;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels;

namespace YasGMP.Wpf
{
    /// <summary>
    /// Bootstrapper for the YasGMP WPF shell. Wires the generic host, DI and root window.
    /// </summary>
    public partial class App : Application
    {
        private IHost? _host;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((_, cfg) =>
                {
                    cfg.SetBasePath(AppContext.BaseDirectory);
                    cfg.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                })
                .ConfigureServices((ctx, services) =>
                {
                    services.AddSingleton(ctx.Configuration);

                    services.AddSingleton<IUserSession, UserSession>();
                    services.AddSingleton<IMachineDataService, MockMachineDataService>();
                    services.AddSingleton<DockLayoutPersistenceService>();
                    services.AddSingleton<ShellLayoutController>();

                    services.AddSingleton<MainWindowViewModel>();
                    services.AddSingleton<MainWindow>();

                    services.AddSingleton(sp =>
                    {
                        var cfg = sp.GetRequiredService<IConfiguration>();
                        var conn = cfg.GetConnectionString("MySqlDb")
                                   ?? cfg["ConnectionStrings:MySqlDb"]
                                   ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(conn))
                        {
                            conn = "Server=127.0.0.1;Port=3306;Database=YASGMP;User ID=yasgmp_app;Password=Jasenka1;Character Set=utf8mb4;Connection Timeout=5;Default Command Timeout=30;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=50;";
                        }

                        var db = new DatabaseService(conn);
                        DatabaseService.GlobalConfiguration = cfg;
                        return db;
                    });
                })
                .Build();

            DatabaseService.GlobalConfiguration = _host.Services.GetRequiredService<IConfiguration>();

            _host.Start();

            var window = _host.Services.GetRequiredService<MainWindow>();
            window.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_host != null)
            {
                try
                {
                    _host.StopAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
                }
                catch
                {
                    // ignore shutdown failures
                }
                finally
                {
                    _host.Dispose();
                }
            }

            base.OnExit(e);
        }
    }
}
