using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.ViewModels.Dialogs;
using YasGMP.Wpf.Views.Dialogs;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Concrete implementation that displays the WPF electronic signature dialog and exposes
/// persistence of the captured signature as an explicit follow-up step.
/// </summary>
public sealed class ElectronicSignatureDialogService : IElectronicSignatureDialogService
{
    private readonly IUiDispatcher _uiDispatcher;
    private readonly DatabaseService _databaseService;
    private readonly IAuthContext _authContext;
    private readonly AuditService _auditService;
    private readonly Func<ElectronicSignatureDialogViewModel, bool?> _showDialog;

    public ElectronicSignatureDialogService(
        IUiDispatcher uiDispatcher,
        DatabaseService databaseService,
        IAuthContext authContext,
        AuditService auditService,
        Func<ElectronicSignatureDialogViewModel, bool?>? dialogInvoker = null)
    {
        _uiDispatcher = uiDispatcher ?? throw new ArgumentNullException(nameof(uiDispatcher));
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _showDialog = dialogInvoker ?? ShowDialog;
    }

    public async Task<ElectronicSignatureDialogResult?> CaptureSignatureAsync(
        ElectronicSignatureContext context,
        CancellationToken cancellationToken = default)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        cancellationToken.ThrowIfCancellationRequested();

        var viewModel = new ElectronicSignatureDialogViewModel(_authContext, context);
        ElectronicSignatureDialogResult? result = null;

        await _uiDispatcher.InvokeAsync(() =>
        {
            bool? confirmed = _showDialog(viewModel);
            if (confirmed == true)
            {
                result = viewModel.Result;
            }
        }).ConfigureAwait(false);

        if (result is not null)
        {
            await _auditService.LogSystemEventAsync(
                "SIGNATURE_CAPTURE_CONFIRMED",
                BuildCaptureAuditDetails(context, result),
                context.TableName,
                context.RecordId).ConfigureAwait(false);
        }

        return result;
    }

    public async Task PersistSignatureAsync(
        ElectronicSignatureDialogResult result,
        CancellationToken cancellationToken = default)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        if (result.Signature is null)
        {
            throw new ArgumentException("Signature result does not contain a digital signature.", nameof(result));
        }

        cancellationToken.ThrowIfCancellationRequested();
        await _databaseService.InsertDigitalSignatureAsync(result.Signature, cancellationToken).ConfigureAwait(false);

        await _auditService.LogSystemEventAsync(
            "SIGNATURE_PERSISTED",
            BuildPersistAuditDetails(result),
            result.Signature.TableName,
            result.Signature.RecordId).ConfigureAwait(false);
    }

    private bool? ShowDialog(ElectronicSignatureDialogViewModel viewModel)
    {
        var dialog = new ElectronicSignatureDialog(viewModel);
        var owner = Application.Current?.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.IsActive) ?? Application.Current?.MainWindow;
        if (owner is not null)
        {
            dialog.Owner = owner;
        }

        return dialog.ShowDialog();
    }

    private string BuildCaptureAuditDetails(ElectronicSignatureContext context, ElectronicSignatureDialogResult result)
    {
        string reasonDetail = string.IsNullOrWhiteSpace(result.ReasonDetail) ? "-" : result.ReasonDetail;
        string method = result.Signature?.Method ?? context.Method;
        string status = result.Signature?.Status ?? context.Status;
        string display = string.IsNullOrWhiteSpace(result.ReasonDisplay) ? result.ReasonCode : result.ReasonDisplay;

        return $"reason={result.ReasonCode}; display={display}; detail={reasonDetail}; method={method}; status={status}; session={_authContext.CurrentSessionId}";
    }

    private string BuildPersistAuditDetails(ElectronicSignatureDialogResult result)
    {
        var signature = result.Signature;
        string hash = string.IsNullOrWhiteSpace(signature.SignatureHash) ? "-" : signature.SignatureHash;
        string note = string.IsNullOrWhiteSpace(signature.Note) ? "-" : signature.Note;
        string method = string.IsNullOrWhiteSpace(signature.Method) ? "-" : signature.Method;
        string status = string.IsNullOrWhiteSpace(signature.Status) ? "-" : signature.Status;

        return $"reason={result.ReasonCode}; detail={result.ReasonDetail ?? "-"}; method={method}; status={status}; hash={hash}; note={note}; session={signature.SessionId ?? _authContext.CurrentSessionId}";
    }
}
