using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using YasGMP.ViewModels;

namespace YasGMP.Views.Dialogs
{
    /// <summary>
    /// Modal dialog for editing users and initiating impersonation workflows. Mirrors the
    /// task-completion pattern used by existing MAUI dialogs so callers can await results.
    /// </summary>
    public partial class UserEditDialog : ContentPage
    {
        private readonly TaskCompletionSource<UserEditDialogResult?> _tcs = new();

        /// <summary>Initializes a new instance of the dialog with the supplied view model.</summary>
        /// <param name="viewModel">Prepared <see cref="UserEditDialogViewModel"/> instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="viewModel"/> is <c>null</c>.</exception>
        public UserEditDialog(UserEditDialogViewModel viewModel)
        {
            InitializeComponent();

            if (viewModel is null)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }

            BindingContext = viewModel;
            viewModel.DialogCompleted += OnDialogCompleted;
        }

        /// <summary>Awaitable result that completes when the dialog closes.</summary>
        public Task<UserEditDialogResult?> Result => _tcs.Task;

        private async void OnDialogCompleted(object? sender, UserEditDialogResult e)
        {
            if (!_tcs.Task.IsCompleted)
            {
                _tcs.TrySetResult(e);
            }

            // Attempt to close the modal; guard against navigation stack changes.
            if (Navigation?.NavigationStack?.Count > 0)
            {
                await Navigation.PopModalAsync();
            }
        }

        protected override void OnDisappearing()
        {
            if (BindingContext is UserEditDialogViewModel vm)
            {
                vm.DialogCompleted -= OnDialogCompleted;
            }

            if (!_tcs.Task.IsCompleted)
            {
                _tcs.TrySetResult(null);
            }

            base.OnDisappearing();
        }
    }
}
