// Views/ChangeControlPage.xaml.cs
#nullable enable
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using YasGMP.ViewModels;

namespace YasGMP.Views
{
    /// <summary>
    /// UI surface for managing change control records, including assignment of responsible users.
    /// </summary>
    public partial class ChangeControlPage : ContentPage
    {
        /// <summary>Backing view-model resolved via the MAUI service provider.</summary>
        public ChangeControlViewModel ViewModel { get; }

        /// <summary>Initializes the page and resolves the required <see cref="ChangeControlViewModel"/>.</summary>
        /// <exception cref="InvalidOperationException">Thrown when the MAUI service provider is unavailable.</exception>
        public ChangeControlPage()
        {
            InitializeComponent();

            var services = Application.Current?.Handler?.MauiContext?.Services
                ?? throw new InvalidOperationException(
                    "Service provider unavailable. Ensure ChangeControlPage is registered in MauiProgram.");

            ViewModel = services.GetRequiredService<ChangeControlViewModel>();
            BindingContext = ViewModel;
        }

        /// <inheritdoc />
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                await ViewModel.LoadAssignableUsersAsync();
            }
            catch
            {
                // LoadAssignableUsersAsync already reports errors via StatusMessage.
            }
        }
    }
}
