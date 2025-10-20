using System.Windows;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Dialogs;

/// <summary>
/// Interaction logic for StockTransactionDialog.xaml
/// </summary>
public partial class StockTransactionDialog : Window
{
    public StockTransactionDialog()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is StockTransactionDialogViewModel oldVm)
        {
            oldVm.CloseRequested -= OnCloseRequested;
        }

        if (e.NewValue is StockTransactionDialogViewModel newVm)
        {
            newVm.CloseRequested += OnCloseRequested;
        }
    }

    private void OnCloseRequested(object? sender, bool e)
    {
        DialogResult = e;
        Close();
    }
}
