using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.Views.Dialogs
{
    public partial class WorkOrderEditDialog : ContentPage
    {
        private readonly DatabaseService _db;
        private readonly DocumentService _docs;
        private readonly int _currentUserId;

        public WorkOrder WorkOrder { get; }
        public TaskCompletionSource<bool> _tcs = new();
        public Task<bool> Result => _tcs.Task;

        private List<(string name, int id)> _machines = new();
        private List<(string name, int id)> _components = new();
        private List<(string name, int id)> _users = new();

        public WorkOrderEditDialog(WorkOrder wo, DatabaseService db, int currentUserId)
        {
            InitializeComponent();
            WorkOrder = wo;
            _db = db;
            _docs = new DocumentService(db);
            _currentUserId = currentUserId;
            BindingContext = WorkOrder;
            _ = LoadLookupsAsync();
        }

        private async Task LoadLookupsAsync()
        {
            try
            {
                var types = new[] { "korektivni", "preventivni", "vanredni" };
                var prios = new[] { "nizak", "srednji", "visok", "kritican" };
                var stats = new[] { "otvoren", "u_tijeku", "zavrsen", "odbijen", "planiran" };
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    TypePicker.ItemsSource = types;
                    PriorityPicker.ItemsSource = prios;
                    StatusPicker.ItemsSource = stats;
                });
                // Enhance pickers with inline "Dodaj novi…"
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // Type
                    if (TypePicker.ItemsSource is IEnumerable<string> titems)
                    {
                        var list = new List<string> { "Dodaj noviâ€¦" };
                        list.AddRange(titems);
                        TypePicker.ItemsSource = (System.Collections.IList)list;
                        if (!string.IsNullOrWhiteSpace(WorkOrder.Type))
                        {
                            var idx = list.FindIndex(x => string.Equals(x, WorkOrder.Type, StringComparison.OrdinalIgnoreCase));
                            if (idx >= 0) TypePicker.SelectedIndex = idx;
                        }
                    }
                    // Priority
                    if (PriorityPicker.ItemsSource is IEnumerable<string> pitems)
                    {
                        var list = new List<string> { "Dodaj noviâ€¦" };
                        list.AddRange(pitems);
                        PriorityPicker.ItemsSource = (System.Collections.IList)list;
                        if (!string.IsNullOrWhiteSpace(WorkOrder.Priority))
                        {
                            var idx = list.FindIndex(x => string.Equals(x, WorkOrder.Priority, StringComparison.OrdinalIgnoreCase));
                            if (idx >= 0) PriorityPicker.SelectedIndex = idx;
                        }
                    }
                    // Status
                    if (StatusPicker.ItemsSource is IEnumerable<string> sitems)
                    {
                        var list = new List<string> { "Dodaj noviâ€¦" };
                        list.AddRange(sitems);
                        StatusPicker.ItemsSource = (System.Collections.IList)list;
                        if (!string.IsNullOrWhiteSpace(WorkOrder.Status))
                        {
                            var idx = list.FindIndex(x => string.Equals(x, WorkOrder.Status, StringComparison.OrdinalIgnoreCase));
                            if (idx >= 0) StatusPicker.SelectedIndex = idx;
                        }
                    }
                });

                var dtM = await _db.ExecuteSelectAsync("SELECT id, name FROM machines ORDER BY name");
                _machines = dtM.Rows.Cast<System.Data.DataRow>()
                    .Select(r => (r["name"]?.ToString() ?? string.Empty, Convert.ToInt32(r["id"])) ).ToList();
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    MachinePicker.ItemsSource = _machines.Select(t => t.name).ToList();
                    if (WorkOrder.MachineId > 0)
                    {
                        var idx = _machines.FindIndex(x => x.id == WorkOrder.MachineId);
                        if (idx >= 0) MachinePicker.SelectedIndex = idx;
                    }
                });

                if (WorkOrder.MachineId > 0)
                    await LoadComponentsAsync(WorkOrder.MachineId);

                // Users for AssignedTo
                await LoadUsersAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Greška", ex.Message, "OK");
            }
        }

        private async Task LoadComponentsAsync(int machineId)
        {
            var dtC = await _db.ExecuteSelectAsync("SELECT id, name FROM machine_components WHERE machine_id=@m ORDER BY name",
                new[] { new MySqlConnector.MySqlParameter("@m", machineId) });
            _components = dtC.Rows.Cast<System.Data.DataRow>()
                .Select(r => (r["name"]?.ToString() ?? string.Empty, Convert.ToInt32(r["id"])) ).ToList();
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ComponentPicker.ItemsSource = _components.Select(t => t.name).ToList();
                if (WorkOrder.ComponentId.HasValue)
                {
                    var idx = _components.FindIndex(x => x.id == WorkOrder.ComponentId.Value);
                    if (idx >= 0) ComponentPicker.SelectedIndex = idx;
                }
            });
        }

        private async Task LoadUsersAsync()
        {
            var dtU = await _db.ExecuteSelectAsync("SELECT id, full_name FROM users WHERE active=1 ORDER BY full_name");
            _users = dtU.Rows.Cast<System.Data.DataRow>()
                .Select(r => (r["full_name"]?.ToString() ?? string.Empty, Convert.ToInt32(r["id"])) ).ToList();
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var names = new List<string> { "Dodaj novogâ€¦" };
                names.AddRange(_users.Select(u => u.name));
                UserPicker.ItemsSource = names;
                if (WorkOrder.AssignedToId > 0)
                {
                    var idx = _users.FindIndex(x => x.id == WorkOrder.AssignedToId);
                    UserPicker.SelectedIndex = idx >= 0 ? idx + 1 : 0;
                }
            });
        }

        private async void OnMachineChanged(object? sender, EventArgs e)
        {
            if (MachinePicker.SelectedIndex >= 0 && MachinePicker.SelectedIndex < _machines.Count)
            {
                var sel = _machines[MachinePicker.SelectedIndex];
                WorkOrder.MachineId = sel.id;
                await LoadComponentsAsync(sel.id);
            }
        }

        private async void OnSignClicked(object? sender, EventArgs e)
        {
            string? reason = await DisplayPromptAsync("Potpis", "Razlog/napomena potpisa:");
            string payload = $"WO:{WorkOrder.Id}|{WorkOrder.Title}|{DateTime.UtcNow:O}|{reason}";
            using var sha = System.Security.Cryptography.SHA256.Create();
            var hash = Convert.ToBase64String(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload)));
            await _db.AddWorkOrderSignatureAsync(WorkOrder.Id, _currentUserId, hash, reason);
            await DisplayAlert("OK", "Potpis spremljen.", "Zatvori");
        }

        private async Task AddPhotosAsync(string kind)
        {
            try
            {
                var files = await FilePicker.PickMultipleAsync();
                if (files == null) return;
                foreach (var f in files)
                {
                    using var fs = File.OpenRead(f.FullPath);
                    await _db.AttachWorkOrderPhotoAsync(WorkOrder.Id, fs, Path.GetFileName(f.FullPath), kind, _currentUserId);
                }
                await DisplayAlert("OK", "Slike dodane.", "Zatvori");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Greška", ex.Message, "OK");
            }
        }

        private async void OnAddBeforePhotosClicked(object? sender, EventArgs e) => await AddPhotosAsync("before");
        private async void OnAddAfterPhotosClicked(object? sender, EventArgs e) => await AddPhotosAsync("after");

        private async void OnSaveClicked(object? sender, EventArgs e)
        {
            if (UserPicker.SelectedIndex > 0 && (UserPicker.SelectedIndex - 1) < _users.Count)
            {
                WorkOrder.AssignedToId = _users[UserPicker.SelectedIndex - 1].id;
            }
            _tcs.TrySetResult(true);
            await Navigation.PopModalAsync();
        }

        private async void OnCancelClicked(object? sender, EventArgs e)
        {
            _tcs.TrySetResult(false);
            await Navigation.PopModalAsync();
        }

        private async void OnTypeChanged(object? sender, EventArgs e)
        {
            if (TypePicker.SelectedIndex == 0)
            {
                var val = await DisplayPromptAsync("Novi tip", "Unesite naziv tipa naloga:");
                if (!string.IsNullOrWhiteSpace(val))
                {
                    var list = (TypePicker.ItemsSource as IList<string>) ?? new List<string>();
                    list.Add(val);
                    TypePicker.ItemsSource = (System.Collections.IList)list;
                    var idx = list.IndexOf(val);
                    TypePicker.SelectedIndex = idx >= 0 ? idx : 1;
                    WorkOrder.Type = val;
                }
                else
                {
                    TypePicker.SelectedIndex = 1;
                }
            }
            else if (TypePicker.SelectedIndex > 0)
            {
                WorkOrder.Type = (TypePicker.SelectedItem?.ToString() ?? string.Empty);
            }
        }

        private async void OnPriorityChanged(object? sender, EventArgs e)
        {
            if (PriorityPicker.SelectedIndex == 0)
            {
                var val = await DisplayPromptAsync("Novi prioritet", "Unesite naziv prioriteta:");
                if (!string.IsNullOrWhiteSpace(val))
                {
                    var list = (PriorityPicker.ItemsSource as IList<string>) ?? new List<string>();
                    list.Add(val);
                    PriorityPicker.ItemsSource = (System.Collections.IList)list;
                    var idx = list.IndexOf(val);
                    PriorityPicker.SelectedIndex = idx >= 0 ? idx : 1;
                    WorkOrder.Priority = val;
                }
                else
                {
                    PriorityPicker.SelectedIndex = 1;
                }
            }
            else if (PriorityPicker.SelectedIndex > 0)
            {
                WorkOrder.Priority = (PriorityPicker.SelectedItem?.ToString() ?? string.Empty);
            }
        }

        private async void OnStatusChanged(object? sender, EventArgs e)
        {
            if (StatusPicker.SelectedIndex == 0)
            {
                var val = await DisplayPromptAsync("Novi status", "Unesite naziv statusa:");
                if (!string.IsNullOrWhiteSpace(val))
                {
                    var list = (StatusPicker.ItemsSource as IList<string>) ?? new List<string>();
                    list.Add(val);
                    StatusPicker.ItemsSource = (System.Collections.IList)list;
                    var idx = list.IndexOf(val);
                    StatusPicker.SelectedIndex = idx >= 0 ? idx : 1;
                    WorkOrder.Status = val;
                }
                else
                {
                    StatusPicker.SelectedIndex = 1;
                }
            }
            else if (StatusPicker.SelectedIndex > 0)
            {
                WorkOrder.Status = (StatusPicker.SelectedItem?.ToString() ?? string.Empty);
            }
        }

        private async void OnUserChanged(object? sender, EventArgs e)
        {
            if (UserPicker.SelectedIndex == 0)
            {
                var fullName = await DisplayPromptAsync("Novi korisnik", "Puno ime tehni\u010Dara:");
                if (string.IsNullOrWhiteSpace(fullName)) { UserPicker.SelectedIndex = -1; return; }
                var username = await DisplayPromptAsync("Korisni\u010Dko ime", "Upi\u0161ite korisni\u010Dko ime (ostavite prazno za automatsko):");
                if (string.IsNullOrWhiteSpace(username))
                {
                    username = new string((fullName ?? string.Empty).ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
                    if (string.IsNullOrWhiteSpace(username)) username = $"user{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
                }
                try
                {
                    const string sql = "INSERT INTO users (username, password, full_name, role, active) VALUES (@u,@p,@f,'tehni\u010Dar',1)";
                    var pars = new[]
                    {
                        new MySqlConnector.MySqlParameter("@u", username),
                        new MySqlConnector.MySqlParameter("@p", "!"),
                        new MySqlConnector.MySqlParameter("@f", fullName)
                    };
                    await _db.ExecuteNonQueryAsync(sql, pars).ConfigureAwait(false);
                    var idObj = await _db.ExecuteScalarAsync("SELECT LAST_INSERT_ID();").ConfigureAwait(false);
                    var newId = Convert.ToInt32(idObj);
                    await _db.LogSystemEventAsync(_currentUserId, "USER_CREATE", "users", "WorkOrders", newId, fullName, "ui", "audit", "WorkOrderEditDialog", null).ConfigureAwait(false);

                    await LoadUsersAsync();
                    var idx = _users.FindIndex(x => x.id == newId);
                    await MainThread.InvokeOnMainThreadAsync(() => { UserPicker.SelectedIndex = idx >= 0 ? idx + 1 : 0; });
                    WorkOrder.AssignedToId = newId;
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Gre\u0161ka", ex.Message, "OK");
                }
                return;
            }

            var si = UserPicker.SelectedIndex;
            if (si > 0 && (si - 1) < _users.Count)
            {
                var sel = _users[si - 1];
                WorkOrder.AssignedToId = sel.id;
            }
        }
    }
}


