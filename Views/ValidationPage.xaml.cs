// ==============================================================================
//  File: Views/ValidationPage.xaml.cs
//  Project: YasGMP
//  Summary:
//      GMP/Annex 11–compliant editor for validation records (IQ/OQ/PQ/URS/DQ/FAT/SAT).
//      • Safe UI-thread dialogs via SafeNavigator/MainThread (fixes 0x8001010E)
//      • DI-friendly + parameterless constructors
//      • Robust connection-string resolution from App.AppConfig (with DI fallback)
//      • Auto-reselect & center-in-view after save
//      • PDF add/import via FilePicker (local copy into AppData)
// ==============================================================================

using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;         // Dictionary<,>, IEnumerable<>
using System.IO;                           // File IO
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;     // MainThread
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;              // FilePicker, FileSystem
using Microsoft.Extensions.DependencyInjection;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.Views
{
    /// <summary>
    /// <b>ValidationPage</b> — GMP/Annex 11 compliant editor for validation records (IQ/OQ/PQ/URS/DQ/FAT/SAT).
    /// <para>
    /// This page loads validations, supports add/update/delete flows via <see cref="DatabaseService"/>,
    /// and provides robust, thread-safe UI updates using <see cref="MainThread"/> helpers.
    /// </para>
    /// <remarks>
    /// • Data sources are resolved via DI first, then AppConfig (connection string fallback).<br/>
    /// • <see cref="IsBusy"/> intentionally shadows <see cref="Page.IsBusy"/> and keeps it synchronized to avoid ambiguity.<br/>
    /// • Auto-reselect and scroll-to-selected is supported via CollectionView.ScrollTo.
    /// </remarks>
    /// </summary>
    public partial class ValidationPage : ContentPage
    {
        private readonly DatabaseService _db;

        /// <summary>Bindable, observable list of all validation items for the CollectionView.</summary>
        public ObservableCollection<Validation> ValidationList { get; } = new();

        /// <summary>
        /// The currently edited record (a copy of <see cref="SelectedValidation"/>). Safe for two-way binding.
        /// </summary>
        public Validation EditValidation { get; private set; } = new();

        private Validation? _selectedValidation;

        // Local busy-state backing store (kept in sync with Page.IsBusy)
        private bool _isBusy;

        // Status bar text
        private string _statusMessage = string.Empty;

        /// <summary>
        /// The currently selected record from the list (one of <see cref="ValidationList"/>).
        /// Setting this raises change notifications and copies it into <see cref="EditValidation"/>.
        /// </summary>
        public Validation? SelectedValidation
        {
            get => _selectedValidation;
            set
            {
                if (_selectedValidation == value) return;
                _selectedValidation = value;
                EditValidation = value?.DeepCopy() ?? new Validation();
                OnPropertyChanged(nameof(SelectedValidation));
                OnPropertyChanged(nameof(EditValidation));
                OnPropertyChanged(nameof(CanDelete));
            }
        }

        /// <summary>
        /// Busy flag for the page and UI throttling.
        /// This property intentionally <b>shadows</b> <see cref="Page.IsBusy"/>.
        /// </summary>
        public new bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (_isBusy == value) return;
                _isBusy = value;
                base.IsBusy = value; // keep platform indicators consistent
                OnPropertyChanged();
            }
        }

        /// <summary>Text shown in the status area under the header.</summary>
        public string StatusMessage
        {
            get => _statusMessage;
            private set
            {
                var v = value ?? string.Empty;
                if (_statusMessage == v) return;
                _statusMessage = v;
                OnPropertyChanged();
            }
        }

        /// <summary>Whether the current selection can be deleted (requires persisted Id).</summary>
        public bool CanDelete => (SelectedValidation?.Id ?? 0) > 0;

        /// <summary>Shell/HotReload-friendly ctor: resolves DB via DI, with AppConfig fallback.</summary>
        public ValidationPage() : this(ResolveDb()) { }

        /// <summary>DI-friendly constructor.</summary>
        public ValidationPage(DatabaseService db)
        {
            InitializeComponent();
            _db = db ?? throw new ArgumentNullException(nameof(db));
            BindingContext = this;

            // Fire-and-forget: initial data load on the UI thread.
            MainThread.BeginInvokeOnMainThread(async () => await LoadDataAsync());
        }

        /// <summary>
        /// Resolves a <see cref="DatabaseService"/> from DI or from App.AppConfig as a fallback.
        /// </summary>
        private static DatabaseService ResolveDb()
        {
            var sp = Application.Current?.Handler?.MauiContext?.Services;
            var byDi = sp?.GetService<DatabaseService>();
            if (byDi != null) return byDi;

            if (Application.Current is App app && app.AppConfig is not null)
            {
                string? cs = null;
                try
                {
                    var idxer = app.AppConfig.GetType().GetProperty("Item", new[] { typeof(string) });
                    cs = idxer?.GetValue(app.AppConfig, new object[] { "ConnectionStrings:MySqlDb" }) as string;
                }
                catch { /* ignore */ }

                if (string.IsNullOrWhiteSpace(cs))
                {
                    try
                    {
                        var csObj = app.AppConfig.GetType().GetProperty("ConnectionStrings")?.GetValue(app.AppConfig);
                        cs = csObj?.GetType().GetProperty("MySqlDb")?.GetValue(csObj) as string;
                    }
                    catch { /* ignore */ }
                }

                if (!string.IsNullOrWhiteSpace(cs))
                    return new DatabaseService(cs!);
            }

            throw new InvalidOperationException("MySqlDb connection string nije pronađen.");
        }

        /// <summary>
        /// Loads data (validations + lookup lists) and optionally attempts to reselect & scroll to an item by Id.
        /// </summary>
        /// <param name="reselectId">Optional Id to reselect and scroll to; falls back to current selection.</param>
        private async Task LoadDataAsync(int? reselectId = null)
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                // Keep current selection id if none explicitly provided
                reselectId ??= SelectedValidation?.Id;

                var list = await _db.GetAllValidationsAsync().ConfigureAwait(false) ?? new();
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ValidationList.Clear();
                    foreach (var v in list) ValidationList.Add(v);
                });

                // Reselect & center
                if (reselectId is int id && id > 0)
                {
                    var target = ValidationList.FirstOrDefault(v => v.Id == id);
                    if (target != null)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            SelectedValidation = target;
                            if (FindByName("ValidationListView") is CollectionView cv)
                                cv.ScrollTo(target, position: ScrollToPosition.Center, animate: true);
                        });
                    }
                    else
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            SelectedValidation = null;
                            EditValidation = new Validation();
                            OnPropertyChanged(nameof(EditValidation));
                        });
                    }
                }

                // Populate pickers (Machines)
                var machines = await _db.GetAllMachinesAsync().ConfigureAwait(false) ?? new();
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (FindByName("MachinePicker") is Picker p)
                    {
                        p.ItemsSource = machines;
                        p.ItemDisplayBinding = new Binding(nameof(Machine.Name));
                    }
                });

                // Populate pickers (Components)
                var components = await _db.GetAllComponentsAsync().ConfigureAwait(false) ?? new();
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (FindByName("ComponentPicker") is Picker p)
                    {
                        p.ItemsSource = components;
                        p.ItemDisplayBinding = new Binding(nameof(MachineComponent.Name));
                    }
                });

                SetStatusMessage($"Učitano: {ValidationList.Count} zapisa.");
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("Učitavanje", $"Greška pri učitavanju: {ex.Message}", "OK");
                SetStatusMessage("Greška pri učitavanju.");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Save: insert or update <see cref="EditValidation"/>; then reselect and center the saved record.
        /// </summary>
        private async void OnSaveClicked(object sender, EventArgs e)
        {
            if (EditValidation == null)
            {
                SetStatusMessage("Nijedna validacija za spremanje.");
                return;
            }

            if (IsBusy) return;
            IsBusy = true;
            try
            {
                bool update = EditValidation.Id > 0;
                int userId = (App.Current as App)?.LoggedUser?.Id ?? 0;

                await _db.InsertOrUpdateValidationAsync(EditValidation, update, userId).ConfigureAwait(false);

                var savedId = EditValidation.Id; // service should set Id on insert
                SetStatusMessage(update ? "Validacija ažurirana!" : "Validacija spremljena!");
                await LoadDataAsync(savedId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                SetStatusMessage($"Greška pri spremanju: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Delete selected validation after confirmation.
        /// </summary>
        private async void OnDeleteClicked(object sender, EventArgs e)
        {
            if (!CanDelete || SelectedValidation == null) return;
            if (IsBusy) return;

            try
            {
                var confirm = await SafeNavigator.ConfirmAsync("Potvrda", "Obrisati ovu validaciju?", "Da", "Ne");
                if (!confirm) return;

                IsBusy = true;

                int userId = (App.Current as App)?.LoggedUser?.Id ?? 0;
                await _db.DeleteValidationAsync(SelectedValidation.Id, userId).ConfigureAwait(false);

                SetStatusMessage("Validacija obrisana!");
                await LoadDataAsync().ConfigureAwait(false);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    SelectedValidation = null;
                    EditValidation = new Validation();
                    OnPropertyChanged(nameof(EditValidation));
                });
            }
            catch (Exception ex)
            {
                SetStatusMessage($"Greška pri brisanju: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Add document: open a PDF picker, copy into app storage, set EditValidation.Documentation.
        /// </summary>
        private async void OnAddDocumentClicked(object? sender, EventArgs e)
        {
            try
            {
                // Let users attach even before selecting a persisted record; we write to EditValidation
                var fileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.iOS,         new[] { "com.adobe.pdf" } },
                    { DevicePlatform.MacCatalyst, new[] { "com.adobe.pdf" } },
                    { DevicePlatform.Android,     new[] { "application/pdf" } },
                    { DevicePlatform.WinUI,       new[] { ".pdf" } }
                });

                var pickOptions = new PickOptions
                {
                    PickerTitle = "Odaberite PDF dokument",
                    FileTypes   = fileTypes
                };

                var file = await FilePicker.Default.PickAsync(pickOptions);
                if (file is null)
                {
                    SetStatusMessage("Odabir dokumenta je otkazan.");
                    return;
                }

                // Copy into app data for durable access
                var docsDir  = Path.Combine(FileSystem.AppDataDirectory, "validation_docs");
                Directory.CreateDirectory(docsDir);

                var safeName = SanitizeFileName(string.IsNullOrWhiteSpace(file.FileName) ? "dokument.pdf" : file.FileName);
                var stamp    = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                var destPath = Path.Combine(docsDir, $"{stamp}_{safeName}");

                using (var src = await file.OpenReadAsync())
                using (var dst = File.Create(destPath))
                    await src.CopyToAsync(dst).ConfigureAwait(false);

                // Update model & notify bindings (UI thread)
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    EditValidation.Documentation = destPath;
                    OnPropertyChanged(nameof(EditValidation));
                });

                SetStatusMessage("Dokument dodan. Spremite zapis za trajno pohranjivanje.");
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("Greška", $"Dodavanje dokumenta nije uspjelo: {ex.Message}", "OK");
            }
        }

        /// <summary>Replace invalid filename characters with '_'.</summary>
        private static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var cleaned = new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
            return string.IsNullOrWhiteSpace(cleaned) ? "dokument.pdf" : cleaned;
        }

        /// <summary>Set status bar text (with change notification).</summary>
        private void SetStatusMessage(string message) => StatusMessage = message;
    }
}
