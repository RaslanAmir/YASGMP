-- YasGMP DB hardening: add source_ip/session_id/device_info columns where missing
-- Safe to rerun (idempotent via information_schema checks)
-- Target DB: change this if your schema name differs
SET @db = DATABASE();

DELIMITER $$

DROP PROCEDURE IF EXISTS add_col_if_missing$$
CREATE PROCEDURE add_col_if_missing(IN tbl VARCHAR(64), IN col VARCHAR(64), IN defn TEXT)
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.TABLES
             WHERE TABLE_SCHEMA=@db AND TABLE_NAME=tbl AND TABLE_TYPE='BASE TABLE')
     AND NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS
                     WHERE TABLE_SCHEMA=@db AND TABLE_NAME=tbl AND COLUMN_NAME=col) THEN
    SET @sql = CONCAT('ALTER TABLE `', @db, '`.`', tbl, '` ADD COLUMN `', col, '` ', defn);
    PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;
  END IF;
END$$

-- Helper: add trio (source_ip/session_id/device_info) to a table if missing
DROP PROCEDURE IF EXISTS add_trio_if_missing$$
CREATE PROCEDURE add_trio_if_missing(IN tbl VARCHAR(64))
BEGIN
  CALL add_col_if_missing(tbl, 'source_ip', 'VARCHAR(45) NULL');
  CALL add_col_if_missing(tbl, 'session_id', 'VARCHAR(100) NULL');
  CALL add_col_if_missing(tbl, 'device_info', 'VARCHAR(255) NULL');
END$$

-- Core tables used by WPF adapters and audits
CALL add_trio_if_missing('system_event_log');
CALL add_trio_if_missing('work_orders');
CALL add_trio_if_missing('work_order_comments');
CALL add_trio_if_missing('parts');
CALL add_trio_if_missing('warehouses');
CALL add_trio_if_missing('suppliers');
CALL add_trio_if_missing('validations');
CALL add_trio_if_missing('calibrations');
CALL add_trio_if_missing('incidents');
CALL add_trio_if_missing('change_controls');
CALL add_trio_if_missing('components');

-- Optional: some audit tables also record source_ip
CALL add_col_if_missing('validation_audit', 'source_ip', 'VARCHAR(45) NULL');
CALL add_col_if_missing('incident_audit', 'source_ip', 'VARCHAR(45) NULL');
CALL add_col_if_missing('export_print_log', 'source_ip', 'VARCHAR(45) NULL');

DROP PROCEDURE add_trio_if_missing$$
DROP PROCEDURE add_col_if_missing$$

DELIMITER ;

-- Verification: list any tables still missing source_ip
SELECT t.TABLE_NAME
FROM information_schema.TABLES t
LEFT JOIN information_schema.COLUMNS c
  ON c.TABLE_SCHEMA=t.TABLE_SCHEMA AND c.TABLE_NAME=t.TABLE_NAME AND c.COLUMN_NAME='source_ip'
WHERE t.TABLE_SCHEMA=@db AND c.COLUMN_NAME IS NULL
  AND t.TABLE_NAME IN ('system_event_log','work_orders','work_order_comments','parts','warehouses','suppliers','validations','calibrations','incidents','change_controls','components');
