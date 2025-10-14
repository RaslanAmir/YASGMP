using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Wpf.Helpers;
using YasGMP.Services;

// Alias to the AppCore POCO so the signature matches the MAUI ViewModel.
using SystemEvent = YasGMP.Services.SystemEvent;

namespace YasGMP.Wpf.ViewModels;

/// <summary>
/// Desktop-friendly audit log view-model that mirrors the MAUI surface area used by the shell.
/// </summary>
public class AuditLogViewModel : ObservableObject
{
    private readonly DatabaseService? _databaseService;

    private string? _filterUserIdText;
    private string? _filterEntity;
    private string? _selectedAction;
    private DateTime? _filterFrom;
    private DateTime? _filterTo;
    private string? _statusMessage;
    private bool _isBusy;

    public AuditLogViewModel()
        : this(null)
    {
    }

    public AuditLogViewModel(DatabaseService? databaseService)
    {
        _databaseService = databaseService;

        ApplyFilterCommand = new AsyncRelayCommand(_ => ApplyFilterAsync(), () => !IsBusy);
        RefreshCommand = new AsyncRelayCommand(_ => LoadAsync(), () => !IsBusy);
        ExportCsvCommand = new AsyncRelayCommand(_ => ExportCsvAsync(), CanExport);
        ExportXlsxCommand = new AsyncRelayCommand(_ => ExportXlsxAsync(), CanExport);
        ExportPdfCommand = new AsyncRelayCommand(_ => ExportPdfAsync(), CanExport);

        FilteredEvents.CollectionChanged += (_, __) => UpdateExportCommandStates();
    }

    public ObservableCollection<SystemEvent> FilteredEvents { get; } = new();

    public ObservableCollection<string> ActionTypes { get; } = new(new[]
    {
        "CREATE", "UPDATE", "DELETE", "APPROVE", "CLOSE", "ESCALATE", "EXPORT", "ROLLBACK", "COMMENT", "LOGIN", "LOGOUT", "PRINT", "SIGN", "CONFIG_CHANGE"
    });

    public string? FilterUserIdText
    {
        get => _filterUserIdText;
        set => SetProperty(ref _filterUserIdText, value);
    }

    public string? FilterEntity
    {
        get => _filterEntity;
        set => SetProperty(ref _filterEntity, value);
    }

    public string? SelectedAction
    {
        get => _selectedAction;
        set => SetProperty(ref _selectedAction, value);
    }

    public DateTime? FilterFrom
    {
        get => _filterFrom;
        set => SetProperty(ref _filterFrom, value);
    }

    public DateTime? FilterTo
    {
        get => _filterTo;
        set => SetProperty(ref _filterTo, value);
    }

