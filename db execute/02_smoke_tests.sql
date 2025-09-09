SET NAMES utf8mb4;
USE yasgmp;

-- CAPA lifecycle smoke
INSERT INTO `capa_cases` (`component_id`, `date_open`, `reason`, `actions`, `status`)
VALUES (NULL, CURDATE(), 'SMOKE_CAPA', 'Initial action', 'otvoren');
SET @capa_id = LAST_INSERT_ID();
UPDATE `capa_cases` SET `status`='u_tijeku' WHERE `id`=@capa_id;
UPDATE `capa_cases` SET `status`='zatvoren' WHERE `id`=@capa_id;
DELETE FROM `capa_cases` WHERE `id`=@capa_id;

-- Deviations (minimal schema)
INSERT INTO `deviations` (`created_at`) VALUES (NOW());
SET @dev_id = LAST_INSERT_ID();
UPDATE `deviations` SET `updated_at`=NOW() WHERE `id`=@dev_id;
DELETE FROM `deviations` WHERE `id`=@dev_id;

-- System parameters (replaces 'settings')
INSERT INTO `system_parameters` (`param_name`,`param_value`,`updated_by`,`note`)
VALUES ('CLI_TEST_PARAM','123',1,'smoke')
ON DUPLICATE KEY UPDATE `param_value`=VALUES(`param_value`), `updated_by`=VALUES(`updated_by`), `updated_at`=CURRENT_TIMESTAMP, `note`=VALUES(`note`);
DELETE FROM `system_parameters` WHERE `param_name`='CLI_TEST_PARAM';

-- API keys & usage
INSERT INTO `api_keys` (`key_value`, `description`, `owner_id`, `is_active`) VALUES ('SMOKE_TEST_KEY', 'smoke', NULL, 1);
SET @key_id = LAST_INSERT_ID();
INSERT INTO `api_usage_log` (`api_key_id`, `user_id`, `call_time`, `endpoint`, `method`, `response_code`, `duration_ms`, `success`, `source_ip`)
VALUES (@key_id, NULL, NOW(), '/smoke', 'GET', 200, 12, 1, '127.0.0.1');
DELETE FROM `api_usage_log` WHERE `api_key_id`=@key_id;
DELETE FROM `api_keys` WHERE `id`=@key_id;

-- User login audit
INSERT INTO `user_login_audit` (`user_id`, `login_time`, `session_token`, `ip_address`, `device_info`, `success`, `reason`, `geo_location`, `risk_score`, `status`, `digital_signature`, `note`)
VALUES (NULL, NOW(), 'SMOKESESSION', '127.0.0.1', 'CLI', 1, NULL, 'Zagreb', 0.01, 'LOGIN', 'sig', 'smoke');
SET @ula = LAST_INSERT_ID();
UPDATE `user_login_audit` SET `success`=0, `reason`='fail' WHERE `id`=@ula;
DELETE FROM `user_login_audit` WHERE `id`=@ula;

