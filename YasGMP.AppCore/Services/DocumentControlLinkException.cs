using System;

namespace YasGMP.Services
{
    /// <summary>
    /// Represents a recoverable failure when attempting to link a change control to a document.
    /// </summary>
    public sealed class DocumentControlLinkException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the DocumentControlLinkException class.
        /// </summary>
        public DocumentControlLinkException(string message) : base(message)
        {
        }
        /// <summary>
        /// Initializes a new instance of the DocumentControlLinkException class.
        /// </summary>

        public DocumentControlLinkException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
