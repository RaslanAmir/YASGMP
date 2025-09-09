# Table `logs`
Status: MISSING in schema.json
Used columns in code: cat, evt, json, lvl, msg, ts_utc
\nSuggested DDL:
```sql
-- Suggested CREATE TABLE for missing table `logs`
CREATE TABLE `logs` (
  `id` INT NOT NULL AUTO_INCREMENT,
  evt VARCHAR(255) NULL,
  json VARCHAR(255) NULL,
  ts_utc VARCHAR(255) NULL,
  cat VARCHAR(255) NULL,
  msg VARCHAR(255) NULL,
  lvl VARCHAR(255) NULL
,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```
