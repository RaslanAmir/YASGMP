using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AvalonDock;
using AvalonDock.Layout.Serialization;
using YasGMP.Wpf.ViewModels;

namespace YasGMP.Wpf.Services
{
    /// <summary>Coordinates AvalonDock with layout persistence and view-model resolution.</summary>
    public sealed class ShellLayoutController
    {
        private readonly DockLayoutPersistenceService _persistence;
        private DockingManager? _dockManager;
        private MainWindowViewModel? _viewModel;
        private string? _defaultLayout;
        private const string LayoutKey = "YasGmp.Wpf.Shell";

        public ShellLayoutController(DockLayoutPersistenceService persistence)
        {
            _persistence = persistence;
        }

        public void Attach(DockingManager dockManager, MainWindowViewModel viewModel)
        {
            _dockManager = dockManager ?? throw new ArgumentNullException(nameof(dockManager));
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }

        public void CaptureDefaultLayout()
        {
            if (_dockManager == null)
            {
                return;
            }

            var serializer = new XmlLayoutSerializer(_dockManager);
            using var writer = new StringWriter();
            serializer.Serialize(writer);
            _defaultLayout = writer.ToString();
        }

        public async Task RestoreLayoutAsync(Window window, CancellationToken token = default)
        {
            if (_dockManager == null)
            {
                return;
            }

            var snapshot = await _persistence.LoadAsync(LayoutKey, token).ConfigureAwait(false);
            if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.Value.LayoutXml))
            {
                return;
            }

            _viewModel?.PrepareForLayoutImport();
            await window.Dispatcher.InvokeAsync(() =>
            {
                var serializer = new XmlLayoutSerializer(_dockManager);
                serializer.LayoutSerializationCallback += OnLayoutSerializationCallback;
                using var reader = new StringReader(snapshot.Value.LayoutXml);
                serializer.Deserialize(reader);
                serializer.LayoutSerializationCallback -= OnLayoutSerializationCallback;
            });

            ApplyGeometry(window, snapshot.Value.Geometry);
        }

        public async Task SaveLayoutAsync(Window window, CancellationToken token = default)
        {
            if (_dockManager == null)
            {
                return;
            }

            var serializer = new XmlLayoutSerializer(_dockManager);
            string layoutXml;
            using (var writer = new StringWriter())
            {
                serializer.Serialize(writer);
                layoutXml = writer.ToString();
            }

            var geometry = CaptureGeometry(window);
            await _persistence.SaveAsync(LayoutKey, layoutXml, geometry, token).ConfigureAwait(false);
        }

        public async Task ResetLayoutAsync(Window window, CancellationToken token = default)
        {
            if (_dockManager == null || _defaultLayout == null)
            {
                return;
            }

            _viewModel?.PrepareForLayoutImport();
            await window.Dispatcher.InvokeAsync(() =>
            {
                var serializer = new XmlLayoutSerializer(_dockManager);
                serializer.LayoutSerializationCallback += OnLayoutSerializationCallback;
                using var reader = new StringReader(_defaultLayout);
                serializer.Deserialize(reader);
                serializer.LayoutSerializationCallback -= OnLayoutSerializationCallback;
            });

            if (_viewModel != null)
            {
                _viewModel.StatusText = "Layout reset to default";
            }
            await SaveLayoutAsync(window, token).ConfigureAwait(false);
        }

        private void OnLayoutSerializationCallback(object? sender, LayoutSerializationCallbackEventArgs e)
        {
            if (_viewModel == null)
            {
                return;
            }

            switch (e.Model.ContentId)
            {
                case "YasGmp.Shell.Modules":
                    e.Content = _viewModel.ModulesPane;
                    break;
                case "YasGmp.Shell.Inspector":
                    e.Content = _viewModel.InspectorPane;
                    break;
                default:
                    if (!string.IsNullOrWhiteSpace(e.Model.ContentId))
                    {
                        e.Content = _viewModel.EnsureDocumentForId(e.Model.ContentId);
                    }
                    break;
            }
        }

        private static WindowGeometry CaptureGeometry(Window window)
        {
            if (window == null)
            {
                return new WindowGeometry(null, null, null, null);
            }

            if (window.WindowState == WindowState.Normal)
            {
                return new WindowGeometry(window.Left, window.Top, window.Width, window.Height);
            }

            var bounds = window.RestoreBounds;
            return new WindowGeometry(bounds.Left, bounds.Top, bounds.Width, bounds.Height);
        }

        private static void ApplyGeometry(Window window, WindowGeometry geometry)
        {
            if (geometry.Left.HasValue)
            {
                window.Left = geometry.Left.Value;
            }

            if (geometry.Top.HasValue)
            {
                window.Top = geometry.Top.Value;
            }

            if (geometry.Width.HasValue && geometry.Width.Value > 200)
            {
                window.Width = geometry.Width.Value;
            }

            if (geometry.Height.HasValue && geometry.Height.Value > 200)
            {
                window.Height = geometry.Height.Value;
            }
        }
    }
}
