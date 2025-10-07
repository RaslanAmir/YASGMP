using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using YasGMP.Models;
using YasGMP.Models.DTO;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Common;

namespace YasGMP.Views.Dialogs
{
    /// <summary>
    /// Modal editor for creating and updating work orders with linked resources.
    /// </summary>
    public partial class WorkOrderEditDialog : ContentPage
    {
        private readonly DatabaseService _db;
        private readonly IAttachmentService _attachments;
        private readonly int _currentUserId;
        private readonly AuthService _authService;
        private readonly AuditService _auditService;
        private readonly IRBACService _rbacService;

        private static readonly DateTime NoDateSentinel = DateTime.MinValue.Date;

        private bool _suppressDueDateSync;
        private bool _suppressCloseDateSync;
        /// <summary>
        /// Gets or sets the work order.
        /// </summary>

        public WorkOrder WorkOrder { get; }
        /// <summary>
        /// Executes the tcs operation.
        /// </summary>
        public TaskCompletionSource<bool> _tcs = new();
        /// <summary>
        /// Gets or sets the result.
        /// </summary>
        public Task<bool> Result => _tcs.Task;

        private List<(string name, int id)> _machines = new();
        private List<(string name, int id)> _components = new();
        private List<(string name, int id)> _users = new();
        /// <summary>
        /// Represents the due date picker value property value.
        /// </summary>

        public static readonly BindableProperty DueDatePickerValueProperty =
            BindableProperty.Create(nameof(DueDatePickerValue), typeof(DateTime), typeof(WorkOrderEditDialog), DateTime.Today,
                BindingMode.TwoWay, propertyChanged: OnDueDatePickerValueChanged);
        /// <summary>
        /// Represents the date close picker value property value.
        /// </summary>

        public static readonly BindableProperty DateClosePickerValueProperty =
            BindableProperty.Create(nameof(DateClosePickerValue), typeof(DateTime), typeof(WorkOrderEditDialog), DateTime.Today,
                BindingMode.TwoWay, propertyChanged: OnDateClosePickerValueChanged);
        /// <summary>
        /// Represents the due date picker value value.
        /// </summary>

        public DateTime DueDatePickerValue
        {
            get => (DateTime)GetValue(DueDatePickerValueProperty);
            set => SetValue(DueDatePickerValueProperty, value);
        }
        /// <summary>
        /// Represents the date close picker value value.
        /// </summary>

        public DateTime DateClosePickerValue
        {
            get => (DateTime)GetValue(DateClosePickerValueProperty);
            set => SetValue(DateClosePickerValueProperty, value);
        }
        /// <summary>
        /// Initializes a new instance of the WorkOrderEditDialog class.
        /// </summary>

        public WorkOrderEditDialog(
            WorkOrder wo,
            DatabaseService db,
            int currentUserId,
            IAttachmentService? attachmentService = null,
            AuthService? authService = null,
            AuditService? auditService = null,
            IRBACService? rbacService = null)
        {
            InitializeComponent();
            WorkOrder = wo;
            _db = db;
            _attachments = attachmentService ?? ServiceLocator.GetRequiredService<IAttachmentService>();
            _currentUserId = currentUserId;
            _authService = authService ?? ServiceLocator.GetService<AuthService>() ?? throw new InvalidOperationException("AuthService nije konfiguriran.");
            _auditService = auditService ?? ServiceLocator.GetService<AuditService>() ?? throw new InvalidOperationException("AuditService nije konfiguriran.");
            _rbacService = rbacService ?? ServiceLocator.GetService<IRBACService>() ?? throw new InvalidOperationException("RBACService nije konfiguriran.");
            BindingContext = WorkOrder;
            SyncDatePickersFromModel();
            _ = LoadLookupsAsync();
        }

