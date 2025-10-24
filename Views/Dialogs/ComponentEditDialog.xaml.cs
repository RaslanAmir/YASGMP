using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using YasGMP.Models;

namespace YasGMP.Views.Dialogs
{
    /// <summary>
    /// Modal editor for maintaining component metadata inline.
    /// </summary>
    public partial class ComponentEditDialog : ContentPage
    {
        private readonly TaskCompletionSource<bool> _tcs = new();
        public Task<bool> Result => _tcs.Task;

        public MachineComponent Component { get; }

        public ComponentEditDialog(MachineComponent component)
        {
            InitializeComponent();
            Component = component ?? throw new ArgumentNullException(nameof(component));
            BindingContext = Component;
        }

        private async void OnSaveClicked(object? sender, EventArgs e)
        {
            _tcs.TrySetResult(true);
            await Navigation.PopModalAsync();
        }

        private async void OnCancelClicked(object? sender, EventArgs e)
        {
            _tcs.TrySetResult(false);
            await Navigation.PopModalAsync();
        }
    }
}

