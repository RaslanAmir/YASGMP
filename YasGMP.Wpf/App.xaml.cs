using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MySqlConnector;
using YasGMP.AppCore.DependencyInjection;
using YasGMP.Common;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Services.Ui;
using YasGMP.Diagnostics;
using YasGMP.Diagnostics.LogSinks;
using YasGMP.Services.Logging;
using YasGMP.ViewModels;
using YasGMP.Wpf.Configuration;
using YasGMP.Wpf.Runtime;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels;
using YasGMP.Wpf.ViewModels.Dialogs;
using YasGMP.Wpf.ViewModels.Modules;
using CoreAssetViewModel = YasGMP.ViewModels.AssetViewModel;
using DocumentControlViewModel = YasGMP.ViewModels.DocumentControlViewModel;
using WpfAssetViewModel = YasGMP.Wpf.ViewModels.AssetViewModel;

namespace YasGMP.Wpf
{
    /// <summary>
    /// Bootstrapper for the YasGMP WPF shell. Wires the generic host, DI and root window.
    /// </summary>
    public partial class App : Application
    {
        private IHost? _host;
        private DiagnosticsFeedService? _diagnosticsFeed;

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
                    services.AddSingleton(new DiagnosticContext(ctx.Configuration));
                    services.AddSingleton<IEnumerable<ILogSink>>(sp =>
                        CreateDiagnosticsSinks(ctx.Configuration, sp.GetRequiredService<DiagnosticContext>()).ToList());
                    services.AddSingleton<ILogWriter>(sp => new LogWriter(
                        DiagnosticsConstants.QueueCapacity,
                        DiagnosticsConstants.QueueDrainBatch,
                        DiagnosticsConstants.QueueDrainIntervalMs,
                        sp.GetRequiredService<IEnumerable<ILogSink>>()));
                    services.AddSingleton<ITrace>(sp => new TraceManager(
                        sp.GetRequiredService<DiagnosticContext>(),
                        sp.GetRequiredService<ILogWriter>()));
                    services.AddSingleton<IProfiler>(sp => new Profiler(sp.GetRequiredService<ITrace>()));
                    services.AddSingleton(sp =>
                    {
                        var crash = new CrashHandler(sp.GetRequiredService<ITrace>(), sp.GetRequiredService<DiagnosticContext>());
                        crash.RegisterGlobal();
                        return crash;
                    });
                    services.AddSingleton(sp => new DiagnosticsHub(
                        sp.GetRequiredService<DiagnosticContext>(),
                        sp.GetRequiredService<IEnumerable<ILogSink>>()));

                    var connectionString = ResolveConnectionString(ctx.Configuration);
                    var databaseOptions = DatabaseOptions.FromConnectionString(connectionString);

