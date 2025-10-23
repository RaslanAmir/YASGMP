using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using YasGMP.Common;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Views;

/// <summary>
/// Hosts the SOP governance module document surface.
/// </summary>
public partial class SopGovernanceModuleView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SopGovernanceModuleView"/> class.
    /// </summary>
    public SopGovernanceModuleView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;

        if (!DesignerProperties.GetIsInDesignMode(this) && DataContext is null)
        {
            DataContext = ServiceLocator.GetRequiredService<SopGovernanceModuleViewModel>();
        }
    }
}