        private static void OnDueDatePickerValueChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is WorkOrderEditDialog dialog && newValue is DateTime dt)
            {
                dialog.HandleDueDatePickerValueChanged(dt);
            }
        }

        private static void OnDateClosePickerValueChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is WorkOrderEditDialog dialog && newValue is DateTime dt)
            {
                dialog.HandleDateClosePickerValueChanged(dt);
            }
        }

        private void HandleDueDatePickerValueChanged(DateTime newValue)
        {
            if (_suppressDueDateSync || WorkOrder is null)
            {
                return;
            }

            if (IsSentinel(newValue))
            {
                WorkOrder.DueDate = null;
                ResetDueDatePickerValue();
            }
            else
            {
                WorkOrder.DueDate = newValue.Date;
            }
        }

        private void HandleDateClosePickerValueChanged(DateTime newValue)
        {
            if (_suppressCloseDateSync || WorkOrder is null)
            {
                return;
            }

            if (IsSentinel(newValue))
            {
                WorkOrder.DateClose = null;
                ResetDateClosePickerValue();
            }
            else
            {
                WorkOrder.DateClose = newValue.Date;
            }
        }

        private static bool IsSentinel(DateTime value) => value.Date <= NoDateSentinel;

        private void SyncDatePickersFromModel()
        {
            if (WorkOrder is null)
            {
                return;
            }

            _suppressDueDateSync = true;
            DueDatePickerValue = WorkOrder.DueDate ?? DateTime.Today;
            _suppressDueDateSync = false;

            _suppressCloseDateSync = true;
            DateClosePickerValue = WorkOrder.DateClose ?? DateTime.Today;
            _suppressCloseDateSync = false;
        }

        private void ResetDueDatePickerValue()
        {
            _suppressDueDateSync = true;
            DueDatePickerValue = DateTime.Today;
            _suppressDueDateSync = false;
        }

        private void ResetDateClosePickerValue()
        {
            _suppressCloseDateSync = true;
            DateClosePickerValue = DateTime.Today;
            _suppressCloseDateSync = false;
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
            if (WorkOrder == null || WorkOrder.Id <= 0)
            {
                await DisplayAlert("Potpis", "Radni nalog mora biti spremljen prije potpisa.", "OK");
                return;
            }

            try
            {
                var defaultUser = _authService.CurrentUser?.Username
                    ?? (Application.Current as App)?.LoggedUser?.Username;
                var dialog = new ReauthenticationDialog(defaultUser);
                await Navigation.PushModalAsync(dialog);
                var reauth = await dialog.Result;
                if (reauth == null)
                {
                    await LogSignatureAttemptAsync(null, "cancelled").ConfigureAwait(false);
                    return;
                }

                var authenticatedUser = await _authService.AuthenticateAsync(reauth.Username, reauth.Password).ConfigureAwait(false);
                if (authenticatedUser == null)
                {
                    await LogSignatureAttemptAsync(null, $"auth_failed:{reauth.Username}").ConfigureAwait(false);
                    await DisplayAlert("Potpis", "Provjera vjerodajnica nije uspjela.", "OK");
                    return;
                }

                if (!string.IsNullOrWhiteSpace(reauth.MfaCode))
                {
                    bool mfaOk = await _authService.VerifyTwoFactorCodeAsync(reauth.Username, reauth.MfaCode).ConfigureAwait(false);
                    if (!mfaOk)
                    {
                        await LogSignatureAttemptAsync(authenticatedUser, "mfa_failed").ConfigureAwait(false);
                        await DisplayAlert("Potpis", "Nevažeći MFA kod.", "OK");
                        return;
                    }
                }

                if (!await EnsureSignerPermissionAsync(authenticatedUser.Id).ConfigureAwait(false))
                {
                    await LogSignatureAttemptAsync(authenticatedUser, "rbac_denied").ConfigureAwait(false);
                    await DisplayAlert("Potpis", "Nemate dozvolu potpisati ovaj nalog.", "OK");
                    return;
                }

                int recordVersion = await _db.GetNextWorkOrderSignatureVersionAsync(WorkOrder.Id).ConfigureAwait(false);
                int revisionNo = await _db.GetCurrentWorkOrderRevisionAsync(WorkOrder.Id).ConfigureAwait(false);
                DateTime signedAtUtc = DateTime.UtcNow;

                string snapshotJson = BuildWorkOrderSnapshotJson();
                string recordHash = ComputeRecordHash(snapshotJson);
                string signatureHash = ComputeSignatureHash(authenticatedUser, reauth, recordHash, recordVersion, revisionNo, signedAtUtc);

                var request = new WorkOrderSignaturePersistRequest
                {
                    WorkOrderId = WorkOrder.Id,
                    UserId = authenticatedUser.Id,
                    SignatureHash = signatureHash,
                    ReasonCode = reauth.ReasonCode,
                    ReasonDescription = reauth.ReasonDetail,
                    SignatureType = "potvrda",
                    RecordHash = recordHash,
                    RecordVersion = recordVersion,
                    SignedAtUtc = signedAtUtc,
                    ServerTimeZone = TimeZoneInfo.Local.Id,
                    IpAddress = _authService.CurrentIpAddress,
                    DeviceInfo = _authService.CurrentDeviceInfo,
                    SessionId = _authService.CurrentSessionId,
                    RevisionNo = revisionNo,
                    MfaEvidence = string.IsNullOrWhiteSpace(reauth.MfaCode) ? null : AuthService.HashPassword(reauth.MfaCode),
                    WorkOrderSnapshotJson = snapshotJson,
                    SignerUsername = authenticatedUser.Username ?? reauth.Username,
                    SignerFullName = authenticatedUser.FullName,
                    ReasonDisplay = reauth.ReasonDisplay
                };

                var signature = await _db.AddWorkOrderSignatureAsync(request, _attachments).ConfigureAwait(false);
                WorkOrder.Signatures ??= new List<WorkOrderSignature>();
                WorkOrder.Signatures.Add(signature);

                await LogSignatureAttemptAsync(authenticatedUser, $"success:{signature.Id}").ConfigureAwait(false);
                await DisplayAlert("Potpis", "Potpis je uspješno spremljen.", "OK");
            }
            catch (Exception ex)
            {
                await LogSignatureAttemptAsync(null, $"error:{ex.Message}").ConfigureAwait(false);
                await DisplayAlert("Potpis", $"Neuspješan potpis: {ex.Message}", "OK");
            }
        }

        private async Task<bool> EnsureSignerPermissionAsync(int userId)
        {
            try
            {
                if (await _rbacService.HasPermissionAsync(userId, PermissionCodes.WorkOrders.Sign).ConfigureAwait(false))
                    return true;
                return await _rbacService.HasPermissionAsync(userId, PermissionCodes.SystemAdmin).ConfigureAwait(false);
            }
            catch
            {
                return false;
            }
        }

        private static string ComputeRecordHash(string snapshotJson)
        {
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(snapshotJson ?? string.Empty)));
        }

        private string BuildWorkOrderSnapshotJson()
        {
            var snapshot = new
            {
                WorkOrder.Id,
                WorkOrder.Title,
                WorkOrder.Description,
                WorkOrder.TaskDescription,
                WorkOrder.Type,
                WorkOrder.Priority,
                WorkOrder.Status,
                WorkOrder.DateOpen,
                WorkOrder.DueDate,
                WorkOrder.DateClose,
                WorkOrder.MachineId,
                WorkOrder.ComponentId,
                WorkOrder.AssignedToId,
                WorkOrder.RequestedById,
                WorkOrder.CreatedById,
                WorkOrder.Result,
                WorkOrder.Notes,
                WorkOrder.LastModified,
                WorkOrder.LastModifiedById
            };

            return JsonSerializer.Serialize(snapshot, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
        }

        private static string ComputeSignatureHash(User signer, ReauthenticationResult reauth, string recordHash, int recordVersion, int revisionNo, DateTime signedAtUtc)
        {
            string payload = string.Join('|', new[]
            {
                signer.Id.ToString(CultureInfo.InvariantCulture),
                signer.Username ?? string.Empty,
                recordHash,
                $"v{recordVersion}",
                $"rev{revisionNo}",
                reauth.ReasonCode,
                reauth.ReasonDetail ?? string.Empty,
                signedAtUtc.ToString("O", CultureInfo.InvariantCulture)
            });

            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(payload)));
        }

        private async Task LogSignatureAttemptAsync(User? user, string outcome)
        {
            if (_auditService == null) return;
            try
            {
                string username = user?.Username ?? string.Empty;
                string descriptor = $"wo={WorkOrder?.Id}; outcome={outcome}; user={username}";
                await _auditService.LogSystemEventAsync("WO_SIGNATURE_ATTEMPT", descriptor, "work_order_signatures", WorkOrder?.Id).ConfigureAwait(false);
            }
            catch
            {
                // audit logging is best effort for UI events
            }
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
                    await _db.AttachWorkOrderPhotoAsync(WorkOrder.Id, fs, Path.GetFileName(f.FullPath), kind, _currentUserId, _attachments).ConfigureAwait(false);
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


