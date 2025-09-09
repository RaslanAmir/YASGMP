using System;
using System.Drawing;
using System.IO;
using SDI = System.Drawing.Imaging;   // alias to avoid ImageFormat ambiguity
using ZXing;
using ZXing.Common;
using ZXing.QrCode;

namespace YasGMP.Services
{
    /// <summary>
    /// Utility for generating QR code PNG images using ZXing.Net (core).
    /// </summary>
    public class QRCodeService
    {
        /// <summary>
        /// Generates a PNG stream for the supplied payload.
        /// </summary>
        /// <param name="payload">Text to encode inside the QR code.</param>
        /// <param name="size">Image size in pixels (square).</param>
        /// <returns>Stream positioned at beginning containing PNG data.</returns>
        public Stream GeneratePng(string payload, int size = 256)
        {
            payload ??= string.Empty;

            var writer = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions
                {
                    Height = size,
                    Width  = size,
                    Margin = 0
                }
            };

            var pixelData = writer.Write(payload);

            using var bitmap = new Bitmap(pixelData.Width, pixelData.Height, SDI.PixelFormat.Format32bppRgb);
            var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var bmpData = bitmap.LockBits(rect, SDI.ImageLockMode.WriteOnly, bitmap.PixelFormat);

            try
            {
                System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bmpData.Scan0, pixelData.Pixels.Length);
            }
            finally
            {
                bitmap.UnlockBits(bmpData);
            }

            var ms = new MemoryStream();
            bitmap.Save(ms, SDI.ImageFormat.Png);  // explicitly use System.Drawing.Imaging.ImageFormat
            ms.Position = 0;
            return ms;
        }
    }
}
