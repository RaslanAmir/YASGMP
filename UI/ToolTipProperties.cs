// ==============================================================================
//  File: UI/ToolTipProperties.cs
//  Project: YasGMP
//  Summary:
//      Project-local attached property for tooltips that always resolves at XAML
//      compile-time. Stores tooltip text and mirrors it to SemanticProperties
//      for accessibility and (on some platforms) native hints.
//  Author: YasGMP
//  (c) 2025 YasGMP. All rights reserved.
// ==============================================================================

using System;
using Microsoft.Maui.Controls;

namespace YasGMP.UI
{
    /// <summary>
    /// Provides a project-local, cross-platform attached property for simple tooltips.
    /// <para>
    /// Usage in XAML:
    /// <code lang="xml">
    /// xmlns:ui="clr-namespace:YasGMP.UI"
    /// &lt;Button Text="Save" ui:ToolTipProperties.Text="Saves current item"/&gt;
    /// </code>
    /// </para>
    /// <remarks>
    /// This implementation guarantees XAML type resolution and build success even if
    /// community toolkit packages are absent. It also mirrors the tooltip text to
    /// <see cref="SemanticProperties.DescriptionProperty"/> to improve accessibility.
    /// </remarks>
    /// </summary>
    public static class ToolTipProperties
    {
        /// <summary>
        /// Backing store for <see cref="TextProperty"/>.
        /// </summary>
        public static readonly BindableProperty TextProperty = BindableProperty.CreateAttached(
            propertyName: "Text",
            returnType: typeof(string),
            declaringType: typeof(ToolTipProperties),
            defaultValue: default(string),
            propertyChanged: OnTextChanged);

        /// <summary>
        /// Gets the tooltip text attached to a <see cref="BindableObject"/>.
        /// </summary>
        /// <param name="bindable">Target object.</param>
        /// <returns>Tooltip text, or <c>null</c> if unset.</returns>
        public static string GetText(BindableObject bindable)
            => (string)bindable.GetValue(TextProperty);

        /// <summary>
        /// Sets the tooltip text attached to a <see cref="BindableObject"/>.
        /// </summary>
        /// <param name="bindable">Target object.</param>
        /// <param name="value">Tooltip text.</param>
        public static void SetText(BindableObject bindable, string value)
            => bindable.SetValue(TextProperty, value);

        /// <summary>
        /// When the tooltip text changes, mirror it to <see cref="SemanticProperties.DescriptionProperty"/>
        /// to aid screen readers and (on some platforms) native hints/tooltips.
        /// </summary>
        private static void OnTextChanged(BindableObject bindable, object oldValue, object newValue)
        {
            try
            {
                if (bindable is VisualElement ve)
                {
                    var text = newValue as string;
                    SemanticProperties.SetDescription(ve, text);
                }
            }
            catch
            {
                // Swallow to be design-time safe — tooltip is optional sugar.
            }
        }
    }
}
