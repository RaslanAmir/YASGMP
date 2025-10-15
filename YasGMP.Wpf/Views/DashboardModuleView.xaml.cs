using System;
using System.Windows;
using System.Windows.Controls;

namespace YasGMP.Wpf.Views;
/// <summary>
/// Represents the Dashboard Module View.
/// </summary>

public partial class DashboardModuleView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the DashboardModuleView class.
    /// </summary>
    public DashboardModuleView()
    {
        InitializeComponent();
        Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }

        Unloaded -= OnUnloaded;
    }
}
