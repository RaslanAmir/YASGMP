using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using MySqlConnector;
using Xunit;
using YasGMP.Services;
using YasGMP.ViewModels;
using YasGMP.Wpf.Tests.TestDoubles;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Tests;

public class AuditLogDocumentViewModelTests
{
    [Fact]
    public async Task RefreshCommand_ProjectsMauiEventsIntoRecordsAndStatus()
    {
        var auditLog = new ConfigurableAuditLogViewModel();
        var events = new[]
        {
            CreateEvent(10, "LOGIN", "system", description: "User darko logged in", userId: 5,
                timestamp: new DateTime(2025, 1, 12, 8, 15, 0, DateTimeKind.Utc), sourceIp: "192.168.1.5"),
            CreateEvent(11, "UPDATE", "work_orders", recordId: 77, description: "Adjusted plan",
                userId: 7, timestamp: new DateTime(2025, 1, 12, 9, 30, 0, DateTimeKind.Utc), sourceIp: "10.0.0.7")
        };
        auditLog.QueueLoad(events);

        var document = CreateDocument(auditLog);

        await document.RefreshCommand.ExecuteAsync(null);

        Assert.Equal(2, document.Records.Count);
        Assert.True(document.HasResults);
        Assert.False(document.HasError);
        Assert.Equal("Prikazano: 2", document.StatusMessage);
        Assert.False(document.IsBusy);
        Assert.False(auditLog.IsBusy);

        var titles = document.Records.Select(r => r.Title).ToList();
        Assert.Contains("LOGIN: system", titles);
        Assert.Contains("UPDATE: work_orders #77", titles);
    }

    [Fact]
    public async Task ApplyFilterCommand_UpdatesRecordsAndSyncsStatusAndBusy()
    {
        var auditLog = new ConfigurableAuditLogViewModel();
        var initialEvents = new[]
        {
            CreateEvent(20, "UPDATE", "equipment", recordId: 5, description: "Calibrated", userId: 12),
            CreateEvent(21, "DELETE", "equipment", recordId: 6, description: "Removed")
        };
        auditLog.ReplaceFilteredEvents(initialEvents);
        auditLog.StatusMessage = "Prikazano: 2";

        var filteredEvent = CreateEvent(21, "DELETE", "equipment", recordId: 6, description: "Removed");
        auditLog.SetApplyFilterCommand(new RelayCommand(() =>
        {
            auditLog.ReplaceFilteredEvents(new[] { filteredEvent });
            auditLog.StatusMessage = "Prikazano: 1";
        }));

        var document = CreateDocument(auditLog);
        Assert.Equal(2, document.Records.Count);
        Assert.True(document.HasResults);

        await document.ApplyFilterCommand.ExecuteAsync(null);

        Assert.Single(document.Records);
        Assert.True(document.HasResults);
        Assert.Equal("Prikazano: 1", document.StatusMessage);
        Assert.False(document.IsBusy);

        auditLog.IsBusy = true;
        Assert.True(document.IsBusy);

        auditLog.IsBusy = false;
        Assert.False(document.IsBusy);

        auditLog.StatusMessage = "MAUI custom status";
        Assert.Equal("MAUI custom status", document.StatusMessage);
    }

    [Fact]
    public async Task ExportCsvCommand_AsyncRelay_SetsBusyLifecycleAndStatus()
    {
        var auditLog = new ConfigurableAuditLogViewModel();
        var sampleEvent = CreateEvent(30, "EXPORT", "audit_log", description: "CSV request");
        auditLog.ReplaceFilteredEvents(new[] { sampleEvent });

        var busySnapshots = new List<bool>();
        AuditLogDocumentViewModel? document = null;

        auditLog.SetExportCsvCommand(new AsyncRelayCommand(async () =>
        {
            auditLog.IsBusy = true;
            busySnapshots.Add(document!.IsBusy);
            auditLog.StatusMessage = "CSV izvezen: C:/exports/audit.csv";
            auditLog.IsBusy = false;
            await Task.CompletedTask;
        }));

        document = CreateDocument(auditLog);
        Assert.True(document.ExportCsvCommand.CanExecute(null));

        await document.ExportCsvCommand.ExecuteAsync(null);

        Assert.NotEmpty(busySnapshots);
        Assert.All(busySnapshots, state => Assert.True(state));
        Assert.False(document.IsBusy);
        Assert.False(document.HasError);
        Assert.Equal("CSV izvezen: C:/exports/audit.csv", document.StatusMessage);
        Assert.True(document.ExportCsvCommand.CanExecute(null));
    }

