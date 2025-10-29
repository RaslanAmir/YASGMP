using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace YasGMP.Wpf.Controls;

/// <summary>
/// Provides attached helpers to automatically scroll items controls to the end when
/// new content is appended, keeping live feeds (e.g., diagnostics logs) in view.
/// </summary>
public static class AutoScrollBehavior
{
    /// <summary>Identifies the <see cref="AutoScrollToEndProperty"/> attached property.</summary>
    public static readonly DependencyProperty AutoScrollToEndProperty = DependencyProperty.RegisterAttached(
        "AutoScrollToEnd",
        typeof(bool),
        typeof(AutoScrollBehavior),
        new PropertyMetadata(false, OnAutoScrollToEndChanged));

    private static readonly DependencyProperty CollectionChangedHandlerProperty = DependencyProperty.RegisterAttached(
        "CollectionChangedHandler",
        typeof(NotifyCollectionChangedEventHandler),
        typeof(AutoScrollBehavior));

    /// <summary>Gets the value indicating whether auto scrolling is enabled.</summary>
    public static bool GetAutoScrollToEnd(DependencyObject obj)
        => (bool)(obj.GetValue(AutoScrollToEndProperty) ?? false);

    /// <summary>Sets the value indicating whether auto scrolling is enabled.</summary>
    public static void SetAutoScrollToEnd(DependencyObject obj, bool value)
        => obj.SetValue(AutoScrollToEndProperty, value);

    private static void OnAutoScrollToEndChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ItemsControl itemsControl)
        {
            return;
        }

        if (e.NewValue is true)
        {
            Attach(itemsControl);
        }
        else
        {
            Detach(itemsControl);
        }
    }

    private static void Attach(ItemsControl itemsControl)
    {
        Detach(itemsControl);

        if (itemsControl.Items is not INotifyCollectionChanged collection)
        {
            return;
        }

        NotifyCollectionChangedEventHandler handler = (_, args) =>
        {
            if (!GetAutoScrollToEnd(itemsControl) || args.Action == NotifyCollectionChangedAction.Reset)
            {
                return;
            }

            itemsControl.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                if (itemsControl.Items.Count == 0)
                {
                    return;
                }

                var lastItem = itemsControl.Items[itemsControl.Items.Count - 1];
                switch (itemsControl)
                {
                    case ListBox listBox:
                        listBox.ScrollIntoView(lastItem);
                        break;
                    case ListView listView:
                        listView.ScrollIntoView(lastItem);
                        break;
                    default:
                        if (TryGetScrollViewer(itemsControl) is ScrollViewer viewer)
                        {
                            viewer.ScrollToEnd();
                        }
                        break;
                }
            }));
        };

        collection.CollectionChanged += handler;
        itemsControl.SetValue(CollectionChangedHandlerProperty, handler);
        itemsControl.Unloaded += OnItemsControlUnloaded;
    }

    private static void Detach(ItemsControl itemsControl)
    {
        if (itemsControl.Items is INotifyCollectionChanged collection)
        {
            if (itemsControl.GetValue(CollectionChangedHandlerProperty) is NotifyCollectionChangedEventHandler handler)
            {
                collection.CollectionChanged -= handler;
            }
        }

        itemsControl.ClearValue(CollectionChangedHandlerProperty);
        itemsControl.Unloaded -= OnItemsControlUnloaded;
    }

    private static void OnItemsControlUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is ItemsControl itemsControl)
        {
            Detach(itemsControl);
        }
    }

    private static ScrollViewer? TryGetScrollViewer(DependencyObject parent)
    {
        if (parent is ScrollViewer viewer)
        {
            return viewer;
        }

        var childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (var index = 0; index < childCount; index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (TryGetScrollViewer(child) is ScrollViewer descendant)
            {
                return descendant;
            }
        }

        return null;
    }
}
