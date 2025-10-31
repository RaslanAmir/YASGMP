using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Threading.Tasks;
using Fluent.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using YasGMP.AppCore.DependencyInjection;
using YasGMP.Common;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Services;
using YasGMP.ViewModels;
using YasGMP.Wpf.ViewModels;
using YasGMP.Wpf.ViewModels.Dialogs;
using YasGMP.Wpf.ViewModels.Modules;
using YasGMP.Wpf.Localization;

namespace YasGMP.Wpf
{
    /// <summary>
    /// Bootstrapper for the YasGMP WPF shell. Wires the generic host, DI and root window.
    /// </summary>
    public partial class App : Application
    {
        private IHost? _host;
        internal IHost? Host => _host;

        static App()
        {
            ConfigureRibbonLocalization();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Load localization resources (EN↔HR) before composing the shell.
            HookGlobalExceptionLogging();
            TryLoadLocalizationResources();
            ConfigureRibbonLocalization();

            // CLI switches: --smoke enables smoke mode; --smoke-strict enables strict smoke mode
            try
            {
                if (e?.Args != null)
                {
                    foreach (var arg in e.Args)
                    {
                        if (string.Equals(arg, "--smoke", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(arg, "/smoke", StringComparison.OrdinalIgnoreCase))
                        {
                            Environment.SetEnvironmentVariable("YASGMP_SMOKE", "1");
                        }
                        if (string.Equals(arg, "--smoke-strict", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(arg, "/smoke-strict", StringComparison.OrdinalIgnoreCase))
                        {
                            Environment.SetEnvironmentVariable("YASGMP_STRICT_SMOKE", "1");
                        }
                    }
                }
            }
            catch { /* ignore arg parsing issues */ }

            _host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
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

                    // Config switches: Smoke:Enabled=true, Smoke:Strict=true
                    try
                    {
                        var smokeEnabled = ctx.Configuration["Smoke:Enabled"];
                        if (!string.IsNullOrWhiteSpace(smokeEnabled) &&
                            (string.Equals(smokeEnabled, "1", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(smokeEnabled, "true", StringComparison.OrdinalIgnoreCase)))
                        {
                            var currentSmoke = Environment.GetEnvironmentVariable("YASGMP_SMOKE");
                            if (string.IsNullOrWhiteSpace(currentSmoke))
                            {
                                Environment.SetEnvironmentVariable("YASGMP_SMOKE", "1");
                            }
                        }
                        var strictCfg = ctx.Configuration["Smoke:Strict"];
                        if (!string.IsNullOrWhiteSpace(strictCfg) &&
                            (string.Equals(strictCfg, "1", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(strictCfg, "true", StringComparison.OrdinalIgnoreCase)))
                        {
                            var current = Environment.GetEnvironmentVariable("YASGMP_STRICT_SMOKE");
                            if (string.IsNullOrWhiteSpace(current))
                            {
                                Environment.SetEnvironmentVariable("YASGMP_STRICT_SMOKE", "1");
                            }
                        }
                    }
                    catch { /* tolerate config issues in constrained hosts */ }

                    // Attachments encryption options (align with MAUI setup)
                    var encryptionOptions = new AttachmentEncryptionOptions
                    {
                        KeyMaterial = Environment.GetEnvironmentVariable("YASGMP_ATTACHMENT_KEY")
                                      ?? ctx.Configuration["Attachments:Encryption:Key"],
                        KeyId = Environment.GetEnvironmentVariable("YASGMP_ATTACHMENT_KEY_ID")
                                 ?? ctx.Configuration["Attachments:Encryption:KeyId"]
                                 ?? "default"
                    };

                    var chunkEnv = Environment.GetEnvironmentVariable("YASGMP_ATTACHMENT_CHUNK_SIZE");
                    if (!string.IsNullOrWhiteSpace(chunkEnv) && int.TryParse(chunkEnv, out var chunkSizeEnv) && chunkSizeEnv > 0)
                    {
                        encryptionOptions.ChunkSize = chunkSizeEnv;
                    }
                    else if (int.TryParse(ctx.Configuration["Attachments:Encryption:ChunkSize"], out var chunkSizeCfg) && chunkSizeCfg > 0)
                    {
                        encryptionOptions.ChunkSize = chunkSizeCfg;
                    }

                    services.AddSingleton(encryptionOptions);

                    services.AddYasGmpCoreServices(core =>
                    {
                        core.UseConnectionString(connectionString);
                        core.UseDatabaseService<DatabaseService>((_, conn) => new DatabaseService(conn));

                        var svc = core.Services;
                        svc.AddSingleton<AuditService>();
                        // Ensure Validation audit sink is available for ValidationService
                        svc.AddTransient<IValidationAuditService, ValidationAuditService>();
                        svc.AddSingleton<ICalibrationAuditService, CalibrationAuditAdapter>();
                        svc.AddSingleton<ExportService>();
                        svc.AddSingleton<IUserSession, UserSession>();
                        svc.AddSingleton<IPlatformService, WpfPlatformService>();
                        svc.AddSingleton<WpfAuthContext>();
                        svc.AddSingleton<IAuthContext>(sp => sp.GetRequiredService<WpfAuthContext>());
                        svc.AddSingleton<UserService>();
                        svc.AddSingleton<IUserService>(sp => sp.GetRequiredService<UserService>());
                        svc.AddSingleton<AuthService>();
                        svc.AddSingleton<IUiDispatcher, WpfUiDispatcher>();
                        svc.AddSingleton<IDialogService, WpfDialogService>();
                        svc.AddSingleton<IFilePicker, WpfFilePicker>();
                        svc.AddSingleton<IAttachmentService, AttachmentService>();
                        svc.AddSingleton<IAttachmentWorkflowService, AttachmentWorkflowService>();
                        svc.AddSingleton<IElectronicSignatureDialogService, ElectronicSignatureDialogService>();
                        svc.AddSingleton<ICflDialogService, CflDialogService>();
                        svc.AddSingleton<IRBACService, RBACService>();
                        svc.AddSingleton<WorkOrderAuditService>();
                        svc.AddTransient<ICapaAuditService, CapaAuditService>();
                        svc.AddTransient<INotificationService, NotificationService>();
                        svc.AddTransient<WorkOrderService>();
                        svc.AddTransient<IWorkOrderCrudService, WorkOrderCrudServiceAdapter>();
                        svc.AddTransient<MachineService>();
                        svc.AddTransient<ComponentService>();
                        svc.AddTransient<CalibrationService>();
                        svc.AddTransient<PartService>();
                        svc.AddTransient<SupplierService>();
                        svc.AddTransient<ExternalServicerService>();
                        svc.AddTransient<ChangeControlService>();
                        svc.AddTransient<ValidationService>();
                        svc.AddTransient<IIncidentAuditService, IncidentAuditService>();
                        svc.AddTransient<IncidentService>();
                        svc.AddTransient<CAPAService>();
                        svc.AddTransient<IMachineCrudService, MachineCrudServiceAdapter>();
                        svc.AddTransient<IComponentCrudService, ComponentCrudServiceAdapter>();
                        svc.AddTransient<IPartCrudService, PartCrudServiceAdapter>();
                        svc.AddTransient<IWarehouseCrudService, WarehouseCrudServiceAdapter>();
                        svc.AddTransient<ICalibrationCrudService, CalibrationCrudServiceAdapter>();
                        svc.AddTransient<IIncidentCrudService, IncidentCrudServiceAdapter>();
                        svc.AddTransient<ICapaCrudService, CapaCrudServiceAdapter>();
                        svc.AddTransient<IChangeControlCrudService, ChangeControlCrudServiceAdapter>();
                        svc.AddTransient<IValidationCrudService, ValidationCrudServiceAdapter>();
                        svc.AddTransient<ISupplierAuditService, SupplierAuditService>();
                        svc.AddTransient<ISupplierCrudService, SupplierCrudServiceAdapter>();
                        svc.AddTransient<IExternalServicerCrudService, ExternalServicerCrudServiceAdapter>();
                        svc.AddTransient<IUserCrudService, UserCrudServiceAdapter>();
                        svc.AddTransient<IScheduledJobCrudService, ScheduledJobCrudServiceAdapter>();
                        svc.AddSingleton<ShellInteractionService>();
                        svc.AddSingleton<IModuleNavigationService>(sp => sp.GetRequiredService<ShellInteractionService>());
                        svc.AddSingleton<IShellInteractionService>(sp => sp.GetRequiredService<ShellInteractionService>());
                        svc.AddSingleton<ModulesPaneViewModel>();
                        svc.AddSingleton<InspectorPaneViewModel>();
                        svc.AddSingleton<ShellStatusBarViewModel>();
                        svc.AddSingleton<DebugSmokeTestService>();
                        // AI assistant (shared service + dialog VM)
                        svc.AddSingleton<YasGMP.AppCore.Services.Ai.OpenAiOptions>(sp =>
                        {
                            var cfg = sp.GetRequiredService<IConfiguration>();
                            var opts = new YasGMP.AppCore.Services.Ai.OpenAiOptions
                            {
                                ApiKey = cfg["Ai:OpenAI:ApiKey"],
                                Organization = cfg["Ai:OpenAI:Organization"],
                                Project = cfg["Ai:OpenAI:Project"],
                                BaseUrl = cfg["Ai:OpenAI:BaseUrl"],
                                ChatModel = cfg["Ai:OpenAI:Model"] ?? "gpt-4o-mini",
                                EmbeddingModel = cfg["Ai:OpenAI:EmbeddingModel"] ?? "text-embedding-3-small",
                                ModerationModel = cfg["Ai:OpenAI:ModerationModel"] ?? "omni-moderation-latest"
                            };
                            opts.ApplyEnvironmentOverrides();
                            return opts;
                        });
                        svc.AddHttpClient();
                        svc.AddSingleton<YasGMP.AppCore.Services.Ai.IAiAssistantService, YasGMP.AppCore.Services.Ai.OpenAiAssistantService>();
                        svc.AddTransient<YasGMP.Wpf.ViewModels.AiAssistantDialogViewModel>();
                        svc.AddSingleton<YasGMP.Services.ITextExtractor, YasGMP.Services.PdfTextExtractor>();
                        svc.AddSingleton<YasGMP.Services.AttachmentEmbeddingService>();
                        svc.AddTransient<YasGMP.Wpf.ViewModels.Modules.AiModuleViewModel>();
                        svc.AddTransient<DigitalSignatureViewModel>();
                        svc.AddTransient<ElectronicSignatureDialogViewModel>();
                        svc.AddTransient<AuditLogViewModel>(sp =>
                        {
                            var database = sp.GetRequiredService<DatabaseService>();
                            return new AuditLogViewModel(database);
                        });
                        svc.AddTransient<AuditDashboardViewModel>();
                        svc.AddTransient<DashboardModuleViewModel>();
                        svc.AddTransient<AssetsModuleViewModel>();
                        svc.AddTransient<ComponentsModuleViewModel>();
                        svc.AddTransient<WarehouseModuleViewModel>();
                        svc.AddTransient<WorkOrdersModuleViewModel>();
                        svc.AddTransient<CalibrationModuleViewModel>();
                        svc.AddTransient<PartsModuleViewModel>();
                        svc.AddTransient<SuppliersModuleViewModel>();
                        svc.AddTransient<ExternalServicersModuleViewModel>();
                        svc.AddTransient<CapaModuleViewModel>();
                        svc.AddTransient<IncidentsModuleViewModel>();
                        svc.AddTransient<ChangeControlModuleViewModel>();
                        svc.AddTransient<ValidationsModuleViewModel>();
                        svc.AddTransient<SchedulingModuleViewModel>();
                        svc.AddTransient<SecurityModuleViewModel>();
                        svc.AddTransient<AdminModuleViewModel>();
                        svc.AddTransient<ReportsModuleViewModel>();
                        svc.AddTransient<AuditModuleViewModel>();
                        svc.AddTransient<AuditLogDocumentViewModel>(sp => new AuditLogDocumentViewModel(
                            sp.GetRequiredService<AuditService>(),
                            sp.GetRequiredService<ExportService>(),
                            sp.GetRequiredService<AuditLogViewModel>(),
                            sp.GetRequiredService<ICflDialogService>(),
                            sp.GetRequiredService<IShellInteractionService>(),
                            sp.GetRequiredService<IModuleNavigationService>()));
                        svc.AddTransient<AuditDashboardDocumentViewModel>();
                        svc.AddTransient<ApiAuditModuleViewModel>();
                        svc.AddTransient<DiagnosticsModuleViewModel>();
                        svc.AddTransient(sp => new AttachmentsModuleViewModel(
                            sp.GetRequiredService<DatabaseService>(),
                            sp.GetRequiredService<IAttachmentService>(),
                            sp.GetRequiredService<IFilePicker>(),
                            sp.GetRequiredService<IElectronicSignatureDialogService>(),
                            sp.GetRequiredService<AuditService>(),
                            sp.GetRequiredService<ICflDialogService>(),
                            sp.GetRequiredService<IShellInteractionService>(),
                            sp.GetRequiredService<IModuleNavigationService>()));

                        svc.AddSingleton<ModuleRegistry>(sp =>
                        {
                            var registry = new ModuleRegistry(sp);
                            registry.Register<DashboardModuleViewModel>(DashboardModuleViewModel.ModuleKey,
                                L("Module_Dashboard_Title", "Dashboard"), L("Category_Cockpit", "Cockpit"), L("Module_Dashboard_Tooltip", "Operations overview and KPIs"));
                            registry.Register<AssetsModuleViewModel>(AssetsModuleViewModel.ModuleKey,
                                L("Module_Assets_Title", "Assets"), L("Category_Maintenance", "Maintenance"), L("Module_Assets_Tooltip", "Asset register and lifecycle"));
                            registry.Register<ComponentsModuleViewModel>(ComponentsModuleViewModel.ModuleKey,
                                L("Module_Components_Title", "Components"), L("Category_Maintenance", "Maintenance"), L("Module_Components_Tooltip", "Component hierarchy and lifecycle"));
                            registry.Register<WarehouseModuleViewModel>(WarehouseModuleViewModel.ModuleKey,
                                L("Module_Warehouse_Title", "Warehouse"), L("Category_Maintenance", "Maintenance"), L("Module_Warehouse_Tooltip", "Warehouse master data"));
                            registry.Register<WorkOrdersModuleViewModel>(WorkOrdersModuleViewModel.ModuleKey,
                                L("Module_WorkOrders_Title", "Work Orders"), L("Category_Maintenance", "Maintenance"), L("Module_WorkOrders_Tooltip", "Corrective and preventive jobs"));
                            registry.Register<CalibrationModuleViewModel>(CalibrationModuleViewModel.ModuleKey,
                                L("Module_Calibration_Title", "Calibration"), L("Category_Maintenance", "Maintenance"), L("Module_Calibration_Tooltip", "Calibration records"));
                            registry.Register<PartsModuleViewModel>(PartsModuleViewModel.ModuleKey,
                                L("Module_Parts_Title", "Parts"), L("Category_Maintenance", "Maintenance"), L("Module_Parts_Tooltip", "Parts and spare stock"));
                            registry.Register<SuppliersModuleViewModel>(SuppliersModuleViewModel.ModuleKey,
                                L("Module_Suppliers_Title", "Suppliers"), L("Category_SupplyChain", "Supply Chain"), L("Module_Suppliers_Tooltip", "Approved suppliers and contractors"));
                            registry.Register<ExternalServicersModuleViewModel>(ExternalServicersModuleViewModel.ModuleKey,
                                L("Module_ExternalServicers_Title", "External Servicers"), L("Category_SupplyChain", "Supply Chain"), L("Module_ExternalServicers_Tooltip", "Accredited laboratories and service partners"));
                            registry.Register<CapaModuleViewModel>(CapaModuleViewModel.ModuleKey,
                                L("Module_CAPA_Title", "CAPA"), L("Category_Quality", "Quality"), L("Module_CAPA_Tooltip", "Corrective actions and preventive plans"));
                            registry.Register<IncidentsModuleViewModel>(IncidentsModuleViewModel.ModuleKey,
                                L("Module_Incidents_Title", "Incidents"), L("Category_Quality", "Quality"), L("Module_Incidents_Tooltip", "Incident intake and investigations"));
                            registry.Register<ChangeControlModuleViewModel>(ChangeControlModuleViewModel.ModuleKey,
                                L("Module_ChangeControl_Title", "Change Control"), L("Category_Quality", "Quality"), L("Module_ChangeControl_Tooltip", "Change control workflow"));
                            registry.Register<ValidationsModuleViewModel>(ValidationsModuleViewModel.ModuleKey,
                                L("Module_Validations_Title", "Validations"), L("Category_Quality", "Quality"), L("Module_Validations_Tooltip", "IQ/OQ/PQ lifecycle and requalification"));
                            registry.Register<SchedulingModuleViewModel>(SchedulingModuleViewModel.ModuleKey,
                                L("Module_Scheduling_Title", "Scheduling"), L("Category_Planning", "Planning"), L("Module_Scheduling_Tooltip", "Automated job schedules"));
                            registry.Register<SecurityModuleViewModel>(SecurityModuleViewModel.ModuleKey,
                                L("Module_Security_Title", "Security"), L("Category_Administration", "Administration"), L("Module_Security_Tooltip", "Users and security roles"));
                            registry.Register<AdminModuleViewModel>(AdminModuleViewModel.ModuleKey,
                                L("Module_Administration_Title", "Administration"), L("Category_Administration", "Administration"), L("Module_Administration_Tooltip", "Global configuration settings"));
                            registry.Register<AuditLogDocumentViewModel>(AuditLogDocumentViewModel.ModuleKey,
                                L("Module_AuditTrail_Title", "Audit Trail"), L("Category_QualityCompliance", "Quality & Compliance"), L("Module_AuditTrail_Tooltip", "System event history"));
                            registry.Register<AuditDashboardDocumentViewModel>(AuditDashboardDocumentViewModel.ModuleKey,
                                L("Module_AuditDashboard_Title", "Audit Dashboard"), L("Category_QualityCompliance", "Quality & Compliance"), L("Module_AuditDashboard_Tooltip", "Real-time audit feed and exports"));
                            registry.Register<ApiAuditModuleViewModel>(ApiAuditModuleViewModel.ModuleKey,
                                L("Module_ApiAuditTrail_Title", "API Audit Trail"), L("Category_QualityCompliance", "Quality & Compliance"), L("Module_ApiAuditTrail_Tooltip", "API key activity history and forensic request payloads"));
                            registry.Register<DiagnosticsModuleViewModel>(DiagnosticsModuleViewModel.ModuleKey,
                                L("Module_Diagnostics_Title", "Diagnostics"), L("Category_Diagnostics", "Diagnostics"), L("Module_Diagnostics_Tooltip", "Telemetry snapshots and health checks"));
                            registry.Register<AttachmentsModuleViewModel>(AttachmentsModuleViewModel.ModuleKey,
                                L("Module_Attachments_Title", "Attachments"), L("Category_Documents", "Documents"), L("Module_Attachments_Tooltip", "File attachments and certificates"));
                            registry.Register<ReportsModuleViewModel>(ReportsModuleViewModel.ModuleKey,
                                L("Module_Reports_Title", "Reports"), L("Category_Reports", "Reports"), L("Module_Reports_Tooltip", "Operational and compliance reports"));
                            registry.Register<AiModuleViewModel>(AiModuleViewModel.ModuleKey,
                                L("Module_AI_Title", "AI Assistant"), L("Category_Tools", "Tools"), L("Module_AI_Tooltip", "ChatGPT-powered assistance and summaries"));
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

            try
            {
                var window = _host.Services.GetRequiredService<MainWindow>();
                window.Show();
            }
            catch (Exception ex)
            {
                var path = WriteCrashLog("MainWindow.Show", ex);
                try
                {
                    MessageBox.Show($"Startup error: {ex.Message}\nDetails: {path}", "YasGMP WPF", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch { }
                Shutdown(-1);
                return;
            }
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
            var conn = ConfigurationExtensionsCompat.GetConnectionString(configuration, "MySqlDb")
                       ?? configuration["ConnectionStrings:MySqlDb"]
                       ?? string.Empty;

            if (string.IsNullOrWhiteSpace(conn))
            {
                return "Server=127.0.0.1;Port=3306;Database=YASGMP;User ID=yasgmp_app;Password=Jasenka1;Character Set=utf8mb4;Connection Timeout=5;Default Command Timeout=30;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=50;";
            }

            return conn;
        }

        private static void ConfigureRibbonLocalization()
        {
            try
            {
                var culture = CultureInfo.CurrentUICulture;
                if (culture.TwoLetterISOLanguageName.Equals("hr", StringComparison.OrdinalIgnoreCase))
                {
                    var map = Fluent.RibbonLocalization.Current.LocalizationMap;
                    map["hr-HR"] = typeof(CroatianRibbonLocalization);
                    map["hr"] = typeof(CroatianRibbonLocalization);
                    Fluent.RibbonLocalization.Current.Localization = new CroatianRibbonLocalization();
                    Fluent.RibbonLocalization.Current.Culture = culture;
                }
            }
            catch
            {
                // Ignore localization failures and keep Fluent.Ribbon defaults.
            }
        }

        /// <summary>
        /// Attempts to load a culture-specific resource dictionary for UI strings.
        /// Falls back to English if a specific culture pack is not found.
        /// </summary>
        private static void TryLoadLocalizationResources()
        {
            try
            {
                var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName?.ToLowerInvariant();
                var app = Current;
                if (app is null)
                {
                    return;
                }

                string packRelative = culture switch
                {
                    "hr" => "Resources/Strings.hr.xaml",
                    _ => "Resources/Strings.en.xaml"
                };

                var uri = new Uri(packRelative, UriKind.Relative);
                var dict = (ResourceDictionary)Application.LoadComponent(uri);
                app.Resources.MergedDictionaries.Add(dict);
            }
            catch
            {
                // Non-fatal: localization resources are additive; fallback to default labels.
            }
        }

        /// <summary>
        /// Hooks global exception handlers to capture unhandled exceptions to a crash log under
        /// %LOCALAPPDATA%/YasGMP/logs. This helps diagnose early startup/XAML errors that do not
        /// surface nicely under Visual Studio's Just My Code settings.
        /// </summary>
        private static void HookGlobalExceptionLogging()
        {
            try
            {
                Application.Current.DispatcherUnhandledException += (_, args) =>
                {
                    try { WriteCrashLog("DispatcherUnhandledException", args.Exception); }
                    catch { /* ignore secondary failures */ }
                };

                AppDomain.CurrentDomain.UnhandledException += (_, args) =>
                {
                    try { WriteCrashLog("AppDomain.UnhandledException", args.ExceptionObject as Exception); }
                    catch { }
                };

                TaskScheduler.UnobservedTaskException += (_, args) =>
                {
                    try { WriteCrashLog("TaskScheduler.UnobservedTaskException", args.Exception); }
                    catch { }
                };
            }
            catch
            {
                // Swallow any unexpected issues while wiring logging.
            }
        }

        private static string WriteCrashLog(string source, Exception? ex)
        {
            try
            {
                var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var dir = Path.Combine(local, "YasGMP", "logs");
                Directory.CreateDirectory(dir);
                var file = Path.Combine(dir, $"wpf-crash-{DateTime.UtcNow:yyyyMMdd-HHmmss}.txt");
                using var sw = new StreamWriter(file, append: false);
                sw.WriteLine($"[{DateTime.UtcNow:O}] {source}");
                if (ex is not null)
                {
                    sw.WriteLine(ex.ToString());
                }
                else
                {
                    sw.WriteLine("(no exception instance available)");
                }
                sw.Flush();
                return file;
            }
            catch
            {
                // As a last resort, do nothing if logging fails.
                return string.Empty;
            }
        }

        /// <summary>
        /// Resolves a localized string from application resources with a safe fallback.
        /// </summary>
        private static string L(string key, string fallback)
        {
            try
            {
                var app = Current;
                if (app?.Resources.Contains(key) == true)
                {
                    var value = app.Resources[key] as string;
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value!;
                    }
                }
            }
            catch
            {
                // Ignore localization lookup failures and use fallback.
            }

            return fallback;
        }
    }
}
