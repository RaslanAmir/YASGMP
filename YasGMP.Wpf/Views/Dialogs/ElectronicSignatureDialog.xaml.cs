using System;
using System.Windows;
using System.Windows.Controls;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Views.Dialogs;
/// <summary>
/// Represents the Electronic Signature Dialog.
/// </summary>

public partial class ElectronicSignatureDialog : Window
{
    /// <summary>
    /// Initializes a new instance of the ElectronicSignatureDialog class.
    /// </summary>
    public ElectronicSignatureDialog()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closed += OnClosed;
    }
    /// <summary>
    /// Initializes a new instance of the ElectronicSignatureDialog class.
    /// </summary>

    public ElectronicSignatureDialog(ElectronicSignatureDialogViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ElectronicSignatureDialogViewModel vm)
        {
            vm.RequestClose += OnRequestClose;
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (DataContext is ElectronicSignatureDialogViewModel vm)
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

    private void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ElectronicSignatureDialogViewModel vm && sender is PasswordBox passwordBox)
        {
            vm.Password = passwordBox.Password;
        }
    }
}
