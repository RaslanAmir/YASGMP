using System;
using System.IO;
using YasGMP.Services;

namespace YasGMP.Wpf.Services
{
    /// <summary>
    /// Routes QR code generation requests from the WPF shell into the shared
    /// <see cref="YasGMP.Services.QRCodeService"/> so the MAUI and WPF experiences remain aligned.
    /// </summary>
    public sealed class QRCodeServiceAdapter : IQRCodeService
    {
        private readonly QRCodeService _inner;

        /// <summary>
        /// Initializes a new instance of the <see cref="QRCodeServiceAdapter"/> class.
        /// </summary>
        public QRCodeServiceAdapter(QRCodeService inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        /// <inheritdoc />
        public Stream GeneratePng(string payload, int pixelSize = 20)
            => _inner.GeneratePng(payload, pixelSize);
    }
}
