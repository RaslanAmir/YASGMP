using System.Collections.Generic;
using YasGMP.Models;

namespace YasGMP.Services
{
    /// <summary>
    /// Payload passed to <see cref="IDialogService"/> when opening the calibration editor.
    /// </summary>
    public sealed class CalibrationDialogRequest
    {
        /// <summary>
        /// Initializes a new instance of the CalibrationDialogRequest class.
        /// </summary>
        public CalibrationDialogRequest(Calibration calibration, IReadOnlyList<MachineComponent> components, IReadOnlyList<Supplier> suppliers)
        {
            Calibration = calibration;
            Components = components;
            Suppliers = suppliers;
        }
        /// <summary>
        /// Gets or sets the calibration.
        /// </summary>

        public Calibration Calibration { get; }
        /// <summary>
        /// Gets or sets the components.
        /// </summary>

        public IReadOnlyList<MachineComponent> Components { get; }
        /// <summary>
        /// Gets or sets the suppliers.
        /// </summary>

        public IReadOnlyList<Supplier> Suppliers { get; }
    }
}
