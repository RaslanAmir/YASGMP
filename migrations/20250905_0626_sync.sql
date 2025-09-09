-- YASGMP sync migration (idempotent)
-- Generated at (UTC): 2025-09-05T06:26:35Z
-- Baseline: d6a931bcbbcb9da2b2294f1bdd1a06512197a6335f71144175c56b71ef998fc2
-- Source of truth: tools/schema/snapshots/schema.json + schema.sql
-- Drift: none; applying baseline hardening (UTC timestamps, soft-delete guards, audit)

-- Note: MySQL 8.0 uses atomic DDL; CREATE/DROP of routines/triggers performs implicit commit.
-- Transaction wraps DML only; DDL remains atomic but not part of outer transaction.

START TRANSACTION;

-- 1) Ensure audit table exists (idempotent)
CREATE TABLE IF NOT EXISTS `audit_log` (
  `id` BIGINT NOT NULL AUTO_INCREMENT,
  `table_name` varchar(128) NOT NULL,
  `operation` enum('INSERT','UPDATE','DELETE') NOT NULL,
  `pk_json` json NULL,
  `old_data` json NULL,
  `new_data` json NULL,
  `actor` varchar(255) NULL,
  `event_time_utc` datetime NOT NULL DEFAULT UTC_TIMESTAMP(),
  PRIMARY KEY (`id`),
  KEY `idx_audit_table_time` (`table_name`,`event_time_utc`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

COMMIT;

-- 2) Generate idempotent audit, timestamp, and soft-delete triggers
SET @old_gcml = @@SESSION.group_concat_max_len;
SET SESSION group_concat_max_len = 1000000;

DROP PROCEDURE IF EXISTS `sp_yasgmp_sync_all`;
DELIMITER $$
CREATE PROCEDURE `sp_yasgmp_sync_all`()
BEGIN
  DECLARE done INT DEFAULT 0;
  DECLARE v_table VARCHAR(128);
  DECLARE v_has_created_at TINYINT DEFAULT 0;
  DECLARE v_has_updated_at TINYINT DEFAULT 0;
  DECLARE v_has_is_deleted TINYINT DEFAULT 0;
  DECLARE v_pk_list TEXT;
  DECLARE v_new_json LONGTEXT;
  DECLARE v_old_json LONGTEXT;
  DECLARE v_pk_json LONGTEXT;
  DECLARE v_schema VARCHAR(128);
  DECLARE v_ai_name VARCHAR(64);
  DECLARE v_au_name VARCHAR(64);
  DECLARE v_ad_name VARCHAR(64);
  DECLARE v_bi_name VARCHAR(64);
  DECLARE v_bu_name VARCHAR(64);
  DECLARE v_bd_name VARCHAR(64);
  DECLARE v_sql LONGTEXT;

  SET v_schema = DATABASE();

  -- Cursor for base tables excluding audit helper tables
  DECLARE cur CURSOR FOR
    SELECT t.table_name
    FROM information_schema.tables t
    WHERE t.table_schema = v_schema
      AND t.table_type = 'BASE TABLE'
      AND t.table_name NOT IN ('audit_log');
  DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = 1;

  OPEN cur;
  read_loop: LOOP
    FETCH cur INTO v_table;
    IF done = 1 THEN
      LEAVE read_loop;
    END IF;

    -- Detect standard columns
    SELECT COUNT(*) > 0 INTO v_has_created_at
    FROM information_schema.columns
    WHERE table_schema = v_schema AND table_name = v_table AND column_name = 'created_at';

    SELECT COUNT(*) > 0 INTO v_has_updated_at
    FROM information_schema.columns
    WHERE table_schema = v_schema AND table_name = v_table AND column_name = 'updated_at';

    SELECT COUNT(*) > 0 INTO v_has_is_deleted
    FROM information_schema.columns
    WHERE table_schema = v_schema AND table_name = v_table AND column_name = 'is_deleted';

    -- Build JSON object expressions (NEW and OLD), skipping binary/blob columns
    SELECT GROUP_CONCAT(CONCAT("'", c.column_name, "', NEW.`", c.column_name, "`") ORDER BY c.ordinal_position SEPARATOR ',') INTO v_new_json
    FROM information_schema.columns c
    WHERE c.table_schema = v_schema AND c.table_name = v_table
      AND c.data_type NOT IN ('blob','tinyblob','mediumblob','longblob','binary','varbinary');

    SELECT GROUP_CONCAT(CONCAT("'", c.column_name, "', OLD.`", c.column_name, "`") ORDER BY c.ordinal_position SEPARATOR ',') INTO v_old_json
    FROM information_schema.columns c
    WHERE c.table_schema = v_schema AND c.table_name = v_table
      AND c.data_type NOT IN ('blob','tinyblob','mediumblob','longblob','binary','varbinary');

    -- Primary key JSON pair list for NEW/OLD
    SELECT GROUP_CONCAT(CONCAT("'", k.column_name, "', NEW.`", k.column_name, "`") ORDER BY k.ordinal_position SEPARATOR ',') INTO v_pk_list
    FROM information_schema.key_column_usage k
    WHERE k.table_schema = v_schema AND k.table_name = v_table AND k.constraint_name = 'PRIMARY';

    -- If no PK defined, leave v_pk_list NULL

    -- Trigger names (truncate to avoid 64-char limit)
    SET v_ai_name = CONCAT('ai_', LEFT(v_table, 45), '_yasgmp_audit');
    SET v_au_name = CONCAT('au_', LEFT(v_table, 45), '_yasgmp_audit');
    SET v_ad_name = CONCAT('ad_', LEFT(v_table, 45), '_yasgmp_audit');
    SET v_bi_name = CONCAT('bi_', LEFT(v_table, 45), '_yasgmp_utc_ts');
    SET v_bu_name = CONCAT('bu_', LEFT(v_table, 45), '_yasgmp_utc_ts');
    SET v_bd_name = CONCAT('bd_', LEFT(v_table, 45), '_yasgmp_softdel');

    -- AFTER INSERT audit trigger
    IF (SELECT COUNT(*) FROM information_schema.triggers WHERE trigger_schema = v_schema AND trigger_name = v_ai_name) = 0 THEN
      SET v_sql = CONCAT(
        'CREATE TRIGGER `', v_ai_name, '` AFTER INSERT ON `', v_table, '`\n',
        'FOR EACH ROW\n',
        'BEGIN\n',
        '  INSERT INTO `audit_log` (`table_name`,`operation`,`pk_json`,`old_data`,`new_data`,`actor`,`event_time_utc`)\n',
        '  VALUES (\n',
        "    '", v_table, "', 'INSERT', ",
        '    ', IFNULL(CONCAT('JSON_OBJECT(', v_pk_list, ')'), 'NULL'), ',',
        '    NULL,',
        '    ', IFNULL(CONCAT('JSON_OBJECT(', v_new_json, ')'), 'NULL'), ',',
        '    CURRENT_USER(), UTC_TIMESTAMP()\n',
        '  );\n',
        'END'
      );
      PREPARE stmt FROM v_sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
    END IF;

    -- AFTER UPDATE audit trigger
    IF (SELECT COUNT(*) FROM information_schema.triggers WHERE trigger_schema = v_schema AND trigger_name = v_au_name) = 0 THEN
      SET v_sql = CONCAT(
        'CREATE TRIGGER `', v_au_name, '` AFTER UPDATE ON `', v_table, '`\n',
        'FOR EACH ROW\n',
        'BEGIN\n',
        '  INSERT INTO `audit_log` (`table_name`,`operation`,`pk_json`,`old_data`,`new_data`,`actor`,`event_time_utc`)\n',
        '  VALUES (\n',
        "    '", v_table, "', 'UPDATE', ",
        '    ', IFNULL(CONCAT('JSON_OBJECT(', v_pk_list, ')'), 'NULL'), ',',
        '    ', IFNULL(CONCAT('JSON_OBJECT(', v_old_json, ')'), 'NULL'), ',',
        '    ', IFNULL(CONCAT('JSON_OBJECT(', v_new_json, ')'), 'NULL'), ',',
        '    CURRENT_USER(), UTC_TIMESTAMP()\n',
        '  );\n',
        'END'
      );
      PREPARE stmt FROM v_sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
    END IF;

    -- AFTER DELETE audit trigger
    IF (SELECT COUNT(*) FROM information_schema.triggers WHERE trigger_schema = v_schema AND trigger_name = v_ad_name) = 0 THEN
      SET v_sql = CONCAT(
        'CREATE TRIGGER `', v_ad_name, '` AFTER DELETE ON `', v_table, '`\n',
        'FOR EACH ROW\n',
        'BEGIN\n',
        '  INSERT INTO `audit_log` (`table_name`,`operation`,`pk_json`,`old_data`,`new_data`,`actor`,`event_time_utc`)\n',
        '  VALUES (\n',
        "    '", v_table, "', 'DELETE', ",
        '    ', IFNULL(CONCAT('JSON_OBJECT(', REPLACE(v_pk_list, 'NEW.', 'OLD.'), ')'), 'NULL'), ',',
        '    ', IFNULL(CONCAT('JSON_OBJECT(', v_old_json, ')'), 'NULL'), ',',
        '    NULL,',
        '    CURRENT_USER(), UTC_TIMESTAMP()\n',
        '  );\n',
        'END'
      );
      PREPARE stmt FROM v_sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
    END IF;

    -- BEFORE INSERT UTC timestamps (if columns exist)
    IF v_has_created_at = 1 OR v_has_updated_at = 1 THEN
      IF (SELECT COUNT(*) FROM information_schema.triggers WHERE trigger_schema = v_schema AND trigger_name = v_bi_name) = 0 THEN
        SET v_sql = CONCAT(
          'CREATE TRIGGER `', v_bi_name, '` BEFORE INSERT ON `', v_table, '`\n',
          'FOR EACH ROW\n',
          'BEGIN\n',
          '  IF COALESCE(@yasgmp_disable_ts_triggers,0) = 1 THEN\n',
          '    SET @yasgmp_disable_ts_triggers = @yasgmp_disable_ts_triggers;\n',
          '  ELSE\n',
          IF(v_has_created_at = 1, '    IF NEW.`created_at` IS NULL THEN SET NEW.`created_at` = UTC_TIMESTAMP(); END IF;\n', ''),
          IF(v_has_updated_at = 1, '    IF NEW.`updated_at` IS NULL THEN SET NEW.`updated_at` = UTC_TIMESTAMP(); END IF;\n', ''),
          '  END IF;\n',
          'END'
        );
        PREPARE stmt FROM v_sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
      END IF;
    END IF;

    -- BEFORE UPDATE UTC timestamps (if updated_at exists)
    IF v_has_updated_at = 1 THEN
      IF (SELECT COUNT(*) FROM information_schema.triggers WHERE trigger_schema = v_schema AND trigger_name = v_bu_name) = 0 THEN
        SET v_sql = CONCAT(
          'CREATE TRIGGER `', v_bu_name, '` BEFORE UPDATE ON `', v_table, '`\n',
          'FOR EACH ROW\n',
          'BEGIN\n',
          '  IF COALESCE(@yasgmp_disable_ts_triggers,0) = 1 THEN\n',
          '    SET @yasgmp_disable_ts_triggers = @yasgmp_disable_ts_triggers;\n',
          '  ELSE\n',
          '    SET NEW.`updated_at` = UTC_TIMESTAMP();\n',
          '  END IF;\n',
          'END'
        );
        PREPARE stmt FROM v_sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
      END IF;
    END IF;

    -- BEFORE DELETE soft-delete guard (only if is_deleted exists)
    IF v_has_is_deleted = 1 THEN
      IF (SELECT COUNT(*) FROM information_schema.triggers WHERE trigger_schema = v_schema AND trigger_name = v_bd_name) = 0 THEN
        SET v_sql = CONCAT(
          'CREATE TRIGGER `', v_bd_name, '` BEFORE DELETE ON `', v_table, '`\n',
          'FOR EACH ROW\n',
          'BEGIN\n',
          '  IF COALESCE(@yasgmp_allow_hard_delete,0) = 1 THEN\n',
          '    SET @yasgmp_allow_hard_delete = @yasgmp_allow_hard_delete;\n',
          '  ELSE\n',
          "    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = '",
          'Hard delete blocked; use soft delete (set is_deleted=1) or set @yasgmp_allow_hard_delete=1 to override',
          "';\n",
          '  END IF;\n',
          'END'
        );
        PREPARE stmt FROM v_sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
      END IF;
    END IF;

  END LOOP;
  CLOSE cur;
END$$
DELIMITER ;

-- Execute and cleanup
CALL `sp_yasgmp_sync_all`();
DROP PROCEDURE IF EXISTS `sp_yasgmp_sync_all`;
SET SESSION group_concat_max_len = @old_gcml;