    [Fact]
    public async Task ExportPdfCommand_RelayException_SetsErrorAndAllowsRetry()
    {
        var auditLog = new ConfigurableAuditLogViewModel();
        var sampleEvent = CreateEvent(40, "EXPORT", "audit_log", description: "PDF request");
        auditLog.ReplaceFilteredEvents(new[] { sampleEvent });
        auditLog.StatusMessage = string.Empty;

        auditLog.SetExportPdfCommand(new RelayCommand(() => throw new InvalidOperationException("boom")));

        var document = CreateDocument(auditLog);
        Assert.True(document.ExportPdfCommand.CanExecute(null));

        await document.ExportPdfCommand.ExecuteAsync(null);

        Assert.True(document.HasError);
        Assert.False(document.IsBusy);
        Assert.Equal("Failed to export audit log to PDF: boom", document.StatusMessage);
        Assert.True(document.ExportPdfCommand.CanExecute(null));
    }

    private static AuditLogDocumentViewModel CreateDocument(ConfigurableAuditLogViewModel auditLog)
        => new(
            CreateUnused<AuditService>(),
            CreateUnused<ExportService>(),
            auditLog,
            new StubCflDialogService(),
            new StubShellInteractionService(),
            new StubModuleNavigationService());

    private static SystemEvent CreateEvent(
        int id,
        string eventType,
        string tableName,
        int? recordId = null,
        string? description = null,
        int? userId = null,
        DateTime? timestamp = null,
        string? sourceIp = null)
        => new()
        {
            Id = id,
            EventType = eventType,
            TableName = tableName,
            RecordId = recordId,
            Description = description,
            UserId = userId,
            EventTime = timestamp ?? DateTime.UtcNow,
            SourceIp = sourceIp,
            DeviceInfo = "Win11",
        };

    private static T CreateUnused<T>() where T : class
        => (T)FormatterServices.GetUninitializedObject(typeof(T));

    private sealed class ConfigurableAuditLogViewModel : AuditLogViewModel
    {
        private readonly DatabaseService _databaseService;
        private List<SystemEvent> _pendingLoadEvents = new();

        public ConfigurableAuditLogViewModel()
            : this(new DatabaseService("Server=stub;Uid=stub;Pwd=stub;Database=stub;"))
        {
        }

        private ConfigurableAuditLogViewModel(DatabaseService databaseService)
            : base(databaseService)
        {
            _databaseService = databaseService;
            InstallSelectOverride();

            SetCommand(nameof(ApplyFilterCommand), new RelayCommand(() => { }));
            SetCommand(nameof(RefreshCommand), new AsyncRelayCommand(() => Task.CompletedTask));
            SetCommand(nameof(ExportCsvCommand), new AsyncRelayCommand(() => Task.CompletedTask));
            SetCommand(nameof(ExportXlsxCommand), new AsyncRelayCommand(() => Task.CompletedTask));
            SetCommand(nameof(ExportPdfCommand), new AsyncRelayCommand(() => Task.CompletedTask));
        }

        public void QueueLoad(IEnumerable<SystemEvent> events)
        {
            _pendingLoadEvents = events?.Select(Clone).ToList() ?? new List<SystemEvent>();
        }

        public void ReplaceFilteredEvents(IEnumerable<SystemEvent> events)
        {
            FilteredEvents.Clear();
            foreach (var item in events)
            {
                FilteredEvents.Add(Clone(item));
            }
        }

        public void SetApplyFilterCommand(ICommand command)
            => SetCommand(nameof(ApplyFilterCommand), command);

