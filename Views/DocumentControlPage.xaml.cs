using System;
using Microsoft.Maui.Controls;
using YasGMP.Common;
using YasGMP.ViewModels;

namespace YasGMP.Views
{
    /// <summary>
    /// Content page backing the document control workspace, binding to its ViewModel.
    /// </summary>
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
