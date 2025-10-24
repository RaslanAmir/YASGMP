using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Represents a stored document or attachment.
    /// Mirrors the <c>documents</c> table.
    /// </summary>
    [Table("documents")]
    public partial class Document
    {
        public Document()
        {
        }
    }
}

