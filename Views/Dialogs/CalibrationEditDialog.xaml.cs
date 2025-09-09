// ==============================================================================
//  File: CalibrationEditDialog.xaml.cs
//  Project: YasGMP
//  Summary:
//      Modal dialog for entering/editing a Calibration record. Exposes a simple
//      OnSave callback and DI-friendly constructors used by callers.
// ==============================================================================

#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Maui.ApplicationModel; // MainThread
using Microsoft.Maui.Controls;
using YasGMP.Models;
using YasGMP.ViewModels;

namespace YasGMP.Views.Dialogs
{
    /// <summary>
    /// Modal dialog for adding/editing a <see cref="Calibration"/>.
    /// </summary>
    public partial class CalibrationEditDialog : ContentPage
    {
        /// <summary>
        /// Callback invoked when the user saves the dialog. Supplies the edited <see cref="Calibration"/>.
        /// </summary>
        public Action<Calibration>? OnSave { get; set; }

        private CalibrationEditDialogViewModel? _vm;
        private bool _isClosing;

        /// <summary>
        /// Parameterless constructor required for XAML/Shell/Hot Reload.
        /// </summary>
        public CalibrationEditDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// DI-friendly constructor. Provide an initialized ViewModel instance.
        /// </summary>
        public CalibrationEditDialog(CalibrationEditDialogViewModel viewModel) : this()
        {
            if (viewModel is null) throw new ArgumentNullException(nameof(viewModel));
            WireViewModel(viewModel);
            BindingContext = _vm = viewModel;
        }

        /// <summary>
        /// Creates a dialog pre-bound to a <see cref="CalibrationEditDialogViewModel"/>.
        /// </summary>
        /// <param name="calibration">Calibration to edit.</param>
        /// <param name="components">Lookup: machine components.</param>
        /// <param name="suppliers">Lookup: suppliers.</param>
        /// <exception cref="ArgumentNullException">Thrown for null arguments.</exception>
        public CalibrationEditDialog(
            Calibration calibration,
            List<MachineComponent> components,
            List<Supplier> suppliers) : this()
        {
            if (calibration is null) throw new ArgumentNullException(nameof(calibration));
            if (components is null) throw new ArgumentNullException(nameof(components));
            if (suppliers  is null) throw new ArgumentNullException(nameof(suppliers));

            var vm = new CalibrationEditDialogViewModel(calibration, components, suppliers);
            WireViewModel(vm);
            BindingContext = _vm = vm;
        }

        /// <summary>
        /// Ensure we unsubscribe to avoid leaks.
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (_vm is not null)
                _vm.DialogResult -= OnDialogResult; // match handler signature
        }

        private void WireViewModel(CalibrationEditDialogViewModel vm)
        {
            _vm = vm;
            // Bridge VM DialogResult to our simple OnSave callback + close.
            _vm.DialogResult += OnDialogResult; // subscribe async-void handler
        }

        /// <summary>
        /// Handles the VM result, invoking <see cref="OnSave"/> on the UI thread and closing once.
        /// NOTE: The VM event is an Action-like delegate, so we use async void here.
        /// </summary>
        private async void OnDialogResult(bool saved, Calibration? result)
        {
            try
            {
                if (saved && result is not null && OnSave is not null)
                {
                    // Invoke client callback on UI thread (safer for collection updates/bindings)
                    await MainThread.InvokeOnMainThreadAsync(() => OnSave?.Invoke(result));
                }
            }
            finally
            {
                await CloseOnceAsync();
            }
        }

        /// <summary>
        /// Ensures the modal is popped only once (guards against double event raises).
        /// </summary>
        private async System.Threading.Tasks.Task CloseOnceAsync()
        {
            if (_isClosing) return;
            _isClosing = true;

            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (Navigation?.ModalStack?.Count > 0)
                        await Navigation.PopModalAsync(animated: true);
                });
            }
            catch
            {
                // Never throw from closing logic.
            }
        }
    }
}
