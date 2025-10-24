using YasGMP.Models;

namespace YasGMP.Services
{
    /// <summary>
    /// Payload for CAPA editor dialogs.
    /// </summary>
    public sealed class CapaDialogRequest
    {
        public CapaDialogRequest(CapaCase? capaCase)
        {
            CapaCase = capaCase;
        }

        public CapaCase? CapaCase { get; }
    }
}

