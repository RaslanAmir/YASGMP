using System.Windows;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Dialogs;

/// <summary>
/// Interaction logic for WarehouseStockTransactionDialog.xaml
/// </summary>
public partial class WarehouseStockTransactionDialog : Window
{
    public WarehouseStockTransactionDialog()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is WarehouseStockTransactionDialogViewModel oldVm)
        {
            oldVm.CloseRequested -= OnCloseRequested;
        }

        if (e.NewValue is WarehouseStockTransactionDialogViewModel newVm)
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
