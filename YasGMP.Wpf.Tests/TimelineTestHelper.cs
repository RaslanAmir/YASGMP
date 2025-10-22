using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Xunit;

namespace YasGMP.Wpf.Tests;

internal static class TimelineTestHelper
{
    internal static IReadOnlyList<object> GetTimelineEntries(object viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        var property = viewModel.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(p => p.PropertyType.IsGenericType
                && p.PropertyType.GetGenericTypeDefinition() == typeof(ObservableCollection<>)
                && p.Name.EndsWith("Timeline", StringComparison.Ordinal));

        Assert.NotNull(property);

        var value = property!.GetValue(viewModel);
        Assert.NotNull(value);

        return ((IEnumerable)value!).Cast<object>().ToList();
    }

    internal static DateTime GetTimestamp(object entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var property = entry.GetType().GetProperty("Timestamp", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(property);

        var value = property!.GetValue(entry);
        Assert.IsType<DateTime>(value);

        return (DateTime)value!;
    }

    internal static string GetSummary(object entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var property = entry.GetType().GetProperty("Summary", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(property);

        var value = property!.GetValue(entry) ?? string.Empty;
        return Assert.IsType<string>(value);
    }

    internal static string GetDetails(object entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var property = entry.GetType().GetProperty("Details", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(property);

        var value = property!.GetValue(entry) ?? string.Empty;
        return Assert.IsType<string>(value);
    }
}
