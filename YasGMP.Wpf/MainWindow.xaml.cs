using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Fluent;
using YasGMP.Common;
using YasGMP.Wpf.Automation;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf
{
    /// <summary>
    /// Shell host window that bridges the view-model with AvalonDock layout services.
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
        private readonly MainWindowViewModel _viewModel;
        private readonly ShellLayoutController _layoutController;
        private HwndSource? _windowSource;
        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>

        public MainWindow(MainWindowViewModel viewModel, ShellLayoutController layoutController)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _layoutController = layoutController;
            DataContext = _viewModel;

            _viewModel.WindowCommands.SaveLayoutRequested += OnSaveLayoutRequested;
            _viewModel.WindowCommands.ResetLayoutRequested += OnResetLayoutRequested;
        }

        /// <inheritdoc />
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var helper = new WindowInteropHelper(this);
            _windowSource = HwndSource.FromHwnd(helper.Handle);
            _windowSource?.AddHook(WndProc);
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

        /// <inheritdoc />
        protected override void OnClosed(EventArgs e)
        {
            if (_windowSource is not null)
            {
                _windowSource.RemoveHook(WndProc);
                _windowSource = null;
            }

            base.OnClosed(e);
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

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WmCopyData)
            {
                var data = Marshal.PtrToStructure<NativeMethods.CopyDataStruct>(lParam);

                switch ((SmokeAutomationCommand)data.dwData)
                {
                    case SmokeAutomationCommand.SetLanguage:
                        HandleSetLanguage(data, ref handled);
                        break;
                    case SmokeAutomationCommand.ResetInspector:
                        HandleResetInspector(ref handled);
                        break;
                }
            }

            return IntPtr.Zero;
        }

        private void HandleSetLanguage(NativeMethods.CopyDataStruct data, ref bool handled)
        {
            var payload = NativeMethods.ReadUnicodeString(data);
            if (string.IsNullOrWhiteSpace(payload))
            {
                return;
            }

            Dispatcher.Invoke(() =>
            {
                var localization = ServiceLocator.GetService<ILocalizationService>();
                localization?.SetLanguage(payload);
            });

            handled = true;
        }

        private void HandleResetInspector(ref bool handled)
        {
            Dispatcher.Invoke(() =>
            {
                var inspector = _viewModel.InspectorPane;
                inspector.Update(new InspectorContext(
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    Array.Empty<InspectorField>()));
            });

            handled = true;
        }

        private static class NativeMethods
        {
            public const int WmCopyData = 0x004A;

            [StructLayout(LayoutKind.Sequential)]
            public struct CopyDataStruct
            {
                public IntPtr dwData;
                public int cbData;
                public IntPtr lpData;
            }

            public static string? ReadUnicodeString(CopyDataStruct data)
            {
                if (data.cbData <= 0 || data.lpData == IntPtr.Zero)
                {
                    return null;
                }

                var length = data.cbData / sizeof(char);
                if (length <= 0)
                {
                    return null;
                }

                var text = Marshal.PtrToStringUni(data.lpData, length);
                return text?.TrimEnd('\0');
            }
        }
    }
}
