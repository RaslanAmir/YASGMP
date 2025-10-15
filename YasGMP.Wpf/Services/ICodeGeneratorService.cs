namespace YasGMP.Wpf.Services;

/// <summary>
/// Exposes machine code generation helpers to the WPF shell while delegating
/// to the shared MAUI <see cref="YasGMP.Services.CodeGeneratorService"/> implementation.
/// </summary>
/// <remarks>
/// <para>
/// View-models should request this interface instead of directly instantiating the
/// AppCore service so that dependency injection can provide a single adapter which
/// mirrors the MAUI experience. The adapter ensures both shells use the same
/// timestamp and string-normalization rules when composing machine identifiers.
/// </para>
/// <para>
/// Consumers typically call <see cref="GenerateMachineCode(string?, string?)"/> when
/// capturing equipment metadata (e.g., Assets module Add flow). The parameterless
/// overload exists for legacy dialogs that only rely on timestamp-based identifiers.
/// </para>
/// </remarks>
public interface ICodeGeneratorService
{
    /// <summary>
    /// Generates a machine code from the supplied metadata using the shared
    /// MAUI rules (three-character prefixes plus a UTC timestamp).
    /// </summary>
    string GenerateMachineCode(string? name, string? manufacturer);

    /// <summary>
    /// Generates a legacy timestamp-only machine code (XXX-XXX-yyyMMddHHmmss).
    /// </summary>
    string GenerateMachineCode();
}
