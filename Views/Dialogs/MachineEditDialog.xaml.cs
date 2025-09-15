using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Helpers;
// ---- Resolve type collisions with aliases ----
using MachineType = YasGMP.Models.MachineType;
using Manufacturer = YasGMP.Models.Manufacturer;
using LocationModel = YasGMP.Models.Location;
using ResponsibleParty = YasGMP.Models.ResponsibleParty;
using MachineStatus = YasGMP.Models.MachineStatus;

namespace YasGMP.Views.Dialogs
{
    public partial class MachineEditDialog : ContentPage
    {
        private readonly TaskCompletionSource<bool> _tcs = new();
        public Task<bool> Result => _tcs.Task;
        public Machine Machine { get; }
        private readonly DatabaseService _db;
        private readonly CodeGeneratorService _codeGen = new();

        public ObservableCollection<MachineType> TypeItems { get; } = new();
        public ObservableCollection<Manufacturer> ManufacturerItems { get; } = new();
        public ObservableCollection<LocationModel> LocationItems { get; } = new();
        public ObservableCollection<ResponsibleParty> ResponsibleItems { get; } = new();
        public ObservableCollection<MachineStatus> StatusItems { get; } = new();

        public MachineEditDialog(Machine machine, DatabaseService db)
        {
            InitializeComponent();
            Machine = machine;
            _db = db;
            BindingContext = Machine;
            _ = LoadLookupsAsync();
    }

        private static void TrySelectByName<T>(Picker picker, ObservableCollection<T> items, string? value)
            where T : class, new()
        {
            try
            {
                if (picker == null || items == null) return;
                string name = (value ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(name)) { picker.SelectedIndex = -1; return; }

                foreach (var it in items)
                {
                    dynamic d = it!;
                    string n = d?.Name ?? string.Empty;
                    if (string.Equals(n, name, StringComparison.OrdinalIgnoreCase))
                    {
                        picker.SelectedItem = it;
                        return;
                    }
                }

                // Not found in lookup list: create a virtual item and select it
                var add = new T();
                dynamic nd = add;
                try { nd.Id = -1; } catch { }
                nd.Name = name;
                int insertAt = Math.Max(items.Count - 1, 0); // before "Dodaj novi..."
                items.Insert(insertAt, add);
                picker.SelectedItem = add;
            }
            catch { }
        }

