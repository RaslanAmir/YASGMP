// ==============================================================================
//  File: Views/AuditDashboardPage.xaml.cs
//  Project: YasGMP
//  Summary:
//      Code-behind for AuditDashboardPage. Provides a thin adapter layer so the
//      XAML you supplied (FilterUser, FilterEntity, ActionTypes, SelectedAction,
//      FilterFrom, FilterTo, ApplyFilterCommand, ExportPdfCommand, ExportExcelCommand,
//      FilteredAudits) works seamlessly on top of the strong, production-ready
//      AuditLogViewModel (which exposes FilterUserIdText, FilteredEvents, etc.).
//
//      The adapter keeps names expected by the XAML and maps them to the actual
//      ViewModel. It also projects SystemEvent rows into lightweight items that
//      expose Action, EntityName, UserFullName, ChangedAt for your DataTemplate.
//
//      DI-first wiring: resolves AuditLogViewModel via IServiceProvider if
//      available; otherwise constructs it safely (injecting DatabaseService if
//      present). On appearing, it triggers an initial load and keeps the adapter
//      collection synchronized with the ViewModel's FilteredEvents collection.
//
//  © 2025 YasGMP. All rights reserved.
// ==============================================================================
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using YasGMP.Services;
using YasGMP.ViewModels;

// Alias to match the POCO returned by DatabaseService.GetSystemEventsAsync
using SystemEvent = YasGMP.Services.SystemEvent;

namespace YasGMP.Views
{
    /// <summary>
    /// <b>AuditDashboardPage</b> — Dashboard-flavored audit viewer that uses the
    /// same underlying <see cref="AuditLogViewModel"/> as the main Audit page,
    /// but exposes bindings under the property names used in your XAML.
    /// </summary>
    public partial class AuditDashboardPage : ContentPage
    {
        private AuditDashboardAdapter? _adapter;

        /// <summary>
        /// Initializes the page, wires the BindingContext via DI when possible,
        /// and prepares synchronization with the underlying <see cref="AuditLogViewModel"/>.
        /// </summary>
        public AuditDashboardPage()
        {
            InitializeComponent();
            TryWireViewModel();
        }

