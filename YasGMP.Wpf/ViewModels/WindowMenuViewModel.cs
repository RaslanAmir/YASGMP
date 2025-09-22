using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;

namespace YasGMP.Wpf.ViewModels
{
    /// <summary>Window menu command surface wired to shell-level operations.</summary>
    public partial class WindowMenuViewModel
    {
        private readonly MainWindowViewModel _shell;

        public WindowMenuViewModel(MainWindowViewModel shell)
        {
            _shell = shell;
            NewMachinesDocumentCommand = new RelayCommand(() => _shell.OpenMachinesDocument());
            NavigateToMachinesCommand = new RelayCommand(_shell.NavigateToMachines);
            SaveLayoutCommand = new AsyncRelayCommand(ExecuteSaveLayoutAsync);
            ResetLayoutCommand = new AsyncRelayCommand(ExecuteResetLayoutAsync);
        }

        public IRelayCommand NewMachinesDocumentCommand { get; }

        public IRelayCommand NavigateToMachinesCommand { get; }

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
