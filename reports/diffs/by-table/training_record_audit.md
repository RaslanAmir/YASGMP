# Table `training_record_audit`
Status: MISSING in schema.json
Used columns in code: action, description, device_info, session_id, source_ip, timestamp, training_record_id
\nSuggested DDL:
```sql
-- Suggested CREATE TABLE for missing table `training_record_audit`
CREATE TABLE `training_record_audit` (
  `id` INT NOT NULL AUTO_INCREMENT,
  action VARCHAR(255) NULL,
  source_ip VARCHAR(255) NULL,
  description VARCHAR(255) NULL,
  session_id INT NULL,
  timestamp VARCHAR(255) NULL,
  training_record_id INT NULL,
  device_info VARCHAR(255) NULL
,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```
