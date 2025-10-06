using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels
{
    /// <summary>Module explorer anchorable view-model.</summary>
    public partial class ModuleTreeViewModel : AnchorableViewModel
    {
        private readonly ILocalizationService _localization;

        public ModuleTreeViewModel(ILocalizationService localization)
        {
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            ContentId = "YasGmp.Shell.Modules";
            Modules = new ObservableCollection<ModuleNodeViewModel>();

            ApplyLocalization();
            RebuildModules();

            _localization.LanguageChanged += OnLanguageChanged;
        }

        /// <summary>Tree of modules grouped by feature area.</summary>
        public ObservableCollection<ModuleNodeViewModel> Modules { get; }

        [ObservableProperty]
        private string _toolTip = string.Empty;

        [ObservableProperty]
        private string _automationName = string.Empty;

        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            ApplyLocalization();
            RebuildModules();
        }

        private void ApplyLocalization()
        {
            Title = _localization.GetString("Dock.Modules.Title");
            AutomationId = _localization.GetString("Dock.Modules.AutomationId");
            AutomationName = _localization.GetString("ModuleTree.AutomationName");
            ToolTip = _localization.GetString("ModuleTree.ToolTip");
        }

        private void RebuildModules()
        {
            Modules.Clear();
            foreach (var node in ModuleNodeViewModel.CreateDefaultTree(_localization))
            {
                Modules.Add(node);
            }
        }
    }

    /// <summary>Tree node representation for a GMP feature module.</summary>
    public partial class ModuleNodeViewModel : ObservableObject
    {
        private readonly ILocalizationService _localization;
        private readonly string _titleKey;
        private readonly string? _toolTipKey;
        private readonly string? _automationNameKey;
        private readonly string? _automationIdKey;

        public ModuleNodeViewModel(
            ILocalizationService localization,
            string titleKey,
            string? toolTipKey = null,
            string? automationNameKey = null,
            string? automationIdKey = null)
        {
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _titleKey = titleKey ?? throw new ArgumentNullException(nameof(titleKey));
            _toolTipKey = toolTipKey;
            _automationNameKey = automationNameKey;
            _automationIdKey = automationIdKey;

            Children = new ObservableCollection<ModuleNodeViewModel>();
            RefreshLocalization();
        }

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string? _toolTip;

        [ObservableProperty]
        private string? _automationName;

        [ObservableProperty]
        private string? _automationId;

        public ObservableCollection<ModuleNodeViewModel> Children { get; }

        public void RefreshLocalization()
        {
            Title = _localization.GetString(_titleKey);
            ToolTip = _toolTipKey is null ? null : _localization.GetString(_toolTipKey);
            AutomationName = _localization.GetString(_automationNameKey ?? _titleKey);
            AutomationId = _automationIdKey is null ? AutomationName : _localization.GetString(_automationIdKey);
        }

        public static ObservableCollection<ModuleNodeViewModel> CreateDefaultTree(ILocalizationService localization)
        {
            var quality = new ModuleNodeViewModel(
                localization,
                "ModuleTree.Category.Quality.Title",
                "ModuleTree.Category.Quality.ToolTip",
                "ModuleTree.Category.Quality.AutomationName",
                "ModuleTree.Category.Quality.AutomationId");
            quality.Children.Add(new ModuleNodeViewModel(localization,
                "ModuleTree.Node.Quality.Capa.Title",
                "ModuleTree.Node.Quality.Capa.ToolTip",
                "ModuleTree.Node.Quality.Capa.AutomationName",
                "ModuleTree.Node.Quality.Capa.AutomationId"));
            quality.Children.Add(new ModuleNodeViewModel(localization,
                "ModuleTree.Node.Quality.Audits.Title",
                "ModuleTree.Node.Quality.Audits.ToolTip",
                "ModuleTree.Node.Quality.Audits.AutomationName",
                "ModuleTree.Node.Quality.Audits.AutomationId"));
            quality.Children.Add(new ModuleNodeViewModel(localization,
                "ModuleTree.Node.Quality.DocumentControl.Title",
                "ModuleTree.Node.Quality.DocumentControl.ToolTip",
                "ModuleTree.Node.Quality.DocumentControl.AutomationName",
                "ModuleTree.Node.Quality.DocumentControl.AutomationId"));

            var maintenance = new ModuleNodeViewModel(
                localization,
                "ModuleTree.Category.Maintenance.Title",
                "ModuleTree.Category.Maintenance.ToolTip",
                "ModuleTree.Category.Maintenance.AutomationName",
                "ModuleTree.Category.Maintenance.AutomationId");
            maintenance.Children.Add(new ModuleNodeViewModel(localization,
                "ModuleTree.Node.Maintenance.Machines.Title",
                "ModuleTree.Node.Maintenance.Machines.ToolTip",
                "ModuleTree.Node.Maintenance.Machines.AutomationName",
                "ModuleTree.Node.Maintenance.Machines.AutomationId"));
            maintenance.Children.Add(new ModuleNodeViewModel(localization,
                "ModuleTree.Node.Maintenance.PreventivePlans.Title",
                "ModuleTree.Node.Maintenance.PreventivePlans.ToolTip",
                "ModuleTree.Node.Maintenance.PreventivePlans.AutomationName",
                "ModuleTree.Node.Maintenance.PreventivePlans.AutomationId"));
            maintenance.Children.Add(new ModuleNodeViewModel(localization,
                "ModuleTree.Node.Maintenance.Calibration.Title",
                "ModuleTree.Node.Maintenance.Calibration.ToolTip",
                "ModuleTree.Node.Maintenance.Calibration.AutomationName",
                "ModuleTree.Node.Maintenance.Calibration.AutomationId"));

            var warehouse = new ModuleNodeViewModel(
                localization,
                "ModuleTree.Category.Warehouse.Title",
                "ModuleTree.Category.Warehouse.ToolTip",
                "ModuleTree.Category.Warehouse.AutomationName",
                "ModuleTree.Category.Warehouse.AutomationId");
            warehouse.Children.Add(new ModuleNodeViewModel(localization,
                "ModuleTree.Node.Warehouse.Inventory.Title",
                "ModuleTree.Node.Warehouse.Inventory.ToolTip",
                "ModuleTree.Node.Warehouse.Inventory.AutomationName",
                "ModuleTree.Node.Warehouse.Inventory.AutomationId"));
            warehouse.Children.Add(new ModuleNodeViewModel(localization,
                "ModuleTree.Node.Warehouse.Suppliers.Title",
                "ModuleTree.Node.Warehouse.Suppliers.ToolTip",
                "ModuleTree.Node.Warehouse.Suppliers.AutomationName",
                "ModuleTree.Node.Warehouse.Suppliers.AutomationId"));
            warehouse.Children.Add(new ModuleNodeViewModel(localization,
                "ModuleTree.Node.Warehouse.Transactions.Title",
                "ModuleTree.Node.Warehouse.Transactions.ToolTip",
                "ModuleTree.Node.Warehouse.Transactions.AutomationName",
                "ModuleTree.Node.Warehouse.Transactions.AutomationId"));

            return new ObservableCollection<ModuleNodeViewModel>
            {
                quality,
                maintenance,
                warehouse
            };
        }
    }
}
