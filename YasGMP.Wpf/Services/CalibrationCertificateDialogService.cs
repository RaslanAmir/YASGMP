using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using YasGMP.Services;
using YasGMP.Wpf.ViewModels.Dialogs;
using YasGMP.Wpf.Views.Dialogs;

namespace YasGMP.Wpf.Services;

/// <summary>Default implementation that displays the WPF calibration certificate dialog.</summary>
public sealed class CalibrationCertificateDialogService : ICalibrationCertificateDialogService
{
    private readonly IUiDispatcher _dispatcher;
    private readonly IFilePicker _filePicker;
    private readonly Func<CalibrationCertificateDialogViewModel, bool?> _showDialog;

    /// <summary>Initializes a new instance of the <see cref="CalibrationCertificateDialogService"/> class.</summary>
    public CalibrationCertificateDialogService(
        IUiDispatcher dispatcher,
        IFilePicker filePicker,
        Func<CalibrationCertificateDialogViewModel, bool?>? dialogInvoker = null)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _showDialog = dialogInvoker ?? ShowDialog;
    }

    /// <inheritdoc />
    public async Task<CalibrationCertificateDialogResult?> ShowAsync(
        CalibrationCertificateDialogRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        cancellationToken.ThrowIfCancellationRequested();

        CalibrationCertificateDialogResult? result = null;

        await _dispatcher.InvokeAsync(() =>
        {
            var viewModel = new CalibrationCertificateDialogViewModel(request, _filePicker);
            bool? confirmed = _showDialog(viewModel);
            if (confirmed == true)
            {
                result = viewModel.Result;
            }
        }).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        return result;
    }

    private bool? ShowDialog(CalibrationCertificateDialogViewModel viewModel)
    {
        var dialog = new CalibrationCertificateDialog(viewModel);
        var owner = Application.Current?.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.IsActive) ?? Application.Current?.MainWindow;

        if (owner is not null)
        {
            dialog.Owner = owner;
        }

        return dialog.ShowDialog();
    }
}
