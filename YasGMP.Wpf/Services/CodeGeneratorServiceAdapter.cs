using System;
using YasGMP.Services;

namespace YasGMP.Wpf.Services
{
    /// <summary>
    /// Bridges WPF view-model requests into the shared <see cref="YasGMP.Services.CodeGeneratorService"/>.
    /// </summary>
    /// <remarks>
    /// The adapter wraps the MAUI implementation so both shells generate identical codes without
    /// duplicating logic. Consumers should resolve <see cref="ICodeGeneratorService"/> via DI.
    /// </remarks>
    public sealed class CodeGeneratorServiceAdapter : ICodeGeneratorService
    {
        private readonly CodeGeneratorService _inner;

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeGeneratorServiceAdapter"/> class.
        /// </summary>
        public CodeGeneratorServiceAdapter(CodeGeneratorService inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        /// <inheritdoc />
        public string GenerateMachineCode(string? name, string? manufacturer)
            => _inner.GenerateMachineCode(name, manufacturer);

        /// <inheritdoc />
        public string GenerateMachineCode()
            => _inner.GenerateMachineCode();
    }
}
