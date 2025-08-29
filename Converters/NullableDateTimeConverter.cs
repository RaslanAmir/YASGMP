using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace YasGMP.Converters
{
    /// <summary>
    /// Converts nullable/unknown date inputs to a safe <see cref="DateTime"/> for UI bindings
    /// (e.g., <see cref="DatePicker.Date"/>), and converts back to <c>DateTime?</c>.
    /// This avoids XAML warnings like: "'(null)' cannot be converted to type 'System.DateTime'".
    /// </summary>
    public sealed class NullableDateTimeConverter : IValueConverter
    {
        /// <summary>
        /// Converts <paramref name="value"/> (which may be null or various types) into a non-null <see cref="DateTime"/>.
        /// Returns <see cref="DateTime.Today"/> when no valid date can be obtained.
        /// </summary>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is DateTime dt) return dt;
            if (value is DateTimeOffset dto) return dto.DateTime;
            if (value is string s && DateTime.TryParse(s, culture, DateTimeStyles.None, out var parsed)) return parsed;

            // Fallback for null/unknown: keep UI happy with a concrete date
            return DateTime.Today;
        }

        /// <summary>
        /// Converts the UI value back to the model. If the value can't be parsed as a date, returns <c>null</c>.
        /// </summary>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is DateTime dt) return dt;
            if (value is DateTimeOffset dto) return dto.DateTime;
            if (value is string s && DateTime.TryParse(s, culture, DateTimeStyles.None, out var parsed)) return parsed;

            return null; // model remains nullable
        }
    }
}
