using System.Collections.Generic;
using YasGMP.Models;

namespace YasGMP.Services
{
    /// <summary>
    /// Payload passed to <see cref="IDialogService"/> when opening the calibration editor.
    /// </summary>
    public sealed class CalibrationDialogRequest
    {
        public CalibrationDialogRequest(Calibration calibration, IReadOnlyList<MachineComponent> components, IReadOnlyList<Supplier> suppliers)
        {
            Calibration = calibration;
            Components = components;
            Suppliers = suppliers;
        }

        public Calibration Calibration { get; }

        public IReadOnlyList<MachineComponent> Components { get; }

        public IReadOnlyList<Supplier> Suppliers { get; }
    }
}
