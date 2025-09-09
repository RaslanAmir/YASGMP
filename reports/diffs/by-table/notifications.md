# Table `notifications`
Status: MISSING in schema.json
Used columns in code: acked_at, acked_by, created_at, device_info, entity, entity_id, ip_address, link, message, muted_until, priority, recipient_id, recipients, sender_id, session_id, status, title, type
\nSuggested DDL:
```sql
-- Suggested CREATE TABLE for missing table `notifications`
CREATE TABLE `notifications` (
  `id` INT NOT NULL AUTO_INCREMENT,
  type VARCHAR(255) NULL,
  priority VARCHAR(255) NULL,
  created_at DATETIME NULL,
  entity VARCHAR(255) NULL,
  acked_at DATETIME NULL,
  session_id INT NULL,
  recipients VARCHAR(255) NULL,
  muted_until VARCHAR(255) NULL,
  title VARCHAR(255) NULL,
  message VARCHAR(255) NULL,
  sender_id INT NULL,
  entity_id INT NULL,
  ip_address VARCHAR(255) NULL,
  acked_by VARCHAR(255) NULL,
  status VARCHAR(255) NULL,
  device_info VARCHAR(255) NULL,
  link VARCHAR(255) NULL,
  recipient_id INT NULL
,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```
