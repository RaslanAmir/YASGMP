using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Models;
using YasGMP.Models.Enums;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
namespace YasGMP.Diagnostics
{
    /// <summary>
    /// Utility harness that exercises the ChangeControl assignment workflow with
    /// stubbed database/authentication services. It is designed so QA can validate SQL emitted during
    /// assignment workflows without a full UI automation stack.
    /// </summary>
    public static class ChangeControlAssignmentHarness
    {
#if YASGMP_APP_CORE_MAUI && YASGMP_INCLUDE_CHANGE_CONTROL_HARNESS
        /// <summary>
        /// Runs the harness end-to-end: initial assignment followed by a reassignment. The harness captures
        /// executed SQL statements and audit metadata emitted via <c>system_event_log</c> insertions.
        /// </summary>
        /// <returns>The populated <see cref="ChangeControlAssignmentHarnessResult"/>.</returns>
        public static async Task<ChangeControlAssignmentHarnessResult> RunAsync()
        {
            var executedSql = new List<string>();
            var auditEvents = new List<ChangeControlAssignmentHarnessEvent>();
            var statusMessages = new List<string>();

            var table = BuildSeedData();
            var db = new DatabaseService("Server=stub;Database=stub;Uid=stub;Pwd=stub;");

            db.ExecuteSelectOverride = (sql, parameters, token) =>
            {
                executedSql.Add(sql);
                return Task.FromResult(table.Copy());
            };

            db.ExecuteNonQueryOverride = (sql, parameters, token) =>
            {
                executedSql.Add(sql);

                if (sql.Contains("change_controls", StringComparison.OrdinalIgnoreCase))
                {
                    var assignedParam = parameters?.FirstOrDefault(p => p.ParameterName == "@assigned");
                    if (assignedParam != null)
                    {
                        table.Rows[0]["assigned_to_id"] = assignedParam.Value ?? DBNull.Value;
                    }
                }

                if (sql.Contains("system_event_log", StringComparison.OrdinalIgnoreCase))
                {
                    var paramList = parameters?.ToList() ?? new List<MySqlParameter>();
                    var eventType = paramList.FirstOrDefault(p => p.ParameterName == "@etype")?.Value?.ToString() ?? string.Empty;
                    var oldValue = paramList.FirstOrDefault(p => p.ParameterName == "@old")?.Value?.ToString();
                    var newValue = paramList.FirstOrDefault(p => p.ParameterName == "@new")?.Value?.ToString();
                    var description = paramList.FirstOrDefault(p => p.ParameterName == "@desc")?.Value?.ToString();
                    auditEvents.Add(new ChangeControlAssignmentHarnessEvent(eventType, NullIfEmpty(oldValue), NullIfEmpty(newValue), description));
                }

                return Task.FromResult(1);
            };

            var auth = new HarnessAuthContext();
            var workflow = new HarnessChangeControlWorkflow(db, auth);
            await workflow.LoadChangeControlsAsync().ConfigureAwait(false);

            var first = workflow.ChangeControls.FirstOrDefault();
            if (first == null)
            {
                db.ResetTestOverrides();
                return new ChangeControlAssignmentHarnessResult(statusMessages, auditEvents, executedSql);
            }

            workflow.SelectedChangeControl = first;
            first.AssignedToId = auth.InitialAssigneeId;
            await workflow.AssignChangeControlAsync().ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(workflow.StatusMessage))
            {
                statusMessages.Add(workflow.StatusMessage!);
            }

            var second = workflow.ChangeControls.FirstOrDefault();
            if (second != null)
            {
                second.AssignedToId = auth.ReassignmentAssigneeId;
                workflow.SelectedChangeControl = second;
                await workflow.AssignChangeControlAsync().ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(workflow.StatusMessage))
                {
                    statusMessages.Add(workflow.StatusMessage!);
                }
            }

            db.ResetTestOverrides();

