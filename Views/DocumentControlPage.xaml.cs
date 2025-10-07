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
        /// <summary>
        /// Gets or sets the view model.
        /// </summary>
        public DocumentControlViewModel ViewModel { get; }
        /// <summary>
        /// Initializes a new instance of the DocumentControlPage class.
        /// </summary>

        public DocumentControlPage(DocumentControlViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            BindingContext = ViewModel;
        }
        /// <summary>
        /// Initializes a new instance of the DocumentControlPage class.
        /// </summary>

        public DocumentControlPage()
            : this(ServiceLocator.GetRequiredService<DocumentControlViewModel>())
        {
        }
    }
}
