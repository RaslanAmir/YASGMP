using System;
using Microsoft.Maui.Controls;
using YasGMP.Common;
using YasGMP.ViewModels;

namespace YasGMP.Views
{
    public partial class DocumentControlPage : ContentPage
    {
        public DocumentControlViewModel ViewModel { get; }

        public DocumentControlPage(DocumentControlViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            BindingContext = ViewModel;
        }

        public DocumentControlPage()
            : this(ServiceLocator.GetRequiredService<DocumentControlViewModel>())
        {
        }
    }
}
