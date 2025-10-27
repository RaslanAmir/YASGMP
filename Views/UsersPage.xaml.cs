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
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.ViewModels;
using YasGMP.Helpers;

namespace YasGMP.Views
{
    /// <summary>
    /// Users admin page for managing users, roles, and permissions.
    /// ViewModels are resolved via the MAUI service provider and exposed
    /// as properties for XAML binding.
    /// </summary>
    public partial class UsersPage : ContentPage
    {
        private readonly IServiceProvider _services;
        private readonly IDialogService _dialogService;

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

            _services = Application.Current?.Handler?.MauiContext?.Services
                ?? throw new InvalidOperationException(
                    "Service provider unavailable. Ensure UsersPage and its ViewModels are registered in MauiProgram.");

            UserVM     = _services.GetRequiredService<UserViewModel>();
            RolePermVM = _services.GetRequiredService<UserRolePermissionViewModel>();
            _dialogService = _services.GetRequiredService<IDialogService>();

            // Expose both VMs (and any helper props) through this page as the BindingContext.
            BindingContext = this;
        }

        private async void OnEditUserClicked(object? sender, EventArgs e)
        {
            var selectedUser = UserVM.SelectedUser;
            var mode = selectedUser is null
                ? UserEditDialogMode.Add
                : UserEditDialogMode.Update;

            var request = new UserEditDialogRequest(
                mode,
                selectedUser,
                RolePermVM.Roles,
                UserVM.Users);

            UserEditDialogResult? result;
            try
            {
                result = await _dialogService
                    .ShowDialogAsync<UserEditDialogResult>(DialogIds.UserEdit, request)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (result is null)
            {
                return;
            }

            if (result.ImpersonationRequested)
            {
                var message = result.ImpersonationTargetId.HasValue
                    ? string.Format(
                        CultureInfo.CurrentCulture,
                        ShellString.Get("Dialog.UserEdit.Status.ImpersonationRequestedWithTarget"),
                        result.ImpersonationTargetId.Value)
                    : ShellString.Get("Dialog.UserEdit.Status.ImpersonationRequested");
                UserVM.StatusMessage = message;
                return;
            }

            if (result.Saved)
            {
                var editor = result.EditorState;
                var target = selectedUser ?? new User();
                editor.ApplyTo(target);

                if (selectedUser is null)
                {
                    UserVM.Users.Add(target);
                }

                UserVM.SelectedUser = target;
                UserVM.StatusMessage = ShellString.Get("Dialog.UserEdit.Status.UpdatedFromDialog");
            }
        }
    }
}
