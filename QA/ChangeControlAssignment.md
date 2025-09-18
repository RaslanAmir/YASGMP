# Change Control Assignment QA Checklist

This guide captures the manual verification steps for the change-control assignment workflow. It focuses on
ensuring that both initial assignments and subsequent reassignments behave as expected and that every action
is written to `system_event_log` for Annex 11 / 21 CFR Part 11 traceability.

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
7. Confirm the audit entry shows:
   - `old_value` (or description `previous=none`) reflecting no prior assignment.
   - `new_value` matching the new assignee's user id.
   - Description includes the change control code and the acting user's id.

## Test 2 – Reassignment

Goal: Reassign the same record to a different user and verify old/new values are tracked.

1. On the same change control, pick a different assignee.
2. Trigger the **Assign** command again.
3. Confirm the status message updates to:
   
   > `Change control '<title>' reassigned from user ID <old> to user ID <new>.`
4. Refresh the change-control list to ensure the assignee reflects the new value.
5. In the Audit Log (or via SQL), filter for `Event Type = CC_REASSIGN`.
6. Validate the new row contains:
   - `old_value` = previous user id
   - `new_value` = the latest user id
   - Description string summarising the transition (e.g., `code=CC-2025-...; new=user ID 2002; previous=user ID 2001; actor=...`).

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

- The most recent row is `CC_REASSIGN` with `field_name = 'assigned_to_id'` and the correct `old_value`/`new_value` pair.
- The preceding row is `CC_ASSIGN` with `old_value` = `NULL` (or empty) and `new_value` = the initial assignee.
- Each entry includes the acting user's id, IP, and session metadata.

If any of the fields are missing or the event type is not recorded, investigate the assignment command handler
before releasing.

## Harness / Diagnostics validation (optional but recommended)

A lightweight harness backs these flows and can be executed without a live database:

1. Build the solution (`dotnet build`).
2. From the Debug dashboard inside the app (**Debug → Diagnostics Hub → Run Self Tests**), execute the self-test suite.
   - The new harness logs a `cc_assign_harness` entry that summarises the synthetic assignment and reassignment and
     confirms a `system_event_log` insert was emitted.
3. Alternatively, from a development shell you can run the harness directly using C# Interactive
   (requires the [dotnet-script](https://github.com/dotnet-script/dotnet-script) global tool — install via
   `dotnet tool install -g dotnet-script`):
   
   ```bash
   dotnet script -q -c "await YasGMP.Diagnostics.ChangeControlAssignmentHarness.RunAsync()"
   ```
   
   Examine the returned payload to ensure `LoggedAudit` is `true` and that `LoggedEvents` includes both `CC_ASSIGN`
   and `CC_REASSIGN` records.

Document any anomalies (unexpected status messages, missing audit rows, SQL failures) in the release notes and block
shipping until the discrepancy is resolved.
