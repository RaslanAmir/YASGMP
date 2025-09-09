-- Routines snapshot for yasgmp - 2025-09-05T07:34:23.9971995Z

-- PROCEDURE add_column_if_missing
CREATE DEFINER=`root`@`localhost` PROCEDURE `add_column_if_missing`(
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
END;

-- PROCEDURE add_fk_if_missing
CREATE DEFINER=`root`@`localhost` PROCEDURE `add_fk_if_missing`(IN p_tbl VARCHAR(64), IN p_fk VARCHAR(64), IN p_ddl TEXT)
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.tables
              WHERE table_schema = DATABASE() AND table_name = p_tbl) THEN
    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints
                    WHERE table_schema   = DATABASE()
                      AND table_name     = p_tbl
                      AND constraint_type = 'FOREIGN KEY'
                      AND constraint_name = p_fk) THEN
      SET @sql := p_ddl; PREPARE ps FROM @sql; EXECUTE ps; DEALLOCATE PREPARE ps;
    END IF;
  END IF;
END;

-- PROCEDURE add_index_if_missing
CREATE DEFINER=`root`@`localhost` PROCEDURE `add_index_if_missing`(IN p_tbl VARCHAR(64), IN p_idx VARCHAR(64), IN p_ddl TEXT)
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.tables
              WHERE table_schema = DATABASE() AND table_name = p_tbl) THEN
    IF NOT EXISTS (SELECT 1 FROM information_schema.statistics
                    WHERE table_schema = DATABASE()
                      AND table_name   = p_tbl
                      AND index_name   = p_idx) THEN
      SET @sql := p_ddl; PREPARE ps FROM @sql; EXECUTE ps; DEALLOCATE PREPARE ps;
    END IF;
  END IF;
END;

-- PROCEDURE backfill_catalog_ids
CREATE DEFINER=`root`@`localhost` PROCEDURE `backfill_catalog_ids`()
BEGIN
  
  UPDATE work_orders wo
  JOIN ref_domain d ON d.code='work_order_status'
  JOIN ref_value rv ON rv.domain_id=d.id AND rv.code=wo.status
  SET wo.status_id=rv.id
  WHERE wo.status IS NOT NULL;

  UPDATE work_orders wo
  JOIN ref_domain d ON d.code='work_order_type'
  JOIN ref_value rv ON rv.domain_id=d.id AND rv.code=wo.type
  SET wo.type_id=rv.id
  WHERE wo.type IS NOT NULL;

  UPDATE work_orders wo
  JOIN ref_domain d ON d.code='priority'
  JOIN ref_value rv ON rv.domain_id=d.id AND rv.code=wo.priority
  SET wo.priority_id=rv.id
  WHERE wo.priority IS NOT NULL;

  
  UPDATE inspections i
  JOIN ref_domain d1 ON d1.code='inspection_type'
  JOIN ref_value rv1 ON rv1.domain_id=d1.id AND rv1.code=i.type
  SET i.type_id=rv1.id
  WHERE i.type IS NOT NULL;

  UPDATE inspections i
  JOIN ref_domain d2 ON d2.code='inspection_result'
  JOIN ref_value rv2 ON rv2.domain_id=d2.id AND rv2.code=i.result
  SET i.result_id=rv2.id
  WHERE i.result IS NOT NULL;

  
  UPDATE calibrations c
  JOIN ref_domain d ON d.code='calibration_result'
  JOIN ref_value rv ON rv.domain_id=d.id AND rv.code=c.result
  SET c.result_id=rv.id
  WHERE c.result IS NOT NULL;

  
  UPDATE quality_events q
  JOIN ref_domain d ON d.code='quality_event_type'
  JOIN ref_value rv ON rv.domain_id=d.id AND rv.code=q.event_type
  SET q.type_id=rv.id
  WHERE q.event_type IS NOT NULL;

  UPDATE quality_events q
  JOIN ref_domain d ON d.code='quality_status'
  JOIN ref_value rv ON rv.domain_id=d.id AND rv.code=q.status
  SET q.status_id=rv.id
  WHERE q.status IS NOT NULL;

  
  UPDATE machines m
  JOIN ref_domain d ON d.code='asset_status'
  JOIN ref_value rv ON rv.domain_id=d.id AND rv.code=m.status
  SET m.status_id=rv.id
  WHERE m.status IS NOT NULL;

  UPDATE machines m
  JOIN ref_domain d ON d.code='lifecycle_phase'
  JOIN ref_value rv ON rv.domain_id=d.id AND rv.code=m.lifecycle_phase
  SET m.lifecycle_phase_id=rv.id
  WHERE m.lifecycle_phase IS NOT NULL;

  
  UPDATE suppliers s
  JOIN ref_domain d1 ON d1.code='supplier_status'
  JOIN ref_value rv1 ON rv1.domain_id=d1.id AND rv1.code=s.status
  SET s.status_id=rv1.id
  WHERE s.status IS NOT NULL;

  UPDATE suppliers s
  JOIN ref_domain d2 ON d2.code='supplier_type'
  JOIN ref_value rv2 ON rv2.domain_id=d2.id AND rv2.code=s.type
  SET s.type_id=rv2.id
  WHERE s.type IS NOT NULL;
