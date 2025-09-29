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
    /// <param name="cancellationToken">Token used to observe cancellation while persisting.</param>
    /// <returns>The captured signature metadata when confirmed; otherwise <c>null</c>.</returns>
    Task<ElectronicSignatureDialogResult?> CaptureSignatureAsync(
        ElectronicSignatureContext context,
        CancellationToken cancellationToken = default);
}
