using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using YasGMP.Common;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels
{
    /// <summary>Simple cockpit summary displayed in the bottom anchorable.</summary>
    public partial class CockpitViewModel : AnchorableViewModel
    {
        /// <summary>
        /// Initializes a new instance of the CockpitViewModel class.
        /// </summary>
        public CockpitViewModel()
            : this(ServiceLocator.GetService<ILocalizationService>())
        {
        }

        public CockpitViewModel(ILocalizationService? localization)
        {
            _localization = localization ?? new LocalizationService();
            _localization.LanguageChanged += OnLanguageChanged;

            ContentId = "YasGmp.Shell.Cockpit";
            Metrics = new ObservableCollection<CockpitMetric>();
            Notices = new ObservableCollection<string>();

            RefreshLocalizedContent();
        }
        /// <summary>
        /// Gets or sets the metrics.
        /// </summary>

        public ObservableCollection<CockpitMetric> Metrics { get; }
        /// <summary>
        /// Gets or sets the notices.
        /// </summary>

        public ObservableCollection<string> Notices { get; }

        private readonly ILocalizationService _localization;

        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            RefreshLocalizedContent();
        }

        private void RefreshLocalizedContent()
        {
            Title = _localization.GetString("Cockpit.Anchor.Title");
            AutomationId = _localization.GetString("Cockpit.Anchor.AutomationId");

            Metrics.Clear();
            Metrics.Add(new CockpitMetric(_localization.GetString("Cockpit.Metric.OpenCapas.Label"), 7, "#c7522a"));
            Metrics.Add(new CockpitMetric(_localization.GetString("Cockpit.Metric.PreventiveJobsDue.Label"), 12, "#d9842b"));
            Metrics.Add(new CockpitMetric(_localization.GetString("Cockpit.Metric.MachinesOffline.Label"), 2, "#b71c1c"));
            Metrics.Add(new CockpitMetric(_localization.GetString("Cockpit.Metric.OnTimeDeliveries.Label"), 96, "#2e7d32"));

            Notices.Clear();
            Notices.Add(_localization.GetString("Cockpit.Notice.SterilizerExpiring"));
            Notices.Add(_localization.GetString("Cockpit.Notice.CalibrationOverdue"));
            Notices.Add(_localization.GetString("Cockpit.Notice.DeviationsAwaitingApproval"));
        }
    }

    /// <summary>Represents a key KPI displayed in the cockpit.</summary>
    public record CockpitMetric(string Label, int Value, string AccentHex)
    {
        /// <summary>
        /// Executes the formatted value operation.
        /// </summary>
        public string FormattedValue => Value.ToString("N0");
    }
}
