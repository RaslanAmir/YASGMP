using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using YasGMP.Models;

namespace YasGMP.Views.Dialogs
{
    public partial class MachineEditDialog : ContentPage
    {
        private readonly TaskCompletionSource<bool> _tcs = new();
        public Task<bool> Result => _tcs.Task;
        public Machine Machine { get; }

        public ObservableCollection<string> Types { get; } = new() { "Tip A", "Dodaj novi..." };
        public ObservableCollection<string> Manufacturers { get; } = new() { "Proizvođač A", "Dodaj novi..." };
        public ObservableCollection<string> Locations { get; } = new() { "Lokacija A", "Dodaj novi..." };
        public ObservableCollection<string> Responsibles { get; } = new() { "Osoba A", "Dodaj novi..." };
        public ObservableCollection<string> Statuses { get; } = new() { "active", "maintenance", "Dodaj novi..." };

        public MachineEditDialog(Machine machine)
        {
            InitializeComponent();
            Machine = machine;
            BindingContext = Machine;
            TypePicker.ItemsSource = Types;
            ManufacturerPicker.ItemsSource = Manufacturers;
            LocationPicker.ItemsSource = Locations;
            ResponsiblePicker.ItemsSource = Responsibles;
            StatusPicker.ItemsSource = Statuses;
        }

        private async void OnTypeChanged(object? sender, EventArgs e) => await HandleAddNewAsync(TypePicker, Types);
        private async void OnManufacturerChanged(object? sender, EventArgs e) => await HandleAddNewAsync(ManufacturerPicker, Manufacturers);
        private async void OnLocationChanged(object? sender, EventArgs e) => await HandleAddNewAsync(LocationPicker, Locations);
        private async void OnResponsibleChanged(object? sender, EventArgs e) => await HandleAddNewAsync(ResponsiblePicker, Responsibles);
        private async void OnStatusChanged(object? sender, EventArgs e) => await HandleAddNewAsync(StatusPicker, Statuses);

        private async Task HandleAddNewAsync(Picker picker, ObservableCollection<string> list)
        {
            if (picker.SelectedItem?.ToString() == "Dodaj novi...")
            {
                var entry = await DisplayPromptAsync(picker.Title, "Unesi novu vrijednost");
                if (!string.IsNullOrWhiteSpace(entry))
                {
                    list.Insert(list.Count - 1, entry);
                    picker.SelectedItem = entry;
                }
                else
                {
                    picker.SelectedIndex = -1;
                }
            }
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