using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Common;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.ViewModels.Modules;
/// <summary>
/// Represents the Admin Module View Model.
/// </summary>

public sealed class AdminModuleViewModel : DataDrivenModuleDocumentViewModel
{
    /// <summary>
    /// Editable representation of a <see cref="Setting"/> instance surfaced to the UI.
    /// </summary>
    public sealed class EditableSetting : ObservableObject
    {
        private readonly Func<EditableSetting, string?, Task>? _changeCallback;
        private bool _suppressCallback;

        private string? _key;
        private string? _value;
        private string? _category;
        private string? _description;
        private bool _isNew;
        private bool _isMarkedForDeletion;

        public EditableSetting(Func<EditableSetting, string?, Task>? changeCallback)
        {
            _changeCallback = changeCallback;
        }

        public string? Key
        {
            get => _key;
            set => SetPropertyAndNotify(ref _key, value);
        }

        public string? Value
        {
            get => _value;
            set => SetPropertyAndNotify(ref _value, value);
        }

        public string? Category
        {
            get => _category;
            set => SetPropertyAndNotify(ref _category, value);
        }

        public string? Description
        {
            get => _description;
            set => SetPropertyAndNotify(ref _description, value);
        }

        public bool IsNew
        {
            get => _isNew;
            set => SetPropertyAndNotify(ref _isNew, value);
        }

        public bool IsMarkedForDeletion
        {
            get => _isMarkedForDeletion;
            set => SetPropertyAndNotify(ref _isMarkedForDeletion, value);
        }

        public static EditableSetting FromSetting(Setting setting, Func<EditableSetting, string?, Task>? changeCallback)
        {
            if (setting is null)
            {
                throw new ArgumentNullException(nameof(setting));
            }

            var editable = new EditableSetting(changeCallback);
            using (editable.DeferNotifications())
            {
                editable.Key = setting.Key;
                editable.Value = setting.Value;
                editable.Category = setting.Category;
                editable.Description = setting.Description;
                editable.IsNew = false;
                editable.IsMarkedForDeletion = false;
            }

            return editable;
        }

        public static EditableSetting CreateNew(Func<EditableSetting, string?, Task>? changeCallback)
        {
            var editable = new EditableSetting(changeCallback);
            using (editable.DeferNotifications())
            {
                editable.IsNew = true;
                editable.IsMarkedForDeletion = false;
            }

            return editable;
        }

        public IDisposable DeferNotifications()
            => new NotificationSuppression(this);

        private void SetPropertyAndNotify<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (SetProperty(ref storage, value, propertyName) && !_suppressCallback)
            {
                var callback = _changeCallback;
                if (callback is not null)
                {
                    _ = callback(this, propertyName);
                }
            }
        }

        private sealed class NotificationSuppression : IDisposable
        {
            private readonly EditableSetting _owner;
            private bool _disposed;

            public NotificationSuppression(EditableSetting owner)
            {
                _owner = owner;
                _owner._suppressCallback = true;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _owner._suppressCallback = false;
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Represents the module key value.
    /// </summary>
    public const string ModuleKey = "Admin";

    private readonly INotificationPreferenceService _notificationPreferences;
    private readonly IShellAlertService? _alerts;
    private readonly ILocalizationService _localizationService;
    private readonly IElectronicSignatureDialogService _signatureDialog;
    private readonly IDialogService _dialogService;
    private readonly IAuthContext _authContext;
    private readonly Dictionary<string, Setting> _settingsByRecordKey = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Setting> _settingsByCode = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _validationLock = new();
    private readonly object _pendingSettingEditsLock = new();
    private readonly HashSet<EditableSetting> _pendingSettingEdits = new();

    private bool _suppressPreferenceDirty;
    private bool _statusBarAlertsEnabled;
    private bool _toastAlertsEnabled;
    private bool _isNotificationPreferencesDirty;
    private EditableSetting? _currentSetting;
    private CancellationTokenSource? _validationCts;
    /// <summary>
    /// Initializes a new instance of the AdminModuleViewModel class.
    /// </summary>

    public AdminModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        IElectronicSignatureDialogService signatureDialog,
        IDialogService dialogService,
        IAuthContext authContext,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation,
        ILocalizationService localization,
        INotificationPreferenceService notificationPreferences)
        : base(ModuleKey, localization.GetString("Module.Title.Administration"), databaseService, localization, cflDialogService, shellInteraction, navigation, auditService)
    {
        _notificationPreferences = notificationPreferences ?? throw new ArgumentNullException(nameof(notificationPreferences));
        _localizationService = localization ?? throw new ArgumentNullException(nameof(localization));
        _signatureDialog = signatureDialog ?? throw new ArgumentNullException(nameof(signatureDialog));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _alerts = ServiceLocator.GetService<IShellAlertService>();

        SaveNotificationPreferencesCommand = new AsyncRelayCommand(SaveNotificationPreferencesAsync, CanSaveNotificationPreferences);
        RestoreSettingCommand = new AsyncRelayCommand(RestoreSelectedSettingAsync, CanRestoreSetting);
        SettingCategories = new ObservableCollection<string>(new[] { "General", "System", "Notifications" });
        SettingStatuses = new ObservableCollection<string>(new[] { "active", "inactive", "deprecated" });
        PropertyChanged += OnPropertyChanged;
    }

