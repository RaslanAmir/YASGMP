using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace YasGMP.Wpf.ViewModels
{
    /// <summary>Represents the tabbed machines workspace.</summary>
    public partial class MachinesDocumentViewModel : DocumentViewModel
    {
        [ObservableProperty]
        private MachineRowViewModel? _selectedMachine;
        /// <summary>
        /// Initializes a new instance of the MachinesDocumentViewModel class.
        /// </summary>

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
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        public int Id { get; init; }
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; init; } = string.Empty;
        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        public string Status { get; init; } = string.Empty;
        /// <summary>
        /// Gets or sets the location.
        /// </summary>
        public string Location { get; init; } = string.Empty;
        /// <summary>
        /// Gets or sets the oee.
        /// </summary>
        public double Oee { get; init; }
        /// <summary>
        /// Gets or sets the last maintenance.
        /// </summary>
        public string LastMaintenance { get; init; } = string.Empty;
        /// <summary>
        /// Gets or sets the next maintenance.
        /// </summary>
        public string NextMaintenance { get; init; } = string.Empty;
    }
}