    public string? StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                UpdateCommandStates();
            }
        }
    }

    public ICommand ApplyFilterCommand { get; }

    public ICommand RefreshCommand { get; }

    public ICommand ExportCsvCommand { get; }

    public ICommand ExportXlsxCommand { get; }

    public ICommand ExportPdfCommand { get; }

    public async Task LoadAsync()
    {
        if (_databaseService is null)
        {
            FilteredEvents.Clear();
            StatusMessage = "Audit log unavailable.";
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = "Loading audit log...";

            var events = await _databaseService
                .GetSystemEventsAsync(
                    ParseUserId(FilterUserIdText),
                    module: null,
                    tableName: Normalize(FilterEntity),
                    severity: Normalize(SelectedAction),
                    from: NormalizeFrom(FilterFrom),
                    to: NormalizeTo(FilterTo),
                    processed: null,
                    limit: 1000)
                .ConfigureAwait(false);

            ReplaceCollection(FilteredEvents, events);
            UpdateActionTypes(events);
            StatusMessage = $"Loaded {FilteredEvents.Count} entries.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load audit log: {ex.Message}";
            throw;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private Task ApplyFilterAsync()
        => LoadAsync();

    private bool CanExport()
        => !IsBusy && FilteredEvents.Count > 0;

    private async Task ExportCsvAsync()
    {
        if (!CanExport())
        {
            StatusMessage = "Nothing to export.";
            return;
        }

        var snapshot = FilteredEvents.ToList();

        try
        {
            IsBusy = true;
            var path = await Task.Run(() => WriteCsv(snapshot)).ConfigureAwait(false);
            StatusMessage = $"CSV exported: {path}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error exporting CSV: {ex.Message}";
            throw;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExportXlsxAsync()
    {
        if (!CanExport())
        {
            StatusMessage = "Nothing to export.";
            return;
        }

        var snapshot = FilteredEvents.ToList();

        try
        {
            IsBusy = true;
            var path = await Task.Run(() => WriteTsv(snapshot)).ConfigureAwait(false);
            StatusMessage = $"XLSX exported: {path}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error exporting XLSX: {ex.Message}";
            throw;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExportPdfAsync()
    {
        if (!CanExport())
        {
            StatusMessage = "Nothing to export.";
            return;
        }

        var snapshot = FilteredEvents.ToList();

        try
        {
            IsBusy = true;
            var path = await Task.Run(() => WritePdf(snapshot)).ConfigureAwait(false);
            StatusMessage = $"PDF exported: {path}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error exporting PDF: {ex.Message}";
            throw;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateCommandStates()
    {
        NotifyCanExecuteChanged(ApplyFilterCommand);
        NotifyCanExecuteChanged(RefreshCommand);
        UpdateExportCommandStates();
    }

    private void UpdateExportCommandStates()
    {
        NotifyCanExecuteChanged(ExportCsvCommand);
        NotifyCanExecuteChanged(ExportXlsxCommand);
        NotifyCanExecuteChanged(ExportPdfCommand);
    }

    private static void NotifyCanExecuteChanged(ICommand? command)
        => UiCommandHelper.NotifyCanExecuteOnUi(command);

    private static void ReplaceCollection(ObservableCollection<SystemEvent> collection, IEnumerable<SystemEvent> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }

    private static void UpdateActionTypes(IEnumerable<SystemEvent> events)
    {
        // Action types are managed externally (collection exposed for binding).
        // This implementation keeps defaults intact to avoid UI churn.
    }

    private static int? ParseUserId(string? value)
    {
        if (int.TryParse(value, out var id))
        {
            return id;
        }

        return null;
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTime? NormalizeFrom(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        var local = DateTime.SpecifyKind(value.Value.Date, DateTimeKind.Local);
        return local.ToUniversalTime();
    }

    private static DateTime? NormalizeTo(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        var endOfDay = value.Value.Date.AddDays(1).AddTicks(-1);
        var local = DateTime.SpecifyKind(endOfDay, DateTimeKind.Local);
        return local.ToUniversalTime();
    }

    private static string WriteCsv(IReadOnlyCollection<SystemEvent> entries)
    {
        var filePath = Path.Combine(EnsureExportRoot(), $"AuditLog_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
        writer.WriteLine("Id,EventType,TableName,RecordId,UserId,EventTime,SourceIp,DeviceInfo,Description");
        foreach (var entry in entries)
        {
            var line = new[]
            {
                entry.Id.ToString(CultureInfo.InvariantCulture),
                Csv(entry.EventType),
                Csv(entry.TableName),
                entry.RecordId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                entry.UserId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                entry.EventTime.ToLocalTime().ToString("u", CultureInfo.InvariantCulture),
                Csv(entry.SourceIp),
                Csv(entry.DeviceInfo),
                Csv(entry.Description)
            };
            writer.WriteLine(string.Join(',', line));
        }

        return filePath;
    }

    private static string WriteTsv(IReadOnlyCollection<SystemEvent> entries)
    {
        var filePath = Path.Combine(EnsureExportRoot(), $"AuditLog_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
        writer.WriteLine("Id\tEventType\tTableName\tRecordId\tUserId\tEventTime\tSourceIp\tDeviceInfo\tDescription");
        foreach (var entry in entries)
        {
            var fields = new[]
            {
                entry.Id.ToString(CultureInfo.InvariantCulture),
                entry.EventType ?? string.Empty,
                entry.TableName ?? string.Empty,
                entry.RecordId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                entry.UserId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                entry.EventTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture),
                entry.SourceIp ?? string.Empty,
                entry.DeviceInfo ?? string.Empty,
                entry.Description ?? string.Empty
            };
            writer.WriteLine(string.Join('\t', fields));
        }

        return filePath;
    }

    private static string WritePdf(IReadOnlyCollection<SystemEvent> entries)
    {
        var filePath = Path.Combine(EnsureExportRoot(), $"AuditLog_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
        writer.WriteLine("Audit Log Export");
        writer.WriteLine(new string('=', 60));
        foreach (var entry in entries)
        {
            writer.WriteLine($"[{entry.EventTime:yyyy-MM-dd HH:mm}] {entry.EventType} {entry.TableName} #{entry.RecordId} - {entry.Description}");
        }

        return filePath;
    }

    private static string Csv(string? value)
    {
        value ??= string.Empty;
        value = value.Replace("\r", " ").Replace("\n", " ").Trim();
        return (value.Contains(',') || value.Contains('"'))
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }

    private static string EnsureExportRoot()
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "YasGMP", "Exports", "AuditLog");
        Directory.CreateDirectory(root);
        return root;
    }
}
