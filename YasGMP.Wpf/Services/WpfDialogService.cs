using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using YasGMP.Services;
using YasGMP.Wpf.Dialogs;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Services
{
    /// <summary>
    /// Bridges MAUI's <see cref="IDialogService"/> abstractions to simple WPF <see cref="MessageBox"/>
    /// prompts so shell modules retain baseline confirmation/alert parity when hosted on desktop.
    /// </summary>
    /// <remarks>
    /// <para><strong>Supported MAUI APIs:</strong> <see cref="ShowAlertAsync"/> and
    /// <see cref="ShowConfirmationAsync"/> translate directly to information/question message boxes and
    /// therefore keep existing MAUI flows working without code changes.</para>
    /// <para><strong>Unsupported APIs:</strong> <see cref="ShowActionSheetAsync"/> currently has no WPF shell
    /// equivalent. <see cref="ShowDialogAsync{T}(string, object?, CancellationToken)"/> supports
    /// <see cref="DialogIds.UserEdit"/> while additional dialogs await dedicated desktop UX.
    /// Callers migrating from MAUI should replace action sheets with ribbon commands, docked panes, or
    /// module views, and document TODOs for dialogs not yet implemented.</para>
    /// <para><strong>Localization:</strong> the service does not look up resources on its own; callers
    /// are responsible for passing fully localized titles/buttons sourced from the shared localization
    /// dictionaries (RESX, <c>ShellStrings</c>, etc.) so WPF mirrors MAUI translations.</para>
    /// <para><strong>Migration checklist:</strong> audit MAUI usages of <see cref="IDialogService"/> for
    /// custom buttons or complex payloads, guard unsupported calls with platform checks, and document
    /// TODOs to avoid silent regressions during the MAUI → WPF transition.</para>
    /// </remarks>
    public sealed class WpfDialogService : IDialogService
    {
        private readonly Func<UserEditDialogViewModel> _userEditDialogFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="WpfDialogService"/> class.
        /// </summary>
        /// <param name="userEditDialogFactory">Factory that resolves a fresh dialog view-model per invocation.</param>
        public WpfDialogService(Func<UserEditDialogViewModel> userEditDialogFactory)
        {
            _userEditDialogFactory = userEditDialogFactory ?? throw new ArgumentNullException(nameof(userEditDialogFactory));
        }

        /// <summary>
        /// Executes the show alert async operation.
        /// </summary>
        public Task ShowAlertAsync(string title, string message, string cancel)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            return Task.CompletedTask;
        }
        /// <summary>
        /// Executes the show confirmation async operation.
        /// </summary>

        public Task<bool> ShowConfirmationAsync(string title, string message, string accept, string cancel)
        {
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return Task.FromResult(result == MessageBoxResult.Yes);
        }
        /// <summary>
        /// Executes the show action sheet async operation.
        /// </summary>

        public Task<string?> ShowActionSheetAsync(string title, string cancel, string? destruction, params string[] buttons)
        {
            // Not supported in the WPF shell; return null.
            return Task.FromResult<string?>(null);
        }
        /// <summary>
        /// Executes the show dialog async operation.
        /// </summary>

        public Task<T?> ShowDialogAsync<T>(string dialogId, object? parameter = null, CancellationToken cancellationToken = default)
        {
            return dialogId switch
            {
                DialogIds.UserEdit => ShowUserEditDialogAsync<T>(parameter, cancellationToken),
                _ => throw new NotSupportedException($"Dialog '{dialogId}' is not available in the WPF shell."),
            };
        }

        private async Task<T?> ShowUserEditDialogAsync<T>(object? parameter, CancellationToken cancellationToken)
        {
            if (typeof(T) != typeof(UserEditDialogViewModel.UserEditDialogResult))
            {
                throw new InvalidOperationException($"Dialog '{DialogIds.UserEdit}' expects result type '{typeof(UserEditDialogViewModel.UserEditDialogResult).FullName}'.");
            }

            if (parameter is not UserEditDialogRequest request)
            {
                throw new ArgumentException($"Dialog parameter must be of type {nameof(UserEditDialogRequest)}.", nameof(parameter));
            }

            var viewModel = _userEditDialogFactory();
            await viewModel.InitializeAsync(request, cancellationToken).ConfigureAwait(false);

            var result = await ShowUserEditDialogAsyncCore(viewModel, cancellationToken).ConfigureAwait(false);
            return (T?)(object?)result;
        }

        private Task<UserEditDialogViewModel.UserEditDialogResult?> ShowUserEditDialogAsyncCore(
            UserEditDialogViewModel viewModel,
            CancellationToken cancellationToken)
        {
            if (Application.Current?.Dispatcher is null)
            {
                throw new InvalidOperationException("The WPF application dispatcher is not available.");
            }

            var completionSource = new TaskCompletionSource<UserEditDialogViewModel.UserEditDialogResult?>(TaskCreationOptions.RunContinuationsAsynchronously);

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (completionSource.Task.IsCompleted)
                {
                    return;
                }

                var dialog = new UserEditDialogWindow
                {
                    DataContext = viewModel,
                };

                AttachOwner(dialog);

                var cancellationRegistration = default(CancellationTokenRegistration);
                var handlersDetached = false;

                void DetachHandlers()
                {
                    if (handlersDetached)
                    {
                        return;
                    }

                    handlersDetached = true;
                    viewModel.RequestClose -= OnRequestClose;
                    dialog.Closed -= OnDialogClosed;
                    cancellationRegistration.Dispose();
                }

                void OnDialogClosed(object? sender, EventArgs e)
                {
                    DetachHandlers();
                    completionSource.TrySetResult(viewModel.Result);
                }

                void OnRequestClose(object? sender, bool _)
                {
                    viewModel.RequestClose -= OnRequestClose;
                    if (dialog.IsVisible)
                    {
                        dialog.Close();
                    }
                    else
                    {
                        DetachHandlers();
                        completionSource.TrySetResult(viewModel.Result);
                    }
                }

                viewModel.RequestClose += OnRequestClose;
                dialog.Closed += OnDialogClosed;

                if (cancellationToken.CanBeCanceled)
                {
                    cancellationRegistration = cancellationToken.Register(() =>
                    {
                        dialog.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            if (dialog.IsVisible)
                            {
                                dialog.Close();
                            }
                        }));
                    });
                }

                dialog.ShowDialog();
            }));

            return completionSource.Task;
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
