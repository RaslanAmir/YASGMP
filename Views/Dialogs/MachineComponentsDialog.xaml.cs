using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using YasGMP.Common;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.Views.Dialogs
{
    /// <summary>
    /// Dialog that displays components associated with a machine for quick navigation.
    /// </summary>
    public partial class MachineComponentsDialog : ContentPage
    {
        private readonly TaskCompletionSource<bool> _tcs = new();
        /// <summary>
        /// Gets or sets the result.
        /// </summary>
        public Task<bool> Result => _tcs.Task;

        private readonly DatabaseService _db;
        private readonly int _machineId;
        /// <summary>
        /// Gets or sets the components.
        /// </summary>

        public ObservableCollection<ComponentRow> Components { get; } = new();
        /// <summary>
        /// Initializes a new instance of the MachineComponentsDialog class.
        /// </summary>

        public MachineComponentsDialog(DatabaseService db, int machineId)
        {
            InitializeComponent();
            _db = db; _machineId = machineId;
            TitleLabel.Text = $"Komponente stroja #{machineId}";
            CompList.ItemsSource = Components;
            _ = LoadAsync();
            _ = LoadTitleAsync();
        }

        private async Task LoadTitleAsync()
        {
            try
            {
                var m = await _db.GetMachineByIdAsync(_machineId).ConfigureAwait(false);
                if (m != null)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        TitleLabel.Text = $"Komponente: {m.Name} ({m.Code})";
                    });
                }
            }
            catch { }
        }

        private async Task LoadAsync()
        {
            var list = await _db.GetComponentsByMachineIdAsync(_machineId).ConfigureAwait(false);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Components.Clear();

                // Preload document counts for all components
                var ids = new System.Collections.Generic.List<int>(list.Count);
                foreach (var c in list) ids.Add(c.Id);
                var counts = new System.Collections.Generic.Dictionary<int, int>();
                try
                {
                    if (ids.Count > 0)
                    {
                        var pars = new System.Collections.Generic.List<MySqlConnector.MySqlParameter> { new MySqlConnector.MySqlParameter("@et", "Component") };
                        var inClauses = new System.Collections.Generic.List<string>(ids.Count);
                        for (int i = 0; i < ids.Count; i++) { string n = "@id" + i; inClauses.Add(n); pars.Add(new MySqlConnector.MySqlParameter(n, ids[i])); }
                        string sqlCnt = $"SELECT entity_id, COUNT(*) AS cnt FROM document_links WHERE entity_type=@et AND entity_id IN ({string.Join(",", inClauses)}) GROUP BY entity_id";
                        var dtCnt = _db.ExecuteSelectAsync(sqlCnt, pars).GetAwaiter().GetResult();
                        foreach (System.Data.DataRow r in dtCnt.Rows)
                        {
                            int eid = r["entity_id"] == DBNull.Value ? 0 : Convert.ToInt32(r["entity_id"]);
                            int cnt = r["cnt"] == DBNull.Value ? 0 : Convert.ToInt32(r["cnt"]);
                            counts[eid] = cnt;
                        }
                    }
                }
                catch { }

                foreach (var c in list)
                    Components.Add(new ComponentRow(_db, c, OnChanged, counts.ContainsKey(c.Id) ? counts[c.Id] : 0));
            });
        }

        private async Task OnChanged() => await LoadAsync();

        private async void OnAddClicked(object? sender, EventArgs e)
        {
            try
            {
                var code = CodeEntry.Text?.Trim();
                var name = NameEntry.Text?.Trim();
                var type = TypeEntry.Text?.Trim();
                if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
                {
                    await DisplayAlert("Napomena", "Unesite šifru i naziv.", "OK");
                    return;
                }
                var mc = new MachineComponent { MachineId = _machineId, Code = code!, Name = name!, Type = type };

                var app = Application.Current as App;
                int userId = app?.LoggedUser?.Id ?? 0;
                string ip = ServiceLocator.GetService<IPlatformService>()?.GetLocalIpAddress() ?? string.Empty;
                string? sessionId = app?.SessionId;
                await _db.InsertOrUpdateComponentAsync(mc, update: false, actorUserId: userId, ip: ip, deviceInfo: "MachineComponentsDialog", sessionId: sessionId);

                CodeEntry.Text = string.Empty; NameEntry.Text = string.Empty; TypeEntry.Text = string.Empty;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Greška", ex.Message, "OK");
            }
        }

        private async void OnLinkExistingClicked(object? sender, EventArgs e)
        {
            try
            {
                var unassigned = await _db.GetUnassignedComponentsAsync();
                if (unassigned.Count == 0)
                {
                    await DisplayAlert("Info", "Nema dostupnih nevezanih komponenti.", "OK");
                    return;
                }

                var options = new string[unassigned.Count];
                for (int i = 0; i < unassigned.Count; i++)
                {
                    var c = unassigned[i];
                    options[i] = $"[{c.Id}] {c.Code} — {c.Name}";
                }

                var pick = await DisplayActionSheet("Poveži postojeću komponentu", "Odustani", null, options);
                if (string.IsNullOrWhiteSpace(pick) || pick == "Odustani") return;

                int lb = pick.IndexOf('[');
                int rb = pick.IndexOf(']');
                if (lb >= 0 && rb > lb)
                {
                    var idStr = pick.Substring(lb + 1, rb - lb - 1);
                    if (int.TryParse(idStr, out int compId))
                    {
                        var comp = await _db.GetComponentByIdAsync(compId);
                        if (comp is null) return;
                        comp.MachineId = _machineId;

                        var app = Application.Current as App;
                        int userId = app?.LoggedUser?.Id ?? 0;
                        string ip = ServiceLocator.GetService<IPlatformService>()?.GetLocalIpAddress() ?? string.Empty;
                        string? sessionId = app?.SessionId;
                        await _db.InsertOrUpdateComponentAsync(comp, update: true, actorUserId: userId, ip: ip, deviceInfo: "MachineComponentsDialog", sessionId: sessionId);
                        await LoadAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Greška", ex.Message, "OK");
            }
        }

        private async void OnCloseClicked(object? sender, EventArgs e)
        {
            _tcs.TrySetResult(true);
            await Navigation.PopModalAsync();
        }
        /// <summary>
        /// Represents the Component Row.
        /// </summary>

        public sealed class ComponentRow
        {
            private readonly DatabaseService _db;
            private readonly MachineComponent _mc;
            private readonly Func<Task> _onChanged;
            /// <summary>
            /// Executes the name operation.
            /// </summary>

            public string Name => string.IsNullOrWhiteSpace(_mc?.Name) ? _mc?.Code ?? string.Empty : _mc!.Name;
            /// <summary>
            /// Gets or sets the calibrate command.
            /// </summary>
            public Command CalibrateCommand { get; }
            /// <summary>
            /// Gets or sets the calibrate command2.
            /// </summary>
            public Command CalibrateCommand2 { get; }
            /// <summary>
            /// Gets or sets the edit command.
            /// </summary>
            public Command EditCommand { get; }
            /// <summary>
            /// Gets or sets the remove command.
            /// </summary>
            public Command RemoveCommand { get; }
            /// <summary>
            /// Gets or sets the remove or detach command.
            /// </summary>
            public Command RemoveOrDetachCommand { get; }
            /// <summary>
            /// Gets or sets the docs command.
            /// </summary>
            public Command DocsCommand { get; }
            /// <summary>
            /// Gets or sets the docs count.
            /// </summary>
            public int DocsCount { get; private set; }
            /// <summary>
            /// Initializes a new instance of the ComponentRow class.
            /// </summary>

            public ComponentRow(DatabaseService db, MachineComponent mc, Func<Task> onChanged, int docsCount)
            {
                _db = db; _mc = mc; _onChanged = onChanged; DocsCount = docsCount;
                CalibrateCommand = new Command(async () => await CalibrateAsync());
                CalibrateCommand2 = new Command(async () => await CalibrateNavigateAsync());
                EditCommand = new Command(async () => await EditAsync());
                RemoveCommand = new Command(async () => await RemoveAsync());
                RemoveOrDetachCommand = new Command(async () => await RemoveOrDetachAsync());
                DocsCommand = new Command(async () => await OpenDocsAsync());
            }

            private async Task CalibrateAsync()
            {
                try
                {
                    await Application.Current?.MainPage.DisplayAlert("Kalibracija", $"Pokreni kalibraciju za komponentu: {_mc.Name}", "OK");
                }
                catch { }
            }

            private async Task CalibrateNavigateAsync()
            {
                try
                {
                    var choice = await Application.Current?.MainPage.DisplayActionSheet(
                        $"Kalibracija — {_mc.Name}", "Zatvori", null,
                        "Pregled kalibracija", "Nova kalibracija");
                    if (string.IsNullOrWhiteSpace(choice) || choice == "Zatvori") return;

                    var page = new YasGMP.Views.CalibrationsPage();
                    page.SetFilter(null, _mc, null, null);
                    if (Application.Current?.MainPage?.Navigation != null)
                        await Application.Current.MainPage.Navigation.PushModalAsync(page);
                }
                catch { }
            }

            private async Task EditAsync()
            {
                try
                {
                    var snapshot = new MachineComponent
                    {
                        Id = _mc.Id,
                        MachineId = _mc.MachineId,
                        Code = _mc.Code,
                        Name = _mc.Name,
                        Type = _mc.Type,
                        Model = _mc.Model,
                        Status = _mc.Status,
                        Note = _mc.Note
                    };
                    var dlg = new ComponentEditDialog(snapshot);
                    if (Application.Current?.MainPage?.Navigation != null)
                        await Application.Current.MainPage.Navigation.PushModalAsync(dlg);
                    var ok = await dlg.Result;
                    if (!ok) return;

                    _mc.Code = snapshot.Code;
                    _mc.Name = snapshot.Name;
                    _mc.Type = snapshot.Type;
                    _mc.Model = snapshot.Model;
                    _mc.Status = snapshot.Status;
                    _mc.Note = snapshot.Note;

                    var app = Application.Current as App;
                    int userId = app?.LoggedUser?.Id ?? 0;
                    string ip = ServiceLocator.GetService<IPlatformService>()?.GetLocalIpAddress() ?? string.Empty;
                    string? sessionId = app?.SessionId;
                    await _db.InsertOrUpdateComponentAsync(_mc, update: true, actorUserId: userId, ip: ip, deviceInfo: "MachineComponentsDialog", sessionId: sessionId);
                    await _onChanged();
                }
                catch (Exception ex)
                {
                    await Application.Current?.MainPage.DisplayAlert("Greška", ex.Message, "OK");
                }
            }

            private async Task RemoveAsync()
            {
                try
                {
                    var app = Application.Current as App;
                    int userId = app?.LoggedUser?.Id ?? 0;
                    string ip = ServiceLocator.GetService<IPlatformService>()?.GetLocalIpAddress() ?? string.Empty;
                    string? sessionId = app?.SessionId;
                    await _db.DeleteComponentAsync(_mc.Id, userId, ip, "MachineComponentsDialog", sessionId);
                    await _onChanged();
                }
                catch (Exception ex)
                {
                    await Application.Current?.MainPage.DisplayAlert("Greška", ex.Message, "OK");
                }
            }

            private async Task RemoveOrDetachAsync()
            {
                try
                {
                    var choice = await Application.Current?.MainPage.DisplayActionSheet(
                        "Ukloni komponentu", "Odustani", null, "Odspoji od stroja", "Obriši komponentu");
                    if (string.IsNullOrWhiteSpace(choice) || choice == "Odustani") return;

                    var app = Application.Current as App;
                    int userId = app?.LoggedUser?.Id ?? 0;
                    string ip = ServiceLocator.GetService<IPlatformService>()?.GetLocalIpAddress() ?? string.Empty;
                    string? sessionId = app?.SessionId;

                    if (choice == "Odspoji od stroja")
                    {
                        _mc.MachineId = null;
                        await _db.InsertOrUpdateComponentAsync(_mc, update: true, actorUserId: userId, ip: ip, deviceInfo: "MachineComponentsDialog", sessionId: sessionId);
                        await _onChanged();
                        return;
                    }

                    if (choice == "Obriši komponentu")
                    {
                        await _db.DeleteComponentAsync(_mc.Id, userId, ip, "MachineComponentsDialog", sessionId);
                        await _onChanged();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    await Application.Current?.MainPage.DisplayAlert("Greška", ex.Message, "OK");
                }
            }

            private async Task OpenDocsAsync()
            {
                try
                {
                    var dlg = new ComponentDocumentsDialog(_db, _mc.Id);
                    if (Application.Current?.MainPage?.Navigation != null)
                        await Application.Current.MainPage.Navigation.PushModalAsync(dlg);
                    _ = await dlg.Result;
                }
                catch (Exception ex)
                {
                    await Application.Current?.MainPage.DisplayAlert("Greška", ex.Message, "OK");
                }
            }
        }
    }
}
