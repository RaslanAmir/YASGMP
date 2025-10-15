using System;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using YasGMP.Models.DTO;
using YasGMP.Wpf.ViewModels;
using YasGMP.Wpf.ViewModels.Dialogs;
using YasGMP.Wpf.Views;
using YasGMP.Wpf.Views.Dialogs;

namespace YasGMP.Wpf.Services
{
    /// <summary>
    /// Default implementation that surfaces login and reauthentication dialogs backed by WPF windows.
    /// </summary>
    public sealed class AuthenticationDialogService : IAuthenticationDialogService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly UserSession _userSession;

        /// <summary>Initializes a new instance of the <see cref="AuthenticationDialogService"/> class.</summary>
        public AuthenticationDialogService(IServiceProvider serviceProvider, UserSession userSession)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
        }

        /// <inheritdoc />
        public bool EnsureAuthenticated()
        {
            if (_userSession.CurrentUser is not null)
            {
                return true;
            }

            var viewModel = _serviceProvider.GetRequiredService<LoginViewModel>();
            var dialog = new LoginView
            {
                DataContext = viewModel
            };

            AttachOwner(dialog);

            return dialog.ShowDialog() == true;
        }

        /// <inheritdoc />
        public ReauthenticationResult? PromptReauthentication()
        {
            var viewModel = _serviceProvider.GetRequiredService<ReauthenticationDialogViewModel>();
            var dialog = new ReauthenticationDialog
            {
                DataContext = viewModel
            };

            AttachOwner(dialog);

            bool? confirmed = dialog.ShowDialog();
            return confirmed == true ? viewModel.Result : null;
        }

        private static void AttachOwner(Window dialog)
        {
            if (Application.Current?.Windows is null)
            {
                return;
            }

            var owner = Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.IsActive) ?? Application.Current.MainWindow;

            if (owner is not null && owner != dialog)
            {
                dialog.Owner = owner;
            }
        }
    }
}
