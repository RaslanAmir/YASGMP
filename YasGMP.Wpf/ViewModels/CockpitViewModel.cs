using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace YasGMP.Wpf.ViewModels
{
    /// <summary>Simple cockpit summary displayed in the bottom anchorable.</summary>
    public partial class CockpitViewModel : AnchorableViewModel
    {
        public CockpitViewModel()
        {
            Title = "Cockpit";
            ContentId = "YasGmp.Shell.Cockpit";
            Metrics = new ObservableCollection<CockpitMetric>
            {
                new("Open CAPAs", 7, "#c7522a"),
                new("Preventive Jobs Due", 12, "#d9842b"),
                new("Machines Offline", 2, "#b71c1c"),
                new("On-Time Deliveries", 96, "#2e7d32")
            };

            Notices = new ObservableCollection<string>
            {
                "Sterilizer #2 validation expires in 5 days",
                "Calibration overdue: Dissolution tester",
                "Two deviations awaiting QA approval"
            };
        }

        public ObservableCollection<CockpitMetric> Metrics { get; }

        public ObservableCollection<string> Notices { get; }
    }

    /// <summary>Represents a key KPI displayed in the cockpit.</summary>
    public record CockpitMetric(string Label, int Value, string AccentHex)
    {
        public string FormattedValue => Value.ToString("N0");
    }
}

