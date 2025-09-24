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
using YasGMP.Wpf.ViewModels.Modules;

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
                        svc.AddSingleton<IPlatformService, WpfPlatformService>();
                        svc.AddSingleton<IAuthContext, WpfAuthContext>();
                        svc.AddSingleton<IUiDispatcher, WpfUiDispatcher>();
                        svc.AddSingleton<IDialogService, WpfDialogService>();
                        svc.AddSingleton<IFilePicker, WpfFilePicker>();
                        svc.AddSingleton<ICflDialogService, CflDialogService>();
                        svc.AddSingleton<WorkOrderAuditService>();
                        svc.AddTransient<WorkOrderService>();
                        svc.AddSingleton<ShellInteractionService>();
                        svc.AddSingleton<IModuleNavigationService>(sp => sp.GetRequiredService<ShellInteractionService>());
                        svc.AddSingleton<IShellInteractionService>(sp => sp.GetRequiredService<ShellInteractionService>());
                        svc.AddSingleton<ModulesPaneViewModel>();
                        svc.AddSingleton<InspectorPaneViewModel>();
                        svc.AddSingleton<ShellStatusBarViewModel>();
                        svc.AddSingleton<DebugSmokeTestService>();

                        svc.AddTransient<DashboardModuleViewModel>();
                        svc.AddTransient<AssetsModuleViewModel>();
                        svc.AddTransient<WarehouseModuleViewModel>();
                        svc.AddTransient<WorkOrdersModuleViewModel>();
                        svc.AddTransient<CalibrationModuleViewModel>();
                        svc.AddTransient<PartsModuleViewModel>();
                        svc.AddTransient<SuppliersModuleViewModel>();
                        svc.AddTransient<CapaModuleViewModel>();
                        svc.AddTransient<IncidentsModuleViewModel>();
                        svc.AddTransient<ChangeControlModuleViewModel>();
                        svc.AddTransient<SchedulingModuleViewModel>();
                        svc.AddTransient<SecurityModuleViewModel>();
                        svc.AddTransient<AdminModuleViewModel>();
                        svc.AddTransient<AuditModuleViewModel>();
                        svc.AddTransient<DiagnosticsModuleViewModel>();
                        svc.AddTransient<AttachmentsModuleViewModel>();

                        svc.AddSingleton<ModuleRegistry>(sp =>
                        {
                            var registry = new ModuleRegistry(sp);
                            registry.Register<DashboardModuleViewModel>(DashboardModuleViewModel.ModuleKey, "Dashboard", "Cockpit", "Operations overview and KPIs");
                            registry.Register<AssetsModuleViewModel>(AssetsModuleViewModel.ModuleKey, "Assets", "Maintenance", "Asset register and lifecycle");
                            registry.Register<WarehouseModuleViewModel>(WarehouseModuleViewModel.ModuleKey, "Warehouse", "Maintenance", "Warehouse master data");
                            registry.Register<WorkOrdersModuleViewModel>(WorkOrdersModuleViewModel.ModuleKey, "Work Orders", "Maintenance", "Corrective and preventive jobs");
                            registry.Register<CalibrationModuleViewModel>(CalibrationModuleViewModel.ModuleKey, "Calibration", "Maintenance", "Calibration records");
                            registry.Register<PartsModuleViewModel>(PartsModuleViewModel.ModuleKey, "Parts", "Maintenance", "Parts and spare stock");
                            registry.Register<SuppliersModuleViewModel>(SuppliersModuleViewModel.ModuleKey, "Suppliers", "Supply Chain", "Approved suppliers and contractors");
                            registry.Register<CapaModuleViewModel>(CapaModuleViewModel.ModuleKey, "CAPA", "Quality", "Corrective actions and preventive plans");
                            registry.Register<IncidentsModuleViewModel>(IncidentsModuleViewModel.ModuleKey, "Incidents", "Quality", "Incident intake and investigations");
                            registry.Register<ChangeControlModuleViewModel>(ChangeControlModuleViewModel.ModuleKey, "Change Control", "Quality", "Change control workflow");
                            registry.Register<SchedulingModuleViewModel>(SchedulingModuleViewModel.ModuleKey, "Scheduling", "Planning", "Automated job schedules");
                            registry.Register<SecurityModuleViewModel>(SecurityModuleViewModel.ModuleKey, "Security", "Administration", "Users and security roles");
                            registry.Register<AdminModuleViewModel>(AdminModuleViewModel.ModuleKey, "Administration", "Administration", "Global configuration settings");
                            registry.Register<AuditModuleViewModel>(AuditModuleViewModel.ModuleKey, "Audit Trail", "Compliance", "System event history");
                            registry.Register<DiagnosticsModuleViewModel>(DiagnosticsModuleViewModel.ModuleKey, "Diagnostics", "Diagnostics", "Telemetry snapshots and health checks");
                            registry.Register<AttachmentsModuleViewModel>(AttachmentsModuleViewModel.ModuleKey, "Attachments", "Documents", "File attachments and certificates");
                            return registry;
                        });
                        svc.AddSingleton<IModuleRegistry>(sp => sp.GetRequiredService<ModuleRegistry>());
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
