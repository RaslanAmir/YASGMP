# Table `incidents`
Status: MISSING in schema.json
Used columns in code: assigned_to_id, closed_at, closed_by_id, component_id, description, detected_at, digital_signature, last_modified, last_modified_by_id, machine_id, reported_at, reported_by_id, root_cause, source_ip, status, title, type
\nSuggested DDL:
```sql
-- Suggested CREATE TABLE for missing table `incidents`
CREATE TABLE `incidents` (
  `id` INT NOT NULL AUTO_INCREMENT,
  closed_at DATETIME NULL,
  closed_by_id INT NULL,
  assigned_to_id INT NULL,
  type VARCHAR(255) NULL,
  reported_by_id INT NULL,
  title VARCHAR(255) NULL,
  detected_at DATETIME NULL,
  digital_signature VARCHAR(255) NULL,
  last_modified_by_id INT NULL,
  description VARCHAR(255) NULL,
  status VARCHAR(255) NULL,
  machine_id INT NULL,
  root_cause VARCHAR(255) NULL,
  reported_at DATETIME NULL,
  component_id INT NULL,
  source_ip VARCHAR(255) NULL,
  last_modified VARCHAR(255) NULL
,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```
