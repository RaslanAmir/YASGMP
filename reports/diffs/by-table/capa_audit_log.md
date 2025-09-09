# Table `capa_audit_log`
Status: MISSING in schema.json
Used columns in code: action, capa_id, changed_at, details, digital_signature, user_id
\nSuggested DDL:
```sql
-- Suggested CREATE TABLE for missing table `capa_audit_log`
CREATE TABLE `capa_audit_log` (
  `id` INT NOT NULL AUTO_INCREMENT,
  digital_signature VARCHAR(255) NULL,
  action VARCHAR(255) NULL,
  capa_id INT NULL,
  changed_at DATETIME NULL,
  details VARCHAR(255) NULL,
  user_id INT NULL
,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```
