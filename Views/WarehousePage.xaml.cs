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
        /// <summary>
        /// Gets or sets the view model.
        /// </summary>
        public WarehouseViewModel ViewModel { get; }
        /// <summary>
        /// Initializes a new instance of the WarehousePage class.
        /// </summary>

        public WarehousePage(WarehouseViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            BindingContext = ViewModel;
        }
        /// <summary>
        /// Initializes a new instance of the WarehousePage class.
        /// </summary>

        public WarehousePage()
            : this(ServiceLocator.GetRequiredService<WarehouseViewModel>())
        {
        }
    }
}
