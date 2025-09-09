SET NAMES utf8mb4;
USE yasgmp;

-- Helper: add a column if it doesn't exist (works on MySQL 5.7+)
DELIMITER $$
DROP PROCEDURE IF EXISTS add_column_if_missing $$
CREATE PROCEDURE add_column_if_missing(
    IN p_table  VARCHAR(64),
    IN p_column VARCHAR(64),
    IN p_def    TEXT
)
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
         WHERE TABLE_SCHEMA = DATABASE()
           AND TABLE_NAME   = p_table
           AND COLUMN_NAME  = p_column
    ) THEN
        SET @ddl = CONCAT('ALTER TABLE `', p_table, '` ADD COLUMN `', p_column, '` ', p_def);
        PREPARE stmt FROM @ddl;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;
END $$
DELIMITER ;

-- Align system_event_log with model extras used by the app
CALL add_column_if_missing('system_event_log','username',          'varchar(128) NULL');
CALL add_column_if_missing('system_event_log','digital_signature', 'varchar(256) NULL');
CALL add_column_if_missing('system_event_log','entry_hash',        'varchar(256) NULL');
CALL add_column_if_missing('system_event_log','mac_address',       'varchar(64)  NULL');
CALL add_column_if_missing('system_event_log','geo_location',      'varchar(128) NULL');
CALL add_column_if_missing('system_event_log','regulator',         'varchar(64)  NULL');
CALL add_column_if_missing('system_event_log','related_case_id',   'int          NULL');
CALL add_column_if_missing('system_event_log','related_case_type', 'varchar(64)  NULL');
CALL add_column_if_missing('system_event_log','anomaly_score',     'double       NULL');

-- Align user_login_audit with model extras used by the app
CALL add_column_if_missing('user_login_audit','two_factor_ok',     'tinyint(1)   NULL');
CALL add_column_if_missing('user_login_audit','sso_used',          'tinyint(1)   NULL');
CALL add_column_if_missing('user_login_audit','biometric_used',    'tinyint(1)   NULL');
CALL add_column_if_missing('user_login_audit','geo_location',      'varchar(128) NULL');
CALL add_column_if_missing('user_login_audit','risk_score',        'double       NULL');
CALL add_column_if_missing('user_login_audit','status',            'varchar(32)  NULL');
CALL add_column_if_missing('user_login_audit','digital_signature', 'varchar(128) NULL');
CALL add_column_if_missing('user_login_audit','note',              'text         NULL');

-- CAPA: ensure expected columns exist
CALL add_column_if_missing('capa_cases','component_id', 'int NULL');
CALL add_column_if_missing('capa_cases','date_open',    'date NULL');
CALL add_column_if_missing('capa_cases','date_close',   'date NULL');
CALL add_column_if_missing('capa_cases','reason',       'text NULL');
CALL add_column_if_missing('capa_cases','actions',      'text NULL');
CALL add_column_if_missing('capa_cases','doc_file',     'varchar(255) NULL');
CALL add_column_if_missing('capa_cases','status_id',    'int NULL');

-- Deviations: ensure minimal timestamps exist
CALL add_column_if_missing('deviations','created_at', 'datetime DEFAULT CURRENT_TIMESTAMP');
CALL add_column_if_missing('deviations','updated_at', 'datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP');
