using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using YasGMP.Wpf.ViewModels;

namespace YasGMP.Wpf.Views
{
    /// <summary>
    /// Interaction logic for the login dialog window. Wires password updates and closes when the
    /// bound <see cref="LoginViewModel"/> signals completion.
    /// </summary>
    public partial class LoginView : Window
    {
        /// <summary>Initializes a new instance of the <see cref="LoginView"/> class.</summary>
        public LoginView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Closed += (_, _) => DataContextChanged -= OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is LoginViewModel oldVm)
            {
                oldVm.RequestClose -= OnRequestClose;
            }

            if (e.NewValue is LoginViewModel vm)
            {
                vm.RequestClose += OnRequestClose;
            }
        }

        private void OnRequestClose(object? sender, bool accepted)
        {
            DialogResult = accepted;
            Close();
        }

        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm && sender is PasswordBox box)
            {
                vm.SetPassword(box.Password);
            }
        }

        private void OnPasswordBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is LoginViewModel vm && vm.LoginCommand.CanExecute(null))
            {
                _ = vm.LoginCommand.ExecuteAsync(null);
                e.Handled = true;
            }
        }
    }
}
