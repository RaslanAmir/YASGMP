-- YasGMP DB hardening: attachments encryption columns + attachment_embeddings table
-- Safe to rerun (idempotent checks via information_schema and IF NOT EXISTS)
-- Ensure required columns on attachments table exist (MySQL 8+ supports IF NOT EXISTS)
ALTER TABLE `attachments` ADD COLUMN `encrypted` TINYINT(1) NOT NULL DEFAULT 0;
ALTER TABLE `attachments` ADD COLUMN `encryption_metadata` TEXT NULL;
ALTER TABLE `attachments` ADD COLUMN `ocr_text` LONGTEXT NULL;
ALTER TABLE `attachments` ADD COLUMN `ai_score` DOUBLE NULL;
ALTER TABLE `attachments` ADD COLUMN `chain_id` VARCHAR(128) NULL;
ALTER TABLE `attachments` ADD COLUMN `version_uid` VARCHAR(128) NULL;
ALTER TABLE `attachments` ADD COLUMN `device_info` VARCHAR(256) NULL;
ALTER TABLE `attachments` ADD COLUMN `session_id` VARCHAR(128) NULL;
ALTER TABLE `attachments` ADD COLUMN `ip_address` VARCHAR(64) NULL;
ALTER TABLE `attachments` ADD COLUMN `status` VARCHAR(64) NULL;
ALTER TABLE `attachments` ADD COLUMN `file_content` LONGBLOB NULL;

-- Create attachment_embeddings table if it does not exist
CREATE TABLE IF NOT EXISTS `attachment_embeddings` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `attachment_id` INT NOT NULL,
  `model` VARCHAR(128) NOT NULL,
  `dimension` INT NOT NULL,
  `vector` LONGBLOB NOT NULL,
  `source_sha256` VARCHAR(64) NULL,
  `created_at` DATETIME NULL,
  PRIMARY KEY (`id`),
  KEY `idx_embedding_attachment` (`attachment_id`),
  KEY `idx_embedding_model` (`model`),
  UNIQUE KEY `ux_attachment_model` (`attachment_id`,`model`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
