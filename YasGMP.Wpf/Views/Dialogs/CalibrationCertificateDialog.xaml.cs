using System;
using System.Windows;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Views.Dialogs;

/// <summary>Interaction logic for CalibrationCertificateDialog.xaml.</summary>
public partial class CalibrationCertificateDialog : Window
{
    /// <summary>Initializes a new instance of the <see cref="CalibrationCertificateDialog"/> class.</summary>
    public CalibrationCertificateDialog()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    /// <summary>Initializes a new instance of the <see cref="CalibrationCertificateDialog"/> class with a data context.</summary>
    /// <param name="viewModel">View-model that backs the dialog.</param>
    public CalibrationCertificateDialog(CalibrationCertificateDialogViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is CalibrationCertificateDialogViewModel vm)
        {
            vm.RequestClose += OnRequestClose;
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (DataContext is CalibrationCertificateDialogViewModel vm)
        {
            vm.RequestClose -= OnRequestClose;
        }
    }

    private void OnRequestClose(object? sender, bool confirmed)
    {
        Dispatcher.Invoke(() =>
        {
            DialogResult = confirmed;
            Close();
        });
    }
}
