using System;
using YasGMP.Services;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.ViewModels.Dialogs;

/// <summary>
/// Payload supplied to <see cref="IDialogService"/> when opening the user editor dialog.
/// </summary>
public sealed class UserEditDialogRequest
{
    /// <summary>Initializes a new instance of the <see cref="UserEditDialogRequest"/> class.</summary>
    /// <param name="mode">Form mode that triggered the dialog.</param>
    /// <param name="viewModel">Prepared <see cref="UserEditDialogViewModel"/> instance.</param>
    public UserEditDialogRequest(FormMode mode, UserEditDialogViewModel viewModel)
    {
        Mode = mode;
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    }

    /// <summary>Gets the form mode that triggered the dialog.</summary>
    public FormMode Mode { get; }

    /// <summary>Gets the prepared dialog view-model.</summary>
    public UserEditDialogViewModel ViewModel { get; }
}
