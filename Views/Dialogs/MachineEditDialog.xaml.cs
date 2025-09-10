using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;
using YasGMP.Models;
using YasGMP.Services;
// ---- Resolve type collisions with aliases ----
using MachineType = YasGMP.Models.MachineType;
using Manufacturer = YasGMP.Models.Manufacturer;
using LocationModel = YasGMP.Models.Location;              // <-- alias your model
using ResponsibleEntity = YasGMP.Models.ResponsibleEntity;
using MachineStatus = YasGMP.Models.MachineStatus;

namespace YasGMP.Views.Dialogs
{
    public partial class MachineEditDialog : ContentPage
    {
        private readonly TaskCompletionSource<bool> _tcs = new();
        public Task<bool> Result => _tcs.Task;
        public Machine Machine { get; }
        private readonly DatabaseService _db;

        public ObservableCollection<MachineType> TypeItems { get; } = new();
        public ObservableCollection<Manufacturer> ManufacturerItems { get; } = new();
        public ObservableCollection<LocationModel> LocationItems { get; } = new();   // <-- use alias
        public ObservableCollection<ResponsibleEntity> ResponsibleItems { get; } = new();
        public ObservableCollection<MachineStatus> StatusItems { get; } = new();

        public MachineEditDialog(Machine machine, DatabaseService db)
        {
            InitializeComponent();
            Machine = machine;
            _db = db;
            BindingContext = Machine;
            _ = LoadLookupsAsync();
        }

        private async Task LoadLookupsAsync()
        {
            TypeItems.Clear();
            foreach (var t in await _db.GetMachineTypesAsync()) TypeItems.Add(t);
            TypeItems.Add(new MachineType { Id = 0, Name = "Dodaj novi..." });
            TypePicker.ItemsSource = TypeItems;

            ManufacturerItems.Clear();
            foreach (var m in await _db.GetManufacturersAsync()) ManufacturerItems.Add(m);
            ManufacturerItems.Add(new Manufacturer { Id = 0, Name = "Dodaj novi..." });
            ManufacturerPicker.ItemsSource = ManufacturerItems;

            LocationItems.Clear();
            foreach (var l in await _db.GetLocationsAsync()) LocationItems.Add(l);
            LocationItems.Add(new LocationModel { Id = 0, Name = "Dodaj novi..." });  // <-- use alias
            LocationPicker.ItemsSource = LocationItems;

            ResponsibleItems.Clear();
            foreach (var r in await _db.GetResponsibleEntitiesAsync()) ResponsibleItems.Add(r);
            ResponsibleItems.Add(new ResponsibleEntity { Id = 0, Name = "Dodaj novi..." });
            ResponsiblePicker.ItemsSource = ResponsibleItems;

            StatusItems.Clear();
            foreach (var s in await _db.GetMachineStatusesAsync()) StatusItems.Add(s);
            StatusItems.Add(new MachineStatus { Id = 0, Name = "Dodaj novi..." });
            StatusPicker.ItemsSource = StatusItems;
        }

        private async void OnTypeChanged(object? sender, EventArgs e)
            => await HandleAddNewAsync(TypePicker, TypeItems, name => _db.AddMachineTypeAsync(name), v => Machine.MachineType = v);

        private async void OnManufacturerChanged(object? sender, EventArgs e)
            => await HandleAddNewAsync(ManufacturerPicker, ManufacturerItems, name => _db.AddManufacturerAsync(name), v => Machine.Manufacturer = v);

        private async void OnLocationChanged(object? sender, EventArgs e)
            => await HandleAddNewAsync(LocationPicker, LocationItems, name => _db.AddLocationAsync(name), v => Machine.Location = v);

        private async void OnResponsibleChanged(object? sender, EventArgs e)
            => await HandleAddNewAsync(ResponsiblePicker, ResponsibleItems, name => _db.AddResponsibleEntityAsync(name), v => Machine.ResponsibleEntity = v);

        private async void OnStatusChanged(object? sender, EventArgs e)
            => await HandleAddNewAsync(StatusPicker, StatusItems, name => _db.AddMachineStatusAsync(name), v => Machine.Status = v);

        private async Task HandleAddNewAsync<T>(Picker picker, ObservableCollection<T> list, Func<string, Task<int>> addFunc, Action<string> assign)
            where T : class, new()
        {
            var selected = picker.SelectedItem;
            if (selected is null)
                return;

            dynamic item = selected;
            if (item.Id == 0)
            {
                var entry = await DisplayPromptAsync(picker.Title, "Unesi novu vrijednost");
                if (!string.IsNullOrWhiteSpace(entry))
                {
                    int id = await addFunc(entry);
                    var newItem = new T();
                    dynamic dynItem = newItem;
                    dynItem.Id = id;
                    dynItem.Name = entry;
                    list.Insert(list.Count - 1, newItem);
                    picker.SelectedItem = newItem;
                    assign(entry);
                }
                else picker.SelectedIndex = -1;
            }
            else
            {
                assign((string)item.Name);
            }
        }

        private async void OnPickDocumentClicked(object? sender, EventArgs e)
        {
            var file = await FilePicker.Default.PickAsync();
            if (file != null)
            {
                Machine.UrsDoc = file.FullPath;
            }
        }

        private async void OnOpenQrClicked(object? sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(Machine.QrCode) && File.Exists(Machine.QrCode))
            {
                await Launcher.OpenAsync(new OpenFileRequest
                {
                    File = new ReadOnlyFile(Machine.QrCode)
                });
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
