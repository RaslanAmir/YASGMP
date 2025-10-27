using System;
using Microsoft.Maui.Controls;

namespace YasGMP.Helpers
{
    /// <summary>
    /// XAML markup extension that resolves strings from the shared ShellStrings resource set.
    /// Usage: <c>{helpers:ShellString Key=Module.Security.Text.Username}</c>
    /// </summary>
    [ContentProperty(nameof(Key))]
    public sealed class ShellStringExtension : IMarkupExtension<string>
    {
        /// <summary>Resource key inside <c>ShellStrings</c>.</summary>
        public string? Key { get; set; }

        /// <inheritdoc />
        public string ProvideValue(IServiceProvider serviceProvider)
            => ShellString.Get(Key ?? string.Empty);

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
            => ProvideValue(serviceProvider);
    }
}