        public void SetExportCsvCommand(ICommand command)
            => SetCommand(nameof(ExportCsvCommand), command);

        public void SetExportPdfCommand(ICommand command)
            => SetCommand(nameof(ExportPdfCommand), command);

        private void InstallSelectOverride()
        {
            var property = typeof(DatabaseService).GetProperty(
                "ExecuteSelectOverride",
                BindingFlags.Instance | BindingFlags.NonPublic);

            property!.SetValue(
                _databaseService,
                new Func<string, IEnumerable<MySqlParameter>?, CancellationToken, Task<DataTable>>(HandleExecuteSelectAsync));
        }

        private Task<DataTable> HandleExecuteSelectAsync(
            string sql,
            IEnumerable<MySqlParameter>? parameters,
            CancellationToken cancellationToken)
        {
            var table = CreateTable();
            foreach (var item in _pendingLoadEvents)
            {
                AddRow(table, item);
            }

            _pendingLoadEvents = new List<SystemEvent>();
            return Task.FromResult(table);
        }

        private static DataTable CreateTable()
        {
            var table = new DataTable();
            table.Columns.Add("id", typeof(int));
            table.Columns.Add("ts_utc", typeof(DateTime));
            table.Columns.Add("user_id", typeof(int));
            table.Columns.Add("event_type", typeof(string));
            table.Columns.Add("table_name", typeof(string));
            table.Columns.Add("related_module", typeof(string));
            table.Columns.Add("record_id", typeof(int));
            table.Columns.Add("field_name", typeof(string));
            table.Columns.Add("old_value", typeof(string));
            table.Columns.Add("new_value", typeof(string));
            table.Columns.Add("description", typeof(string));
            table.Columns.Add("source_ip", typeof(string));
            table.Columns.Add("device_info", typeof(string));
            table.Columns.Add("session_id", typeof(string));
            table.Columns.Add("severity", typeof(string));
            table.Columns.Add("processed", typeof(bool));
            return table;
        }

        private static void AddRow(DataTable table, SystemEvent evt)
        {
            var row = table.NewRow();
            row["id"] = evt.Id;
            row["ts_utc"] = evt.EventTime == DateTime.MinValue
                ? DateTime.UtcNow
                : evt.EventTime.ToUniversalTime();
            row["user_id"] = evt.UserId.HasValue ? evt.UserId.Value : (object)DBNull.Value;
            row["event_type"] = evt.EventType ?? string.Empty;
            row["table_name"] = evt.TableName ?? string.Empty;
            row["related_module"] = evt.RelatedModule ?? string.Empty;
            row["record_id"] = evt.RecordId.HasValue ? evt.RecordId.Value : (object)DBNull.Value;
            row["field_name"] = evt.FieldName ?? string.Empty;
            row["old_value"] = evt.OldValue ?? string.Empty;
            row["new_value"] = evt.NewValue ?? string.Empty;
            row["description"] = evt.Description ?? string.Empty;
            row["source_ip"] = evt.SourceIp ?? string.Empty;
            row["device_info"] = evt.DeviceInfo ?? string.Empty;
            row["session_id"] = evt.SessionId ?? string.Empty;
            row["severity"] = evt.Severity ?? string.Empty;
            row["processed"] = evt.Processed;
            table.Rows.Add(row);
        }

        private static SystemEvent Clone(SystemEvent source)
            => new()
            {
                Id = source.Id,
                EventTime = source.EventTime,
                UserId = source.UserId,
                EventType = source.EventType,
                TableName = source.TableName,
                RelatedModule = source.RelatedModule,
                RecordId = source.RecordId,
                FieldName = source.FieldName,
                OldValue = source.OldValue,
                NewValue = source.NewValue,
                Description = source.Description,
                SourceIp = source.SourceIp,
                DeviceInfo = source.DeviceInfo,
                SessionId = source.SessionId,
                Severity = source.Severity,
                Processed = source.Processed
            };

        private void SetCommand(string propertyName, object command)
        {
            var field = typeof(AuditLogViewModel).GetField(
                $"<{propertyName}>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);

            field!.SetValue(this, command);
        }
    }
}
