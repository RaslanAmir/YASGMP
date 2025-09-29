using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.ViewModels.Modules;

public sealed partial class SecurityModuleViewModel : DataDrivenModuleDocumentViewModel
{
    public const string ModuleKey = "Security";

    private readonly IUserCrudService _userService;
    private readonly IAuthContext _authContext;
    private readonly IElectronicSignatureDialogService _signatureDialog;
    private readonly ObservableCollection<RoleOption> _roleOptions = new();
    private readonly List<Role> _availableRoles = new();
    private User? _loadedUser;
    private UserEditor? _snapshot;
    private bool _suppressEditorDirty;
    private bool _suppressRoleDirty;

    public SecurityModuleViewModel(
        DatabaseService databaseService,
        IUserCrudService userService,
        IAuthContext authContext,
        IElectronicSignatureDialogService signatureDialog,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "Security", databaseService, cflDialogService, shellInteraction, navigation)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _signatureDialog = signatureDialog ?? throw new ArgumentNullException(nameof(signatureDialog));
        Editor = UserEditor.CreateEmpty();
        RoleOptions = new ReadOnlyObservableCollection<RoleOption>(_roleOptions);
    }

    [ObservableProperty]
    private UserEditor _editor;

    [ObservableProperty]
    private bool _isEditorEnabled;

    public ReadOnlyObservableCollection<RoleOption> RoleOptions { get; }

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var users = await _userService.GetAllAsync().ConfigureAwait(false);
        return users.Select(ToRecord).ToList();
    }

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
    {
        var sample = new[]
        {
            new User
            {
                Id = 1,
                Username = "darko",
                FullName = "Darko Kovač",
                Email = "darko@example.com",
                Role = "Administrator",
                Active = true,
                DepartmentName = "IT"
            },
            new User
            {
                Id = 2,
                Username = "qa.lead",
                FullName = "QA Lead",
                Email = "qa@example.com",
                Role = "Quality",
                Active = true,
                DepartmentName = "Quality"
            }
        };

        return sample.Select(ToRecord).ToList();
    }

    protected override async Task<CflRequest?> CreateCflRequestAsync()
    {
        var users = await _userService.GetAllAsync().ConfigureAwait(false);
        var items = users
            .Select(user =>
            {
                var key = user.Id.ToString(CultureInfo.InvariantCulture);
                var label = string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName;
                var description = string.IsNullOrWhiteSpace(user.Email)
                    ? user.Username
                    : $"{user.Username} • {user.Email}";
                return new CflItem(key, label, description);
            })
            .ToList();

        return new CflRequest("Select User", items);
    }

    protected override Task OnCflSelectionAsync(CflResult result)
    {
        var match = Records.FirstOrDefault(r => r.Key == result.Selected.Key);
        if (match is not null)
        {
            SelectedRecord = match;
        }

        return Task.CompletedTask;
    }

    protected override async Task OnRecordSelectedAsync(ModuleRecord? record)
    {
        if (record is null)
        {
            _loadedUser = null;
            SetEditor(UserEditor.CreateEmpty());
            if (_roleOptions.Count > 0)
            {
                ApplyRoleSelection(Array.Empty<int>());
            }
            return;
        }

        if (IsInEditMode)
        {
            return;
        }

        if (!int.TryParse(record.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
        {
            StatusMessage = "Invalid user identifier.";
            return;
        }

        var user = await _userService.TryGetByIdAsync(id).ConfigureAwait(false);
        if (user is null)
        {
            StatusMessage = $"Unable to locate user #{id}.";
            return;
        }

        _loadedUser = user;
        SetEditor(UserEditor.FromUser(user));
        await EnsureRolesLoadedAsync().ConfigureAwait(false);
        ApplyRoleSelection(user.RoleIds ?? Array.Empty<int>());
        ResetDirty();
    }

    protected override async Task OnModeChangedAsync(FormMode mode)
    {
        IsEditorEnabled = mode is FormMode.Add or FormMode.Update;

        if (mode is FormMode.Add or FormMode.Update)
        {
            await EnsureRolesLoadedAsync().ConfigureAwait(false);
        }

        switch (mode)
        {
            case FormMode.Add:
                _snapshot = null;
                _loadedUser = null;
                SetEditor(UserEditor.CreateForNew());
                ApplyRoleSelection(Array.Empty<int>());
                break;
            case FormMode.Update:
                _snapshot = Editor.Clone();
                break;
            case FormMode.Find:
                SetEditor(UserEditor.CreateEmpty());
                ApplyRoleSelection(Array.Empty<int>());
                break;
        }
    }

    protected override void OnCancel()
    {
        if (Mode == FormMode.Add)
        {
            SetEditor(UserEditor.CreateEmpty());
            ApplyRoleSelection(Array.Empty<int>());
            return;
        }

        if (Mode == FormMode.Update)
        {
            if (_snapshot is not null)
            {
                SetEditor(_snapshot.Clone());
                ApplyRoleSelection(_snapshot.RoleIds);
            }
            else if (_loadedUser is not null)
            {
                SetEditor(UserEditor.FromUser(_loadedUser));
                ApplyRoleSelection(_loadedUser.RoleIds ?? Array.Empty<int>());
            }
        }
    }

    protected override async Task<IReadOnlyList<string>> ValidateAsync()
    {
        var errors = new List<string>();
        var editor = Editor;

        if (string.IsNullOrWhiteSpace(editor.Username))
        {
            errors.Add("Username is required.");
        }

        if (string.IsNullOrWhiteSpace(editor.FullName))
        {
            errors.Add("Full name is required.");
        }

        if (string.IsNullOrWhiteSpace(editor.Role))
        {
            errors.Add("Primary role description is required.");
        }

        if (!string.IsNullOrWhiteSpace(editor.Email) && !editor.Email.Contains('@', StringComparison.Ordinal))
        {
            errors.Add("Email address appears to be invalid.");
        }

        if (Mode == FormMode.Add && string.IsNullOrWhiteSpace(editor.NewPassword))
        {
            errors.Add("Password is required when creating a new user.");
        }

        if (!string.IsNullOrWhiteSpace(editor.NewPassword) &&
            !string.Equals(editor.NewPassword, editor.ConfirmPassword, StringComparison.Ordinal))
        {
            errors.Add("Password confirmation must match the new password.");
        }

        if (editor.RoleIds is null || editor.RoleIds.Length == 0)
        {
            errors.Add("At least one role must be assigned to the user.");
        }

        try
        {
            var probe = _loadedUser is null ? new User() : CloneUser(_loadedUser);
            editor.ApplyTo(probe);
            probe.RoleIds = editor.RoleIds ?? Array.Empty<int>();
            _userService.Validate(probe);
        }
        catch (Exception ex)
        {
            errors.Add(ex.Message);
        }

        await Task.CompletedTask;
        return errors;
    }

    protected override async Task<bool> OnSaveAsync()
    {
        await EnsureRolesLoadedAsync().ConfigureAwait(false);

        var editor = Editor;
        var user = _loadedUser is null ? new User() : CloneUser(_loadedUser);
        editor.ApplyTo(user);
        user.RoleIds = editor.RoleIds ?? Array.Empty<int>();

        if (user.RoleIds.Length == 0 && _availableRoles.Count > 0)
        {
            var primary = _availableRoles.FirstOrDefault(r =>
                string.Equals(r.Name, user.Role, StringComparison.OrdinalIgnoreCase));
            if (primary is not null)
            {
                user.RoleIds = new[] { primary.Id };
            }
        }

        _userService.Validate(user);

        if (Mode == FormMode.Update && _loadedUser is null)
        {
            StatusMessage = "Select a user before saving.";
            return false;
        }

        var context = CreateCrudContext();
        var password = string.IsNullOrWhiteSpace(editor.NewPassword)
            ? null
            : editor.NewPassword.Trim();

        var recordId = user.Id;
        ElectronicSignatureDialogResult? signatureResult;
        try
        {
            signatureResult = await _signatureDialog
                .CaptureSignatureAsync(new ElectronicSignatureContext("users", recordId))
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Electronic signature failed: {ex.Message}";
            return false;
        }

        if (signatureResult is null)
        {
            StatusMessage = "Electronic signature cancelled. Save aborted.";
            return false;
        }

        if (signatureResult.Signature is null)
        {
            StatusMessage = "Electronic signature was not captured.";
            return false;
        }

        var signatureHash = signatureResult.Signature.SignatureHash ?? string.Empty;
        user.DigitalSignature = signatureHash;
        user.LastChangeSignature = signatureHash;
        user.LastModified = DateTime.UtcNow;
        user.LastModifiedById = _authContext.CurrentUser?.Id;

        try
        {
            if (user.Id == 0)
            {
                if (password is null)
                {
                    throw new InvalidOperationException("Password is required for new users.");
                }

                await _userService.CreateAsync(user, password, context).ConfigureAwait(false);
            }
            else
            {
                if (password is null && _loadedUser is not null)
                {
                    user.PasswordHash = _loadedUser.PasswordHash;
                }

                await _userService.UpdateAsync(user, password, context).ConfigureAwait(false);
            }

            await _userService.UpdateRoleAssignmentsAsync(user.Id, user.RoleIds, context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to persist user: {ex.Message}", ex);
        }

        _loadedUser = user;
        _snapshot = null;

        _suppressEditorDirty = true;
        Editor.NewPassword = string.Empty;
        Editor.ConfirmPassword = string.Empty;
        _suppressEditorDirty = false;

        ApplyRoleSelection(user.RoleIds);
        ResetDirty();

        signatureResult.Signature.RecordId = user.Id;

        try
        {
            await _signatureDialog.PersistSignatureAsync(signatureResult).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to persist electronic signature: {ex.Message}";
            Mode = FormMode.Update;
            return false;
        }

        StatusMessage = $"Electronic signature captured ({signatureResult.ReasonDisplay}).";
        return true;
    }

    protected override bool MatchesSearch(ModuleRecord record, string searchText)
    {
        if (base.MatchesSearch(record, searchText))
        {
            return true;
        }

        return record.InspectorFields.Any(field =>
            field.Value.Contains(searchText, StringComparison.OrdinalIgnoreCase));
    }

    private static ModuleRecord ToRecord(User user)
    {
        var inspector = new List<InspectorField>
        {
            new("Full Name", user.FullName ?? string.Empty),
            new("Email", user.Email ?? string.Empty),
            new("Phone", user.Phone ?? string.Empty),
            new("Department", user.DepartmentName ?? string.Empty),
            new("Locked", user.IsLocked ? "Yes" : "No"),
            new("Two-Factor", user.IsTwoFactorEnabled ? "Enabled" : "Disabled")
        };

        return new ModuleRecord(
            user.Id.ToString(CultureInfo.InvariantCulture),
            user.Username,
            user.Username,
            user.Active ? "Active" : "Inactive",
            user.Role,
            inspector,
            AdminModuleViewModel.ModuleKey,
            user.Id);
    }

    private void SetEditor(UserEditor editor)
    {
        _suppressEditorDirty = true;
        Editor = editor;
        _suppressEditorDirty = false;
        ResetDirty();
    }

    private async Task EnsureRolesLoadedAsync()
    {
        if (_roleOptions.Count > 0)
        {
            return;
        }

        var roles = await _userService.GetAllRolesAsync().ConfigureAwait(false);
        _availableRoles.Clear();

        foreach (var role in roles.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase))
        {
            _availableRoles.Add(role);
        }

        _suppressRoleDirty = true;
        foreach (var option in _roleOptions)
        {
            option.PropertyChanged -= OnRoleOptionPropertyChanged;
        }

        _roleOptions.Clear();
        foreach (var role in _availableRoles)
        {
            var option = new RoleOption(role.Id, role.Name, role.Description);
            option.PropertyChanged += OnRoleOptionPropertyChanged;
            _roleOptions.Add(option);
        }

        _suppressRoleDirty = false;
    }

    private void ApplyRoleSelection(IEnumerable<int> roleIds)
    {
        var set = new HashSet<int>(roleIds ?? Array.Empty<int>());
        _suppressRoleDirty = true;
        foreach (var option in _roleOptions)
        {
            option.IsSelected = set.Contains(option.RoleId);
        }
        _suppressRoleDirty = false;

        UpdateEditorRoleSelectionFromOptions();
    }

    private void UpdateEditorRoleSelectionFromOptions()
    {
        var selected = _roleOptions
            .Where(o => o.IsSelected)
            .Select(o => o.RoleId)
            .ToArray();

        _suppressEditorDirty = true;
        Editor.RoleIds = selected;
        _suppressEditorDirty = false;
    }

    private void OnEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_suppressEditorDirty)
        {
            return;
        }

        if (IsInEditMode)
        {
            MarkDirty();
        }
    }

    private void OnRoleOptionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(RoleOption.IsSelected) || _suppressRoleDirty)
        {
            return;
        }

        UpdateEditorRoleSelectionFromOptions();
        if (IsInEditMode)
        {
            MarkDirty();
        }
    }

    private UserCrudContext CreateCrudContext()
    {
        var currentUserId = _authContext.CurrentUser?.Id ?? 0;
        return UserCrudContext.Create(currentUserId, _authContext.CurrentIpAddress, _authContext.CurrentDeviceInfo, _authContext.CurrentSessionId);
    }

    private static User CloneUser(User source)
    {
        return new User
        {
            Id = source.Id,
            Username = source.Username,
            PasswordHash = source.PasswordHash,
            FullName = source.FullName,
            Email = source.Email,
            Phone = source.Phone,
            Role = source.Role,
            Active = source.Active,
            DepartmentName = source.DepartmentName,
            IsLocked = source.IsLocked,
            IsTwoFactorEnabled = source.IsTwoFactorEnabled,
            DepartmentId = source.DepartmentId,
            DigitalSignature = source.DigitalSignature,
            RoleIds = source.RoleIds?.ToArray() ?? Array.Empty<int>()
        };
    }

    partial void OnEditorChanging(UserEditor value)
    {
        if (value is not null)
        {
            value.PropertyChanged -= OnEditorPropertyChanged;
        }
    }

    partial void OnEditorChanged(UserEditor value)
    {
        if (value is not null)
        {
            value.PropertyChanged += OnEditorPropertyChanged;
        }
    }

    public sealed partial class UserEditor : ObservableObject
    {
        [ObservableProperty]
        private int _id;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _fullName = string.Empty;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _phone = string.Empty;

        [ObservableProperty]
        private string _role = string.Empty;

        [ObservableProperty]
        private string _departmentName = string.Empty;

        [ObservableProperty]
        private bool _active = true;

        [ObservableProperty]
        private bool _isLocked;

        [ObservableProperty]
        private bool _isTwoFactorEnabled;

        [ObservableProperty]
        private string _newPassword = string.Empty;

        [ObservableProperty]
        private string _confirmPassword = string.Empty;

        [ObservableProperty]
        private int[] _roleIds = Array.Empty<int>();

        public static UserEditor CreateEmpty() => new();

        public static UserEditor CreateForNew() => new() { Active = true, IsLocked = false, IsTwoFactorEnabled = false };

        public static UserEditor FromUser(User user)
        {
            return new UserEditor
            {
                Id = user.Id,
                Username = user.Username ?? string.Empty,
                FullName = user.FullName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Phone = user.Phone ?? string.Empty,
                Role = user.Role ?? string.Empty,
                DepartmentName = user.DepartmentName ?? string.Empty,
                Active = user.Active,
                IsLocked = user.IsLocked,
                IsTwoFactorEnabled = user.IsTwoFactorEnabled,
                RoleIds = user.RoleIds?.ToArray() ?? Array.Empty<int>()
            };
        }

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
                RoleIds = RoleIds.ToArray()
            };
        }

        public void ApplyTo(User target)
        {
            target.Id = Id;
            target.Username = (Username ?? string.Empty).Trim();
            target.FullName = (FullName ?? string.Empty).Trim();
            target.Email = (Email ?? string.Empty).Trim();
            target.Phone = (Phone ?? string.Empty).Trim();
            target.Role = string.IsNullOrWhiteSpace(Role) ? "User" : Role.Trim();
            target.DepartmentName = (DepartmentName ?? string.Empty).Trim();
            target.Active = Active;
            target.IsLocked = IsLocked;
            target.IsTwoFactorEnabled = IsTwoFactorEnabled;
        }
    }

    public sealed partial class RoleOption : ObservableObject
    {
        public RoleOption(int roleId, string name, string? description)
        {
            RoleId = roleId;
            Name = name;
            Description = description ?? string.Empty;
        }

        public int RoleId { get; }

        public string Name { get; }

        public string Description { get; }

        [ObservableProperty]
        private bool _isSelected;
    }
}
