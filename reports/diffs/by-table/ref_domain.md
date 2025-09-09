# Table `ref_domain`
Status: MISSING in schema.json
Used columns in code: name
\nSuggested DDL:
```sql
-- Suggested CREATE TABLE for missing table `ref_domain`
CREATE TABLE `ref_domain` (
  `id` INT NOT NULL AUTO_INCREMENT,
  name VARCHAR(255) NULL
,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```
