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
            OpenWorkOrdersCommand = new RelayCommand(() => _shell.OpenModule(WorkOrdersModuleViewModel.ModuleKey));
            SaveLayoutCommand = new AsyncRelayCommand(ExecuteSaveLayoutAsync);
            ResetLayoutCommand = new AsyncRelayCommand(ExecuteResetLayoutAsync);
        }

        public IRelayCommand OpenDashboardCommand { get; }

        public IRelayCommand OpenAssetsCommand { get; }

        public IRelayCommand OpenWorkOrdersCommand { get; }

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
