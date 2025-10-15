using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Common;
using YasGMP.Models.DTO;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Dialogs
{
    /// <summary>
    /// View-model backing the WPF reauthentication dialog. Collects credentials and GMP reason codes
    /// when sensitive operations such as digital signatures require renewed user confirmation.
    /// </summary>
    public sealed partial class ReauthenticationDialogViewModel : ObservableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReauthenticationDialogViewModel"/> class.
        /// </summary>
        public ReauthenticationDialogViewModel(IUserSession userSession)
        {
            if (userSession is null)
            {
                throw new ArgumentNullException(nameof(userSession));
            }

            Username = userSession.Username ?? string.Empty;
            Reasons = new ObservableCollection<WorkOrderSignatureReasonCodes.Reason>(WorkOrderSignatureReasonCodes.All);
            SelectedReason = Reasons.Count > 0 ? Reasons[0] : null;

            ConfirmCommand = new RelayCommand(Confirm, CanConfirm);
            CancelCommand = new RelayCommand(Cancel);
        }

        /// <summary>Raised when the dialog should close. Payload indicates whether confirmation succeeded.</summary>
        public event EventHandler<bool>? RequestClose;

        /// <summary>Gets the command executed when the operator confirms the dialog.</summary>
        public IRelayCommand ConfirmCommand { get; }

        /// <summary>Gets the command executed when the operator cancels the dialog.</summary>
        public IRelayCommand CancelCommand { get; }

        /// <summary>Available GMP reason codes surfaced to the picker.</summary>
        public ObservableCollection<WorkOrderSignatureReasonCodes.Reason> Reasons { get; }

        /// <summary>Result emitted once confirmation succeeds; otherwise <c>null</c>.</summary>
        public ReauthenticationResult? Result { get; private set; }

        [ObservableProperty]
        private string _username = string.Empty;

        partial void OnUsernameChanged(string value) => ConfirmCommand.NotifyCanExecuteChanged();

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string? _mfaCode;

        [ObservableProperty]
        private WorkOrderSignatureReasonCodes.Reason? _selectedReason;

        [ObservableProperty]
        private string? _customReasonCode;

        [ObservableProperty]
        private string? _reasonDetail;

        [ObservableProperty]
        private string? _statusMessage;

        [ObservableProperty]
        private bool _isCustomReasonVisible;

        partial void OnSelectedReasonChanged(WorkOrderSignatureReasonCodes.Reason? value)
        {
            IsCustomReasonVisible = value is not null &&
                string.Equals(value.Code, WorkOrderSignatureReasonCodes.Custom, StringComparison.OrdinalIgnoreCase);
            ConfirmCommand.NotifyCanExecuteChanged();
        }

        partial void OnPasswordChanged(string value) => ConfirmCommand.NotifyCanExecuteChanged();

        private bool CanConfirm()
            => !string.IsNullOrWhiteSpace(Username)
               && !string.IsNullOrWhiteSpace(Password)
               && SelectedReason is not null;

        private void Confirm()
        {
            if (!CanConfirm())
            {
                StatusMessage = "Username, password, and reason are required.";
                return;
            }

            string reasonCode = SelectedReason!.Code;
            string? reasonDisplay = SelectedReason.DisplayLabel;

            if (IsCustomReasonVisible)
            {
                reasonCode = CustomReasonCode?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(reasonCode))
                {
                    StatusMessage = "Custom reason code is required.";
                    return;
                }

                reasonDisplay = string.IsNullOrWhiteSpace(SelectedReason.DisplayLabel)
                    ? reasonCode
                    : SelectedReason.DisplayLabel;
            }

            Result = new ReauthenticationResult(
                Username.Trim(),
                Password,
                string.IsNullOrWhiteSpace(MfaCode) ? null : MfaCode!.Trim(),
                reasonCode,
                string.IsNullOrWhiteSpace(ReasonDetail) ? null : ReasonDetail!.Trim(),
                reasonDisplay ?? reasonCode);

            StatusMessage = null;
            RequestClose?.Invoke(this, true);
        }

        private void Cancel()
        {
            Result = null;
            RequestClose?.Invoke(this, false);
        }

        /// <summary>Updates the password backing field from the view's password box.</summary>
        public void SetPassword(string value)
        {
            Password = value ?? string.Empty;
        }
    }
}
