using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Dialogs;

/// <summary>
/// Interaction logic for UserEditDialogWindow.xaml.
/// </summary>
public partial class UserEditDialogWindow : Window
{
    private UserEditDialogViewModel? _viewModel;
    private UserEditDialogViewModel.UserEditor? _editor;
    private bool _suppressPasswordSync;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserEditDialogWindow"/> class.
    /// </summary>
    public UserEditDialogWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.RequestClose -= OnRequestCloseRequested;
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        if (_editor is not null)
        {
            _editor.PropertyChanged -= OnEditorPropertyChanged;
        }

        _viewModel = e.NewValue as UserEditDialogViewModel;
        _editor = _viewModel?.Editor;

        if (_viewModel is not null)
        {
            _viewModel.RequestClose += OnRequestCloseRequested;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        if (_editor is not null)
        {
            _editor.PropertyChanged += OnEditorPropertyChanged;
            SynchronizePasswords(_editor);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UserEditDialogViewModel.Editor))
        {
            if (_editor is not null)
            {
                _editor.PropertyChanged -= OnEditorPropertyChanged;
            }

            _editor = _viewModel?.Editor;
            if (_editor is not null)
            {
                _editor.PropertyChanged += OnEditorPropertyChanged;
                SynchronizePasswords(_editor);
            }
        }
    }

    private void OnEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not UserEditDialogViewModel.UserEditor editor)
        {
            return;
        }

        switch (e.PropertyName)
        {
            case nameof(UserEditDialogViewModel.UserEditor.NewPassword):
                UpdatePasswordBox(NewPasswordBox, editor.NewPassword);
                break;
            case nameof(UserEditDialogViewModel.UserEditor.ConfirmPassword):
                UpdatePasswordBox(ConfirmPasswordBox, editor.ConfirmPassword);
                break;
        }
    }

    private void UpdatePasswordBox(PasswordBox passwordBox, string? value)
    {
        var normalized = value ?? string.Empty;
        if (passwordBox.Password == normalized)
        {
            return;
        }

        _suppressPasswordSync = true;
        try
        {
            passwordBox.Password = normalized;
        }
        finally
        {
            _suppressPasswordSync = false;
        }
    }

    private void OnRequestCloseRequested(object? sender, bool result)
    {
        DialogResult = result;
        Close();
    }

    private void OnNewPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_suppressPasswordSync)
        {
            return;
        }

        if (_viewModel is not null)
        {
            _viewModel.Editor.NewPassword = NewPasswordBox.Password;
        }
    }

    private void OnConfirmPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_suppressPasswordSync)
        {
            return;
        }

        if (_viewModel is not null)
        {
            _viewModel.Editor.ConfirmPassword = ConfirmPasswordBox.Password;
        }
    }

    private void SynchronizePasswords(UserEditDialogViewModel.UserEditor editor)
    {
        UpdatePasswordBox(NewPasswordBox, editor.NewPassword);
        UpdatePasswordBox(ConfirmPasswordBox, editor.ConfirmPassword);
    }

    /// <inheritdoc />
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        if (_viewModel is not null)
        {
            _viewModel.RequestClose -= OnRequestCloseRequested;
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        if (_editor is not null)
        {
            _editor.PropertyChanged -= OnEditorPropertyChanged;
        }

        _viewModel = null;
        _editor = null;
    }
}
