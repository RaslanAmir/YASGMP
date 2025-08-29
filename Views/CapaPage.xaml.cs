using Microsoft.Maui.Controls;
using YasGMP.ViewModels;

namespace YasGMP.Views
{
    /// <summary>
    /// UI View for managing Corrective and Preventive Actions (CAPA).
    /// Binds to <see cref="CapaViewModel"/>, responsible for displaying and controlling CAPA records.
    /// </summary>
    public partial class CapaPage : ContentPage
    {
        /// <summary>
        /// Initializes the CAPA page. BindingContext is set in XAML.
        /// </summary>
        public CapaPage()
        {
            InitializeComponent();
        }
    }
}
