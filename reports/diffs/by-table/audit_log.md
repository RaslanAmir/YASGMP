# Table `audit_log`
Status: MISSING in schema.json
Used columns in code: action, details, entity, entity_id, timestamp, user_id, user_name
\nSuggested DDL:
```sql
-- Suggested CREATE TABLE for missing table `audit_log`
CREATE TABLE `audit_log` (
  `id` INT NOT NULL AUTO_INCREMENT,
  action VARCHAR(255) NULL,
  user_name VARCHAR(255) NULL,
  entity_id INT NULL,
  details VARCHAR(255) NULL,
  timestamp VARCHAR(255) NULL,
  entity VARCHAR(255) NULL,
  user_id INT NULL
,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```
