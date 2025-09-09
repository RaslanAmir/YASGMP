SET NAMES utf8mb4;
USE yasgmp;

-- Create minimal reference tables used by CAPA triggers
CREATE TABLE IF NOT EXISTS `ref_domain` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(64) NOT NULL,
  `description` varchar(255) DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_ref_domain_name` (`name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `ref_value` (
  `id` int NOT NULL AUTO_INCREMENT,
  `domain_id` int NOT NULL,
  `code` varchar(255) NOT NULL,
  `label` varchar(255) DEFAULT NULL,
  `is_active` tinyint(1) DEFAULT '1',
  `sort_order` int DEFAULT '0',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_ref_value_domain_code` (`domain_id`,`code`),
  KEY `ix_ref_value_domain` (`domain_id`),
  CONSTRAINT `fk_ref_value_domain` FOREIGN KEY (`domain_id`) REFERENCES `ref_domain` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Helper: ensures domain/value exist and exposes value id via LAST_INSERT_ID()
DELIMITER $$
DROP PROCEDURE IF EXISTS `ref_touch` $$
CREATE PROCEDURE `ref_touch`(
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
END $$
DELIMITER ;
