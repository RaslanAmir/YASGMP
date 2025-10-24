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
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>Administers user and role metadata inside the WPF shell using SAP B1 flows.</summary>
/// <remarks>
/// Form Modes: Find performs user lookup via CFL, Add seeds <see cref="UserEditor.CreateEmpty"/>, View is read-only for account review, and Update unlocks editing (including password resets and role assignments).
/// Audit &amp; Logging: Saves flow through <see cref="IUserCrudService"/> with required e-signature capture; RBAC audit footprints are emitted by that service rather than this view-model.
/// Localization: Inline literals such as `"Security"`, `"Select User"`, role labels, and status prompts remain until the security resource file is wired in.
/// Navigation: ModuleKey `Security` registers the module, and `ModuleRecord` entries expose user identifiers so Golden Arrow and CFL navigation from other modules (e.g. attachments) route back here while status strings keep the shell informed.
/// </remarks>
public sealed partial class SecurityModuleViewModel : DataDrivenModuleDocumentViewModel
{
    /// <summary>Shell registration key that binds Security into the docking layout.</summary>
    /// <remarks>Execution: Resolved when the shell composes modules and persists layouts. Form Mode: Identifier applies across Find/Add/View/Update. Localization: Currently paired with the inline caption "Security" until `Modules_Security_Title` is introduced.</remarks>
    public new const string ModuleKey = "Security";

    private readonly IUserCrudService _userService;
    private readonly IAuthContext _authContext;
    private readonly IElectronicSignatureDialogService _signatureDialog;
    private readonly ObservableCollection<RoleOption> _roleOptions = new();
    private readonly List<Role> _availableRoles = new();
    private User? _loadedUser;
    private UserEditor? _snapshot;
    private bool _suppressEditorDirty;
    private bool _suppressRoleDirty;

