using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.ViewModels;

namespace YasGMP.Diagnostics
{
    /// <summary>
    /// Utility harness that exercises <see cref="ChangeControlViewModel.AssignChangeControlAsync"/> with
    /// stubbed database/authentication services. It is designed so QA can validate SQL emitted during
    /// assignment workflows without a full UI automation stack.
    /// </summary>
    public static class ChangeControlAssignmentHarness
    {
        /// <summary>
        /// Runs the harness end-to-end: initial assignment followed by a reassignment. The harness captures
        /// executed SQL statements and audit metadata emitted via <c>system_event_log</c> insertions.
        /// </summary>
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
            var vm = new ChangeControlViewModel(db, auth);
            await vm.LoadChangeControlsAsync().ConfigureAwait(false);

            var first = vm.ChangeControls.FirstOrDefault();
            if (first == null)
            {
                db.ResetTestOverrides();
                return new ChangeControlAssignmentHarnessResult(statusMessages, auditEvents, executedSql);
            }

            vm.SelectedChangeControl = first;
            first.AssignedToId = auth.InitialAssigneeId;
            await vm.AssignChangeControlAsync().ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(vm.StatusMessage))
                statusMessages.Add(vm.StatusMessage!);

            var second = vm.ChangeControls.FirstOrDefault();
            if (second != null)
            {
                second.AssignedToId = auth.ReassignmentAssigneeId;
                vm.SelectedChangeControl = second;
                await vm.AssignChangeControlAsync().ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(vm.StatusMessage))
                    statusMessages.Add(vm.StatusMessage!);
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
            /// <summary>
            /// Initializes a new instance of the HarnessAuthContext class.
            /// </summary>
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
            /// <summary>
            /// Gets or sets the current user.
            /// </summary>

            public User? CurrentUser { get; }
            /// <summary>
            /// Gets or sets the current session id.
            /// </summary>
            public string CurrentSessionId { get; }
            /// <summary>
            /// Gets or sets the current device info.
            /// </summary>
            public string CurrentDeviceInfo { get; } = "Device=Harness;OS=Linux;App=Diagnostics";
            /// <summary>
            /// Gets or sets the current ip address.
            /// </summary>
            public string CurrentIpAddress { get; } = "127.0.0.1";
            /// <summary>
            /// Gets or sets the initial assignee id.
            /// </summary>

            public int InitialAssigneeId => 2001;
            /// <summary>
            /// Gets or sets the reassignment assignee id.
            /// </summary>
            public int ReassignmentAssigneeId => 2002;
        }
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
                missing.Add("CC_ASSIGN");
            if (!HasReassignmentEvent)
                missing.Add("CC_REASSIGN");
            return missing;
        }
    }
}
