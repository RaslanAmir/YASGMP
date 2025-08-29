using System;
using System.Globalization;
using System.Reflection;
using Microsoft.Maui.Controls;

namespace YasGMP.Converters
{
    /// <summary>
    /// Provides a resilient value conversion that attempts to read one of several
    /// candidate property names from the bound object via reflection.
    /// <para>
    /// Use the <c>ConverterParameter</c> to pass a <c>|</c>-separated list of property
    /// names ordered by preference, e.g. <c>"Code|CompanyCode|ContractorCode"</c>.
    /// The converter returns the first non-empty string representation it can obtain.
    /// </para>
    /// <para>
    /// This is useful when model properties have evolved across versions and you need
    /// a single XAML binding that tolerates different property names without causing
    /// XAML compile-time binding warnings (e.g., XFC0045).
    /// </para>
    /// </summary>
    /// <remarks>
    /// <b>One-way only:</b> <see cref="ConvertBack(object?, Type, object?, CultureInfo)"/> throws <see cref="NotSupportedException"/>.
    /// </remarks>
    public sealed class PropertyOrFallbackConverter : IValueConverter
    {
        /// <summary>
        /// Attempts to extract a readable string value from <paramref name="value"/> by
        /// trying each candidate property name (provided in <paramref name="parameter"/>)
        /// until the first non-empty result is found.
        /// </summary>
        /// <param name="value">The bound object instance (may be <c>null</c>).</param>
        /// <param name="targetType">The target binding type (ignored).</param>
        /// <param name="parameter">
        /// A <see cref="string"/> containing candidate property names separated by <c>|</c>,
        /// e.g. <c>"Code|CompanyCode|SupplierCode"</c>. May be <c>null</c>.
        /// </param>
        /// <param name="culture">The culture (ignored for now; invariant formatting is used).</param>
        /// <returns>
        /// The first non-empty string obtained from any candidate property, or <see cref="string.Empty"/>
        /// if no candidates matched or produced a value.
        /// </returns>
        /// <remarks>
        /// Nullability is aligned with <see cref="IValueConverter"/> to satisfy analyzers:
        /// <list type="bullet">
        /// <item><description>Return type: <c>object?</c></description></item>
        /// <item><description><paramref name="value"/> and <paramref name="parameter"/> are nullable.</description></item>
        /// </list>
        /// </remarks>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is null)
                return string.Empty;

            // Parse parameter into candidate property names
            var paramString = parameter as string;
            if (string.IsNullOrWhiteSpace(paramString))
                return string.Empty;

            var candidates = paramString.Split(
                separator: '|',
                options: StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (candidates.Length == 0)
                return string.Empty;

            var type = value.GetType();

            foreach (var name in candidates)
            {
                // Case-insensitive public instance property lookup
                var prop = type.GetProperty(
                    name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

                if (prop is null)
                    continue;

                var raw = prop.GetValue(value);
                if (raw is null)
                    continue;

                // Convert value to string; prefer IFormattable with invariant culture
                string? s = raw is IFormattable f
                    ? f.ToString(null, CultureInfo.InvariantCulture)
                    : raw.ToString();

                if (!string.IsNullOrWhiteSpace(s))
                    return s;
            }

            return string.Empty;
        }

        /// <summary>
        /// Not supported. This converter is intended for one-way bindings only.
        /// </summary>
        /// <param name="value">Ignored.</param>
        /// <param name="targetType">Ignored.</param>
        /// <param name="parameter">Ignored.</param>
        /// <param name="culture">Ignored.</param>
        /// <returns>Never returns; always throws.</returns>
        /// <exception cref="NotSupportedException">
        /// Thrown in all cases because this converter does not support two-way conversion.
        /// </exception>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException("PropertyOrFallbackConverter is one-way only.");
    }
}
