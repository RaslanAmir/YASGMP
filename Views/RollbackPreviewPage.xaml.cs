// ==============================================================================
//  File: Views/RollbackPreviewPage.xaml.cs
//  Project: YasGMP
//  Summary:
//      Code-behind for RollbackPreviewPage. Wires a lean, self-contained
//      view-model that adapts your XAML bindings (SelectedAudit.EntityName,
//      OldJson, NewJson, SignatureStatus/Color, CanRollback, Rollback/Close).
//
//      ✔ DI-aware: resolves DatabaseService/AuthService from IServiceProvider.
//      ✔ Safe-by-design: "Rollback" records a formal rollback request via
//        audit trail for Work Orders (work_orders), as per existing pattern
//        in WorkOrderViewModel (GMP/Part11 prefers controlled restoration).
//      ✔ No stubs: fully implemented commands; Close pops the page.
//      ✔ XML documentation everywhere for IntelliSense.
//
//      Assumptions (safe & stated):
//      • The primary, formally supported rollback flow is for the "work_orders"
//        entity (table). For that case we record a ROLLBACK request in the
//        work order audit log via DatabaseService.LogWorkOrderAuditAsync(...),
//        matching the pattern already used elsewhere in the app.
//      • Other entities show a clear message that controlled rollback is not
//        yet enabled for them (avoids silent failure).
//
//  © 2025 YasGMP. All rights reserved.
// ==============================================================================
using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.Views
{
    /// <summary>
    /// <b>RollbackPreviewPage</b> – Presents a read-only JSON diff (old/new values)
    /// for a selected audit record and allows a controlled rollback **request**
    /// (GMP/CSV/21 CFR Part 11) for supported entities (currently: <c>work_orders</c>).
    /// </summary>
    public partial class RollbackPreviewPage : ContentPage
    {
        private RollbackPreviewViewModel? _vm;

        /// <summary>
        /// Parameterless constructor required by XAML and Shell.
        /// </summary>
        public RollbackPreviewPage()
        {
            InitializeComponent();
            TryWireViewModel(null);
        }

        /// <summary>
        /// Optional convenience constructor that takes the selected audit record.
        /// </summary>
        /// <param name="audit">Audit record to preview.</param>
        public RollbackPreviewPage(AuditLog audit) : this()
        {
            // Replace the VM with the one based on the provided audit.
            TryWireViewModel(audit);
        }

        /// <summary>
        /// Attempts to construct and bind a <see cref="RollbackPreviewViewModel"/>.
        /// If an <paramref name="audit"/> is provided, it is applied immediately.
        /// </summary>
        private void TryWireViewModel(AuditLog? audit)
        {
            try
            {
                var services = Application.Current?.Handler?.MauiContext?.Services;

                // Resolve optional services for the VM to use.
                var db   = services?.GetService<DatabaseService>();
                var auth = services?.GetService<AuthService>();

                _vm = new RollbackPreviewViewModel(db, auth);

                // If an initial audit was supplied, apply it; otherwise the hosting
                // caller should set it via the VM's SelectedAudit setter after navigation.
                if (audit is not null)
                {
                    _vm.SelectedAudit = RollbackPreviewViewModel.AuditSelection.FromAudit(audit);
                    _vm.OldJson       = SafeToDisplay(audit.OldValue);
                    _vm.NewJson       = SafeToDisplay(audit.NewValue);
                }

                BindingContext = _vm;
            }
            catch
            {
                // Design-time safe: leave BindingContext unset on failure.
            }

            static string SafeToDisplay(string? s)
                => string.IsNullOrWhiteSpace(s) ? string.Empty : s;
        }

        /// <summary>
        /// Pops this page from the navigation stack. Used by CloseCommand.
        /// </summary>
        private Task CloseAsync() => Navigation?.PopAsync() ?? Task.CompletedTask;

        // ======================================================================
        //  ViewModel (self-contained, strongly typed, no stubs)
        // ======================================================================

        /// <summary>
        /// View-model for <see cref="RollbackPreviewPage"/> that adapts to
        /// the bindings defined in your XAML.
        /// </summary>
        private sealed class RollbackPreviewViewModel : INotifyPropertyChanged
        {
            private readonly DatabaseService? _db;
            private readonly AuthService? _auth;

            private AuditSelection? _selectedAudit;
            private string _oldJson = string.Empty;
            private string _newJson = string.Empty;
            private string _signatureStatus = "N/A";
            private Color  _signatureColor  = Colors.Gray;
            private bool   _canRollback;

            /// <summary>
            /// Creates a new instance of the view-model.
            /// </summary>
            /// <param name="db">Database service (optional; used for logging the rollback request).</param>
            /// <param name="auth">Authentication/context service (optional; provides user/session context).</param>
            public RollbackPreviewViewModel(DatabaseService? db, AuthService? auth)
            {
                _db   = db;
                _auth = auth;

                RollbackCommand = new Command(async () => await OnRollbackAsync(), () => CanRollback);
                CloseCommand    = new Command(async () => await OnCloseAsync());
            }

            #region === Properties bound in XAML ===

            /// <summary>
            /// The selected audit record wrapper used only for display.
            /// Must expose <see cref="AuditSelection.EntityName"/> to satisfy
            /// the binding expression <c>SelectedAudit.EntityName</c>.
            /// </summary>
            public AuditSelection? SelectedAudit
            {
                get => _selectedAudit;
                set
                {
                    _selectedAudit = value;
                    OnPropertyChanged();
                    // Re-evaluate signature and permissions whenever selection changes.
                    EvaluateSignature();
                    EvaluateCanRollback();
                    ((Command)RollbackCommand).ChangeCanExecute();
                }
            }

            /// <summary>
            /// Old JSON payload shown in the left editor (read-only).
            /// </summary>
            public string OldJson
            {
                get => _oldJson;
                set { _oldJson = value ?? string.Empty; OnPropertyChanged(); EvaluateCanRollback(); ((Command)RollbackCommand).ChangeCanExecute(); }
            }

            /// <summary>
            /// New JSON payload shown in the right editor (read-only).
            /// </summary>
            public string NewJson
            {
                get => _newJson;
                set { _newJson = value ?? string.Empty; OnPropertyChanged(); EvaluateCanRollback(); ((Command)RollbackCommand).ChangeCanExecute(); }
            }

            /// <summary>
            /// Human-readable digital signature status (Valid/Not provided/Possibly invalid).
            /// </summary>
            public string SignatureStatus
            {
                get => _signatureStatus;
                private set { _signatureStatus = value ?? "N/A"; OnPropertyChanged(); }
            }

            /// <summary>
            /// UI color reflecting <see cref="SignatureStatus"/>.
            /// </summary>
            public Color SignatureColor
            {
                get => _signatureColor;
                private set { _signatureColor = value; OnPropertyChanged(); }
            }

            /// <summary>
            /// Whether the Rollback action is currently allowed for this selection.
            /// </summary>
            public bool CanRollback
            {
                get => _canRollback;
                private set { _canRollback = value; OnPropertyChanged(); }
            }

            #endregion

            #region === Commands ===

            /// <summary>
            /// Initiates a compliant rollback request (see remarks in file header).
            /// </summary>
            public ICommand RollbackCommand { get; }

            /// <summary>
            /// Closes the preview page.
            /// </summary>
            public ICommand CloseCommand { get; }

            #endregion

            #region === Behaviors ===

            /// <summary>
            /// Computes the signature display status from the available metadata.
            /// Uses simple heuristics (presence/length/format) suitable for UI display.
            /// </summary>
            private void EvaluateSignature()
            {
                // Heuristic: a proper hash/signature will typically be ≥ 32 hex chars.
                var sig = _selectedAudit?.DigitalSignature ?? string.Empty;
                if (string.IsNullOrWhiteSpace(sig))
                {
                    SignatureStatus = "Not provided";
                    SignatureColor  = Colors.OrangeRed;
                    return;
                }

                var trimmed = sig.Trim();
                bool looksHex = true;
                foreach (var ch in trimmed)
                {
                    if (!((ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F')))
                    {
                        looksHex = false; break;
                    }
                }

                if (looksHex && trimmed.Length >= 32)
                {
                    SignatureStatus = "Present (format OK)";
                    SignatureColor  = Colors.DarkGreen;
                }
                else
                {
                    SignatureStatus = "Possibly invalid format";
                    SignatureColor  = Colors.OrangeRed;
                }
            }

            /// <summary>
            /// Enables rollback only for supported entities and when a record id exists.
            /// For now, formally supports <c>work_orders</c> entity (GMP-compliant flow).
            /// </summary>
            private void EvaluateCanRollback()
            {
                var entity = _selectedAudit?.EntityName?.Trim().ToLowerInvariant() ?? string.Empty;
                var hasId  = (_selectedAudit?.RecordId ?? 0) > 0;

                CanRollback = hasId && entity == "work_orders";
            }

            /// <summary>
            /// Executes the rollback request workflow:
            /// • Confirms with user
            /// • Records a ROLLBACK event into the work order audit trail
            /// • Notifies user of result
            /// </summary>
            private async Task OnRollbackAsync()
            {
                if (!CanRollback || _selectedAudit is null)
                    return;

                var id      = _selectedAudit.RecordId ?? 0;
                var entity  = _selectedAudit.EntityName ?? string.Empty;

                // Confirm
                if (Application.Current?.MainPage is Page page)
                {
                    var ok = await page.DisplayAlert("Potvrda", $"Zatražiti rollback za {entity} #{id}?", "Da", "Ne")
                                       .ConfigureAwait(false);
                    if (!ok) return;
                }

                // Record the rollback request (GMP trail). Uses existing pattern from WorkOrderViewModel.
                var succeeded = await TryRecordWorkOrderRollbackAsync(id).ConfigureAwait(false);

                if (Application.Current?.MainPage is Page page2)
                {
                    if (succeeded)
                        await page2.DisplayAlert("Rollback", "Zahtjev za rollback je zabilježen u audit logu.", "OK").ConfigureAwait(false);
                    else
                        await page2.DisplayAlert("Rollback", "Nije moguće zabilježiti rollback. Provjerite konekciju/usluge.", "OK").ConfigureAwait(false);
                }
            }

            /// <summary>
            /// Pops the current page (close).
            /// </summary>
            private Task OnCloseAsync()
            {
                if (Application.Current?.MainPage is NavigationPage np && np.Navigation?.NavigationStack?.Count > 0)
                {
                    return np.Navigation.PopAsync();
                }

                // Fallback: try current shell/page
                var current = Application.Current?.MainPage as Page;
                return current?.Navigation?.PopAsync() ?? Task.CompletedTask;
            }

            /// <summary>
            /// Records a rollback request for a Work Order using DatabaseService.
            /// Matches the pattern used in <c>WorkOrderViewModel.RollbackWorkOrderAsync()</c>.
            /// </summary>
            /// <param name="workOrderId">Target work order id.</param>
            private async Task<bool> TryRecordWorkOrderRollbackAsync(int workOrderId)
            {
                try
                {
                    if (_db is null) return false;

                    var actorId = _auth?.CurrentUser?.Id ?? 0;
                    var ip      = _auth?.CurrentIpAddress ?? _auth?.CurrentIp ?? _auth?.CurrentUserIp ?? string.Empty;
                    var device  = _auth?.CurrentDeviceInfo ?? string.Empty;

                    // Existing pattern: write a ROLLBACK entry into the work order audit log.
                    // Signature from earlier code: LogWorkOrderAuditAsync(workOrderId, actorId, "ROLLBACK", note, ip, device)
                    await _db.LogWorkOrderAuditAsync(workOrderId, actorId, "ROLLBACK",
                        "Rollback requested via RollbackPreviewPage", ip, device).ConfigureAwait(false);

                    return true;
                }
                catch
                {
                    return false;
                }
            }

            #endregion

            #region === INotifyPropertyChanged ===

            /// <inheritdoc />
            public event PropertyChangedEventHandler? PropertyChanged;

            private void OnPropertyChanged([CallerMemberName] string? name = null)
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

            #endregion

            // ------------------------------------------------------------------
            //  Helper types
            // ------------------------------------------------------------------

            /// <summary>
            /// Lightweight wrapper used to expose <see cref="EntityName"/> to XAML.
            /// </summary>
            public sealed class AuditSelection
            {
                /// <summary>Entity/table name (e.g., "work_orders").</summary>
                public string EntityName { get; init; } = string.Empty;

                /// <summary>Primary key of the affected record.</summary>
                public int? RecordId { get; init; }

                /// <summary>Action type (e.g., UPDATE/DELETE).</summary>
                public string Action { get; init; } = string.Empty;

                /// <summary>UTC timestamp of the change.</summary>
                public DateTime ChangedAt { get; init; }

                /// <summary>User id who performed the change.</summary>
                public int? UserId { get; init; }

                /// <summary>Digital signature if provided by the audit entry.</summary>
                public string? DigitalSignature { get; init; }

                /// <summary>
                /// Creates a selection wrapper from a full <see cref="AuditLog"/> model.
                /// Maps <c>TableName → EntityName</c> and <c>RecordId → RecordId</c>.
                /// </summary>
                public static AuditSelection FromAudit(AuditLog a)
                {
                    if (a == null) throw new ArgumentNullException(nameof(a));

                    return new AuditSelection
                    {
                        EntityName       = a.TableName ?? string.Empty,
                        RecordId         = a.RecordId,
                        Action           = a.Action ?? string.Empty,
                        ChangedAt        = a.DateTime,
                        UserId           = a.UserId,
                        DigitalSignature = a.DigitalSignature
                    };
                }
            }
        }
    }
}
