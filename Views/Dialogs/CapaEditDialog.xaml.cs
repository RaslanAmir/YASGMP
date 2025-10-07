// ==============================================================================
//  File: CapaEditDialog.xaml.cs
//  Project: YasGMP
//  Summary:
//      Modal dialog for entering/editing a CAPA case.
//      Provides a parameterless ctor (XAML/Shell/Hot Reload) and overloads
//      for both ViewModel and CapaCase usage from CapaViewModel.
// ==============================================================================

using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using YasGMP.Models;
using YasGMP.ViewModels;

namespace YasGMP
{
    /// <summary>
    /// Represents the Capa Edit Dialog.
    /// </summary>
    public partial class CapaEditDialog : ContentPage
    {
        /// <summary>
        /// Parameterless constructor required by XAML compiler and Shell.
        /// Note: CapaEditDialogViewModel requires a CapaCase? parameter; for a new entry we pass null.
        /// </summary>
        public CapaEditDialog()
            : this(new CapaEditDialogViewModel(capaCase: null))
        {
        }

        /// <summary>
        /// Preferred constructor when the caller already created a ViewModel.
        /// </summary>
        /// <param name="viewModel">CAPA edit ViewModel.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="viewModel"/> is null.</exception>
        public CapaEditDialog(CapaEditDialogViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }

        /// <summary>
        /// Convenience overload: allow creating the dialog directly from a CapaCase model.
        /// </summary>
        /// <param name="capaCase">Existing CAPA case to edit (or a new instance for create).</param>
        public CapaEditDialog(CapaCase? capaCase)
            : this(new CapaEditDialogViewModel(capaCase))
        {
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            if (BindingContext is CapaEditDialogViewModel vm && vm.SaveCommand?.CanExecute(null) == true)
                vm.SaveCommand.Execute(null);

            await SafeCloseAsync();
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            if (BindingContext is CapaEditDialogViewModel vm && vm.CancelCommand?.CanExecute(null) == true)
                vm.CancelCommand.Execute(null);

            await SafeCloseAsync();
        }

        private async Task SafeCloseAsync()
        {
            try
            {
                if (Navigation.ModalStack.Count > 0)
                    await Navigation.PopModalAsync(animated: true);
            }
            catch
            {
                // Page may already be closed by VM callback—ignore
            }
        }
    }
}
