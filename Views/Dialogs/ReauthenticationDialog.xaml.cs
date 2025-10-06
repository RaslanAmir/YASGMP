using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Controls;
using YasGMP.Common;
using YasGMP.Models.DTO;

namespace YasGMP.Views.Dialogs
{
    /// <summary>
    /// Collects username/password/MFA and a mandatory GMP reason code before a work-order signature is persisted.
    /// </summary>
    public partial class ReauthenticationDialog : ContentPage
    {
        private readonly TaskCompletionSource<ReauthenticationResult?> _tcs = new();

        public Task<ReauthenticationResult?> Result => _tcs.Task;

        public ReauthenticationDialog(string? defaultUsername = null)
        {
            InitializeComponent();
            UsernameEntry.Text = defaultUsername ?? string.Empty;
            ReasonPicker.ItemsSource = WorkOrderSignatureReasonCodes.All.ToList();
        }

        private void OnReasonChanged(object? sender, EventArgs e)
        {
            if (ReasonPicker.SelectedItem is WorkOrderSignatureReasonCodes.Reason reason)
            {
                CustomReasonEntry.IsVisible = string.Equals(reason.Code, WorkOrderSignatureReasonCodes.Custom, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                CustomReasonEntry.IsVisible = false;
            }
        }

        private async void OnConfirmClicked(object? sender, EventArgs e)
        {
            if (_tcs.Task.IsCompleted)
            {
                return;
            }

            string username = UsernameEntry.Text?.Trim() ?? string.Empty;
            string password = PasswordEntry.Text ?? string.Empty;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                await DisplayAlert("Potvrda", "Korisničko ime i lozinka su obavezni za potpis.", "OK");
                return;
            }

            if (ReasonPicker.SelectedItem is not WorkOrderSignatureReasonCodes.Reason selectedReason)
            {
                await DisplayAlert("Potpis", "Molimo odaberite razlog potpisa.", "OK");
                return;
            }

            string reasonCode = selectedReason.Code;
            if (string.Equals(reasonCode, WorkOrderSignatureReasonCodes.Custom, StringComparison.OrdinalIgnoreCase))
            {
                reasonCode = CustomReasonEntry.Text?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(reasonCode))
                {
                    await DisplayAlert("Potpis", "Custom reason code je obavezan.", "OK");
                    return;
                }
            }

            string? reasonDetail = ReasonDetailEditor.Text?.Trim();
            string? mfa = string.IsNullOrWhiteSpace(MfaEntry.Text) ? null : MfaEntry.Text.Trim();

            _tcs.TrySetResult(new ReauthenticationResult(
                username,
                password,
                mfa,
                reasonCode,
                reasonDetail,
                selectedReason.DisplayLabel));

            await Navigation.PopModalAsync();
        }

        private async void OnCancelClicked(object? sender, EventArgs e)
        {
            if (!_tcs.Task.IsCompleted)
            {
                _tcs.TrySetResult(null);
            }
            await Navigation.PopModalAsync();
        }

        protected override void OnDisappearing()
        {
            if (!_tcs.Task.IsCompleted)
            {
                _tcs.TrySetResult(null);
            }

            base.OnDisappearing();
        }
    }
}

