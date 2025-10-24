using System;
using System.ComponentModel;
using System.Windows;
using Fluent;
using Microsoft.Extensions.DependencyInjection;
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

            _layoutController.RegisterAnchorableContent(ModulesPaneContent, InspectorPaneContent);
            _viewModel.WindowCommands.SaveLayoutRequested += OnSaveLayoutRequested;
            _viewModel.WindowCommands.ResetLayoutRequested += OnResetLayoutRequested;
            _viewModel.WindowCommands.OpenAiAssistantRequested += OnOpenAiAssistantRequested;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            _layoutController.Attach(DockManager, _viewModel);
            ModulesPaneContent.DataContext = _viewModel.ModulesPane;
            InspectorPaneContent.DataContext = _viewModel.InspectorPane;
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

        private void OnOpenAiAssistantRequested(object? sender, EventArgs e)
        {
            try
            {
                var vm = (Application.Current as App)?.Host?.Services
                    .GetService(typeof(YasGMP.Wpf.ViewModels.AiAssistantDialogViewModel)) as YasGMP.Wpf.ViewModels.AiAssistantDialogViewModel;
                if (vm is null)
                {
                    _viewModel.StatusText = "AI assistant unavailable (service not resolved).";
                    return;
                }

                var dialog = new YasGMP.Wpf.Dialogs.AiAssistantDialog(vm)
                {
                    Owner = this
                };
                dialog.Show();
            }
            catch (Exception ex)
            {
                _viewModel.StatusText = $"Failed to open AI assistant: {ex.Message}";
            }
        }
    }
}
