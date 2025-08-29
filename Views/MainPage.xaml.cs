using Microsoft.Maui.Controls;
using YasGMP.ViewModels;

namespace YasGMP.Views
{
    /// <summary>
    /// Code-behind za glavni dashboard (MainPage).
    /// Povezuje ViewModel i upravlja osnovnom logikom prikaza.
    /// </summary>
    public partial class MainPage : ContentPage
    {
        /// <summary>
        /// Inicijalizira dashboard i postavlja DataContext na MainPageViewModel.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();

            // Ako već koristiš Dependency Injection, zamijeni ovdje!
            BindingContext = new MainPageViewModel();
        }
    }
}
