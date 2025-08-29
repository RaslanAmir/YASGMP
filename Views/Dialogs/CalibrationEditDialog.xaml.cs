// ==============================================================================
//  File: CalibrationEditDialog.xaml.cs
//  Project: YasGMP
//  Summary:
//      Modal dialog for entering/editing a Calibration record. Exposes a simple
//      OnSave callback and a DI-friendly constructor used by callers.
// ==============================================================================

using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Parameterless constructor required for XAML/Shell/Hot Reload.
        /// </summary>
        public CalibrationEditDialog()
        {
            InitializeComponent();
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

            // Bridge VM DialogResult to our simple OnSave callback + close.
            vm.DialogResult += async (saved, result) =>
            {
                if (saved && result is not null)
                    OnSave?.Invoke(result);

                await Navigation.PopModalAsync(animated: true);
            };

            BindingContext = vm;
        }
    }
}
