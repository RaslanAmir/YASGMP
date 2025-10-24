using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    /// <summary>
    /// Legacy placeholder retained so older migrations referencing a PermissionType model compile.
    /// Actual permission taxonomy lives in <see cref="Enums.PermissionType"/>.
    /// </summary>
    [Keyless]
    public sealed class PermissionType
    {
    }
}

