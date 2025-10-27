using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    private readonly IDialogService _dialogService;
    private readonly Func<UserEditDialogViewModel> _userDialogFactory;
    private readonly UserEditDialogViewModel _inspectorDialog;
    private User? _loadedUser;

    /// <summary>Initializes a new instance of the <see cref="SecurityModuleViewModel"/> class.</summary>
    public SecurityModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        IUserCrudService userService,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation,
        ILocalizationService localization,
        IDialogService dialogService,
        Func<UserEditDialogViewModel> userDialogFactory)
        : base(ModuleKey, localization.GetString("Module.Title.Security"), databaseService, localization, cflDialogService, shellInteraction, navigation, auditService)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _userDialogFactory = userDialogFactory ?? throw new ArgumentNullException(nameof(userDialogFactory));

        _inspectorDialog = _userDialogFactory();

        OpenUserEditDialogCommand = new AsyncRelayCommand<OpenUserEditDialogContext?>(ExecuteOpenUserEditDialogAsync);
        CreateUserCommand = new AsyncRelayCommand(CreateUserAsync);
        EditUserCommand = new AsyncRelayCommand(EditUserAsync);
    }

    /// <summary>Dialog-oriented editor surface used by the module view.</summary>
    public UserEditDialogViewModel Dialog => _inspectorDialog;

    /// <summary>Command that orchestrates the dialog workflow.</summary>
    public IAsyncRelayCommand<OpenUserEditDialogContext?> OpenUserEditDialogCommand { get; }

    /// <summary>Command invoked when Add mode is triggered.</summary>
    public IAsyncRelayCommand CreateUserCommand { get; }

    /// <summary>Command invoked when Update mode is triggered.</summary>
    public IAsyncRelayCommand EditUserCommand { get; }

    /// <summary>
    /// Provides a binding-friendly dialog context for the currently loaded user so the view can
    /// launch the shared dialog command without rebuilding parameters in XAML.
    /// </summary>
    public OpenUserEditDialogContext? SelectedUserDialogContext
        => _loadedUser is null
            ? null
            : new OpenUserEditDialogContext(FormMode.Update, CloneUser(_loadedUser), SelectedRecord);

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
            NotifySelectedUserDialogContextChanged();
            await _inspectorDialog.InitializeAsync(null).ConfigureAwait(false);
            return;
        }

        if (!int.TryParse(record.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
        {
            _loadedUser = null;
            NotifySelectedUserDialogContextChanged();
            StatusMessage = "Invalid user identifier.";
            return;
        }

        var user = await _userService.TryGetByIdAsync(id).ConfigureAwait(false);
        if (user is null)
        {
            _loadedUser = null;
            NotifySelectedUserDialogContextChanged();
            StatusMessage = $"Unable to locate user #{id}.";
            return;
        }

        _loadedUser = user;
        NotifySelectedUserDialogContextChanged();
        await _inspectorDialog.InitializeAsync(user).ConfigureAwait(false);
    }

    protected override async Task OnModeChangedAsync(FormMode mode)
    {
        switch (mode)
        {
            case FormMode.Add:
                await CreateUserCommand.ExecuteAsync(null).ConfigureAwait(false);
                break;
            case FormMode.Update:
                await EditUserCommand.ExecuteAsync(null).ConfigureAwait(false);
                break;
        }
    }

    protected override void OnCancel()
    {
        Mode = FormMode.View;
    }

    protected override Task<IReadOnlyList<string>> ValidateAsync()
        => Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

    protected override Task<bool> OnSaveAsync()
        => Task.FromResult(false);

    protected override bool MatchesSearch(ModuleRecord record, string searchText)
    {
        if (base.MatchesSearch(record, searchText))
        {
            return true;
        }

        return record.InspectorFields.Any(field =>
            field.Value.Contains(searchText, StringComparison.OrdinalIgnoreCase));
    }

    private async Task ExecuteOpenUserEditDialogAsync(OpenUserEditDialogContext? context)
    {
        if (context is null || IsBusy)
        {
            return;
        }

        var dialogViewModel = _userDialogFactory();
        await dialogViewModel.InitializeAsync(context.User).ConfigureAwait(false);

        UserEditDialogViewModel.UserEditDialogResult? result;
        try
        {
            IsBusy = true;
            result = await _dialogService
                .ShowDialogAsync<UserEditDialogViewModel.UserEditDialogResult>(
                    DialogIds.UserEdit,
                    new UserEditDialogRequest(context.Mode, dialogViewModel))
                .ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
        }

        await ApplyDialogResultAsync(dialogViewModel, result).ConfigureAwait(false);
    }

    private Task CreateUserAsync()
    {
        if (IsBusy)
        {
            return Task.CompletedTask;
        }

        var context = new OpenUserEditDialogContext(FormMode.Add, null, SelectedRecord);
        return ExecuteOpenUserEditDialogAsync(context);
    }

    private Task EditUserAsync()
    {
        if (IsBusy)
        {
            return Task.CompletedTask;
        }

        if (_loadedUser is null && SelectedRecord is not null)
        {
            if (int.TryParse(SelectedRecord.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            {
                return LoadAndEditAsync(id);
            }
        }

        if (_loadedUser is null)
        {
            StatusMessage = "Select a user before editing.";
            Mode = FormMode.View;
            return Task.CompletedTask;
        }

        var context = new OpenUserEditDialogContext(FormMode.Update, CloneUser(_loadedUser), SelectedRecord);
        return ExecuteOpenUserEditDialogAsync(context);
    }

    private async Task LoadAndEditAsync(int userId)
    {
        var user = await _userService.TryGetByIdAsync(userId).ConfigureAwait(false);
        if (user is null)
        {
            StatusMessage = $"Unable to locate user #{userId}.";
            Mode = FormMode.View;
            return;
        }

        _loadedUser = user;
        NotifySelectedUserDialogContextChanged();
        var context = new OpenUserEditDialogContext(FormMode.Update, CloneUser(user), SelectedRecord);
        await ExecuteOpenUserEditDialogAsync(context).ConfigureAwait(false);
    }

    private async Task ApplyDialogResultAsync(
        UserEditDialogViewModel dialogViewModel,
        UserEditDialogViewModel.UserEditDialogResult? result)
    {
        Mode = FormMode.View;
        ValidationMessages.Clear();

        if (!string.IsNullOrWhiteSpace(dialogViewModel.StatusMessage))
        {
            StatusMessage = dialogViewModel.StatusMessage;
        }

        if (result is null)
        {
            return;
        }

        if (result.Saved)
        {
            if (string.IsNullOrWhiteSpace(StatusMessage))
            {
                StatusMessage = "User saved successfully.";
            }

            await RefreshAsync().ConfigureAwait(false);
            ReselectRecord(result.EditorState);
            return;
        }

        if (result.ImpersonationRequested)
        {
            StatusMessage = result.ImpersonationTargetId.HasValue
                ? $"Impersonation requested for #{result.ImpersonationTargetId}."
                : "Impersonation requested.";
            return;
        }

        if (result.ImpersonationEnded)
        {
            StatusMessage = "Impersonation session ended.";
        }
    }

    private void ReselectRecord(UserEditDialogViewModel.UserEditor editor)
    {
        ModuleRecord? match = null;
        if (editor.Id > 0)
        {
            var key = editor.Id.ToString(CultureInfo.InvariantCulture);
            match = Records.FirstOrDefault(r => r.Key == key);
        }

        if (match is null && !string.IsNullOrWhiteSpace(editor.Username))
        {
            match = Records.FirstOrDefault(r =>
                string.Equals(r.Title, editor.Username, StringComparison.OrdinalIgnoreCase));
        }

        if (match is not null)
        {
            SelectedRecord = match;
        }
    }

    private void NotifySelectedUserDialogContextChanged()
        => OnPropertyChanged(nameof(SelectedUserDialogContext));

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

    /// <summary>Context passed to <see cref="OpenUserEditDialogCommand"/>.</summary>
    public sealed record OpenUserEditDialogContext(FormMode Mode, User? User, ModuleRecord? SelectedRecord);
}