END;

-- FUNCTION CURRENT_USER_ID
CREATE DEFINER=`root`@`localhost` FUNCTION `CURRENT_USER_ID`() RETURNS int
    DETERMINISTIC
    SQL SECURITY INVOKER
BEGIN
  
  RETURN NULL;
END;

-- PROCEDURE ensure_timestamps_all
CREATE DEFINER=`root`@`localhost` PROCEDURE `ensure_timestamps_all`()
BEGIN
  DECLARE done INT DEFAULT 0;
  DECLARE tbl VARCHAR(100);
  DECLARE cur CURSOR FOR
    SELECT TABLE_NAME
      FROM INFORMATION_SCHEMA.TABLES
     WHERE TABLE_SCHEMA = DATABASE()
       AND TABLE_TYPE = 'BASE TABLE';
  DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = 1;

  OPEN cur;
  read_loop: LOOP
    FETCH cur INTO tbl;
    IF done THEN
      LEAVE read_loop;
    END IF;

    IF NOT EXISTS (
      SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
       WHERE TABLE_SCHEMA = DATABASE()
         AND TABLE_NAME = tbl
         AND COLUMN_NAME = 'created_at'
    ) THEN
      SET @s = CONCAT('ALTER TABLE ', tbl,
                      ' ADD COLUMN created_at DATETIME DEFAULT CURRENT_TIMESTAMP');
      PREPARE stmt FROM @s; EXECUTE stmt; DEALLOCATE PREPARE stmt;
    END IF;

    IF NOT EXISTS (
      SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
       WHERE TABLE_SCHEMA = DATABASE()
         AND TABLE_NAME = tbl
         AND COLUMN_NAME = 'updated_at'
    ) THEN
      SET @s = CONCAT('ALTER TABLE ', tbl,
                      ' ADD COLUMN updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP');
      PREPARE stmt FROM @s; EXECUTE stmt; DEALLOCATE PREPARE stmt;
    END IF;

  END LOOP;
  CLOSE cur;
END;

-- FUNCTION fn_normalize_machine_status
CREATE DEFINER=`root`@`localhost` FUNCTION `fn_normalize_machine_status`(raw VARCHAR(64)) RETURNS varchar(32) CHARSET utf8mb4 COLLATE utf8mb4_unicode_ci
    DETERMINISTIC
BEGIN
  DECLARE s VARCHAR(64) DEFAULT TRIM(LOWER(IFNULL(raw,'')));
  RETURN
    CASE
      WHEN s IN ('maintenance','maint','service','van pogona','neispravan','kvar','servis') THEN 'maintenance'
      WHEN s IN ('decommissioned','decom','retired','dekomisioniran') THEN 'decommissioned'
      WHEN s IN ('reserved','rezerviran') THEN 'reserved'
      WHEN s IN ('scrapped','scrap','otpisan','rashodovan') THEN 'scrapped'
      WHEN s IN ('u pogonu','operativan','operational','active','') THEN 'active'
      ELSE 'active'
    END;
