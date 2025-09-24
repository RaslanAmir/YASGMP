using System;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using YasGMP.AppCore.DependencyInjection;
using YasGMP.Common;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
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

                    var connectionString = ResolveConnectionString(ctx.Configuration);
                    services.AddSingleton(new DatabaseOptions(connectionString));

                    services.AddYasGmpCoreServices(core =>
                    {
                        core.UseConnectionString(connectionString);
                        core.UseDatabaseService<DatabaseService>((_, conn) => new DatabaseService(conn));

                        var svc = core.Services;
                        svc.AddSingleton<IUserSession, UserSession>();
                        svc.AddSingleton<IMachineDataService, MockMachineDataService>();
                        svc.AddSingleton<IPlatformService, WpfPlatformService>();
                        svc.AddSingleton<IAuthContext, WpfAuthContext>();
                        svc.AddSingleton<DockLayoutPersistenceService>();
                        svc.AddSingleton<ShellLayoutController>();

                        svc.AddSingleton<MainWindowViewModel>();
                        svc.AddSingleton<MainWindow>();
                    });
                })
                .Build();

            ServiceLocator.Initialize(_host.Services);

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

        private static string ResolveConnectionString(IConfiguration configuration)
        {
            var conn = configuration.GetConnectionString("MySqlDb")
                       ?? configuration["ConnectionStrings:MySqlDb"]
                       ?? string.Empty;

            if (string.IsNullOrWhiteSpace(conn))
            {
                return "Server=127.0.0.1;Port=3306;Database=YASGMP;User ID=yasgmp_app;Password=Jasenka1;Character Set=utf8mb4;Connection Timeout=5;Default Command Timeout=30;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=50;";
            }

            return conn;
        }
    }
}