    /// <summary>Gets or sets whether shell status bar alerts are enabled.</summary>
    public bool StatusBarAlertsEnabled
    {
        get => _statusBarAlertsEnabled;
        set
        {
            if (SetProperty(ref _statusBarAlertsEnabled, value))
            {
                OnPreferenceChanged();
            }
        }
    }

    /// <summary>Gets or sets whether toast notifications are enabled.</summary>
    public bool ToastAlertsEnabled
    {
        get => _toastAlertsEnabled;
        set
        {
            if (SetProperty(ref _toastAlertsEnabled, value))
            {
                OnPreferenceChanged();
            }
        }
    }

    /// <summary>Indicates whether notification preferences have unsaved changes.</summary>
    public bool IsNotificationPreferencesDirty
    {
        get => _isNotificationPreferencesDirty;
        private set
        {
            if (SetProperty(ref _isNotificationPreferencesDirty, value))
            {
                SaveNotificationPreferencesCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>Command that persists notification preference changes.</summary>
    public IAsyncRelayCommand SaveNotificationPreferencesCommand { get; }

    /// <summary>Command that reverts the selected setting to its default value.</summary>
    public IAsyncRelayCommand RestoreSettingCommand { get; }

    /// <summary>Currently edited setting payload.</summary>
    public EditableSetting? CurrentSetting
    {
        get => _currentSetting;
        private set => SetCurrentSetting(value);
    }

    /// <summary>Lookup values for setting categories exposed to the UI.</summary>
    public ObservableCollection<string> SettingCategories { get; }

    /// <summary>Lookup values for setting status choices exposed to the UI.</summary>
    public ObservableCollection<string> SettingStatuses { get; }

    private string? _lastSignatureStatus;

    /// <summary>Gets the most recent electronic signature status surfaced to QA.</summary>
    public string? LastSignatureStatus
    {
        get => _lastSignatureStatus;
        private set => SetProperty(ref _lastSignatureStatus, value);
    }

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        await LoadNotificationPreferencesAsync().ConfigureAwait(false);
        var settings = await Database.GetAllSettingsFullAsync().ConfigureAwait(false);
        _settingsByRecordKey.Clear();
        _settingsByCode.Clear();

        foreach (var setting in settings)
        {
            var recordKey = setting.Id.ToString(CultureInfo.InvariantCulture);
            _settingsByRecordKey[recordKey] = setting;

            if (!string.IsNullOrWhiteSpace(setting.Key))
            {
                _settingsByCode[setting.Key!] = setting;
            }
        }

        await UpdateChoiceCollectionsAsync(settings).ConfigureAwait(false);

        return settings.Select(ToRecord).ToList();
    }

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
        => new List<ModuleRecord>
        {
            new("CFG-001", "Default Locale", "locale", "Active", "System default locale",
                new[]
                {
                    CreateInspectorField("CFG-001", "Default Locale", "Value", "hr-HR"),
                    CreateInspectorField("CFG-001", "Default Locale", "Category", "System"),
                    CreateInspectorField(
                        "CFG-001",
                        "Default Locale",
                        "Updated",
                        System.DateTime.Now.AddDays(-2).ToString("g"))
                },
                null, null),
            new("CFG-002", "Maintenance Window", "maintenance_window", "Active", "Weekly downtime",
                new[]
                {
                    CreateInspectorField("CFG-002", "Maintenance Window", "Value", "Sundays 02:00-03:00"),
                    CreateInspectorField("CFG-002", "Maintenance Window", "Category", "System"),
                    CreateInspectorField(
                        "CFG-002",
                        "Maintenance Window",
                        "Updated",
                        System.DateTime.Now.AddDays(-10).ToString("g"))
                },
                null, null)
        };

    private ModuleRecord ToRecord(Setting setting)
    {
        var recordKey = setting.Id.ToString();
        var recordTitle = string.IsNullOrWhiteSpace(setting.Key) ? "Setting" : setting.Key!;

        InspectorField Field(string label, string? value) => CreateInspectorField(recordKey, recordTitle, label, value);

        var fields = new List<InspectorField>
        {
            Field("Category", setting.Category ?? "-"),
            Field("Value", setting.Value ?? string.Empty),
            Field("Description", setting.Description ?? string.Empty),
            Field("Updated", setting.UpdatedAt?.ToString("g") ?? "-"),
        };

        return new ModuleRecord(
            recordKey,
            recordTitle,
            setting.Key,
            "Active",
            setting.Description,
            fields,
            null,
            null);
    }

    protected override Task OnRecordSelectedAsync(ModuleRecord? record)
    {
        UpdateCurrentSetting(record);
        ResetDirty();
        ClearValidationMessages();

        return base.OnRecordSelectedAsync(record);
    }

    protected override async Task OnModeChangedAsync(FormMode mode)
    {
        if (mode == FormMode.Add)
        {
            var editable = EditableSetting.CreateNew(OnEditableSettingChangedAsync);
            SetCurrentSetting(editable);
        }
        else if (mode != FormMode.Update && CurrentSetting is not null)
        {
            var current = CurrentSetting;
            current.PropertyChanged -= OnCurrentSettingPropertyChanged;

            try
            {
                using (current.DeferNotifications())
                {
                    current.IsMarkedForDeletion = false;
                    current.IsNew = false;
                }

                lock (_pendingSettingEditsLock)
                {
                    _pendingSettingEdits.Remove(current);
                }
            }
            finally
            {
                current.PropertyChanged += OnCurrentSettingPropertyChanged;
            }
        }

        await base.OnModeChangedAsync(mode).ConfigureAwait(false);
    }

    protected override async Task<IReadOnlyList<string>> ValidateAsync()
    {
        var messages = new List<string>();
        var setting = CurrentSetting;

        if (setting is null)
        {
            await RunOnDispatcherAsync(() => ApplyValidation(messages)).ConfigureAwait(false);
            return messages.AsReadOnly();
        }

        if (string.IsNullOrWhiteSpace(setting.Key))
        {
            messages.Add(_localizationService.GetString("Module.Admin.Settings.Validation.KeyRequired"));
        }

        if (string.IsNullOrWhiteSpace(setting.Value))
        {
            messages.Add(_localizationService.GetString("Module.Admin.Settings.Validation.ValueRequired"));
        }

        if (string.IsNullOrWhiteSpace(setting.Category))
        {
            messages.Add(_localizationService.GetString("Module.Admin.Settings.Validation.CategoryRequired"));
        }

        if (setting.IsNew)
        {
            var rawKey = setting.Key;
            var normalizedKey = rawKey?.Trim();
            var comparisonKey = string.IsNullOrWhiteSpace(normalizedKey) ? rawKey : normalizedKey;

            if (!string.IsNullOrWhiteSpace(comparisonKey))
            {
                var duplicate = _settingsByCode.ContainsKey(comparisonKey!);

                if (!duplicate && !string.Equals(comparisonKey, rawKey, StringComparison.Ordinal))
                {
                    duplicate = !string.IsNullOrWhiteSpace(rawKey) && _settingsByCode.ContainsKey(rawKey!);
                }

                if (!duplicate)
                {
                    List<EditableSetting> pendingSnapshot;
                    lock (_pendingSettingEditsLock)
                    {
                        pendingSnapshot = _pendingSettingEdits.ToList();
                    }

                    if (pendingSnapshot.Count > 0)
                    {
                        duplicate = pendingSnapshot
                            .Where(edit => !ReferenceEquals(edit, setting))
                            .Any(edit =>
                            {
                                var pendingRawKey = edit.Key;
                                var pendingNormalized = pendingRawKey?.Trim();
                                var pendingComparison = string.IsNullOrWhiteSpace(pendingNormalized)
                                    ? pendingRawKey
                                    : pendingNormalized;

                                return !string.IsNullOrWhiteSpace(pendingComparison)
                                    && string.Equals(pendingComparison, comparisonKey, StringComparison.OrdinalIgnoreCase);
                            });
                    }
                }

                if (duplicate)
                {
                    var displayKey = string.IsNullOrWhiteSpace(normalizedKey) ? comparisonKey! : normalizedKey!;
                    messages.Add(_localizationService.GetString("Module.Admin.Settings.Validation.DuplicateKey", displayKey));
                }
            }
        }

        await RunOnDispatcherAsync(() => ApplyValidation(messages)).ConfigureAwait(false);
        return messages.AsReadOnly();
    }

    protected override async Task<bool> OnSaveAsync()
    {
        var editable = CurrentSetting;
        if (editable is null)
        {
            var message = _localizationService.GetString("Module.Admin.Settings.Validation.SelectionRequired");
            ApplyValidation(new[] { message });
            StatusMessage = message;
            _alerts?.PublishStatus(message, AlertSeverity.Warning);
            return false;
        }

        var existing = ResolveExistingSetting(editable);
        var actorUserId = _authContext.CurrentUser?.Id ?? 0;
        var actorIp = _authContext.CurrentIpAddress ?? string.Empty;
        var actorDevice = _authContext.CurrentDeviceInfo ?? string.Empty;
        var actorSession = _authContext.CurrentSessionId;
        var displayName = GetSettingDisplayName(editable, existing);

        var deleteRequested = editable.IsMarkedForDeletion;

        try
        {
            if (deleteRequested)
            {
                return await DeleteSettingAsync(
                        editable,
                        existing,
                        actorUserId,
                        actorIp,
                        actorDevice,
                        actorSession,
                        displayName)
                    .ConfigureAwait(false);
            }

            return await UpsertSettingAsync(
                    editable,
                    existing,
                    actorUserId,
                    actorIp,
                    actorDevice,
                    actorSession,
                    displayName)
                .ConfigureAwait(false);
        }
        finally
        {
            editable.IsMarkedForDeletion = false;
        }
    }

    private async Task LoadNotificationPreferencesAsync()
    {
        try
        {
            _suppressPreferenceDirty = true;
            var preferences = await _notificationPreferences.ReloadAsync().ConfigureAwait(false);
            StatusBarAlertsEnabled = preferences.ShowStatusBarAlerts;
            ToastAlertsEnabled = preferences.ShowToastAlerts;
            IsNotificationPreferencesDirty = false;
        }
        catch (Exception ex)
        {
            var message = _localizationService.GetString("Module.Admin.NotificationPreferences.StatusLoadFailed");
            StatusMessage = string.Format(CultureInfo.CurrentCulture, message, ex.Message);
            _alerts?.PublishStatus(StatusMessage, AlertSeverity.Warning);
        }
        finally
        {
            _suppressPreferenceDirty = false;
        }
    }

    private async Task SaveNotificationPreferencesAsync()
    {
        try
        {
            IsBusy = true;
            var preferences = new NotificationPreferences
            {
                ShowStatusBarAlerts = StatusBarAlertsEnabled,
                ShowToastAlerts = ToastAlertsEnabled,
            };

            await _notificationPreferences.SaveAsync(preferences).ConfigureAwait(false);

            IsNotificationPreferencesDirty = false;
            var message = _localizationService.GetString("Module.Admin.NotificationPreferences.StatusSaved");
            StatusMessage = message;
            _alerts?.PublishStatus(message, AlertSeverity.Success);
        }
        catch (Exception ex)
        {
            var message = _localizationService.GetString("Module.Admin.NotificationPreferences.StatusSaveFailed");
            StatusMessage = string.Format(CultureInfo.CurrentCulture, message, ex.Message);
            _alerts?.PublishStatus(StatusMessage, AlertSeverity.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanSaveNotificationPreferences()
        => !IsBusy && IsNotificationPreferencesDirty;

    private bool CanRestoreSetting()
        => !IsBusy && SelectedRecord is not null;

    private void OnPreferenceChanged()
    {
        if (_suppressPreferenceDirty)
        {
            return;
        }

        IsNotificationPreferencesDirty = true;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(IsBusy), StringComparison.Ordinal))
        {
            SaveNotificationPreferencesCommand.NotifyCanExecuteChanged();
            RestoreSettingCommand.NotifyCanExecuteChanged();
        }

        if (string.Equals(e.PropertyName, nameof(SelectedRecord), StringComparison.Ordinal))
        {
            RestoreSettingCommand.NotifyCanExecuteChanged();
        }
    }

    private Task OnEditableSettingChangedAsync(EditableSetting setting, string? propertyName)
    {
        if (!ReferenceEquals(_currentSetting, setting))
        {
            return Task.CompletedTask;
        }

        MarkDirty();
        QueueValidation();
        return Task.CompletedTask;
    }

    private void OnCurrentSettingPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not EditableSetting editable || !ReferenceEquals(editable, _currentSetting))
        {
            return;
        }

        if (string.Equals(e.PropertyName, nameof(EditableSetting.IsNew), StringComparison.Ordinal))
        {
            lock (_pendingSettingEditsLock)
            {
                if (editable.IsNew)
                {
                    _pendingSettingEdits.Add(editable);
                }
                else
                {
                    _pendingSettingEdits.Remove(editable);
                }
            }
        }

        MarkDirty();
        QueueValidation();
    }

    private void QueueValidation()
    {
        var next = new CancellationTokenSource();
        CancellationTokenSource? previous;

        lock (_validationLock)
        {
            previous = _validationCts;
            _validationCts = next;
        }

        previous?.Cancel();
        previous?.Dispose();

        _ = RunValidationAsync(next.Token);
    }

    private async Task RunValidationAsync(CancellationToken token)
    {
        try
        {
            var validation = await ValidateAsync().ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            await RunOnDispatcherAsync(() => ApplyValidation(validation)).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Swallow cancellation to allow the most recent validation request to win.
        }
        catch (Exception ex)
        {
            var failure = string.Format(CultureInfo.CurrentCulture, "Validation failed: {0}", ex.Message);
            StatusMessage = failure;
            _alerts?.PublishStatus(failure, AlertSeverity.Error);
        }
    }

    private async Task<bool> DeleteSettingAsync(
        EditableSetting editable,
        Setting? existing,
        int actorUserId,
        string actorIp,
        string actorDevice,
        string actorSession,
        string displayName)
    {
        if (existing is null || editable.IsNew)
        {
            var message = _localizationService.GetString("Module.Admin.Settings.Validation.DeleteRequiresExisting");
            ApplyValidation(new[] { message });
            StatusMessage = message;
            _alerts?.PublishStatus(message, AlertSeverity.Warning);
            return false;
        }

        var recordId = existing.Id;
        ElectronicSignatureDialogResult? signatureResult;
        try
        {
            signatureResult = await _signatureDialog
                .CaptureSignatureAsync(new ElectronicSignatureContext("settings", recordId))
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var failure = string.Format(
                CultureInfo.CurrentCulture,
                _localizationService.GetString("Module.Admin.Settings.Save.Status.SignatureFailed"),
                ex.Message);
            LastSignatureStatus = failure;
            StatusMessage = failure;
            _alerts?.PublishStatus(failure, AlertSeverity.Error);
            return false;
        }

        if (signatureResult is null)
        {
            var cancelled = _localizationService.GetString("Module.Admin.Settings.Save.Status.SignatureCancelled");
            LastSignatureStatus = cancelled;
            StatusMessage = cancelled;
            _alerts?.PublishStatus(cancelled, AlertSeverity.Warning);
            return false;
        }

        if (signatureResult.Signature is null)
        {
            var missing = _localizationService.GetString("Module.Admin.Settings.Save.Status.SignatureMissing");
            LastSignatureStatus = missing;
            StatusMessage = missing;
            _alerts?.PublishStatus(missing, AlertSeverity.Error);
            return false;
        }

        var signature = signatureResult.Signature;
        var reasonDisplay = signatureResult.ReasonDisplay ?? signatureResult.Reason ?? string.Empty;
        var fallbackReason = string.IsNullOrWhiteSpace(reasonDisplay)
            ? _localizationService.GetString("Module.Admin.Settings.Save.Status.SignatureCaptured.Unknown")
            : reasonDisplay;
        var capturedStatus = string.Format(
            CultureInfo.CurrentCulture,
            _localizationService.GetString("Module.Admin.Settings.Save.Status.SignatureCaptured"),
            fallbackReason);

        SignaturePersistenceHelper.ApplyEntityMetadata(
            signatureResult,
            tableName: "settings",
            recordId: recordId,
            metadata: null,
            fallbackSignatureHash: existing.DigitalSignature,
            fallbackMethod: signature.Method,
            fallbackStatus: signature.Status,
            fallbackNote: signature.Note,
            signedAt: signature.SignedAt,
            fallbackDeviceInfo: actorDevice,
            fallbackIpAddress: actorIp,
            fallbackSessionId: actorSession);

        try
        {
            await SignaturePersistenceHelper
                .PersistIfRequiredAsync(_signatureDialog, signatureResult)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var persistFailed = string.Format(
                CultureInfo.CurrentCulture,
                _localizationService.GetString("Module.Admin.Settings.Save.Status.SignaturePersistFailed"),
                ex.Message);
            LastSignatureStatus = persistFailed;
            StatusMessage = persistFailed;
            _alerts?.PublishStatus(persistFailed, AlertSeverity.Error);
            return false;
        }

        LastSignatureStatus = capturedStatus;

        try
        {
            if (existing.Id > 0)
            {
                await Database
                    .DeleteSettingAsync(existing.Id, actorUserId, actorIp, actorDevice)
                    .ConfigureAwait(false);
            }
            else if (!string.IsNullOrWhiteSpace(existing.Key))
            {
                await Database
                    .DeleteSettingByKeyAsync(existing.Key!, actorUserId, actorIp, actorDevice, actorSession)
                    .ConfigureAwait(false);
            }
            else
            {
                var message = _localizationService.GetString("Module.Admin.Settings.Validation.DeleteRequiresExisting");
                ApplyValidation(new[] { message });
                StatusMessage = message;
                _alerts?.PublishStatus(message, AlertSeverity.Error);
                return false;
            }
        }
        catch (Exception ex)
        {
            var failure = string.Format(
                CultureInfo.CurrentCulture,
                _localizationService.GetString("Module.Admin.Settings.Save.Status.DeleteFailed"),
                displayName,
                ex.Message);
            StatusMessage = failure;
            _alerts?.PublishStatus(failure, AlertSeverity.Error);
            return false;
        }

        var success = string.Format(
            CultureInfo.CurrentCulture,
            _localizationService.GetString("Module.Admin.Settings.Save.Status.DeleteSuccess"),
            displayName);
        StatusMessage = success;
        _alerts?.PublishStatus(success, AlertSeverity.Success);
        ResetDirty();

        var details = ComposeAuditDetails(existing, actorUserId, actorIp, actorDevice, actorSession, signatureResult);
        await LogAuditAsync(
                audit => audit.LogEntityAuditAsync("settings", existing.Id, "DELETE", details),
                _localizationService.GetString("Module.Admin.Settings.Save.Status.AuditFailed"))
            .ConfigureAwait(false);

        return true;
    }

    private async Task<bool> UpsertSettingAsync(
        EditableSetting editable,
        Setting? existing,
        int actorUserId,
        string actorIp,
        string actorDevice,
        string actorSession,
        string displayName)
    {
        LastSignatureStatus = null;

        if (!editable.IsNew && existing is null)
        {
            var message = _localizationService.GetString("Module.Admin.Settings.Validation.UpdateRequiresExisting");
            ApplyValidation(new[] { message });
            StatusMessage = message;
            _alerts?.PublishStatus(message, AlertSeverity.Error);
            return false;
        }

        var settingToPersist = existing is not null
            ? CloneSetting(existing)
            : new Setting();

        ApplyEditableToSetting(settingToPersist, editable);

        var update = existing is not null && !editable.IsNew;
        int persistedId;
        try
        {
            persistedId = await Database
                .UpsertSettingAsync(settingToPersist, update, actorUserId, actorIp, actorDevice, actorSession)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var failure = string.Format(
                CultureInfo.CurrentCulture,
                _localizationService.GetString("Module.Admin.Settings.Save.Status.PersistenceFailed"),
                displayName,
                ex.Message);
            StatusMessage = failure;
            _alerts?.PublishStatus(failure, AlertSeverity.Error);
            return false;
        }

        if (!update && persistedId > 0)
        {
            settingToPersist.Id = persistedId;
        }

        editable.IsNew = false;

        var successKey = update
            ? "Module.Admin.Settings.Save.Status.UpdateSuccess"
            : "Module.Admin.Settings.Save.Status.CreateSuccess";
        var success = string.Format(
            CultureInfo.CurrentCulture,
            _localizationService.GetString(successKey),
            displayName);

        StatusMessage = success;
        _alerts?.PublishStatus(success, AlertSeverity.Success);
        ResetDirty();

        var action = update ? "UPDATE" : "CREATE";
        var details = ComposeAuditDetails(settingToPersist, actorUserId, actorIp, actorDevice, actorSession, null);
        await LogAuditAsync(
                audit => audit.LogEntityAuditAsync("settings", settingToPersist.Id, action, details),
                _localizationService.GetString("Module.Admin.Settings.Save.Status.AuditFailed"))
            .ConfigureAwait(false);

        return true;
    }

    private Setting? ResolveExistingSetting(EditableSetting editable)
    {
        var record = SelectedRecord;
        if (record is not null)
        {
            if (_settingsByRecordKey.TryGetValue(record.Key, out var byRecord))
            {
                return byRecord;
            }

            if (!string.IsNullOrWhiteSpace(record.Code)
                && _settingsByCode.TryGetValue(record.Code!, out var byCode))
            {
                return byCode;
            }
        }

        var key = editable.Key;
        if (!string.IsNullOrWhiteSpace(key))
        {
            if (_settingsByCode.TryGetValue(key!, out var byKey))
            {
                return byKey;
            }

            var normalized = key!.Trim();
            if (!string.Equals(normalized, key, StringComparison.Ordinal))
            {
                if (_settingsByCode.TryGetValue(normalized, out var byNormalized))
                {
                    return byNormalized;
                }
            }
        }

        return null;
    }

    private string GetSettingDisplayName(EditableSetting editable, Setting? existing)
    {
        if (!string.IsNullOrWhiteSpace(editable.Key))
        {
            return editable.Key!.Trim();
        }

        if (existing is not null && !string.IsNullOrWhiteSpace(existing.Key))
        {
            return existing.Key!;
        }

        var record = SelectedRecord;
        if (record is not null)
        {
            if (!string.IsNullOrWhiteSpace(record.Title))
            {
                return record.Title!;
            }

            if (!string.IsNullOrWhiteSpace(record.Key))
            {
                return record.Key;
            }
        }

        return _localizationService.GetString("Module.Title.Administration");
    }

    private static void ApplyEditableToSetting(Setting target, EditableSetting source)
    {
        var key = source.Key?.Trim();
        target.Key = string.IsNullOrWhiteSpace(key) ? source.Key ?? string.Empty : key;
        target.Value = source.Value ?? string.Empty;
        target.Category = source.Category ?? string.Empty;
        target.Description = source.Description ?? string.Empty;
    }

    private static Setting CloneSetting(Setting source)
        => new()
        {
            Id = source.Id,
            Key = source.Key,
            Value = source.Value,
            DefaultValue = source.DefaultValue,
            ValueType = source.ValueType,
            MinValue = source.MinValue,
            MaxValue = source.MaxValue,
            Description = source.Description,
            Category = source.Category,
            Subcategory = source.Subcategory,
            IsSensitive = source.IsSensitive,
            IsGlobal = source.IsGlobal,
            UserId = source.UserId,
            RoleId = source.RoleId,
            ApprovedById = source.ApprovedById,
            ApprovedAt = source.ApprovedAt,
            DigitalSignature = source.DigitalSignature,
            Status = source.Status,
            UpdatedAt = source.UpdatedAt,
            UpdatedById = source.UpdatedById,
            Versions = source.Versions is null
                ? new List<SettingVersion>()
                : new List<SettingVersion>(source.Versions),
            AuditLogs = source.AuditLogs is null
                ? new List<SettingAuditLog>()
                : new List<SettingAuditLog>(source.AuditLogs),
            ExpiryDate = source.ExpiryDate,
        };

    private static string ComposeAuditDetails(
        Setting setting,
        int actorUserId,
        string actorIp,
        string actorDevice,
        string? actorSession,
        ElectronicSignatureDialogResult? signatureResult)
    {
        var parts = new List<string?>
        {
            string.IsNullOrWhiteSpace(setting.Key) ? null : $"key={setting.Key}",
            string.IsNullOrWhiteSpace(setting.Value) ? null : $"value={setting.Value}",
            $"user={actorUserId}",
            string.IsNullOrWhiteSpace(actorIp) ? null : $"ip={actorIp}",
            string.IsNullOrWhiteSpace(actorDevice) ? null : $"device={actorDevice}",
            string.IsNullOrWhiteSpace(actorSession) ? null : $"session={actorSession}",
        };

        if (signatureResult is not null)
        {
            var reason = signatureResult.ReasonDisplay ?? signatureResult.ReasonCode;
            if (!string.IsNullOrWhiteSpace(reason))
            {
                parts.Add($"reason={reason}");
            }

            var signature = signatureResult.Signature;
            if (signature is not null)
            {
                if (!string.IsNullOrWhiteSpace(signature.SignatureHash))
                {
                    parts.Add($"signature={signature.SignatureHash}");
                }

                if (!string.IsNullOrWhiteSpace(signature.Method))
                {
                    parts.Add($"method={signature.Method}");
                }

                if (!string.IsNullOrWhiteSpace(signature.Status))
                {
                    parts.Add($"status={signature.Status}");
                }
            }
        }

        return string.Join(", ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private async Task UpdateChoiceCollectionsAsync(IEnumerable<Setting> settings)
    {
        var categories = settings
            .Select(s => s.Category)
            .Where(static c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static c => c, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        if (categories.Count == 0)
        {
            categories.Add("General");
        }

        var statuses = settings
            .Select(s => s.Status)
            .Where(static s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static s => s, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        if (statuses.Count == 0)
        {
            statuses.Add("active");
            statuses.Add("inactive");
        }

        await RunOnDispatcherAsync(() =>
        {
            ReplaceCollection(SettingCategories, categories);
            ReplaceCollection(SettingStatuses, statuses);
        }).ConfigureAwait(false);
    }

    private void UpdateCurrentSetting(ModuleRecord? record)
    {
        if (record is null)
        {
            SetCurrentSetting(null);
            return;
        }

        if (!_settingsByRecordKey.TryGetValue(record.Key, out var setting) &&
            !string.IsNullOrWhiteSpace(record.Code) &&
            !_settingsByCode.TryGetValue(record.Code!, out setting))
        {
            SetCurrentSetting(null);
            return;
        }

        var editable = EditableSetting.FromSetting(setting!, OnEditableSettingChangedAsync);
        SetCurrentSetting(editable);
    }

    private void SetCurrentSetting(EditableSetting? setting)
    {
        if (ReferenceEquals(_currentSetting, setting))
        {
            return;
        }

        if (_currentSetting is not null)
        {
            _currentSetting.PropertyChanged -= OnCurrentSettingPropertyChanged;

            lock (_pendingSettingEditsLock)
            {
                _pendingSettingEdits.Remove(_currentSetting);
            }
        }

        _currentSetting = setting;
        OnPropertyChanged(nameof(CurrentSetting));

        if (_currentSetting is not null)
        {
            _currentSetting.PropertyChanged += OnCurrentSettingPropertyChanged;

            if (_currentSetting.IsNew)
            {
                lock (_pendingSettingEditsLock)
                {
                    _pendingSettingEdits.Add(_currentSetting);
                }
            }
        }
    }

    private static void ReplaceCollection<T>(ObservableCollection<T> target, IEnumerable<T> values)
    {
        target.Clear();
        foreach (var value in values)
        {
            target.Add(value);
        }
    }

    private static Task RunOnDispatcherAsync(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }

        return dispatcher.InvokeAsync(action, DispatcherPriority.DataBind).Task;
    }

    private async Task RestoreSelectedSettingAsync()
    {
        if (SelectedRecord is null)
        {
            var noSelection = _localizationService.GetString("Module.Admin.Restore.Status.NoSelection");
            StatusMessage = noSelection;
            _alerts?.PublishStatus(noSelection, AlertSeverity.Warning);
            return;
        }

        if (!_settingsByRecordKey.TryGetValue(SelectedRecord.Key, out var setting) &&
            !string.IsNullOrWhiteSpace(SelectedRecord.Code) &&
            !_settingsByCode.TryGetValue(SelectedRecord.Code!, out setting))
        {
            var missing = _localizationService.GetString("Module.Admin.Restore.Status.NotFound");
            StatusMessage = missing;
            _alerts?.PublishStatus(missing, AlertSeverity.Error);
            return;
        }

        var resolvedSetting = setting!;
        var settingKey = !string.IsNullOrWhiteSpace(resolvedSetting.Key)
            ? resolvedSetting.Key!
            : SelectedRecord.Code ?? string.Empty;
        var settingDisplayName = string.IsNullOrWhiteSpace(SelectedRecord.Title)
            ? (string.IsNullOrWhiteSpace(settingKey) ? SelectedRecord.Key : settingKey)
            : SelectedRecord.Title;

        var confirmTitle = _localizationService.GetString("Module.Admin.Restore.Confirm.Title");
        var confirmMessage = string.Format(
            CultureInfo.CurrentCulture,
            _localizationService.GetString("Module.Admin.Restore.Confirm.Message"),
            settingDisplayName);
        var confirmAccept = _localizationService.GetString("Module.Admin.Restore.Confirm.Accept");
        var confirmCancel = _localizationService.GetString("Module.Admin.Restore.Confirm.Cancel");

        bool confirmed;
        try
        {
            confirmed = await _dialogService
                .ShowConfirmationAsync(confirmTitle, confirmMessage, confirmAccept, confirmCancel)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var dialogFailed = string.Format(
                CultureInfo.CurrentCulture,
                _localizationService.GetString("Module.Admin.Restore.Status.Failure"),
                settingDisplayName,
                ex.Message);
            StatusMessage = dialogFailed;
            _alerts?.PublishStatus(dialogFailed, AlertSeverity.Error);
            return;
        }

        if (!confirmed)
        {
            var declined = _localizationService.GetString("Module.Admin.Restore.Status.ConfirmationDeclined");
            StatusMessage = string.Format(CultureInfo.CurrentCulture, declined, settingDisplayName);
            _alerts?.PublishStatus(StatusMessage, AlertSeverity.Info);
            return;
        }

        ElectronicSignatureDialogResult? signatureResult;
        try
        {
            var recordId = setting?.Id ?? 0;
            signatureResult = await _signatureDialog
                .CaptureSignatureAsync(new ElectronicSignatureContext("settings", recordId))
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var failure = string.Format(
                CultureInfo.CurrentCulture,
                _localizationService.GetString("Module.Admin.Restore.Status.SignatureFailed"),
                ex.Message);
            LastSignatureStatus = failure;
            StatusMessage = failure;
            _alerts?.PublishStatus(failure, AlertSeverity.Error);
            return;
        }

        if (signatureResult is null)
        {
            var cancelled = _localizationService.GetString("Module.Admin.Restore.Status.SignatureCancelled");
            LastSignatureStatus = cancelled;
            StatusMessage = cancelled;
            _alerts?.PublishStatus(cancelled, AlertSeverity.Warning);
            return;
        }

        if (signatureResult.Signature is null)
        {
            var missingSignature = _localizationService.GetString("Module.Admin.Restore.Status.SignatureMissing");
            LastSignatureStatus = missingSignature;
            StatusMessage = missingSignature;
            _alerts?.PublishStatus(missingSignature, AlertSeverity.Error);
            return;
        }

        var actorUserId = _authContext.CurrentUser?.Id ?? 0;
        var actorIp = _authContext.CurrentIpAddress ?? string.Empty;
        var actorDevice = _authContext.CurrentDeviceInfo ?? string.Empty;
        var actorSession = _authContext.CurrentSessionId;

        var restoreSucceeded = false;
        try
        {
            IsBusy = true;
            await Database.RollbackSettingByKeyAsync(
                    settingKey,
                    actorUserId,
                    actorIp,
                    actorDevice,
                    actorSession)
                .ConfigureAwait(false);

            try
            {
                await SignaturePersistenceHelper
                        .PersistIfRequiredAsync(_signatureDialog, signatureResult)
                        .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var persistFailed = string.Format(
                    CultureInfo.CurrentCulture,
                    _localizationService.GetString("Module.Admin.Restore.Status.SignaturePersistFailed"),
                    ex.Message);
                LastSignatureStatus = persistFailed;
                StatusMessage = persistFailed;
                _alerts?.PublishStatus(persistFailed, AlertSeverity.Error);
                return;
            }

            var reasonDisplay = signatureResult.ReasonDisplay ?? signatureResult.Reason ?? string.Empty;
            var fallbackReason = string.IsNullOrWhiteSpace(reasonDisplay)
                ? _localizationService.GetString("Module.Admin.Restore.Status.SignatureCaptured.Unknown")
                : reasonDisplay;
            var signatureCaptured = string.Format(
                CultureInfo.CurrentCulture,
                _localizationService.GetString("Module.Admin.Restore.Status.SignatureCaptured"),
                fallbackReason);
            LastSignatureStatus = signatureCaptured;

            var success = string.Format(
                CultureInfo.CurrentCulture,
                _localizationService.GetString("Module.Admin.Restore.Status.Success"),
                settingDisplayName);
            StatusMessage = success;
            _alerts?.PublishStatus(success, AlertSeverity.Success);

            var signature = signatureResult.Signature;
            static string? FormatPart(string label, string? value)
                => string.IsNullOrWhiteSpace(value) ? null : $"{label}={value}";

            var detailsParts = new[]
            {
                FormatPart("key", settingKey),
                FormatPart("reason", reasonDisplay),
                FormatPart("signature", signature?.SignatureHash),
                FormatPart("method", signature?.Method),
                FormatPart("status", signature?.Status),
                FormatPart("ip", actorIp),
                FormatPart("device", actorDevice),
                FormatPart("session", actorSession)
            }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Cast<string>();

            var details = string.Join(", ", detailsParts);

            await LogAuditAsync(
                audit => audit.LogEntityAuditAsync("settings", resolvedSetting.Id, "ROLLBACK", details),
                _localizationService.GetString("Module.Admin.Restore.Status.AuditFailed"))
                .ConfigureAwait(false);

            restoreSucceeded = true;
        }
        catch (Exception ex)
        {
            var failure = string.Format(
                CultureInfo.CurrentCulture,
                _localizationService.GetString("Module.Admin.Restore.Status.Failure"),
                settingDisplayName,
                ex.Message);
            StatusMessage = failure;
            _alerts?.PublishStatus(failure, AlertSeverity.Error);
        }
        finally
        {
            IsBusy = false;
            RestoreSettingCommand.NotifyCanExecuteChanged();
        }

        if (restoreSucceeded)
        {
            await RefreshCommand.ExecuteAsync(null).ConfigureAwait(false);
        }
    }
}
