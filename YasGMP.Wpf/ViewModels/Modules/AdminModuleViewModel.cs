using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
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

    private bool _suppressPreferenceDirty;
    private bool _statusBarAlertsEnabled;
    private bool _toastAlertsEnabled;
    private bool _isNotificationPreferencesDirty;
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
        var refreshRecords = false;
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

            refreshRecords = true;

            var persistenceFailed = false;
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
                persistenceFailed = true;
            }

            if (!persistenceFailed)
            {
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

        if (refreshRecords || restoreSucceeded)
        {
            await RefreshCommand.ExecuteAsync(null).ConfigureAwait(false);
        }
    }
}
