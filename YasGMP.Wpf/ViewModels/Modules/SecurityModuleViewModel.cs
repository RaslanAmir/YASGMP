using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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

/// <summary>
/// Represents the security module view model surfaced inside the WPF shell.
/// </summary>
public sealed partial class SecurityModuleViewModel : DataDrivenModuleDocumentViewModel
{
    /// <summary>Stable registration key for the module.</summary>
    public const string ModuleKey = "Security";

    private readonly IUserCrudService _userService;
    private readonly IAuthContext _authContext;
    private readonly IElectronicSignatureDialogService _signatureDialog;
    private readonly UserEditDialogViewModel _dialog;
    private User? _loadedUser;
    private UserEditDialogViewModel.UserEditor? _snapshot;
    private UserEditDialogViewModel.UserEditor? _currentEditor;
    private bool _suppressDialogNotifications;

    /// <summary>Initializes a new instance of the <see cref="SecurityModuleViewModel"/> class.</summary>
    public SecurityModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        IUserCrudService userService,
        IAuthContext authContext,
        IElectronicSignatureDialogService signatureDialog,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation,
        ILocalizationService localization,
        UserEditDialogViewModel dialogViewModel)
        : base(ModuleKey, localization.GetString("Module.Title.Security"), databaseService, localization, cflDialogService, shellInteraction, navigation, auditService)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _signatureDialog = signatureDialog ?? throw new ArgumentNullException(nameof(signatureDialog));
        _dialog = dialogViewModel ?? throw new ArgumentNullException(nameof(dialogViewModel));

        _dialog.RequestClose += OnDialogRequestClose;
        _dialog.ValidationMessages.CollectionChanged += OnDialogValidationMessagesChanged;
        _dialog.PropertyChanged += OnDialogPropertyChanged;
        _dialog.SaveCallback = ExecuteDialogSaveAsync;

        HookEditor(_dialog.Editor);
        SyncValidationMessages();
    }

    /// <summary>Dialog-oriented editor surface used by the module view.</summary>
    public UserEditDialogViewModel Dialog => _dialog;

    [ObservableProperty]
    private bool _isEditorEnabled;

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
            await _dialog.EnsureRolesLoadedAsync().ConfigureAwait(false);
            SuppressDialogNotifications(() =>
            {
                _dialog.LoadEditor(UserEditDialogViewModel.UserEditor.CreateEmpty());
                _dialog.ApplyRoleSelection(Array.Empty<int>());
            });
            ResetDirty();
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
        await InitializeDialogAsync(user).ConfigureAwait(false);
        ResetDirty();
    }

    protected override async Task OnModeChangedAsync(FormMode mode)
    {
        IsEditorEnabled = mode is FormMode.Add or FormMode.Update;

        if (mode is FormMode.Add or FormMode.Update)
        {
            await _dialog.EnsureRolesLoadedAsync().ConfigureAwait(false);
        }

        switch (mode)
        {
            case FormMode.Add:
                _snapshot = null;
                _loadedUser = null;
                SuppressDialogNotifications(() =>
                {
                    _dialog.LoadEditor(UserEditDialogViewModel.UserEditor.CreateForNew());
                    _dialog.ApplyRoleSelection(Array.Empty<int>());
                });
                break;
            case FormMode.Update:
                _snapshot = _dialog.Editor.Clone();
                break;
            case FormMode.Find:
                SuppressDialogNotifications(() =>
                {
                    _dialog.LoadEditor(UserEditDialogViewModel.UserEditor.CreateEmpty());
                    _dialog.ApplyRoleSelection(Array.Empty<int>());
                });
                break;
        }
    }

    protected override void OnCancel()
    {
        if (Mode == FormMode.Add)
        {
            SuppressDialogNotifications(() =>
            {
                _dialog.LoadEditor(UserEditDialogViewModel.UserEditor.CreateEmpty());
                _dialog.ApplyRoleSelection(Array.Empty<int>());
            });
            ResetDirty();
            return;
        }

        if (Mode == FormMode.Update)
        {
            SuppressDialogNotifications(() =>
            {
                if (_snapshot is not null)
                {
                    _dialog.LoadEditor(_snapshot.Clone());
                    _dialog.ApplyRoleSelection(_snapshot.RoleIds);
                }
                else if (_loadedUser is not null)
                {
                    _dialog.LoadEditor(UserEditDialogViewModel.UserEditor.FromUser(_loadedUser));
                    _dialog.ApplyRoleSelection(_loadedUser.RoleIds ?? Array.Empty<int>());
                }
            });
        }
    }

    protected override async Task<IReadOnlyList<string>> ValidateAsync()
    {
        await _dialog.ValidateAsync().ConfigureAwait(false);
        SyncValidationMessages();
        return _dialog.ValidationMessages.ToList();
    }

    protected override async Task<bool> OnSaveAsync()
    {
        var result = await PersistDialogAsync().ConfigureAwait(false);
        return result.Saved;
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

    private Task InitializeDialogAsync(User? user)
        => SuppressDialogNotificationsAsync(() => _dialog.InitializeAsync(user));

    private async Task<UserEditDialogViewModel.UserEditDialogResult> PersistDialogAsync()
    {
        await _dialog.EnsureRolesLoadedAsync().ConfigureAwait(false);

        var editor = _dialog.Editor;
        var user = _loadedUser is null ? new User() : CloneUser(_loadedUser);
        editor.ApplyTo(user);
        user.RoleIds = editor.RoleIds ?? Array.Empty<int>();

        if (user.RoleIds.Length == 0 && _dialog.RoleOptions.Count > 0)
        {
            var primary = _dialog.RoleOptions.FirstOrDefault(r =>
                string.Equals(r.Name, user.Role, StringComparison.OrdinalIgnoreCase));
            if (primary is not null)
            {
                user.RoleIds = new[] { primary.RoleId };
            }
        }

        _userService.Validate(user);

        if (Mode == FormMode.Update && _loadedUser is null)
        {
            StatusMessage = "Select a user before saving.";
            return CreateResult(saved: false, impersonationRequested: false, impersonationEnded: false);
        }

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
            StatusMessage = $"Electronic signature failed: {ex.Message}";
            return CreateResult(saved: false, impersonationRequested: false, impersonationEnded: false);
        }

        if (signatureResult is null)
        {
            StatusMessage = "Electronic signature cancelled. Save aborted.";
            return CreateResult(saved: false, impersonationRequested: false, impersonationEnded: false);
        }

        if (signatureResult.Signature is null)
        {
            StatusMessage = "Electronic signature was not captured.";
            return CreateResult(saved: false, impersonationRequested: false, impersonationEnded: false);
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
        try
        {
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
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to persist user: {ex.Message}", ex);
        }

        _loadedUser = user;
        _dialog.UpdateLoadedUser(user);
        _snapshot = null;

        SuppressDialogNotifications(() =>
        {
            _dialog.Editor.NewPassword = string.Empty;
            _dialog.Editor.ConfirmPassword = string.Empty;
            _dialog.ApplyRoleSelection(user.RoleIds ?? Array.Empty<int>());
        });

        ResetDirty();

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
            return CreateResult(saved: false, impersonationRequested: false, impersonationEnded: false);
        }

        StatusMessage = $"Electronic signature captured ({signatureResult.ReasonDisplay}).";
        return CreateResult(saved: true, impersonationRequested: false, impersonationEnded: false);
    }

    private Task<UserEditDialogViewModel.UserEditDialogResult> ExecuteDialogSaveAsync(UserEditDialogViewModel dialog)
        => PersistDialogAsync();

    private void OnDialogPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UserEditDialogViewModel.Editor))
        {
            HookEditor(_dialog.Editor);
        }
    }

    private void HookEditor(UserEditDialogViewModel.UserEditor? editor)
    {
        if (_currentEditor is not null)
        {
            _currentEditor.PropertyChanged -= OnDialogEditorPropertyChanged;
        }

        _currentEditor = editor;
        if (_currentEditor is not null)
        {
            _currentEditor.PropertyChanged += OnDialogEditorPropertyChanged;
        }
    }

    private void OnDialogEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_suppressDialogNotifications)
        {
            return;
        }

        if (IsInEditMode)
        {
            MarkDirty();
        }
    }

    private void OnDialogValidationMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => SyncValidationMessages();

    private void SyncValidationMessages()
    {
        ValidationMessages.Clear();
        foreach (var message in _dialog.ValidationMessages)
        {
            ValidationMessages.Add(message);
        }
    }

    private void OnDialogRequestClose(object? sender, bool saved)
    {
        var result = _dialog.Result;
        if (result is null)
        {
            return;
        }

        if (result.Saved)
        {
            Mode = FormMode.View;
            return;
        }

        if (result.ImpersonationRequested)
        {
            StatusMessage = result.ImpersonationTargetId.HasValue
                ? $"Impersonation requested for #{result.ImpersonationTargetId}."
                : "Impersonation requested.";
        }
        else if (result.ImpersonationEnded)
        {
            StatusMessage = "Impersonation session ended.";
        }

        if (!result.Saved && !result.ImpersonationRequested && !result.ImpersonationEnded && Mode is FormMode.Add or FormMode.Update)
        {
            Mode = FormMode.View;
        }
    }

    private void SuppressDialogNotifications(Action action)
    {
        try
        {
            _suppressDialogNotifications = true;
            action();
        }
        finally
        {
            _suppressDialogNotifications = false;
        }
    }

    private async Task SuppressDialogNotificationsAsync(Func<Task> action)
    {
        _suppressDialogNotifications = true;
        try
        {
            await action().ConfigureAwait(false);
        }
        finally
        {
            _suppressDialogNotifications = false;
        }
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

    private UserEditDialogViewModel.UserEditDialogResult CreateResult(bool saved, bool impersonationRequested, bool impersonationEnded)
    {
        return new UserEditDialogViewModel.UserEditDialogResult(
            saved,
            impersonationRequested,
            impersonationEnded,
            _dialog.Editor.Clone(),
            _dialog.SelectedImpersonationTarget?.Id,
            _dialog.ImpersonationReason,
            _dialog.ImpersonationNotes);
    }
}
