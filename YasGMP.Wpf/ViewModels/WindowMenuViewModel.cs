using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.ViewModels
{
    /// <summary>Window menu command surface wired to shell-level operations.</summary>
    public partial class WindowMenuViewModel
    {
        private readonly MainWindowViewModel _shell;
        /// <summary>
        /// Initializes a new instance of the WindowMenuViewModel class.
        /// </summary>

        public WindowMenuViewModel(MainWindowViewModel shell)
        {
            _shell = shell;
            OpenDashboardCommand = new RelayCommand(() => _shell.OpenModule(DashboardModuleViewModel.ModuleKey));
            OpenAssetsCommand = new RelayCommand(() => _shell.OpenModule(AssetsModuleViewModel.ModuleKey));
            OpenComponentsCommand = new RelayCommand(() => _shell.OpenModule(ComponentsModuleViewModel.ModuleKey));
            OpenWorkOrdersCommand = new RelayCommand(() => _shell.OpenModule(WorkOrdersModuleViewModel.ModuleKey));
            OpenWarehouseCommand = new RelayCommand(() => _shell.OpenModule(WarehouseModuleViewModel.ModuleKey));
            OpenCalibrationCommand = new RelayCommand(() => _shell.OpenModule(CalibrationModuleViewModel.ModuleKey));
            OpenPartsCommand = new RelayCommand(() => _shell.OpenModule(PartsModuleViewModel.ModuleKey));
            OpenSuppliersCommand = new RelayCommand(() => _shell.OpenModule(SuppliersModuleViewModel.ModuleKey));
            OpenCapaCommand = new RelayCommand(() => _shell.OpenModule(CapaModuleViewModel.ModuleKey));
            OpenIncidentsCommand = new RelayCommand(() => _shell.OpenModule(IncidentsModuleViewModel.ModuleKey));
            OpenChangeControlCommand = new RelayCommand(() => _shell.OpenModule(ChangeControlModuleViewModel.ModuleKey));
            OpenDocumentControlCommand = new RelayCommand(() => _shell.OpenModule(DocumentControlModuleViewModel.ModuleKey));
            OpenValidationsCommand = new RelayCommand(() => _shell.OpenModule(ValidationsModuleViewModel.ModuleKey));
            OpenSecurityCommand = new RelayCommand(() => _shell.OpenModule(SecurityModuleViewModel.ModuleKey));
            OpenAdministrationCommand = new RelayCommand(() => _shell.OpenModule(AdminModuleViewModel.ModuleKey));
            OpenDiagnosticsCommand = new RelayCommand(() => _shell.OpenModule(DiagnosticsModuleViewModel.ModuleKey));
            var openAuditTrailCommand = new RelayCommand(() => _shell.OpenModule(AuditModuleViewModel.ModuleKey));
            OpenAuditTrailCommand = openAuditTrailCommand;
            OpenAuditCommand = openAuditTrailCommand;
            OpenAuditDashboardCommand = new RelayCommand(() => _shell.OpenModule(AuditDashboardDocumentViewModel.ModuleKey));
            OpenApiAuditCommand = new RelayCommand(() => _shell.OpenModule(ApiAuditModuleViewModel.ModuleKey));
            SaveLayoutCommand = new AsyncRelayCommand(ExecuteSaveLayoutAsync);
            ResetLayoutCommand = new AsyncRelayCommand(ExecuteResetLayoutAsync);
        }
        /// <summary>
        /// Gets or sets the open dashboard command.
        /// </summary>

        public IRelayCommand OpenDashboardCommand { get; }
        /// <summary>
        /// Gets or sets the open assets command.
        /// </summary>

        public IRelayCommand OpenAssetsCommand { get; }
        /// <summary>
        /// Gets or sets the open components command.
        /// </summary>

        public IRelayCommand OpenComponentsCommand { get; }
        /// <summary>
        /// Gets or sets the open work orders command.
        /// </summary>

        public IRelayCommand OpenWorkOrdersCommand { get; }
        /// <summary>
        /// Gets or sets the open warehouse command.
        /// </summary>

        public IRelayCommand OpenWarehouseCommand { get; }
        /// <summary>
        /// Gets or sets the open calibration command.
        /// </summary>

        public IRelayCommand OpenCalibrationCommand { get; }
        /// <summary>
        /// Gets or sets the open parts command.
        /// </summary>

        public IRelayCommand OpenPartsCommand { get; }
        /// <summary>
        /// Gets or sets the open suppliers command.
        /// </summary>

        public IRelayCommand OpenSuppliersCommand { get; }
        /// <summary>
        /// Gets or sets the open capa command.
        /// </summary>

        public IRelayCommand OpenCapaCommand { get; }
        /// <summary>
        /// Gets or sets the open incidents command.
        /// </summary>

        public IRelayCommand OpenIncidentsCommand { get; }
        /// <summary>
        /// Gets or sets the open change control command.
        /// </summary>

        public IRelayCommand OpenChangeControlCommand { get; }
        /// <summary>
        /// Gets or sets the open document control command.
        /// </summary>

        public IRelayCommand OpenDocumentControlCommand { get; }
        /// <summary>
        /// Gets or sets the open validations command.
        /// </summary>

        public IRelayCommand OpenValidationsCommand { get; }
        /// <summary>
        /// Gets or sets the open security command.
        /// </summary>

        public IRelayCommand OpenSecurityCommand { get; }
        /// <summary>
        /// Gets or sets the open administration command.
        /// </summary>

        public IRelayCommand OpenAdministrationCommand { get; }
        /// <summary>
        /// Gets or sets the open diagnostics command.
        /// </summary>

        public IRelayCommand OpenDiagnosticsCommand { get; }

        /// <summary>Opens the immutable audit trail module (legacy alias for <see cref="OpenAuditTrailCommand"/>).</summary>
        public IRelayCommand OpenAuditCommand { get; }

        /// <summary>Opens the immutable audit trail module (Quality &amp; Compliance surface).</summary>
        public IRelayCommand OpenAuditTrailCommand { get; }

        /// <summary>Opens the audit dashboard document.</summary>
        public IRelayCommand OpenAuditDashboardCommand { get; }

        /// <summary>Opens the API audit module.</summary>
        public IRelayCommand OpenApiAuditCommand { get; }
        /// <summary>
        /// Gets or sets the save layout command.
        /// </summary>

        public IAsyncRelayCommand SaveLayoutCommand { get; }
        /// <summary>
        /// Gets or sets the reset layout command.
        /// </summary>

        public IAsyncRelayCommand ResetLayoutCommand { get; }
        /// <summary>
        /// Occurs when event handler is raised.
        /// </summary>

        public event EventHandler? SaveLayoutRequested;
        /// <summary>
        /// Occurs when event handler is raised.
        /// </summary>

        public event EventHandler? ResetLayoutRequested;

        private Task ExecuteSaveLayoutAsync()
        {
            SaveLayoutRequested?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }

        private Task ExecuteResetLayoutAsync()
        {
            ResetLayoutRequested?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }
    }
}
