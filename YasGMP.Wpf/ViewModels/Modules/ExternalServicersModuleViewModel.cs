using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.ViewModels.Modules;
/// <summary>
/// Represents the external servicers module view model value.
/// </summary>

public sealed partial class ExternalServicersModuleViewModel : ModuleDocumentViewModel
{
    /// <summary>
    /// Represents the module key value.
    /// </summary>
    public const string ModuleKey = "ExternalServicers";

    private readonly DatabaseService _databaseService;
    private readonly IExternalServicerCrudService _servicerService;
    private readonly IAuthContext _authContext;
    private readonly IElectronicSignatureDialogService _signatureDialog;
    private readonly ILocalizationService _localization;
    private readonly IModuleNavigationService _navigation;

    private ExternalServicer? _loadedServicer;
    private ExternalServicerEditor? _snapshot;
    private bool _suppressDirtyNotifications;
    private int? _lastSavedServicerId;
    private List<ContractorIntervention> _interventions = new();
    /// <summary>
    /// Initializes a new instance of the ExternalServicersModuleViewModel class.
    /// </summary>

    public ExternalServicersModuleViewModel(
        DatabaseService databaseService,
        IExternalServicerCrudService servicerService,
        IAuthContext authContext,
        IElectronicSignatureDialogService signatureDialog,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation,
        ILocalizationService localization)
        : base(ModuleKey, localization.GetString("Module.Title.ExternalServicers"), localization, cflDialogService, shellInteraction, navigation)
    {
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        _servicerService = servicerService ?? throw new ArgumentNullException(nameof(servicerService));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _signatureDialog = signatureDialog ?? throw new ArgumentNullException(nameof(signatureDialog));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));

        Editor = ExternalServicerEditor.CreateEmpty();
        StatusOptions = new ReadOnlyCollection<string>(new[]
        {
            _localization.GetString("Module.ExternalServicers.Status.Active"),
            _localization.GetString("Module.ExternalServicers.Status.Pending"),
            _localization.GetString("Module.ExternalServicers.Status.Suspended"),
            _localization.GetString("Module.ExternalServicers.Status.Expired"),
            _localization.GetString("Module.ExternalServicers.Status.OnHold"),
            _localization.GetString("Module.ExternalServicers.Status.Inactive")
        });
        ServiceTypeOptions = new ReadOnlyCollection<string>(new[]
        {
            _localization.GetString("Module.ExternalServicers.ServiceType.Calibration"),
            _localization.GetString("Module.ExternalServicers.ServiceType.Maintenance"),
            _localization.GetString("Module.ExternalServicers.ServiceType.Validation"),
            _localization.GetString("Module.ExternalServicers.ServiceType.Laboratory"),
            _localization.GetString("Module.ExternalServicers.ServiceType.Audit"),
            _localization.GetString("Module.ExternalServicers.ServiceType.ItServices"),
            _localization.GetString("Module.ExternalServicers.ServiceType.Logistics")
        });

        OversightMetrics = new ObservableCollection<ContractorOversightMetric>();
        InterventionTimeline = new ObservableCollection<ContractorInterventionTimelineItem>();
        OversightAnalytics = new ObservableCollection<ContractorOversightAnalyticsRow>();

        RefreshOversightCommand = new AsyncRelayCommand(ExecuteRefreshOversightAsync, () => !IsOversightBusy);
        DrillIntoOversightCommand = new RelayCommand(DrillIntoOversight, CanDrillIntoOversight);
    }

    [ObservableProperty]
    private ExternalServicerEditor _editor;

    [ObservableProperty]
    private bool _isEditorEnabled;

    [ObservableProperty]
    private IReadOnlyList<string> _statusOptions;

    [ObservableProperty]
    private IReadOnlyList<string> _serviceTypeOptions;

    [ObservableProperty]
    private ObservableCollection<ContractorOversightMetric> _oversightMetrics;

    [ObservableProperty]
    private ObservableCollection<ContractorInterventionTimelineItem> _interventionTimeline;

    [ObservableProperty]
    private ObservableCollection<ContractorOversightAnalyticsRow> _oversightAnalytics;

    [ObservableProperty]
    private bool _isOversightBusy;

    public IAsyncRelayCommand RefreshOversightCommand { get; }

    public IRelayCommand DrillIntoOversightCommand { get; }

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var servicers = await _servicerService.GetAllAsync().ConfigureAwait(false);
        var ordered = servicers
            .OrderBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(s => s.Id)
            .Select(ToRecord)
            .ToList();

        await UpdateOversightMetricsAsync(reloadData: true).ConfigureAwait(false);

        if (_lastSavedServicerId.HasValue)
        {
            var savedKey = _lastSavedServicerId.Value.ToString(CultureInfo.InvariantCulture);
            var index = ordered.FindIndex(r => r.Key == savedKey);
            if (index > 0)
            {
                var match = ordered[index];
                ordered.RemoveAt(index);
                ordered.Insert(0, match);
            }

            _lastSavedServicerId = null;
        }

        return ordered;
    }

    private List<ContractorIntervention> FilterInterventionsForSelection(List<ContractorIntervention> interventions)
    {
        if (SelectedRecord is null || !int.TryParse(SelectedRecord.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
        {
            return interventions;
        }

        return interventions.Where(i => i.ContractorId == id).ToList();
    }

    private static void ReplaceCollection<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();
        foreach (var item in items)
        {
            target.Add(item);
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

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
    {
        var sample = new[]
        {
            new ExternalServicer
            {
                Id = 1,
                Name = "Contoso Calibration",
                Code = "EXT-001",
                Type = ServiceTypeOptions[0],
                Status = StatusOptions[0],
                ContactPerson = "Ivana Horvat",
                Email = "calibration@contoso.example",
                Phone = "+385 91 111 222",
                Comment = "ISO 17025 accredited laboratory"
            },
            new ExternalServicer
            {
                Id = 2,
                Name = "Globex Maintenance",
                Code = "EXT-002",
                Type = ServiceTypeOptions[1],
                Status = StatusOptions[2],
                ContactPerson = "Marko Barić",
                Email = "support@globex.example",
                Phone = "+385 91 555 666",
                Comment = "Pending contract renewal"
            }
        };

        return sample.Select(ToRecord).ToList();
    }

    protected override async Task OnActivatedAsync(object? parameter)
    {
        if (parameter is null)
        {
            return;
        }

        string key = parameter switch
        {
            int id => id.ToString(CultureInfo.InvariantCulture),
            string text => text,
            _ => parameter.ToString() ?? string.Empty
        };

        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        var match = Records.FirstOrDefault(r =>
            string.Equals(r.Key, key, StringComparison.OrdinalIgnoreCase)
            || string.Equals(r.Title, key, StringComparison.OrdinalIgnoreCase)
            || string.Equals(r.Code ?? string.Empty, key, StringComparison.OrdinalIgnoreCase));

        if (match is not null)
        {
            SelectedRecord = match;
        }
    }

    protected override async Task OnRecordSelectedAsync(ModuleRecord? record)
    {
        if (record is null)
        {
            _loadedServicer = null;
            SetEditor(ExternalServicerEditor.CreateEmpty());
            await UpdateOversightMetricsAsync(reloadData: false).ConfigureAwait(false);
            DrillIntoOversightCommand.NotifyCanExecuteChanged();
            return;
        }

        if (IsInEditMode)
        {
            DrillIntoOversightCommand.NotifyCanExecuteChanged();
            return;
        }

        if (!int.TryParse(record.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
        {
            await UpdateOversightMetricsAsync(reloadData: false).ConfigureAwait(false);
            DrillIntoOversightCommand.NotifyCanExecuteChanged();
            return;
        }

        var entity = await _servicerService.TryGetByIdAsync(id).ConfigureAwait(false);
        if (entity is null)
        {
            StatusMessage = $"External servicer #{id} could not be located.";
            await UpdateOversightMetricsAsync(reloadData: false).ConfigureAwait(false);
            DrillIntoOversightCommand.NotifyCanExecuteChanged();
            return;
        }

        _loadedServicer = entity;
        LoadEditor(entity);
        await UpdateOversightMetricsAsync(reloadData: false).ConfigureAwait(false);
        DrillIntoOversightCommand.NotifyCanExecuteChanged();
    }

    protected override Task OnModeChangedAsync(FormMode mode)
    {
        IsEditorEnabled = mode is FormMode.Add or FormMode.Update;

        switch (mode)
        {
            case FormMode.Add:
                _snapshot = null;
                _loadedServicer = null;
                SetEditor(ExternalServicerEditor.CreateForNew());
                break;
            case FormMode.Update:
                if (_loadedServicer is not null)
                {
                    _snapshot = Editor.Clone();
                }
                break;
            case FormMode.View:
            case FormMode.Find:
                _snapshot = null;
                break;
        }

        return Task.CompletedTask;
    }

    partial void OnIsOversightBusyChanged(bool value)
    {
        RefreshOversightCommand.NotifyCanExecuteChanged();
        DrillIntoOversightCommand.NotifyCanExecuteChanged();
    }

    protected override async Task<IReadOnlyList<string>> ValidateAsync()
    {
        var errors = new List<string>();

        try
        {
            var draft = Editor.ToServicer(_loadedServicer);
            _servicerService.Validate(draft);
        }
        catch (Exception ex)
        {
            errors.Add(ex.Message);
        }

        if (!string.IsNullOrWhiteSpace(Editor.Email)
            && !Editor.Email.Contains('@', StringComparison.Ordinal))
        {
            errors.Add("Email address must contain '@'.");
        }

        return await Task.FromResult<IReadOnlyList<string>>(errors).ConfigureAwait(false);
    }

    protected override async Task<bool> OnSaveAsync()
    {
        if (Mode == FormMode.Update && _loadedServicer is null)
        {
            StatusMessage = "Select an external servicer before saving.";
            return false;
        }

        var servicer = Editor.ToServicer(_loadedServicer);
        servicer.Status = _servicerService.NormalizeStatus(servicer.Status);

        var recordId = Mode == FormMode.Update ? _loadedServicer!.Id : 0;
        ElectronicSignatureDialogResult? signatureResult;
        try
        {
            signatureResult = await _signatureDialog
                .CaptureSignatureAsync(new ElectronicSignatureContext("external_contractors", recordId))
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

        servicer.DigitalSignature = signatureResult.Signature.SignatureHash ?? string.Empty;
        servicer.LastModified = DateTime.UtcNow;
        servicer.LastModifiedById = _authContext.CurrentUser?.Id ?? servicer.LastModifiedById;
        servicer.SourceIp = _authContext.CurrentIpAddress ?? servicer.SourceIp ?? string.Empty;

        var context = ExternalServicerCrudContext.Create(
            _authContext.CurrentUser?.Id ?? 0,
            _authContext.CurrentIpAddress,
            _authContext.CurrentDeviceInfo,
            _authContext.CurrentSessionId,
            signatureResult);

        ExternalServicer adapterResult;
        CrudSaveResult saveResult;
        try
        {
            if (Mode == FormMode.Add)
            {
                saveResult = await _servicerService.CreateAsync(servicer, context).ConfigureAwait(false);
                servicer.Id = saveResult.Id;
                adapterResult = servicer;
            }
            else if (Mode == FormMode.Update)
            {
                servicer.Id = _loadedServicer.Id;
                saveResult = await _servicerService.UpdateAsync(servicer, context).ConfigureAwait(false);
                adapterResult = servicer;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to persist external servicer: {ex.Message}", ex);
        }

        _loadedServicer = servicer;
        _lastSavedServicerId = servicer.Id;
        LoadEditor(servicer);
        UpdateAttachmentCommandState();

        SignaturePersistenceHelper.ApplyEntityMetadata(
            signatureResult,
            tableName: "external_contractors",
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

    protected override void OnCancel()
    {
        if (Mode == FormMode.Add)
        {
            if (_loadedServicer is not null)
            {
                LoadEditor(_loadedServicer);
            }
            else
            {
                SetEditor(ExternalServicerEditor.CreateEmpty());
            }
        }
        else if (Mode == FormMode.Update && _snapshot is not null)
        {
            SetEditor(_snapshot.Clone());
        }
    }

    protected override async Task<CflRequest?> CreateCflRequestAsync()
    {
        var servicers = await _servicerService.GetAllAsync().ConfigureAwait(false);
        var items = servicers.Select(servicer =>
        {
            var key = servicer.Id.ToString(CultureInfo.InvariantCulture);
            var descriptionParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(servicer.Type))
            {
                descriptionParts.Add(servicer.Type!);
            }

            if (!string.IsNullOrWhiteSpace(servicer.Status))
            {
                descriptionParts.Add(servicer.Status!);
            }

            if (!string.IsNullOrWhiteSpace(servicer.ContactPerson))
            {
                descriptionParts.Add(servicer.ContactPerson!);
            }

            var description = descriptionParts.Count > 0 ? string.Join(" • ", descriptionParts) : null;
            return new CflItem(key, servicer.Name, description);
        }).ToList();

        return new CflRequest("Select External Servicer", items);
    }

    private Task ExecuteRefreshOversightAsync()
        => UpdateOversightMetricsAsync(reloadData: true);

    private bool CanDrillIntoOversight()
        => !IsOversightBusy && SelectedRecord is not null;

    private void DrillIntoOversight()
    {
        if (SelectedRecord is null)
        {
            StatusMessage = _localization.GetString("Module.ExternalServicers.Oversight.Status.SelectServicer");
            return;
        }

        try
        {
            var document = _navigation.OpenModule(SchedulingModuleViewModel.ModuleKey, SelectedRecord.Key);
            _navigation.Activate(document);
            StatusMessage = string.Format(
                CultureInfo.CurrentCulture,
                _localization.GetString("Module.ExternalServicers.Oversight.Status.DrilledIn"),
                SelectedRecord.Title);
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(
                CultureInfo.CurrentCulture,
                _localization.GetString("Module.ExternalServicers.Oversight.Status.DrillFailed"),
                ex.Message);
        }
    }

    private async Task UpdateOversightMetricsAsync(bool reloadData)
    {
        try
        {
            if (IsOversightBusy)
            {
                return;
            }

            IsOversightBusy = true;

            if (reloadData || _interventions.Count == 0)
            {
                var data = await _databaseService
                    .GetAllContractorInterventionsAsync()
                    .ConfigureAwait(false);
                _interventions = data ?? new List<ContractorIntervention>();
            }

            var scoped = FilterInterventionsForSelection(_interventions);

            var now = DateTime.UtcNow;
            var currentStart = now.AddDays(-30);
            var previousStart = now.AddDays(-60);
            var currentWindow = scoped.Where(i => i.InterventionDate >= currentStart).ToList();
            var previousWindow = scoped
                .Where(i => i.InterventionDate >= previousStart && i.InterventionDate < currentStart)
                .ToList();

            var metrics = BuildMetrics(scoped, currentWindow.Count, previousWindow.Count);
            var timeline = BuildTimeline(scoped);
            var analytics = BuildAnalytics(scoped);

            await RunOnDispatcherAsync(() =>
            {
                ReplaceCollection(OversightMetrics, metrics);
                ReplaceCollection(InterventionTimeline, timeline);
                ReplaceCollection(OversightAnalytics, analytics);
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(
                CultureInfo.CurrentCulture,
                _localization.GetString("Module.ExternalServicers.Oversight.Status.RefreshFailed"),
                ex.Message);
        }
        finally
        {
            IsOversightBusy = false;
        }
    }

    private IReadOnlyList<ContractorOversightMetric> BuildMetrics(
        IReadOnlyCollection<ContractorIntervention> scoped,
        int currentWindowCount,
        int previousWindowCount)
    {
        var culture = CultureInfo.CurrentCulture;
        var metrics = new List<ContractorOversightMetric>
        {
            new(
                _localization.GetString("Module.ExternalServicers.Oversight.Metric.Total"),
                scoped.Count.ToString("N0", culture),
                BuildTrendText(currentWindowCount, previousWindowCount),
                Brushes.SlateBlue)
        };

        var openStatuses = scoped
            .Count(i => !string.IsNullOrWhiteSpace(i.Status) && !IsClosedStatus(i.Status));
        metrics.Add(new ContractorOversightMetric(
            _localization.GetString("Module.ExternalServicers.Oversight.Metric.Open"),
            openStatuses.ToString("N0", culture),
            _localization.GetString(openStatuses == 0
                ? "Module.ExternalServicers.Oversight.Trend.NoBacklog"
                : "Module.ExternalServicers.Oversight.Trend.Backlog"),
            Brushes.Teal));

        var upcomingThreshold = DateTime.UtcNow.AddDays(30);
        var upcoming = scoped.Count(i => i.EndDate is { } end && end <= upcomingThreshold && end >= DateTime.UtcNow);
        metrics.Add(new ContractorOversightMetric(
            _localization.GetString("Module.ExternalServicers.Oversight.Metric.UpcomingExpirations"),
            upcoming.ToString("N0", culture),
            upcoming > 0
                ? _localization.GetString("Module.ExternalServicers.Oversight.Trend.ActionRequired")
                : _localization.GetString("Module.ExternalServicers.Oversight.Trend.OnTrack"),
            Brushes.Orange));

        var mttr = CalculateAverageDuration(scoped);
        metrics.Add(new ContractorOversightMetric(
            _localization.GetString("Module.ExternalServicers.Oversight.Metric.AverageMttr"),
            mttr is null
                ? _localization.GetString("Module.ExternalServicers.Oversight.Value.NotAvailable")
                : FormatDuration(mttr.Value, culture),
            mttr is null
                ? _localization.GetString("Module.ExternalServicers.Oversight.Trend.InsufficientData")
                : _localization.GetString("Module.ExternalServicers.Oversight.Trend.Mttr"),
            Brushes.MediumPurple));

        return metrics;
    }

    private IReadOnlyList<ContractorOversightAnalyticsRow> BuildAnalytics(IReadOnlyCollection<ContractorIntervention> scoped)
    {
        var culture = CultureInfo.CurrentCulture;
        var analytics = new List<ContractorOversightAnalyticsRow>();

        if (scoped.Count == 0)
        {
            analytics.Add(new ContractorOversightAnalyticsRow(
                _localization.GetString("Module.ExternalServicers.Oversight.Analytics.Metric.NoData"),
                _localization.GetString("Module.ExternalServicers.Oversight.Value.NotAvailable"),
                "-",
                _localization.GetString("Module.ExternalServicers.Oversight.Analytics.Status.Waiting")));
            return analytics;
        }

        var statusGroups = scoped
            .Where(i => !string.IsNullOrWhiteSpace(i.Status))
            .GroupBy(i => i.Status!.Trim())
            .Select(g => $"{g.Key}: {g.Count():N0}")
            .ToList();

        analytics.Add(new ContractorOversightAnalyticsRow(
            _localization.GetString("Module.ExternalServicers.Oversight.Analytics.Metric.StatusDistribution"),
            statusGroups.Count == 0
                ? _localization.GetString("Module.ExternalServicers.Oversight.Value.NotAvailable")
                : string.Join(" | ", statusGroups),
            _localization.GetString("Module.ExternalServicers.Oversight.Analytics.Target.Balanced"),
            _localization.GetString("Module.ExternalServicers.Oversight.Analytics.Status.Info")));

        var complianceCount = scoped.Count(i => i.GmpCompliance);
        var complianceRate = scoped.Count == 0 ? 0 : (double)complianceCount / scoped.Count;
        analytics.Add(new ContractorOversightAnalyticsRow(
            _localization.GetString("Module.ExternalServicers.Oversight.Analytics.Metric.Compliance"),
            complianceRate.ToString("P0", culture),
            _localization.GetString("Module.ExternalServicers.Oversight.Analytics.Target.Compliance"),
            complianceRate >= 0.9
                ? _localization.GetString("Module.ExternalServicers.Oversight.Analytics.Status.OnTrack")
                : _localization.GetString("Module.ExternalServicers.Oversight.Analytics.Status.AtRisk")));

        var activeAges = scoped
            .Where(i => !string.IsNullOrWhiteSpace(i.Status) && !IsClosedStatus(i.Status))
            .Select(i => (DateTime.UtcNow - i.InterventionDate).TotalDays)
            .Where(days => days >= 0)
            .ToList();

        var averageAge = activeAges.Count == 0 ? 0 : activeAges.Average();
        analytics.Add(new ContractorOversightAnalyticsRow(
            _localization.GetString("Module.ExternalServicers.Oversight.Analytics.Metric.BacklogAge"),
            averageAge == 0
                ? _localization.GetString("Module.ExternalServicers.Oversight.Value.NotAvailable")
                : string.Format(culture, "{0:F1} d", averageAge),
            _localization.GetString("Module.ExternalServicers.Oversight.Analytics.Target.Age"),
            averageAge <= 14
                ? _localization.GetString("Module.ExternalServicers.Oversight.Analytics.Status.OnTrack")
                : _localization.GetString("Module.ExternalServicers.Oversight.Analytics.Status.AtRisk")));

        var mttr = CalculateAverageDuration(scoped);
        analytics.Add(new ContractorOversightAnalyticsRow(
            _localization.GetString("Module.ExternalServicers.Oversight.Analytics.Metric.Mttr"),
            mttr is null
                ? _localization.GetString("Module.ExternalServicers.Oversight.Value.NotAvailable")
                : FormatDuration(mttr.Value, culture),
            _localization.GetString("Module.ExternalServicers.Oversight.Analytics.Target.Mttr"),
            mttr is not null && mttr.Value.TotalHours <= 72
                ? _localization.GetString("Module.ExternalServicers.Oversight.Analytics.Status.OnTrack")
                : _localization.GetString("Module.ExternalServicers.Oversight.Analytics.Status.AtRisk")));

        return analytics;
    }

    private static string BuildTrendText(int currentCount, int previousCount)
    {
        if (previousCount == 0 && currentCount == 0)
        {
            return "▬ 0";
        }

        if (previousCount == 0)
        {
            return $"▲ {currentCount}";
        }

        var delta = currentCount - previousCount;
        if (delta == 0)
        {
            return "▬ 0";
        }

        var symbol = delta > 0 ? "▲" : "▼";
        return $"{symbol} {Math.Abs(delta)}";
    }

    private static bool IsClosedStatus(string status)
    {
        var normalized = status.Trim().ToLowerInvariant();
        return normalized is "closed" or "completed" or "done" or "resolved" or "archived";
    }

    private static TimeSpan? CalculateAverageDuration(IEnumerable<ContractorIntervention> scoped)
    {
        var durations = scoped
            .Where(i => i.StartDate.HasValue && i.EndDate.HasValue && i.EndDate >= i.StartDate)
            .Select(i => i.EndDate!.Value - i.StartDate!.Value)
            .Where(duration => duration.TotalMinutes >= 0)
            .ToList();

        if (durations.Count == 0)
        {
            return null;
        }

        var averageTicks = Convert.ToInt64(durations.Average(d => d.Ticks));
        return new TimeSpan(averageTicks);
    }

    private static string FormatDuration(TimeSpan duration, CultureInfo culture)
    {
        if (duration.TotalHours < 24)
        {
            return string.Format(culture, "{0:F1} h", duration.TotalHours);
        }

        return string.Format(culture, "{0:F1} d", duration.TotalDays);
    }

    private IReadOnlyList<ContractorInterventionTimelineItem> BuildTimeline(IEnumerable<ContractorIntervention> scoped)
        => scoped
            .OrderByDescending(i => i.InterventionDate)
            .Take(15)
            .Select(i => new ContractorInterventionTimelineItem(
                i.InterventionDate,
                string.Format(
                    CultureInfo.CurrentCulture,
                    _localization.GetString("Module.ExternalServicers.Oversight.Timeline.Summary"),
                    string.IsNullOrWhiteSpace(i.InterventionType) ? _localization.GetString("Module.ExternalServicers.Oversight.Value.UnknownType") : i.InterventionType,
                    string.IsNullOrWhiteSpace(i.Status) ? _localization.GetString("Module.ExternalServicers.Oversight.Value.UnknownStatus") : i.Status),
                string.Format(
                    CultureInfo.CurrentCulture,
                    _localization.GetString("Module.ExternalServicers.Oversight.Timeline.Detail"),
                    string.IsNullOrWhiteSpace(i.Reason) ? _localization.GetString("Module.ExternalServicers.Oversight.Value.NoReason") : i.Reason,
                    string.IsNullOrWhiteSpace(i.Result) ? _localization.GetString("Module.ExternalServicers.Oversight.Value.NoResult") : i.Result)))
            .ToList();

    protected override Task OnCflSelectionAsync(CflResult result)
    {
        var match = Records.FirstOrDefault(r => r.Key == result.Selected.Key);
        if (match is not null)
        {
            SelectedRecord = match;
            SearchText = match.Title;
        }
        else
        {
            SearchText = result.Selected.Label;
        }

        StatusMessage = $"Filtered external servicers by \"{SearchText}\".";
        return Task.CompletedTask;
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

    partial void OnEditorChanging(ExternalServicerEditor value)
    {
        if (value is null)
        {
            return;
        }

        value.PropertyChanged -= OnEditorPropertyChanged;
    }

    partial void OnEditorChanged(ExternalServicerEditor value)
    {
        if (value is null)
        {
            return;
        }

        value.PropertyChanged += OnEditorPropertyChanged;
    }

    private void OnEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_suppressDirtyNotifications)
        {
            return;
        }

        if (IsEditorEnabled)
        {
            MarkDirty();
        }
    }

    private void LoadEditor(ExternalServicer servicer)
    {
        _suppressDirtyNotifications = true;
        Editor = ExternalServicerEditor.FromServicer(servicer);
        _suppressDirtyNotifications = false;
        ResetDirty();
    }

    private void SetEditor(ExternalServicerEditor editor)
    {
        _suppressDirtyNotifications = true;
        Editor = editor;
        _suppressDirtyNotifications = false;
        ResetDirty();
    }

    private static ModuleRecord ToRecord(ExternalServicer servicer)
    {
        var inspector = new List<InspectorField>
        {
            new("Type", string.IsNullOrWhiteSpace(servicer.Type) ? "-" : servicer.Type!),
            new("Contact", string.IsNullOrWhiteSpace(servicer.ContactPerson) ? "-" : servicer.ContactPerson!),
            new("Email", string.IsNullOrWhiteSpace(servicer.Email) ? "-" : servicer.Email!),
            new("Phone", string.IsNullOrWhiteSpace(servicer.Phone) ? "-" : servicer.Phone!)
        };

        var relatedParameter = !string.IsNullOrWhiteSpace(servicer.VatOrId)
            ? servicer.VatOrId
            : servicer.Name;

        return new ModuleRecord(
            servicer.Id.ToString(CultureInfo.InvariantCulture),
            servicer.Name,
            servicer.Code,
            servicer.Status,
            servicer.Comment,
            inspector,
            SuppliersModuleViewModel.ModuleKey,
            relatedParameter);
    }
}

public sealed class ContractorOversightMetric
{
    public ContractorOversightMetric(string title, string formattedValue, string trendText, Brush accentBrush)
    {
        Title = title;
        FormattedValue = formattedValue;
        TrendText = trendText;
        AccentBrush = accentBrush;
    }

    public string Title { get; }

    public string FormattedValue { get; }

    public string TrendText { get; }

    public Brush AccentBrush { get; }
}

public sealed class ContractorInterventionTimelineItem
{
    public ContractorInterventionTimelineItem(DateTime timestamp, string summary, string details)
    {
        Timestamp = timestamp;
        Summary = summary;
        Details = details;
    }

    public DateTime Timestamp { get; }

    public string Summary { get; }

    public string Details { get; }
}

public sealed class ContractorOversightAnalyticsRow
{
    public ContractorOversightAnalyticsRow(string metric, string value, string target, string status)
    {
        Metric = metric;
        Value = value;
        Target = target;
        Status = status;
    }

    public string Metric { get; }

    public string Value { get; }

    public string Target { get; }

    public string Status { get; }
}
/// <summary>
/// Represents the external servicer editor value.
/// </summary>

public sealed partial class ExternalServicerEditor : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _code = string.Empty;

    [ObservableProperty]
    private string _status = "Active";

    [ObservableProperty]
    private string _type = string.Empty;

    [ObservableProperty]
    private string _vatOrId = string.Empty;

    [ObservableProperty]
    private string _contactPerson = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _phone = string.Empty;

    [ObservableProperty]
    private string _address = string.Empty;

    [ObservableProperty]
    private DateTime? _cooperationStart;

    [ObservableProperty]
    private DateTime? _cooperationEnd;

    [ObservableProperty]
    private string _comment = string.Empty;

    [ObservableProperty]
    private string _extraNotes = string.Empty;

    [ObservableProperty]
    private string _digitalSignature = string.Empty;

    [ObservableProperty]
    private string _certificateFiles = string.Empty;
    /// <summary>
    /// Executes the create empty operation.
    /// </summary>

    public static ExternalServicerEditor CreateEmpty() => new();
    /// <summary>
    /// Executes the create for new operation.
    /// </summary>

    public static ExternalServicerEditor CreateForNew()
        => new()
        {
            Status = "Active",
            CooperationStart = DateTime.UtcNow.Date
        };
    /// <summary>
    /// Executes the from servicer operation.
    /// </summary>

    public static ExternalServicerEditor FromServicer(ExternalServicer servicer)
        => new()
        {
            Id = servicer.Id,
            Name = servicer.Name ?? string.Empty,
            Code = servicer.Code ?? string.Empty,
            Status = string.IsNullOrWhiteSpace(servicer.Status)
                ? "Active"
                : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(servicer.Status),
            Type = servicer.Type ?? string.Empty,
            VatOrId = servicer.VatOrId ?? string.Empty,
            ContactPerson = servicer.ContactPerson ?? string.Empty,
            Email = servicer.Email ?? string.Empty,
            Phone = servicer.Phone ?? string.Empty,
            Address = servicer.Address ?? string.Empty,
            CooperationStart = servicer.CooperationStart,
            CooperationEnd = servicer.CooperationEnd,
            Comment = servicer.Comment ?? string.Empty,
            ExtraNotes = servicer.ExtraNotes ?? string.Empty,
            DigitalSignature = servicer.DigitalSignature ?? string.Empty,
            CertificateFiles = servicer.CertificateFiles.Count == 0
                ? string.Empty
                : string.Join(Environment.NewLine, servicer.CertificateFiles)
        };
    /// <summary>
    /// Executes the clone operation.
    /// </summary>

    public ExternalServicerEditor Clone()
        => new()
        {
            Id = Id,
            Name = Name,
            Code = Code,
            Status = Status,
            Type = Type,
            VatOrId = VatOrId,
            ContactPerson = ContactPerson,
            Email = Email,
            Phone = Phone,
            Address = Address,
            CooperationStart = CooperationStart,
            CooperationEnd = CooperationEnd,
            Comment = Comment,
            ExtraNotes = ExtraNotes,
            DigitalSignature = DigitalSignature,
            CertificateFiles = CertificateFiles
        };
    /// <summary>
    /// Executes the to servicer operation.
    /// </summary>

    public ExternalServicer ToServicer(ExternalServicer? existing)
    {
        var target = existing is null ? new ExternalServicer() : CloneServicer(existing);
        target.Id = Id;
        target.Name = Name?.Trim() ?? string.Empty;
        target.Code = Code?.Trim();
        target.Status = Status?.Trim() ?? string.Empty;
        target.Type = Type?.Trim();
        target.VatOrId = VatOrId?.Trim();
        target.ContactPerson = ContactPerson?.Trim();
        target.Email = Email?.Trim();
        target.Phone = Phone?.Trim();
        target.Address = Address?.Trim();
        target.CooperationStart = CooperationStart;
        target.CooperationEnd = CooperationEnd;
        target.Comment = Comment?.Trim();
        target.ExtraNotes = ExtraNotes?.Trim();
        target.DigitalSignature = DigitalSignature?.Trim();
        target.CertificateFiles = ParseCertificates(CertificateFiles);
        return target;
    }

    private static ExternalServicer CloneServicer(ExternalServicer source)
    {
        return new ExternalServicer
        {
            Id = source.Id,
            Name = source.Name,
            Code = source.Code,
            Status = source.Status,
            Type = source.Type,
            VatOrId = source.VatOrId,
            ContactPerson = source.ContactPerson,
            Email = source.Email,
            Phone = source.Phone,
            Address = source.Address,
            CooperationStart = source.CooperationStart,
            CooperationEnd = source.CooperationEnd,
            Comment = source.Comment,
            ExtraNotes = source.ExtraNotes,
            DigitalSignature = source.DigitalSignature,
            CertificateFiles = new List<string>(source.CertificateFiles)
        };
    }

    private static List<string> ParseCertificates(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<string>();
        }

        var separators = new[] { '\n', '\r', ';', ',' };
        return text.Split(separators, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToList();
    }
}
