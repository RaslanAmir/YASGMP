using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace YasGMP.Wpf.ViewModels
{
    /// <summary>Represents the tabbed machines workspace.</summary>
    public partial class MachinesDocumentViewModel : DocumentViewModel
    {
        [ObservableProperty]
        private MachineRowViewModel? _selectedMachine;

        public MachinesDocumentViewModel(string title, string contentId, ObservableCollection<MachineRowViewModel> machines)
        {
            Title = title;
            ContentId = contentId;
            Machines = machines;
        }

        /// <summary>Table rows rendered inside the machines tab.</summary>
        public ObservableCollection<MachineRowViewModel> Machines { get; }
    }

    /// <summary>Row backing model for the machines data grid.</summary>
    public partial class MachineRowViewModel : ObservableObject
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public string Location { get; init; } = string.Empty;
        public double Oee { get; init; }
        public string LastMaintenance { get; init; } = string.Empty;
        public string NextMaintenance { get; init; } = string.Empty;
    }
}
