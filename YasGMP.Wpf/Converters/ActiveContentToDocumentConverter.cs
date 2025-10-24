using System;
using System.Globalization;
using System.Windows.Data;

namespace YasGMP.Wpf.Converters;

/// <summary>
/// Bridges AvalonDock's ActiveContent (which may be a UserControl like ModulesPane/InspectorPane)
/// and the view-model's <see cref="ViewModels.DocumentViewModel"/> expectation.
/// - Convert: only passes through DocumentViewModel instances; otherwise returns null.
/// - ConvertBack: updates source only when the value is a DocumentViewModel; otherwise Binding.DoNothing.
/// </summary>
public sealed class ActiveContentToDocumentConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value as ViewModels.DocumentViewModel;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is ViewModels.DocumentViewModel vm ? vm : Binding.DoNothing;
}