            return new ChangeControlAssignmentHarnessResult(statusMessages, auditEvents, executedSql);
        }

        private static DataTable BuildSeedData()
        {
            var table = new DataTable();
            table.Columns.Add("id", typeof(int));
            table.Columns.Add("code", typeof(string));
            table.Columns.Add("title", typeof(string));
            table.Columns.Add("description", typeof(string));
            table.Columns.Add("status", typeof(string));
            table.Columns.Add("requested_by_id", typeof(int));
            table.Columns.Add("date_requested", typeof(DateTime));
            table.Columns.Add("assigned_to_id", typeof(int));
            table.Rows.Add(1, "CC-HARNESS", "Harness change control", "Synthetic record for assignment harness", "Draft", 2, DateTime.UtcNow, DBNull.Value);
            return table;
        }

        private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

        private sealed class HarnessAuthContext : IAuthContext
        {
            public HarnessAuthContext()
            {
                CurrentUser = new User
                {
                    Id = 9001,
                    Username = "qa.harness",
                    FullName = "QA Harness",
                    Role = "qa"
                };
                CurrentSessionId = Guid.NewGuid().ToString();
            }

            public User? CurrentUser { get; }
            public string CurrentSessionId { get; }
            public string CurrentDeviceInfo { get; } = "Device=Harness;OS=Windows;App=Diagnostics";
            public string CurrentIpAddress { get; } = "127.0.0.1";

            public int InitialAssigneeId => 2001;
            public int ReassignmentAssigneeId => 2002;
        }

        private sealed class HarnessChangeControlWorkflow
        {
            private readonly DatabaseService _dbService;
            private readonly IAuthContext _authContext;
            private readonly Dictionary<int, int?> _initialAssignees = new();
            private readonly List<ChangeControl> _changeControls = new();

            public HarnessChangeControlWorkflow(DatabaseService dbService, IAuthContext authContext)
            {
                _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
                _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
            }

            public IReadOnlyList<ChangeControl> ChangeControls => _changeControls;

            public ChangeControl? SelectedChangeControl { get; set; }

            public string? StatusMessage { get; private set; }

            public async Task LoadChangeControlsAsync()
            {
                try
                {
                    var table = await _dbService.ExecuteSelectAsync(
                        "SELECT id, code, title, description, status, requested_by_id, date_requested, assigned_to_id FROM change_controls")
                        .ConfigureAwait(false);

                    _changeControls.Clear();
                    _initialAssignees.Clear();

                    foreach (DataRow row in table.Rows)
                    {
                        var statusText = row["status"]?.ToString();
                        if (!Enum.TryParse(statusText ?? nameof(ChangeControlStatus.Draft), true, out ChangeControlStatus parsed))
                        {
                            parsed = ChangeControlStatus.Draft;
                        }

                        var changeControl = new ChangeControl
                        {
                            Id = Convert.ToInt32(row["id"]),
                            Code = row["code"]?.ToString() ?? string.Empty,
                            Title = row["title"]?.ToString() ?? string.Empty,
                            Description = row["description"]?.ToString() ?? string.Empty,
                            Status = parsed,
                            RequestedById = table.Columns.Contains("requested_by_id") && row["requested_by_id"] != DBNull.Value
                                ? Convert.ToInt32(row["requested_by_id"])
                                : (int?)null,
                            DateRequested = table.Columns.Contains("date_requested") && row["date_requested"] != DBNull.Value
                                ? Convert.ToDateTime(row["date_requested"])
                                : null,
                            AssignedToId = table.Columns.Contains("assigned_to_id") && row["assigned_to_id"] != DBNull.Value
                                ? Convert.ToInt32(row["assigned_to_id"])
                                : (int?)null
                        };

                        changeControl.AssignedToId = NormalizeAssignee(changeControl.AssignedToId);
                        _initialAssignees[changeControl.Id] = changeControl.AssignedToId;
                        _changeControls.Add(changeControl);
                    }

                    StatusMessage = $"Loaded {_changeControls.Count} change controls.";
                    SelectedChangeControl = _changeControls.FirstOrDefault();
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error loading change controls: {ex.Message}";
                }
            }

            public async Task AssignChangeControlAsync()
            {
                if (SelectedChangeControl == null)
                {
                    StatusMessage = "No change control selected.";
                    return;
                }

                var actor = _authContext.CurrentUser;
                if (actor == null)
                {
                    StatusMessage = "Authentication required to assign change controls.";
                    return;
                }

                try
                {
                    _initialAssignees.TryGetValue(SelectedChangeControl.Id, out var storedOriginal);
                    var previousAssignee = NormalizeAssignee(storedOriginal);
                    var newAssignee = NormalizeAssignee(SelectedChangeControl.AssignedToId);
                    SelectedChangeControl.AssignedToId = newAssignee;

                    if (previousAssignee == newAssignee)
                    {
                        StatusMessage = $"Change control '{SelectedChangeControl.Title}' already assigned to {FormatAssignee(newAssignee)}.";
                    }
                    else
                    {
                        try
                        {
                            var parameters = new MySqlParameter[]
                            {
                                new("@assigned", (object?)newAssignee ?? DBNull.Value),
                                new("@id", SelectedChangeControl.Id)
                            };

                            await _dbService.ExecuteNonQueryAsync(
                                "UPDATE change_controls SET assigned_to_id=@assigned, updated_at=UTC_TIMESTAMP() WHERE id=@id",
                                parameters).ConfigureAwait(false);
                        }
                        catch (MySqlException ex) when (ex.Number == 1054 || ex.Number == 1146)
                        {
                            // Schema without assigned_to_id column (dev/test DB). Swallow silently and continue.
                        }

                        var eventType = previousAssignee.HasValue ? "CC_REASSIGN" : "CC_ASSIGN";
                        var description = $"code={SelectedChangeControl.Code ?? SelectedChangeControl.Id.ToString()}; new={FormatAssignee(newAssignee)}; previous={FormatAssignee(previousAssignee)}; actor={actor.Id}";

                        await _dbService.LogSystemEventAsync(
                            userId: actor.Id,
                            eventType: eventType,
                            tableName: "change_controls",
                            module: "ChangeControl",
                            recordId: SelectedChangeControl.Id,
                            description: description,
                            ip: _authContext.CurrentIpAddress,
                            severity: "audit",
                            deviceInfo: _authContext.CurrentDeviceInfo,
                            sessionId: _authContext.CurrentSessionId,
                            fieldName: "assigned_to_id",
                            oldValue: previousAssignee?.ToString(),
                            newValue: newAssignee?.ToString()).ConfigureAwait(false);

                        StatusMessage = previousAssignee.HasValue
                            ? $"Change control '{SelectedChangeControl.Title}' reassigned from {FormatAssignee(previousAssignee)} to {FormatAssignee(newAssignee)}."
                            : $"Change control '{SelectedChangeControl.Title}' assigned to {FormatAssignee(newAssignee)}.";
                    }

                    _initialAssignees[SelectedChangeControl.Id] = newAssignee;
                    await LoadChangeControlsAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Assignment failed: {ex.Message}";
                }
            }

            private static int? NormalizeAssignee(int? assigneeId)
            {
                if (!assigneeId.HasValue)
                {
                    return null;
                }

                var value = assigneeId.Value;
                return value > 0 ? value : null;
            }

            private static string FormatAssignee(int? assigneeId)
                => assigneeId.HasValue ? $"user ID {assigneeId.Value}" : "no one";
        }
