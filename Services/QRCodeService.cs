using System.IO;
using System.Threading.Tasks;
using ZXing.Net.Maui;

namespace YasGMP.Services
{
    /// <summary>
    /// Helper for generating QR code images using ZXing.Net.Maui.
    /// </summary>
    public class QRCodeService
    {
        /// <summary>
        /// Generates a PNG stream for the supplied payload.
        /// </summary>
        public Stream GeneratePng(string payload, int size = 256)
        {
            var generator = new BarcodeGenerator
            {
                Format = BarcodeFormat.QrCode,
                Value = payload
            };
            var stream = new MemoryStream();
            generator.GenerateStream(stream, size, size, ImageFormat.Png);
            stream.Position = 0;
            return stream;
        }
    }
}
