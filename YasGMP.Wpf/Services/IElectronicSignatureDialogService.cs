using System.Threading;
using System.Threading.Tasks;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Abstraction used by save workflows to capture an electronic signature via the WPF dialog.
/// </summary>
public interface IElectronicSignatureDialogService
{
    /// <summary>
    /// Presents the electronic signature dialog and returns the captured metadata when confirmed.
    /// </summary>
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
    /// <param name="result">The capture result that was previously persisted.</param>
    /// <param name="cancellationToken">Token used to observe cancellation while logging.</param>
    Task LogPersistedSignatureAsync(
        ElectronicSignatureDialogResult result,
        CancellationToken cancellationToken = default);
}