                    services.AddYasGmpCoreServices(core =>
                    {
                        core.UseConnectionString(connectionString);
                        core.UseDatabaseService<DatabaseService>((sp, conn) => new DatabaseService(conn), (sp, db, _) =>
                        {
                            var ctx = sp.GetRequiredService<DiagnosticContext>();
                            var trace = sp.GetRequiredService<ITrace>();
                            var configuration = sp.GetRequiredService<IConfiguration>();

                            db.SetDiagnostics(ctx, trace);

                            DatabaseService.GlobalDiagnosticContext = ctx;
                            DatabaseService.GlobalTrace = trace;
                            DatabaseService.GlobalConfiguration = configuration;

                            DatabaseTestHookBootstrapper.Configure(db, sp);
                        });

                        var svc = core.Services;
                        svc.AddSingleton(databaseOptions);
                        svc.AddSingleton(TimeProvider.System);
                        svc.AddSingleton<AuditService>();
                        svc.AddSingleton<ExportService>();
                        svc.AddSingleton<CodeGeneratorService>();
                        svc.AddSingleton<ICodeGeneratorService, CodeGeneratorServiceAdapter>();
                        svc.AddSingleton<QRCodeService>();
                        svc.AddSingleton<IQRCodeService, QRCodeServiceAdapter>();
                        svc.AddSingleton<IUserSession, UserSession>();
                        svc.AddSingleton<FileLogService>(sp =>
                        {
                            var session = sp.GetRequiredService<IUserSession>();
                            return new FileLogService(() => session.UserId, sessionId: session.SessionId);
                        });
                        svc.AddSingleton<DiagnosticsFeedService>();
                        svc.AddSingleton<IPlatformService, WpfPlatformService>();
                        svc.AddSingleton<WpfAuthContext>();
                        svc.AddSingleton<IAuthContext>(sp => sp.GetRequiredService<WpfAuthContext>());
                        svc.AddSingleton<IUiDispatcher, WpfUiDispatcher>();
                        svc.AddSingleton<IDialogService>(sp =>
                            new WpfDialogService(() => sp.GetRequiredService<UserEditDialogViewModel>()));
                        svc.AddSingleton<IAuthenticationDialogService, AuthenticationDialogService>();
                        svc.AddSingleton<ILocalizationService, LocalizationService>();
                        svc.AddSingleton<IFilePicker, WpfFilePicker>();
                        svc.AddSingleton<IAttachmentService, AttachmentService>();
                        svc.AddSingleton<IAttachmentWorkflowService, AttachmentWorkflowService>();
                        svc.AddSingleton<IElectronicSignatureDialogService, ElectronicSignatureDialogService>();
                        svc.AddSingleton<ICalibrationCertificateDialogService, CalibrationCertificateDialogService>();
                        svc.AddSingleton<ICflDialogService, CflDialogService>();
                        // Register IRBACService before UserService (dependency)
                        svc.AddSingleton<IRBACService, RBACService>();
                        // Register UserService before AuthService (dependency)
                        svc.AddSingleton<UserService>();
                        svc.AddSingleton<IUserService>(sp => sp.GetRequiredService<UserService>());
                        svc.AddSingleton<AuthService>();
                        svc.AddSingleton<IAuthenticator, AuthServiceAuthenticator>();
                        svc.AddTransient<ICalibrationAuditService, CalibrationAuditServiceAdapter>();
                        svc.AddTransient<IPpmAuditService, PpmAuditServiceAdapter>();
                        svc.AddTransient<PreventiveMaintenanceService>();
                        svc.AddTransient<PreventiveMaintenancePlanService>();
                        svc.AddTransient<IPreventiveMaintenanceService, PreventiveMaintenanceServiceAdapter>();
                        svc.AddTransient<IPreventiveMaintenancePlanService, PreventiveMaintenancePlanServiceAdapter>();
                        svc.AddSingleton<WorkOrderAuditService>();
                        svc.AddSingleton<BackgroundScheduler>();
                        svc.AddSingleton(sp => new Lazy<BackgroundScheduler>(() => sp.GetRequiredService<BackgroundScheduler>()));
                        svc.AddSingleton<ISignalRClientService, SignalRClientService>();
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
                        svc.AddTransient<DeviationService>();
                        svc.AddTransient<IMachineCrudService, MachineCrudServiceAdapter>();
                        svc.AddTransient<IComponentCrudService, ComponentCrudServiceAdapter>();
                        svc.AddTransient<IPartCrudService, PartCrudServiceAdapter>();
                        svc.AddTransient<IInventoryTransactionService, InventoryTransactionServiceAdapter>();
                        svc.AddTransient<IWarehouseCrudService, WarehouseCrudServiceAdapter>();
                        svc.AddTransient<ICalibrationCrudService, CalibrationCrudServiceAdapter>();
                        svc.AddTransient<IIncidentCrudService, IncidentCrudServiceAdapter>();
                        svc.AddTransient<IDeviationCrudService, DeviationCrudServiceAdapter>();
                        svc.AddTransient<ICapaCrudService, CapaCrudServiceAdapter>();
                        svc.AddTransient<IChangeControlCrudService, ChangeControlCrudServiceAdapter>();
                        svc.AddTransient<IValidationCrudService, ValidationCrudServiceAdapter>();
                        svc.AddTransient<ISupplierAuditService, SupplierAuditService>();
                        svc.AddTransient<ISupplierCrudService, SupplierCrudServiceAdapter>();
                        svc.AddTransient<IExternalServicerCrudService, ExternalServicerCrudServiceAdapter>();
                        svc.AddTransient<IUserCrudService, UserCrudServiceAdapter>();
                        svc.AddSingleton<ISecurityImpersonationWorkflowService, SecurityImpersonationWorkflowService>();
                        svc.AddTransient<IScheduledJobCrudService, ScheduledJobCrudServiceAdapter>();
                        svc.AddSingleton<ShellInteractionService>();
                        svc.AddSingleton<IModuleNavigationService>(sp => sp.GetRequiredService<ShellInteractionService>());
                        svc.AddSingleton<IShellInteractionService>(sp => sp.GetRequiredService<ShellInteractionService>());
                        svc.AddSingleton<INotificationPreferenceService, NotificationPreferenceService>();
                        svc.AddSingleton<IShellAlertService, AlertService>();
                        svc.AddSingleton<IAlertService>(sp => sp.GetRequiredService<IShellAlertService>());
                        svc.AddTransient<IDocumentControlService>(sp =>
                            ActivatorUtilities.CreateInstance<DocumentControlServiceAdapter>(
                                sp,
                                sp.GetRequiredService<DatabaseService>(),
                                sp.GetRequiredService<IAuthContext>(),
                                sp.GetRequiredService<IAttachmentWorkflowService>(),
                                sp.GetRequiredService<IAttachmentService>(),
                                sp.GetRequiredService<IElectronicSignatureDialogService>()));
                        svc.AddTransient<ITrainingRecordService>(sp =>
                            ActivatorUtilities.CreateInstance<TrainingRecordServiceAdapter>(
                                sp,
                                sp.GetRequiredService<DatabaseService>(),
                                sp.GetRequiredService<IAuthContext>(),
                                sp.GetRequiredService<IAttachmentWorkflowService>(),
                                sp.GetRequiredService<IAttachmentService>(),
                                sp.GetRequiredService<IElectronicSignatureDialogService>()));
                        svc.AddTransient<ISopGovernanceService>(sp =>
                            ActivatorUtilities.CreateInstance<SopGovernanceServiceAdapter>(
                                sp,
                                sp.GetRequiredService<DatabaseService>(),
                                sp.GetRequiredService<IAuthContext>(),
                                sp.GetRequiredService<IAttachmentWorkflowService>(),
                                sp.GetRequiredService<IAttachmentService>(),
                                sp.GetRequiredService<IElectronicSignatureDialogService>()));
                        svc.AddSingleton<CoreAssetViewModel>();
                        svc.AddSingleton<WpfAssetViewModel>(sp =>
                        {
                            var machineService = sp.GetRequiredService<IMachineCrudService>();
                            var sharedAsset = sp.GetRequiredService<CoreAssetViewModel>();
                            return new WpfAssetViewModel(machineService, sharedAsset);
                        });
                        svc.AddTransient<TrainingRecordViewModel>();
                        svc.AddTransient<SopViewModel>();
                        svc.AddSingleton<RiskAssessmentViewModel>();
                        svc.AddSingleton<QualificationViewModel>();
                        svc.AddSingleton<ModulesPaneViewModel>();
                        svc.AddSingleton<NotificationsPaneViewModel>();
                        svc.AddSingleton<InspectorPaneViewModel>();
                        svc.AddSingleton<ShellStatusBarViewModel>();
                        svc.AddSingleton<DebugSmokeTestService>();
                        svc.AddTransient<DocumentControlViewModel>();
                        svc.AddTransient<LoginViewModel>();
                        svc.AddTransient<ReauthenticationDialogViewModel>();
                        svc.AddTransient<UserEditDialogViewModel>();
                        svc.AddTransient<DigitalSignatureViewModel>();
                        svc.AddTransient<ElectronicSignatureDialogViewModel>();
                        svc.AddTransient<AuditLogViewModel>(sp =>
                        {
                            var database = sp.GetRequiredService<DatabaseService>();
                            return new AuditLogViewModel(database);
                        });
                        svc.AddTransient<AuditDashboardViewModel>();
                        svc.AddTransient<IReportAnalyticsViewModel, ReportViewModel>();
                        svc.AddTransient<INotificationAnalyticsViewModel, NotificationViewModel>();
                        svc.AddTransient<DashboardModuleViewModel>();
                        svc.AddTransient<AssetsModuleViewModel>(sp =>
                        {
                            var adapter = sp.GetRequiredService<WpfAssetViewModel>();
                            var shared = sp.GetRequiredService<CoreAssetViewModel>();
                            return ActivatorUtilities.CreateInstance<AssetsModuleViewModel>(sp, adapter, shared);
                        });
                        svc.AddTransient<ComponentsModuleViewModel>();
                        svc.AddTransient<WarehouseModuleViewModel>();
                        svc.AddTransient<WorkOrdersModuleViewModel>();
                        svc.AddTransient<CalibrationModuleViewModel>();
                        svc.AddTransient<PreventiveMaintenanceModuleViewModel>();
                        svc.AddTransient<PartsModuleViewModel>();
                        svc.AddTransient<SuppliersModuleViewModel>();
                        svc.AddTransient<ExternalServicersModuleViewModel>();
                        svc.AddTransient<CapaModuleViewModel>();
                        svc.AddTransient<DeviationModuleViewModel>();
                        svc.AddTransient<IncidentsModuleViewModel>();
                        svc.AddTransient<ChangeControlModuleViewModel>();
                        svc.AddTransient<DocumentControlModuleViewModel>(sp =>
                            ActivatorUtilities.CreateInstance<DocumentControlModuleViewModel>(
                                sp,
                                sp.GetRequiredService<DocumentControlViewModel>(),
                                sp.GetRequiredService<ILocalizationService>(),
                                sp.GetRequiredService<ICflDialogService>(),
                                sp.GetRequiredService<IShellInteractionService>(),
                                sp.GetRequiredService<IModuleNavigationService>(),
                                sp.GetRequiredService<IDocumentControlService>()));
                        svc.AddTransient<ValidationsModuleViewModel>();
                        svc.AddTransient<SchedulingModuleViewModel>();
                        svc.AddTransient<SecurityModuleViewModel>();
                        svc.AddTransient<AdminModuleViewModel>();
                        svc.AddTransient<AuditModuleViewModel>();
                        svc.AddTransient<AuditLogDocumentViewModel>(sp => new AuditLogDocumentViewModel(
                            sp.GetRequiredService<AuditService>(),
                            sp.GetRequiredService<ExportService>(),
                            sp.GetRequiredService<AuditLogViewModel>(),
                            sp.GetRequiredService<ICflDialogService>(),
                            sp.GetRequiredService<IShellInteractionService>(),
                            sp.GetRequiredService<IModuleNavigationService>()));
                        svc.AddTransient<RollbackPreviewDocumentViewModel>();
                        svc.AddTransient<AuditDashboardDocumentViewModel>();
                        svc.AddTransient<ReportsDocumentViewModel>();
                        svc.AddTransient<ApiAuditModuleViewModel>();
                        svc.AddTransient<TrainingRecordsModuleViewModel>();
                        svc.AddTransient<SopGovernanceModuleViewModel>();
                        svc.AddTransient<RiskAssessmentsModuleViewModel>();
                        svc.AddTransient<QualificationsModuleViewModel>();
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
                            var localization = sp.GetRequiredService<ILocalizationService>();
                            registry.Register<DashboardModuleViewModel>(DashboardModuleViewModel.ModuleKey, "Dashboard", "Cockpit", "Operations overview and KPIs");
                            registry.Register<AssetsModuleViewModel>(AssetsModuleViewModel.ModuleKey, "Assets", "Maintenance", "Asset register and lifecycle");
                            registry.Register<ComponentsModuleViewModel>(ComponentsModuleViewModel.ModuleKey, "Components", "Maintenance", "Component hierarchy and lifecycle");
                            registry.Register<WarehouseModuleViewModel>(WarehouseModuleViewModel.ModuleKey, "Warehouse", "Maintenance", "Warehouse master data");
                            registry.Register<WorkOrdersModuleViewModel>(WorkOrdersModuleViewModel.ModuleKey, "Work Orders", "Maintenance", "Corrective and preventive jobs");
                            registry.Register<PreventiveMaintenanceModuleViewModel>(PreventiveMaintenanceModuleViewModel.ModuleKey, "Preventive Maintenance", "Maintenance", "Preventive plans calendar and lifecycle");
                            registry.Register<CalibrationModuleViewModel>(CalibrationModuleViewModel.ModuleKey, "Calibration", "Maintenance", "Calibration records");
                            registry.Register<PartsModuleViewModel>(PartsModuleViewModel.ModuleKey, "Parts", "Maintenance", "Parts and spare stock");
                            registry.Register<SuppliersModuleViewModel>(SuppliersModuleViewModel.ModuleKey, "Suppliers", "Supply Chain", "Approved suppliers and contractors");
                            registry.Register<ExternalServicersModuleViewModel>(ExternalServicersModuleViewModel.ModuleKey, "External Servicers", "Supply Chain", "Accredited laboratories and service partners");
                            registry.Register<CapaModuleViewModel>(CapaModuleViewModel.ModuleKey, "CAPA", "Quality", "Corrective actions and preventive plans");
                            registry.Register<DeviationModuleViewModel>(DeviationModuleViewModel.ModuleKey, "Deviations", "Quality", "Deviation intake, investigation, and CAPA linkage");
                            registry.Register<IncidentsModuleViewModel>(IncidentsModuleViewModel.ModuleKey, "Incidents", "Quality", "Incident intake and investigations");
                            registry.Register<ChangeControlModuleViewModel>(ChangeControlModuleViewModel.ModuleKey, "Change Control", "Quality", "Change control workflow");
                            registry.Register<DocumentControlModuleViewModel>(
                                DocumentControlModuleViewModel.ModuleKey,
                                localization.GetString("Module.Title.DocumentControl"),
                                "Quality",
                                localization.GetString("Module.Description.DocumentControl"));
                            registry.Register<TrainingRecordsModuleViewModel>(
                                TrainingRecordsModuleViewModel.ModuleKey,
                                localization.GetString("Module.Title.TrainingRecords"),
                                "Quality",
                                localization.GetString("Module.Description.TrainingRecords"));
                            registry.Register<SopGovernanceModuleViewModel>(
                                SopGovernanceModuleViewModel.ModuleKey,
                                localization.GetString("Module.Title.SopGovernance"),
                                "Quality",
                                localization.GetString("Module.Description.SopGovernance"));
                            registry.Register<RiskAssessmentsModuleViewModel>(
                                RiskAssessmentsModuleViewModel.ModuleKey,
                                localization.GetString("Module.Title.RiskAssessments"),
                                "Quality",
                                localization.GetString("Module.Description.RiskAssessments"));
                            registry.Register<QualificationsModuleViewModel>(
                                QualificationsModuleViewModel.ModuleKey,
                                localization.GetString("Module.Title.Qualifications"),
                                "Quality",
                                localization.GetString("Module.Description.Qualifications"));
                            registry.Register<ValidationsModuleViewModel>(ValidationsModuleViewModel.ModuleKey, "Validations", "Quality", "IQ/OQ/PQ lifecycle and requalification");
                            registry.Register<SchedulingModuleViewModel>(SchedulingModuleViewModel.ModuleKey, "Scheduling", "Planning", "Automated job schedules");
                            registry.Register<SecurityModuleViewModel>(SecurityModuleViewModel.ModuleKey, "Security", "Administration", "Users and security roles");
                            registry.Register<AdminModuleViewModel>(AdminModuleViewModel.ModuleKey, "Administration", "Administration", "Global configuration settings");
                            registry.Register<AuditLogDocumentViewModel>(AuditLogDocumentViewModel.ModuleKey, "Audit Trail", "Quality & Compliance", "System event history");
                            registry.Register<AuditDashboardDocumentViewModel>(AuditDashboardDocumentViewModel.ModuleKey, "Audit Dashboard", "Quality & Compliance", "Real-time audit feed and exports");
                            registry.Register<ReportsDocumentViewModel>(ReportsDocumentViewModel.ModuleKey, "Reports", "Quality & Compliance", "Analytics reports and exports");
                            registry.Register<ApiAuditModuleViewModel>(ApiAuditModuleViewModel.ModuleKey, "API Audit Trail", "Quality & Compliance", "API key activity history and forensic request payloads");
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

            _ = _host.Services.GetRequiredService<CrashHandler>();
            _diagnosticsFeed = _host.Services.GetRequiredService<DiagnosticsFeedService>();
            _diagnosticsFeed.StartAsync().GetAwaiter().GetResult();

            var authDialogs = _host.Services.GetRequiredService<IAuthenticationDialogService>();
            if (!authDialogs.EnsureAuthenticated())
            {
                Shutdown();
                return;
            }

            _ = EnsureMachineTriggersSafeAsync();

            var realtime = _host.Services.GetRequiredService<ISignalRClientService>();
            realtime.Start();

            var shellViewModel = _host.Services.GetRequiredService<MainWindowViewModel>();
            shellViewModel.RefreshShellContext();

            var window = _host.Services.GetRequiredService<MainWindow>();
            window.Show();
        }

        private Task EnsureMachineTriggersSafeAsync()
        {
            if (_host?.Services is not IServiceProvider services)
            {
                return Task.CompletedTask;
            }

            var trace = services.GetService<ITrace>();

            return Task.Run(async () =>
            {
                try
                {
                    var database = services.GetRequiredService<DatabaseService>();
                    await database.EnsureMachineTriggersForMachinesAsync().ConfigureAwait(false);
                    trace?.Log(DiagLevel.Info, "startup", "machines_triggers_succeeded", "Machine trigger reconciliation completed.");
                }
                catch (MySqlException ex)
                {
                    trace?.Log(DiagLevel.Warn, "startup", "machines_triggers_permission", ex.Message, ex);
                }
                catch (InvalidOperationException ex)
                {
                    trace?.Log(DiagLevel.Warn, "startup", "machines_triggers_permission", ex.Message, ex);
                }
                catch (Exception ex)
                {
                    trace?.Log(DiagLevel.Error, "startup", "machines_triggers_failed", ex.Message, ex);
                }
            });
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_diagnosticsFeed is not null)
            {
                try
                {
                    _diagnosticsFeed.StopAsync().GetAwaiter().GetResult();
                }
                catch
                {
                    // ignore shutdown failures
                }
                finally
                {
                    _diagnosticsFeed.Dispose();
                    _diagnosticsFeed = null;
                }
            }

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

        private static IEnumerable<ILogSink> CreateDiagnosticsSinks(IConfiguration configuration, DiagnosticContext context)
        {
            var sinks = (configuration[DiagnosticsConstants.KeySinks] ?? "file,stdout")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var name in sinks)
            {
                switch (name.ToLowerInvariant())
                {
                    case "file":
                        yield return new FileLogSink(context);
                        break;
                    case "stdout":
                        yield return new StdoutLogSink();
                        break;
                    case "sqlite":
                        yield return new SQLiteLogSink();
                        break;
                    case "elastic":
                        yield return new ElasticCompatibleSink(configuration);
                        break;
                }
            }
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
