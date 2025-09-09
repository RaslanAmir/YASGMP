using System;
using Microsoft.Maui.Controls;
using YasGMP.Models;
using YasGMP.ViewModels;

namespace YasGMP.Views
{
    /// <summary>
    /// <b>WorkOrderEditDialog</b> – YASTECH-themed modal ContentPage for creating or editing
    /// a <see cref="WorkOrder"/> under GMP/CSV requirements. The page is fully data-bound to
    /// an instance of <see cref="WorkOrderEditDialogViewModel"/> that exposes the target
    /// <see cref="WorkOrder"/> instance, validation, and <c>SaveCommand</c>/<c>CancelCommand</c>.
    /// <para>
    /// Consumers should open the dialog as a modal page, and subscribe to <see cref="DialogResult"/>
    /// for a strongly-typed outcome and (optionally) the final <see cref="WorkOrder"/> snapshot.
    /// </para>
    /// <example>
    /// <code>
    /// var vm = serviceProvider.GetRequiredService&lt;WorkOrderEditDialogViewModel&gt;();
    /// var dlg = new WorkOrderEditDialog(vm);
    /// dlg.DialogResult += (saved, wo) => { /* handle result */ };
    /// await Navigation.PushModalAsync(dlg);
    /// </code>
    /// </example>
    /// </summary>
    public partial class WorkOrderEditDialog : ContentPage
    {
        /// <summary>
        /// Event raised when the dialog intends to close.
        /// <list type="bullet">
        /// <item><term><c>true</c></term>: The user confirmed/save.</item>
        /// <item><term><c>false</c></term>: The user cancelled.</item>
        /// </list>
        /// The <see cref="WorkOrder"/> argument may be <c>null</c> depending on the ViewModel semantics.
        /// </summary>
        public event Action<bool, WorkOrder?>? DialogResult;

        /// <summary>
        /// Initializes a new instance of the dialog and wires up the ViewModel result callback.
        /// </summary>
        /// <param name="viewModel">
        /// An initialized <see cref="WorkOrderEditDialogViewModel"/> that exposes the editable
        /// <see cref="WorkOrder"/> and the <c>SaveCommand</c>/<c>CancelCommand</c>. This instance
        /// is assigned to <see cref="BindableObject.BindingContext"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="viewModel"/> is <c>null</c>.</exception>
        public WorkOrderEditDialog(WorkOrderEditDialogViewModel viewModel)
        {
            InitializeComponent();

            if (viewModel is null)
                throw new ArgumentNullException(nameof(viewModel), "BindingContext za WorkOrderEditDialog ne smije biti null.");

            BindingContext = viewModel;
            viewModel.DialogResult += OnDialogResultFromViewModel;
        }

        /// <summary>
        /// Handles the ViewModel's dialog-close event, relays the outcome to subscribers,
        /// and closes the modal page via Navigation.PopModalAsync.
        /// </summary>
        /// <param name="result"><c>true</c> for save/confirm, <c>false</c> for cancel.</param>
        /// <param name="order">The final <see cref="WorkOrder"/> (may be <c>null</c>).</param>
        private async void OnDialogResultFromViewModel(bool result, WorkOrder? order)
        {
            try
            {
                DialogResult?.Invoke(result, order);
            }
            finally
            {
                // Always close the modal, even if a subscriber throws.
                // Executed on UI thread by MAUI event pipeline.
                await Navigation.PopModalAsync();
            }
        }

        /// <summary>
        /// Unsubscribes from the ViewModel event to prevent handler leaks/double invocation
        /// if the page is reused by navigation stacks or hot reload.
        /// </summary>
        protected override void OnDisappearing()
        {
            if (BindingContext is WorkOrderEditDialogViewModel vm)
            {
                vm.DialogResult -= OnDialogResultFromViewModel;
            }

            base.OnDisappearing();
        }
    }
}
