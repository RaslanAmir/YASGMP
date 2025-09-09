using System;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using YasGMP.ViewModels;

namespace YasGMP.Views
{
    /// <summary>
    /// YASTECH RBAC Manager UI hosting <see cref="UserRolePermissionViewModel"/>.
    /// Resolves the VM from DI on handler creation to avoid XAML/XFC issues.
    /// </summary>
    public partial class UserRolePermissionPage : ContentPage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserRolePermissionPage"/> class.
        /// </summary>
        /// <remarks>
        /// The BindingContext is set in <see cref="OnHandlerChanged"/> once the page is
        /// attached to the app's MAUI Handler so the service provider is guaranteed.
        /// </remarks>
        public UserRolePermissionPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// When the Handler is ready, resolve the <see cref="UserRolePermissionViewModel"/>
        /// from DI and assign it as BindingContext. This avoids the need for a parameterless
        /// constructor on the VM and keeps AOT-friendly behavior.
        /// </summary>
        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();

            if (BindingContext is null)
            {
                var services = Handler?.MauiContext?.Services
                               ?? throw new InvalidOperationException("Service provider not available.");
                var vm = services.GetService<UserRolePermissionViewModel>()
                         ?? throw new InvalidOperationException("UserRolePermissionViewModel not registered in DI.");
                BindingContext = vm;
            }
        }
    }
}
