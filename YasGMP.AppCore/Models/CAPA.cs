using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>CAPA</b> – Alias for <see cref="CapaCase"/> to support legacy references and ViewModel compatibility.
    /// <para>
    /// Use <see cref="CapaCase"/> for all new code. This type enables ViewModel and data compatibility for any code using "CAPA" as the class name.
    /// </para>
    /// </summary>
    [NotMapped]
    public class CAPA : CapaCase
    {
        // Inherits everything from CapaCase.
        // This alias exists only for legacy compatibility.
    }
}
