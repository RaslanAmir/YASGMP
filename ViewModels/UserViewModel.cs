using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Helpers;
using CommunityToolkit.Mvvm.Input;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// ViewModel for robust user management with CRUD, filtering, and full audit trail.
    /// 21 CFR Part 11 / EU GMP Annex 11 ready.
    /// </summary>
    public class UserViewModel : INotifyPropertyChanged
    {
        #region Fields & Constructor

        private readonly DatabaseService _dbService;
        private readonly AuthService _authService;

        private ObservableCollection<User> _users = new();
        private ObservableCollection<User> _filteredUsers = new();

        // Selection/filters can be null -> mark accordingly
        private User? _selectedUser;
        private string? _searchTerm;
        private string? _roleFilter;

        private bool _showInactive;
        private bool _isBusy;
        private string? _statusMessage;

        private readonly string _currentSessionId;
        private readonly string _currentDeviceInfo;
        private readonly string _currentIpAddress;

        /// <summary>
        /// Initializes the <see cref="UserViewModel"/> and wires all commands.
        /// </summary>
        public UserViewModel(DatabaseService dbService, AuthService authService)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            // Coalesce to non-null forensic strings (CS8601)
            _currentSessionId = _authService.CurrentSessionId ?? string.Empty;
            _currentDeviceInfo = _authService.CurrentDeviceInfo ?? "ui";
            _currentIpAddress  = _authService.CurrentIpAddress  ?? "0.0.0.0";

            LoadUsersCommand     = new AsyncRelayCommand(LoadUsersAsync);
            AddUserCommand       = new AsyncRelayCommand(AddUserAsync,      () => !IsBusy);
            UpdateUserCommand    = new AsyncRelayCommand(UpdateUserAsync,   () => !IsBusy && SelectedUser is not null);
            DeleteUserCommand    = new AsyncRelayCommand(DeleteUserAsync,   () => !IsBusy && SelectedUser is not null);
            RollbackUserCommand  = new AsyncRelayCommand(RollbackUserAsync, () => !IsBusy && SelectedUser is not null);
            ExportUsersCommand   = new AsyncRelayCommand(ExportUsersAsync,  () => !IsBusy);
            FilterChangedCommand = new RelayCommand(FilterUsers);

            _ = LoadUsersAsync();
        }

        #endregion

        #region Properties

        /// <summary>All users.</summary>
        public ObservableCollection<User> Users
        {
            get => _users;
            set { _users = value; OnPropertyChanged(); }
        }

        /// <summary>Filtered users for UI.</summary>
        public ObservableCollection<User> FilteredUsers
        {
            get => _filteredUsers;
            set { _filteredUsers = value; OnPropertyChanged(); }
        }

        /// <summary>Currently selected user (nullable by design).</summary>
        public User? SelectedUser
        {
            get => _selectedUser;
            set { _selectedUser = value; OnPropertyChanged(); }
        }

        /// <summary>Text search term.</summary>
        public string? SearchTerm
        {
            get => _searchTerm;
            set { _searchTerm = value; OnPropertyChanged(); FilterUsers(); }
        }

        /// <summary>Role filter.</summary>
        public string? RoleFilter
        {
            get => _roleFilter;
            set { _roleFilter = value; OnPropertyChanged(); FilterUsers(); }
        }

        /// <summary>Include inactive users?</summary>
        public bool ShowInactive
        {
            get => _showInactive;
            set { _showInactive = value; OnPropertyChanged(); FilterUsers(); }
        }

        /// <summary>Busy flag for UI.</summary>
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        /// <summary>Status message for UI.</summary>
        public string? StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        /// <summary>Available roles for filtering.</summary>
        public string[] AvailableRoles => new[] { "tehnicar", "auditor", "sef", "admin", "superadmin" };

        #endregion

        #region Commands
        /// <summary>
        /// Gets or sets the load users command.
        /// </summary>

        public ICommand LoadUsersCommand { get; }
        /// <summary>
        /// Gets or sets the add user command.
        /// </summary>
        public ICommand AddUserCommand { get; }
        /// <summary>
        /// Gets or sets the update user command.
        /// </summary>
        public ICommand UpdateUserCommand { get; }
        /// <summary>
        /// Gets or sets the delete user command.
        /// </summary>
        public ICommand DeleteUserCommand { get; }
        /// <summary>
        /// Gets or sets the rollback user command.
        /// </summary>
        public ICommand RollbackUserCommand { get; }
        /// <summary>
        /// Gets or sets the export users command.
        /// </summary>
        public ICommand ExportUsersCommand { get; }
        /// <summary>
        /// Gets or sets the filter changed command.
        /// </summary>
        public ICommand FilterChangedCommand { get; }

        #endregion

        #region Methods

        /// <summary>Loads all users using DatabaseService user operations.</summary>
        public async Task LoadUsersAsync()
        {
            IsBusy = true;
            try
            {
                var users = await _dbService.GetAllUsersAsync(includeAudit: true).ConfigureAwait(false);
                Users = new ObservableCollection<User>(users ?? new());
                FilterUsers();
                StatusMessage = $"Loaded {Users.Count} users.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading users: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>Adds the currently selected user and writes user audit.</summary>
        public async Task AddUserAsync()
        {
            if (SelectedUser is null) { StatusMessage = "No user selected."; return; }
            IsBusy = true;
            try
            {
                string signatureHash = ComputeSignature(SelectedUser, _currentSessionId, _currentDeviceInfo);

                await DatabaseServiceRbacExtensions.AddUserAsync(_dbService, SelectedUser, signatureHash, _currentIpAddress, _currentDeviceInfo, _currentSessionId).ConfigureAwait(false);
                await _dbService.LogUserAuditAsync(SelectedUser, "CREATE", _currentIpAddress, _currentDeviceInfo, _currentSessionId, signatureHash).ConfigureAwait(false);

                StatusMessage = $"User '{SelectedUser.FullName}' added successfully.";
                await LoadUsersAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Add failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>Updates the currently selected user and writes user audit.</summary>
        public async Task UpdateUserAsync()
        {
            if (SelectedUser is null) { StatusMessage = "No user selected."; return; }
            IsBusy = true;
            try
            {
                string signatureHash = ComputeSignature(SelectedUser, _currentSessionId, _currentDeviceInfo);

                await DatabaseServiceRbacExtensions.UpdateUserAsync(_dbService, SelectedUser, signatureHash, _currentIpAddress, _currentDeviceInfo, _currentSessionId).ConfigureAwait(false);
                await _dbService.LogUserAuditAsync(SelectedUser, "UPDATE", _currentIpAddress, _currentDeviceInfo, _currentSessionId, signatureHash).ConfigureAwait(false);

                StatusMessage = $"User '{SelectedUser.FullName}' updated.";
                await LoadUsersAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Update failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>Deletes the currently selected user and writes user audit.</summary>
        public async Task DeleteUserAsync()
        {
            if (SelectedUser is null) { StatusMessage = "No user selected."; return; }
            IsBusy = true;
            try
            {
                await DatabaseServiceRbacExtensions.DeleteUserAsync(_dbService, SelectedUser.Id, _currentIpAddress, _currentDeviceInfo, _currentSessionId).ConfigureAwait(false);
                await _dbService.LogUserAuditAsync(SelectedUser, "DELETE", _currentIpAddress, _currentDeviceInfo, _currentSessionId, null).ConfigureAwait(false);

                StatusMessage = $"User '{SelectedUser.FullName}' deleted.";
                await LoadUsersAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Delete failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>Logical rollback (audit/log only) for selected user.</summary>
        public async Task RollbackUserAsync()
        {
            if (SelectedUser is null) { StatusMessage = "No user selected."; return; }
            IsBusy = true;
            try
            {
                await DatabaseServiceRbacExtensions.RollbackUserAsync(_dbService, SelectedUser.Id, _currentIpAddress, _currentDeviceInfo, _currentSessionId).ConfigureAwait(false);
                StatusMessage = $"Rollback completed for user '{SelectedUser.FullName}'.";
                await LoadUsersAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Rollback failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>Exports current filtered users and writes export audit.</summary>
        public async Task ExportUsersAsync()
        {
            IsBusy = true;
            try
            {
                await DatabaseServiceRbacExtensions.ExportUsersAsync(_dbService, FilteredUsers.ToList(), _currentIpAddress, _currentDeviceInfo, _currentSessionId).ConfigureAwait(false);
                await _dbService.LogUserAuditAsync(null, "EXPORT", _currentIpAddress, _currentDeviceInfo, _currentSessionId, null).ConfigureAwait(false);

                StatusMessage = "Users exported successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>Applies in-memory filtering on <see cref="Users"/>.</summary>
        public void FilterUsers()
        {
            var filtered = Users.Where(u =>
                (string.IsNullOrWhiteSpace(SearchTerm) || (u.FullName?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) || (u.Username?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (string.IsNullOrWhiteSpace(RoleFilter) || u.Role == RoleFilter) &&
                (ShowInactive || u.Active)
            );
            FilteredUsers = new ObservableCollection<User>(filtered);
        }

        /// <summary>True if current user can edit user records.</summary>
        public bool CanEditUsers => _authService.CurrentUser?.Role == "admin" || _authService.CurrentUser?.Role == "superadmin";

        #endregion

        #region Audit mapping (DTO→Model tolerant)

        /// <summary>Loads raw audit DTO and maps to <see cref="AuditLogEntry"/>.</summary>
        public async Task<ObservableCollection<AuditLogEntry>> LoadUserAuditAsync(int userId)
        {
            var raw = await _dbService.GetAuditLogForEntityAsync("users", userId).ConfigureAwait(false);
            var mapped = raw?.Select(MapAuditEntryToModel) ?? Enumerable.Empty<AuditLogEntry>();
            return new ObservableCollection<AuditLogEntry>(mapped);
        }

        private static AuditLogEntry MapAuditEntryToModel(object src)
        {
            if (src is AuditLogEntry a) return a;

            object? Get(string n1, string? n2 = null, string? n3 = null)
            {
                var t = src.GetType();
                var p = t.GetProperty(n1, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                        ?? (n2 != null ? t.GetProperty(n2, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase) : null)
                        ?? (n3 != null ? t.GetProperty(n3, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase) : null);
                return p?.GetValue(src);
            }

            T? Cast<T>(object? o)
            {
                if (o is null || o is DBNull) return default;
                try { return (T)Convert.ChangeType(o, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T)); }
                catch { return default; }
            }

            return new AuditLogEntry
            {
                Id          = Cast<int>(Get("Id")),
                TableName   = Cast<string>(Get("Table", "TableName", "EntityTable")) ?? "users",
                EntityId    = Cast<int?>(Get("EntityId", "RecordId", "TargetId")),
                Action      = Cast<string>(Get("Action", "EventType")) ?? string.Empty,
                Description = Cast<string>(Get("Description", "Details", "Message")),
                Timestamp   = Cast<DateTime?>(Get("Timestamp", "ChangedAt", "CreatedAt")) ?? DateTime.UtcNow,
                UserId      = Cast<int?>(Get("UserId", "ActorUserId", "ChangedBy")),
                SourceIp    = Cast<string>(Get("SourceIp", "Ip", "IPAddress")),
                DeviceInfo  = Cast<string>(Get("DeviceInfo", "Device")),
                SessionId   = Cast<string>(Get("SessionId", "Session"))
            };
        }

        #endregion

        #region INotifyPropertyChanged

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raises <see cref="PropertyChanged"/> for the provided property.
        /// </summary>
        /// <param name="propName">Property name (auto-filled by compiler when omitted).</param>
        private void OnPropertyChanged([CallerMemberName] string? propName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName ?? string.Empty));

        #endregion

        #region Signature Helper

        /// <summary>
        /// Computes a GMP-grade digital signature for the specified entity using a canonical,
        /// reflection-driven snapshot salted with session/device info. SHA-256, hex.
        /// </summary>
        /// <param name="entity">Entity to sign.</param>
        /// <param name="sessionId">Authenticated session identifier.</param>
        /// <param name="deviceInfo">Device fingerprint string.</param>
        /// <returns>Uppercase hex SHA-256 signature.</returns>
        private static string ComputeSignature(object entity, string sessionId, string deviceInfo)
        {
            if (entity == null) return string.Empty;

            var sb = new StringBuilder();
            sb.Append("SID=").Append(sessionId ?? "").Append('|');
            sb.Append("DEV=").Append(deviceInfo ?? "").Append('|');

            var props = entity.GetType()
                              .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                              .Where(p => p.CanRead)
                              .OrderBy(p => p.Name, StringComparer.Ordinal);

            foreach (var p in props)
            {
                object? val;
                try { val = p.GetValue(entity); }
                catch { continue; }

                var str = val switch
                {
                    null => "",
                    DateTime dt => dt.ToUniversalTime().ToString("O"),
                    DateTimeOffset dto => dto.ToUniversalTime().ToString("O"),
                    byte[] bytes => Convert.ToBase64String(bytes),
                    _ => val.ToString() ?? ""
                };

                sb.Append(p.Name).Append('=').Append(str).Append(';');
            }

            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
            var hex = new StringBuilder(hash.Length * 2);
            foreach (var b in hash) hex.Append(b.ToString("X2"));
            return hex.ToString();
        }

        #endregion
    }
}
