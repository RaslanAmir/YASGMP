using System;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using YasGMP.ViewModels;

namespace YasGMP.Views
{
    /// <summary>
    /// <b>AdminPanelPage</b> — Central administration console with Users, RBAC, System, and Tools tabs.
    /// Resolves <see cref="AdminViewModel"/> via DI and triggers an initial refresh on appear.
    /// </summary>
    public partial class AdminPanelPage : TabbedPage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AdminPanelPage"/> class and resolves the view model.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the DI service provider or <see cref="AdminViewModel"/> cannot be resolved.
        /// </exception>
        public AdminPanelPage()
        {
            InitializeComponent();

            var services = Application.Current?.Handler?.MauiContext?.Services
                ?? throw new InvalidOperationException(
                    "MAUI ServiceProvider is not available. Ensure this page is registered in DI and the app is built via MauiProgram.CreateMauiApp().");

            var vm = services.GetService<AdminViewModel>()
                ?? throw new InvalidOperationException(
                    "Unable to resolve AdminViewModel from the ServiceProvider. Confirm that builder.Services.AddTransient<AdminViewModel>() is configured in MauiProgram.");

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

        /// <summary>
        /// Opens the dedicated Users page for detailed user, role and permission management.
        /// </summary>
        private async void OnToolbarOpenUsers(object sender, EventArgs e)
        {
            // Route registered in AppShell.AppRoutes.Users = "routes/users"
            if (Shell.Current is not null)
            {
                await Shell.Current.GoToAsync("routes/users").ConfigureAwait(false);
            }
        }
    }
}
