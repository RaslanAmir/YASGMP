// ==============================================================================
// File: Views/UsersPage.xaml.cs
// Purpose: Users admin page (users, roles, permissions) — resolves VMs via DI
// Notes:
//   • Exposes UserVM and RolePermVM for XAML binding.
//   • Requires registrations in MauiProgram.cs:
//       services.AddTransient<UserViewModel>();
//       services.AddTransient<UserRolePermissionViewModel>();
//       services.AddTransient<UsersPage>();
//   • XAML must use x:Class="YasGMP.Views.UsersPage"
// ==============================================================================

#nullable enable
using System;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using YasGMP.ViewModels;

namespace YasGMP.Views
{
    /// <summary>
    /// Users admin page for managing users, roles, and permissions.
    /// ViewModels are resolved via the MAUI service provider and exposed
    /// as properties for XAML binding.
    /// </summary>
    public partial class UsersPage : ContentPage
    {
        /// <summary>
        /// ViewModel handling user CRUD, filtering, and search.
        /// Bind in XAML with <c>{Binding UserVM.*}</c>.
        /// </summary>
        public UserViewModel UserVM { get; }

        /// <summary>
        /// ViewModel handling role and permission assignments for a selected user.
        /// Bind in XAML with <c>{Binding RolePermVM.*}</c>.
        /// </summary>
        public UserRolePermissionViewModel RolePermVM { get; }

        /// <summary>
        /// Initializes the page and resolves required view models from the MAUI service provider.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the MAUI service provider is not available (page not registered in DI).
        /// </exception>
        public UsersPage()
        {
            InitializeComponent();

            var services = Application.Current?.Handler?.MauiContext?.Services
                ?? throw new InvalidOperationException(
                    "Service provider unavailable. Ensure UsersPage and its ViewModels are registered in MauiProgram.");

            UserVM = services.GetRequiredService<UserViewModel>();
            RolePermVM = services.GetRequiredService<UserRolePermissionViewModel>();

            // Expose both VMs (and any helper props) through this page as the BindingContext.
            BindingContext = this;
        }
    }
}
