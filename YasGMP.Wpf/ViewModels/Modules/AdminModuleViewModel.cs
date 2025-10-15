using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Common;
using YasGMP.Wpf.Services;

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
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation,
        ILocalizationService localization,
        INotificationPreferenceService notificationPreferences)
        : base(ModuleKey, localization.GetString("Module.Title.Administration"), databaseService, localization, cflDialogService, shellInteraction, navigation, auditService)
    {
        _notificationPreferences = notificationPreferences ?? throw new ArgumentNullException(nameof(notificationPreferences));
        _localizationService = localization ?? throw new ArgumentNullException(nameof(localization));
        _alerts = ServiceLocator.GetService<IShellAlertService>();

        SaveNotificationPreferencesCommand = new AsyncRelayCommand(SaveNotificationPreferencesAsync, CanSaveNotificationPreferences);
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

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        await LoadNotificationPreferencesAsync().ConfigureAwait(false);
        var settings = await Database.GetAllSettingsFullAsync().ConfigureAwait(false);
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
        var recordTitle = setting.Name ?? setting.Key ?? "Setting";

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
        }
    }
}