#else
        /// <summary>
        /// Throws because the MAUI-only harness is not available in the desktop diagnostics build.
        /// </summary>
        /// <returns>A task faulted with <see cref="NotSupportedException"/>.</returns>
        public static Task<ChangeControlAssignmentHarnessResult> RunAsync()
        {
            return Task.FromException<ChangeControlAssignmentHarnessResult>(
                new NotSupportedException("ChangeControlAssignmentHarness requires the MAUI diagnostics stack and should be compiled with YASGMP_INCLUDE_CHANGE_CONTROL_HARNESS defined when MAUI view-models are available."));
        }
#endif
    }

    /// <summary>Minimal projection of an audit event captured by the harness.</summary>
    public sealed record ChangeControlAssignmentHarnessEvent(string EventType, string? OldValue, string? NewValue, string? Description);

    /// <summary>Return payload for the harness including status messages and captured SQL.</summary>
    public sealed record ChangeControlAssignmentHarnessResult(
        IReadOnlyList<string> StatusMessages,
        IReadOnlyList<ChangeControlAssignmentHarnessEvent> LoggedEvents,
        IReadOnlyList<string> ExecutedSql)
    {
        /// <summary>True when at least one INSERT into system_event_log was observed.</summary>
        public bool LoggedAudit => LoggedEvents.Count > 0;

        /// <summary>True when the harness observed a CC_ASSIGN audit event.</summary>
        public bool HasInitialAssignmentEvent => LoggedEvents.Any(e =>
            string.Equals(e.EventType, "CC_ASSIGN", StringComparison.OrdinalIgnoreCase));

        /// <summary>True when the harness observed a CC_REASSIGN audit event.</summary>
        public bool HasReassignmentEvent => LoggedEvents.Any(e =>
            string.Equals(e.EventType, "CC_REASSIGN", StringComparison.OrdinalIgnoreCase));

        /// <summary>Returns the expected audit event types that did not appear during the harness run.</summary>
        public IReadOnlyList<string> MissingAuditEvents => _missingAuditEvents ??= ComputeMissingAuditEvents();

        private IReadOnlyList<string>? _missingAuditEvents;

        private IReadOnlyList<string> ComputeMissingAuditEvents()
        {
            var missing = new List<string>();
            if (!HasInitialAssignmentEvent)
            {
                missing.Add("CC_ASSIGN");
            }
            if (!HasReassignmentEvent)
            {
                missing.Add("CC_REASSIGN");
            }
            return missing;
        }
    }
}