END;

-- PROCEDURE migrate_enum_to_fk
CREATE DEFINER=`root`@`localhost` PROCEDURE `migrate_enum_to_fk`(
    IN p_table        VARCHAR(64),
    IN p_old_col      VARCHAR(64),
    IN p_new_col      VARCHAR(64),
    IN p_domain_code  VARCHAR(50),
    IN p_default_code VARCHAR(100)
)
BEGIN
  DECLARE v_domain_id  INT;
  DECLARE v_fk_name    VARCHAR(128);

  
  SELECT id INTO v_domain_id
    FROM lookup_domain
   WHERE domain_code = p_domain_code;

  
  CALL add_column_if_missing(p_table, CONCAT('`',p_new_col,'`'),'INT NULL');

  
  SET @sql := CONCAT(
    'UPDATE ',p_table,' t ',
    'JOIN lookup_value v ',
    '  ON v.domain_id = ',v_domain_id,
    ' AND v.value_code COLLATE utf8mb4_unicode_ci = ',
    '     COALESCE(t.',p_old_col,',\'',p_default_code,'\') ',
    '       COLLATE utf8mb4_unicode_ci ',
    'SET t.',p_new_col,' = v.id ',
    'WHERE t.',p_new_col,' IS NULL');
  PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

  
  SET v_fk_name := CONCAT('fk_',p_table,'_',p_new_col);
  CALL add_fk_if_missing(
        p_table,
        v_fk_name,
        CONCAT(
          'ALTER TABLE ',p_table,
          ' ADD CONSTRAINT ',v_fk_name,
          ' FOREIGN KEY (',p_new_col,') REFERENCES lookup_value(id)')
  );
END;

-- PROCEDURE ref_touch
CREATE DEFINER=`root`@`localhost` PROCEDURE `ref_touch`(
    IN p_domain VARCHAR(64),
    IN p_code   VARCHAR(255),
    IN p_label  VARCHAR(255)
)
BEGIN
    DECLARE v_domain_id INT;
    DECLARE v_value_id  INT;

    -- ensure domain exists; set LAST_INSERT_ID to its id
    INSERT INTO ref_domain(`name`) VALUES (p_domain)
    ON DUPLICATE KEY UPDATE `id` = LAST_INSERT_ID(`id`);
    SET v_domain_id = LAST_INSERT_ID();

    -- ensure value exists; set LAST_INSERT_ID to its id (inserted or existing)
    INSERT INTO ref_value(`domain_id`,`code`,`label`,`is_active`)
    VALUES (v_domain_id, p_code, COALESCE(p_label, p_code), 1)
    ON DUPLICATE KEY UPDATE `id` = LAST_INSERT_ID(`id`), `label`=VALUES(`label`), `is_active`=1;
    SET v_value_id = LAST_INSERT_ID();

    -- expose value id via session LAST_INSERT_ID() without returning a result set
    DO LAST_INSERT_ID(v_value_id);
END;

