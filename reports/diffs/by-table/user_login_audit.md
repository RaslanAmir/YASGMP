# Table `user_login_audit`
Status: MISSING in schema.json
Used columns in code: device_info, digital_signature, geo_location, ip_address, login_time, note, reason, risk_score, session_token, status, success, user_id
\nSuggested DDL:
```sql
-- Suggested CREATE TABLE for missing table `user_login_audit`
CREATE TABLE `user_login_audit` (
  `id` INT NOT NULL AUTO_INCREMENT,
  note VARCHAR(255) NULL,
  geo_location VARCHAR(255) NULL,
  user_id INT NULL,
  risk_score INT NULL,
  device_info VARCHAR(255) NULL,
  reason VARCHAR(255) NULL,
  digital_signature VARCHAR(255) NULL,
  login_time DATETIME NULL,
  ip_address VARCHAR(255) NULL,
  status VARCHAR(255) NULL,
  session_token VARCHAR(255) NULL,
  success TINYINT(1) NULL
,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```
