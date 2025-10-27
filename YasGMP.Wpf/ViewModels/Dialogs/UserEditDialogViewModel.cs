using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Models;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.ViewModels.Dialogs;

/// <summary>
/// View-model backing the WPF user editor dialog. Relocates the editor helpers from the module view-model,
/// performs validation prior to persistence, and exposes commands for save/cancel as well as impersonation
/// workflow transitions.
/// </summary>
public sealed partial class UserEditDialogViewModel : ObservableObject
{
    private readonly IUserCrudService _userService;
    private readonly ISecurityImpersonationWorkflowService _impersonationWorkflow;
    private readonly IElectronicSignatureDialogService _signatureDialog;
    private readonly IAuthContext _authContext;
    private readonly IShellInteractionService _shellInteraction;
    private readonly ILocalizationService _localization;
    private readonly ObservableCollection<RoleOption> _roleOptions = new();
    private readonly ObservableCollection<string> _validationMessages = new();
    private readonly ObservableCollection<UserSummary> _impersonationTargets = new();
    private readonly List<Role> _availableRoles = new();
    private bool _rolesLoaded;
    private bool _suppressRoleNotifications;
    private bool _suppressEditorNotifications;
    private User? _loadedUser;

    /// <summary>Initializes a new instance of the <see cref="UserEditDialogViewModel"/> class.</summary>
    public UserEditDialogViewModel(
        IUserCrudService userService,
        ISecurityImpersonationWorkflowService impersonationWorkflow,
        IElectronicSignatureDialogService signatureDialog,
        IAuthContext authContext,
        IShellInteractionService shellInteraction,
        ILocalizationService localization)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _impersonationWorkflow = impersonationWorkflow ?? throw new ArgumentNullException(nameof(impersonationWorkflow));
        _signatureDialog = signatureDialog ?? throw new ArgumentNullException(nameof(signatureDialog));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _shellInteraction = shellInteraction ?? throw new ArgumentNullException(nameof(shellInteraction));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));

        Editor = UserEditor.CreateEmpty();
        Editor.PropertyChanged += OnEditorPropertyChanged;

        SaveCommand = new AsyncRelayCommand(SaveAsync, CanExecuteSave);
        CancelCommand = new RelayCommand(Cancel);
        BeginImpersonationCommand = new AsyncRelayCommand(BeginImpersonationAsync, CanBeginImpersonation);
        EndImpersonationCommand = new AsyncRelayCommand(EndImpersonationAsync, CanEndImpersonation);
    }

    /// <summary>Raised when the hosting dialog should close. Payload indicates whether the action completed successfully.</summary>
    public event EventHandler<bool>? RequestClose;

    /// <summary>Callback invoked once validation succeeds and the save command executes.</summary>
    public Func<UserEditDialogViewModel, Task<UserEditDialogResult>>? SaveCallback { get; set; }

    /// <summary>Callback invoked when the impersonation begin command succeeds validation.</summary>
    public Func<UserEditDialogViewModel, Task<UserEditDialogResult>>? BeginImpersonationCallback { get; set; }

    /// <summary>Callback invoked when the impersonation end command executes.</summary>
    public Func<UserEditDialogViewModel, Task<UserEditDialogResult>>? EndImpersonationCallback { get; set; }

    /// <summary>Callback invoked when the cancel command executes.</summary>
    public Func<UserEditDialogViewModel, Task<UserEditDialogResult>>? CancelCallback { get; set; }

    /// <summary>Current editor surface reflected in the dialog.</summary>
    [ObservableProperty]
    private UserEditor _editor;

    /// <summary>Indicates whether a command is executing.</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>Whether impersonation is currently active.</summary>
    [ObservableProperty]
    private bool _isImpersonating;

    /// <summary>Selected impersonation target.</summary>
    [ObservableProperty]
    private UserSummary? _selectedImpersonationTarget;

    /// <summary>Reason supplied when initiating impersonation.</summary>
    [ObservableProperty]
    private string _impersonationReason = string.Empty;

    /// <summary>Optional notes associated with impersonation requests.</summary>
    [ObservableProperty]
    private string _impersonationNotes = string.Empty;

    /// <summary>Status message surfaced to the hosting dialog.</summary>
    [ObservableProperty]
    private string? _statusMessage;

    /// <summary>Exposes the role checkbox collection for binding.</summary>
    public ObservableCollection<RoleOption> RoleOptions => _roleOptions;

    /// <summary>Validation messages populated after <see cref="ValidateAsync"/> runs.</summary>
    public ObservableCollection<string> ValidationMessages => _validationMessages;

    /// <summary>Available impersonation targets surfaced by the workflow service.</summary>
    public ObservableCollection<UserSummary> ImpersonationTargets => _impersonationTargets;

    /// <summary>Command invoked when save is requested.</summary>
    public IAsyncRelayCommand SaveCommand { get; }

    /// <summary>Command invoked when the dialog is cancelled.</summary>
    public IRelayCommand CancelCommand { get; }

    /// <summary>Command invoked to start impersonating the selected user.</summary>
    public IAsyncRelayCommand BeginImpersonationCommand { get; }

    /// <summary>Command invoked to end the active impersonation session.</summary>
    public IAsyncRelayCommand EndImpersonationCommand { get; }

    /// <summary>Result populated when the dialog completes.</summary>
    public UserEditDialogResult? Result { get; private set; }

    /// <summary>Hydrates the editor state from the supplied user and refreshes lookup collections.</summary>
    public async Task InitializeAsync(User? user)
    {
        _loadedUser = user is null ? null : CloneUser(user);
        SetEditor(user is null ? UserEditor.CreateForNew() : UserEditor.FromUser(user));

        await EnsureRolesLoadedAsync().ConfigureAwait(false);
        ApplyRoleSelection(Editor.RoleIds ?? Array.Empty<int>());
        await LoadImpersonationTargetsAsync().ConfigureAwait(false);

        IsImpersonating = _impersonationWorkflow.IsImpersonating;
        if (IsImpersonating)
        {
            var active = _impersonationTargets.FirstOrDefault(t => t.Id == _impersonationWorkflow.ImpersonatedUserId);
            if (active is not null)
            {
                SelectedImpersonationTarget = active;
            }
        }
    }

    partial void OnIsBusyChanged(bool value)
    {
        SaveCommand.NotifyCanExecuteChanged();
        BeginImpersonationCommand.NotifyCanExecuteChanged();
        EndImpersonationCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsImpersonatingChanged(bool value)
    {
        BeginImpersonationCommand.NotifyCanExecuteChanged();
        EndImpersonationCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedImpersonationTargetChanged(UserSummary? value)
        => BeginImpersonationCommand.NotifyCanExecuteChanged();

    private bool CanExecuteSave() => !IsBusy;

    private bool CanBeginImpersonation()
        => !IsBusy && !IsImpersonating && SelectedImpersonationTarget is not null;

    private bool CanEndImpersonation()
        => !IsBusy && (IsImpersonating || _impersonationWorkflow.IsImpersonating);

    public async Task EnsureRolesLoadedAsync()
    {
        if (_rolesLoaded)
        {
            return;
        }

        await LoadRolesAsync().ConfigureAwait(false);
        _rolesLoaded = true;
    }

    private async Task LoadRolesAsync()
    {
        var roles = await _userService.GetAllRolesAsync().ConfigureAwait(false);
        _availableRoles.Clear();
        foreach (var role in roles.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase))
        {
            _availableRoles.Add(role);
        }

        _suppressRoleNotifications = true;
        foreach (var option in _roleOptions)
        {
            option.PropertyChanged -= OnRoleOptionPropertyChanged;
        }

        _roleOptions.Clear();
        foreach (var role in _availableRoles)
        {
            var option = new RoleOption(role.Id, role.Name ?? string.Empty, role.Description);
            option.PropertyChanged += OnRoleOptionPropertyChanged;
            _roleOptions.Add(option);
        }

        _suppressRoleNotifications = false;
    }

    /// <summary>Replaces the current editor state with the supplied instance.</summary>
    public void LoadEditor(UserEditor editor)
        => SetEditor(editor);

    /// <summary>Refreshes the selected role options based on the supplied identifiers.</summary>
    public void ApplyRoleSelection(IEnumerable<int> roleIds)
    {
        var selected = new HashSet<int>(roleIds ?? Array.Empty<int>());
        _suppressRoleNotifications = true;
        foreach (var option in _roleOptions)
        {
            option.IsSelected = selected.Contains(option.RoleId);
        }
        _suppressRoleNotifications = false;
        UpdateEditorRoleSelectionFromOptions();
    }

    /// <summary>Updates the stored reference used for validation and save defaults.</summary>
    public void UpdateLoadedUser(User user)
    {
        _loadedUser = CloneUser(user);
    }

    private async Task LoadImpersonationTargetsAsync()
    {
        var targets = await _impersonationWorkflow.GetImpersonationCandidatesAsync().ConfigureAwait(false);
        _impersonationTargets.Clear();
        foreach (var user in targets.OrderBy(u => u.FullName ?? u.Username, StringComparer.OrdinalIgnoreCase))
        {
            if (user.Id == 0)
            {
                continue;
            }

            _impersonationTargets.Add(UserSummary.FromUser(user));
        }
    }

    private void SetEditor(UserEditor editor)
    {
        if (Editor is not null)
        {
            Editor.PropertyChanged -= OnEditorPropertyChanged;
        }

        Editor = editor;
        Editor.PropertyChanged += OnEditorPropertyChanged;
    }

    private void UpdateEditorRoleSelectionFromOptions()
    {
        var selection = _roleOptions
            .Where(option => option.IsSelected)
            .Select(option => option.RoleId)
            .ToArray();

        _suppressEditorNotifications = true;
        Editor.RoleIds = selection;
        _suppressEditorNotifications = false;
    }

    private void OnEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_suppressEditorNotifications)
        {
            return;
        }

        if (e.PropertyName == nameof(UserEditor.RoleIds))
        {
            var set = new HashSet<int>(Editor.RoleIds ?? Array.Empty<int>());
            _suppressRoleNotifications = true;
            foreach (var option in _roleOptions)
            {
                option.IsSelected = set.Contains(option.RoleId);
            }
            _suppressRoleNotifications = false;
        }
    }

    private void OnRoleOptionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(RoleOption.IsSelected) || _suppressRoleNotifications)
        {
            return;
        }

        UpdateEditorRoleSelectionFromOptions();
    }

    /// <summary>
    /// Validates the editor state and populates <see cref="ValidationMessages"/> when issues are detected.
    /// </summary>
    public async Task<bool> ValidateAsync()
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

        if ((_loadedUser is null || _loadedUser.Id == 0) && string.IsNullOrWhiteSpace(editor.NewPassword))
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

        _validationMessages.Clear();
        foreach (var error in errors)
        {
            _validationMessages.Add(error);
        }

        await Task.CompletedTask;
        return errors.Count == 0;
    }

    private async Task SaveAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = null;
        try
        {
            if (!await ValidateAsync().ConfigureAwait(false))
            {
                StatusMessage = _localization.GetString("Dialog.UserEdit.Status.ResolveValidationBeforeSaving");
                return;
            }

            UserEditDialogResult result;
            if (SaveCallback is not null)
            {
                result = await SaveCallback(this).ConfigureAwait(false);
            }
            else
            {
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

                var password = string.IsNullOrWhiteSpace(editor.NewPassword)
                    ? null
                    : editor.NewPassword.Trim();

                ElectronicSignatureDialogResult? signatureResult;
                try
                {
                    signatureResult = await _signatureDialog
                        .CaptureSignatureAsync(new ElectronicSignatureContext("users", user.Id))
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Electronic signature failed: {ex.Message}", ex);
                }

                if (signatureResult is null)
                {
                    throw new InvalidOperationException("Electronic signature cancelled. Save aborted.");
                }

                if (signatureResult.Signature is null)
                {
                    throw new InvalidOperationException("Electronic signature was not captured.");
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

                CrudSaveResult saveResult;
                if (user.Id == 0)
                {
                    if (string.IsNullOrWhiteSpace(password))
                    {
                        throw new InvalidOperationException("Password is required for new users.");
                    }

                    saveResult = await _userService.CreateAsync(user, password!, context).ConfigureAwait(false);
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

                await _userService.UpdateRoleAssignmentsAsync(
                    user.Id,
                    user.RoleIds ?? Array.Empty<int>(),
                    context).ConfigureAwait(false);

                _loadedUser = user;

                Editor.NewPassword = string.Empty;
                Editor.ConfirmPassword = string.Empty;
                ApplyRoleSelection(user.RoleIds ?? Array.Empty<int>());

                SignaturePersistenceHelper.ApplyEntityMetadata(
                    signatureResult,
                    tableName: "users",
                    recordId: user.Id,
                    metadata: saveResult.SignatureMetadata,
                    fallbackSignatureHash: user.DigitalSignature,
                    fallbackMethod: context.SignatureMethod,
                    fallbackStatus: context.SignatureStatus,
                    fallbackNote: context.SignatureNote,
                    signedAt: signatureResult.Signature.SignedAt,
                    fallbackDeviceInfo: context.DeviceInfo,
                    fallbackIpAddress: context.Ip,
                    fallbackSessionId: context.SessionId);

                await SignaturePersistenceHelper
                    .PersistIfRequiredAsync(_signatureDialog, signatureResult)
                    .ConfigureAwait(false);

                _shellInteraction.UpdateStatus(_localization.GetString("Dialog.UserEdit.Status.Saved"));
                result = CreateResult(saved: true, impersonationRequested: false, impersonationEnded: false);
            }

            Result = result;
            RequestClose?.Invoke(this, result.Saved);
        }
        catch (Exception ex)
        {
            StatusMessage = _localization.GetString("Dialog.UserEdit.Status.SaveFailed", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Cancel()
    {
        if (CancelCallback is not null)
        {
            _ = CancelCallback(this);
        }

        Result = CreateResult(saved: false, impersonationRequested: false, impersonationEnded: false);
        RequestClose?.Invoke(this, false);
    }

    private async Task BeginImpersonationAsync()
    {
        if (IsBusy)
        {
            return;
        }

        ValidationMessages.Clear();
        if (SelectedImpersonationTarget is null)
        {
            ValidationMessages.Add(_localization.GetString("Dialog.UserEdit.Validation.ImpersonationTargetRequired"));
            return;
        }

        if (string.IsNullOrWhiteSpace(ImpersonationReason))
        {
            ValidationMessages.Add(_localization.GetString("Dialog.UserEdit.Validation.ImpersonationReasonRequired"));
            return;
        }

        IsBusy = true;
        StatusMessage = null;
        try
        {
            UserEditDialogResult result;
            if (BeginImpersonationCallback is not null)
            {
                result = await BeginImpersonationCallback(this).ConfigureAwait(false);
            }
            else
            {
                await _impersonationWorkflow.BeginImpersonationAsync(
                    SelectedImpersonationTarget.Id,
                    FormatImpersonationReason() ?? string.Empty,
                    FormatImpersonationNotes()).ConfigureAwait(false);
                _shellInteraction.UpdateStatus(_localization.GetString(
                    "Dialog.UserEdit.Status.ImpersonationRequestedWithTarget",
                    SelectedImpersonationTarget.Id));
                result = CreateResult(saved: false, impersonationRequested: true, impersonationEnded: false);
            }

            Result = result;
            RequestClose?.Invoke(this, result.ImpersonationRequested);
        }
        catch (Exception ex)
        {
            StatusMessage = _localization.GetString("Dialog.UserEdit.Status.ImpersonationFailed", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task EndImpersonationAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = null;
        try
        {
            UserEditDialogResult result;
            if (EndImpersonationCallback is not null)
            {
                result = await EndImpersonationCallback(this).ConfigureAwait(false);
            }
            else
            {
                await _impersonationWorkflow.EndImpersonationAsync().ConfigureAwait(false);
                _shellInteraction.UpdateStatus(_localization.GetString("Dialog.UserEdit.Status.ImpersonationEnded"));
                result = CreateResult(saved: false, impersonationRequested: false, impersonationEnded: true);
            }

            Result = result;
            RequestClose?.Invoke(this, result.ImpersonationEnded);
        }
        catch (Exception ex)
        {
            StatusMessage = _localization.GetString("Dialog.UserEdit.Status.EndImpersonationFailed", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private UserEditDialogResult CreateResult(bool saved, bool impersonationRequested, bool impersonationEnded)
        => new(
            saved,
            impersonationRequested,
            impersonationEnded,
            Editor.Clone(),
            SelectedImpersonationTarget?.Id,
            FormatImpersonationReason(),
            FormatImpersonationNotes());

    private string? FormatImpersonationReason()
    {
        var reason = (ImpersonationReason ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(reason) ? null : reason;
    }

    private string? FormatImpersonationNotes()
    {
        var notes = (ImpersonationNotes ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(notes) ? null : notes;
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

    /// <summary>Represents the user editor surface.</summary>
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

        /// <summary>Creates an empty editor.</summary>
        public static UserEditor CreateEmpty() => new();

        /// <summary>Creates an editor primed for a new user.</summary>
        public static UserEditor CreateForNew() => new() { Active = true, IsLocked = false, IsTwoFactorEnabled = false };

        /// <summary>Hydrates an editor from an existing user.</summary>
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
                RoleIds = RoleIds.ToArray()
            };
        }

        /// <summary>Applies the editor state to the supplied user instance.</summary>
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

    /// <summary>Represents a selectable role option within the dialog.</summary>
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

    /// <summary>Lightweight summary for impersonation candidates.</summary>
    public sealed record UserSummary(int Id, string DisplayName)
    {
        public static UserSummary FromUser(User user)
        {
            var display = string.IsNullOrWhiteSpace(user.FullName)
                ? user.Username
                : user.FullName;
            return new UserSummary(user.Id, display ?? string.Empty);
        }
    }

    /// <summary>Result payload raised when the dialog closes.</summary>
    public sealed record UserEditDialogResult(
        bool Saved,
        bool ImpersonationRequested,
        bool ImpersonationEnded,
        UserEditor EditorState,
        int? ImpersonationTargetId,
        string? ImpersonationReason,
        string? ImpersonationNotes);
}
