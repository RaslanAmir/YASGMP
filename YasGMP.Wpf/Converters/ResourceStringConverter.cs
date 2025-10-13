using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace YasGMP.Wpf.Converters;

/// <summary>
/// Resolves an application resource by key and returns it as a string.
/// Returns the original value when the key is not found or not a string.
/// One-way only; ConvertBack is not supported.
/// </summary>
public sealed class ResourceStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string key && !string.IsNullOrWhiteSpace(key))
        {
            try
            {
                var app = Application.Current;
                if (app?.Resources.Contains(key) == true)
                {
                    var s = app.Resources[key] as string;
                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        return s!;
                    }
                }
            }
            catch
            {
                // ignore and fall through
            }
        }

        // Fallback to the input as-is
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException("ResourceStringConverter is one-way only.");
}

