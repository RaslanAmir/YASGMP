using Microsoft.Maui.Controls;
using YasGMP.ViewModels;

namespace YasGMP.Views
{
    /// <summary>
    /// Glavni dashboard (MainPage). Povezuje ViewModel i upravlja osnovnom logikom prikaza.
    /// </summary>
    public partial class MainPage : ContentPage
    {
        /// <summary>
        /// Initializes a new instance of the MainPage class.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();
            // If you use DI for this VM, replace with DI resolve.
            BindingContext = new MainPageViewModel();
        }
    }
}
