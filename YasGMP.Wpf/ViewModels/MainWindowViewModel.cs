using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels
{
    /// <summary>
    /// View-model backing the root shell window. Hosts dockable content collections
    /// and exposes menu commands for opening/navigating workspaces.
    /// </summary>
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly IMachineDataService _machineDataService;
        private DocumentViewModel? _activeDocument;
        private string _statusText = "Ready";
        private int _machinesOpened = 0;

        public MainWindowViewModel(IMachineDataService machineDataService)
        {
            _machineDataService = machineDataService;
            ModuleTree = new ModuleTreeViewModel();
            Cockpit = new CockpitViewModel();
            Documents = new ObservableCollection<DocumentViewModel>();
            WindowCommands = new WindowMenuViewModel(this);
        }

        /// <summary>Gets the module navigator anchorable.</summary>
        public ModuleTreeViewModel ModuleTree { get; }

        /// <summary>Gets the cockpit/status anchorable.</summary>
        public CockpitViewModel Cockpit { get; }

        /// <summary>Collection of tabbed documents (bound to AvalonDock).</summary>
        public ObservableCollection<DocumentViewModel> Documents { get; }

        /// <summary>Gets or sets the currently active document.</summary>
        public DocumentViewModel? ActiveDocument
        {
            get => _activeDocument;
            set => SetProperty(ref _activeDocument, value);
        }

        /// <summary>Command surface for the Window menu.</summary>
        public WindowMenuViewModel WindowCommands { get; }

        /// <summary>Status line content shown in the shell status bar.</summary>
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        /// <summary>Ensures initial anchorables and a starter machines tab are present.</summary>
        public void InitializeWorkspace()
        {
            if (Documents.Count == 0)
            {
                OpenMachinesDocument();
            }
        }

        /// <summary>Creates a new machines workspace tab populated with mock data.</summary>
        public MachinesDocumentViewModel OpenMachinesDocument()
        {
            _machinesOpened++;
            var title = _machinesOpened == 1 ? "Machines" : $"Machines {_machinesOpened}";
            var contentId = $"YasGmp.Shell.Machines.{Guid.NewGuid():N}";
            var machines = new ObservableCollection<MachineRowViewModel>(_machineDataService.GetMachines());
            var vm = new MachinesDocumentViewModel(title, contentId, machines);
            Documents.Add(vm);
            ActiveDocument = vm;
            StatusText = $"Opened {title} at {DateTime.Now:t}";
            return vm;
        }

        /// <summary>Locates or creates a machines workspace for navigation.</summary>
        public void NavigateToMachines()
        {
            var target = Documents.OfType<MachinesDocumentViewModel>().FirstOrDefault();
            if (target == null)
            {
                target = OpenMachinesDocument();
            }

            ActiveDocument = target;
            StatusText = $"Navigated to {target.Title} at {DateTime.Now:t}";
        }

        /// <summary>
        /// Used by layout deserialization to resolve persisted content by id.
        /// Creates a new machines document if the requested id does not exist yet.
        /// </summary>
        public DocumentViewModel EnsureDocumentForId(string contentId)
        {
            var existing = Documents.FirstOrDefault(d => d.ContentId == contentId);
            if (existing != null)
            {
                return existing;
            }

            _machinesOpened++;
            var machines = new ObservableCollection<MachineRowViewModel>(_machineDataService.GetMachines());
            var title = _machinesOpened == 1 ? "Machines" : $"Machines {_machinesOpened}";
            var vm = new MachinesDocumentViewModel(title, contentId, machines);
            Documents.Add(vm);
            return vm;
        }

        /// <summary>Clears document state before applying a serialized layout.</summary>
        public void PrepareForLayoutImport()
        {
            Documents.Clear();
            _machinesOpened = 0;
            ActiveDocument = null;
        }

        /// <summary>Removes all dynamic documents and re-adds a fresh machines workspace.</summary>
        public void ResetWorkspace()
        {
            Documents.Clear();
            _machinesOpened = 0;
            InitializeWorkspace();
            StatusText = "Layout reset to default";
        }
    }
}
