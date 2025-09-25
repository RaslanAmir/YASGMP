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
            OpenSecurityCommand = new RelayCommand(() => _shell.OpenModule(SecurityModuleViewModel.ModuleKey));
            OpenAdministrationCommand = new RelayCommand(() => _shell.OpenModule(AdminModuleViewModel.ModuleKey));
            OpenDiagnosticsCommand = new RelayCommand(() => _shell.OpenModule(DiagnosticsModuleViewModel.ModuleKey));
            SaveLayoutCommand = new AsyncRelayCommand(ExecuteSaveLayoutAsync);
            ResetLayoutCommand = new AsyncRelayCommand(ExecuteResetLayoutAsync);
        }

        public IRelayCommand OpenDashboardCommand { get; }

        public IRelayCommand OpenAssetsCommand { get; }

        public IRelayCommand OpenComponentsCommand { get; }

        public IRelayCommand OpenWorkOrdersCommand { get; }

        public IRelayCommand OpenWarehouseCommand { get; }

        public IRelayCommand OpenCalibrationCommand { get; }

        public IRelayCommand OpenPartsCommand { get; }

        public IRelayCommand OpenSuppliersCommand { get; }

        public IRelayCommand OpenCapaCommand { get; }

        public IRelayCommand OpenIncidentsCommand { get; }

        public IRelayCommand OpenChangeControlCommand { get; }

        public IRelayCommand OpenSecurityCommand { get; }

        public IRelayCommand OpenAdministrationCommand { get; }

        public IRelayCommand OpenDiagnosticsCommand { get; }

        public IAsyncRelayCommand SaveLayoutCommand { get; }

        public IAsyncRelayCommand ResetLayoutCommand { get; }

        public event EventHandler? SaveLayoutRequested;

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