        /// <summary>
        /// Called when the page becomes visible. Ensures data is loaded at least once.
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (_adapter is not null)
            {
                await _adapter.EnsureLoadedAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Resolves <see cref="AuditLogViewModel"/> via DI, or creates it with
        /// an available <see cref="DatabaseService"/>; then wraps it in an adapter
        /// that matches the bindings used by the XAML file you provided.
        /// </summary>
        private void TryWireViewModel()
        {
            try
            {
                var services = Application.Current?.Handler?.MauiContext?.Services;

                // Preferred: resolve VM from DI
                var vm = services?.GetService<AuditLogViewModel>();
                if (vm == null)
                {
                    // Next best: resolve DatabaseService and inject into VM
                    var db = services?.GetService<DatabaseService>();
                    vm = db != null ? new AuditLogViewModel(db) : new AuditLogViewModel();
                }

                _adapter = new AuditDashboardAdapter(vm);
                BindingContext = _adapter;
            }
            catch
            {
                // Design-time safe: leave BindingContext null if anything goes wrong.
            }
        }

        // ==========================================================================
        //  Adapter types
        // ==========================================================================

        /// <summary>
        /// Adapter that exposes properties/commands expected by your XAML and
        /// bridges them to the underlying <see cref="AuditLogViewModel"/>.
        /// </summary>
        private sealed class AuditDashboardAdapter : INotifyPropertyChanged
        {
            private readonly AuditLogViewModel _vm;
            /// <summary>
            /// Initializes a new instance of the AuditDashboardAdapter class.
            /// </summary>

            public AuditDashboardAdapter(AuditLogViewModel vm)
            {
                _vm = vm ?? throw new ArgumentNullException(nameof(vm));

                // Wrap/forward commands so XAML names remain unchanged
                ApplyFilterCommand   = _vm.ApplyFilterCommand;
                ExportPdfCommand     = _vm.ExportPdfCommand;
                ExportExcelCommand   = new Command(() => _vm.ExportXlsxCommand.Execute(null));

                // Mirror action types directly
                ActionTypes = _vm.ActionTypes;

                // Build initial projection and keep it synchronized
                SyncFromVm();
                _vm.FilteredEvents.CollectionChanged += OnVmFilteredEventsChanged;
                _vm.PropertyChanged += OnVmPropertyChanged;
            }

            #region === Properties expected by the XAML ===

            /// <summary>
            /// Text field labeled "User..." in your XAML.
            /// Mapped to <see cref="AuditLogViewModel.FilterUserIdText"/> (user id as text).
            /// </summary>
            public string? FilterUser
            {
                get => _vm.FilterUserIdText;
                set { _vm.FilterUserIdText = value; OnPropertyChanged(); }
            }

            /// <summary>
            /// Entity/table name filter. Pass-through to the underlying VM.
            /// </summary>
            public string? FilterEntity
            {
                get => _vm.FilterEntity;
                set { _vm.FilterEntity = value; OnPropertyChanged(); }
            }

            /// <summary>
            /// Selected action filter (exact match, case-insensitive).
            /// </summary>
            public string? SelectedAction
            {
                get => _vm.SelectedAction;
                set { _vm.SelectedAction = value; OnPropertyChanged(); }
            }

            /// <summary>
            /// Lower date bound (inclusive).
            /// </summary>
            public DateTime? FilterFrom
            {
                get => _vm.FilterFrom;
                set { _vm.FilterFrom = value; OnPropertyChanged(); }
            }

            /// <summary>
            /// Upper date bound (inclusive).
            /// </summary>
            public DateTime? FilterTo
            {
                get => _vm.FilterTo;
                set { _vm.FilterTo = value; OnPropertyChanged(); }
            }

            /// <summary>
            /// Available action types for the <see cref="Picker"/> in the XAML.
            /// </summary>
            public ObservableCollection<string> ActionTypes { get; }

            /// <summary>
            /// The collection the XAML binds to; projects <see cref="SystemEvent"/>
            /// items into simple display rows with the expected property names.
            /// </summary>
            public ObservableCollection<AuditRow> FilteredAudits { get; } = new();

            #endregion

            #region === Commands expected by the XAML ===

            /// <summary>Applies filters (forwards to ViewModel).</summary>
            public ICommand ApplyFilterCommand { get; }

            /// <summary>Exports to PDF (forwards to ViewModel).</summary>
            public ICommand ExportPdfCommand { get; }

            /// <summary>Exports to Excel .xlsx (bridges to ViewModel's XLSX export).</summary>
            public ICommand ExportExcelCommand { get; }

            #endregion

            #region === Public API ===

            /// <summary>
            /// Ensures the underlying ViewModel has loaded data at least once.
            /// Safe to call repeatedly.
            /// </summary>
            public async Task EnsureLoadedAsync()
            {
                // If nothing loaded yet, load now.
                if (_vm.AllEvents.Count == 0)
                {
                    await _vm.LoadAsync().ConfigureAwait(false);
                }
                else
                {
                    // Re-apply to keep projection in sync with filters
                    _vm.ApplyFilterCommand.Execute(null);
                }
            }

            #endregion

            #region === Synchronization with ViewModel ===

            private void OnVmFilteredEventsChanged(object? sender, NotifyCollectionChangedEventArgs e)
            {
                // For simplicity and correctness, rebuild projection on any change
                SyncFromVm();
            }

            private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
            {
                // If VM changed filters or status, we may need to refresh our bindings
                switch (e.PropertyName)
                {
                    case nameof(AuditLogViewModel.FilterUserIdText):
                        OnPropertyChanged(nameof(FilterUser));
                        break;
                    case nameof(AuditLogViewModel.FilterEntity):
                        OnPropertyChanged(nameof(FilterEntity));
                        break;
                    case nameof(AuditLogViewModel.SelectedAction):
                        OnPropertyChanged(nameof(SelectedAction));
                        break;
                    case nameof(AuditLogViewModel.FilterFrom):
                        OnPropertyChanged(nameof(FilterFrom));
                        break;
                    case nameof(AuditLogViewModel.FilterTo):
                        OnPropertyChanged(nameof(FilterTo));
                        break;
                    default:
                        break;
                }
            }

            /// <summary>
            /// Projects the ViewModel's <see cref="AuditLogViewModel.FilteredEvents"/>
            /// into <see cref="FilteredAudits"/> with the property names required by the XAML.
            /// </summary>
            private void SyncFromVm()
            {
                FilteredAudits.Clear();
                foreach (var e in _vm.FilteredEvents)
                {
                    FilteredAudits.Add(AuditRow.FromEvent(e));
                }
                OnPropertyChanged(nameof(FilteredAudits));
            }

            #endregion

            #region === INotifyPropertyChanged ===

            /// <inheritdoc />
            public event PropertyChangedEventHandler? PropertyChanged;

            private void OnPropertyChanged([CallerMemberName] string? name = null)
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

            #endregion
        }

        /// <summary>
        /// Lightweight, display-focused row that matches your DataTemplate:
        /// exposes Action, EntityName, UserFullName, ChangedAt.
        /// </summary>
        private sealed class AuditRow
        {
            /// <summary>Event type (CREATE/UPDATE/…)</summary>
            public string Action { get; init; } = string.Empty;

            /// <summary>Target table/entity name.</summary>
            public string EntityName { get; init; } = string.Empty;

            /// <summary>
            /// Display name of the user if known; otherwise "User #&lt;id&gt;".
            /// The SystemEvent stream typically contains <c>UserId</c> only.
            /// </summary>
            public string UserFullName { get; init; } = string.Empty;

            /// <summary>UTC timestamp of the event (displayed with default ToString in XAML).</summary>
            public DateTime ChangedAt { get; init; }

            /// <summary>
            /// Builds an <see cref="AuditRow"/> from a <see cref="SystemEvent"/> record.
            /// </summary>
            public static AuditRow FromEvent(SystemEvent e)
            {
                var userLabel = e.UserId.HasValue
                    ? $"User #{e.UserId.Value.ToString(CultureInfo.InvariantCulture)}"
                    : "User #—";

                return new AuditRow
                {
                    Action      = e.EventType ?? string.Empty,
                    EntityName  = e.TableName ?? string.Empty,
                    UserFullName= userLabel,
                    ChangedAt   = e.EventTime
                };
            }
        }
    }
}
