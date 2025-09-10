using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using YasGMP.Models;
using YasGMP.Services;

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
        public ObservableCollection<Location> LocationItems { get; } = new();
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
            LocationItems.Add(new Location { Id = 0, Name = "Dodaj novi..." });
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
            => await HandleAddNewAsync(TypePicker, TypeItems, _db.AddMachineTypeAsync, v => Machine.MachineType = v);

        private async void OnManufacturerChanged(object? sender, EventArgs e)
            => await HandleAddNewAsync(ManufacturerPicker, ManufacturerItems, _db.AddManufacturerAsync, v => Machine.Manufacturer = v);

        private async void OnLocationChanged(object? sender, EventArgs e)
            => await HandleAddNewAsync(LocationPicker, LocationItems, _db.AddLocationAsync, v => Machine.Location = v);

        private async void OnResponsibleChanged(object? sender, EventArgs e)
            => await HandleAddNewAsync(ResponsiblePicker, ResponsibleItems, _db.AddResponsibleEntityAsync, v => Machine.ResponsibleEntity = v);

        private async void OnStatusChanged(object? sender, EventArgs e)
            => await HandleAddNewAsync(StatusPicker, StatusItems, _db.AddMachineStatusAsync, v => Machine.Status = v);

        private async Task HandleAddNewAsync<T>(Picker picker, ObservableCollection<T> list, Func<string, Task<int>> addFunc, Action<string> assign)
            where T : class
        {
            if (picker.SelectedItem is dynamic item && item.Id == 0)
            {
                var entry = await DisplayPromptAsync(picker.Title, "Unesi novu vrijednost");
                if (!string.IsNullOrWhiteSpace(entry))
                {
                    int id = await addFunc(entry);
                    var newItem = Activator.CreateInstance<T>();
                    if (newItem is MachineType mt) { mt.Id = id; mt.Name = entry; }
                    else if (newItem is Manufacturer mf) { mf.Id = id; mf.Name = entry; }
                    else if (newItem is Location loc) { loc.Id = id; loc.Name = entry; }
                    else if (newItem is ResponsibleEntity re) { re.Id = id; re.Name = entry; }
                    else if (newItem is MachineStatus st) { st.Id = id; st.Name = entry; }
                    list.Insert(list.Count - 1, (T)(object)newItem);
                    picker.SelectedItem = newItem;
                    assign(entry);
                }
                else picker.SelectedIndex = -1;
            }
            else if (picker.SelectedItem is dynamic existing)
            {
                assign(existing.Name);
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
