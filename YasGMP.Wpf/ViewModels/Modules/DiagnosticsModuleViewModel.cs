using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>Represents the Diagnostics Module View Model.</summary>
public sealed partial class DiagnosticsModuleViewModel : DataDrivenModuleDocumentViewModel, IDisposable
{
    private const int LogBufferLimit = 200;
    private const int InspectorPreviewLimit = 5;

    private static readonly string[] TelemetryKeyOrder =
    {
        "ts_utc",
        "corr_id",
        "span_id",
        "parent_span",
        "user_id",
        "username",
        "roles",
        "ip",
        "session_id",
        "device",
        "os",
        "app_version",
        "git_commit",
        "diag_level",
        "diag_enabled"
    };

    private static readonly string[] HealthKeyOrder =
    {
        "ts_utc",
        "process_id",
        "proc_start_utc",
        "working_set_mb",
        "os",
        "framework",
        "assembly",
        "assembly_ver"
    };

    /// <summary>Represents the module key value.</summary>
    public const string ModuleKey = "Diagnostics";

    private readonly DiagnosticsFeedService? _feedService;
    private readonly ILocalizationService _localization;
    private readonly HashSet<string> _telemetryOrderLookup;
    private readonly HashSet<string> _healthOrderLookup;

    private IDisposable? _telemetrySubscription;
    private IDisposable? _logSubscription;
    private IDisposable? _healthSubscription;
    private bool _subscriptionsInitialized;
    private bool _hasTelemetrySnapshot;
    private bool _hasHealthSnapshot;
    private bool _hasLogEntry;

