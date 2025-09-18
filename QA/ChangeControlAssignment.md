# Change Control Assignment QA Checklist

This guide captures the manual verification steps for the change-control assignment workflow. It focuses on
ensuring that both initial assignments and subsequent reassignments behave as expected and that every action
is written to `system_event_log` for Annex 11 / 21 CFR Part 11 traceability. Follow the three test flows
below (initial assignment, reassignment, and direct SQL validation) and confirm that diagnostics tooling
reports the expected audit events.

> **Scope**: These checks target the `ChangeControlViewModel.AssignChangeControlAsync` workflow and related audit
> instrumentation. They should be executed for every release that touches change control routing, audit, or
> diagnostics infrastructure.

## Prerequisites

1. **Environment**: A QA, staging, or developer instance of YasGMP with a reachable MySQL backend.
2. **Seed data**: At least one change-control record in status `Draft` or `UnderReview` to exercise the workflow.
   You can create one through the UI or by executing the `Initiate` action in the Change Control workspace.
3. **User permissions**: Sign in with an account that has role `qa`, `admin`, or `superadmin`. The assignment command
   is gated by these roles (`ChangeControlViewModel.CanManageChangeControl`).
4. **Audit visibility**: Confirm that the Audit Log page (Navigation: **Admin → Audit Log**) is accessible, or that you
   have SQL access to query `system_event_log` directly.
5. **Time window**: Perform the assignment and reassignment within the same session so the resulting audit entries
   are adjacent when sorted by timestamp.

## Audit expectations at a glance

| Scenario                  | Expected `event_type` | `field_name`      | `old_value`      | `new_value`      | Notes |
| ------------------------- | --------------------- | ----------------- | ---------------- | ---------------- | ----- |
| First-time assignment     | `CC_ASSIGN`           | `assigned_to_id`  | `NULL` / `∅`     | `<new user id>`  | Description includes `previous=no one` and the acting user's id/IP/session info. |
| Subsequent reassignment   | `CC_REASSIGN`         | `assigned_to_id`  | `<previous id>`  | `<new user id>`  | Description summarises the transition (`code=...; new=user ID ...; previous=user ID ...; actor=...`). |

Every manual run should produce both rows (in the above order) for the specific change-control id under test.

## Test 1 – Initial assignment

Goal: Assign an unassigned change control to a specific owner and confirm the UI and audit trail update.

1. Navigate to the **Change Control** workspace.
2. Select an unassigned change-control record (verify the "Assigned To" column or details pane shows `None`).
3. Choose an assignee (e.g., via the user picker/dropdown) and trigger the **Assign** action.
4. Observe the toast/status line. Expected message:
   
   > `Change control '<title>' assigned to user ID <id>.`
5. Reload the grid or details panel. The assigned user should now be visible.
6. Navigate to **Admin → Audit Log** (or query the database) and filter for:
   - `Table` = `change_controls`
   - `Event Type` = `CC_ASSIGN`
   - `Record Id` = the change-control id you acted on
7. Confirm the audit entry matches the first row in the expectations table above. Pay particular attention to
   the `old_value` (should be empty) and the `field_name` (`assigned_to_id`).

## Test 2 – Reassignment

Goal: Reassign the same record to a different user and verify old/new values are tracked.

1. On the same change control, pick a different assignee.
2. Trigger the **Assign** command again.
3. Confirm the status message updates to:
   
   > `Change control '<title>' reassigned from user ID <old> to user ID <new>.`
4. Refresh the change-control list to ensure the assignee reflects the new value.
5. In the Audit Log (or via SQL), filter for `Event Type = CC_REASSIGN`.
6. Validate the row aligns with the reassignment expectations in the table. Confirm the `old_value` matches
   the user id selected in Test 1 and that the description captures both the new and previous assignees.

## Test 3 – Direct system_event_log verification

When SQL access is available, run the following query after performing the above UI actions:

```sql
SELECT ts_utc,
       event_type,
       record_id,
       field_name,
       old_value,
       new_value,
       description
FROM   system_event_log
WHERE  table_name = 'change_controls'
  AND  record_id = <CHANGE_CONTROL_ID>
ORDER BY ts_utc DESC;
```

Expected results:

- Exactly two rows (unless additional historical assignments exist) corresponding to the table above.
- The most recent row is `CC_REASSIGN` with the correct `old_value`/`new_value` pair.
- The preceding row is `CC_ASSIGN` with `old_value` = `NULL` (or empty) and `new_value` = the initial assignee.
- Each entry includes the acting user's id, IP, device, and session metadata. Capture a screenshot of the SQL
  output for release records.

If any of the fields are missing or the event type is not recorded, investigate the assignment command handler
before releasing.

## Harness / Diagnostics validation (optional but recommended)

A lightweight harness backs these flows and can be executed without a live database:

1. Build the solution (`dotnet build`).
2. From the Debug dashboard inside the app (**Debug → Diagnostics Hub → Run Self Tests**), execute the self-test suite.
   - The harness emits either an INFO (`cc_assign_harness`) or WARN (`cc_assign_harness_missing_audit`) trace. Inspect the
     payload — `missingAuditEvents` must be empty and the boolean flags `hasInitialAssignmentEvent` and
     `hasReassignmentEvent` should both read `true`.
3. Alternatively, from a development shell you can run the harness directly using C# Interactive (requires the
   [dotnet-script](https://github.com/dotnet-script/dotnet-script) global tool — install via `dotnet tool install -g
   dotnet-script`):

   ```bash
   dotnet script -q -c "var result = await YasGMP.Diagnostics.ChangeControlAssignmentHarness.RunAsync();\nConsole.WriteLine($\"LoggedAudit={result.LoggedAudit}; Missing=[{string.Join(", ", result.MissingAuditEvents)}]\");"
   ```

   A passing run prints `LoggedAudit=True` and `Missing=[]`. For deeper inspection you can dump
   `result.LoggedEvents` to confirm both `CC_ASSIGN` and `CC_REASSIGN` entries along with their captured
   `old_value`/`new_value` pairs.

Document any anomalies (unexpected status messages, missing audit rows, SQL failures) in the release notes and block
shipping until the discrepancy is resolved.
