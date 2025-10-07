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
/// <remarks>
/// <para>
/// The service bridges shell commands to the shared infrastructure: <see cref="IUiDispatcher"/>
/// marshals the WPF dialog interactions back to the UI thread while mirroring the dispatcher usage
/// in the MAUI implementation, <see cref="DatabaseService"/> persists the digital signature through
/// the shared AppCore helpers so both shells store identical payloads, and <see cref="AuditService"/>
/// emits system events that feed the consolidated audit trail. This ensures that MAUI and WPF share
/// persistence, authentication, and auditing semantics even though the UI layer differs.
/// </para>
/// <para>
/// Callers may execute the service from any background thread as long as they supply a valid
/// dispatcher. The implementation guarantees that UI work only executes via
/// <see cref="IUiDispatcher.InvokeAsync(System.Func{System.Threading.Tasks.Task})"/> while database
/// and audit work stay on background threads. Consumers must not cache the view-model or dialog
/// instance between calls to avoid re-entrancy or duplicate audit writes.
/// </para>
/// <para>
/// Cancellation tokens are observed before each asynchronous hop and exceptions are allowed to
/// bubble so upstream save workflows can match MAUI's cancellation and error handling surface. Audit
/// helpers apply a "log once" policy to avoid duplicating persisted signature entries when retries
/// occur after the identifier has been assigned.
/// </para>
/// </remarks>
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

    /// <summary>
    /// Shows the signature dialog on the UI thread, returning capture metadata if the user confirms
    /// the prompt.
    /// </summary>
    /// <remarks>
    /// The dispatcher invocation mirrors MAUI's `MainThread.InvokeOnMainThreadAsync` usage, keeping
    /// validation, localization, and error reporting consistent across shells. Cancellation is
    /// checked both before and after the UI hop; any thrown exceptions are propagated so callers can
    /// surface errors in the same way as MAUI dialogs. A <c>SIGNATURE_CAPTURE_CONFIRMED</c> event is
    /// logged exactly once per acceptance.
    /// </remarks>
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

    /// <summary>
    /// Persists the provided capture payload and emits the associated audit trail entry.
    /// </summary>
    /// <remarks>
    /// Uses the shared <see cref="DatabaseService"/> helpers so MAUI and WPF persist signatures via
    /// the same SQL paths. Cancellation tokens are honored before persistence and audit logging so
    /// workflows can abort without partial writes. If a signature identifier already exists the
    /// method only logs the audit event, preventing duplicate <c>SIGNATURE_PERSISTED</c> rows.
    /// Exceptions from the database or audit service propagate to the caller to maintain parity with
    /// MAUI error semantics.
    /// </remarks>
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
        if (result.Signature.Id == 0)
        {
            int signatureId = await _databaseService
                .InsertDigitalSignatureAsync(result.Signature, cancellationToken)
                .ConfigureAwait(false);

            result.Signature.Id = signatureId;

            await LogPersistedSignatureAsync(result, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await LogPersistedSignatureAsync(result, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Writes the <c>SIGNATURE_PERSISTED</c> audit entry for a previously saved signature payload.
    /// </summary>
    /// <remarks>
    /// This method intentionally bypasses database writes so shared consumers that persist the
    /// signature elsewhere can still emit the audit entry without re-opening the dialog. It respects
    /// cancellation before invoking <see cref="AuditService"/> and forwards any exceptions, enabling
    /// MAUI and WPF callers to present consistent retry/rollback experiences.
    /// </remarks>
    public Task LogPersistedSignatureAsync(
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

        return LogSignaturePersistedAsync(result, cancellationToken);
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

    private Task LogSignaturePersistedAsync(ElectronicSignatureDialogResult result, CancellationToken cancellationToken)
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

        return _auditService.LogSystemEventAsync(
            "SIGNATURE_PERSISTED",
            BuildPersistAuditDetails(result),
            result.Signature.TableName,
            result.Signature.RecordId);
    }
}