        private async Task LoadLookupsAsync()
        {
            try
            {
                // fetch on background thread
                var types = await _db.GetMachineTypesAsync().ConfigureAwait(false);
                var mans  = await _db.GetManufacturersAsync().ConfigureAwait(false);
                var locs  = await _db.GetLocationsAsync().ConfigureAwait(false);
                var reps  = await _db.GetResponsiblePartiesAsync().ConfigureAwait(false);
                var stats = await _db.GetMachineStatusesAsync().ConfigureAwait(false);

                // update UI on main thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    TypeItems.Clear();
                    foreach (var t in types) TypeItems.Add(t);
                    TypeItems.Add(new MachineType { Id = 0, Name = "Dodaj novi..." });
                    TypePicker.ItemsSource = TypeItems;

                    ManufacturerItems.Clear();
                    foreach (var m in mans) ManufacturerItems.Add(m);
                    ManufacturerItems.Add(new Manufacturer { Id = 0, Name = "Dodaj novi..." });
                    ManufacturerPicker.ItemsSource = ManufacturerItems;

                    LocationItems.Clear();
                    foreach (var l in locs) LocationItems.Add(l);
                    LocationItems.Add(new LocationModel { Id = 0, Name = "Dodaj novi..." });
                    LocationPicker.ItemsSource = LocationItems;

                    ResponsibleItems.Clear();
                    foreach (var r in reps) ResponsibleItems.Add(r);
                    ResponsibleItems.Add(new ResponsibleParty { Id = 0, Name = "Dodaj novi..." });
                    ResponsiblePicker.ItemsSource = ResponsibleItems;

                    StatusItems.Clear();
                    foreach (var s in stats) StatusItems.Add(s);
                    StatusItems.Add(new MachineStatus { Id = 0, Name = "Dodaj novi..." });
                    StatusPicker.ItemsSource = StatusItems;

                    // Preselect values based on current Machine fields
                    TrySelectByName(TypePicker,         TypeItems,         Machine.MachineType);
                    TrySelectByName(ManufacturerPicker, ManufacturerItems, Machine.Manufacturer);
                    TrySelectByName(LocationPicker,     LocationItems,     Machine.Location);
                    TrySelectByName(ResponsiblePicker,  ResponsibleItems,  Machine.ResponsibleParty);
                    TrySelectByName(StatusPicker,       StatusItems,       Machine.Status);
                });
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                    DisplayAlert("Greška", ex.Message, "OK")
                );
            }
        }

        private async void OnTypeChanged(object? sender, EventArgs e)
            => await HandleAddNewAsync(TypePicker, TypeItems, name => _db.AddMachineTypeAsync(name), v => Machine.MachineType = v);

        private async void OnManufacturerChanged(object? sender, EventArgs e)
            => await HandleAddNewAsync(ManufacturerPicker, ManufacturerItems, name => _db.AddManufacturerAsync(name), v => Machine.Manufacturer = v);

        private async void OnLocationChanged(object? sender, EventArgs e)
            => await HandleAddNewAsync(LocationPicker, LocationItems, name => _db.AddLocationAsync(name), v => Machine.Location = v);

        private async void OnResponsibleChanged(object? sender, EventArgs e)
            => await HandleAddNewAsync(ResponsiblePicker, ResponsibleItems, name => _db.AddResponsiblePartyAsync(name), v => Machine.ResponsibleParty = v);

        private async void OnStatusChanged(object? sender, EventArgs e)
            => await HandleAddNewAsync(StatusPicker, StatusItems, name => _db.AddMachineStatusAsync(name), v => Machine.Status = v);

        private async Task HandleAddNewAsync<T>(
            Picker picker,
            ObservableCollection<T> list,
            Func<string, Task<int>> addFunc,
            Action<string> assign)
            where T : class, new()
        {
            var selected = picker.SelectedItem;
            if (selected is null) return;

            dynamic item = selected;
            if (item.Id == 0)
            {
                var entry = await DisplayPromptAsync(picker.Title, "Unesi novu vrijednost");
                if (!string.IsNullOrWhiteSpace(entry))
                {
                    // DB insert (may resume on background thread)
                    int id = await addFunc(entry).ConfigureAwait(false);

                    // UI updates must be on main thread
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        var newItem = new T();
                        dynamic dynItem = newItem;
                        dynItem.Id = id;
                        dynItem.Name = entry;
                        list.Insert(list.Count - 1, newItem);
                        picker.SelectedItem = newItem;
                        assign(entry);
                    });
                }
                else
                {
                    await MainThread.InvokeOnMainThreadAsync(() => picker.SelectedIndex = -1);
                }
            }
            else
            {
                assign((string)item.Name);
            }
        }

        private async void OnPickDocumentsClicked(object? sender, EventArgs e)
        {
            try
            {
                var files = await FilePicker.PickMultipleAsync();
                if (files != null)
                {
                    foreach (var f in files)
                    {
                        if (!string.IsNullOrWhiteSpace(f?.FullPath))
                            Machine.LinkedDocuments.Add(f.FullPath);
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Greška", ex.Message, "OK");
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

        private void OnAutoCodeClicked(object? sender, EventArgs e)
        {
            Machine.Code = _codeGen.GenerateMachineCode(Machine.Name, Machine.Manufacturer);
        }

        private async void OnSaveClicked(object? sender, EventArgs e)
        {
            try
            {
                // Ensure required fields minimally
                if (string.IsNullOrWhiteSpace(Machine.Name))
                {
                    await DisplayAlert("Napomena", "Naziv stroja je obavezan.", "OK");
                    return;
                }

                // Ensure code exists before closing the dialog (UI-level safety)
                if (string.IsNullOrWhiteSpace(Machine.Code))
                {
                    var gen = new CodeGeneratorService();
                    Machine.Code = gen.GenerateMachineCode(Machine.Name, Machine.Manufacturer);
                    if (string.IsNullOrWhiteSpace(Machine.Code))
                        Machine.Code = $"MCH-AUTO-{DateTime.UtcNow:yyyyMMddHHmmss}";
                }

                _tcs.TrySetResult(true);
                await Navigation.PopModalAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Greška", ex.Message, "OK");
            }
        }

        private async void OnCancelClicked(object? sender, EventArgs e)
        {
            _tcs.TrySetResult(false);
            await Navigation.PopModalAsync();
        }
    }
}
