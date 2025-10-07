using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// <b>AdminViewModel</b> — ViewModel for the Admin console (Users, full RBAC editor,
    /// system diagnostics, and quick tools). Uses CommunityToolkit.MVVM for relay commands.
    /// Designed to be resilient, AOT/WinRT-safe, and auditable.
    /// </summary>
    public partial class AdminViewModel : ObservableObject
    {
        private readonly UserService _users;
        private readonly AuditService _audit;
        private readonly IRBACService _rbac;

        /// <summary>
        /// Initializes the AdminViewModel with required services.
        /// </summary>
        public AdminViewModel(UserService users, AuditService audit, IRBACService rbac)
        {
            _users = users ?? throw new ArgumentNullException(nameof(users));
            _audit = audit ?? throw new ArgumentNullException(nameof(audit));
            _rbac  = rbac  ?? throw new ArgumentNullException(nameof(rbac));

            HeaderSubtitle = $"Prijavljen: {(Application.Current as App)?.LoggedUser?.Username ?? "n/a"}";
            AppVersion     = $"{Assembly.GetExecutingAssembly().GetName().Name} {Assembly.GetExecutingAssembly().GetName().Version}";
            RefreshSystemInfo();
        }

        #region === Header / System info ===

        private string _headerSubtitle = string.Empty;
        /// <summary>
        /// Represents the header subtitle value.
        /// </summary>
        public string HeaderSubtitle
        {
            get => _headerSubtitle;
            set => SetProperty(ref _headerSubtitle, value);
        }

        private string _appVersion = string.Empty;
        /// <summary>
        /// Represents the app version value.
        /// </summary>
        public string AppVersion
        {
            get => _appVersion;
            set => SetProperty(ref _appVersion, value);
        }

        private string _osVersion = Environment.OSVersion.ToString();
        /// <summary>
        /// Represents the os version value.
        /// </summary>
        public string OsVersion
        {
            get => _osVersion;
            set => SetProperty(ref _osVersion, value);
        }

        private string _hostName = Environment.MachineName;
        /// <summary>
        /// Represents the host name value.
        /// </summary>
        public string HostName
        {
            get => _hostName;
            set => SetProperty(ref _hostName, value);
        }

        private string _userName = Environment.UserName;
        /// <summary>
        /// Represents the user name value.
        /// </summary>
        public string UserName
        {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

        private string _localIp = "127.0.0.1";
        /// <summary>
        /// Represents the local ip value.
        /// </summary>
        public string LocalIp
        {
            get => _localIp;
            set => SetProperty(ref _localIp, value);
        }

        private void RefreshSystemInfo()
        {
            LocalIp   = ResolveBestIpAddress();
            OsVersion = Environment.OSVersion.ToString();
            HostName  = Environment.MachineName;
            UserName  = Environment.UserName;
        }

        private static string ResolveBestIpAddress()
        {
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel) continue;

                    foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                        if (ua.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ua.Address))
                            return ua.Address.ToString();
                }

                var entry = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var addr in entry.AddressList)
                    if (addr.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(addr))
                        return addr.ToString();

                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
                socket.Connect("8.8.8.8", 65530);
                if (socket.LocalEndPoint is IPEndPoint ep)
                    return ep.Address.ToString();
            }
            catch { }
            return "127.0.0.1";
        }

        #endregion

        #region === Users ===
        /// <summary>
        /// Gets or sets the users.
        /// </summary>

        public ObservableCollection<User> Users { get; } = new();

        private string _userFilter = string.Empty;
        /// <summary>
        /// Represents the user filter value.
        /// </summary>
        public string UserFilter
        {
            get => _userFilter;
            set => SetProperty(ref _userFilter, value);
        }

        private User? _selectedUser;
        /// <summary>
        /// Represents the selected user value.
        /// </summary>
        public User? SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (SetProperty(ref _selectedUser, value))
                {
                    OnPropertyChanged(nameof(HasSelectedUser));

                    (LockSelectedCommand as IRelayCommand)?.NotifyCanExecuteChanged();
                    (UnlockSelectedCommand as IRelayCommand)?.NotifyCanExecuteChanged();
                    (ResetPasswordCommand as IRelayCommand)?.NotifyCanExecuteChanged();
                    (DeactivateUserCommand as IRelayCommand)?.NotifyCanExecuteChanged();
                    (DeleteUserCommand as IRelayCommand)?.NotifyCanExecuteChanged();
                    (LoadSelectedUserPermissionsCommand as IRelayCommand)?.NotifyCanExecuteChanged();

                    OnSelectedUserChanged(value);
                }
            }
        }
        /// <summary>
        /// Gets or sets the has selected user.
        /// </summary>

        public bool HasSelectedUser => SelectedUser != null;

        private string _editUsername = string.Empty;
        /// <summary>
        /// Represents the edit username value.
        /// </summary>
        public string EditUsername
        {
            get => _editUsername;
            set => SetProperty(ref _editUsername, value);
        }

        private string _editRole = string.Empty;
        /// <summary>
        /// Represents the edit role value.
        /// </summary>
        public string EditRole
        {
            get => _editRole;
            set => SetProperty(ref _editRole, value);
        }

        private string _editPassword = string.Empty;
        /// <summary>
        /// Represents the edit password value.
        /// </summary>
        public string EditPassword
        {
            get => _editPassword;
            set => SetProperty(ref _editPassword, value);
        }

        /// <summary>
        /// Executes the load users async operation.
        /// </summary>
        [RelayCommand]
        public async Task LoadUsersAsync()
        {
            try
            {
                var list = await _users.GetAllUsersAsync().ConfigureAwait(false);
                var filtered = string.IsNullOrWhiteSpace(UserFilter)
                    ? list
                    : list.Where(u =>
                        (!string.IsNullOrEmpty(u.Username) && u.Username.Contains(UserFilter, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(u.Role)     && u.Role.Contains(UserFilter,     StringComparison.OrdinalIgnoreCase)))
                      .ToList();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Users.Clear();
                    foreach (var u in filtered) Users.Add(u);
                });

                if (SelectedUser != null)
                    await RefreshUserRoleListsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Services.SafeNavigator.ShowAlertAsync("Load users failed", ex.Message, "OK").ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Executes the save user async operation.
        /// </summary>
        [RelayCommand]
        public async Task SaveUserAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(EditUsername))
                {
                    await Services.SafeNavigator.ShowAlertAsync("Validation", "Username is required.", "OK").ConfigureAwait(false);
                    return;
                }

                var existing = Users.FirstOrDefault(u => u.Username?.Equals(EditUsername, StringComparison.OrdinalIgnoreCase) == true);
                var adminId = (Application.Current as App)?.LoggedUser?.Id ?? 0;

                if (existing is null)
                {
                    var newUser = new User
                    {
                        Username     = EditUsername.Trim(),
                        Role         = string.IsNullOrWhiteSpace(EditRole) ? "User" : EditRole.Trim(),
                        Active       = true,
                        IsLocked     = false,
                        PasswordHash = EditPassword ?? string.Empty
                    };

                    await _users.CreateUserAsync(newUser, adminId).ConfigureAwait(false);
                    await _audit.LogEntityAuditAsync("users", newUser.Id, "CREATE_UI", $"Kreiran kroz Admin UI: {newUser.Username}").ConfigureAwait(false);
                }
                else
                {
                    existing.Role = string.IsNullOrWhiteSpace(EditRole) ? existing.Role : EditRole.Trim();
                    if (!string.IsNullOrWhiteSpace(EditPassword))
                        await _users.ChangePasswordAsync(existing.Id, EditPassword, adminId).ConfigureAwait(false);

                    await _users.UpdateUserAsync(existing, adminId).ConfigureAwait(false);
                    await _audit.LogEntityAuditAsync("users", existing.Id, "UPDATE_UI", $"Ažuriran kroz Admin UI: {existing.Username}").ConfigureAwait(false);
                }

                EditUsername = EditRole = EditPassword = string.Empty;
                await LoadUsersAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Services.SafeNavigator.ShowAlertAsync("Save failed", ex.Message, "OK").ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Executes the lock selected async operation.
        /// </summary>
        [RelayCommand(CanExecute = nameof(HasSelectedUser))]
        public async Task LockSelectedAsync()
        {
            if (SelectedUser is null) return;
            await _users.LockUserAsync(SelectedUser.Id).ConfigureAwait(false);
            await _audit.LogEntityAuditAsync("users", SelectedUser.Id, "LOCK_UI", "Zaključan kroz Admin UI").ConfigureAwait(false);
            await LoadUsersAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the unlock selected async operation.
        /// </summary>
        [RelayCommand(CanExecute = nameof(HasSelectedUser))]
        public async Task UnlockSelectedAsync()
        {
            if (SelectedUser is null) return;
            var adminId = (Application.Current as App)?.LoggedUser?.Id ?? 0;
            await _users.UnlockUserAsync(SelectedUser.Id, adminId).ConfigureAwait(false);
            await _audit.LogEntityAuditAsync("users", SelectedUser.Id, "UNLOCK_UI", "Otključan kroz Admin UI").ConfigureAwait(false);
            await LoadUsersAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the reset password async operation.
        /// </summary>
        [RelayCommand(CanExecute = nameof(HasSelectedUser))]
        public async Task ResetPasswordAsync()
        {
            if (SelectedUser is null) return;

            string? newPass = null;
            try
            {
                newPass = await MainThread.InvokeOnMainThreadAsync(async () =>
                    await (Application.Current?.MainPage)?.DisplayPromptAsync(
                        "Reset password",
                        $"New password for {SelectedUser.Username}:",
                        "Save", "Cancel", "********", -1, Keyboard.Default)!)
                    .ConfigureAwait(false);
            }
            catch { }

            if (string.IsNullOrWhiteSpace(newPass)) return;

            var adminId = (Application.Current as App)?.LoggedUser?.Id ?? 0;
            await _users.ChangePasswordAsync(SelectedUser.Id, newPass, adminId).ConfigureAwait(false);
            await _audit.LogEntityAuditAsync("users", SelectedUser.Id, "RESET_PASSWORD_UI", "Resetirana lozinka kroz Admin UI").ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the deactivate user async operation.
        /// </summary>
        [RelayCommand(CanExecute = nameof(HasSelectedUser))]
        public async Task DeactivateUserAsync(User? user)
        {
            var target = user ?? SelectedUser;
            if (target is null) return;

            await _users.DeactivateUserAsync(target.Id).ConfigureAwait(false);
            await _audit.LogEntityAuditAsync("users", target.Id, "DEACTIVATE_UI", "Deaktiviran kroz Admin UI").ConfigureAwait(false);
            await LoadUsersAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the delete user async operation.
        /// </summary>
        [RelayCommand(CanExecute = nameof(HasSelectedUser))]
        public async Task DeleteUserAsync(User? user)
        {
            var target = user ?? SelectedUser;
            if (target is null) return;

            var ok = await Services.SafeNavigator.ConfirmAsync(
                "Confirm delete", $"Delete user '{target.Username}' (ID={target.Id})?", "Delete", "Cancel").ConfigureAwait(false);
            if (!ok) return;

            var adminId = (Application.Current as App)?.LoggedUser?.Id ?? 0;
            await _users.DeleteUserAsync(target.Id, adminId).ConfigureAwait(false);
            await _audit.LogEntityAuditAsync("users", target.Id, "DELETE_UI", "Obrisan kroz Admin UI").ConfigureAwait(false);
            await LoadUsersAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the enable2 fa async operation.
        /// </summary>
        [RelayCommand(CanExecute = nameof(HasSelectedUser))]
        public async Task Enable2FaAsync()
        {
            if (SelectedUser is null) return;
            await _users.SetTwoFactorEnabledAsync(SelectedUser.Id, true).ConfigureAwait(false);
            await _audit.LogEntityAuditAsync("users", SelectedUser.Id, "ENABLE_2FA_UI", "Omogućen 2FA kroz Admin UI").ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the disable2 fa async operation.
        /// </summary>
        [RelayCommand(CanExecute = nameof(HasSelectedUser))]
        public async Task Disable2FaAsync()
        {
            if (SelectedUser is null) return;
            await _users.SetTwoFactorEnabledAsync(SelectedUser.Id, false).ConfigureAwait(false);
            await _audit.LogEntityAuditAsync("users", SelectedUser.Id, "DISABLE_2FA_UI", "Onemogućen 2FA kroz Admin UI").ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the edit user async operation.
        /// </summary>
        [RelayCommand]
        public async Task EditUserAsync(User? user)
        {
            var u = user ?? SelectedUser;
            if (u is null) return;

            EditUsername = u.Username ?? string.Empty;
            EditRole     = u.Role     ?? string.Empty;
            EditPassword = string.Empty;

            await Task.CompletedTask;
        }

        /// <summary>
        /// Executes the export users async operation.
        /// </summary>
        [RelayCommand]
        public async Task ExportUsersAsync()
        {
            await _audit.LogSystemEventAsync("ADMIN_EXPORT", "Export korisnika pokrenut iz Admin UI").ConfigureAwait(false);
            await Services.SafeNavigator.ShowAlertAsync("Export", "Export pokrenut (implementirajte u ExportService prema potrebi).", "OK")
                .ConfigureAwait(false);
        }

        #endregion

        #region === RBAC · Roles / Permissions / Assignments ===
        /// <summary>
        /// Gets or sets the roles.
        /// </summary>

        public ObservableCollection<Role> Roles { get; } = new();

        private string _roleFilter = string.Empty;
        /// <summary>
        /// Represents the role filter value.
        /// </summary>
        public string RoleFilter
        {
            get => _roleFilter;
            set => SetProperty(ref _roleFilter, value);
        }

        private Role? _selectedRole;
        /// <summary>
        /// Represents the selected role value.
        /// </summary>
        public Role? SelectedRole
        {
            get => _selectedRole;
            set
            {
                if (SetProperty(ref _selectedRole, value))
                {
                    OnSelectedRoleChanged(value);
                }
            }
        }

        private string _editRoleCode = string.Empty;
        /// <summary>Role editor: machine code (mapped to <see cref="Role.Name"/>).</summary>
        public string EditRoleCode
        {
            get => _editRoleCode;
            set => SetProperty(ref _editRoleCode, value);
        }

        private string _editRoleName = string.Empty;
        /// <summary>Role editor: display name (mapped to <see cref="Role.Description"/>).</summary>
        public string EditRoleName
        {
            get => _editRoleName;
            set => SetProperty(ref _editRoleName, value);
        }

        private string _editRoleDescription = string.Empty;
        /// <summary>
        /// Role editor: descriptive notes (mapped to <see cref="Role.Notes"/>).
        /// </summary>
        public string EditRoleDescription
        {
            get => _editRoleDescription;
            set => SetProperty(ref _editRoleDescription, value);
        }

        private List<Permission> _allPermissions = new();
        /// <summary>
        /// Gets or sets the available permissions.
        /// </summary>

        public ObservableCollection<Permission> AvailablePermissions { get; } = new();
        /// <summary>
        /// Gets or sets the role permissions.
        /// </summary>
        public ObservableCollection<Permission> RolePermissions { get; } = new();

        private string _permissionFilter = string.Empty;
        /// <summary>
        /// Represents the permission filter value.
        /// </summary>
        public string PermissionFilter
        {
            get => _permissionFilter;
            set
            {
                if (SetProperty(ref _permissionFilter, value))
                {
                    _ = RefreshRolePermissionListsAsync();
                }
            }
        }

        private Permission? _selectedAvailablePermission;
        /// <summary>
        /// Represents the selected available permission value.
        /// </summary>
        public Permission? SelectedAvailablePermission
        {
            get => _selectedAvailablePermission;
            set => SetProperty(ref _selectedAvailablePermission, value);
        }

        private Permission? _selectedRolePermission;
        /// <summary>
        /// Represents the selected role permission value.
        /// </summary>
        public Permission? SelectedRolePermission
        {
            get => _selectedRolePermission;
            set => SetProperty(ref _selectedRolePermission, value);
        }
        /// <summary>
        /// Gets or sets the assigned roles for user.
        /// </summary>

        public ObservableCollection<Role> AssignedRolesForUser { get; } = new();
        /// <summary>
        /// Gets or sets the available roles for user.
        /// </summary>
        public ObservableCollection<Role> AvailableRolesForUser { get; } = new();

        private Role? _selectedAvailableRoleForUser;
        /// <summary>
        /// Represents the selected available role for user value.
        /// </summary>
        public Role? SelectedAvailableRoleForUser
        {
            get => _selectedAvailableRoleForUser;
            set => SetProperty(ref _selectedAvailableRoleForUser, value);
        }

        private Role? _selectedAssignedRoleForUser;
        /// <summary>
        /// Represents the selected assigned role for user value.
        /// </summary>
        public Role? SelectedAssignedRoleForUser
        {
            get => _selectedAssignedRoleForUser;
            set => SetProperty(ref _selectedAssignedRoleForUser, value);
        }
        /// <summary>
        /// Gets or sets the selected user permissions.
        /// </summary>

        public ObservableCollection<string> SelectedUserPermissions { get; } = new();

        private void OnSelectedRoleChanged(Role? value) => _ = RefreshRolePermissionListsAsync();
        private void OnSelectedUserChanged(User? value)
        {
            _ = RefreshUserRoleListsAsync();
            _ = LoadSelectedUserPermissionsAsync();
        }

        /// <summary>
        /// Executes the load roles async operation.
        /// </summary>
        [RelayCommand]
        public async Task LoadRolesAsync()
        {
            try
            {
                var roles = await _rbac.GetAllRolesAsync().ConfigureAwait(false);
                _allPermissions = await _rbac.GetAllPermissionsAsync().ConfigureAwait(false);

                var filtered = string.IsNullOrWhiteSpace(RoleFilter)
                    ? roles.Where(r => !r.IsDeleted).ToList()
                    : roles.Where(r => !r.IsDeleted &&
                        ((r.Name?.Contains(RoleFilter, StringComparison.OrdinalIgnoreCase) ?? false) ||
                         (r.Description?.Contains(RoleFilter, StringComparison.OrdinalIgnoreCase) ?? false)))
                      .ToList();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Roles.Clear();
                    foreach (var r in filtered) Roles.Add(r);
                });

                await RefreshRolePermissionListsAsync().ConfigureAwait(false);
                await RefreshUserRoleListsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Services.SafeNavigator.ShowAlertAsync("RBAC", "Load roles failed: " + ex.Message, "OK").ConfigureAwait(false);
            }
        }

        private async Task RefreshRolePermissionListsAsync()
        {
            if (SelectedRole is null)
            {
                MainThread.BeginInvokeOnMainThread(() => { RolePermissions.Clear(); AvailablePermissions.Clear(); });
                return;
            }

            var assigned  = await _rbac.GetPermissionsForRoleAsync(SelectedRole.Id).ConfigureAwait(false);
            var available = await _rbac.GetPermissionsNotInRoleAsync(SelectedRole.Id).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(PermissionFilter))
            {
                available = available.Where(p =>
                    (p.Code?.Contains(PermissionFilter, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (p.Name?.Contains(PermissionFilter, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                RolePermissions.Clear();
                foreach (var p in assigned.OrderBy(p => p.Code, StringComparer.OrdinalIgnoreCase)) RolePermissions.Add(p);
                AvailablePermissions.Clear();
                foreach (var p in available.OrderBy(p => p.Code, StringComparer.OrdinalIgnoreCase)) AvailablePermissions.Add(p);
            });
        }

        private async Task RefreshUserRoleListsAsync()
        {
            if (SelectedUser is null)
            {
                MainThread.BeginInvokeOnMainThread(() => { AssignedRolesForUser.Clear(); AvailableRolesForUser.Clear(); });
                return;
            }

            var assigned  = await _rbac.GetRolesForUserAsync(SelectedUser.Id).ConfigureAwait(false);
            var available = await _rbac.GetAvailableRolesForUserAsync(SelectedUser.Id).ConfigureAwait(false);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                AssignedRolesForUser.Clear();  foreach (var r in assigned.OrderBy(r => r.Name)) AssignedRolesForUser.Add(r);
                AvailableRolesForUser.Clear(); foreach (var r in available.OrderBy(r => r.Name)) AvailableRolesForUser.Add(r);
            });
        }

        /// <summary>
        /// Executes the new role async operation.
        /// </summary>
        [RelayCommand]
        public async Task NewRoleAsync()
        {
            EditRoleCode = string.Empty;
            EditRoleName = string.Empty;
            EditRoleDescription = string.Empty;
            SelectedRole = null;
            await Task.CompletedTask;
        }

        /// <summary>
        /// Executes the save role async operation.
        /// </summary>
        [RelayCommand]
        public async Task SaveRoleAsync()
        {
            try
            {
                var adminId = (Application.Current as App)?.LoggedUser?.Id ?? 0;

                if (SelectedRole is null)
                {
                    if (string.IsNullOrWhiteSpace(EditRoleCode))
                    {
                        await Services.SafeNavigator.ShowAlertAsync("Validation", "Code/Name is required.", "OK").ConfigureAwait(false);
                        return;
                    }

                    var role = new Role
                    {
                        Name        = EditRoleCode.Trim(),        // machine code
                        Description = string.IsNullOrWhiteSpace(EditRoleName) ? null : EditRoleName.Trim(),
                        Notes       = string.IsNullOrWhiteSpace(EditRoleDescription) ? null : EditRoleDescription.Trim()
                    };

                    var newId = await _rbac.CreateRoleAsync(role, adminId).ConfigureAwait(false);
                    await _audit.LogSystemEventAsync("ADMIN_ROLE_CREATE_UI", $"Created role {newId} ({role.Name})").ConfigureAwait(false);
                }
                else
                {
                    SelectedRole.Name        = string.IsNullOrWhiteSpace(EditRoleCode)        ? SelectedRole.Name        : EditRoleCode.Trim();
                    SelectedRole.Description = string.IsNullOrWhiteSpace(EditRoleName)        ? SelectedRole.Description : EditRoleName.Trim();
                    SelectedRole.Notes       = string.IsNullOrWhiteSpace(EditRoleDescription) ? SelectedRole.Notes       : EditRoleDescription.Trim();

                    await _rbac.UpdateRoleAsync(SelectedRole, adminId).ConfigureAwait(false);
                    await _audit.LogSystemEventAsync("ADMIN_ROLE_UPDATE_UI", $"Updated role {SelectedRole.Id}").ConfigureAwait(false);
                }

                EditRoleCode = EditRoleName = EditRoleDescription = string.Empty;
                await LoadRolesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Services.SafeNavigator.ShowAlertAsync("Role", "Save failed: " + ex.Message, "OK").ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Executes the delete role async operation.
        /// </summary>
        [RelayCommand]
        public async Task DeleteRoleAsync(Role? role)
        {
            var target = role ?? SelectedRole;
            if (target is null) return;

            var ok = await Services.SafeNavigator.ConfirmAsync(
                "Confirm delete", $"Delete role '{target.Name}' (ID={target.Id})?", "Delete", "Cancel").ConfigureAwait(false);
            if (!ok) return;

            var adminId = (Application.Current as App)?.LoggedUser?.Id ?? 0;
            await _rbac.DeleteRoleAsync(target.Id, adminId, "UI delete").ConfigureAwait(false);
            await _audit.LogSystemEventAsync("ADMIN_ROLE_DELETE_UI", $"Deleted role {target.Id}").ConfigureAwait(false);
            await LoadRolesAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the add permission to role async operation.
        /// </summary>
        [RelayCommand]
        public async Task AddPermissionToRoleAsync(Permission? p)
        {
            var perm = p ?? SelectedAvailablePermission;
            if (SelectedRole is null || perm is null) return;
            var adminId = (Application.Current as App)?.LoggedUser?.Id ?? 0;
            await _rbac.AddPermissionToRoleAsync(SelectedRole.Id, perm.Id, adminId, "UI add").ConfigureAwait(false);
            await RefreshRolePermissionListsAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the remove permission from role async operation.
        /// </summary>
        [RelayCommand]
        public async Task RemovePermissionFromRoleAsync(Permission? p)
        {
            var perm = p ?? SelectedRolePermission;
            if (SelectedRole is null || perm is null) return;
            var adminId = (Application.Current as App)?.LoggedUser?.Id ?? 0;
            await _rbac.RemovePermissionFromRoleAsync(SelectedRole.Id, perm.Id, adminId, "UI remove").ConfigureAwait(false);
            await RefreshRolePermissionListsAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the assign role to user async operation.
        /// </summary>
        [RelayCommand]
        public async Task AssignRoleToUserAsync(Role? r)
        {
            var role = r ?? SelectedAvailableRoleForUser;
            if (SelectedUser is null || role is null) return;
            var adminId = (Application.Current as App)?.LoggedUser?.Id ?? 0;
            await _rbac.GrantRoleAsync(SelectedUser.Id, role.Id, adminId, null, "UI assign").ConfigureAwait(false);
            await RefreshUserRoleListsAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the remove role from user async operation.
        /// </summary>
        [RelayCommand]
        public async Task RemoveRoleFromUserAsync(Role? r)
        {
            var role = r ?? SelectedAssignedRoleForUser;
            if (SelectedUser is null || role is null) return;
            var adminId = (Application.Current as App)?.LoggedUser?.Id ?? 0;
            await _rbac.RevokeRoleAsync(SelectedUser.Id, role.Id, adminId, "UI remove").ConfigureAwait(false);
            await RefreshUserRoleListsAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the load selected user permissions async operation.
        /// </summary>
        [RelayCommand(CanExecute = nameof(HasSelectedUser))]
        public async Task LoadSelectedUserPermissionsAsync()
        {
            if (SelectedUser is null) return;

            try
            {
                var perms = await _rbac.GetAllUserPermissionsAsync(SelectedUser.Id).ConfigureAwait(false);
                var ordered = perms?.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList()
                               ?? new List<string>();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    SelectedUserPermissions.Clear();
                    foreach (var p in ordered) SelectedUserPermissions.Add(p);
                });
            }
            catch (Exception ex)
            {
                await Services.SafeNavigator.ShowAlertAsync("RBAC", "Failed to load permissions: " + ex.Message, "OK").ConfigureAwait(false);
            }
        }

        #endregion

        #region === Diagnostics / Tools ===

        private string _dbTestResult = string.Empty;
        /// <summary>
        /// Represents the db test result value.
        /// </summary>
        public string DbTestResult
        {
            get => _dbTestResult;
            set => SetProperty(ref _dbTestResult, value);
        }

        /// <summary>
        /// Executes the test db async operation.
        /// </summary>
        [RelayCommand]
        public async Task TestDbAsync()
        {
            try
            {
                await _users.GetAllUsersAsync().ConfigureAwait(false);
                DbTestResult = "DB OK (users query succeeded)";
            }
            catch (Exception ex)
            {
                DbTestResult = "DB ERROR: " + ex.Message;
            }
        }

        /// <summary>
        /// Executes the force audit snapshot async operation.
        /// </summary>
        [RelayCommand]
        public async Task ForceAuditSnapshotAsync()
        {
            await _audit.LogSystemEventAsync("ADMIN_TOOL", "Force audit snapshot").ConfigureAwait(false);
            await Services.SafeNavigator.ShowAlertAsync("Tool", "Audit snapshot recorded.", "OK").ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the record system note async operation.
        /// </summary>
        [RelayCommand]
        public async Task RecordSystemNoteAsync()
        {
            await _audit.LogSystemEventAsync("ADMIN_NOTE", "Manual note from Admin panel").ConfigureAwait(false);
            await Services.SafeNavigator.ShowAlertAsync("Note", "System note recorded.", "OK").ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the refresh all async operation.
        /// </summary>
        [RelayCommand]
        public async Task RefreshAllAsync()
        {
            RefreshSystemInfo();
            await LoadUsersAsync().ConfigureAwait(false);
            await LoadRolesAsync().ConfigureAwait(false);
            if (HasSelectedUser) await LoadSelectedUserPermissionsAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the open audit async operation.
        /// </summary>
        [RelayCommand]
        public async Task OpenAuditAsync()
        {
            await Services.SafeNavigator.GoToAsync("//root/quality/auditlog").ConfigureAwait(false);
        }

        #endregion
    }
}
