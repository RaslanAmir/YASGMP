using System.Threading;
using System.Threading.Tasks;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Abstraction used by save workflows to capture an electronic signature via the WPF dialog.
/// </summary>
/// <remarks>
/// <para>
/// Implementations are invoked from the shared shell save pipeline (`B1FormDocumentViewModel` and
/// MAUI bridges) so the interface must remain stable for both the WPF host and the multi-platform
/// code paths. Consumers call into this service from shell entry points such as ribbon commands,
/// form mode transitions, and validation hooks where dispatcher affinity is already enforced.
/// </para>
/// <para>
/// All UI work must occur on the shell dispatcher (`IUiDispatcher`) because modules frequently
/// initiate captures from background continuations. Implementations should source localization for
/// prompts, reason pickers, and validation text through the shell localization service so MAUI and
/// WPF prompts remain synchronized.
/// </para>
/// <para>
/// Audit expectations mirror the MAUI dialog: a <c>SIGNATURE_CAPTURE_CONFIRMED</c> system event is
/// logged when capture is confirmed, <c>SIGNATURE_PERSISTED</c> is logged once the payload is saved,
/// and <c>SIGNATURE_PERSISTED</c> should only be raised once per persisted record to avoid duplicate
/// audit entries in downstream reports.
/// </para>
/// <para>
/// Cancellation tokens are observed prior to dispatcher marshalling, during persistence, and while
/// logging to provide parity with the MAUI implementation. Consumers should translate shell
/// cancellation back into their save status messaging so MAUI and WPF respond identically when users
/// abort the prompt or the operation is cancelled by the workflow.
/// </para>
/// </remarks>
public interface IElectronicSignatureDialogService
{
    /// <summary>
    /// Presents the electronic signature dialog and returns the captured metadata when confirmed.
    /// </summary>
    /// <remarks>
    /// The dialog must be shown on the UI dispatcher context and should surface localized prompts
    /// and reason pickers. Implementations are responsible for emitting a
    /// <c>SIGNATURE_CAPTURE_CONFIRMED</c> audit event when the dialog is accepted; cancellation or
    /// dismissal should not produce audit traffic. Exceptions should bubble so that caller error
    /// handling mirrors the MAUI dialog behavior.
    /// </remarks>
    /// <param name="context">Target record context for the signature.</param>
    /// <param name="cancellationToken">Token used to observe cancellation while the dialog is shown.</param>
    /// <returns>The captured signature metadata when confirmed; otherwise <c>null</c>.</returns>
    Task<ElectronicSignatureDialogResult?> CaptureSignatureAsync(
        ElectronicSignatureContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the supplied signature payload without re-opening the dialog.
    /// Callers may update <see cref="ElectronicSignatureDialogResult.Signature"/> (e.g. record id)
    /// before invoking this method.
    /// </summary>
    /// <remarks>
    /// Implementations should reuse the same persistence path as the MAUI dialog, including
    /// dispatcher marshalling when database APIs require the UI thread. A
    /// <c>SIGNATURE_PERSISTED</c> audit event must be logged the first time the signature is
    /// successfully written; subsequent retries with an existing identifier should skip re-inserting
    /// and only ensure the audit entry exists. Cancellation should be honored prior to executing any
    /// data access so callers can abort and surface consistent status messages.
    /// </remarks>
    /// <param name="result">The capture result that should be persisted.</param>
    /// <param name="cancellationToken">Token used to observe cancellation while persisting.</param>
    Task PersistSignatureAsync(
        ElectronicSignatureDialogResult result,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Emits the persisted signature audit event without touching the database.
    /// Callers must ensure the signature has already been saved and includes any
    /// updated identifiers prior to invoking this helper.
    /// </summary>
    /// <remarks>
    /// This helper is surfaced for shell workflows that persist signatures through different
    /// channels but still need parity audit records. Implementations should de-duplicate audit
    /// entries by inspecting the persisted identifier and only logging once per signature. Any
    /// exceptions should propagate to maintain MAUI/WPF parity, allowing callers to decide whether to
    /// retry, surface the failure, or mark the operation cancelled.
    /// </remarks>
    /// <param name="result">The capture result that was previously persisted.</param>
    /// <param name="cancellationToken">Token used to observe cancellation while logging.</param>
    Task LogPersistedSignatureAsync(
        ElectronicSignatureDialogResult result,
        CancellationToken cancellationToken = default);
}
