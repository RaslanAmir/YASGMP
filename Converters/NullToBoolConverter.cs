using System;
using System.ComponentModel;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace YasGMP.Converters
{
    /// <summary>
    /// <b>NullToBoolConverter</b> – Converts a value to a <see cref="bool"/> that is
    /// <c>true</c> when the value is non-<c>null</c>, and <c>false</c> when it is <c>null</c>.
    /// Supports inversion either via the <see cref="Invert"/> property or the
    /// string converter parameter <c>"invert"</c> (case-insensitive).
    /// </summary>
    /// <remarks>
    /// Default mapping: <c>null → false</c>, <c>non-null → true</c>.
    /// Set <see cref="Invert"/> to <c>true</c> or pass <c>"invert"</c> as <c>ConverterParameter</c> to reverse it.
    /// </remarks>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <ContentPage.Resources>
    ///   <ResourceDictionary>
    ///     <conv:NullToBoolConverter x:Key="NullToBoolConverter" />
    ///   </ResourceDictionary>
    /// </ContentPage.Resources>
    ///
    /// <!-- Enabled only when SelectedItem is not null -->
    /// <Button IsEnabled="{Binding SelectedItem, Converter={StaticResource NullToBoolConverter}}" />
    ///
    /// <!-- Visible only when SelectedItem is null (invert) -->
    /// <Label IsVisible="{Binding SelectedItem, Converter={StaticResource NullToBoolConverter}, ConverterParameter=invert}" />
    /// ]]></code>
    /// </example>
    [EditorBrowsable(EditorBrowsableState.Always)]
    public sealed class NullToBoolConverter : IValueConverter
    {
        /// <summary>
        /// When <c>true</c>, reverses the mapping (i.e., <c>null → true</c>, <c>non-null → false</c>).
        /// </summary>
        public bool Invert { get; set; }

        /// <inheritdoc />
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool isNotNull = value is not null;

            // Parameter-based inversion has priority over the property.
            if (parameter is string p && p.Equals("invert", StringComparison.OrdinalIgnoreCase))
                return !isNotNull;

            return Invert ? !isNotNull : isNotNull;
        }

        /// <inheritdoc />
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException($"{nameof(NullToBoolConverter)} does not support ConvertBack.");
    }
}