    /// <summary>Initializes the Security module view model with domain and shell services.</summary>
    /// <remarks>Execution: Invoked when the shell activates the module or Golden Arrow navigation materializes it. Form Mode: Seeds Find/View immediately while deferring Add/Update wiring to later transitions. Localization: Relies on inline strings for tab titles and prompts until module resources exist.</remarks>
    public SecurityModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        IUserCrudService userService,
        IAuthContext authContext,
        IElectronicSignatureDialogService signatureDialog,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "Security", databaseService, cflDialogService, shellInteraction, navigation, auditService)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _signatureDialog = signatureDialog ?? throw new ArgumentNullException(nameof(signatureDialog));
        Editor = UserEditor.CreateEmpty();
        RoleOptions = new ReadOnlyObservableCollection<RoleOption>(_roleOptions);
        SummarizeWithAiCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(OpenAiSummary);
        Toolbar.Add(new ModuleToolbarCommand("Summarize (AI)", SummarizeWithAiCommand));
    }

    /// <summary>Generated property exposing the editor for the Security module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Security_Editor` resources are available.</remarks>
    [ObservableProperty]
    private UserEditor _editor;

    /// <summary>Generated property exposing the is editor enabled for the Security module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Security_IsEditorEnabled` resources are available.</remarks>
    [ObservableProperty]
    private bool _isEditorEnabled;

    /// <summary>Opens the AI module to summarize the selected user’s RBAC and activity.</summary>
    public CommunityToolkit.Mvvm.Input.IRelayCommand SummarizeWithAiCommand { get; }

    /// <summary>Collection presenting the role options for the Security document host.</summary>
    /// <remarks>Execution: Populated as records load or staging mutates. Form Mode: Visible in all modes with editing reserved for Add/Update. Localization: Grid headers/tooltips remain inline until `Modules_Security_Grid` resources exist.</remarks>
    public ReadOnlyObservableCollection<RoleOption> RoleOptions { get; }

    /// <summary>Loads Security records from domain services.</summary>
    /// <remarks>Execution: Triggered by Find refreshes and shell activation. Form Mode: Supplies data for Find/View while Add/Update reuse cached results. Localization: Emits inline status strings pending `Status_Security_Loaded` resources.</remarks>
    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var users = await _userService.GetAllAsync().ConfigureAwait(false);
        return users.Select(ToRecord).ToList();
    }

    /// <summary>Provides design-time sample data for the Security designer experience.</summary>
    /// <remarks>Execution: Invoked only by design-mode checks to support Blend/preview tooling. Form Mode: Mirrors Find mode to preview list layouts. Localization: Sample literals remain inline for clarity.</remarks>
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

    /// <summary>Builds the Choose-From-List request used for Golden Arrow navigation.</summary>
    /// <remarks>Execution: Called when the shell launches CFL dialogs, routing via `ModuleKey` "Security". Form Mode: Provides lookup data irrespective of current mode. Localization: Dialog titles and descriptions use inline strings until `CFL_Security` resources exist.</remarks>
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

    /// <summary>Applies CFL selections back into the Security workspace.</summary>
    /// <remarks>Execution: Runs after CFL or Golden Arrow completion, updating `StatusMessage` for `ModuleKey` "Security". Form Mode: Navigates records without disturbing active edits. Localization: Status feedback uses inline phrases pending `Status_Security_Filtered`.</remarks>
    protected override Task OnCflSelectionAsync(CflResult result)
    {
        var match = Records.FirstOrDefault(r => r.Key == result.Selected.Key);
        if (match is not null)
        {
            SelectedRecord = match;
        }

        return Task.CompletedTask;
    }

    /// <summary>Loads editor payloads for the selected Security record.</summary>
    /// <remarks>Execution: Triggered when document tabs change or shell routing targets `ModuleKey` "Security". Form Mode: Honors Add/Update safeguards to avoid overwriting dirty state. Localization: Inline status/error strings remain until `Status_Security` resources are available.</remarks>
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

    /// <summary>Adjusts command enablement and editor state when the form mode changes.</summary>
    /// <remarks>Execution: Fired by the SAP B1 style form state machine when Find/Add/View/Update transitions occur. Form Mode: Governs which controls are writable and which commands are visible. Localization: Mode change prompts use inline strings pending localization resources.</remarks>
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

    private void OpenAiSummary()
    {
        if (SelectedRecord is null && _loadedUser is null)
        {
            StatusMessage = "Select a user to summarize.";
            return;
        }

        var u = _loadedUser;
        string prompt = u is null
            ? $"Summarize user: {SelectedRecord?.Title}. Provide roles/permissions and potential RBAC risks in <= 8 bullets."
            : $"Summarize this user (<= 8 bullets). Username={u.Username}; Email={u.Email}; Roles={string.Join(',', u.RoleIds ?? Array.Empty<int>())}; Active={u.Active}; LastModified={u.LastModified:O}. Include RBAC concerns if any.";

        var shell = YasGMP.Common.ServiceLocator.GetRequiredService<IShellInteractionService>();
        var doc = shell.OpenModule(AiModuleViewModel.ModuleKey, $"prompt:{prompt}");
        shell.Activate(doc);
    }

    /// <summary>Reverts in-flight edits and restores the last committed snapshot.</summary>
    /// <remarks>Execution: Activated when Cancel is chosen mid-edit. Form Mode: Applies to Add/Update; inert elsewhere. Localization: Cancellation prompts use inline text until localized resources exist.</remarks>
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

    /// <summary>Validates the current editor payload before persistence.</summary>
    /// <remarks>Execution: Invoked immediately prior to OK/Update actions. Form Mode: Only Add/Update trigger validation. Localization: Error messages flow from inline literals until validation resources are added.</remarks>
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

    /// <summary>Persists the current record and coordinates signatures, attachments, and audits.</summary>
    /// <remarks>Execution: Runs after validation when OK/Update is confirmed. Form Mode: Exclusive to Add/Update operations. Localization: Success/failure messaging remains inline pending dedicated resources.</remarks>
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

        var context = UserCrudContext.Create(
            _authContext.CurrentUser?.Id ?? 0,
            _authContext.CurrentIpAddress,
            _authContext.CurrentDeviceInfo,
            _authContext.CurrentSessionId,
            signatureResult);

        var signatureHash = signatureResult.Signature.SignatureHash ?? string.Empty;
        user.DigitalSignature = signatureHash;
        user.LastChangeSignature = signatureHash;
        user.LastModified = DateTime.UtcNow;
        user.LastModifiedById = _authContext.CurrentUser?.Id;

        User adapterResult = user;
        CrudSaveResult saveResult;
        try
        {
            if (user.Id == 0)
            {
                if (password is null)
                {
                    throw new InvalidOperationException("Password is required for new users.");
                }

                saveResult = await _userService.CreateAsync(user, password, context).ConfigureAwait(false);
                if (user.Id == 0 && saveResult.Id > 0)
                {
                    user.Id = saveResult.Id;
                }
            }
            else
            {
                if (password is null && _loadedUser is not null)
                {
                    user.PasswordHash = _loadedUser.PasswordHash;
                }

                saveResult = await _userService.UpdateAsync(user, password, context).ConfigureAwait(false);
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

        SignaturePersistenceHelper.ApplyEntityMetadata(
            signatureResult,
            tableName: "users",
            recordId: adapterResult.Id,
            metadata: saveResult.SignatureMetadata,
            fallbackSignatureHash: adapterResult.DigitalSignature,
            fallbackMethod: context.SignatureMethod,
            fallbackStatus: context.SignatureStatus,
            fallbackNote: context.SignatureNote,
            signedAt: signatureResult.Signature.SignedAt,
            fallbackDeviceInfo: context.DeviceInfo,
            fallbackIpAddress: context.Ip,
            fallbackSessionId: context.SessionId);

        try
        {
            await SignaturePersistenceHelper
                .PersistIfRequiredAsync(_signatureDialog, signatureResult)
                .ConfigureAwait(false);
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

    /// <summary>Executes the matches search routine for the Security module.</summary>
    /// <remarks>Execution: Part of the module lifecycle. Form Mode: Applies as dictated by the calling sequence. Localization: Emits inline text pending localized resources.</remarks>
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


