using System;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using YasGMP.ViewModels;

namespace YasGMP.Views
{
    /// <summary>
    /// AdminPanelPage — Central administration console with Users, RBAC, System, and Tools tabs.
    /// Resolves <see cref="AdminViewModel"/> via DI and triggers an initial refresh on appear.
    /// </summary>
    public partial class AdminPanelPage : TabbedPage
    {
        /// <summary>
        /// Initialize and resolve the <see cref="AdminViewModel"/> from DI.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if DI is not available or VM not registered.</exception>
        public AdminPanelPage()
        {
            InitializeComponent();

            var services = Application.Current?.Handler?.MauiContext?.Services
                ?? throw new InvalidOperationException(
                    "MAUI ServiceProvider is not available. Ensure this page is registered in DI and the app is built via MauiProgram.CreateMauiApp().");

            var vm = services.GetService<AdminViewModel>()
                ?? throw new InvalidOperationException(
                    "Unable to resolve AdminViewModel from the ServiceProvider. Confirm builder.Services.AddTransient<AdminViewModel>().");

            BindingContext = vm;
        }

        /// <summary>Triggers an initial refresh when the page appears.</summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is AdminViewModel vm && vm.RefreshAllCommand is IAsyncRelayCommand refresh)
            {
                _ = refresh.ExecuteAsync(null);
            }
        }

        /// <summary>Open the Users page via Shell route.</summary>
        private async void OnToolbarOpenUsers(object sender, EventArgs e)
        {
            if (Shell.Current is not null)
            {
                await Shell.Current.GoToAsync("routes/users").ConfigureAwait(false);
            }
        }
    }
}
