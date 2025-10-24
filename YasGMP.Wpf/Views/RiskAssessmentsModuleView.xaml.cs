using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using YasGMP.Common;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Views;

/// <summary>
/// Hosts the Risk Assessments quality workspace.
/// </summary>
public partial class RiskAssessmentsModuleView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RiskAssessmentsModuleView"/> class.
    /// </summary>
    public RiskAssessmentsModuleView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;

        if (!DesignerProperties.GetIsInDesignMode(this) && DataContext is null)
        {
            DataContext = ServiceLocator.GetRequiredService<RiskAssessmentsModuleViewModel>();
        }
    }
}
