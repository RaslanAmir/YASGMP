using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Tests.TestDoubles;

public sealed class TestCalibrationCertificateDialogService : ICalibrationCertificateDialogService
{
    public List<CalibrationCertificateDialogRequest> Requests { get; } = new();

    public CalibrationCertificateDialogResult? ResultToReturn { get; set; }

    public Task<CalibrationCertificateDialogResult?> ShowAsync(
        CalibrationCertificateDialogRequest request,
        CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        return Task.FromResult(ResultToReturn);
    }
}
