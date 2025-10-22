using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using YasGMP.Common;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Views;

/// <summary>
/// Hosts the Document Control module surface inside the WPF shell.
/// </summary>
public partial class DocumentControlModuleView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentControlModuleView"/> class.
    /// </summary>
    public DocumentControlModuleView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;

        if (!DesignerProperties.GetIsInDesignMode(this) && DataContext is null)
        {
            DataContext = ServiceLocator.GetRequiredService<DocumentControlModuleViewModel>();
        }

        _ = SearchTextBox?.Focus();
    }
}
