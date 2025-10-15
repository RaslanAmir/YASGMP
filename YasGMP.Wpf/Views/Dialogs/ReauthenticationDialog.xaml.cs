using System.Windows;
using System.Windows.Controls;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for the reauthentication dialog. Handles password synchronization and closes
    /// when the underlying view-model signals completion.
    /// </summary>
    public partial class ReauthenticationDialog : Window
    {
        /// <summary>Initializes a new instance of the <see cref="ReauthenticationDialog"/> class.</summary>
        public ReauthenticationDialog()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Closed += (_, _) => DataContextChanged -= OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is ReauthenticationDialogViewModel oldVm)
            {
                oldVm.RequestClose -= OnRequestClose;
            }

            if (e.NewValue is ReauthenticationDialogViewModel vm)
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
            if (DataContext is ReauthenticationDialogViewModel vm && sender is PasswordBox box)
            {
                vm.SetPassword(box.Password);
            }
        }
    }
}
