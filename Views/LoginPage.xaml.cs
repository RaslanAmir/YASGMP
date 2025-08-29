using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using YasGMP.ViewModels;

namespace YasGMP.Views
{
    /// <summary>
    /// Login ekran za prijavu korisnika u YasGMP sustav.
    /// ViewModel se dohvaća iz DI spremnika (nema potrebe za parametarskimless ctor-om).
    /// </summary>
    public partial class LoginPage : ContentPage
    {
        public LoginPage()
        {
            InitializeComponent();
            // BindingContext se postavlja kada Handler bude spreman
        }

        /// <summary>
        /// Kada MAUI Handler postane dostupan, uzimamo VM iz DI i postavljamo ga kao BindingContext.
        /// Time izbjegavamo XFC0004 (nema default constructora u LoginViewModel).
        /// </summary>
        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();

            if (BindingContext is null)
            {
                var services = Handler?.MauiContext?.Services;
                if (services != null)
                {
                    var vm = services.GetService<LoginViewModel>();
                    if (vm != null)
                        BindingContext = vm;
                }
            }
        }
    }
}
