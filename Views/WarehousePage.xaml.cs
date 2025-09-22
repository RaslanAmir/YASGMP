using System;
using Microsoft.Maui.Controls;
using YasGMP.Common;
using YasGMP.ViewModels;

namespace YasGMP.Views
{
    /// <summary>
    /// Content page that exposes warehouse inventory dashboards and actions.
    /// </summary>
    public partial class WarehousePage : ContentPage
    {
        public WarehouseViewModel ViewModel { get; }

        public WarehousePage(WarehouseViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            BindingContext = ViewModel;
        }

        public WarehousePage()
            : this(ServiceLocator.GetRequiredService<WarehouseViewModel>())
        {
        }
    }
}
