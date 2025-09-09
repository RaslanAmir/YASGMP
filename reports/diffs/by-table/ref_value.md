# Table `ref_value`
Status: MISSING in schema.json
Used columns in code: code, domain_id, is_active, label
\nSuggested DDL:
```sql
-- Suggested CREATE TABLE for missing table `ref_value`
CREATE TABLE `ref_value` (
  `id` INT NOT NULL AUTO_INCREMENT,
  label VARCHAR(255) NULL,
  code VARCHAR(255) NULL,
  is_active TINYINT(1) NULL,
  domain_id INT NULL
,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```
