using System;
using System.Text;

namespace YasGMP.Wpf.ViewModels;

/// <summary>Provides deterministic normalization for automation identifiers.</summary>
public static class AutomationIdSanitizer
{
    private const char Separator = '-';

    /// <summary>
    /// Normalizes the provided value into a deterministic token composed of lowercase letters, numbers, and separators.
    /// </summary>
    /// <param name="input">Value to normalize.</param>
    /// <param name="fallback">Fallback token used when <paramref name="input"/> cannot be normalized.</param>
    /// <returns>A normalized token safe for automation identifiers.</returns>
    public static string Normalize(string? input, string fallback)
    {
        if (fallback is null)
        {
            throw new ArgumentNullException(nameof(fallback));
        }

        var sanitizedFallback = SanitizeCore(fallback);
        if (string.IsNullOrEmpty(sanitizedFallback))
        {
            sanitizedFallback = "fallback";
        }

        var sanitizedInput = SanitizeCore(input);
        return string.IsNullOrEmpty(sanitizedInput) ? sanitizedFallback : sanitizedInput;
    }

    private static string SanitizeCore(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        var builder = new StringBuilder(trimmed.Length);
        var lastAppendedSeparator = false;

        foreach (var ch in trimmed)
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(char.ToLowerInvariant(ch));
                lastAppendedSeparator = false;
            }
            else if (!lastAppendedSeparator && builder.Length > 0)
            {
                builder.Append(Separator);
                lastAppendedSeparator = true;
            }
        }

        if (builder.Length == 0)
        {
            return string.Empty;
        }

        var result = builder.ToString().Trim(Separator);
        return result;
    }
}
