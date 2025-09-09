# Table `api_usage_log`
Status: MISSING in schema.json
Used columns in code: api_key_id, call_time, duration_ms, endpoint, method, response_code, source_ip, success, user_id
\nSuggested DDL:
```sql
-- Suggested CREATE TABLE for missing table `api_usage_log`
CREATE TABLE `api_usage_log` (
  `id` INT NOT NULL AUTO_INCREMENT,
  response_code VARCHAR(255) NULL,
  api_key_id INT NULL,
  method VARCHAR(255) NULL,
  success TINYINT(1) NULL,
  source_ip VARCHAR(255) NULL,
  call_time DATETIME NULL,
  duration_ms INT NULL,
  endpoint VARCHAR(255) NULL,
  user_id INT NULL
,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```
