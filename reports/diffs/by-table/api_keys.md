# Table `api_keys`
Status: MISSING in schema.json
Used columns in code: description, is_active, key_value, owner_id
\nSuggested DDL:
```sql
-- Suggested CREATE TABLE for missing table `api_keys`
CREATE TABLE `api_keys` (
  `id` INT NOT NULL AUTO_INCREMENT,
  key_value VARCHAR(255) NULL,
  description VARCHAR(255) NULL,
  is_active TINYINT(1) NULL,
  owner_id INT NULL
,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```
