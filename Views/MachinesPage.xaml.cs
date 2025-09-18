// ==============================================================================
//  File: Views/MachinesPage.xaml.cs
//  Project: YasGMP
//  Summary:
//      Ekran za pregled/unos/uređivanje/brisanje strojeva/opreme (MySQL).
// ==============================================================================
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using MySqlConnector;
using YasGMP.Common;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Views.Dialogs;

namespace YasGMP.Views
{
    public partial class MachinesPage : ContentPage
    {
        public ObservableCollection<Machine> Machines { get; } = new();
        public Machine? SelectedMachine { get; set; }
        private List<Machine> _allMachines = new();
        private readonly DatabaseService _dbService;
        private readonly CodeGeneratorService _codeService;
        private readonly QRCodeService _qrService;
        private readonly IAttachmentService _attachmentService;

        public MachinesPage(DatabaseService dbService, CodeGeneratorService codeService, QRCodeService qrService, IAttachmentService attachmentService)
        {
            InitializeComponent();

            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            _codeService = codeService ?? throw new ArgumentNullException(nameof(codeService));
            _qrService = qrService ?? throw new ArgumentNullException(nameof(qrService));
            _attachmentService = attachmentService ?? throw new ArgumentNullException(nameof(attachmentService));
            BindingContext = this;

            // One-time DB safety net: ensure triggers cannot null-out machines.code
            // Run safely and ignore permission issues for non-DBA users
            _ = EnsureTriggersSafeAsync();
            _ = LoadMachinesAsync();
        }

        /// <summary>Parameterless ctor for Shell/XAML. Resolves dependencies via ServiceLocator.</summary>
        public MachinesPage()
            : this(
                ServiceLocator.GetRequiredService<DatabaseService>(),
                ServiceLocator.GetRequiredService<CodeGeneratorService>(),
                ServiceLocator.GetRequiredService<QRCodeService>(),
                ServiceLocator.GetRequiredService<IAttachmentService>())
        {
        }

        private async Task EnsureTriggersSafeAsync()
        {
            try
            {
                await _dbService.EnsureMachineTriggersForMachinesAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Intentionally ignore: lower-privileged accounts cannot manage TRIGGERs.
                // The app can function without trigger reconciliation.
            }
        }

