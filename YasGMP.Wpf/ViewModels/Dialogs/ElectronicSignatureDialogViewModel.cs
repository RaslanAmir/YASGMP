using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Common;
using YasGMP.Helpers;
using YasGMP.Models;
using YasGMP.Services.Interfaces;
using YasGMP.ViewModels;

namespace YasGMP.Wpf.ViewModels.Dialogs;

/// <summary>
/// View-model backing the WPF electronic signature dialog. Collects the operator credential
/// and GMP reason metadata, forwards persistence through the shared DigitalSignatureViewModel,
/// and exposes a result describing the captured signature.
/// </summary>
public sealed partial class ElectronicSignatureDialogViewModel : ObservableObject
{
    private readonly DigitalSignatureViewModel _digitalSignatureViewModel;
    private readonly IAuthContext _authContext;

    public ElectronicSignatureDialogViewModel(
        DigitalSignatureViewModel digitalSignatureViewModel,
        IAuthContext authContext,
        ElectronicSignatureContext context)
    {
        _digitalSignatureViewModel = digitalSignatureViewModel ?? throw new ArgumentNullException(nameof(digitalSignatureViewModel));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        Context = context ?? throw new ArgumentNullException(nameof(context));

        Reasons = new ObservableCollection<WorkOrderSignatureReasonCodes.Reason>(WorkOrderSignatureReasonCodes.All);
        SelectedReason = Reasons.Count > 0 ? Reasons[0] : null;

        ConfirmCommand = new AsyncRelayCommand(ConfirmAsync, CanConfirm);
        CancelCommand = new RelayCommand(Cancel);

        CurrentUserDisplay = BuildCurrentUserDisplay();
    }

    /// <summary>Context describing the target record and default signature attributes.</summary>
    public ElectronicSignatureContext Context { get; }

    /// <summary>Available GMP reason codes for signature justification.</summary>
    public ObservableCollection<WorkOrderSignatureReasonCodes.Reason> Reasons { get; }

    /// <summary>Display text that surfaces the active session user.</summary>
    public string CurrentUserDisplay { get; }

    /// <summary>Captured result populated once confirmation succeeds.</summary>
    public ElectronicSignatureDialogResult? Result { get; private set; }

    /// <summary>Command executed when the operator confirms the signature.</summary>
    public IAsyncRelayCommand ConfirmCommand { get; }

    /// <summary>Command executed when the operator cancels the dialog.</summary>
    public IRelayCommand CancelCommand { get; }

    [ObservableProperty]
    private WorkOrderSignatureReasonCodes.Reason? _selectedReason;

    [ObservableProperty]
    private string? _customReasonCode;

    [ObservableProperty]
    private string? _reasonDetail;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isCustomReasonVisible;

    [ObservableProperty]
    private string? _statusMessage;

    /// <summary>Raised when the dialog should close. Payload indicates success (<c>true</c>) or cancel (<c>false</c>).</summary>
    public event EventHandler<bool>? RequestClose;

    partial void OnSelectedReasonChanged(WorkOrderSignatureReasonCodes.Reason? value)
    {
        IsCustomReasonVisible = value is not null &&
            string.Equals(value.Code, WorkOrderSignatureReasonCodes.Custom, StringComparison.OrdinalIgnoreCase);
        ConfirmCommand.NotifyCanExecuteChanged();
    }

    partial void OnPasswordChanged(string value) => ConfirmCommand.NotifyCanExecuteChanged();

    partial void OnIsBusyChanged(bool value) => ConfirmCommand.NotifyCanExecuteChanged();

    private bool CanConfirm()
        => !IsBusy && !string.IsNullOrWhiteSpace(Password) && SelectedReason is not null;

    private async Task ConfirmAsync()
    {
        if (SelectedReason is null)
        {
            StatusMessage = "Select a signature reason.";
            return;
        }

        string reasonCode = SelectedReason.Code;
        if (IsCustomReasonVisible)
        {
            reasonCode = CustomReasonCode?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(reasonCode))
            {
                StatusMessage = "Custom reason code is required.";
                return;
            }
        }

        string? reasonDetail = string.IsNullOrWhiteSpace(ReasonDetail) ? null : ReasonDetail!.Trim();
        var user = _authContext.CurrentUser;
        if (user is null || user.Id == 0)
        {
            StatusMessage = "Active user session is required.";
            return;
        }

        IsBusy = true;
        StatusMessage = null;

        try
        {
            var timestampUtc = DateTime.UtcNow;
            var signatureContext = DigitalSignatureHelper.ComputeUserContextSignature(
                user.Id,
                _authContext.CurrentSessionId,
                _authContext.CurrentDeviceInfo,
                timestampUtc);

            var signature = new DigitalSignature
            {
                TableName = Context.TableName,
                RecordId = Context.RecordId,
                UserId = user.Id,
                SignatureHash = signatureContext.Hash,
                Method = Context.Method,
                Status = Context.Status,
                SignedAt = timestampUtc,
                DeviceInfo = _authContext.CurrentDeviceInfo,
                IpAddress = _authContext.CurrentIpAddress,
                SessionId = _authContext.CurrentSessionId,
                Note = ComposeNote(reasonCode, reasonDetail)
            };

            await _digitalSignatureViewModel.InsertSignatureAsync(signature, CancellationToken.None).ConfigureAwait(false);

            Result = new ElectronicSignatureDialogResult(
                Password,
                reasonCode,
                reasonDetail,
                SelectedReason.DisplayLabel,
                signature);

            RequestClose?.Invoke(this, true);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to capture signature: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string ComposeNote(string reasonCode, string? reasonDetail)
        => string.IsNullOrWhiteSpace(reasonDetail)
            ? reasonCode
            : $"{reasonCode}: {reasonDetail}";

    private void Cancel()
    {
        Result = null;
        RequestClose?.Invoke(this, false);
    }

    private string BuildCurrentUserDisplay()
    {
        var user = _authContext.CurrentUser;
        if (user is null)
        {
            return "User: (not authenticated)";
        }

        string display = string.IsNullOrWhiteSpace(user.FullName) ? user.Username ?? "unknown" : user.FullName;
        return $"User: {display}";
    }
}

/// <summary>Describes the record and metadata that should be associated with the captured signature.</summary>
/// <param name="TableName">Target table name persisted with the signature.</param>
/// <param name="RecordId">Record identifier associated with the signature.</param>
/// <param name="Method">Signature method (defaults to "password").</param>
/// <param name="Status">Signature status (defaults to "valid").</param>
public sealed record ElectronicSignatureContext(string TableName, int RecordId, string Method = "password", string Status = "valid");

/// <summary>Result returned from the dialog once the signature capture succeeds.</summary>
/// <param name="Password">Credential supplied by the operator.</param>
/// <param name="ReasonCode">Normalized reason code (or custom input).</param>
/// <param name="ReasonDetail">Free-form justification text.</param>
/// <param name="ReasonDisplay">Human readable label of the selected reason.</param>
/// <param name="Signature">Inserted digital signature row.</param>
public sealed record ElectronicSignatureDialogResult(
    string Password,
    string ReasonCode,
    string? ReasonDetail,
    string ReasonDisplay,
    DigitalSignature Signature);
