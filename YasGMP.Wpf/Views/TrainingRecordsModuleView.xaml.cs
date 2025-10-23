using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using YasGMP.Common;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Views;

/// <summary>
/// Hosts the Training Records module surface.
/// </summary>
public partial class TrainingRecordsModuleView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TrainingRecordsModuleView"/> class.
    /// </summary>
    public TrainingRecordsModuleView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;

        if (!DesignerProperties.GetIsInDesignMode(this) && DataContext is null)
        {
            DataContext = ServiceLocator.GetRequiredService<TrainingRecordsModuleViewModel>();
        }
    }
}
