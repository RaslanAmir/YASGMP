using System.IO;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Provides QR code generation helpers for the WPF shell while delegating the
/// actual rendering to the shared MAUI <see cref="YasGMP.Services.QRCodeService"/>.
/// </summary>
/// <remarks>
/// <para>
/// View-models should request this interface when they need to embed QR payloads
/// in documents, attachments, or export workflows. The adapter ensures both shells
/// call the same implementation so image size, error correction, and encoding
/// remain identical.
/// </para>
/// <para>
/// Returned streams are owned by the caller and positioned at the beginning,
/// matching the MAUI service contract. Callers should dispose the stream after
/// use.
/// </para>
/// </remarks>
public interface IQRCodeService
{
    /// <summary>
    /// Generates a PNG stream representing the specified payload.
    /// </summary>
    Stream GeneratePng(string payload, int pixelSize = 20);
}
