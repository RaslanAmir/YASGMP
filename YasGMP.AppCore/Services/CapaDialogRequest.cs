using YasGMP.Models;

namespace YasGMP.Services
{
    /// <summary>
    /// Payload for CAPA editor dialogs.
    /// </summary>
    public sealed class CapaDialogRequest
    {
        /// <summary>
        /// Initializes a new instance of the CapaDialogRequest class.
        /// </summary>
        public CapaDialogRequest(CapaCase? capaCase)
        {
            CapaCase = capaCase;
        }
        /// <summary>
        /// Gets or sets the capa case.
        /// </summary>

        public CapaCase? CapaCase { get; }
    }
}
