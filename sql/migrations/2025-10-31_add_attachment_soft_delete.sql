-- YasGMP DB hardening: ensure attachments.soft_deleted_at exists and backfill
-- Safe to rerun (idempotent via information_schema checks and tolerant migrator)
SET @db = DATABASE();

DELIMITER $$

DROP PROCEDURE IF EXISTS add_col_if_missing$$
CREATE PROCEDURE add_col_if_missing(IN tbl VARCHAR(64), IN col VARCHAR(64), IN defn TEXT)
BEGIN
  IF EXISTS (
      SELECT 1 FROM information_schema.TABLES
      WHERE TABLE_SCHEMA=@db AND TABLE_NAME=tbl AND TABLE_TYPE='BASE TABLE'
  ) AND NOT EXISTS (
      SELECT 1 FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA=@db AND TABLE_NAME=tbl AND COLUMN_NAME=col
  ) THEN
    SET @sql = CONCAT('ALTER TABLE `', @db, '`.`', tbl, '` ADD COLUMN `', col, '` ', defn);
    PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;
  END IF;
END$$

-- Add soft_deleted_at to attachments if missing
CALL add_col_if_missing('attachments', 'soft_deleted_at', 'DATETIME NULL AFTER `is_deleted`')$$

-- Backfill soft_deleted_at for already soft-deleted rows (guarded)
DROP PROCEDURE IF EXISTS backfill_soft_deleted_at$$
CREATE PROCEDURE backfill_soft_deleted_at()
BEGIN
  IF EXISTS (
    SELECT 1 FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA=@db AND TABLE_NAME='attachments' AND COLUMN_NAME='soft_deleted_at'
  ) THEN
    UPDATE `attachments`
      SET soft_deleted_at = COALESCE(soft_deleted_at, uploaded_at)
    WHERE is_deleted = 1;
  END IF;
END$$
CALL backfill_soft_deleted_at()$$
DROP PROCEDURE backfill_soft_deleted_at$$

-- Helpful unique index used by EF queries
-- Will be ignored by migrator if already exists (1061)
CREATE UNIQUE INDEX ux_attachments_sha256_size ON `attachments` (`sha256`, `file_size`)$$

DROP PROCEDURE add_col_if_missing$$
DELIMITER ;
