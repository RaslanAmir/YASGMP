-- Recommended SQL patches generated 2025-09-05T09:53:50.1693895+02:00
-- Review carefully before applying to production.

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
