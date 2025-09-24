using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using Fluent;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels;

namespace YasGMP.Wpf
{
    /// <summary>
    /// Shell host window that bridges the view-model with AvalonDock layout services.
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
        private readonly MainWindowViewModel _viewModel;
        private readonly ShellLayoutController _layoutController;

        public MainWindow(MainWindowViewModel viewModel, ShellLayoutController layoutController)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _layoutController = layoutController;
            DataContext = _viewModel;

            _viewModel.WindowCommands.SaveLayoutRequested += OnSaveLayoutRequested;
            _viewModel.WindowCommands.ResetLayoutRequested += OnResetLayoutRequested;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            _layoutController.Attach(DockManager, _viewModel);
            _viewModel.InitializeWorkspace();
            _layoutController.CaptureDefaultLayout();
            await _layoutController.RestoreLayoutAsync(this);
        }

        private async void OnClosing(object? sender, CancelEventArgs e)
        {
            await _layoutController.SaveLayoutAsync(this);
        }

        private async void OnSaveLayoutRequested(object? sender, EventArgs e)
        {
            ShellBackstage.IsOpen = false;
            await _layoutController.SaveLayoutAsync(this);
        }

        private async void OnResetLayoutRequested(object? sender, EventArgs e)
        {
            ShellBackstage.IsOpen = false;
            await _layoutController.ResetLayoutAsync(this);
        }
    }
}
