-- YASGMP sync migration (idempotent, non-destructive)
-- Generated at (UTC): 2025-09-05T08:35:13Z
-- Context: OBS-004 — Reconcile code↔DB; add compatibility views
-- Summary: Add guarded compatibility views for warehouse→warehouses, role→roles, incidents→incident_log, units→measurement_units. No drops.

-- Notes:
-- - Uses dynamic SQL with information_schema guards to avoid overriding base tables.
-- - CREATE OR REPLACE VIEW is safe and idempotent for views; no data is altered.
-- - If a target base table is missing, the step no-ops (SELECT 1).
-- - MySQL may auto-commit DDL (e.g., CREATE VIEW). Wrapping in START TRANSACTION/COMMIT
--   does not guarantee atomicity for DDL, so this script intentionally avoids a global
--   transaction and relies on per-statement guards for safety.

SET @db := DATABASE();

-- Compatibility view: `warehouse` -> `warehouses`
SET @sql := IF(
  EXISTS(
    SELECT 1 FROM information_schema.tables
    WHERE table_schema = @db AND table_name = 'warehouse' AND table_type = 'BASE TABLE'
  ),
  'SELECT 1',
  IF(
    EXISTS(
      SELECT 1 FROM information_schema.tables
      WHERE table_schema = @db AND table_name = 'warehouses' AND table_type = 'BASE TABLE'
    ),
    'CREATE OR REPLACE VIEW `warehouse` AS SELECT * FROM `warehouses`',
    'SELECT 1'
  )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- Compatibility view: `role` -> `roles`
SET @sql := IF(
  EXISTS(
    SELECT 1 FROM information_schema.tables
    WHERE table_schema = @db AND table_name = 'role' AND table_type = 'BASE TABLE'
  ),
  'SELECT 1',
  IF(
    EXISTS(
      SELECT 1 FROM information_schema.tables
      WHERE table_schema = @db AND table_name = 'roles' AND table_type = 'BASE TABLE'
    ),
    'CREATE OR REPLACE VIEW `role` AS SELECT * FROM `roles`',
    'SELECT 1'
  )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- Compatibility view: `incidents` -> `incident_log`
SET @sql := IF(
  EXISTS(
    SELECT 1 FROM information_schema.tables
    WHERE table_schema = @db AND table_name = 'incidents' AND table_type = 'BASE TABLE'
  ),
  'SELECT 1',
  IF(
    EXISTS(
      SELECT 1 FROM information_schema.tables
      WHERE table_schema = @db AND table_name = 'incident_log' AND table_type = 'BASE TABLE'
    ),
    'CREATE OR REPLACE VIEW `incidents` AS SELECT * FROM `incident_log`',
    'SELECT 1'
  )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- Compatibility view: `units` -> `measurement_units`
SET @sql := IF(
  EXISTS(
    SELECT 1 FROM information_schema.tables
    WHERE table_schema = @db AND table_name = 'units' AND table_type = 'BASE TABLE'
  ),
  'SELECT 1',
  IF(
    EXISTS(
      SELECT 1 FROM information_schema.tables
      WHERE table_schema = @db AND table_name = 'measurement_units' AND table_type = 'BASE TABLE'
    ),
    'CREATE OR REPLACE VIEW `units` AS SELECT * FROM `measurement_units`',
    'SELECT 1'
  )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- Explicitly no-op for SQLite-only `logs` references (diagnostic sink)
-- See SYNC_REPORT.md for rationale.

-- Compatibility view: `notifications` -> `notification_queue`
SET @sql := IF(
  EXISTS(
    SELECT 1 FROM information_schema.tables
    WHERE table_schema = @db AND table_name = 'notifications' AND table_type = 'BASE TABLE'
  ),
  'SELECT 1',
  IF(
    EXISTS(
      SELECT 1 FROM information_schema.tables
      WHERE table_schema = @db AND table_name = 'notification_queue' AND table_type = 'BASE TABLE'
    ),
    'CREATE OR REPLACE VIEW `notifications` AS SELECT * FROM `notification_queue`',
    'SELECT 1'
  )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- Compatibility view: `audit_log` -> `system_event_log`
SET @sql := IF(
  EXISTS(
    SELECT 1 FROM information_schema.tables
    WHERE table_schema = @db AND table_name = 'audit_log' AND table_type = 'BASE TABLE'
  ),
  'SELECT 1',
  IF(
    EXISTS(
      SELECT 1 FROM information_schema.tables
      WHERE table_schema = @db AND table_name = 'system_event_log' AND table_type = 'BASE TABLE'
    ),
    'CREATE OR REPLACE VIEW `audit_log` AS SELECT * FROM `system_event_log`',
    'SELECT 1'
  )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- Compatibility view: `capa_audit_log` -> `capa_status_history`
SET @sql := IF(
  EXISTS(
    SELECT 1 FROM information_schema.tables
    WHERE table_schema = @db AND table_name = 'capa_audit_log' AND table_type = 'BASE TABLE'
  ),
  'SELECT 1',
  IF(
    EXISTS(
      SELECT 1 FROM information_schema.tables
      WHERE table_schema = @db AND table_name = 'capa_status_history' AND table_type = 'BASE TABLE'
    ),
    'CREATE OR REPLACE VIEW `capa_audit_log` AS SELECT * FROM `capa_status_history`',
    'SELECT 1'
  )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
