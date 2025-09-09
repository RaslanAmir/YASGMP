# Table `units`
Status: MISSING in schema.json
Used columns in code: code, name, quantity
\nSuggested DDL:
```sql
-- Suggested CREATE TABLE for missing table `units`
CREATE TABLE `units` (
  `id` INT NOT NULL AUTO_INCREMENT,
  code VARCHAR(255) NULL,
  name VARCHAR(255) NULL,
  quantity VARCHAR(255) NULL
,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```