    /// <summary>Initializes a new instance of the DiagnosticsModuleViewModel class.</summary>
    public DiagnosticsModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation,
        ILocalizationService localization,
        DiagnosticsFeedService? diagnosticsFeedService = null)
        : base(ModuleKey, localization.GetString("Module.Title.Diagnostics"), databaseService, localization, cflDialogService, shellInteraction, navigation, auditService)
    {
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        _feedService = diagnosticsFeedService;
        _telemetryOrderLookup = new HashSet<string>(TelemetryKeyOrder, StringComparer.OrdinalIgnoreCase);
        _healthOrderLookup = new HashSet<string>(HealthKeyOrder, StringComparer.OrdinalIgnoreCase);

        TelemetrySummaries = new ObservableCollection<DiagnosticDatum>();
        LogEntries = new ObservableCollection<string>();
        HealthResults = new ObservableCollection<DiagnosticDatum>();

        HealthStatus = _localization.GetString("Module.Diagnostics.Status.Unknown");

        if (IsInDesignMode())
        {
            PopulateDesignTimeCollections();
            UpdateRecordsCollection(BuildRecordsList());
        }
    }

    /// <summary>Collection describing the current telemetry snapshot.</summary>
    public ObservableCollection<DiagnosticDatum> TelemetrySummaries { get; }

    /// <summary>Rolling buffer of recent log entries provided by the diagnostics feed.</summary>
    public ObservableCollection<string> LogEntries { get; }

    /// <summary>Most recent health report flattened into display rows.</summary>
    public ObservableCollection<DiagnosticDatum> HealthResults { get; }

    /// <summary>Latest raw telemetry snapshot captured from the diagnostics feed.</summary>
    [ObservableProperty]
    private IReadOnlyDictionary<string, object?>? _latestTelemetrySnapshot;

    /// <summary>Localized summary of the most recent health snapshot.</summary>
    [ObservableProperty]
    private string _healthStatus = string.Empty;

    /// <inheritdoc />
    protected override Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        if (IsInDesignMode() || _feedService is null)
        {
            PopulateDesignTimeCollections();
            return Task.FromResult(BuildRecordsList());
        }

        EnsureSubscriptions();
        return Task.FromResult(BuildRecordsList());
    }

    /// <inheritdoc />
    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
    {
        PopulateDesignTimeCollections();
        return BuildRecordsList();
    }

    /// <inheritdoc />
    protected override string FormatLoadedStatus(int count)
    {
        var template = _localization.GetString("Module.Diagnostics.StatusMessage.Ready");
        return string.Format(CultureInfo.CurrentCulture, template, count);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _telemetrySubscription?.Dispose();
        _logSubscription?.Dispose();
        _healthSubscription?.Dispose();
    }

    private void EnsureSubscriptions()
    {
        if (_subscriptionsInitialized || _feedService is null)
        {
            return;
        }

        _telemetrySubscription = _feedService.SubscribeTelemetry(OnTelemetrySnapshot);
        _logSubscription = _feedService.SubscribeLog(OnLogEntry);
        _healthSubscription = _feedService.SubscribeHealth(OnHealthSnapshot);
        _subscriptionsInitialized = true;
        _hasTelemetrySnapshot = false;
        _hasHealthSnapshot = false;
        _hasLogEntry = false;
    }

    private void OnTelemetrySnapshot(IReadOnlyDictionary<string, object?> payload)
    {
        LatestTelemetrySnapshot = new Dictionary<string, object?>(payload, StringComparer.OrdinalIgnoreCase);
        _hasTelemetrySnapshot = true;

        TelemetrySummaries.Clear();
        AppendOrderedEntries(TelemetrySummaries, LatestTelemetrySnapshot, TelemetryKeyOrder, _telemetryOrderLookup, ResolveTelemetryLabel);

        var telemetryTemplate = _localization.GetString("Module.Diagnostics.StatusMessage.TelemetryUpdated");
        StatusMessage = string.Format(
            CultureInfo.CurrentCulture,
            telemetryTemplate,
            DateTime.Now.ToString("T", CultureInfo.CurrentCulture));

        RefreshRecords();
    }

    private void OnLogEntry(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        LogEntries.Add(line);
        while (LogEntries.Count > LogBufferLimit)
        {
            LogEntries.RemoveAt(0);
        }

        if (!_hasLogEntry)
        {
            _hasLogEntry = true;
            StatusMessage = _localization.GetString("Module.Diagnostics.StatusMessage.LogStreaming");
        }

        RefreshRecords();
    }

    private void OnHealthSnapshot(IReadOnlyDictionary<string, object?> payload)
    {
        _hasHealthSnapshot = true;

        HealthResults.Clear();
        AppendOrderedEntries(HealthResults, payload, HealthKeyOrder, _healthOrderLookup, ResolveHealthLabel);

        HealthStatus = _localization.GetString("Module.Diagnostics.Status.Healthy");
        var healthTemplate = _localization.GetString("Module.Diagnostics.StatusMessage.HealthUpdated");
        StatusMessage = string.Format(
            CultureInfo.CurrentCulture,
            healthTemplate,
            DateTime.Now.ToString("T", CultureInfo.CurrentCulture));

        RefreshRecords();
    }

    private void RefreshRecords()
    {
        var snapshot = BuildRecordsList();
        UpdateRecordsCollection(snapshot);
    }

    private IReadOnlyList<ModuleRecord> BuildRecordsList()
    {
        var telemetryFields = BuildTelemetryInspector();
        var logFields = BuildLogInspector();
        var healthFields = BuildHealthInspector();

        var telemetryStatus = _hasTelemetrySnapshot
            ? _localization.GetString("Module.Diagnostics.Status.Live")
            : _localization.GetString("Module.Diagnostics.Status.Waiting");

        var logStatus = _hasLogEntry
            ? _localization.GetString("Module.Diagnostics.Status.Streaming")
            : _localization.GetString("Module.Diagnostics.Status.Waiting");

        var healthStatus = _hasHealthSnapshot
            ? (string.IsNullOrWhiteSpace(HealthStatus)
                ? _localization.GetString("Module.Diagnostics.Status.Healthy")
                : HealthStatus)
            : _localization.GetString("Module.Diagnostics.Status.Unknown");

        return new List<ModuleRecord>
        {
            new(
                "DIAG-TELEMETRY",
                _localization.GetString("Module.Diagnostics.Record.Telemetry.Title"),
                _localization.GetString("Module.Diagnostics.Record.Telemetry.Code"),
                telemetryStatus,
                _localization.GetString("Module.Diagnostics.Record.Telemetry.Description"),
                telemetryFields,
                null,
                null),
            new(
                "DIAG-LOG",
                _localization.GetString("Module.Diagnostics.Record.Log.Title"),
                _localization.GetString("Module.Diagnostics.Record.Log.Code"),
                logStatus,
                _localization.GetString("Module.Diagnostics.Record.Log.Description"),
                logFields,
                null,
                null),
            new(
                "DIAG-HEALTH",
                _localization.GetString("Module.Diagnostics.Record.Health.Title"),
                _localization.GetString("Module.Diagnostics.Record.Health.Code"),
                healthStatus,
                _localization.GetString("Module.Diagnostics.Record.Health.Description"),
                healthFields,
                null,
                null)
        };
    }

    private IReadOnlyList<InspectorField> BuildTelemetryInspector()
    {
        if (TelemetrySummaries.Count == 0)
        {
            return new[]
            {
                new InspectorField(
                    _localization.GetString("Module.Diagnostics.Telemetry.Empty"),
                    string.Empty)
            };
        }

        return TelemetrySummaries
            .Take(InspectorPreviewLimit)
            .Select(item => new InspectorField(item.Label, item.Value))
            .ToList();
    }

    private IReadOnlyList<InspectorField> BuildLogInspector()
    {
        if (LogEntries.Count == 0)
        {
            return new[]
            {
                new InspectorField(
                    _localization.GetString("Module.Diagnostics.Log.Empty"),
                    string.Empty)
            };
        }

        var template = _localization.GetString("Module.Diagnostics.Log.Entry");
        return LogEntries
            .Reverse()
            .Take(InspectorPreviewLimit)
            .Select((entry, index) => new InspectorField(
                string.Format(CultureInfo.CurrentCulture, template, index + 1),
                entry))
            .ToList();
    }

    private IReadOnlyList<InspectorField> BuildHealthInspector()
    {
        if (HealthResults.Count == 0)
        {
            return new[]
            {
                new InspectorField(
                    _localization.GetString("Module.Diagnostics.Health.Empty"),
                    string.Empty)
            };
        }

        return HealthResults
            .Take(InspectorPreviewLimit)
            .Select(item => new InspectorField(item.Label, item.Value))
            .ToList();
    }

    private void UpdateRecordsCollection(IReadOnlyList<ModuleRecord> snapshot)
    {
        var previousKey = SelectedRecord?.Key;

        Records.Clear();
        foreach (var record in snapshot)
        {
            Records.Add(record);
        }

        if (!string.IsNullOrWhiteSpace(previousKey))
        {
            var match = Records.FirstOrDefault(r => string.Equals(r.Key, previousKey, StringComparison.Ordinal));
            if (match is not null)
            {
                SelectedRecord = match;
                return;
            }
        }

        if (Records.Count > 0)
        {
            SelectedRecord = Records[0];
        }
        else
        {
            SelectedRecord = null;
        }
    }

    private void PopulateDesignTimeCollections()
    {
        _hasTelemetrySnapshot = true;
        _hasHealthSnapshot = true;
        _hasLogEntry = true;

        var now = DateTime.Now;
        var telemetry = new Dictionary<string, object?>
        {
            ["ts_utc"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
            ["corr_id"] = Guid.NewGuid().ToString("N"),
            ["span_id"] = Guid.NewGuid().ToString("N")[..16],
            ["parent_span"] = Guid.NewGuid().ToString("N")[..16],
            ["user_id"] = 42,
            ["username"] = "darko",
            ["roles"] = "Admin,Quality", 
            ["ip"] = "10.0.0.15",
            ["session_id"] = Guid.NewGuid().ToString("N"),
            ["device"] = "Surface Laptop Studio",
            ["os"] = "Windows 11 Pro",
            ["app_version"] = "9.0.0-preview",
            ["git_commit"] = "abcdef1234567890",
            ["diag_level"] = "Info",
            ["diag_enabled"] = true
        };

        LatestTelemetrySnapshot = new Dictionary<string, object?>(telemetry, StringComparer.OrdinalIgnoreCase);
        TelemetrySummaries.Clear();
        AppendOrderedEntries(TelemetrySummaries, telemetry, TelemetryKeyOrder, _telemetryOrderLookup, ResolveTelemetryLabel);

        LogEntries.Clear();
        LogEntries.Add($"{now:HH:mm:ss} [INF] Diagnostics feed connected");
        LogEntries.Add($"{now.AddMinutes(-2):HH:mm:ss} [WRN] Background job latency at 6500 ms");
        LogEntries.Add($"{now.AddMinutes(-5):HH:mm:ss} [ERR] CAPA workflow retry scheduled");

        var health = new Dictionary<string, object?>
        {
            ["ts_utc"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
            ["process_id"] = 4242,
            ["proc_start_utc"] = DateTime.UtcNow.AddHours(-6).ToString("o", CultureInfo.InvariantCulture),
            ["working_set_mb"] = 512,
            ["os"] = "Windows 11 Pro 23H2",
            ["framework"] = ".NET 9.0.0-preview",
            ["assembly"] = "YasGMP.Wpf",
            ["assembly_ver"] = "9.0.0.0"
        };

        HealthResults.Clear();
        AppendOrderedEntries(HealthResults, health, HealthKeyOrder, _healthOrderLookup, ResolveHealthLabel);
        HealthStatus = _localization.GetString("Module.Diagnostics.Status.Healthy");
    }

    private void AppendOrderedEntries(
        ObservableCollection<DiagnosticDatum> target,
        IReadOnlyDictionary<string, object?> source,
        IReadOnlyList<string> orderedKeys,
        HashSet<string> orderLookup,
        Func<string, string> labelResolver)
    {
        foreach (var key in orderedKeys)
        {
            if (source.TryGetValue(key, out var value))
            {
                target.Add(new DiagnosticDatum(labelResolver(key), FormatValue(value)));
            }
        }

        foreach (var kvp in source.Where(entry => !orderLookup.Contains(entry.Key)))
        {
            target.Add(new DiagnosticDatum(labelResolver(kvp.Key), FormatValue(kvp.Value)));
        }
    }

    private string ResolveTelemetryLabel(string key)
        => _localization.GetString($"Module.Diagnostics.Telemetry.{key}");

    private string ResolveHealthLabel(string key)
        => _localization.GetString($"Module.Diagnostics.Health.{key}");

    private static string FormatValue(object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        return value switch
        {
            DateTime dt => dt.ToString("G", CultureInfo.CurrentCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.CurrentCulture),
            _ => value.ToString() ?? string.Empty
        };
    }

    private static bool IsInDesignMode()
        => DesignerProperties.GetIsInDesignMode(new DependencyObject());

    /// <summary>Tuple describing a diagnostics key/value pair.</summary>
    public sealed record DiagnosticDatum(string Label, string Value);
}
