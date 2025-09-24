using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using YasGMP.Common;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// View-model for managing CAPA cases. UI-agnostic and driven by services for dialogs and dispatching.
    /// </summary>
    public sealed class CapaViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _dbService;
        private readonly IDialogService _dialogService;
        private readonly IUiDispatcher _dispatcher;

        private CapaCase? _selectedCapa;
        private bool _isRefreshing;

        public ObservableCollection<CapaCase> CapaCases { get; } = new();

        public ICommand AddNewCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand RefreshCommand { get; }

        public CapaCase? SelectedCapa
        {
            get => _selectedCapa;
            set
            {
                if (!ReferenceEquals(_selectedCapa, value))
                {
                    _selectedCapa = value;
                    OnPropertyChanged();
                    InvalidateCommands();
                }
            }
        }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set
            {
                if (_isRefreshing != value)
                {
                    _isRefreshing = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsEmpty => CapaCases.Count == 0;

        public CapaViewModel()
            : this(
                ServiceLocator.GetRequiredService<DatabaseService>(),
                ServiceLocator.GetRequiredService<IDialogService>(),
                ServiceLocator.GetRequiredService<IUiDispatcher>())
        {
        }

        public CapaViewModel(DatabaseService dbService, IDialogService dialogService, IUiDispatcher dispatcher)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

            AddNewCommand = new AsyncDelegateCommand(() => OpenCapaDialogAsync());
            EditCommand = new AsyncDelegateCommand(
                () => SelectedCapa != null ? OpenCapaDialogAsync(SelectedCapa) : Task.CompletedTask,
                () => SelectedCapa != null);
            DeleteCommand = new AsyncDelegateCommand(DeleteCapaAsync, () => SelectedCapa != null);
            RefreshCommand = new AsyncDelegateCommand(LoadCapaCasesAsync);

            _ = LoadCapaCasesAsync();
        }

        public async Task OpenCapaDialogAsync(CapaCase? existing = null)
        {
            var request = new CapaDialogRequest(existing);
            var updated = await _dialogService.ShowDialogAsync<CapaCase>(DialogIds.CapaEdit, request).ConfigureAwait(false);
            if (updated == null)
                return;

            if (existing == null)
            {
                if (updated.Id == 0)
                    updated.Id = GenerateNextId();

                await _dispatcher.InvokeAsync(() => CapaCases.Add(updated)).ConfigureAwait(false);
            }
            else
            {
                await _dispatcher.InvokeAsync(() =>
                {
                    var index = CapaCases.IndexOf(existing);
                    if (index >= 0)
                        CapaCases[index] = updated;
                }).ConfigureAwait(false);
            }

            SelectedCapa = updated;
            OnPropertyChanged(nameof(IsEmpty));
        }

        private async Task DeleteCapaAsync()
        {
            var target = SelectedCapa;
            if (target == null)
                return;

            bool confirmed = await _dialogService
                .ShowConfirmationAsync("Potvrda brisanja", "Jeste li sigurni da želite izbrisati odabrani CAPA slučaj?", "Da", "Ne")
                .ConfigureAwait(false);

            if (!confirmed)
                return;

            await _dispatcher.InvokeAsync(() =>
            {
                CapaCases.Remove(target);
                SelectedCapa = null;
                OnPropertyChanged(nameof(IsEmpty));
            }).ConfigureAwait(false);
        }

        public async Task LoadCapaCasesAsync()
        {
            IsRefreshing = true;
            try
            {
                var items = await _dbService.GetAllCapaCasesAsync().ConfigureAwait(false);
                await _dispatcher.InvokeAsync(() =>
                {
                    CapaCases.Clear();
                    if (items != null)
                    {
                        foreach (var c in items)
                            CapaCases.Add(c);
                    }
                    OnPropertyChanged(nameof(IsEmpty));
                }).ConfigureAwait(false);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private int GenerateNextId() => CapaCases.Count == 0 ? 1 : CapaCases.Max(c => c.Id) + 1;

        private void InvalidateCommands()
        {
            if (EditCommand is AsyncDelegateCommand edit)
                edit.RaiseCanExecuteChanged();
            if (DeleteCommand is AsyncDelegateCommand delete)
                delete.RaiseCanExecuteChanged();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
    }
}
