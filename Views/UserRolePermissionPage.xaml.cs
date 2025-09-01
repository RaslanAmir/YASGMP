using System;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using YasGMP.ViewModels;

namespace YasGMP.Views
{
    /// <summary>
    /// UI for advanced user/role/permission management powered by
    /// <see cref="UserRolePermissionViewModel"/>.
    /// </summary>
    public partial class UserRolePermissionPage : ContentPage
    {
        public UserRolePermissionPage()
        {
            InitializeComponent();
            var services = Application.Current?.Handler?.MauiContext?.Services
                           ?? throw new InvalidOperationException("Service provider not available.");
            BindingContext = services.GetService<UserRolePermissionViewModel>()
                           ?? throw new InvalidOperationException("UserRolePermissionViewModel not registered in DI.");
        }
    }
}