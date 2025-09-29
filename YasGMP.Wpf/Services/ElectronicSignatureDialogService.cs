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

    public ElectronicSignatureDialogService(
        IUiDispatcher uiDispatcher,
        DatabaseService databaseService,
        IAuthContext authContext)
    {
        _uiDispatcher = uiDispatcher ?? throw new ArgumentNullException(nameof(uiDispatcher));
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
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
            var dialog = new ElectronicSignatureDialog(viewModel);
            var owner = Application.Current?.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.IsActive) ?? Application.Current?.MainWindow;
            if (owner is not null)
            {
                dialog.Owner = owner;
            }

            bool? confirmed = dialog.ShowDialog();
            if (confirmed == true)
            {
                result = viewModel.Result;
            }
        }).ConfigureAwait(false);

        return result;
    }

    public Task PersistSignatureAsync(
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
        return _databaseService.InsertDigitalSignatureAsync(result.Signature, cancellationToken);
    }
}
