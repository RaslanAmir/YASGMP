using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using YasGMP.Models;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// View model backing the MAUI user editor dialog. Surfaces the editable <see cref="User"/>
    /// fields through a <see cref="UserEditor"/> adapter, tracks multi-role assignments, and
    /// captures impersonation requests that administrators may trigger while reviewing a user.
    /// </summary>
    public sealed class UserEditDialogViewModel : INotifyPropertyChanged
    {
        private bool _isImpersonating;
        private UserSummary? _selectedImpersonationTarget;
        private string _impersonationReason = string.Empty;
        private string _impersonationNotes = string.Empty;

        /// <summary>Initializes a new instance of the <see cref="UserEditDialogViewModel"/> class.</summary>
        public UserEditDialogViewModel()
        {
            Editor = new UserEditor();
            RoleOptions = new ObservableCollection<RoleOption>();
            ImpersonationTargets = new ObservableCollection<UserSummary>();

            SaveCommand = new Command(OnSave, CanSave);
            CancelCommand = new Command(OnCancel);
            ImpersonateCommand = new Command(OnImpersonate, CanImpersonate);
        }

        /// <summary>Editor surface that exposes user fields for binding.</summary>
        public UserEditor Editor { get; }

        /// <summary>Available role options rendered as checkboxes.</summary>
        public ObservableCollection<RoleOption> RoleOptions { get; }

        /// <summary>Users that can be impersonated from the dialog.</summary>
        public ObservableCollection<UserSummary> ImpersonationTargets { get; }

        /// <summary>Raised when the dialog should close. Payload describes the outcome.</summary>
        public event EventHandler<UserEditDialogResult>? DialogCompleted;

        /// <summary>Command invoked when the operator confirms/save.</summary>
        public ICommand SaveCommand { get; }

        /// <summary>Command invoked when the operator cancels the dialog.</summary>
        public ICommand CancelCommand { get; }

        /// <summary>Command invoked when the operator requests impersonation.</summary>
        public ICommand ImpersonateCommand { get; }

        /// <summary>Whether impersonation workflow is enabled.</summary>
        public bool IsImpersonating
        {
            get => _isImpersonating;
            set
            {
                if (SetField(ref _isImpersonating, value))
                {
                    ((Command)ImpersonateCommand).ChangeCanExecute();
                }
            }
        }

        /// <summary>Selected impersonation target.</summary>
        public UserSummary? SelectedImpersonationTarget
        {
            get => _selectedImpersonationTarget;
            set
            {
                if (SetField(ref _selectedImpersonationTarget, value))
                {
                    ((Command)ImpersonateCommand).ChangeCanExecute();
                }
            }
        }

        /// <summary>Short reason required by GMP/CSV justification.</summary>
        public string ImpersonationReason
        {
            get => _impersonationReason;
            set => SetField(ref _impersonationReason, value ?? string.Empty);
        }

        /// <summary>Optional auditor notes captured alongside impersonation events.</summary>
        public string ImpersonationNotes
        {
            get => _impersonationNotes;
            set => SetField(ref _impersonationNotes, value ?? string.Empty);
        }

        /// <summary>
        /// Hydrates the editor with an existing user and populates lookup collections.
        /// </summary>
        public void Initialize(User? user, IEnumerable<Role>? roles, IEnumerable<User>? impersonationCandidates)
        {
            Editor.LoadFromUser(user);

            foreach (var option in RoleOptions)
            {
                option.PropertyChanged -= OnRoleOptionChanged;
            }

            RoleOptions.Clear();
            var assignedRoles = new HashSet<int>(Editor.RoleIds ?? Array.Empty<int>());
            if (roles is not null)
            {
                foreach (var role in roles)
                {
                    var option = new RoleOption(role.Id, role.Name ?? string.Empty, role.Description);
                    option.IsSelected = assignedRoles.Contains(role.Id);
                    option.PropertyChanged += OnRoleOptionChanged;
                    RoleOptions.Add(option);
                }
            }

            ImpersonationTargets.Clear();
            if (impersonationCandidates is not null)
            {
                foreach (var candidate in impersonationCandidates)
                {
                    if (candidate.Id == 0)
                    {
                        continue;
                    }

                    var summary = UserSummary.FromUser(candidate);
                    ImpersonationTargets.Add(summary);
                }
            }

            ((Command)SaveCommand).ChangeCanExecute();
            ((Command)ImpersonateCommand).ChangeCanExecute();
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        private bool CanSave()
            => !string.IsNullOrWhiteSpace(Editor.Username) && !string.IsNullOrWhiteSpace(Editor.FullName);

        private bool CanImpersonate()
            => IsImpersonating && SelectedImpersonationTarget is not null;

        private void OnSave()
        {
            SynchronizeRoleAssignments();
            var clone = Editor.Clone();
            var result = new UserEditDialogResult(
                true,
                false,
                clone,
                SelectedImpersonationTarget?.Id,
                FormatImpersonationReason(),
                NormalizeNotes());
            DialogCompleted?.Invoke(this, result);
        }

        private void OnCancel()
        {
            var clone = Editor.Clone();
            var result = new UserEditDialogResult(
                false,
                false,
                clone,
                null,
                null,
                null);
            DialogCompleted?.Invoke(this, result);
        }

        private void OnImpersonate()
        {
            if (!CanImpersonate())
            {
                return;
            }

            SynchronizeRoleAssignments();
            var clone = Editor.Clone();
            var result = new UserEditDialogResult(
                false,
                true,
                clone,
                SelectedImpersonationTarget?.Id,
                FormatImpersonationReason(),
                NormalizeNotes());
            DialogCompleted?.Invoke(this, result);
        }

        private void OnRoleOptionChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(RoleOption.IsSelected))
            {
                SynchronizeRoleAssignments();
            }
        }

        private void SynchronizeRoleAssignments()
        {
            Editor.RoleIds = RoleOptions
                .Where(option => option.IsSelected)
                .Select(option => option.RoleId)
                .ToArray();
        }

        private string? FormatImpersonationReason()
        {
            var reason = (ImpersonationReason ?? string.Empty).Trim();
            return string.IsNullOrEmpty(reason) ? null : reason;
        }

        private string? NormalizeNotes()
        {
            var notes = (ImpersonationNotes ?? string.Empty).Trim();
            return string.IsNullOrEmpty(notes) ? null : notes;
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            ((Command)SaveCommand).ChangeCanExecute();
            return true;
        }

        private void OnPropertyChanged(string? propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>Lightweight adapter for editing <see cref="User"/> entities.</summary>
    public sealed class UserEditor : INotifyPropertyChanged
    {
        private int _id;
        private string _username = string.Empty;
        private string _fullName = string.Empty;
        private string _email = string.Empty;
        private string _phone = string.Empty;
        private string _role = string.Empty;
        private string _departmentName = string.Empty;
        private bool _active = true;
        private bool _isLocked;
        private bool _isTwoFactorEnabled;
        private string _newPassword = string.Empty;
        private string _confirmPassword = string.Empty;
        private int[] _roleIds = Array.Empty<int>();

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>User identifier.</summary>
        public int Id
        {
            get => _id;
            set => SetField(ref _id, value);
        }

        /// <summary>Login username.</summary>
        public string Username
        {
            get => _username;
            set => SetField(ref _username, value ?? string.Empty);
        }

        /// <summary>Full name.</summary>
        public string FullName
        {
            get => _fullName;
            set => SetField(ref _fullName, value ?? string.Empty);
        }

        /// <summary>Email address.</summary>
        public string Email
        {
            get => _email;
            set => SetField(ref _email, value ?? string.Empty);
        }

        /// <summary>Phone number.</summary>
        public string Phone
        {
            get => _phone;
            set => SetField(ref _phone, value ?? string.Empty);
        }

        /// <summary>Primary role label.</summary>
        public string Role
        {
            get => _role;
            set => SetField(ref _role, value ?? string.Empty);
        }

        /// <summary>Department description.</summary>
        public string DepartmentName
        {
            get => _departmentName;
            set => SetField(ref _departmentName, value ?? string.Empty);
        }

        /// <summary>Whether the account is active.</summary>
        public bool Active
        {
            get => _active;
            set => SetField(ref _active, value);
        }

        /// <summary>Lockout flag.</summary>
        public bool IsLocked
        {
            get => _isLocked;
            set => SetField(ref _isLocked, value);
        }

        /// <summary>Two-factor authentication flag.</summary>
        public bool IsTwoFactorEnabled
        {
            get => _isTwoFactorEnabled;
            set => SetField(ref _isTwoFactorEnabled, value);
        }

        /// <summary>New password captured during reset.</summary>
        public string NewPassword
        {
            get => _newPassword;
            set => SetField(ref _newPassword, value ?? string.Empty);
        }

        /// <summary>Confirmation of the new password.</summary>
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => SetField(ref _confirmPassword, value ?? string.Empty);
        }

        /// <summary>Assigned role identifiers.</summary>
        public int[] RoleIds
        {
            get => _roleIds;
            set => SetField(ref _roleIds, value ?? Array.Empty<int>());
        }

        /// <summary>Populates fields from an existing user.</summary>
        public void LoadFromUser(User? user)
        {
            if (user is null)
            {
                Id = 0;
                Username = string.Empty;
                FullName = string.Empty;
                Email = string.Empty;
                Phone = string.Empty;
                Role = string.Empty;
                DepartmentName = string.Empty;
                Active = true;
                IsLocked = false;
                IsTwoFactorEnabled = false;
                RoleIds = Array.Empty<int>();
                return;
            }

            Id = user.Id;
            Username = user.Username;
            FullName = user.FullName;
            Email = user.Email;
            Phone = user.Phone;
            Role = user.Role;
            DepartmentName = user.DepartmentName;
            Active = user.Active;
            IsLocked = user.IsLocked;
            IsTwoFactorEnabled = user.IsTwoFactorEnabled;
            RoleIds = user.RoleIds?.ToArray() ?? Array.Empty<int>();
        }

        /// <summary>Creates a deep clone of the editor state.</summary>
        public UserEditor Clone()
        {
            return new UserEditor
            {
                Id = Id,
                Username = Username,
                FullName = FullName,
                Email = Email,
                Phone = Phone,
                Role = Role,
                DepartmentName = DepartmentName,
                Active = Active,
                IsLocked = IsLocked,
                IsTwoFactorEnabled = IsTwoFactorEnabled,
                NewPassword = NewPassword,
                ConfirmPassword = ConfirmPassword,
                RoleIds = RoleIds?.ToArray() ?? Array.Empty<int>()
            };
        }

        /// <summary>Applies the editor state to the provided <see cref="User"/> instance.</summary>
        public void ApplyTo(User target)
        {
            target.Id = Id;
            target.Username = Username?.Trim() ?? string.Empty;
            target.FullName = FullName?.Trim() ?? string.Empty;
            target.Email = Email?.Trim() ?? string.Empty;
            target.Phone = Phone?.Trim() ?? string.Empty;
            target.Role = string.IsNullOrWhiteSpace(Role) ? "User" : Role.Trim();
            target.DepartmentName = DepartmentName?.Trim() ?? string.Empty;
            target.Active = Active;
            target.IsLocked = IsLocked;
            target.IsTwoFactorEnabled = IsTwoFactorEnabled;
            target.RoleIds = RoleIds?.ToArray() ?? Array.Empty<int>();
        }

        private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>Role option wrapper used for checkbox selection.</summary>
    public sealed class RoleOption : INotifyPropertyChanged
    {
        private bool _isSelected;

        public RoleOption(int roleId, string name, string? description)
        {
            RoleId = roleId;
            Name = name;
            Description = description ?? string.Empty;
        }

        public int RoleId { get; }
        public string Name { get; }
        public string Description { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                {
                    return;
                }

                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    /// <summary>Compact representation of a user used by the impersonation picker.</summary>
    public sealed class UserSummary
    {
        private UserSummary(int id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }

        public int Id { get; }

        public string DisplayName { get; }

        public static UserSummary FromUser(User user)
        {
            var display = string.IsNullOrWhiteSpace(user.FullName)
                ? user.Username
                : user.FullName;
            return new UserSummary(user.Id, display ?? string.Empty);
        }
    }

    /// <summary>Result payload raised when the dialog completes.</summary>
    /// <param name="Saved">True when the user confirmed the save action.</param>
    /// <param name="ImpersonationRequested">True when an impersonation action should be initiated.</param>
    /// <param name="EditorState">Snapshot of the editor state at close.</param>
    /// <param name="ImpersonationTargetId">Selected impersonation target identifier.</param>
    /// <param name="ImpersonationReason">Operator supplied reason code.</param>
    /// <param name="ImpersonationNotes">Additional free-form notes.</param>
    public sealed record UserEditDialogResult(
        bool Saved,
        bool ImpersonationRequested,
        UserEditor EditorState,
        int? ImpersonationTargetId,
        string? ImpersonationReason,
        string? ImpersonationNotes);
}
