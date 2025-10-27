using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.ViewModels;
using YasGMP.Views.Dialogs;
using YasGMP;

namespace YasGMP.Services.Platform
{
    /// <summary>MAUI implementation of <see cref="IDialogService"/> using modal pages.</summary>
    public sealed class MauiDialogService : IDialogService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IUiDispatcher _dispatcher;
        /// <summary>
        /// Initializes a new instance of the MauiDialogService class.
        /// </summary>

        public MauiDialogService(IServiceProvider serviceProvider, IUiDispatcher dispatcher)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }
        /// <summary>
        /// Executes the show alert async operation.
        /// </summary>

        public Task ShowAlertAsync(string title, string message, string cancel)
        {
            return _dispatcher.InvokeAsync(async () =>
            {
                var page = Application.Current?.MainPage;
                if (page != null)
                    await page.DisplayAlert(title, message, cancel);
            });
        }
        /// <summary>
        /// Executes the show confirmation async operation.
        /// </summary>

        public async Task<bool> ShowConfirmationAsync(string title, string message, string accept, string cancel)
        {
            return await _dispatcher.InvokeAsync(async () =>
            {
                var page = Application.Current?.MainPage;
                if (page == null)
                    return false;

                return await page.DisplayAlert(title, message, accept, cancel);
            }).ConfigureAwait(false);
        }
        /// <summary>
        /// Executes the show action sheet async operation.
        /// </summary>

        public Task<string?> ShowActionSheetAsync(string title, string cancel, string? destruction, params string[] buttons)
        {
            return _dispatcher.InvokeAsync(async () =>
            {
                var page = Application.Current?.MainPage;
                if (page == null)
                    return null;

                return await page.DisplayActionSheet(title, cancel, destruction, buttons);
            });
        }
        /// <summary>
        /// Executes the show dialog async operation.
        /// </summary>

        public Task<T?> ShowDialogAsync<T>(string dialogId, object? parameter = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(dialogId))
                throw new ArgumentException("Dialog identifier must be provided.", nameof(dialogId));

            return dialogId switch
            {
                DialogIds.CapaEdit        => ShowCapaDialogAsync<T>(parameter, cancellationToken),
                DialogIds.CalibrationEdit => ShowCalibrationDialogAsync<T>(parameter, cancellationToken),
                DialogIds.UserEdit        => ShowUserEditDialogAsync<T>(parameter, cancellationToken),
                _ => throw new NotSupportedException($"Dialog '{dialogId}' is not registered.")
            };
        }

        private Task<T?> ShowUserEditDialogAsync<T>(object? parameter, CancellationToken cancellationToken)
        {
            if (typeof(T) != typeof(UserEditDialogResult))
                throw new InvalidOperationException($"Dialog '{DialogIds.UserEdit}' expects result type '{typeof(UserEditDialogResult).FullName}'.");

            return (Task<T?>)(object)ShowUserEditDialogAsyncCore(parameter, cancellationToken);
        }

        private async Task<UserEditDialogResult?> ShowUserEditDialogAsyncCore(object? parameter, CancellationToken cancellationToken)
        {
            if (parameter is not UserEditDialogRequest request)
                throw new ArgumentException($"Dialog parameter must be of type {nameof(UserEditDialogRequest)}.", nameof(parameter));

            var viewModel = _serviceProvider.GetRequiredService<UserEditDialogViewModel>();
            viewModel.Initialize(request.User, request.Roles, request.ImpersonationCandidates);

            var dialogPage = new UserEditDialog(viewModel);
            return await PresentUserEditDialogAsync(dialogPage, cancellationToken).ConfigureAwait(false);
        }

        private async Task<UserEditDialogResult?> PresentUserEditDialogAsync(UserEditDialog dialogPage, CancellationToken cancellationToken)
        {
            var completion = new TaskCompletionSource<UserEditDialogResult?>(TaskCreationOptions.RunContinuationsAsynchronously);

            _ = dialogPage.Result.ContinueWith(t =>
            {
                if (t.IsCanceled)
                {
                    completion.TrySetCanceled();
                }
                else if (t.IsFaulted)
                {
                    completion.TrySetException(t.Exception!.InnerExceptions);
                }
                else
                {
                    completion.TrySetResult(t.Result);
                }
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

            await _dispatcher.InvokeAsync(async () =>
            {
                await Application.Current!.MainPage!.Navigation.PushModalAsync(dialogPage);
            }).ConfigureAwait(false);

            using var registration = cancellationToken.Register(() =>
            {
                completion.TrySetCanceled(cancellationToken);
                _ = _dispatcher.InvokeAsync(async () =>
                {
                    try
                    {
                        var navigation = Application.Current?.MainPage?.Navigation;
                        if (navigation is null)
                            return;

                        for (var i = navigation.ModalStack.Count - 1; i >= 0; i--)
                        {
                            if (ReferenceEquals(navigation.ModalStack[i], dialogPage))
                            {
                                await navigation.PopModalAsync();
                                break;
                            }
                        }
                    }
                    catch
                    {
                        // Swallow navigation failures triggered during cancellation.
                    }
                });
            });

            return await completion.Task.ConfigureAwait(false);
        }

        private async Task<T?> ShowCapaDialogAsync<T>(object? parameter, CancellationToken cancellationToken)
        {
            if (!typeof(T).IsAssignableFrom(typeof(CapaCase)))
                throw new InvalidOperationException("CAPA dialog expects result type CapaCase.");

            var request = parameter as CapaDialogRequest ?? new CapaDialogRequest(parameter as CapaCase);
            var userSession = _serviceProvider.GetRequiredService<IUserSession>();

            var vm = new CapaEditDialogViewModel(request.CapaCase, userSession, this);
            var dialogPage = new CapaEditDialog(vm);

            return await PresentModalAsync(dialogPage, () =>
            {
                if (vm.DialogResult == true)
                    return (T?)(object)vm.CapaCase;
                return default;
            }, cancellationToken).ConfigureAwait(false);
        }

        private async Task<T?> ShowCalibrationDialogAsync<T>(object? parameter, CancellationToken cancellationToken)
        {
            if (!typeof(T).IsAssignableFrom(typeof(Calibration)))
                throw new InvalidOperationException("Calibration dialog expects result type Calibration.");

            var request = parameter as CalibrationDialogRequest
                ?? throw new ArgumentException("CalibrationDialogRequest payload is required.", nameof(parameter));

            var userSession = _serviceProvider.GetRequiredService<IUserSession>();
            var platform = _serviceProvider.GetRequiredService<IPlatformService>();

            var vm = new CalibrationEditDialogViewModel(
                request.Calibration,
                new List<MachineComponent>(request.Components),
                new List<Supplier>(request.Suppliers),
                userSession,
                this,
                platform);

            var dialogPage = new CalibrationEditDialog(vm);
            return await PresentCalibrationAsync<T>(dialogPage, vm, cancellationToken).ConfigureAwait(false);
        }

        private async Task<T?> PresentCalibrationAsync<T>(Page page, CalibrationEditDialogViewModel vm, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<T?>();

            void OnResult(bool saved, Calibration? calibration)
            {
                vm.DialogResult -= OnResult;
                if (saved && calibration is not null)
                {
                    if (typeof(T).IsAssignableFrom(typeof(Calibration)))
                        tcs.TrySetResult((T?)(object)calibration);
                    else
                        tcs.TrySetResult(default);
                }
                else
                {
                    tcs.TrySetResult(default);
                }
            }

            vm.DialogResult += OnResult;

            void OnDisappearing(object? sender, EventArgs e)
            {
                page.Disappearing -= OnDisappearing;
                vm.DialogResult -= OnResult;
                if (!tcs.Task.IsCompleted)
                    tcs.TrySetResult(default);
            }

            page.Disappearing += OnDisappearing;

            await _dispatcher.InvokeAsync(async () =>
            {
                await Application.Current!.MainPage!.Navigation.PushModalAsync(page);
            }).ConfigureAwait(false);

            using (cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken)))
            {
                return await tcs.Task.ConfigureAwait(false);
            }
        }

        private async Task<T?> PresentModalAsync<T>(Page page, Func<T?> resultAccessor, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<T?>();

            void OnDisappearing(object? sender, EventArgs e)
            {
                page.Disappearing -= OnDisappearing;
                try
                {
                    tcs.TrySetResult(resultAccessor());
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }

            page.Disappearing += OnDisappearing;

            await _dispatcher.InvokeAsync(async () =>
            {
                await Application.Current!.MainPage!.Navigation.PushModalAsync(page);
            }).ConfigureAwait(false);

            using (cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken)))
            {
                return await tcs.Task.ConfigureAwait(false);
            }
        }
    }
}
