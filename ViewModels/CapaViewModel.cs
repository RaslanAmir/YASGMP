using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using YasGMP.Models;
using YasGMP.Views;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// <b>CapaViewModel</b> — ViewModel for managing CAPA records in a GMP system.
    /// Provides display, add, edit, delete, and refresh behaviors through a dialog service abstraction.
    /// All UI interactions are marshaled to the UI thread to avoid WinUI COM exceptions.
    /// </summary>
    public class CapaViewModel : BindableObject
    {
        /// <summary>Observable list of CAPA cases for UI display.</summary>
        public ObservableCollection<CapaCase> CapaCases { get; } = new();

        private CapaCase? _selectedCapa;

        /// <summary>Currently selected CAPA case.</summary>
        public CapaCase? SelectedCapa
        {
            get => _selectedCapa;
            set
            {
                if (!ReferenceEquals(_selectedCapa, value))
                {
                    _selectedCapa = value;
                    OnPropertyChanged(nameof(SelectedCapa));
                    (EditCommand   as Command)?.ChangeCanExecute();
                    (DeleteCommand as Command)?.ChangeCanExecute();
                }
            }
        }

        private bool _isRefreshing;

        /// <summary>Indicates whether loading is in progress.</summary>
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set
            {
                if (_isRefreshing != value)
                {
                    _isRefreshing = value;
                    OnPropertyChanged(nameof(IsRefreshing));
                }
            }
        }

        /// <summary>True if there are no CAPA records.</summary>
        public bool IsEmpty => CapaCases.Count == 0;

        /// <summary>Command to add a new CAPA record.</summary>
        public ICommand AddNewCommand { get; }

        /// <summary>Command to edit the selected CAPA record.</summary>
        public ICommand EditCommand { get; }

        /// <summary>Command to delete the selected CAPA record.</summary>
        public ICommand DeleteCommand { get; }

        /// <summary>Command to reload all CAPA records.</summary>
        public ICommand RefreshCommand { get; }

        /// <summary>Dialog service for modal interactions.</summary>
        private readonly IDialogService _dialogService;

        /// <summary>Default (parameterless) constructor for XAML instantiation.</summary>
        public CapaViewModel() : this(new DefaultDialogService()) { }

        /// <summary>
        /// DI-friendly constructor that accepts a dialog service.
        /// </summary>
        /// <param name="dialogService">Dialog service used to show modal pages and alerts.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="dialogService"/> is <c>null</c>.</exception>
        public CapaViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            AddNewCommand = new Command(async () => await OpenCapaDialogAsync());
            EditCommand   = new Command(async () =>
            {
                if (SelectedCapa != null)
                    await OpenCapaDialogAsync(SelectedCapa);
            }, () => SelectedCapa != null);
            DeleteCommand = new Command(async () => await DeleteCapaAsync(), () => SelectedCapa != null);
            RefreshCommand= new Command(async () => await LoadCapaCasesAsync());

            // Initial non-blocking load
            _ = LoadCapaCasesAsync();
        }

        /// <summary>
        /// Opens the dialog for a new or existing CAPA case.
        /// </summary>
        /// <param name="existing">Existing CAPA case, or <c>null</c> to create a new one.</param>
        public async Task OpenCapaDialogAsync(CapaCase? existing = null)
        {
            var dialogVm   = new CapaEditDialogViewModel(existing);
            var dialogPage = new CapaEditDialog(dialogVm);

            // Show modal dialog and await result
            var result = await _dialogService.ShowDialogAsync(dialogPage).ConfigureAwait(false);

            // On success, add or update local collection
            if (result == true)
            {
                if (existing == null)
                {
                    if (dialogVm.CapaCase.Id == 0)
                        dialogVm.CapaCase.Id = GenerateNextId();

                    await MainThread.InvokeOnMainThreadAsync(() => CapaCases.Add(dialogVm.CapaCase));
                }
                else
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        var index = CapaCases.IndexOf(existing);
                        if (index >= 0)
                            CapaCases[index] = dialogVm.CapaCase;
                    });
                }

                SelectedCapa = dialogVm.CapaCase;
                OnPropertyChanged(nameof(IsEmpty));
            }
        }

        /// <summary>Deletes the selected CAPA case after user confirmation.</summary>
        private async Task DeleteCapaAsync()
        {
            var target = SelectedCapa;
            if (target != null && CapaCases.Contains(target))
            {
                bool confirmed = await _dialogService.DisplayAlertAsync(
                    "Potvrda brisanja",
                    "Jeste li sigurni da želite izbrisati odabrani CAPA slučaj?",
                    "Da", "Ne").ConfigureAwait(false);

                if (confirmed)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        CapaCases.Remove(target);
                        SelectedCapa = null;
                        OnPropertyChanged(nameof(IsEmpty));
                    });
                }
            }
        }

        /// <summary>
        /// Loads all CAPA cases from the data source.
        /// Currently uses demo data; replace with real repository/database calls.
        /// </summary>
        public async Task LoadCapaCasesAsync()
        {
            IsRefreshing = true;
            try
            {
                // Simulate fetch
                await Task.Delay(300).ConfigureAwait(false);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    CapaCases.Clear();

                    // TODO: Replace with real database/API fetch
                    CapaCases.Add(new CapaCase
                    {
                        Id       = 1,
                        Title    = "Neispravna serija proizvoda",
                        Reason   = "Uočen odstupajući uzorak tijekom inspekcije.",
                        Actions  = "Povlačenje serije, ispitivanje procesa, dodatna edukacija osoblja.",
                        DateOpen = DateTime.Today.AddDays(-7),
                        Status   = "otvoren"
                    });

                    OnPropertyChanged(nameof(IsEmpty));
                });
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        /// <summary>Generates the next ID for a new CAPA entry (max + 1).</summary>
        private int GenerateNextId() => CapaCases.Count == 0 ? 1 : CapaCases.Max(c => c.Id) + 1;
    }

    /// <summary>
    /// Abstraction for showing dialogs from a ViewModel (DI-friendly, easily mockable).
    /// </summary>
    public interface IDialogService
    {
        /// <summary>Shows a modal dialog and returns the dialog result.</summary>
        /// <param name="dialogPage">MAUI <see cref="Page"/> to push as a modal dialog.</param>
        Task<bool?> ShowDialogAsync(Page dialogPage);

        /// <summary>Shows a yes/no alert and returns the user's choice.</summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="message">Dialog message.</param>
        /// <param name="accept">Text for the accept button.</param>
        /// <param name="cancel">Text for the cancel button.</param>
        Task<bool> DisplayAlertAsync(string title, string message, string accept, string cancel);
    }

    /// <summary>
    /// Default implementation of <see cref="IDialogService"/> for MAUI.
    /// Renders pages as modal dialogs and uses <see cref="Page.DisplayAlert(string,string,string,string)"/>.
    /// All invocations are forced onto the UI thread to prevent COM exceptions.
    /// </summary>
    public class DefaultDialogService : IDialogService
    {
        /// <inheritdoc/>
        public async Task<bool?> ShowDialogAsync(Page dialogPage)
        {
            var tcs = new TaskCompletionSource<bool?>();

            // When the page is dismissed, pick up the DialogResult from its BindingContext (ViewModel).
            dialogPage.Disappearing += (_, _) =>
            {
                if (dialogPage.BindingContext is CapaEditDialogViewModel vm)
                    tcs.TrySetResult(vm.DialogResult);
                else
                    tcs.TrySetResult(false);
            };

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Application.Current!.MainPage!.Navigation.PushModalAsync(dialogPage);
            });

            return await tcs.Task.ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public Task<bool> DisplayAlertAsync(string title, string message, string accept, string cancel)
        {
            return MainThread.InvokeOnMainThreadAsync(async () =>
            {
                return await Application.Current!.MainPage!.DisplayAlert(title, message, accept, cancel);
            });
        }
    }
}
