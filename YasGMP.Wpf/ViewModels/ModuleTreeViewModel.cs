using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace YasGMP.Wpf.ViewModels
{
    /// <summary>Module explorer anchorable view-model.</summary>
    public partial class ModuleTreeViewModel : AnchorableViewModel
    {
        public ModuleTreeViewModel()
        {
            Title = "Modules";
            ContentId = "YasGmp.Shell.Modules";
            Modules = new ObservableCollection<ModuleNodeViewModel>(ModuleNodeViewModel.CreateDefaultTree());
        }

        /// <summary>Tree of modules grouped by feature area.</summary>
        public ObservableCollection<ModuleNodeViewModel> Modules { get; }
    }

    /// <summary>Tree node representation for a GMP feature module.</summary>
    public class ModuleNodeViewModel : ObservableObject
    {
        public ModuleNodeViewModel(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public ObservableCollection<ModuleNodeViewModel> Children { get; } = new();

        public static ObservableCollection<ModuleNodeViewModel> CreateDefaultTree()
        {
            return new ObservableCollection<ModuleNodeViewModel>
            {
                new("Quality")
                {
                    Children =
                    {
                        new ModuleNodeViewModel("CAPA"),
                        new ModuleNodeViewModel("Audits"),
                        new ModuleNodeViewModel("Document Control")
                    }
                },
                new("Maintenance")
                {
                    Children =
                    {
                        new ModuleNodeViewModel("Machines"),
                        new ModuleNodeViewModel("Preventive Plans"),
                        new ModuleNodeViewModel("Calibration")
                    }
                },
                new("Warehouse")
                {
                    Children =
                    {
                        new ModuleNodeViewModel("Inventory"),
                        new ModuleNodeViewModel("Suppliers"),
                        new ModuleNodeViewModel("Transactions")
                    }
                }
            };
        }
    }
}
