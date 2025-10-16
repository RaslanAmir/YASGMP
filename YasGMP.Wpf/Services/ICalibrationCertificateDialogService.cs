using System.Threading;
using System.Threading.Tasks;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Services;

/// <summary>Service contract for displaying the calibration certificate dialog.</summary>
public interface ICalibrationCertificateDialogService
{
    /// <summary>Shows the dialog and returns the captured certificate metadata when confirmed.</summary>
    Task<CalibrationCertificateDialogResult?> ShowAsync(
        CalibrationCertificateDialogRequest request,
        CancellationToken cancellationToken = default);
}