        private async Task LoadMachinesAsync()
        {
            try
            {
                var list = await _dbService.GetAllMachinesAsync().ConfigureAwait(false) ?? new List<Machine>();
                _allMachines = new List<Machine>(list);
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Machines.Clear();
                    foreach (var m in list) Machines.Add(m);
                });
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("Greška", $"Učitavanje strojeva nije uspjelo: {ex.Message}", "OK");
            }
        }

        private async void OnAddMachineClicked(object? sender, EventArgs e)
        {
            try
            {
                var newMachine = new Machine();
                var ok = await ShowMachineFormAsync(newMachine, "Unesi novi stroj");
                if (!ok) return;

                if (string.IsNullOrWhiteSpace(newMachine.Name))
                {
                    await SafeNavigator.ShowAlertAsync("Napomena", "Naziv stroja je obavezan.", "OK");
                    return;
                }

                if (string.IsNullOrWhiteSpace(newMachine.Code))
                    newMachine.Code = _codeService.GenerateMachineCode(newMachine.Name, newMachine.Manufacturer);
                if (string.IsNullOrWhiteSpace(newMachine.Code))
                    newMachine.Code = $"MCH-AUTO-{DateTime.UtcNow:yyyyMMddHHmmss}";

                newMachine.Status = NormalizeStatus(newMachine.Status);

                // Temporary debug assist: show the code we are about to persist
                try { await SafeNavigator.ShowAlertAsync("Debug", $"Code to save: '{newMachine.Code}'", "OK"); } catch { }

                int newId = await _dbService.InsertOrUpdateMachineAsync(newMachine, false, 0, "ui", "MachinesPage", null).ConfigureAwait(false);

                // Save any picked documents
                if (newMachine.LinkedDocuments?.Count > 0)
                {
                    foreach (var path in newMachine.LinkedDocuments)
                    {
                        try
                        {
                            using var fs = File.OpenRead(path);
                            string name = Path.GetFileName(path);
                            await _attachmentService.UploadAsync(fs, new AttachmentUploadRequest
                            {
                                FileName = name,
                                ContentType = null,
                                EntityType = "Machine",
                                EntityId = newId,
                                UploadedById = null,
                                Notes = "Machine document"
                            }).ConfigureAwait(false);
                        }
                        catch { }
                    }
                }

                // Generate QR image from payload if available
                var dt = await _dbService.ExecuteSelectAsync("SELECT COALESCE(qr_payload,'') AS p FROM machines WHERE id=@id", new[] { new MySqlParameter("@id", newId) }).ConfigureAwait(false);
                if (dt.Rows.Count == 1)
                {
                    string payload = dt.Rows[0]["p"]?.ToString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(payload))
                    {
                        using var stream = _qrService.GeneratePng(payload);
                        var dir = FileSystem.AppDataDirectory;
                        Directory.CreateDirectory(dir);
                        var pathPng = Path.Combine(dir, $"M_{newId}.png");
                        using var fsOut = File.Create(pathPng);
                        stream.CopyTo(fsOut);

                        await _dbService.ExecuteNonQueryAsync("UPDATE machines SET qr_code=@p WHERE id=@id", new[] { new MySqlParameter("@p", pathPng), new MySqlParameter("@id", newId) }).ConfigureAwait(false);
                    }
                }

                await LoadMachinesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("Greška", $"Spremanje novog stroja nije uspjelo: {ex.Message}", "OK");
            }
        }

        private async void OnEditMachineClicked(object? sender, EventArgs e)
        {
            try
            {
                if (SelectedMachine is not Machine selected)
                {
                    await SafeNavigator.ShowAlertAsync("Obavijest", "Molimo odaberite stroj iz liste za uređivanje.", "OK");
                    return;
                }

                var m = selected.DeepCopy();
                var ok = await ShowMachineFormAsync(m, "Uredi stroj");
                if (!ok) return;

                if (string.IsNullOrWhiteSpace(m.Code))
                    m.Code = _codeService.GenerateMachineCode(m.Name, m.Manufacturer);

                m.Status = NormalizeStatus(m.Status);

                await _dbService.InsertOrUpdateMachineAsync(m, true, 0, "ui", "MachinesPage", null).ConfigureAwait(false);

                // Persist newly picked documents (if any)
                if (m.LinkedDocuments?.Count > 0)
                {
                    foreach (var path in m.LinkedDocuments)
                    {
                        try
                        {
                            using var fs = File.OpenRead(path);
                            string name = Path.GetFileName(path);
                            await _attachmentService.UploadAsync(fs, new AttachmentUploadRequest
                            {
                                FileName = name,
                                ContentType = null,
                                EntityType = "Machine",
                                EntityId = m.Id,
                                UploadedById = null,
                                Notes = "Machine document"
                            }).ConfigureAwait(false);
                        }
                        catch { }
                    }
                }

                // Generate QR image if payload provided/updated
                if (!string.IsNullOrWhiteSpace(m.QrPayload))
                {
                    using var stream = _qrService.GeneratePng(m.QrPayload);
                    var dir = FileSystem.AppDataDirectory;
                    Directory.CreateDirectory(dir);
                    var pathPng = Path.Combine(dir, $"M_{m.Id}.png");
                    using var fsOut = File.Create(pathPng);
                    stream.CopyTo(fsOut);

                    await _dbService.ExecuteNonQueryAsync("UPDATE machines SET qr_code=@p WHERE id=@id", new[] { new MySqlParameter("@p", pathPng), new MySqlParameter("@id", m.Id) }).ConfigureAwait(false);
                }

                await LoadMachinesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("Greška", $"AĹľuriranje stroja nije uspjelo: {ex.Message}", "OK");
            }
        }

        private async void OnDeleteMachineClicked(object? sender, EventArgs e)
        {
            try
            {
                if (SelectedMachine is not Machine selected)
                {
                    await SafeNavigator.ShowAlertAsync("Obavijest", "Molimo odaberite stroj iz liste za brisanje.", "OK");
                    return;
                }

                var confirm = await SafeNavigator.ConfirmAsync("Potvrda brisanja", $"Želite li izbrisati stroj: {selected.Name}?", "Da", "Ne");
                if (!confirm) return;

                const string sql = "DELETE FROM machines WHERE id=@id";
                var pars = new[] { new MySqlParameter("@id", selected.Id) };

                await _dbService.ExecuteNonQueryAsync(sql, pars).ConfigureAwait(false);
                await LoadMachinesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("Greška", $"Brisanje stroja nije uspjelo: {ex.Message}", "OK");
            }
        }

        private async Task<bool> ShowMachineFormAsync(Machine machine, string title)
        {
            var dialog = new MachineEditDialog(machine, _dbService) { Title = title };
            await Navigation.PushModalAsync(dialog);
            return await dialog.Result;
        }

        private static string NormalizeStatus(string? raw)
        {
            string s = (raw ?? string.Empty).Trim().ToLowerInvariant();
            return s switch
            {
                "active" => "active",
                "maintenance" or "maint" or "service" => "maintenance",
                "decommissioned" or "decom" or "retired" => "decommissioned",
                "reserved" => "reserved",
                "scrapped" or "scrap" => "scrapped",
                "u pogonu" or "operativan" or "operational" => "active",
                "van pogona" or "neispravan" or "kvar" or "servis" => "maintenance",
                "otpisan" or "rashodovan" => "scrapped",
                "rezerviran" => "reserved",
                "dekomisioniran" => "decommissioned",
                _ => "active"
            };
        }

        private async void OnDocumentsClicked(object? sender, EventArgs e)
        {
            if (SelectedMachine is not Machine selected)
            {
                await SafeNavigator.ShowAlertAsync("Obavijest", "Odaberite stroj kako biste vidjeli/pridruĹľili dokumente.", "OK");
                return;
            }

            var dlg = new YasGMP.Views.Dialogs.MachineDocumentsDialog(_dbService, selected.Id, _attachmentService);
            await Navigation.PushModalAsync(dlg);
            _ = await dlg.Result;
            
        }

        private async void OnQrClicked(object? sender, EventArgs e)
        {
            if (SelectedMachine is not Machine selected)
            {
                await SafeNavigator.ShowAlertAsync("Obavijest", "Odaberite stroj kako biste otvorili QR.", "OK");
                return;
            }
            try
            {
                if (!string.IsNullOrWhiteSpace(selected.QrCode) && File.Exists(selected.QrCode))
                {
                    await Launcher.OpenAsync(new OpenFileRequest { File = new ReadOnlyFile(selected.QrCode) });
                    return;
                }
                var dt = await _dbService.ExecuteSelectAsync("SELECT COALESCE(qr_payload,'') AS p FROM machines WHERE id=@id", new[] { new MySqlParameter("@id", selected.Id) }).ConfigureAwait(false);
                string payload = dt.Rows.Count == 1 ? (dt.Rows[0]["p"]?.ToString() ?? string.Empty) : string.Empty;
                if (!string.IsNullOrWhiteSpace(payload))
                {
                    using var stream = _qrService.GeneratePng(payload);
                    var dir = FileSystem.AppDataDirectory;
                    Directory.CreateDirectory(dir);
                    var pathPng = Path.Combine(dir, $"M_{selected.Id}.png");
                    using var fsOut = File.Create(pathPng);
                    stream.CopyTo(fsOut);
                    await _dbService.ExecuteNonQueryAsync("UPDATE machines SET qr_code=@p WHERE id=@id", new[] { new MySqlParameter("@p", pathPng), new MySqlParameter("@id", selected.Id) }).ConfigureAwait(false);
                    await Launcher.OpenAsync(new OpenFileRequest { File = new ReadOnlyFile(pathPng) });
                    return;
                }
                await SafeNavigator.ShowAlertAsync("Info", "QR payload nije postavljen za ovaj stroj.", "OK");
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("Greška", ex.Message, "OK");
            }
        }

        private async void OnComponentsClicked(object? sender, EventArgs e)
        {
            if (SelectedMachine is not Machine selected)
            {
                await SafeNavigator.ShowAlertAsync("Obavijest", "Odaberite stroj za pregled komponenti.", "OK");
                return;
            }
            var dlg = new YasGMP.Views.Dialogs.MachineComponentsDialog(_dbService, selected.Id);
            await Navigation.PushModalAsync(dlg);
            _ = await dlg.Result;
        }

        private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            SelectedMachine = e?.CurrentSelection != null && e.CurrentSelection.Count > 0
                ? e.CurrentSelection[0] as Machine
                : null;
            try
            {
                SelectedInfo.IsVisible = SelectedMachine != null;
            }
            catch { }
        }

        // Syncfusion DataGrid selection changed handler
        private void OnGridSelectionChanged(object? sender, Syncfusion.Maui.DataGrid.DataGridSelectionChangedEventArgs e)
        {
            try
            {
                var selected = (e != null && e.AddedRows != null && e.AddedRows.Count > 0)
                    ? e.AddedRows[0] as Machine
                    : null;
                SelectedMachine = selected;
                SelectedInfo.IsVisible = SelectedMachine != null;
            }
            catch { }
        }

        private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
        {
            try
            {
                ApplySearchFilter(e?.NewTextValue);
            }
            catch { }
        }

        // Applies only the global SearchBar filter; column filters are now provided by the grid itself.
        private void ApplySearchFilter(string? query)
        {
            string q = (query ?? string.Empty).Trim().ToLowerInvariant();
            IEnumerable<Machine> src = _allMachines;

            if (!string.IsNullOrWhiteSpace(q))
            {
                src = src.Where(m =>
                    (m.Name ?? string.Empty).ToLowerInvariant().Contains(q) ||
                    (m.Code ?? string.Empty).ToLowerInvariant().Contains(q) ||
                    (m.Manufacturer ?? string.Empty).ToLowerInvariant().Contains(q) ||
                    (m.Location ?? string.Empty).ToLowerInvariant().Contains(q));
            }

            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Machines.Clear();
                    foreach (var m in src) Machines.Add(m);
                });
            }
            catch { }
        }

        private async void OnTestConnectionClicked(object? sender, EventArgs e)
        {
            try
            {
                var v = await _dbService.ExecuteScalarAsync("SELECT 1").ConfigureAwait(false);
                await SafeNavigator.ShowAlertAsync("Baza", $"Veza uspješna (SELECT 1 = {v}).", "OK");
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("Greška baze", ex.Message, "OK");
            }
        }
    }
}