-- PROCEDURE sp_emit_audit_triggers
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_emit_audit_triggers`(IN p_table VARCHAR(64))
BEGIN
  /*
    Emits a suggested 3-trigger audit block for p_table as a single TEXT column (ddl).
    Usage:
      CALL sp_emit_audit_triggers('your_table');
    Notes:
      - MySQL cannot PREPARE CREATE TRIGGER; copy the result into a script and run.
      - related_module defaults to CONCAT(p_table, 'Page').
  */
  DECLARE has_id        BOOL DEFAULT FALSE;
  DECLARE has_code      BOOL DEFAULT FALSE;
  DECLARE has_name      BOOL DEFAULT FALSE;
  DECLARE has_number    BOOL DEFAULT FALSE;
  DECLARE has_username  BOOL DEFAULT FALSE;

  SELECT COUNT(*)>0 INTO has_id       FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = p_table AND column_name = 'id';
  SELECT COUNT(*)>0 INTO has_code     FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = p_table AND column_name = 'code';
  SELECT COUNT(*)>0 INTO has_name     FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = p_table AND column_name = 'name';
  SELECT COUNT(*)>0 INTO has_number   FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = p_table AND column_name = 'number';
  SELECT COUNT(*)>0 INTO has_username FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = p_table AND column_name = 'username';

  SELECT CONCAT(
'-- ===== Triggers for ', p_table, ' =====\n',
'DROP TRIGGER IF EXISTS trg_', p_table, '_ai_audit;\n',
'DROP TRIGGER IF EXISTS trg_', p_table, '_au_audit;\n',
'DROP TRIGGER IF EXISTS trg_', p_table, '_ad_audit;\n',
'DELIMITER $$\n\n',

'CREATE TRIGGER trg_', p_table, '_ai_audit\n',
'AFTER INSERT ON ', p_table, '\nFOR EACH ROW\nBEGIN\n',
'  INSERT INTO system_event_log\n',
'    (user_id, event_type, table_name, related_module, record_id, field_name,\n',
'     old_value, new_value, description, source_ip, device_info, session_id, severity)\n',
'  VALUES\n',
'    (NULL, ''INSERT'', ''', p_table, ''', ''', p_table, 'Page'', NEW.', IF(has_id,'id','id'), ', NULL,\n',
'     NULL, NULL, CONCAT(''', p_table, ' insert''',
IF(has_code,     ', CONCAT(''; code='', COALESCE(NEW.code, ''''))', ''),
IF(has_name,     ', CONCAT(''; name='', COALESCE(NEW.name, ''''))', ''),
IF(has_number,   ', CONCAT(''; number='', COALESCE(NEW.number, ''''))', ''),
IF(has_username, ', CONCAT(''; username='', COALESCE(NEW.username, ''''))', ''),
'), ''system'', ''server'', '''', ''info'');\n',
'END$$\n\n',

'CREATE TRIGGER trg_', p_table, '_au_audit\n',
'AFTER UPDATE ON ', p_table, '\nFOR EACH ROW\nBEGIN\n',
'  INSERT INTO system_event_log\n',
'    (user_id, event_type, table_name, related_module, record_id, field_name,\n',
'     old_value, new_value, description, source_ip, device_info, session_id, severity)\n',
'  VALUES\n',
'    (NULL, ''UPDATE'', ''', p_table, ''', ''', p_table, 'Page'', NEW.', IF(has_id,'id','id'), ', NULL,\n',
'     NULL, NULL, CONCAT(''', p_table, ' update''',
IF(has_code,     ', CONCAT(''; code: '', COALESCE(OLD.code, ''''), '' → '', COALESCE(NEW.code, ''''))', ''),
IF(has_name,     ', CONCAT(''; name: '', COALESCE(OLD.name, ''''), '' → '', COALESCE(NEW.name, ''''))', ''),
IF(has_number,   ', CONCAT(''; number: '', COALESCE(OLD.number, ''''), '' → '', COALESCE(NEW.number, ''''))', ''),
IF(has_username, ', CONCAT(''; username: '', COALESCE(OLD.username, ''''), '' → '', COALESCE(NEW.username, ''''))', ''),
'), ''system'', ''server'', '''', ''info'');\n',
IF(has_code, CONCAT(
'  IF COALESCE(OLD.code, '''') <> COALESCE(NEW.code, '''') THEN\n',
'    INSERT INTO system_event_log (user_id, event_type, table_name, related_module, record_id, field_name, old_value, new_value, description, source_ip, device_info, session_id, severity)\n',
'    VALUES (NULL, ''UPDATE'', ''', p_table, ''', ''', p_table, 'Page'', NEW.', IF(has_id,'id','id'), ', ''code'', OLD.code, NEW.code, ''', p_table, ' update: code changed'', ''system'', ''server'', '''', ''info'');\n',
'  END IF;\n'), ''),
IF(has_name, CONCAT(
'  IF COALESCE(OLD.name, '''') <> COALESCE(NEW.name, '''') THEN\n',
'    INSERT INTO system_event_log (user_id, event_type, table_name, related_module, record_id, field_name, old_value, new_value, description, source_ip, device_info, session_id, severity)\n',
'    VALUES (NULL, ''UPDATE'', ''', p_table, ''', ''', p_table, 'Page'', NEW.', IF(has_id,'id','id'), ', ''name'', OLD.name, NEW.name, ''', p_table, ' update: name changed'', ''system'', ''server'', '''', ''info'');\n',
'  END IF;\n'), ''),
'END$$\n\n',

'CREATE TRIGGER trg_', p_table, '_ad_audit\n',
'AFTER DELETE ON ', p_table, '\nFOR EACH ROW\nBEGIN\n',
'  INSERT INTO system_event_log\n',
'    (user_id, event_type, table_name, related_module, record_id, field_name,\n',
'     old_value, new_value, description, source_ip, device_info, session_id, severity)\n',
'  VALUES\n',
'    (NULL, ''DELETE'', ''', p_table, ''', ''', p_table, 'Page'', OLD.', IF(has_id,'id','id'), ', NULL,\n',
'     NULL, NULL, CONCAT(''', p_table, ' delete''',
IF(has_code,     ', CONCAT(''; code='', COALESCE(OLD.code, ''''))', ''),
IF(has_name,     ', CONCAT(''; name='', COALESCE(OLD.name, ''''))', ''),
'), ''system'', ''server'', '''', ''warning'');\n',
'END$$\n\n',
'DELIMITER ;\n'
  ) AS ddl;
END;

-- PROCEDURE sp_inspect_triggers_for_resultsets
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_inspect_triggers_for_resultsets`(IN p_table VARCHAR(64))
BEGIN
  /*
    Scans trigger bodies for suspicious tokens that could emit result sets or do admin ops.
    Usage:
      CALL sp_inspect_triggers_for_resultsets('machines');
      CALL sp_inspect_triggers_for_resultsets(NULL); -- all tables
  */
  SELECT
      TRIGGER_NAME,
      EVENT_OBJECT_TABLE AS table_name,
      ACTION_TIMING,
      EVENT_MANIPULATION AS event_op,
      ACTION_STATEMENT
  FROM INFORMATION_SCHEMA.TRIGGERS
  WHERE TRIGGER_SCHEMA = DATABASE()
    AND (p_table IS NULL OR EVENT_OBJECT_TABLE = p_table)
    AND (
      ACTION_STATEMENT REGEXP '\\bSELECT\\b'
      OR ACTION_STATEMENT REGEXP '\\bSHOW\\b'
      OR ACTION_STATEMENT REGEXP '\\bCREATE\\s+TEMPORARY\\s+TABLE\\b'
      OR ACTION_STATEMENT REGEXP '\\bALTER\\s+TABLE\\b'
      OR ACTION_STATEMENT REGEXP '\\bDROP\\s+TABLE\\b'
    )
  ORDER BY EVENT_OBJECT_TABLE, TRIGGER_NAME;
END;

-- PROCEDURE sp_list_table_triggers
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_list_table_triggers`(IN p_table VARCHAR(64))
BEGIN
  /*
    CALL sp_list_table_triggers('machines');
  */
  SELECT
      TRIGGER_NAME,
      ACTION_TIMING,
      EVENT_MANIPULATION AS event_op
  FROM INFORMATION_SCHEMA.TRIGGERS
  WHERE TRIGGER_SCHEMA = DATABASE()
    AND EVENT_OBJECT_TABLE = p_table
  ORDER BY TRIGGER_NAME;
END;

-- PROCEDURE sp_log_calibration_export
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_log_calibration_export`(
    IN p_user_id INT,
    IN p_format ENUM('excel','pdf'),
    IN p_component_id INT,
    IN p_date_from DATE,
    IN p_date_to DATE,
    IN p_file_path VARCHAR(255)
)
BEGIN
    INSERT INTO calibration_export_log (
        user_id, export_format, filter_component_id, filter_date_from, filter_date_to, file_path
    ) VALUES (
        p_user_id, p_format, p_component_id, p_date_from, p_date_to, p_file_path
    );
END;
