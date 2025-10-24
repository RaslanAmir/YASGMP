using System.Windows;
using YasGMP.Wpf.ViewModels;

namespace YasGMP.Wpf.Dialogs
{
    public partial class AiAssistantDialog : Window
    {
        public AiAssistantDialog(AiAssistantDialogViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}

