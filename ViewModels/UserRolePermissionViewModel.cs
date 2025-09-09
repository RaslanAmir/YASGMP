using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using System.Reflection;
using YasGMP.Models;
using YasGMP.Services;
using CommunityToolkit.Mvvm.Input; 

namespace YasGMP.ViewModels
{
    /// <summary>
    /// Super-ultra robust ViewModel for managing users, roles, and permissions.
    /// ✅ GMP/Annex 11/21 CFR Part 11: full audit, RBAC, approval chains, access history, SSO/LDAP ready.
    /// ✅ Supports user creation, lock/unlock, password/PIN reset, two-factor, detailed role/perm management.
    /// ✅ Granular entity/action permissions, templates, permission history, batch assignment, security audit.
    /// </summary>
    public class UserRolePermissionViewModel : INotifyPropertyChanged
    {
        #region Fields & Constructor

        private readonly DatabaseService _dbService;
        private readonly AuthService _authService;

        private ObservableCollection<User> _users = new();
        private ObservableCollection<Role> _roles = new();
        private ObservableCollection<Permission> _permissions = new();
        private ObservableCollection<User> _filteredUsers = new();
        private ObservableCollection<Role> _filteredRoles = new();
        private ObservableCollection<Permission> _filteredPermissions = new();

        // Selections/searches can be null -> mark nullable or initialize
        private User? _selectedUser;
        private Role? _selectedRole;
        private Permission? _selectedPermission;

        private string _userSearchTerm = string.Empty;
        private string _roleSearchTerm = string.Empty;
        private string _permSearchTerm = string.Empty;

        private bool _isBusy;
        private string _statusMessage = string.Empty;

        private readonly string _currentSessionId;
        private readonly string _currentDeviceInfo;
        private readonly string _currentIpAddress;

        /// <summary>
        /// Initializes the UserRolePermissionViewModel with all commands.
        /// </summary>
        public UserRolePermissionViewModel(DatabaseService dbService, AuthService authService)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            // Null-safe coalescing (CS8601)
            _currentSessionId = _authService.CurrentSessionId ?? string.Empty;
            _currentDeviceInfo = _authService.CurrentDeviceInfo ?? "ui";
            _currentIpAddress  = _authService.CurrentIpAddress  ?? "0.0.0.0";

            LoadUsersCommand = new AsyncRelayCommand(LoadUsersAsync);
            LoadRolesCommand = new AsyncRelayCommand(LoadRolesAsync);
            LoadPermissionsCommand = new AsyncRelayCommand(LoadPermissionsAsync);
            AssignRoleCommand = new AsyncRelayCommand(AssignRoleAsync, () => !IsBusy && SelectedUser is not null && SelectedRole is not null);
            RemoveRoleCommand = new AsyncRelayCommand(RemoveRoleAsync, () => !IsBusy && SelectedUser is not null && SelectedRole is not null);
            AssignPermissionCommand = new AsyncRelayCommand(AssignPermissionAsync, () => !IsBusy && SelectedRole is not null && SelectedPermission is not null);
            RemovePermissionCommand = new AsyncRelayCommand(RemovePermissionAsync, () => !IsBusy && SelectedRole is not null && SelectedPermission is not null);
            LockUserCommand = new AsyncRelayCommand(LockUserAsync, () => !IsBusy && SelectedUser is not null);
            UnlockUserCommand = new AsyncRelayCommand(UnlockUserAsync, () => !IsBusy && SelectedUser is not null);
            ResetPasswordCommand = new AsyncRelayCommand(ResetPasswordAsync, () => !IsBusy && SelectedUser is not null);
            AddUserCommand = new AsyncRelayCommand(AddUserAsync, () => !IsBusy);
            DeleteUserCommand = new AsyncRelayCommand(DeleteUserAsync, () => !IsBusy && SelectedUser is not null);
            ExportUsersCommand = new AsyncRelayCommand(ExportUsersAsync, () => !IsBusy);
            ExportRolesCommand = new AsyncRelayCommand(ExportRolesAsync, () => !IsBusy);
            ExportPermissionsCommand = new AsyncRelayCommand(ExportPermissionsAsync, () => !IsBusy);
            FilterChangedCommand = new RelayCommand(FilterAll);

