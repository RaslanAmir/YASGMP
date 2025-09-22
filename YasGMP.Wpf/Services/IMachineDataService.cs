using System.Collections.Generic;
using YasGMP.Wpf.ViewModels;

namespace YasGMP.Wpf.Services
{
    /// <summary>Provides machines snapshot data for the cockpit shell.</summary>
    public interface IMachineDataService
    {
        IEnumerable<MachineRowViewModel> GetMachines();
    }

    /// <summary>Mock implementation returning deterministic sample data.</summary>
    public sealed class MockMachineDataService : IMachineDataService
    {
        public IEnumerable<MachineRowViewModel> GetMachines()
        {
            return new[]
            {
                new MachineRowViewModel
                {
                    Id = 1001,
                    Name = "Blister Line A",
                    Status = "Running",
                    Location = "Packaging",
                    Oee = 89.4,
                    LastMaintenance = "2024-07-14",
                    NextMaintenance = "2024-10-14"
                },
                new MachineRowViewModel
                {
                    Id = 1002,
                    Name = "Granulator",
                    Status = "Maintenance",
                    Location = "Compounding",
                    Oee = 74.2,
                    LastMaintenance = "2024-06-30",
                    NextMaintenance = "2024-09-01"
                },
                new MachineRowViewModel
                {
                    Id = 1003,
                    Name = "Sterilizer #2",
                    Status = "Attention",
                    Location = "Sterile Suite",
                    Oee = 68.1,
                    LastMaintenance = "2024-05-18",
                    NextMaintenance = "2024-08-15"
                },
                new MachineRowViewModel
                {
                    Id = 1004,
                    Name = "Tablet Press",
                    Status = "Running",
                    Location = "Compression",
                    Oee = 92.6,
                    LastMaintenance = "2024-07-01",
                    NextMaintenance = "2024-09-29"
                }
            };
        }
    }
}
