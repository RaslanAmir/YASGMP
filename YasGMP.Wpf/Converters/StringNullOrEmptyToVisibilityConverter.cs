using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace YasGMP.Wpf.Converters
{
    /// <summary>
    /// Converts strings into <see cref="Visibility"/> states, collapsing the target element when the
    /// bound value is null or whitespace. Setting <see cref="Invert"/> flips the behaviour.
    /// </summary>
    public sealed class StringNullOrEmptyToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Gets or sets a value indicating whether the conversion result should be inverted.
        /// When <c>true</c>, non-empty strings collapse the element instead of showing it.
        /// </summary>
        public bool Invert { get; set; }

        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isNullOrEmpty = value switch
            {
                null => true,
                string s => string.IsNullOrWhiteSpace(s),
                _ => string.IsNullOrWhiteSpace(value.ToString())
            };

            if (Invert)
            {
                isNullOrEmpty = !isNullOrEmpty;
            }

            return isNullOrEmpty ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
