using System;
using System.IO;
using QRCoder;

namespace YasGMP.Services
{
    /// <summary>
    /// Utility for generating QR code PNG images using QRCoder (no System.Drawing dependency).
    /// </summary>
    public class QRCodeService
    {
        /// <summary>
        /// Generates a PNG stream for the supplied payload.
        /// </summary>
        /// <param name="payload">Text to encode inside the QR code.</param>
        /// <param name="pixelSize">
        /// Size of each QR module (pixel) in the output image.
        /// Typical values: 8–20. Defaults to 20.
        /// </param>
        /// <returns>Stream positioned at beginning containing PNG data.</returns>
        public Stream GeneratePng(string payload, int pixelSize = 20)
        {
            payload ??= string.Empty;
            if (pixelSize < 1) pixelSize = 1;

            var generator = new QRCodeGenerator();
            // ECCLevel.Q is a good balance between error correction and density.
            QRCodeData data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);

            var qrCode = new PngByteQRCode(data);
            byte[] bytes = qrCode.GetGraphic(pixelSize);

            var ms = new MemoryStream(bytes);
            ms.Position = 0;
            return ms;
        }
    }
}