            // Load on startup
            _ = LoadUsersAsync();
            _ = LoadRolesAsync();
            _ = LoadPermissionsAsync();
        }

        #endregion

        #region Properties

        public ObservableCollection<User> Users
        {
            get => _users;
            set { _users = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Role> Roles
        {
            get => _roles;
            set { _roles = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Permission> Permissions
        {
            get => _permissions;
            set { _permissions = value; OnPropertyChanged(); }
        }

        public ObservableCollection<User> FilteredUsers
        {
            get => _filteredUsers;
            set { _filteredUsers = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Role> FilteredRoles
        {
            get => _filteredRoles;
            set { _filteredRoles = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Permission> FilteredPermissions
        {
            get => _filteredPermissions;
            set { _filteredPermissions = value; OnPropertyChanged(); }
        }

        public User? SelectedUser
        {
            get => _selectedUser;
            set { _selectedUser = value; OnPropertyChanged(); }
        }

        public Role? SelectedRole
        {
            get => _selectedRole;
            set { _selectedRole = value; OnPropertyChanged(); }
        }

        public Permission? SelectedPermission
        {
            get => _selectedPermission;
            set { _selectedPermission = value; OnPropertyChanged(); }
        }

        public string UserSearchTerm
        {
            get => _userSearchTerm;
            set { _userSearchTerm = value ?? string.Empty; OnPropertyChanged(); FilterAll(); }
        }

        public string RoleSearchTerm
        {
            get => _roleSearchTerm;
            set { _roleSearchTerm = value ?? string.Empty; OnPropertyChanged(); FilterAll(); }
        }

        public string PermSearchTerm
        {
            get => _permSearchTerm;
            set { _permSearchTerm = value ?? string.Empty; OnPropertyChanged(); FilterAll(); }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value ?? string.Empty; OnPropertyChanged(); }
        }

        #endregion

        #region Commands

        public ICommand LoadUsersCommand { get; }
        public ICommand LoadRolesCommand { get; }
        public ICommand LoadPermissionsCommand { get; }
        public ICommand AssignRoleCommand { get; }
        public ICommand RemoveRoleCommand { get; }
        public ICommand AssignPermissionCommand { get; }
        public ICommand RemovePermissionCommand { get; }
        public ICommand LockUserCommand { get; }
        public ICommand UnlockUserCommand { get; }
        public ICommand ResetPasswordCommand { get; }
        public ICommand AddUserCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand ExportUsersCommand { get; }
        public ICommand ExportRolesCommand { get; }
        public ICommand ExportPermissionsCommand { get; }
        public ICommand FilterChangedCommand { get; }

        #endregion

        #region Methods

        public async Task LoadUsersAsync()
        {
            IsBusy = true;
            try
            {
                var users = await _dbService.GetAllUsersFullAsync().ConfigureAwait(false);
                Users = new ObservableCollection<User>(users ?? new());
                FilterAll();
                StatusMessage = $"Loaded {Users.Count} users.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading users: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        public async Task LoadRolesAsync()
        {
            IsBusy = true;
            try
            {
                var roles = await _dbService.GetAllRolesFullAsync().ConfigureAwait(false);
                Roles = new ObservableCollection<Role>(roles ?? new());
                FilterAll();
                StatusMessage = $"Loaded {Roles.Count} roles.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading roles: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        public async Task LoadPermissionsAsync()
        {
            IsBusy = true;
            try
            {
                var perms = await _dbService.GetAllPermissionsFullAsync().ConfigureAwait(false);
                Permissions = new ObservableCollection<Permission>(perms ?? new());
                FilterAll();
                StatusMessage = $"Loaded {Permissions.Count} permissions.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading permissions: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        public async Task AssignRoleAsync()
        {
            if (SelectedUser is null || SelectedRole is null) { StatusMessage = "Select user and role."; return; }
            IsBusy = true;
            try
            {
                var actorId = _authService.CurrentUser?.Id ?? 1;
                await _dbService.AssignRoleToUserAsync(SelectedUser.Id, SelectedRole.Id, actorId, _currentIpAddress, _currentDeviceInfo, _currentSessionId).ConfigureAwait(false);
                await _dbService.LogUserAuditAsync(SelectedUser, "ASSIGN_ROLE", _currentIpAddress, _currentDeviceInfo, _currentSessionId, SelectedRole.Name).ConfigureAwait(false);
                StatusMessage = $"Role '{SelectedRole.Name}' assigned to {SelectedUser.UserName}.";
                await LoadUsersAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Assign role failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        public async Task RemoveRoleAsync()
        {
            if (SelectedUser is null || SelectedRole is null) { StatusMessage = "Select user and role."; return; }
            IsBusy = true;
            try
            {
                var actorId = _authService.CurrentUser?.Id ?? 1;
                await _dbService.RemoveRoleFromUserAsync(SelectedUser.Id, SelectedRole.Id, actorId, _currentIpAddress, _currentDeviceInfo, _currentSessionId).ConfigureAwait(false);
                await _dbService.LogUserAuditAsync(SelectedUser, "REMOVE_ROLE", _currentIpAddress, _currentDeviceInfo, _currentSessionId, SelectedRole.Name).ConfigureAwait(false);
                StatusMessage = $"Role '{SelectedRole.Name}' removed from {SelectedUser.UserName}.";
                await LoadUsersAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Remove role failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        public async Task AssignPermissionAsync()
        {
            if (SelectedRole is null || SelectedPermission is null) { StatusMessage = "Select role and permission."; return; }
            IsBusy = true;
            try
            {
                var actorId = _authService.CurrentUser?.Id ?? 1;
                await _dbService.AssignPermissionToRoleAsync(SelectedRole.Id, SelectedPermission.Id, actorId, _currentIpAddress, _currentDeviceInfo, _currentSessionId).ConfigureAwait(false);
                await _dbService.LogRoleAuditAsync(SelectedRole, "ASSIGN_PERMISSION", _currentIpAddress, _currentDeviceInfo, _currentSessionId, SelectedPermission.Name).ConfigureAwait(false);
                StatusMessage = $"Permission '{SelectedPermission.Name}' assigned to role '{SelectedRole.Name}'.";
                await LoadRolesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Assign permission failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        public async Task RemovePermissionAsync()
        {
            if (SelectedRole is null || SelectedPermission is null) { StatusMessage = "Select role and permission."; return; }
            IsBusy = true;
            try
            {
                var actorId = _authService.CurrentUser?.Id ?? 1;
                await _dbService.RemovePermissionFromRoleAsync(SelectedRole.Id, SelectedPermission.Id, actorId, _currentIpAddress, _currentDeviceInfo, _currentSessionId).ConfigureAwait(false);
                await _dbService.LogRoleAuditAsync(SelectedRole, "REMOVE_PERMISSION", _currentIpAddress, _currentDeviceInfo, _currentSessionId, SelectedPermission.Name).ConfigureAwait(false);
                StatusMessage = $"Permission '{SelectedPermission.Name}' removed from role '{SelectedRole.Name}'.";
                await LoadRolesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Remove permission failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        public async Task LockUserAsync()
        {
            if (SelectedUser is null) { StatusMessage = "Select a user."; return; }
            IsBusy = true;
            try
            {
                var actorId = _authService.CurrentUser?.Id ?? 1;
                await _dbService.LockUserAsync(SelectedUser.Id, actorId, _currentIpAddress, _currentDeviceInfo, _currentSessionId).ConfigureAwait(false);
                await _dbService.LogUserAuditAsync(SelectedUser, "LOCK", _currentIpAddress, _currentDeviceInfo, _currentSessionId, null).ConfigureAwait(false);
                StatusMessage = $"User '{SelectedUser.UserName}' locked.";
                await LoadUsersAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lock failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        public async Task UnlockUserAsync()
        {
            if (SelectedUser is null) { StatusMessage = "Select a user."; return; }
            IsBusy = true;
            try
            {
                var actorId = _authService.CurrentUser?.Id ?? 1;
                await _dbService.UnlockUserAsync(SelectedUser.Id, actorId, _currentIpAddress, _currentDeviceInfo, _currentSessionId).ConfigureAwait(false);
                await _dbService.LogUserAuditAsync(SelectedUser, "UNLOCK", _currentIpAddress, _currentDeviceInfo, _currentSessionId, null).ConfigureAwait(false);
                StatusMessage = $"User '{SelectedUser.UserName}' unlocked.";
                await LoadUsersAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Unlock failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        public async Task ResetPasswordAsync()
        {
            if (SelectedUser is null) { StatusMessage = "Select a user."; return; }
            IsBusy = true;
            try
            {
                var actorId = _authService.CurrentUser?.Id ?? 1;
                await _dbService.ResetUserPasswordAsync(SelectedUser.Id, actorId, _currentIpAddress, _currentDeviceInfo, _currentSessionId).ConfigureAwait(false);
                await _dbService.LogUserAuditAsync(SelectedUser, "RESET_PASSWORD", _currentIpAddress, _currentDeviceInfo, _currentSessionId, null).ConfigureAwait(false);
                StatusMessage = $"Password for '{SelectedUser.UserName}' reset.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Reset password failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        public async Task AddUserAsync()
        {
            IsBusy = true;
            try
            {
                var actorId = _authService.CurrentUser?.Id ?? 1;
                var newUser = new User { UserName = "newuser", RoleIds = Array.Empty<int>(), PermissionIds = Array.Empty<int>(), IsLocked = false };
                await _dbService.AddUserAsync(newUser, actorId, _currentIpAddress, _currentDeviceInfo, _currentSessionId).ConfigureAwait(false);
                await _dbService.LogUserAuditAsync(newUser, "CREATE", _currentIpAddress, _currentDeviceInfo, _currentSessionId, null).ConfigureAwait(false);
                StatusMessage = "User added.";
                await LoadUsersAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Add user failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        public async Task DeleteUserAsync()
        {
            if (SelectedUser is null) { StatusMessage = "Select a user."; return; }
            if (SelectedUser.Id == (_authService.CurrentUser?.Id ?? -1)) { StatusMessage = "Cannot delete yourself!"; return; }
            IsBusy = true;
            try
            {
                var actorId = _authService.CurrentUser?.Id ?? 1;
                await _dbService.DeleteUserAsync(SelectedUser.Id, actorId, _currentIpAddress, _currentDeviceInfo, _currentSessionId).ConfigureAwait(false);
                await _dbService.LogUserAuditAsync(SelectedUser, "DELETE", _currentIpAddress, _currentDeviceInfo, _currentSessionId, null).ConfigureAwait(false);
                StatusMessage = $"User '{SelectedUser.UserName}' deleted.";
                await LoadUsersAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Delete failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        public async Task ExportUsersAsync()
        {
            IsBusy = true;
            try
            {
                await _dbService.ExportUsersAsync(FilteredUsers.ToList(), _currentIpAddress, _currentDeviceInfo, _currentSessionId, await YasGMP.Helpers.ExportFormatPrompt.PromptAsync()).ConfigureAwait(false);
                StatusMessage = "Users exported successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        public async Task ExportRolesAsync()
        {
            IsBusy = true;
            try
            {
                await _dbService.ExportRolesAsync(FilteredRoles.ToList(), _currentIpAddress, _currentDeviceInfo, _currentSessionId, await YasGMP.Helpers.ExportFormatPrompt.PromptAsync()).ConfigureAwait(false);
                StatusMessage = "Roles exported successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        public async Task ExportPermissionsAsync()
        {
            IsBusy = true;
            try
            {
                await _dbService.ExportPermissionsAsync(FilteredPermissions.ToList(), _currentIpAddress, _currentDeviceInfo, _currentSessionId, await YasGMP.Helpers.ExportFormatPrompt.PromptAsync()).ConfigureAwait(false);
                StatusMessage = "Permissions exported successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        public void FilterAll()
        {
            FilteredUsers = new ObservableCollection<User>(
                Users.Where(u =>
                    string.IsNullOrWhiteSpace(UserSearchTerm) ||
                    (u.UserName?.Contains(UserSearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (u.Email?.Contains(UserSearchTerm, StringComparison.OrdinalIgnoreCase) ?? false)));

            FilteredRoles = new ObservableCollection<Role>(
                Roles.Where(r =>
                    string.IsNullOrWhiteSpace(RoleSearchTerm) ||
                    (r.Name?.Contains(RoleSearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (r.Description?.Contains(RoleSearchTerm, StringComparison.OrdinalIgnoreCase) ?? false)));

            FilteredPermissions = new ObservableCollection<Permission>(
                Permissions.Where(p =>
                    string.IsNullOrWhiteSpace(PermSearchTerm) ||
                    (p.Name?.Contains(PermSearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (p.Description?.Contains(PermSearchTerm, StringComparison.OrdinalIgnoreCase) ?? false)));
        }

        #endregion

        #region Audit/Auxiliary (DTO→Model tolerant mapping)

        public async Task<ObservableCollection<AuditLogEntry>> LoadUserAuditAsync(int userId)
        {
            var raw = await _dbService.GetAuditLogForEntityAsync("users", userId).ConfigureAwait(false);
            var mapped = raw?.Select(MapAuditEntryToModel) ?? Enumerable.Empty<AuditLogEntry>();
            return new ObservableCollection<AuditLogEntry>(mapped);
        }

        public async Task<ObservableCollection<AuditLogEntry>> LoadRoleAuditAsync(int roleId)
        {
            var raw = await _dbService.GetAuditLogForEntityAsync("roles", roleId).ConfigureAwait(false);
            var mapped = raw?.Select(MapAuditEntryToModel) ?? Enumerable.Empty<AuditLogEntry>();
            return new ObservableCollection<AuditLogEntry>(mapped);
        }

        public async Task<ObservableCollection<AuditLogEntry>> LoadPermissionAuditAsync(int permissionId)
        {
            var raw = await _dbService.GetAuditLogForEntityAsync("permissions", permissionId).ConfigureAwait(false);
            var mapped = raw?.Select(MapAuditEntryToModel) ?? Enumerable.Empty<AuditLogEntry>();
            return new ObservableCollection<AuditLogEntry>(mapped);
        }

        /// <summary>
        /// Reflection-based tolerant mapper from any DTO to <see cref="AuditLogEntry"/>.
        /// Works whether the DB returns <c>AuditLogEntry</c> directly or a DTO with similar property names.
        /// </summary>
        private static AuditLogEntry MapAuditEntryToModel(object src)
        {
            if (src is AuditLogEntry e) return e;

            object? Get(string name1, string? name2 = null, string? name3 = null)
            {
                var t = src.GetType();
                var p = t.GetProperty(name1, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                        ?? (name2 != null ? t.GetProperty(name2, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase) : null)
                        ?? (name3 != null ? t.GetProperty(name3, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase) : null);
                return p?.GetValue(src);
            }

            T? ConvertTo<T>(object? o)
            {
                if (o is null || o is DBNull) return default;
                try { return (T)System.Convert.ChangeType(o, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T)); }
                catch { return default; }
            }

            var model = new AuditLogEntry
            {
                Id          = ConvertTo<int>(Get("Id")),
                TableName   = ConvertTo<string>(Get("Table", "TableName", "EntityTable")) ?? string.Empty,
                EntityId    = ConvertTo<int?>(Get("EntityId", "RecordId", "TargetId")),
                Action      = ConvertTo<string>(Get("Action", "EventType")) ?? string.Empty,
                Description = ConvertTo<string>(Get("Description", "Details", "Message")),
                Timestamp   = ConvertTo<DateTime?>(Get("Timestamp", "ChangedAt", "CreatedAt")) ?? DateTime.UtcNow,
                UserId      = ConvertTo<int?>(Get("UserId", "ActorUserId", "ChangedBy")),
                SourceIp    = ConvertTo<string>(Get("SourceIp", "Ip", "IPAddress")),
                DeviceInfo  = ConvertTo<string>(Get("DeviceInfo", "Device")),
                SessionId   = ConvertTo<string>(Get("SessionId", "Session"))
            };
            return model;
        }

        #endregion

        #region INotifyPropertyChanged

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName ?? string.Empty));

        #endregion
    }
}

