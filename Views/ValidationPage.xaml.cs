using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel; // MainThread
using Microsoft.Maui.Controls;
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
    /// • Auto-reselect and scroll-to-selected is supported via <see cref="CollectionView.ScrollTo(object, ScrollToPosition, bool)"/>.
    /// </remarks>
    /// </summary>
    public partial class ValidationPage : ContentPage
    {
        private readonly DatabaseService _db;

        /// <summary>
        /// Bindable, observable list of all validation items for the CollectionView.
        /// </summary>
        public ObservableCollection<Validation> ValidationList { get; } = new();

        /// <summary>
        /// The currently edited record (a copy of <see cref="SelectedValidation"/>). Safe for two-way binding.
        /// </summary>
        public Validation EditValidation { get; private set; } = new();

        private Validation? _selectedValidation;

        // Backing store for our local busy-state.
        private bool _isBusy;

        // Backing store for the status bar text.
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
        /// <para>
        /// This property intentionally <b>shadows</b> <see cref="Page.IsBusy"/> to keep existing XAML bindings simple.
        /// The setter synchronizes its value to <see cref="Page.IsBusy"/> to maintain platform semantics and avoid warnings.
        /// </para>
        /// </summary>
        public new bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (_isBusy == value) return;
                _isBusy = value;
                // Keep the base Page.IsBusy in sync so platform indicators (if any) remain consistent.
                base.IsBusy = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Text shown in the status area under the header (e.g., progress, errors, confirmations).
        /// </summary>
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

        /// <summary>
        /// Indicates whether the current selection can be deleted (requires a persisted Id).
        /// </summary>
        public bool CanDelete => (SelectedValidation?.Id ?? 0) > 0;

        /// <summary>
        /// Shell/HotReload friendly constructor: resolves the database via DI, falling back to AppConfig if necessary.
        /// </summary>
        public ValidationPage() : this(ResolveDb()) { }

        /// <summary>
        /// DI-friendly constructor.
        /// </summary>
        /// <param name="db">Instance of <see cref="DatabaseService"/> used to load and persist validation data.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="db"/> is null.</exception>
        public ValidationPage(DatabaseService db)
        {
            InitializeComponent();
            _db = db ?? throw new ArgumentNullException(nameof(db));
            BindingContext = this;
            // Fire-and-forget: initial data load on the UI thread.
            MainThread.BeginInvokeOnMainThread(async () => await LoadDataAsync());
        }

        /// <summary>
        /// Resolves a <see cref="DatabaseService"/> from DI or from <c>App.AppConfig</c> as a fallback.
        /// </summary>
        /// <returns>A constructed, ready-to-use <see cref="DatabaseService"/>.</returns>
        /// <exception cref="InvalidOperationException">If no connection string can be resolved.</exception>
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
        /// <param name="reselectId">
        /// Optional target Id to reselect and scroll to. If omitted, the current selection Id (if any) is used.
        /// </param>
        private async Task LoadDataAsync(int? reselectId = null)
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                // Keep the current selection id if none is explicitly provided.
                reselectId ??= SelectedValidation?.Id;

                var list = await _db.GetAllValidationsAsync().ConfigureAwait(false) ?? new();
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ValidationList.Clear();
                    foreach (var v in list) ValidationList.Add(v);
                });

                // Reselect & scroll if possible
                if (reselectId is int id && id > 0)
                {
                    var target = ValidationList.FirstOrDefault(v => v.Id == id);
                    if (target != null)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            SelectedValidation = target;
                            if (FindByName("ValidationListView") is CollectionView cv)
                            {
                                // ✅ CS1061 FIX: CollectionView uses ScrollTo (non-async) in .NET MAUI
                                cv.ScrollTo(target, position: ScrollToPosition.Center, animate: true);
                            }
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
        /// Handles the Save button click: inserts or updates the current <see cref="EditValidation"/>.
        /// Reselects and centers the saved record in the list on success.
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

                var savedId = EditValidation.Id; // Assumes the service sets Id on insert.

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
        /// Handles the Delete button click: confirms and deletes the current <see cref="SelectedValidation"/>.
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
        /// Sets the status bar message in a single place, ensuring change notification.
        /// </summary>
        /// <param name="message">Message text to display under the header.</param>
        private void SetStatusMessage(string message) => StatusMessage = message;
    }
}
