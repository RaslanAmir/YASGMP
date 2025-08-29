-- MySQL dump 10.13  Distrib 8.0.43, for Win64 (x86_64)
--
-- Host: localhost    Database: yasgmp
-- ------------------------------------------------------
-- Server version	8.0.43

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Current Database: `yasgmp`
--

CREATE DATABASE /*!32312 IF NOT EXISTS*/ `yasgmp` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;

USE `yasgmp`;

--
-- Table structure for table `admin_activity_log`
--

DROP TABLE IF EXISTS `admin_activity_log`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `admin_activity_log` (
  `id` int NOT NULL AUTO_INCREMENT,
  `admin_id` int DEFAULT NULL,
  `activity_time` datetime DEFAULT CURRENT_TIMESTAMP,
  `activity` varchar(255) DEFAULT NULL,
  `affected_table` varchar(100) DEFAULT NULL,
  `affected_record_id` int DEFAULT NULL,
  `details` text,
  `source_ip` varchar(45) DEFAULT NULL,
  `note` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `timestamp` datetime GENERATED ALWAYS AS (`activity_time`) VIRTUAL,
  `action` varchar(255) GENERATED ALWAYS AS (`activity`) VIRTUAL,
  PRIMARY KEY (`id`),
  KEY `fk_adminact_user` (`admin_id`),
  CONSTRAINT `fk_adminact_user` FOREIGN KEY (`admin_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `admin_activity_log`
--

LOCK TABLES `admin_activity_log` WRITE;
/*!40000 ALTER TABLE `admin_activity_log` DISABLE KEYS */;
/*!40000 ALTER TABLE `admin_activity_log` ENABLE KEYS */;
UNLOCK TABLES;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_admin_activity_log_fk_guard` BEFORE INSERT ON `admin_activity_log` FOR EACH ROW BEGIN
  IF NEW.admin_id IS NOT NULL THEN
    IF (SELECT COUNT(*) FROM users WHERE id = NEW.admin_id) = 0 THEN
      SET NEW.admin_id = NULL;
    END IF;
  END IF;
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Table structure for table `api_audit_log`
--

DROP TABLE IF EXISTS `api_audit_log`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `api_audit_log` (
  `id` int NOT NULL AUTO_INCREMENT,
  `api_key_id` int DEFAULT NULL,
  `user_id` int DEFAULT NULL,
  `action` varchar(255) DEFAULT NULL,
  `timestamp` datetime DEFAULT CURRENT_TIMESTAMP,
  `ip_address` varchar(45) DEFAULT NULL,
  `request_details` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `details` text GENERATED ALWAYS AS (`request_details`) VIRTUAL,
  PRIMARY KEY (`id`),
  KEY `fk_api_audit_key` (`api_key_id`),
  KEY `fk_api_audit_user` (`user_id`),
  CONSTRAINT `fk_api_audit_key` FOREIGN KEY (`api_key_id`) REFERENCES `api_keys` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_api_audit_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `api_audit_log`
--

LOCK TABLES `api_audit_log` WRITE;
/*!40000 ALTER TABLE `api_audit_log` DISABLE KEYS */;
/*!40000 ALTER TABLE `api_audit_log` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `api_keys`
--

DROP TABLE IF EXISTS `api_keys`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `api_keys` (
  `id` int NOT NULL AUTO_INCREMENT,
  `key_value` varchar(255) NOT NULL,
  `description` varchar(255) DEFAULT NULL,
  `owner_id` int DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `is_active` tinyint(1) DEFAULT '1',
  `last_used_at` datetime DEFAULT NULL,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `key_value` (`key_value`),
  KEY `fk_apikey_owner` (`owner_id`),
  CONSTRAINT `fk_apikey_owner` FOREIGN KEY (`owner_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `api_keys`
--

LOCK TABLES `api_keys` WRITE;
/*!40000 ALTER TABLE `api_keys` DISABLE KEYS */;
/*!40000 ALTER TABLE `api_keys` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `api_usage_log`
--

DROP TABLE IF EXISTS `api_usage_log`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `api_usage_log` (
  `id` int NOT NULL AUTO_INCREMENT,
  `api_key_id` int DEFAULT NULL,
  `user_id` int DEFAULT NULL,
  `call_time` datetime DEFAULT CURRENT_TIMESTAMP,
  `endpoint` varchar(255) DEFAULT NULL,
  `method` varchar(20) DEFAULT NULL,
  `params` text,
  `response_code` int DEFAULT NULL,
  `duration_ms` int DEFAULT NULL,
  `success` tinyint(1) DEFAULT '1',
  `error_message` text,
  `source_ip` varchar(45) DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `timestamp` datetime GENERATED ALWAYS AS (`call_time`) VIRTUAL,
  `action` varchar(20) GENERATED ALWAYS AS (`method`) VIRTUAL,
  `details` text GENERATED ALWAYS AS (coalesce(`error_message`,`params`)) VIRTUAL,
  PRIMARY KEY (`id`),
  KEY `fk_api_usage_key` (`api_key_id`),
  KEY `fk_api_usage_user` (`user_id`),
  CONSTRAINT `fk_api_usage_key` FOREIGN KEY (`api_key_id`) REFERENCES `api_keys` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_api_usage_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `api_usage_log`
--

LOCK TABLES `api_usage_log` WRITE;
/*!40000 ALTER TABLE `api_usage_log` DISABLE KEYS */;
/*!40000 ALTER TABLE `api_usage_log` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `attachments`
--

DROP TABLE IF EXISTS `attachments`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `attachments` (
  `id` int NOT NULL AUTO_INCREMENT,
  `related_table` varchar(50) DEFAULT NULL,
  `related_id` int DEFAULT NULL,
  `file_name` varchar(255) DEFAULT NULL,
  `file_path` varchar(255) DEFAULT NULL,
  `file_type` varchar(40) DEFAULT NULL,
  `uploaded_by` int DEFAULT NULL,
  `uploaded_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `note` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_att_user` (`uploaded_by`),
  CONSTRAINT `fk_att_user` FOREIGN KEY (`uploaded_by`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `attachments`
--

LOCK TABLES `attachments` WRITE;
/*!40000 ALTER TABLE `attachments` DISABLE KEYS */;
/*!40000 ALTER TABLE `attachments` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `buildings`
--

DROP TABLE IF EXISTS `buildings`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `buildings` (
  `id` int NOT NULL AUTO_INCREMENT,
  `site_id` int NOT NULL,
  `code` varchar(20) DEFAULT NULL,
  `name` varchar(100) DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_build_site` (`site_id`),
  CONSTRAINT `fk_build_site` FOREIGN KEY (`site_id`) REFERENCES `sites` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `buildings`
--

LOCK TABLES `buildings` WRITE;
/*!40000 ALTER TABLE `buildings` DISABLE KEYS */;
/*!40000 ALTER TABLE `buildings` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `calibration_audit_log`
--

DROP TABLE IF EXISTS `calibration_audit_log`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `calibration_audit_log` (
  `id` int NOT NULL AUTO_INCREMENT,
  `calibration_id` int DEFAULT NULL,
  `user_id` int DEFAULT NULL,
  `action` enum('CREATE','UPDATE','DELETE','EXPORT') NOT NULL,
  `old_value` text,
  `new_value` text,
  `changed_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `source_ip` varchar(45) DEFAULT NULL,
  `note` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `timestamp` datetime GENERATED ALWAYS AS (`changed_at`) VIRTUAL,
  `details` text GENERATED ALWAYS AS (`note`) VIRTUAL,
  PRIMARY KEY (`id`),
  KEY `fk_cal_audit_cal` (`calibration_id`),
  KEY `fk_cal_audit_user` (`user_id`),
  CONSTRAINT `fk_cal_audit_cal` FOREIGN KEY (`calibration_id`) REFERENCES `calibrations` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_cal_audit_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `calibration_audit_log`
--

LOCK TABLES `calibration_audit_log` WRITE;
/*!40000 ALTER TABLE `calibration_audit_log` DISABLE KEYS */;
/*!40000 ALTER TABLE `calibration_audit_log` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `calibration_export_log`
--

DROP TABLE IF EXISTS `calibration_export_log`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `calibration_export_log` (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_id` int DEFAULT NULL,
  `export_time` datetime DEFAULT CURRENT_TIMESTAMP,
  `export_format` enum('excel','pdf') DEFAULT NULL,
  `filter_component_id` int DEFAULT NULL,
  `filter_date_from` date DEFAULT NULL,
  `filter_date_to` date DEFAULT NULL,
  `file_path` varchar(255) DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `timestamp` datetime GENERATED ALWAYS AS (`export_time`) VIRTUAL,
  `details` text GENERATED ALWAYS AS (`file_path`) VIRTUAL,
  PRIMARY KEY (`id`),
  KEY `fk_cel_user` (`user_id`),
  CONSTRAINT `fk_cel_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `calibration_export_log`
--

LOCK TABLES `calibration_export_log` WRITE;
/*!40000 ALTER TABLE `calibration_export_log` DISABLE KEYS */;
/*!40000 ALTER TABLE `calibration_export_log` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `calibration_sensors`
--

DROP TABLE IF EXISTS `calibration_sensors`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `calibration_sensors` (
  `id` int NOT NULL AUTO_INCREMENT,
  `component_id` int DEFAULT NULL,
  `sensor_type` enum('temperatura','tlak','vlaga','protok','drugo') DEFAULT NULL,
  `range_min` decimal(10,2) DEFAULT NULL,
  `range_max` decimal(10,2) DEFAULT NULL,
  `unit` varchar(20) DEFAULT NULL,
  `calibration_interval_days` int DEFAULT NULL,
  `iot_device_id` varchar(80) DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `sensor_type_id` int DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_cs_component` (`component_id`),
  KEY `fk_cs_sensor_type` (`sensor_type_id`),
  CONSTRAINT `fk_cs_component` FOREIGN KEY (`component_id`) REFERENCES `machine_components` (`id`),
  CONSTRAINT `fk_cs_sensor_type` FOREIGN KEY (`sensor_type_id`) REFERENCES `sensor_types` (`id`),
  CONSTRAINT `fk_cs_sensortype` FOREIGN KEY (`sensor_type_id`) REFERENCES `ref_value` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `calibration_sensors`
--

LOCK TABLES `calibration_sensors` WRITE;
/*!40000 ALTER TABLE `calibration_sensors` DISABLE KEYS */;
/*!40000 ALTER TABLE `calibration_sensors` ENABLE KEYS */;
UNLOCK TABLES;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_cs_sync` BEFORE INSERT ON `calibration_sensors` FOR EACH ROW BEGIN
  CALL ref_touch('sensor_type', NEW.sensor_type, NEW.sensor_type);
  IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.sensor_type_id = LAST_INSERT_ID(); END IF;
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_cs_sync_u` BEFORE UPDATE ON `calibration_sensors` FOR EACH ROW BEGIN
  IF (NEW.sensor_type <=> OLD.sensor_type) = 0 THEN
    CALL ref_touch('sensor_type', NEW.sensor_type, NEW.sensor_type);
    IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.sensor_type_id = LAST_INSERT_ID(); END IF;
  END IF;
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Table structure for table `calibrations`
--

DROP TABLE IF EXISTS `calibrations`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `calibrations` (
  `id` int NOT NULL AUTO_INCREMENT,
  `component_id` int NOT NULL,
  `supplier_id` int DEFAULT NULL,
  `calibration_date` date DEFAULT NULL,
  `next_due` date DEFAULT NULL,
  `cert_doc` varchar(255) DEFAULT NULL,
  `result` enum('prolaz','pao','uvjetno','napomena') DEFAULT 'prolaz',
  `comment` varchar(255) DEFAULT NULL,
  `digital_signature` varchar(128) DEFAULT NULL,
  `last_modified` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `last_modified_by_id` int DEFAULT NULL,
  `source_ip` varchar(45) DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `result_id` int DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_cal_component` (`component_id`),
  KEY `fk_cal_supplier` (`supplier_id`),
  KEY `fk_cal_modified` (`last_modified_by_id`),
  KEY `idx_cal_result_id` (`result_id`),
  CONSTRAINT `fk_cal_component` FOREIGN KEY (`component_id`) REFERENCES `machine_components` (`id`),
  CONSTRAINT `fk_cal_modified` FOREIGN KEY (`last_modified_by_id`) REFERENCES `users` (`id`),
  CONSTRAINT `fk_cal_result` FOREIGN KEY (`result_id`) REFERENCES `ref_value` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_cal_supplier` FOREIGN KEY (`supplier_id`) REFERENCES `suppliers` (`id`),
  CONSTRAINT `chk_cal_dates` CHECK (((`next_due` is null) or (`next_due` > `calibration_date`)))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `calibrations`
--

LOCK TABLES `calibrations` WRITE;
/*!40000 ALTER TABLE `calibrations` DISABLE KEYS */;
/*!40000 ALTER TABLE `calibrations` ENABLE KEYS */;
UNLOCK TABLES;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_cal_sync2` BEFORE INSERT ON `calibrations` FOR EACH ROW BEGIN
  CALL ref_touch('calibration_result', NEW.result, NEW.result);
  IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.result_id = LAST_INSERT_ID(); END IF;
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_calibrations_insert` AFTER INSERT ON `calibrations` FOR EACH ROW BEGIN
    INSERT INTO calibration_audit_log (
        calibration_id, user_id, action, new_value, source_ip
    ) VALUES (
        NEW.id, NEW.last_modified_by_id, 'CREATE',
        CONCAT('Created calibration for component ', NEW.component_id), NEW.source_ip
    );

    INSERT INTO system_event_log(user_id, event_type, table_name, related_module, record_id, description, source_ip, severity)
    VALUES (NEW.last_modified_by_id, 'CREATE', 'calibrations', 'CalibrationModule', NEW.id,
            CONCAT('Created calibration record ID=', NEW.id), NEW.source_ip, 'audit');
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_cal_sync2_u` BEFORE UPDATE ON `calibrations` FOR EACH ROW BEGIN
  IF (NEW.result <=> OLD.result) = 0 THEN
    CALL ref_touch('calibration_result', NEW.result, NEW.result);
    IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.result_id = LAST_INSERT_ID(); END IF;
  END IF;
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_calibrations_update` AFTER UPDATE ON `calibrations` FOR EACH ROW BEGIN
    INSERT INTO calibration_audit_log (
        calibration_id, user_id, action, old_value, new_value, source_ip
    ) VALUES (
        NEW.id, NEW.last_modified_by_id, 'UPDATE', OLD.comment, NEW.comment, NEW.source_ip
    );

    INSERT INTO system_event_log(user_id, event_type, table_name, related_module, record_id, field_name, old_value, new_value, description, source_ip, severity)
    VALUES (NEW.last_modified_by_id, 'UPDATE', 'calibrations', 'CalibrationModule', NEW.id,
            'comment', OLD.comment, NEW.comment, 'Updated calibration record', NEW.source_ip, 'audit');
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_calibrations_delete` AFTER DELETE ON `calibrations` FOR EACH ROW BEGIN
    INSERT INTO calibration_audit_log (
        calibration_id, user_id, action, old_value, source_ip
    ) VALUES (
        OLD.id, OLD.last_modified_by_id, 'DELETE', OLD.comment, OLD.source_ip
    );

    INSERT INTO system_event_log(user_id, event_type, table_name, related_module, record_id, description, source_ip, severity)
    VALUES (OLD.last_modified_by_id, 'DELETE', 'calibrations', 'CalibrationModule', OLD.id,
            CONCAT('Deleted calibration record ID=', OLD.id), 'system', 'critical');
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Table structure for table `capa_action_log`
--

DROP TABLE IF EXISTS `capa_action_log`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `capa_action_log` (
  `id` int NOT NULL AUTO_INCREMENT,
  `capa_case_id` int NOT NULL,
  `action_type` enum('korektivna','preventivna') DEFAULT NULL,
  `description` text,
  `performed_by` int DEFAULT NULL,
  `performed_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `status` enum('planirano','u_tijeku','zavrseno','otkazano') DEFAULT 'planirano',
  `note` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_cal_case` (`capa_case_id`),
  KEY `fk_cal_user` (`performed_by`),
  CONSTRAINT `fk_cal_case` FOREIGN KEY (`capa_case_id`) REFERENCES `capa_cases` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_cal_user` FOREIGN KEY (`performed_by`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `capa_action_log`
--

LOCK TABLES `capa_action_log` WRITE;
/*!40000 ALTER TABLE `capa_action_log` DISABLE KEYS */;
/*!40000 ALTER TABLE `capa_action_log` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `capa_actions`
--

DROP TABLE IF EXISTS `capa_actions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `capa_actions` (
  `id` int NOT NULL AUTO_INCREMENT,
  `capa_id` int NOT NULL,
  `action_type` varchar(80) DEFAULT NULL,
  `description` text,
  `due_date` date DEFAULT NULL,
  `completed_at` datetime DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_capa_action` (`capa_id`),
  CONSTRAINT `fk_capa_action` FOREIGN KEY (`capa_id`) REFERENCES `capa_cases` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `capa_actions`
--

LOCK TABLES `capa_actions` WRITE;
/*!40000 ALTER TABLE `capa_actions` DISABLE KEYS */;
/*!40000 ALTER TABLE `capa_actions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `capa_cases`
--

DROP TABLE IF EXISTS `capa_cases`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `capa_cases` (
  `id` int NOT NULL AUTO_INCREMENT,
  `component_id` int DEFAULT NULL,
  `date_open` date DEFAULT NULL,
  `date_close` date DEFAULT NULL,
  `reason` text,
  `actions` text,
  `status` enum('otvoren','zatvoren','u_tijeku') DEFAULT NULL,
  `doc_file` varchar(255) DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `status_id` int DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_capa_component` (`component_id`),
  KEY `idx_capa_status_id` (`status_id`),
  CONSTRAINT `fk_capa_component` FOREIGN KEY (`component_id`) REFERENCES `machine_components` (`id`),
  CONSTRAINT `fk_capa_status` FOREIGN KEY (`status_id`) REFERENCES `ref_value` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `capa_cases`
--

LOCK TABLES `capa_cases` WRITE;
/*!40000 ALTER TABLE `capa_cases` DISABLE KEYS */;
/*!40000 ALTER TABLE `capa_cases` ENABLE KEYS */;
UNLOCK TABLES;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_capa_sync` BEFORE INSERT ON `capa_cases` FOR EACH ROW BEGIN
  CALL ref_touch('capa_status', NEW.status, NEW.status);
  IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.status_id = LAST_INSERT_ID(); END IF;
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_capa_sync_u` BEFORE UPDATE ON `capa_cases` FOR EACH ROW BEGIN
  IF (NEW.status <=> OLD.status) = 0 THEN
    CALL ref_touch('capa_status', NEW.status, NEW.status);
    IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.status_id = LAST_INSERT_ID(); END IF;
  END IF;
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Table structure for table `capa_status_history`
--

DROP TABLE IF EXISTS `capa_status_history`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `capa_status_history` (
  `id` int NOT NULL AUTO_INCREMENT,
  `capa_case_id` int DEFAULT NULL,
  `old_status` enum('otvoren','zatvoren','planirano','u_tijeku') DEFAULT NULL,
  `new_status` enum('otvoren','zatvoren','planirano','u_tijeku') DEFAULT NULL,
  `changed_by` int DEFAULT NULL,
  `changed_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `note` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_csh_case` (`capa_case_id`),
  KEY `fk_csh_user` (`changed_by`),
  CONSTRAINT `fk_csh_case` FOREIGN KEY (`capa_case_id`) REFERENCES `capa_cases` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_csh_user` FOREIGN KEY (`changed_by`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `capa_status_history`
--

LOCK TABLES `capa_status_history` WRITE;
/*!40000 ALTER TABLE `capa_status_history` DISABLE KEYS */;
/*!40000 ALTER TABLE `capa_status_history` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `checklist_items`
--

DROP TABLE IF EXISTS `checklist_items`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `checklist_items` (
  `id` int NOT NULL AUTO_INCREMENT,
  `template_id` int NOT NULL,
  `item_order` int DEFAULT '10',
  `label` varchar(255) NOT NULL,
  `expected` varchar(255) DEFAULT NULL,
  `required` tinyint(1) DEFAULT '1',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_ci_tpl` (`template_id`),
  CONSTRAINT `fk_ci_tpl` FOREIGN KEY (`template_id`) REFERENCES `checklist_templates` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `checklist_items`
--

LOCK TABLES `checklist_items` WRITE;
/*!40000 ALTER TABLE `checklist_items` DISABLE KEYS */;
/*!40000 ALTER TABLE `checklist_items` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `checklist_templates`
--

DROP TABLE IF EXISTS `checklist_templates`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `checklist_templates` (
  `id` int NOT NULL AUTO_INCREMENT,
  `code` varchar(60) DEFAULT NULL,
  `name` varchar(150) NOT NULL,
  `description` text,
  `created_by` int DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `code` (`code`),
  KEY `fk_ct_user` (`created_by`),
  CONSTRAINT `fk_ct_user` FOREIGN KEY (`created_by`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `checklist_templates`
--

LOCK TABLES `checklist_templates` WRITE;
/*!40000 ALTER TABLE `checklist_templates` DISABLE KEYS */;
/*!40000 ALTER TABLE `checklist_templates` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `comments`
--

DROP TABLE IF EXISTS `comments`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `comments` (
  `id` int NOT NULL AUTO_INCREMENT,
  `entity` varchar(60) NOT NULL,
  `entity_id` int NOT NULL,
  `user_id` int DEFAULT NULL,
  `comment` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_comm_user` (`user_id`),
  CONSTRAINT `fk_comm_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `comments`
--

LOCK TABLES `comments` WRITE;
/*!40000 ALTER TABLE `comments` DISABLE KEYS */;
/*!40000 ALTER TABLE `comments` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `component_devices`
--

DROP TABLE IF EXISTS `component_devices`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `component_devices` (
  `id` int NOT NULL AUTO_INCREMENT,
  `component_id` int NOT NULL,
  `device_id` int NOT NULL,
  `sensor_model_id` int DEFAULT NULL,
  `started_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `ended_at` datetime DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `ux_comp_device` (`component_id`,`device_id`,`started_at`),
  KEY `fk_cd_dev` (`device_id`),
  KEY `fk_cd_smodel` (`sensor_model_id`),
  CONSTRAINT `fk_cd_comp` FOREIGN KEY (`component_id`) REFERENCES `machine_components` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_cd_dev` FOREIGN KEY (`device_id`) REFERENCES `iot_devices` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_cd_smodel` FOREIGN KEY (`sensor_model_id`) REFERENCES `sensor_models` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `component_devices`
--

LOCK TABLES `component_devices` WRITE;
/*!40000 ALTER TABLE `component_devices` DISABLE KEYS */;
/*!40000 ALTER TABLE `component_devices` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `component_models`
--

DROP TABLE IF EXISTS `component_models`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `component_models` (
  `id` int NOT NULL AUTO_INCREMENT,
  `component_type_id` int DEFAULT NULL,
  `model_code` varchar(100) DEFAULT NULL,
  `model_name` varchar(150) DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `ux_comp_model` (`component_type_id`,`model_code`),
  CONSTRAINT `fk_cmodel_type` FOREIGN KEY (`component_type_id`) REFERENCES `component_types` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `component_models`
--

LOCK TABLES `component_models` WRITE;
/*!40000 ALTER TABLE `component_models` DISABLE KEYS */;
INSERT INTO `component_models` VALUES (1,1,'IEC-IE3-2.2kW','IEC IE3 Motor 2.2kW','2025-08-28 12:36:24','2025-08-28 12:36:24'),(2,7,'SINAMICS-G120','SINAMICS G120 VFD','2025-08-28 12:36:24','2025-08-28 12:36:24'),(3,6,'S7-1200','Siemens S7-1200 PLC','2025-08-28 12:36:24','2025-08-28 12:36:24'),(4,3,'ASCO-2W160','ASCO 2/2 Solenoid Valve 2W160','2025-08-28 12:36:24','2025-08-28 12:36:24'),(5,4,'PALL-0.2UM','Pall 0.2 ╬╝m Filter','2025-08-28 12:36:24','2025-08-28 12:36:24');
/*!40000 ALTER TABLE `component_models` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `component_parts`
--

DROP TABLE IF EXISTS `component_parts`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `component_parts` (
  `id` int NOT NULL AUTO_INCREMENT,
  `component_id` int NOT NULL,
  `part_id` int NOT NULL,
  `nominal_qty` decimal(10,3) NOT NULL DEFAULT '1.000',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `ux_comp_part` (`component_id`,`part_id`),
  KEY `fk_cparts_part` (`part_id`),
  CONSTRAINT `fk_cparts_comp` FOREIGN KEY (`component_id`) REFERENCES `machine_components` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_cparts_part` FOREIGN KEY (`part_id`) REFERENCES `parts` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `component_parts`
--

LOCK TABLES `component_parts` WRITE;
/*!40000 ALTER TABLE `component_parts` DISABLE KEYS */;
/*!40000 ALTER TABLE `component_parts` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `component_qualifications`
--

DROP TABLE IF EXISTS `component_qualifications`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `component_qualifications` (
  `id` int NOT NULL AUTO_INCREMENT,
  `component_id` int DEFAULT NULL,
  `type` enum('IQ','OQ','PQ') DEFAULT NULL,
  `qualification_date` date DEFAULT NULL,
  `next_due` date DEFAULT NULL,
  `status` enum('uspjesno','neuspjesno','planirano') DEFAULT NULL,
  `doc_file` varchar(255) DEFAULT NULL,
  `signed_by` varchar(128) DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_cq_component` (`component_id`),
  CONSTRAINT `fk_cq_component` FOREIGN KEY (`component_id`) REFERENCES `machine_components` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `component_qualifications`
--

LOCK TABLES `component_qualifications` WRITE;
/*!40000 ALTER TABLE `component_qualifications` DISABLE KEYS */;
/*!40000 ALTER TABLE `component_qualifications` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `component_types`
--

DROP TABLE IF EXISTS `component_types`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `component_types` (
  `id` int NOT NULL AUTO_INCREMENT,
  `code` varchar(50) DEFAULT NULL,
  `name` varchar(120) NOT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `code` (`code`)
) ENGINE=InnoDB AUTO_INCREMENT=41 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `component_types`
--

LOCK TABLES `component_types` WRITE;
/*!40000 ALTER TABLE `component_types` DISABLE KEYS */;
INSERT INTO `component_types` VALUES (1,'MOTOR','Electric Motor','2025-08-28 12:36:24','2025-08-28 12:36:24'),(2,'PUMP_HEAD','Pump Head','2025-08-28 12:36:24','2025-08-28 12:36:24'),(3,'VALVE','Valve','2025-08-28 12:36:24','2025-08-28 12:36:24'),(4,'FILTER','Filter Housing/Element','2025-08-28 12:36:24','2025-08-28 12:36:24'),(5,'SENSOR','Process Sensor','2025-08-28 12:36:24','2025-08-28 12:36:24'),(6,'PLC','PLC/Controller','2025-08-28 12:36:24','2025-08-28 12:36:24'),(7,'VFD','Variable Frequency Drive','2025-08-28 12:36:24','2025-08-28 12:36:24'),(8,'GEARBOX','Gearbox/Reducer','2025-08-28 12:36:24','2025-08-28 12:36:24'),(9,'CONVEYOR','Conveyor Module','2025-08-28 12:36:24','2025-08-28 12:36:24'),(10,'HEAT_EXCHANGER','Heat Exchanger','2025-08-28 12:36:24','2025-08-28 12:36:24');
/*!40000 ALTER TABLE `component_types` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `config_change_log`
--

DROP TABLE IF EXISTS `config_change_log`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `config_change_log` (
  `id` int NOT NULL AUTO_INCREMENT,
  `change_time` datetime DEFAULT CURRENT_TIMESTAMP,
  `changed_by` int DEFAULT NULL,
  `config_name` varchar(255) DEFAULT NULL,
  `old_value` text,
  `new_value` text,
  `change_type` enum('parametar','backup','restore','security','feature','drugo') DEFAULT NULL,
  `note` text,
  `source_ip` varchar(45) DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `timestamp` datetime GENERATED ALWAYS AS (`change_time`) VIRTUAL,
  `action` varchar(20) GENERATED ALWAYS AS (`change_type`) VIRTUAL,
  `details` text GENERATED ALWAYS AS (`note`) VIRTUAL,
  PRIMARY KEY (`id`),
  KEY `fk_cfg_user` (`changed_by`),
  CONSTRAINT `fk_cfg_user` FOREIGN KEY (`changed_by`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `config_change_log`
--

LOCK TABLES `config_change_log` WRITE;
/*!40000 ALTER TABLE `config_change_log` DISABLE KEYS */;
/*!40000 ALTER TABLE `config_change_log` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `contractor_intervention_audit`
--

DROP TABLE IF EXISTS `contractor_intervention_audit`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `contractor_intervention_audit` (
  `id` int NOT NULL AUTO_INCREMENT,
  `intervention_id` int NOT NULL,
  `user_id` int DEFAULT NULL,
  `action` varchar(30) NOT NULL,
  `details` text,
  `changed_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `source_ip` varchar(45) DEFAULT NULL,
  `device_info` varchar(255) DEFAULT NULL,
  `session_id` varchar(100) DEFAULT NULL,
  `digital_signature` varchar(255) DEFAULT NULL,
  `old_value` text,
  `new_value` text,
  `note` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_cia_intervention` (`intervention_id`),
  KEY `fk_cia_user` (`user_id`),
  CONSTRAINT `fk_cia_intervention` FOREIGN KEY (`intervention_id`) REFERENCES `contractor_interventions` (`id`),
  CONSTRAINT `fk_cia_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `contractor_intervention_audit`
--

LOCK TABLES `contractor_intervention_audit` WRITE;
/*!40000 ALTER TABLE `contractor_intervention_audit` DISABLE KEYS */;
/*!40000 ALTER TABLE `contractor_intervention_audit` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `contractor_interventions`
--

DROP TABLE IF EXISTS `contractor_interventions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `contractor_interventions` (
  `id` int NOT NULL AUTO_INCREMENT,
  `contractor_id` int DEFAULT NULL,
  `component_id` int DEFAULT NULL,
  `intervention_date` date DEFAULT NULL,
  `reason` text,
  `result` text,
  `gmp_compliance` tinyint(1) DEFAULT '1',
  `doc_file` varchar(255) DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_ci_contractor` (`contractor_id`),
  KEY `fk_ci_component` (`component_id`),
  CONSTRAINT `fk_ci_component` FOREIGN KEY (`component_id`) REFERENCES `machine_components` (`id`),
  CONSTRAINT `fk_ci_contractor` FOREIGN KEY (`contractor_id`) REFERENCES `external_contractors` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `contractor_interventions`
--

LOCK TABLES `contractor_interventions` WRITE;
/*!40000 ALTER TABLE `contractor_interventions` DISABLE KEYS */;
/*!40000 ALTER TABLE `contractor_interventions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `dashboards`
--

DROP TABLE IF EXISTS `dashboards`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `dashboards` (
  `id` int NOT NULL AUTO_INCREMENT,
  `dashboard_name` varchar(100) DEFAULT NULL,
  `description` text,
  `created_by` int DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `config_json` json DEFAULT NULL,
  `is_active` tinyint(1) DEFAULT '1',
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `dashboard_name` (`dashboard_name`),
  KEY `fk_dash_user` (`created_by`),
  CONSTRAINT `fk_dash_user` FOREIGN KEY (`created_by`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `dashboards`
--

LOCK TABLES `dashboards` WRITE;
/*!40000 ALTER TABLE `dashboards` DISABLE KEYS */;
/*!40000 ALTER TABLE `dashboards` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `delegated_permissions`
--

DROP TABLE IF EXISTS `delegated_permissions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `delegated_permissions` (
  `id` int NOT NULL AUTO_INCREMENT,
  `from_user_id` int NOT NULL,
  `to_user_id` int NOT NULL,
  `permission_id` int NOT NULL,
  `start_time` datetime DEFAULT CURRENT_TIMESTAMP,
  `expires_at` datetime NOT NULL,
  `reason` varchar(255) DEFAULT NULL,
  `granted_by` int DEFAULT NULL,
  `revoked` tinyint(1) DEFAULT '0',
  `revoked_at` datetime DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_dp_from` (`from_user_id`),
  KEY `fk_dp_to` (`to_user_id`),
  KEY `fk_dp_perm` (`permission_id`),
  KEY `fk_dp_by` (`granted_by`),
  CONSTRAINT `fk_dp_by` FOREIGN KEY (`granted_by`) REFERENCES `users` (`id`),
  CONSTRAINT `fk_dp_from` FOREIGN KEY (`from_user_id`) REFERENCES `users` (`id`),
  CONSTRAINT `fk_dp_perm` FOREIGN KEY (`permission_id`) REFERENCES `permissions` (`id`),
  CONSTRAINT `fk_dp_to` FOREIGN KEY (`to_user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `delegated_permissions`
--

LOCK TABLES `delegated_permissions` WRITE;
/*!40000 ALTER TABLE `delegated_permissions` DISABLE KEYS */;
/*!40000 ALTER TABLE `delegated_permissions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `delete_log`
--

DROP TABLE IF EXISTS `delete_log`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `delete_log` (
  `id` int NOT NULL AUTO_INCREMENT,
  `deleted_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `deleted_by` int DEFAULT NULL,
  `table_name` varchar(100) DEFAULT NULL,
  `record_id` int DEFAULT NULL,
  `delete_type` enum('soft','hard') DEFAULT NULL,
  `reason` text,
  `recoverable` tinyint(1) DEFAULT '1',
  `backup_file` varchar(255) DEFAULT NULL,
  `source_ip` varchar(45) DEFAULT NULL,
  `note` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `timestamp` datetime GENERATED ALWAYS AS (`deleted_at`) VIRTUAL,
  `action` varchar(20) GENERATED ALWAYS AS (`delete_type`) VIRTUAL,
  `details` text GENERATED ALWAYS AS (`reason`) VIRTUAL,
  PRIMARY KEY (`id`),
  KEY `fk_del_user` (`deleted_by`),
  CONSTRAINT `fk_del_user` FOREIGN KEY (`deleted_by`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `delete_log`
--

LOCK TABLES `delete_log` WRITE;
/*!40000 ALTER TABLE `delete_log` DISABLE KEYS */;
/*!40000 ALTER TABLE `delete_log` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `departments`
--

DROP TABLE IF EXISTS `departments`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `departments` (
  `id` int NOT NULL AUTO_INCREMENT,
  `code` varchar(50) DEFAULT NULL,
  `name` varchar(150) NOT NULL,
  `manager_id` int DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `code` (`code`),
  KEY `fk_dept_manager` (`manager_id`),
  CONSTRAINT `fk_dept_manager` FOREIGN KEY (`manager_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `departments`
--

LOCK TABLES `departments` WRITE;
/*!40000 ALTER TABLE `departments` DISABLE KEYS */;
/*!40000 ALTER TABLE `departments` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `deviation_audit`
--

DROP TABLE IF EXISTS `deviation_audit`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `deviation_audit` (
  `id` int NOT NULL AUTO_INCREMENT,
  `deviation_id` int NOT NULL,
  `user_id` int NOT NULL,
  `action` varchar(40) NOT NULL,
  `details` text,
  `changed_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `device_info` varchar(255) DEFAULT NULL,
  `source_ip` varchar(64) DEFAULT NULL,
  `session_id` varchar(128) DEFAULT NULL,
  `digital_signature` varchar(255) DEFAULT NULL,
  `regulatory_status` enum('compliant','pending_review','invalid','forensic','security') DEFAULT 'compliant',
  `ai_anomaly_score` decimal(5,4) DEFAULT '0.0000',
  `validated` tinyint(1) DEFAULT '1',
  `audit_trail_id` int DEFAULT NULL,
  `comment` text,
  `old_value` text,
  `new_value` text,
  `signature_type` enum('pin','password','certificate','biometric','none') DEFAULT 'none',
  `signature_method` varchar(100) DEFAULT NULL,
  `signature_valid` tinyint(1) DEFAULT '1',
  `export_status` enum('none','pdf','csv','xml','emailed','printed') DEFAULT 'none',
  `export_time` datetime DEFAULT NULL,
  `exported_by` int DEFAULT NULL,
  `restored_from_snapshot` tinyint(1) DEFAULT '0',
  `restoration_reference` varchar(128) DEFAULT NULL,
  `approval_status` enum('none','pending','approved','rejected','escalated') DEFAULT 'none',
  `approval_time` datetime DEFAULT NULL,
  `approved_by` int DEFAULT NULL,
  `deleted` tinyint(1) DEFAULT '0',
  `deleted_at` datetime DEFAULT NULL,
  `deleted_by` int DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `related_file` varchar(255) DEFAULT NULL,
  `related_photo` varchar(255) DEFAULT NULL,
  `iot_event_id` int DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_dev_aud_deviation` (`deviation_id`),
  KEY `fk_dev_aud_user` (`user_id`),
  KEY `fk_dev_aud_approved_by` (`approved_by`),
  KEY `fk_dev_aud_audit_trail` (`audit_trail_id`),
  KEY `fk_dev_aud_exported_by` (`exported_by`),
  KEY `fk_dev_aud_deleted_by` (`deleted_by`),
  KEY `fk_dev_aud_iot` (`iot_event_id`),
  CONSTRAINT `fk_dev_aud_approved_by` FOREIGN KEY (`approved_by`) REFERENCES `users` (`id`),
  CONSTRAINT `fk_dev_aud_audit_trail` FOREIGN KEY (`audit_trail_id`) REFERENCES `system_event_log` (`id`),
  CONSTRAINT `fk_dev_aud_deleted_by` FOREIGN KEY (`deleted_by`) REFERENCES `users` (`id`),
  CONSTRAINT `fk_dev_aud_deviation` FOREIGN KEY (`deviation_id`) REFERENCES `deviations` (`id`),
  CONSTRAINT `fk_dev_aud_exported_by` FOREIGN KEY (`exported_by`) REFERENCES `users` (`id`),
  CONSTRAINT `fk_dev_aud_iot` FOREIGN KEY (`iot_event_id`) REFERENCES `iot_event_audit` (`id`),
  CONSTRAINT `fk_dev_aud_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `deviation_audit`
--

LOCK TABLES `deviation_audit` WRITE;
/*!40000 ALTER TABLE `deviation_audit` DISABLE KEYS */;
/*!40000 ALTER TABLE `deviation_audit` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `deviations`
--

DROP TABLE IF EXISTS `deviations`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `deviations` (
  `id` int NOT NULL AUTO_INCREMENT,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `deviations`
--

LOCK TABLES `deviations` WRITE;
/*!40000 ALTER TABLE `deviations` DISABLE KEYS */;
/*!40000 ALTER TABLE `deviations` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `digital_signatures`
--

DROP TABLE IF EXISTS `digital_signatures`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `digital_signatures` (
  `id` int NOT NULL AUTO_INCREMENT,
  `table_name` varchar(100) DEFAULT NULL,
  `record_id` int DEFAULT NULL,
  `user_id` int DEFAULT NULL,
  `signature_hash` varchar(255) DEFAULT NULL,
  `signed_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `method` enum('pin','certificate','biometric','password') DEFAULT NULL,
  `status` enum('valid','revoked','pending') DEFAULT 'valid',
  `ip_address` varchar(45) DEFAULT NULL,
  `device_info` varchar(255) DEFAULT NULL,
  `note` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_dsig_user` (`user_id`),
  CONSTRAINT `fk_dsig_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `digital_signatures`
--

LOCK TABLES `digital_signatures` WRITE;
/*!40000 ALTER TABLE `digital_signatures` DISABLE KEYS */;
/*!40000 ALTER TABLE `digital_signatures` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `document_versions`
--

DROP TABLE IF EXISTS `document_versions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `document_versions` (
  `id` int NOT NULL AUTO_INCREMENT,
  `related_table` varchar(50) DEFAULT NULL,
  `related_id` int DEFAULT NULL,
  `version` varchar(40) DEFAULT NULL,
  `file_path` varchar(255) DEFAULT NULL,
  `created_by` int DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `status` enum('active','archived','obsolete','review') DEFAULT NULL,
  `note` text,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_docv_user` (`created_by`),
  CONSTRAINT `fk_docv_user` FOREIGN KEY (`created_by`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `document_versions`
--

LOCK TABLES `document_versions` WRITE;
/*!40000 ALTER TABLE `document_versions` DISABLE KEYS */;
/*!40000 ALTER TABLE `document_versions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `entity_audit_log`
--

DROP TABLE IF EXISTS `entity_audit_log`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `entity_audit_log` (
  `id` int NOT NULL AUTO_INCREMENT,
  `timestamp` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `user_id` int DEFAULT NULL,
  `source_ip` varchar(64) DEFAULT NULL,
  `device_info` varchar(256) DEFAULT NULL,
  `entity` varchar(64) NOT NULL,
  `entity_id` int DEFAULT NULL,
  `action` varchar(64) NOT NULL,
  `details` text,
  `session_id` varchar(128) DEFAULT NULL,
  `status` varchar(32) DEFAULT NULL,
  `digital_signature` varchar(256) DEFAULT NULL,
  `signature_hash` varchar(256) DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `idx_eal_time` (`timestamp`),
  KEY `idx_eal_user` (`user_id`),
  KEY `idx_eal_entity` (`entity`),
  KEY `idx_eal_action` (`action`),
  KEY `idx_eal_entity_id` (`entity_id`),
  CONSTRAINT `fk_eal_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `entity_audit_log`
--

LOCK TABLES `entity_audit_log` WRITE;
/*!40000 ALTER TABLE `entity_audit_log` DISABLE KEYS */;
/*!40000 ALTER TABLE `entity_audit_log` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `entity_tags`
--

DROP TABLE IF EXISTS `entity_tags`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `entity_tags` (
  `id` int NOT NULL AUTO_INCREMENT,
  `entity` varchar(60) NOT NULL,
  `entity_id` int NOT NULL,
  `tag_id` int NOT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `ux_et` (`entity`,`entity_id`,`tag_id`),
  KEY `fk_et_tag` (`tag_id`),
  CONSTRAINT `fk_et_tag` FOREIGN KEY (`tag_id`) REFERENCES `tags` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `entity_tags`
--

LOCK TABLES `entity_tags` WRITE;
/*!40000 ALTER TABLE `entity_tags` DISABLE KEYS */;
/*!40000 ALTER TABLE `entity_tags` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `export_print_log`
--

DROP TABLE IF EXISTS `export_print_log`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `export_print_log` (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_id` int DEFAULT NULL,
  `export_time` datetime DEFAULT CURRENT_TIMESTAMP,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `format` enum('excel','pdf','csv','xml') DEFAULT NULL,
  `table_name` varchar(100) DEFAULT NULL,
  `filter_used` text,
  `file_path` varchar(255) DEFAULT NULL,
  `source_ip` varchar(45) DEFAULT NULL,
  `note` text,
  `timestamp` datetime GENERATED ALWAYS AS (`export_time`) VIRTUAL,
  `details` text GENERATED ALWAYS AS (`note`) VIRTUAL,
  PRIMARY KEY (`id`),
  KEY `fk_export_user` (`user_id`),
  CONSTRAINT `fk_export_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `export_print_log`
--

LOCK TABLES `export_print_log` WRITE;
/*!40000 ALTER TABLE `export_print_log` DISABLE KEYS */;
/*!40000 ALTER TABLE `export_print_log` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `external_contractors`
--

DROP TABLE IF EXISTS `external_contractors`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `external_contractors` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(100) DEFAULT NULL,
  `service_type` varchar(100) DEFAULT NULL,
  `contact` varchar(100) DEFAULT NULL,
  `phone` varchar(50) DEFAULT NULL,
  `email` varchar(100) DEFAULT NULL,
  `doc_file` varchar(255) DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `external_contractors`
--

LOCK TABLES `external_contractors` WRITE;
/*!40000 ALTER TABLE `external_contractors` DISABLE KEYS */;
/*!40000 ALTER TABLE `external_contractors` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `failure_modes`
--

DROP TABLE IF EXISTS `failure_modes`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `failure_modes` (
  `id` int NOT NULL AUTO_INCREMENT,
  `component_type_id` int DEFAULT NULL,
  `code` varchar(40) DEFAULT NULL,
  `description` varchar(255) DEFAULT NULL,
  `severity_default` int DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_fm_comp_type` (`component_type_id`),
  CONSTRAINT `fk_fm_comp_type` FOREIGN KEY (`component_type_id`) REFERENCES `component_types` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `failure_modes`
--

LOCK TABLES `failure_modes` WRITE;
/*!40000 ALTER TABLE `failure_modes` DISABLE KEYS */;
/*!40000 ALTER TABLE `failure_modes` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `forensic_user_change_log`
--

DROP TABLE IF EXISTS `forensic_user_change_log`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `forensic_user_change_log` (
  `id` int NOT NULL AUTO_INCREMENT,
  `changed_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `changed_by` int DEFAULT NULL,
  `action` enum('create_user','update_user','disable_user','change_role','reset_password','delete_user','force_logout') DEFAULT NULL,
  `target_user_id` int DEFAULT NULL,
  `old_role` varchar(50) DEFAULT NULL,
  `new_role` varchar(50) DEFAULT NULL,
  `old_status` tinyint(1) DEFAULT NULL,
  `new_status` tinyint(1) DEFAULT NULL,
  `note` text,
  `source_ip` varchar(45) DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `timestamp` datetime GENERATED ALWAYS AS (`changed_at`) VIRTUAL,
  `details` text GENERATED ALWAYS AS (`note`) VIRTUAL,
  PRIMARY KEY (`id`),
  KEY `fk_fucl_changed_by` (`changed_by`),
  KEY `fk_fucl_target_user` (`target_user_id`),
  CONSTRAINT `fk_fucl_changed_by` FOREIGN KEY (`changed_by`) REFERENCES `users` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_fucl_target_user` FOREIGN KEY (`target_user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `forensic_user_change_log`
--

LOCK TABLES `forensic_user_change_log` WRITE;
/*!40000 ALTER TABLE `forensic_user_change_log` DISABLE KEYS */;
/*!40000 ALTER TABLE `forensic_user_change_log` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `incident_log`
--

DROP TABLE IF EXISTS `incident_log`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `incident_log` (
  `id` int NOT NULL AUTO_INCREMENT,
  `detected_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `reported_by` int DEFAULT NULL,
  `severity` enum('low','medium','high','critical','gmp','compliance') DEFAULT 'low',
  `title` varchar(255) DEFAULT NULL,
  `description` text,
  `resolved` tinyint(1) DEFAULT '0',
  `resolved_at` datetime DEFAULT NULL,
  `resolved_by` int DEFAULT NULL,
  `actions_taken` text,
  `follow_up` text,
  `note` text,
  `source_ip` varchar(45) DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `severity_id` int DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_inc_reported_by` (`reported_by`),
  KEY `fk_inc_resolved_by` (`resolved_by`),
  KEY `idx_inc_sev_id` (`severity_id`),
  CONSTRAINT `fk_inc_reported_by` FOREIGN KEY (`reported_by`) REFERENCES `users` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_inc_resolved_by` FOREIGN KEY (`resolved_by`) REFERENCES `users` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_inc_sev` FOREIGN KEY (`severity_id`) REFERENCES `ref_value` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `incident_log`
--

LOCK TABLES `incident_log` WRITE;
/*!40000 ALTER TABLE `incident_log` DISABLE KEYS */;
/*!40000 ALTER TABLE `incident_log` ENABLE KEYS */;
UNLOCK TABLES;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_inc_sync` BEFORE INSERT ON `incident_log` FOR EACH ROW BEGIN
  CALL ref_touch('severity', NEW.severity, NEW.severity);
  IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.severity_id = LAST_INSERT_ID(); END IF;
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_inc_sync_u` BEFORE UPDATE ON `incident_log` FOR EACH ROW BEGIN
  IF (NEW.severity <=> OLD.severity) = 0 THEN
    CALL ref_touch('severity', NEW.severity, NEW.severity);
    IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.severity_id = LAST_INSERT_ID(); END IF;
  END IF;
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Table structure for table `inspections`
--

DROP TABLE IF EXISTS `inspections`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `inspections` (
  `id` int NOT NULL AUTO_INCREMENT,
  `inspection_date` date DEFAULT NULL,
  `inspector_name` varchar(100) DEFAULT NULL,
  `type` enum('HALMED','interni','drugi') DEFAULT NULL,
  `result` enum('prolaz','pao','napomena') DEFAULT NULL,
  `related_machine` int DEFAULT NULL,
  `doc_file` varchar(255) DEFAULT NULL,
  `notes` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `type_id` int DEFAULT NULL,
  `result_id` int DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_insp_machine` (`related_machine`),
  KEY `idx_insp_type_id` (`type_id`),
  KEY `idx_insp_result_id` (`result_id`),
  CONSTRAINT `fk_insp_machine` FOREIGN KEY (`related_machine`) REFERENCES `machines` (`id`),
  CONSTRAINT `fk_insp_result` FOREIGN KEY (`result_id`) REFERENCES `ref_value` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_insp_type` FOREIGN KEY (`type_id`) REFERENCES `ref_value` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `inspections`
--

LOCK TABLES `inspections` WRITE;
/*!40000 ALTER TABLE `inspections` DISABLE KEYS */;
/*!40000 ALTER TABLE `inspections` ENABLE KEYS */;
UNLOCK TABLES;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_insp_sync` BEFORE INSERT ON `inspections` FOR EACH ROW BEGIN
  CALL ref_touch('inspection_type', NEW.type, NEW.type);
  IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.type_id = LAST_INSERT_ID(); END IF;
  CALL ref_touch('inspection_result', NEW.result, NEW.result);
  IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.result_id = LAST_INSERT_ID(); END IF;
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_insp_sync_u` BEFORE UPDATE ON `inspections` FOR EACH ROW BEGIN
  IF (NEW.type <=> OLD.type) = 0 THEN
    CALL ref_touch('inspection_type', NEW.type, NEW.type);
    IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.type_id = LAST_INSERT_ID(); END IF;
  END IF;
  IF (NEW.result <=> OLD.result) = 0 THEN
    CALL ref_touch('inspection_result', NEW.result, NEW.result);
    IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.result_id = LAST_INSERT_ID(); END IF;
  END IF;
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Table structure for table `integration_log`
--

DROP TABLE IF EXISTS `integration_log`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `integration_log` (
  `id` int NOT NULL AUTO_INCREMENT,
  `system_name` varchar(100) DEFAULT NULL,
  `api_endpoint` varchar(255) DEFAULT NULL,
  `request_time` datetime DEFAULT CURRENT_TIMESTAMP,
  `request_json` longtext,
  `response_json` longtext,
  `status_code` int DEFAULT NULL,
  `processed` tinyint(1) DEFAULT '0',
  `processed_at` datetime DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `integration_log`
--

LOCK TABLES `integration_log` WRITE;
/*!40000 ALTER TABLE `integration_log` DISABLE KEYS */;
/*!40000 ALTER TABLE `integration_log` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `inventory_transactions`
--

DROP TABLE IF EXISTS `inventory_transactions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `inventory_transactions` (
  `id` int NOT NULL AUTO_INCREMENT,
  `part_id` int DEFAULT NULL,
  `warehouse_id` int DEFAULT NULL,
  `transaction_type` enum('in','out','transfer','adjust','damage','obsolete') DEFAULT NULL,
  `quantity` int DEFAULT NULL,
  `transaction_date` datetime DEFAULT CURRENT_TIMESTAMP,
  `performed_by` int DEFAULT NULL,
  `related_document` varchar(255) DEFAULT NULL,
  `note` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_it_part` (`part_id`),
  KEY `fk_it_warehouse` (`warehouse_id`),
  KEY `fk_it_user` (`performed_by`),
  CONSTRAINT `fk_it_part` FOREIGN KEY (`part_id`) REFERENCES `parts` (`id`),
  CONSTRAINT `fk_it_user` FOREIGN KEY (`performed_by`) REFERENCES `users` (`id`),
  CONSTRAINT `fk_it_warehouse` FOREIGN KEY (`warehouse_id`) REFERENCES `warehouses` (`id`),
  CONSTRAINT `chk_it_qty_nonneg` CHECK (((`quantity` is null) or (`quantity` >= 0)))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `inventory_transactions`
--

LOCK TABLES `inventory_transactions` WRITE;
/*!40000 ALTER TABLE `inventory_transactions` DISABLE KEYS */;
/*!40000 ALTER TABLE `inventory_transactions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `iot_anomaly_log`
--

DROP TABLE IF EXISTS `iot_anomaly_log`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `iot_anomaly_log` (
  `id` int NOT NULL AUTO_INCREMENT,
  `sensor_data_id` int DEFAULT NULL,
  `detected_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `detected_by` varchar(100) DEFAULT NULL,
  `description` text,
  `severity` enum('low','medium','high','critical') DEFAULT NULL,
  `resolved` tinyint(1) DEFAULT '0',
  `resolved_at` datetime DEFAULT NULL,
  `resolution_note` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_ial_sensor` (`sensor_data_id`),
  CONSTRAINT `fk_ial_sensor` FOREIGN KEY (`sensor_data_id`) REFERENCES `iot_sensor_data` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `iot_anomaly_log`
--

LOCK TABLES `iot_anomaly_log` WRITE;
/*!40000 ALTER TABLE `iot_anomaly_log` DISABLE KEYS */;
/*!40000 ALTER TABLE `iot_anomaly_log` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `iot_devices`
--

DROP TABLE IF EXISTS `iot_devices`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `iot_devices` (
  `id` int NOT NULL AUTO_INCREMENT,
  `device_uid` varchar(120) NOT NULL,
  `vendor` varchar(120) DEFAULT NULL,
  `model` varchar(120) DEFAULT NULL,
  `firmware` varchar(80) DEFAULT NULL,
  `gateway_id` int DEFAULT NULL,
  `last_seen` datetime DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `device_uid` (`device_uid`),
  KEY `fk_dev_gw` (`gateway_id`),
  CONSTRAINT `fk_dev_gw` FOREIGN KEY (`gateway_id`) REFERENCES `iot_gateways` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `iot_devices`
--

LOCK TABLES `iot_devices` WRITE;
/*!40000 ALTER TABLE `iot_devices` DISABLE KEYS */;
/*!40000 ALTER TABLE `iot_devices` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `iot_event_audit`
--

DROP TABLE IF EXISTS `iot_event_audit`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `iot_event_audit` (
  `id` int NOT NULL AUTO_INCREMENT,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `iot_event_audit`
--

LOCK TABLES `iot_event_audit` WRITE;
/*!40000 ALTER TABLE `iot_event_audit` DISABLE KEYS */;
/*!40000 ALTER TABLE `iot_event_audit` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `iot_gateways`
--

DROP TABLE IF EXISTS `iot_gateways`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `iot_gateways` (
  `id` int NOT NULL AUTO_INCREMENT,
  `code` varchar(80) DEFAULT NULL,
  `name` varchar(120) DEFAULT NULL,
  `location_id` int DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `code` (`code`),
  KEY `fk_gw_loc` (`location_id`),
  CONSTRAINT `fk_gw_loc` FOREIGN KEY (`location_id`) REFERENCES `locations` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `iot_gateways`
--

LOCK TABLES `iot_gateways` WRITE;
/*!40000 ALTER TABLE `iot_gateways` DISABLE KEYS */;
/*!40000 ALTER TABLE `iot_gateways` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `iot_sensor_data`
--

DROP TABLE IF EXISTS `iot_sensor_data`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `iot_sensor_data` (
  `id` int NOT NULL AUTO_INCREMENT,
  `device_id` varchar(80) DEFAULT NULL,
  `component_id` int DEFAULT NULL,
  `data_type` varchar(50) DEFAULT NULL,
  `value` decimal(12,4) DEFAULT NULL,
  `unit` varchar(20) DEFAULT NULL,
  `timestamp` datetime DEFAULT NULL,
  `status` enum('ok','alert','out_of_range') DEFAULT NULL,
  `anomaly_detected` tinyint(1) DEFAULT '0',
  `processed` tinyint(1) DEFAULT '0',
  `note` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `unit_id` int DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_isd_component` (`component_id`),
  KEY `fk_isd_unit` (`unit_id`),
  KEY `idx_isd_device_time` (`device_id`,`timestamp`),
  CONSTRAINT `fk_isd_component` FOREIGN KEY (`component_id`) REFERENCES `machine_components` (`id`),
  CONSTRAINT `fk_isd_unit` FOREIGN KEY (`unit_id`) REFERENCES `units` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `iot_sensor_data`
--

LOCK TABLES `iot_sensor_data` WRITE;
/*!40000 ALTER TABLE `iot_sensor_data` DISABLE KEYS */;
/*!40000 ALTER TABLE `iot_sensor_data` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `irregularities_log`
--

DROP TABLE IF EXISTS `irregularities_log`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `irregularities_log` (
  `id` int NOT NULL AUTO_INCREMENT,
  `related_type` enum('work_order','component','machine','inspection','capa_case','training','validation') DEFAULT NULL,
  `related_id` int DEFAULT NULL,
  `user_id` int DEFAULT NULL,
  `description` text,
  `detected_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `status` enum('otvoreno','zatvoreno') DEFAULT 'otvoreno',
  `note` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `details` text GENERATED ALWAYS AS (`description`) VIRTUAL,
  PRIMARY KEY (`id`),
  KEY `fk_irr_user` (`user_id`),
  CONSTRAINT `fk_irr_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `irregularities_log`
--

LOCK TABLES `irregularities_log` WRITE;
/*!40000 ALTER TABLE `irregularities_log` DISABLE KEYS */;
/*!40000 ALTER TABLE `irregularities_log` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `job_titles`
--

DROP TABLE IF EXISTS `job_titles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `job_titles` (
  `id` int NOT NULL AUTO_INCREMENT,
  `title` varchar(100) DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `title` (`title`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `job_titles`
--

LOCK TABLES `job_titles` WRITE;
/*!40000 ALTER TABLE `job_titles` DISABLE KEYS */;
/*!40000 ALTER TABLE `job_titles` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `locations`
--

DROP TABLE IF EXISTS `locations`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `locations` (
  `id` int NOT NULL AUTO_INCREMENT,
  `parent_id` int DEFAULT NULL,
  `code` varchar(50) DEFAULT NULL,
  `name` varchar(150) NOT NULL,
  `type` enum('site','building','area','room','zone','warehouse') NOT NULL DEFAULT 'site',
  `address` varchar(255) DEFAULT NULL,
  `city` varchar(100) DEFAULT NULL,
  `country` varchar(100) DEFAULT NULL,
  `gps_lat` decimal(10,7) DEFAULT NULL,
  `gps_lng` decimal(10,7) DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `code` (`code`),
  KEY `fk_loc_parent` (`parent_id`),
  CONSTRAINT `fk_loc_parent` FOREIGN KEY (`parent_id`) REFERENCES `locations` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `locations`
--

LOCK TABLES `locations` WRITE;
/*!40000 ALTER TABLE `locations` DISABLE KEYS */;
/*!40000 ALTER TABLE `locations` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `lookup_domain`
--

DROP TABLE IF EXISTS `lookup_domain`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `lookup_domain` (
  `id` int NOT NULL AUTO_INCREMENT,
  `domain_code` varchar(50) DEFAULT NULL,
  `description` varchar(255) DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `domain_code` (`domain_code`)
) ENGINE=InnoDB AUTO_INCREMENT=18 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `lookup_domain`
--

LOCK TABLES `lookup_domain` WRITE;
/*!40000 ALTER TABLE `lookup_domain` DISABLE KEYS */;
INSERT INTO `lookup_domain` VALUES (1,'status','Generic status','2025-08-28 07:24:44','2025-08-28 12:36:25'),(2,'priority','Work-order priority','2025-08-28 07:24:44','2025-08-28 12:36:25'),(3,'severity','Incident / anomaly severity','2025-08-28 07:24:44','2025-08-28 12:36:25'),(4,'result','Pass / fail style','2025-08-28 07:24:44','2025-08-28 12:36:25'),(5,'document_type','Attachment / photo type','2025-08-28 07:24:44','2025-08-28 12:36:25'),(6,'inspection_result',NULL,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(7,'calibration_result',NULL,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(8,'asset_status',NULL,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(9,'lifecycle_phase',NULL,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(10,'component_status',NULL,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(11,'sensor_type',NULL,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(12,'quality_event_type',NULL,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(13,'quality_status',NULL,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(14,'capa_status',NULL,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(15,'capa_action_type',NULL,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(16,'supplier_status',NULL,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(17,'supplier_type',NULL,'2025-08-28 10:24:01','2025-08-28 12:36:25');
/*!40000 ALTER TABLE `lookup_domain` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `lookup_value`
--

DROP TABLE IF EXISTS `lookup_value`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `lookup_value` (
  `id` int NOT NULL AUTO_INCREMENT,
  `domain_id` int NOT NULL,
  `value_code` varchar(100) DEFAULT NULL,
  `value_label` varchar(100) DEFAULT NULL,
  `is_active` tinyint(1) DEFAULT '1',
  `sort_order` int DEFAULT '0',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uk_domain_code` (`domain_id`,`value_code`),
  CONSTRAINT `fk_lk_domain` FOREIGN KEY (`domain_id`) REFERENCES `lookup_domain` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=287 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `lookup_value`
--

LOCK TABLES `lookup_value` WRITE;
/*!40000 ALTER TABLE `lookup_value` DISABLE KEYS */;
INSERT INTO `lookup_value` VALUES (1,1,'active','Active',1,1,'2025-08-28 07:24:44','2025-08-28 12:36:25'),(2,1,'maintenance','Maintenance',1,2,'2025-08-28 07:24:44','2025-08-28 12:36:25'),(3,1,'decommissioned','Decommissioned',1,3,'2025-08-28 07:24:44','2025-08-28 12:36:25'),(4,1,'open','Open',1,4,'2025-08-28 07:24:44','2025-08-28 12:36:25'),(5,1,'closed','Closed',1,5,'2025-08-28 07:24:44','2025-08-28 12:36:25'),(6,2,'low','Low',1,1,'2025-08-28 07:24:44','2025-08-28 12:36:25'),(7,2,'medium','Medium',1,2,'2025-08-28 07:24:44','2025-08-28 12:36:25'),(8,2,'high','High',1,3,'2025-08-28 07:24:44','2025-08-28 12:36:25'),(9,2,'critical','Critical',1,4,'2025-08-28 07:24:44','2025-08-28 12:36:25'),(10,3,'low','Low',1,1,'2025-08-28 07:24:44','2025-08-28 12:36:25'),(11,3,'medium','Medium',1,2,'2025-08-28 07:24:44','2025-08-28 12:36:25'),(12,3,'high','High',1,3,'2025-08-28 07:24:44','2025-08-28 12:36:25'),(13,3,'critical','Critical',1,4,'2025-08-28 07:24:44','2025-08-28 12:36:25'),(14,3,'gmp','GMP',1,5,'2025-08-28 07:24:44','2025-08-28 12:36:25'),(15,4,'pass','Pass',1,1,'2025-08-28 07:24:44','2025-08-28 12:36:25'),(16,4,'fail','Fail',1,2,'2025-08-28 07:24:44','2025-08-28 12:36:25'),(17,4,'note','Note',1,3,'2025-08-28 07:24:44','2025-08-28 12:36:25'),(18,5,'sop','SOP',1,1,'2025-08-28 07:24:44','2025-08-28 12:36:25'),(19,5,'inspection','Inspection',1,2,'2025-08-28 07:24:44','2025-08-28 12:36:25'),(20,5,'before','Before',1,3,'2025-08-28 07:24:44','2025-08-28 12:36:25'),(21,5,'after','After',1,4,'2025-08-28 07:24:44','2025-08-28 12:36:25'),(22,5,'other','Other',1,5,'2025-08-28 07:24:44','2025-08-28 12:36:25'),(24,6,'prolaz','Prolaz',1,10,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(25,6,'pao','Pao',1,20,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(26,6,'napomena','Napomena',1,30,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(27,7,'prolaz','Prolaz',1,10,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(28,7,'pao','Pao',1,20,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(29,7,'uvjetno','Uvjetno',1,30,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(30,7,'napomena','Napomena',1,40,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(31,8,'active','Active',1,10,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(32,8,'maintenance','Maintenance',1,20,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(33,8,'decommissioned','Decommissioned',1,30,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(34,8,'reserved','Reserved',1,40,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(35,8,'scrapped','Scrapped',1,50,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(36,9,'concept','Concept',1,10,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(37,9,'commissioning','Commissioning',1,20,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(38,9,'operation','Operation',1,30,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(39,9,'retirement','Retirement',1,40,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(40,10,'active','Active',1,10,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(41,10,'removed','Removed',1,20,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(42,10,'maintenance','Maintenance',1,30,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(43,11,'temperatura','Temperatura',1,10,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(44,11,'tlak','Tlak',1,20,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(45,11,'vlaga','Vlaga',1,30,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(46,11,'protok','Protok',1,40,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(47,11,'drugo','Drugo',1,50,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(48,12,'deviation','Deviation',1,10,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(49,12,'complaint','Complaint',1,20,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(50,12,'recall','Recall',1,30,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(51,12,'out_of_spec','Out of spec',1,40,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(52,12,'change_control','Change control',1,50,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(53,12,'audit','Audit',1,60,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(54,12,'training','Training',1,70,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(55,13,'open','Open',1,10,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(56,13,'under_review','Under review',1,20,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(57,13,'closed','Closed',1,30,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(58,14,'otvoren','Otvoren',1,10,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(59,14,'u_tijeku','U tijeku',1,20,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(60,14,'zatvoren','Zatvoren',1,30,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(61,15,'korektivna','Korektivna',1,10,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(62,15,'preventivna','Preventivna',1,20,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(63,16,'active','Active',1,10,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(64,16,'suspended','Suspended',1,20,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(65,16,'obsolete','Obsolete',1,30,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(66,17,'manufacturer','Manufacturer',1,10,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(67,17,'distributor','Distributor',1,20,'2025-08-28 10:24:01','2025-08-28 12:36:25'),(68,17,'service','Service',1,30,'2025-08-28 10:24:01','2025-08-28 12:36:25');
/*!40000 ALTER TABLE `lookup_value` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `machine_components`
--

DROP TABLE IF EXISTS `machine_components`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `machine_components` (
  `id` int NOT NULL AUTO_INCREMENT,
  `machine_id` int DEFAULT NULL,
  `code` varchar(50) DEFAULT NULL,
  `name` varchar(100) DEFAULT NULL,
  `type` varchar(50) DEFAULT NULL,
  `sop_doc` varchar(255) DEFAULT NULL,
  `status` enum('active','removed','maintenance') DEFAULT 'active',
  `install_date` date DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `component_type_id` int DEFAULT NULL,
  `status_id` int DEFAULT NULL,
  `is_deleted` tinyint(1) DEFAULT '0',
  `deleted_at` datetime DEFAULT NULL,
  `deleted_by` int DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `code` (`code`),
  KEY `fk_mc_machine` (`machine_id`),
  KEY `fk_mc_status` (`status_id`),
  KEY `fk_comp_component_type` (`component_type_id`),
  CONSTRAINT `fk_comp_component_type` FOREIGN KEY (`component_type_id`) REFERENCES `component_types` (`id`),
  CONSTRAINT `fk_mc_machine` FOREIGN KEY (`machine_id`) REFERENCES `machines` (`id`),
  CONSTRAINT `fk_mc_status` FOREIGN KEY (`status_id`) REFERENCES `ref_value` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_mc_type` FOREIGN KEY (`component_type_id`) REFERENCES `component_types` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `machine_components`
--

LOCK TABLES `machine_components` WRITE;
/*!40000 ALTER TABLE `machine_components` DISABLE KEYS */;
/*!40000 ALTER TABLE `machine_components` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `machine_lifecycle_event`
--

DROP TABLE IF EXISTS `machine_lifecycle_event`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `machine_lifecycle_event` (
  `id` int NOT NULL AUTO_INCREMENT,
  `machine_id` int DEFAULT NULL,
  `event_type` enum('procurement','installation','maintenance','repair','upgrade','move','decommission','scrap','other') DEFAULT NULL,
  `event_date` datetime DEFAULT CURRENT_TIMESTAMP,
  `performed_by` int DEFAULT NULL,
  `notes` text,
  `doc_file` varchar(255) DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `event_type_id` int DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_mle_machine` (`machine_id`),
  KEY `fk_mle_user` (`performed_by`),
  KEY `fk_mle_type` (`event_type_id`),
  CONSTRAINT `fk_mle_machine` FOREIGN KEY (`machine_id`) REFERENCES `machines` (`id`),
  CONSTRAINT `fk_mle_type` FOREIGN KEY (`event_type_id`) REFERENCES `ref_value` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_mle_user` FOREIGN KEY (`performed_by`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `machine_lifecycle_event`
--

LOCK TABLES `machine_lifecycle_event` WRITE;
/*!40000 ALTER TABLE `machine_lifecycle_event` DISABLE KEYS */;
/*!40000 ALTER TABLE `machine_lifecycle_event` ENABLE KEYS */;
UNLOCK TABLES;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_mle_sync` BEFORE INSERT ON `machine_lifecycle_event` FOR EACH ROW BEGIN
  CALL ref_touch('lifecycle_phase', NEW.event_type, NEW.event_type);
  IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.event_type_id = LAST_INSERT_ID(); END IF;
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_mle_sync_u` BEFORE UPDATE ON `machine_lifecycle_event` FOR EACH ROW BEGIN
  IF (NEW.event_type <=> OLD.event_type) = 0 THEN
    CALL ref_touch('lifecycle_phase', NEW.event_type, NEW.event_type);
    IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.event_type_id = LAST_INSERT_ID(); END IF;
  END IF;
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Table structure for table `machine_models`
--

DROP TABLE IF EXISTS `machine_models`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `machine_models` (
  `id` int NOT NULL AUTO_INCREMENT,
  `manufacturer_id` int DEFAULT NULL,
  `machine_type_id` int DEFAULT NULL,
  `model_code` varchar(100) DEFAULT NULL,
  `model_name` varchar(150) DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `ux_mach_model` (`manufacturer_id`,`model_code`),
  KEY `fk_mmodel_type` (`machine_type_id`),
  CONSTRAINT `fk_mmodel_manu` FOREIGN KEY (`manufacturer_id`) REFERENCES `manufacturers` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_mmodel_type` FOREIGN KEY (`machine_type_id`) REFERENCES `machine_types` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `machine_models`
--

LOCK TABLES `machine_models` WRITE;
/*!40000 ALTER TABLE `machine_models` DISABLE KEYS */;
INSERT INTO `machine_models` VALUES (1,4,4,'GA30','GA 30 Compressor','2025-08-28 12:36:25','2025-08-28 12:36:25'),(2,3,2,'RAA-500','RAA 500 Chiller','2025-08-28 12:36:25','2025-08-28 12:36:25'),(3,5,5,'STE-6060','STE 6060 Sterilizer','2025-08-28 12:36:25','2025-08-28 12:36:25'),(4,1,1,'S7-1200-HVAC','S7-1200 HVAC Controller','2025-08-28 12:36:25','2025-08-28 12:36:25');
/*!40000 ALTER TABLE `machine_models` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `machine_types`
--

DROP TABLE IF EXISTS `machine_types`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `machine_types` (
  `id` int NOT NULL AUTO_INCREMENT,
  `code` varchar(50) DEFAULT NULL,
  `name` varchar(120) NOT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `code` (`code`)
) ENGINE=InnoDB AUTO_INCREMENT=41 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `machine_types`
--

LOCK TABLES `machine_types` WRITE;
/*!40000 ALTER TABLE `machine_types` DISABLE KEYS */;
INSERT INTO `machine_types` VALUES (1,'HVAC','HVAC System','2025-08-28 12:36:25','2025-08-28 12:36:25'),(2,'CHILLER','Chiller','2025-08-28 12:36:25','2025-08-28 12:36:25'),(3,'PUMP','Pump','2025-08-28 12:36:25','2025-08-28 12:36:25'),(4,'COMPRESSOR','Air Compressor','2025-08-28 12:36:25','2025-08-28 12:36:25'),(5,'STERILIZER','Sterilizer/Autoclave','2025-08-28 12:36:25','2025-08-28 12:36:25'),(6,'FILLING_LINE','Filling/Packaging Line','2025-08-28 12:36:25','2025-08-28 12:36:25'),(7,'BLISTER_PKG','Blister Packaging Machine','2025-08-28 12:36:25','2025-08-28 12:36:25'),(8,'TABLET_PRESS','Tablet Press','2025-08-28 12:36:25','2025-08-28 12:36:25'),(9,'MIXER','Mixer/Agitator','2025-08-28 12:36:25','2025-08-28 12:36:25'),(10,'BIOREACTOR','Bioreactor','2025-08-28 12:36:25','2025-08-28 12:36:25');
/*!40000 ALTER TABLE `machine_types` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `machines`
--

DROP TABLE IF EXISTS `machines`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `machines` (
  `id` int NOT NULL AUTO_INCREMENT,
  `code` varchar(50) DEFAULT NULL,
  `name` varchar(100) DEFAULT NULL,
  `description` varchar(1000) DEFAULT NULL,
  `model` varchar(100) DEFAULT NULL,
  `serial_number` varchar(80) DEFAULT NULL,
  `manufacturer` varchar(100) DEFAULT NULL,
  `location` varchar(100) DEFAULT NULL,
  `install_date` date DEFAULT NULL,
  `procurement_date` date DEFAULT NULL,
  `warranty_until` date DEFAULT NULL,
  `acquisition_cost` decimal(18,2) DEFAULT NULL,
  `rfid_tag` varchar(64) DEFAULT NULL,
  `iot_device_id` varchar(64) DEFAULT NULL,
  `cloud_device_guid` varchar(64) DEFAULT NULL,
  `is_critical` tinyint(1) DEFAULT '0',
  `lifecycle_phase` varchar(30) DEFAULT NULL,
  `note` varchar(200) DEFAULT NULL,
  `status` enum('active','maintenance','decommissioned','reserved','scrapped') DEFAULT 'active',
  `urs_doc` varchar(255) DEFAULT NULL,
  `decommission_date` date DEFAULT NULL,
  `decommission_reason` text,
  `last_modified` datetime DEFAULT NULL,
  `last_modified_by_id` int DEFAULT NULL,
  `digital_signature` varchar(128) DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `location_id` int DEFAULT NULL,
  `manufacturer_id` int DEFAULT NULL,
  `machine_type_id` int DEFAULT NULL,
  `status_id` int DEFAULT NULL,
  `lifecycle_phase_id` int DEFAULT NULL,
  `is_deleted` tinyint(1) DEFAULT '0',
  `deleted_at` datetime DEFAULT NULL,
  `deleted_by` int DEFAULT NULL,
  `tenant_id` int DEFAULT NULL,
  `room_id` int DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `code` (`code`),
  KEY `fk_m_last_modified_by` (`last_modified_by_id`),
  KEY `fk_machines_location` (`location_id`),
  KEY `fk_machines_manufacturer` (`manufacturer_id`),
  KEY `idx_m_status_id` (`status_id`),
  KEY `idx_m_phase_id` (`lifecycle_phase_id`),
  KEY `fk_machines_tenant_id` (`tenant_id`),
  KEY `fk_machine_room` (`room_id`),
  KEY `fk_mach_machine_type` (`machine_type_id`),
  CONSTRAINT `fk_m_last_modified_by` FOREIGN KEY (`last_modified_by_id`) REFERENCES `users` (`id`),
  CONSTRAINT `fk_m_phase` FOREIGN KEY (`lifecycle_phase_id`) REFERENCES `ref_value` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_m_status` FOREIGN KEY (`status_id`) REFERENCES `ref_value` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_mach_machine_type` FOREIGN KEY (`machine_type_id`) REFERENCES `machine_types` (`id`),
  CONSTRAINT `fk_mach_status` FOREIGN KEY (`status_id`) REFERENCES `lookup_value` (`id`),
  CONSTRAINT `fk_machine_room` FOREIGN KEY (`room_id`) REFERENCES `rooms` (`id`),
  CONSTRAINT `fk_machines_location` FOREIGN KEY (`location_id`) REFERENCES `locations` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_machines_manufacturer` FOREIGN KEY (`manufacturer_id`) REFERENCES `manufacturers` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_machines_tenant_id` FOREIGN KEY (`tenant_id`) REFERENCES `tenants` (`id`),
  CONSTRAINT `fk_machines_type` FOREIGN KEY (`machine_type_id`) REFERENCES `machine_types` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `machines`
--

LOCK TABLES `machines` WRITE;
/*!40000 ALTER TABLE `machines` DISABLE KEYS */;
/*!40000 ALTER TABLE `machines` ENABLE KEYS */;
UNLOCK TABLES;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_m_sync` BEFORE INSERT ON `machines` FOR EACH ROW BEGIN
  CALL ref_touch('asset_status', NEW.status, NEW.status);
  IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.status_id = LAST_INSERT_ID(); END IF;
  CALL ref_touch('lifecycle_phase', NEW.lifecycle_phase, NEW.lifecycle_phase);
  IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.lifecycle_phase_id = LAST_INSERT_ID(); END IF;
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_m_sync_u` BEFORE UPDATE ON `machines` FOR EACH ROW BEGIN
  IF (NEW.status <=> OLD.status) = 0 THEN
    CALL ref_touch('asset_status', NEW.status, NEW.status);
    IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.status_id = LAST_INSERT_ID(); END IF;
  END IF;
  IF (NEW.lifecycle_phase <=> OLD.lifecycle_phase) = 0 THEN
    CALL ref_touch('lifecycle_phase', NEW.lifecycle_phase, NEW.lifecycle_phase);
    IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.lifecycle_phase_id = LAST_INSERT_ID(); END IF;
  END IF;
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Table structure for table `manufacturers`
--

DROP TABLE IF EXISTS `manufacturers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `manufacturers` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(150) NOT NULL,
  `website` varchar(255) DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `name` (`name`)
) ENGINE=InnoDB AUTO_INCREMENT=37 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `manufacturers`
--

LOCK TABLES `manufacturers` WRITE;
/*!40000 ALTER TABLE `manufacturers` DISABLE KEYS */;
INSERT INTO `manufacturers` VALUES (1,'Siemens','https://www.siemens.com','2025-08-28 07:24:01','2025-08-28 12:36:25'),(2,'ABB','https://global.abb','2025-08-28 07:24:01','2025-08-28 12:36:25'),(3,'GEA','https://www.gea.com','2025-08-28 07:24:01','2025-08-28 12:36:25'),(4,'Atlas Copco','https://www.atlascopco.com','2025-08-28 07:24:01','2025-08-28 12:36:25'),(5,'Binder','https://www.binder-world.com','2025-08-28 07:24:01','2025-08-28 12:36:25'),(6,'Bosch Rexroth','https://www.boschrexroth.com','2025-08-28 07:24:01','2025-08-28 12:36:25'),(7,'IFM','https://www.ifm.com','2025-08-28 07:24:01','2025-08-28 12:36:25'),(8,'Honeywell','https://www.honeywell.com','2025-08-28 07:24:01','2025-08-28 12:36:25'),(9,'Sensirion','https://sensirion.com','2025-08-28 07:24:01','2025-08-28 12:36:25');
/*!40000 ALTER TABLE `manufacturers` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `measurement_units`
--

DROP TABLE IF EXISTS `measurement_units`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `measurement_units` (
  `id` int NOT NULL AUTO_INCREMENT,
  `code` varchar(10) DEFAULT NULL,
  `name` varchar(50) DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `code` (`code`)
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `measurement_units`
--

LOCK TABLES `measurement_units` WRITE;
/*!40000 ALTER TABLE `measurement_units` DISABLE KEYS */;
INSERT INTO `measurement_units` VALUES (1,'┬░C','Degree Celsius','2025-08-28 12:36:25','2025-08-28 12:36:25'),(2,'Pa','Pascal','2025-08-28 12:36:25','2025-08-28 12:36:25'),(3,'%','Percent','2025-08-28 12:36:25','2025-08-28 12:36:25'),(4,'L/min','Liters per minute','2025-08-28 12:36:25','2025-08-28 12:36:25'),(5,'V','Volt','2025-08-28 12:36:25','2025-08-28 12:36:25'),(6,'A','Ampere','2025-08-28 12:36:25','2025-08-28 12:36:25');
/*!40000 ALTER TABLE `measurement_units` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `mobile_device_log`
--

DROP TABLE IF EXISTS `mobile_device_log`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `mobile_device_log` (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_id` int DEFAULT NULL,
  `device_info` varchar(255) DEFAULT NULL,
  `login_time` datetime DEFAULT NULL,
  `logout_time` datetime DEFAULT NULL,
  `os_version` varchar(50) DEFAULT NULL,
  `location` varchar(100) DEFAULT NULL,
  `ip_address` varchar(45) DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_mdl_user` (`user_id`),
  CONSTRAINT `fk_mdl_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `mobile_device_log`
--

LOCK TABLES `mobile_device_log` WRITE;
/*!40000 ALTER TABLE `mobile_device_log` DISABLE KEYS */;
/*!40000 ALTER TABLE `mobile_device_log` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `notification_queue`
--

DROP TABLE IF EXISTS `notification_queue`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `notification_queue` (
  `id` int NOT NULL AUTO_INCREMENT,
  `template_id` int DEFAULT NULL,
  `recipient_user_id` int DEFAULT NULL,
  `channel` enum('email','sms','push','webhook') DEFAULT 'email',
  `payload` json DEFAULT NULL,
  `scheduled_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `sent_at` datetime DEFAULT NULL,
  `status` enum('queued','sent','failed') DEFAULT 'queued',
  `last_error` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_nq_tpl` (`template_id`),
  KEY `fk_nq_user` (`recipient_user_id`),
  CONSTRAINT `fk_nq_tpl` FOREIGN KEY (`template_id`) REFERENCES `notification_templates` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_nq_user` FOREIGN KEY (`recipient_user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `notification_queue`
--

LOCK TABLES `notification_queue` WRITE;
/*!40000 ALTER TABLE `notification_queue` DISABLE KEYS */;
/*!40000 ALTER TABLE `notification_queue` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `notification_templates`
--

DROP TABLE IF EXISTS `notification_templates`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `notification_templates` (
  `id` int NOT NULL AUTO_INCREMENT,
  `code` varchar(80) DEFAULT NULL,
  `name` varchar(150) DEFAULT NULL,
  `subject` varchar(255) DEFAULT NULL,
  `body` text,
  `channel` enum('email','sms','push','webhook') DEFAULT 'email',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `code` (`code`)
) ENGINE=InnoDB AUTO_INCREMENT=33 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `notification_templates`
--

LOCK TABLES `notification_templates` WRITE;
/*!40000 ALTER TABLE `notification_templates` DISABLE KEYS */;
INSERT INTO `notification_templates` VALUES (1,'WO_ASSIGNED','Work Order Assigned','WO #{work_order_id} assigned','Hello {{assignee}}, Work Order #{{work_order_id}} was assigned. Machine: {{machine_name}} / Component: {{component_name}}.','email','2025-08-28 12:36:25','2025-08-28 12:36:25'),(2,'WO_OVERDUE','Work Order Overdue','WO #{work_order_id} is overdue','Work Order #{{work_order_id}} is overdue. Status: {{status_name}}.','email','2025-08-28 12:36:25','2025-08-28 12:36:25'),(3,'CALIB_DUE','Calibration Due','Calibration due: {{component_name}}','Calibration for {{component_name}} is due on {{next_due}}.','email','2025-08-28 12:36:25','2025-08-28 12:36:25'),(4,'STOCK_BELOW_MIN','Stock Below Min','Stock alert: {{part_code}}','Part {{part_code}} is below minimum in {{warehouse_name}}.','email','2025-08-28 12:36:25','2025-08-28 12:36:25'),(5,'INCIDENT_REPORTED','Incident Reported','Incident {{severity}} reported','Incident \"{{title}}\" reported by {{reported_by}}.','email','2025-08-28 12:36:25','2025-08-28 12:36:25'),(6,'QE_STATUS_CHANGE','Quality Event Status','Quality event status changed','Quality Event #{{qe_id}} is now {{status_name}}.','email','2025-08-28 12:36:25','2025-08-28 12:36:25'),(7,'CAPA_DUE','CAPA Action Due','CAPA action due','CAPA #{{capa_id}} action due {{due_date}}.','email','2025-08-28 12:36:25','2025-08-28 12:36:25'),(8,'SENSOR_ANOMALY','Sensor Anomaly','Anomaly on {{component_name}}','Anomaly detected by {{algorithm}} at {{timestamp}}. Value: {{value}} {{unit}}.','email','2025-08-28 12:36:25','2025-08-28 12:36:25');
/*!40000 ALTER TABLE `notification_templates` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `part_bom`
--

DROP TABLE IF EXISTS `part_bom`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `part_bom` (
  `id` int NOT NULL AUTO_INCREMENT,
  `parent_part_id` int NOT NULL,
  `child_part_id` int NOT NULL,
  `quantity` decimal(10,3) NOT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `ux_bom` (`parent_part_id`,`child_part_id`),
  KEY `fk_bom_child` (`child_part_id`),
  CONSTRAINT `fk_bom_child` FOREIGN KEY (`child_part_id`) REFERENCES `parts` (`id`) ON DELETE RESTRICT,
  CONSTRAINT `fk_bom_parent` FOREIGN KEY (`parent_part_id`) REFERENCES `parts` (`id`) ON DELETE CASCADE,
  CONSTRAINT `part_bom_chk_1` CHECK ((`quantity` > 0))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `part_bom`
--

LOCK TABLES `part_bom` WRITE;
/*!40000 ALTER TABLE `part_bom` DISABLE KEYS */;
/*!40000 ALTER TABLE `part_bom` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `part_supplier_prices`
--

DROP TABLE IF EXISTS `part_supplier_prices`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `part_supplier_prices` (
  `id` int NOT NULL AUTO_INCREMENT,
  `part_id` int DEFAULT NULL,
  `supplier_id` int DEFAULT NULL,
  `unit_price` decimal(10,2) DEFAULT NULL,
  `currency` varchar(10) DEFAULT NULL,
  `valid_until` date DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_psp_part` (`part_id`),
  KEY `fk_psp_supplier` (`supplier_id`),
  CONSTRAINT `fk_psp_part` FOREIGN KEY (`part_id`) REFERENCES `parts` (`id`),
  CONSTRAINT `fk_psp_supplier` FOREIGN KEY (`supplier_id`) REFERENCES `suppliers` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `part_supplier_prices`
--

LOCK TABLES `part_supplier_prices` WRITE;
/*!40000 ALTER TABLE `part_supplier_prices` DISABLE KEYS */;
/*!40000 ALTER TABLE `part_supplier_prices` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `parts`
--

DROP TABLE IF EXISTS `parts`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `parts` (
  `id` int NOT NULL AUTO_INCREMENT,
  `code` varchar(50) DEFAULT NULL,
  `name` varchar(100) DEFAULT NULL,
  `default_supplier_id` int DEFAULT NULL,
  `description` text,
  `status` enum('active','obsolete','reorder') DEFAULT 'active',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `code` (`code`),
  KEY `fk_p_supplier` (`default_supplier_id`),
  CONSTRAINT `fk_p_supplier` FOREIGN KEY (`default_supplier_id`) REFERENCES `suppliers` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `parts`
--

LOCK TABLES `parts` WRITE;
/*!40000 ALTER TABLE `parts` DISABLE KEYS */;
/*!40000 ALTER TABLE `parts` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `permission_change_log`
--

DROP TABLE IF EXISTS `permission_change_log`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `permission_change_log` (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_id` int NOT NULL,
  `changed_by` int NOT NULL,
  `change_type` enum('role','permission','delegation','override') NOT NULL,
  `role_id` int DEFAULT NULL,
  `permission_id` int DEFAULT NULL,
  `action` enum('grant','revoke','deny','expire','delegate','escalate') NOT NULL,
  `reason` varchar(255) DEFAULT NULL,
  `change_time` datetime DEFAULT CURRENT_TIMESTAMP,
  `expires_at` datetime DEFAULT NULL,
  `source_ip` varchar(45) DEFAULT NULL,
  `session_id` varchar(100) DEFAULT NULL,
  `details` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_pcl_user` (`user_id`),
  KEY `fk_pcl_changed_by` (`changed_by`),
  KEY `fk_pcl_role` (`role_id`),
  KEY `fk_pcl_perm` (`permission_id`),
  CONSTRAINT `fk_pcl_changed_by` FOREIGN KEY (`changed_by`) REFERENCES `users` (`id`),
  CONSTRAINT `fk_pcl_perm` FOREIGN KEY (`permission_id`) REFERENCES `permissions` (`id`),
  CONSTRAINT `fk_pcl_role` FOREIGN KEY (`role_id`) REFERENCES `roles` (`id`),
  CONSTRAINT `fk_pcl_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `permission_change_log`
--

LOCK TABLES `permission_change_log` WRITE;
/*!40000 ALTER TABLE `permission_change_log` DISABLE KEYS */;
/*!40000 ALTER TABLE `permission_change_log` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `permission_requests`
--

DROP TABLE IF EXISTS `permission_requests`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `permission_requests` (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_id` int NOT NULL,
  `permission_id` int NOT NULL,
  `requested_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `reason` varchar(255) DEFAULT NULL,
  `status` enum('pending','approved','denied','expired') DEFAULT 'pending',
  `reviewed_by` int DEFAULT NULL,
  `reviewed_at` datetime DEFAULT NULL,
  `review_comment` varchar(255) DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_pr_user` (`user_id`),
  KEY `fk_pr_perm` (`permission_id`),
  KEY `fk_pr_by` (`reviewed_by`),
  CONSTRAINT `fk_pr_by` FOREIGN KEY (`reviewed_by`) REFERENCES `users` (`id`),
  CONSTRAINT `fk_pr_perm` FOREIGN KEY (`permission_id`) REFERENCES `permissions` (`id`),
  CONSTRAINT `fk_pr_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `permission_requests`
--

LOCK TABLES `permission_requests` WRITE;
/*!40000 ALTER TABLE `permission_requests` DISABLE KEYS */;
/*!40000 ALTER TABLE `permission_requests` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `permissions`
--

DROP TABLE IF EXISTS `permissions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `permissions` (
  `id` int NOT NULL AUTO_INCREMENT,
  `code` varchar(100) NOT NULL,
  `description` varchar(255) DEFAULT NULL,
  `module` varchar(100) DEFAULT NULL,
  `critical` tinyint(1) DEFAULT '0',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `code` (`code`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `permissions`
--

LOCK TABLES `permissions` WRITE;
/*!40000 ALTER TABLE `permissions` DISABLE KEYS */;
INSERT INTO `permissions` VALUES (1,'manage_system','Full system administration',NULL,0,'2025-08-25 11:36:22','2025-08-25 11:36:22'),(2,'manage_team','Manage team operations',NULL,0,'2025-08-25 11:36:22','2025-08-25 11:36:22'),(3,'use_system','Standard system usage',NULL,0,'2025-08-25 11:36:22','2025-08-25 11:36:22'),(4,'user.change_password','Allows changing user passwords (self/others).','users',1,'2025-08-28 14:05:44','2025-08-28 14:05:44');
/*!40000 ALTER TABLE `permissions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `photos`
--

DROP TABLE IF EXISTS `photos`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `photos` (
  `id` int NOT NULL AUTO_INCREMENT,
  `work_order_id` int DEFAULT NULL,
  `component_id` int DEFAULT NULL,
  `file_name` varchar(255) DEFAULT NULL,
  `file_path` varchar(255) DEFAULT NULL,
  `type` enum('prije','poslije','dokumentacija','drugo') DEFAULT 'dokumentacija',
  `uploaded_by` int DEFAULT NULL,
  `uploaded_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `watermark_applied` tinyint(1) DEFAULT '0',
  `note` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_ph_wo` (`work_order_id`),
  KEY `fk_ph_component` (`component_id`),
  KEY `fk_ph_user` (`uploaded_by`),
  CONSTRAINT `fk_ph_component` FOREIGN KEY (`component_id`) REFERENCES `machine_components` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_ph_user` FOREIGN KEY (`uploaded_by`) REFERENCES `users` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_ph_wo` FOREIGN KEY (`work_order_id`) REFERENCES `work_orders` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `photos`
--

LOCK TABLES `photos` WRITE;
/*!40000 ALTER TABLE `photos` DISABLE KEYS */;
/*!40000 ALTER TABLE `photos` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `preventive_maintenance_plans`
--

DROP TABLE IF EXISTS `preventive_maintenance_plans`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `preventive_maintenance_plans` (
  `id` int NOT NULL AUTO_INCREMENT,
  `code` varchar(50) DEFAULT NULL,
  `name` varchar(100) DEFAULT NULL,
  `description` text,
  `machine_id` int DEFAULT NULL,
  `component_id` int DEFAULT NULL,
  `frequency` varchar(40) DEFAULT NULL,
  `checklist_file` varchar(255) DEFAULT NULL,
  `responsible_user_id` int DEFAULT NULL,
  `last_executed` date DEFAULT NULL,
  `next_due` date DEFAULT NULL,
  `status` varchar(40) DEFAULT NULL,
  `digital_signature` varchar(128) DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `checklist_template_id` int DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `code` (`code`),
  KEY `fk_pmp_machine` (`machine_id`),
  KEY `fk_pmp_component` (`component_id`),
  KEY `fk_pmp_responsible` (`responsible_user_id`),
  KEY `fk_pmp_tpl` (`checklist_template_id`),
  CONSTRAINT `fk_pmp_component` FOREIGN KEY (`component_id`) REFERENCES `machine_components` (`id`),
  CONSTRAINT `fk_pmp_machine` FOREIGN KEY (`machine_id`) REFERENCES `machines` (`id`),
  CONSTRAINT `fk_pmp_responsible` FOREIGN KEY (`responsible_user_id`) REFERENCES `users` (`id`),
  CONSTRAINT `fk_pmp_tpl` FOREIGN KEY (`checklist_template_id`) REFERENCES `checklist_templates` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `preventive_maintenance_plans`
--

LOCK TABLES `preventive_maintenance_plans` WRITE;
/*!40000 ALTER TABLE `preventive_maintenance_plans` DISABLE KEYS */;
/*!40000 ALTER TABLE `preventive_maintenance_plans` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `quality_events`
--

DROP TABLE IF EXISTS `quality_events`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `quality_events` (
  `id` int NOT NULL AUTO_INCREMENT,
  `event_type` enum('deviation','complaint','recall','out_of_spec','change_control','audit','training') DEFAULT NULL,
  `date_open` date DEFAULT NULL,
  `date_close` date DEFAULT NULL,
  `description` text,
  `related_machine` int DEFAULT NULL,
  `related_component` int DEFAULT NULL,
  `status` enum('open','closed','under_review') DEFAULT NULL,
  `actions` text,
  `doc_file` varchar(255) DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `type_id` int DEFAULT NULL,
  `status_id` int DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_qe_machine` (`related_machine`),
  KEY `fk_qe_component` (`related_component`),
  KEY `idx_qe_type_id` (`type_id`),
  KEY `idx_qe_status_id` (`status_id`),
  CONSTRAINT `fk_qe_component` FOREIGN KEY (`related_component`) REFERENCES `machine_components` (`id`),
  CONSTRAINT `fk_qe_machine` FOREIGN KEY (`related_machine`) REFERENCES `machines` (`id`),
  CONSTRAINT `fk_qe_status` FOREIGN KEY (`status_id`) REFERENCES `ref_value` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_qe_type` FOREIGN KEY (`type_id`) REFERENCES `ref_value` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `quality_events`
--

LOCK TABLES `quality_events` WRITE;
/*!40000 ALTER TABLE `quality_events` DISABLE KEYS */;
/*!40000 ALTER TABLE `quality_events` ENABLE KEYS */;
UNLOCK TABLES;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_qe_sync` BEFORE INSERT ON `quality_events` FOR EACH ROW BEGIN
  CALL ref_touch('quality_event_type', NEW.event_type, NEW.event_type);
  IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.type_id = LAST_INSERT_ID(); END IF;
  CALL ref_touch('quality_status', NEW.status, NEW.status);
  IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.status_id = LAST_INSERT_ID(); END IF;
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_qe_sync_u` BEFORE UPDATE ON `quality_events` FOR EACH ROW BEGIN
  IF (NEW.event_type <=> OLD.event_type) = 0 THEN
    CALL ref_touch('quality_event_type', NEW.event_type, NEW.event_type);
    IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.type_id = LAST_INSERT_ID(); END IF;
  END IF;
  IF (NEW.status <=> OLD.status) = 0 THEN
    CALL ref_touch('quality_status', NEW.status, NEW.status);
    IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.status_id = LAST_INSERT_ID(); END IF;
  END IF;
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Table structure for table `report_schedule`
--

DROP TABLE IF EXISTS `report_schedule`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `report_schedule` (
  `id` int NOT NULL AUTO_INCREMENT,
  `report_name` varchar(100) DEFAULT NULL,
  `schedule_type` enum('daily','weekly','monthly','on_demand') DEFAULT NULL,
  `format` enum('pdf','excel','csv') DEFAULT NULL,
  `recipients` text,
  `last_generated` datetime DEFAULT NULL,
  `next_due` datetime DEFAULT NULL,
  `status` enum('planned','generated','failed') DEFAULT NULL,
  `generated_by` int DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_rsch_user` (`generated_by`),
  CONSTRAINT `fk_rsch_user` FOREIGN KEY (`generated_by`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `report_schedule`
--

LOCK TABLES `report_schedule` WRITE;
/*!40000 ALTER TABLE `report_schedule` DISABLE KEYS */;
/*!40000 ALTER TABLE `report_schedule` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `requalification_schedule`
--

DROP TABLE IF EXISTS `requalification_schedule`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `requalification_schedule` (
  `id` int NOT NULL AUTO_INCREMENT,
  `component_id` int DEFAULT NULL,
  `last_qualified` date DEFAULT NULL,
  `next_due` date DEFAULT NULL,
  `method` varchar(255) DEFAULT NULL,
  `responsible` varchar(100) DEFAULT NULL,
  `notes` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_requal_component` (`component_id`),
  CONSTRAINT `fk_requal_component` FOREIGN KEY (`component_id`) REFERENCES `machine_components` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `requalification_schedule`
--

LOCK TABLES `requalification_schedule` WRITE;
/*!40000 ALTER TABLE `requalification_schedule` DISABLE KEYS */;
/*!40000 ALTER TABLE `requalification_schedule` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `role_audit`
--

DROP TABLE IF EXISTS `role_audit`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `role_audit` (
  `id` int NOT NULL AUTO_INCREMENT,
  `role_id` int DEFAULT NULL,
  `action` varchar(40) NOT NULL,
  `description` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `source_ip` varchar(45) DEFAULT NULL,
  `device_info` varchar(255) DEFAULT NULL,
  `session_id` varchar(100) DEFAULT NULL,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `details` text GENERATED ALWAYS AS (`description`) VIRTUAL,
  PRIMARY KEY (`id`),
  KEY `fk_ra_role` (`role_id`),
  CONSTRAINT `fk_ra_role` FOREIGN KEY (`role_id`) REFERENCES `roles` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `role_audit`
--

LOCK TABLES `role_audit` WRITE;
/*!40000 ALTER TABLE `role_audit` DISABLE KEYS */;
/*!40000 ALTER TABLE `role_audit` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `role_permissions`
--

DROP TABLE IF EXISTS `role_permissions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `role_permissions` (
  `role_id` int NOT NULL,
  `permission_id` int NOT NULL,
  `allowed` tinyint(1) DEFAULT '1',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `assigned_by` int DEFAULT NULL,
  `assigned_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`role_id`,`permission_id`),
  KEY `fk_rp_perm` (`permission_id`),
  CONSTRAINT `fk_rp_perm` FOREIGN KEY (`permission_id`) REFERENCES `permissions` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_rp_role` FOREIGN KEY (`role_id`) REFERENCES `roles` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `role_permissions`
--

LOCK TABLES `role_permissions` WRITE;
/*!40000 ALTER TABLE `role_permissions` DISABLE KEYS */;
INSERT INTO `role_permissions` VALUES (1,1,1,'2025-08-25 11:36:22','2025-08-25 11:36:22',NULL,'2025-08-27 12:08:25'),(1,4,1,'2025-08-28 14:05:44','2025-08-28 14:05:44',NULL,'2025-08-28 14:05:44'),(2,2,1,'2025-08-25 11:36:22','2025-08-25 11:36:22',NULL,'2025-08-27 12:08:25'),(3,3,1,'2025-08-25 11:36:22','2025-08-25 11:36:22',NULL,'2025-08-27 12:08:25');
/*!40000 ALTER TABLE `role_permissions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `roles`
--

DROP TABLE IF EXISTS `roles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `roles` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(50) NOT NULL,
  `description` varchar(255) DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `name` (`name`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `roles`
--

LOCK TABLES `roles` WRITE;
/*!40000 ALTER TABLE `roles` DISABLE KEYS */;
INSERT INTO `roles` VALUES (1,'admin','System Administrator','2025-08-25 11:36:22','2025-08-25 11:36:22'),(2,'manager','Department Manager','2025-08-25 11:36:22','2025-08-25 11:36:22'),(3,'user','Standard User','2025-08-25 11:36:22','2025-08-25 11:36:22');
/*!40000 ALTER TABLE `roles` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `rooms`
--

DROP TABLE IF EXISTS `rooms`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `rooms` (
  `id` int NOT NULL AUTO_INCREMENT,
  `building_id` int NOT NULL,
  `code` varchar(20) DEFAULT NULL,
  `name` varchar(100) DEFAULT NULL,
  `classification` enum('C','D','Not Classified') DEFAULT 'Not Classified',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_room_build` (`building_id`),
  CONSTRAINT `fk_room_build` FOREIGN KEY (`building_id`) REFERENCES `buildings` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `rooms`
--

LOCK TABLES `rooms` WRITE;
/*!40000 ALTER TABLE `rooms` DISABLE KEYS */;
/*!40000 ALTER TABLE `rooms` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `scheduled_job_audit_log`
--

DROP TABLE IF EXISTS `scheduled_job_audit_log`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `scheduled_job_audit_log` (
  `id` int NOT NULL AUTO_INCREMENT,
  `scheduled_job_id` int NOT NULL,
  `user_id` int DEFAULT NULL,
  `action` enum('CREATE','UPDATE','DELETE','EXECUTE','ACKNOWLEDGE','ESCALATE','FAIL','CANCEL','EXPORT') NOT NULL,
  `old_value` text,
  `new_value` text,
  `changed_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `source_ip` varchar(45) DEFAULT NULL,
  `device_info` varchar(255) DEFAULT NULL,
  `session_id` varchar(100) DEFAULT NULL,
  `digital_signature` varchar(255) DEFAULT NULL,
  `note` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `timestamp` datetime GENERATED ALWAYS AS (`changed_at`) VIRTUAL,
  `details` text GENERATED ALWAYS AS (`note`) VIRTUAL,
  PRIMARY KEY (`id`),
  KEY `fk_sjal_job` (`scheduled_job_id`),
  KEY `fk_sjal_user` (`user_id`),
  CONSTRAINT `fk_sjal_job` FOREIGN KEY (`scheduled_job_id`) REFERENCES `scheduled_jobs` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_sjal_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `scheduled_job_audit_log`
--

LOCK TABLES `scheduled_job_audit_log` WRITE;
/*!40000 ALTER TABLE `scheduled_job_audit_log` DISABLE KEYS */;
/*!40000 ALTER TABLE `scheduled_job_audit_log` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `scheduled_jobs`
--

DROP TABLE IF EXISTS `scheduled_jobs`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `scheduled_jobs` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(100) NOT NULL,
  `job_type` varchar(40) NOT NULL,
  `entity_type` varchar(40) DEFAULT NULL,
  `entity_id` int DEFAULT NULL,
  `status` enum('scheduled','in_progress','pending_ack','overdue','completed','failed','canceled','escalated') DEFAULT 'scheduled',
  `next_due` datetime NOT NULL,
  `recurrence_pattern` varchar(100) DEFAULT NULL,
  `cron_expression` varchar(100) DEFAULT NULL,
  `last_executed` datetime DEFAULT NULL,
  `last_result` varchar(255) DEFAULT NULL,
  `escalation_level` int DEFAULT '0',
  `escalation_note` varchar(255) DEFAULT NULL,
  `chain_job_id` int DEFAULT NULL,
  `is_critical` tinyint(1) DEFAULT '0',
  `needs_acknowledgment` tinyint(1) DEFAULT '0',
  `acknowledged_by` int DEFAULT NULL,
  `acknowledged_at` datetime DEFAULT NULL,
  `alert_on_failure` tinyint(1) DEFAULT '1',
  `retries` int DEFAULT '0',
  `max_retries` int DEFAULT '3',
  `last_error` text,
  `iot_device_id` varchar(80) DEFAULT NULL,
  `extra_params` json DEFAULT NULL,
  `created_by` int DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `last_modified` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `last_modified_by` int DEFAULT NULL,
  `digital_signature` varchar(255) DEFAULT NULL,
  `device_info` varchar(255) DEFAULT NULL,
  `session_id` varchar(100) DEFAULT NULL,
  `ip_address` varchar(45) DEFAULT NULL,
  `comment` text,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_sj_created_by` (`created_by`),
  KEY `fk_sj_last_modified_by` (`last_modified_by`),
  KEY `fk_sj_ack_by` (`acknowledged_by`),
  KEY `fk_sj_chain` (`chain_job_id`),
  KEY `idx_scheduled_jobs_next_due` (`next_due`),
  KEY `idx_scheduled_jobs_status` (`status`),
  KEY `idx_scheduled_jobs_job_type` (`job_type`),
  CONSTRAINT `fk_sj_ack_by` FOREIGN KEY (`acknowledged_by`) REFERENCES `users` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_sj_chain` FOREIGN KEY (`chain_job_id`) REFERENCES `scheduled_jobs` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_sj_created_by` FOREIGN KEY (`created_by`) REFERENCES `users` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_sj_last_modified_by` FOREIGN KEY (`last_modified_by`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `scheduled_jobs`
--

LOCK TABLES `scheduled_jobs` WRITE;
/*!40000 ALTER TABLE `scheduled_jobs` DISABLE KEYS */;
/*!40000 ALTER TABLE `scheduled_jobs` ENABLE KEYS */;
UNLOCK TABLES;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_scheduled_jobs_insert` AFTER INSERT ON `scheduled_jobs` FOR EACH ROW BEGIN
    INSERT INTO scheduled_job_audit_log (
        scheduled_job_id, user_id, action, new_value, source_ip, device_info, session_id, digital_signature, note
    ) VALUES (
        NEW.id, NEW.created_by, 'CREATE',
        CONCAT('Created job: ', NEW.name, ' [', NEW.job_type, ']'),
        NEW.ip_address, NEW.device_info, NEW.session_id, NEW.digital_signature, NEW.comment
    );
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_scheduled_jobs_update` AFTER UPDATE ON `scheduled_jobs` FOR EACH ROW BEGIN
    INSERT INTO scheduled_job_audit_log (
        scheduled_job_id, user_id, action, old_value, new_value, source_ip, device_info, session_id, digital_signature, note
    ) VALUES (
        NEW.id, NEW.last_modified_by, 'UPDATE',
        OLD.comment, NEW.comment,
        NEW.ip_address, NEW.device_info, NEW.session_id, NEW.digital_signature, NEW.comment
    );
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_scheduled_jobs_delete` AFTER DELETE ON `scheduled_jobs` FOR EACH ROW BEGIN
    INSERT INTO scheduled_job_audit_log (
        scheduled_job_id, user_id, action, old_value, source_ip, device_info, session_id, note
    ) VALUES (
        OLD.id, OLD.last_modified_by, 'DELETE',
        OLD.comment, OLD.ip_address, OLD.device_info, OLD.session_id, OLD.comment
    );
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Table structure for table `schema_migration_log`
--

DROP TABLE IF EXISTS `schema_migration_log`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `schema_migration_log` (
  `id` int NOT NULL AUTO_INCREMENT,
  `migration_time` datetime DEFAULT CURRENT_TIMESTAMP,
  `migrated_by` int DEFAULT NULL,
  `schema_version` varchar(50) DEFAULT NULL,
  `migration_script` text,
  `description` text,
  `source_ip` varchar(45) DEFAULT NULL,
  `success` tinyint(1) DEFAULT '1',
  `error_message` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `timestamp` datetime GENERATED ALWAYS AS (`migration_time`) VIRTUAL,
  PRIMARY KEY (`id`),
  KEY `fk_mig_user` (`migrated_by`),
  CONSTRAINT `fk_mig_user` FOREIGN KEY (`migrated_by`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `schema_migration_log`
--

LOCK TABLES `schema_migration_log` WRITE;
/*!40000 ALTER TABLE `schema_migration_log` DISABLE KEYS */;
/*!40000 ALTER TABLE `schema_migration_log` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `sensitive_data_access_log`
--

DROP TABLE IF EXISTS `sensitive_data_access_log`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `sensitive_data_access_log` (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_id` int DEFAULT NULL,
  `access_time` datetime DEFAULT CURRENT_TIMESTAMP,
  `table_name` varchar(100) DEFAULT NULL,
  `record_id` int DEFAULT NULL,
  `field_name` varchar(100) DEFAULT NULL,
  `access_type` enum('view','export','print','api','edit','delete') DEFAULT NULL,
  `source_ip` varchar(45) DEFAULT NULL,
  `purpose` varchar(255) DEFAULT NULL,
  `approved_by` int DEFAULT NULL,
  `note` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `accessed_at` datetime GENERATED ALWAYS AS (`access_time`) VIRTUAL,
  `timestamp` datetime GENERATED ALWAYS AS (`access_time`) VIRTUAL,
  `action` varchar(30) GENERATED ALWAYS AS (`access_type`) VIRTUAL,
  `details` text GENERATED ALWAYS AS (coalesce(`purpose`,`note`)) VIRTUAL,
  PRIMARY KEY (`id`),
  KEY `fk_sdal_user` (`user_id`),
  KEY `fk_sdal_appr` (`approved_by`),
  CONSTRAINT `fk_sdal_appr` FOREIGN KEY (`approved_by`) REFERENCES `users` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_sdal_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `sensitive_data_access_log`
--

LOCK TABLES `sensitive_data_access_log` WRITE;
/*!40000 ALTER TABLE `sensitive_data_access_log` DISABLE KEYS */;
/*!40000 ALTER TABLE `sensitive_data_access_log` ENABLE KEYS */;
UNLOCK TABLES;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_sensitive_access_fk_guard` BEFORE INSERT ON `sensitive_data_access_log` FOR EACH ROW BEGIN
  IF NEW.user_id IS NOT NULL THEN
    IF (SELECT COUNT(*) FROM users WHERE id = NEW.user_id) = 0 THEN
      SET NEW.user_id = NULL;
    END IF;
  END IF;
  IF NEW.approved_by IS NOT NULL THEN
    IF (SELECT COUNT(*) FROM users WHERE id = NEW.approved_by) = 0 THEN
      SET NEW.approved_by = NULL;
    END IF;
  END IF;
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Table structure for table `sensor_data_logs`
--

DROP TABLE IF EXISTS `sensor_data_logs`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `sensor_data_logs` (
  `id` int NOT NULL AUTO_INCREMENT,
  `component_id` int DEFAULT NULL,
  `sensor_type` enum('temperatura','tlak','vlaga','protok') DEFAULT NULL,
  `value` decimal(10,2) DEFAULT NULL,
  `unit` varchar(10) DEFAULT NULL,
  `timestamp` datetime DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `unit_id` int DEFAULT NULL,
  `sensor_type_id` int DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_sdl_component` (`component_id`),
  KEY `idx_sdl_sensor_type_id` (`sensor_type_id`),
  KEY `idx_sdl_unit_id` (`unit_id`),
  CONSTRAINT `fk_sdl_component` FOREIGN KEY (`component_id`) REFERENCES `machine_components` (`id`),
  CONSTRAINT `fk_sdl_sensortype` FOREIGN KEY (`sensor_type_id`) REFERENCES `ref_value` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_sdl_unit` FOREIGN KEY (`unit_id`) REFERENCES `units` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `sensor_data_logs`
--

LOCK TABLES `sensor_data_logs` WRITE;
/*!40000 ALTER TABLE `sensor_data_logs` DISABLE KEYS */;
/*!40000 ALTER TABLE `sensor_data_logs` ENABLE KEYS */;
UNLOCK TABLES;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_sdl_sync` BEFORE INSERT ON `sensor_data_logs` FOR EACH ROW BEGIN
  CALL ref_touch('sensor_type', NEW.sensor_type, NEW.sensor_type);
  IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.sensor_type_id = LAST_INSERT_ID(); END IF;

  
  IF NEW.unit IS NOT NULL THEN
    INSERT INTO units(code,name,quantity) VALUES (NEW.unit, NEW.unit, NULL)
    ON DUPLICATE KEY UPDATE id=LAST_INSERT_ID(id), name=VALUES(name);
    SET NEW.unit_id = LAST_INSERT_ID();
  END IF;
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_sdl_sync_u` BEFORE UPDATE ON `sensor_data_logs` FOR EACH ROW BEGIN
  IF (NEW.sensor_type <=> OLD.sensor_type) = 0 THEN
    CALL ref_touch('sensor_type', NEW.sensor_type, NEW.sensor_type);
    IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.sensor_type_id = LAST_INSERT_ID(); END IF;
  END IF;

  IF (NEW.unit <=> OLD.unit) = 0 AND NEW.unit IS NOT NULL THEN
    INSERT INTO units(code,name) VALUES (NEW.unit, NEW.unit)
    ON DUPLICATE KEY UPDATE id=LAST_INSERT_ID(id), name=VALUES(name);
    SET NEW.unit_id = LAST_INSERT_ID();
  END IF;
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Table structure for table `sensor_models`
--

DROP TABLE IF EXISTS `sensor_models`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `sensor_models` (
  `id` int NOT NULL AUTO_INCREMENT,
  `vendor` varchar(120) DEFAULT NULL,
  `model_code` varchar(100) DEFAULT NULL,
  `sensor_type_code` varchar(80) DEFAULT NULL,
  `unit_code` varchar(20) DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `ux_sensor_model` (`vendor`,`model_code`)
) ENGINE=InnoDB AUTO_INCREMENT=33 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `sensor_models`
--

LOCK TABLES `sensor_models` WRITE;
/*!40000 ALTER TABLE `sensor_models` DISABLE KEYS */;
INSERT INTO `sensor_models` VALUES (1,'Sensirion','SHT31-TEMP','temperatura','┬░C','2025-08-28 12:36:26','2025-08-28 12:36:26'),(2,'Sensirion','SHT31-RH','vlaga','%','2025-08-28 12:36:26','2025-08-28 12:36:26'),(3,'Bosch','BMP280','tlak','Pa','2025-08-28 12:36:26','2025-08-28 12:36:26'),(4,'Omega','PT100','temperatura','┬░C','2025-08-28 12:36:26','2025-08-28 12:36:26'),(5,'Siemens','SITRANS-F','protok','L/min','2025-08-28 12:36:26','2025-08-28 12:36:26'),(6,'IFM','PN2094','tlak','Pa','2025-08-28 12:36:26','2025-08-28 12:36:26'),(7,'Honeywell','ABP-PA','tlak','Pa','2025-08-28 12:36:26','2025-08-28 12:36:26'),(8,'Keyence','FT50','protok','L/min','2025-08-28 12:36:26','2025-08-28 12:36:26');
/*!40000 ALTER TABLE `sensor_models` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `sensor_types`
--

DROP TABLE IF EXISTS `sensor_types`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `sensor_types` (
  `id` int NOT NULL AUTO_INCREMENT,
  `code` varchar(20) DEFAULT NULL,
  `name` varchar(100) DEFAULT NULL,
  `default_unit_id` int DEFAULT NULL,
  `accuracy` decimal(6,3) DEFAULT NULL,
  `range_min` decimal(10,3) DEFAULT NULL,
  `range_max` decimal(10,3) DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `code` (`code`),
  KEY `fk_st_unit` (`default_unit_id`),
  CONSTRAINT `fk_st_unit` FOREIGN KEY (`default_unit_id`) REFERENCES `measurement_units` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `sensor_types`
--

LOCK TABLES `sensor_types` WRITE;
/*!40000 ALTER TABLE `sensor_types` DISABLE KEYS */;
/*!40000 ALTER TABLE `sensor_types` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `session_log`
--

DROP TABLE IF EXISTS `session_log`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `session_log` (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_id` int NOT NULL,
  `session_token` varchar(128) NOT NULL,
  `login_time` datetime DEFAULT CURRENT_TIMESTAMP,
  `logout_time` datetime DEFAULT NULL,
  `ip_address` varchar(45) DEFAULT NULL,
  `device_info` varchar(255) DEFAULT NULL,
  `mfa_success` tinyint(1) DEFAULT '1',
  `reason` text,
  `is_terminated` tinyint(1) DEFAULT '0',
  `terminated_by` int DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `timestamp` datetime GENERATED ALWAYS AS (`login_time`) VIRTUAL,
  `action` varchar(20) GENERATED ALWAYS AS (if((`is_terminated` = 1),_cp852'TERMINATE',_cp852'SESSION')) VIRTUAL,
  `details` text GENERATED ALWAYS AS (`reason`) VIRTUAL,
  PRIMARY KEY (`id`),
  KEY `fk_sl_user` (`user_id`),
  KEY `fk_sl_by` (`terminated_by`),
  CONSTRAINT `fk_sl_by` FOREIGN KEY (`terminated_by`) REFERENCES `users` (`id`),
  CONSTRAINT `fk_sl_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `session_log`
--

LOCK TABLES `session_log` WRITE;
/*!40000 ALTER TABLE `session_log` DISABLE KEYS */;
/*!40000 ALTER TABLE `session_log` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `sites`
--

DROP TABLE IF EXISTS `sites`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `sites` (
  `id` int NOT NULL AUTO_INCREMENT,
  `code` varchar(20) DEFAULT NULL,
  `name` varchar(100) DEFAULT NULL,
  `timezone` varchar(40) DEFAULT 'Europe/Zagreb',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `code` (`code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `sites`
--

LOCK TABLES `sites` WRITE;
/*!40000 ALTER TABLE `sites` DISABLE KEYS */;
/*!40000 ALTER TABLE `sites` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `sop_document_log`
--

DROP TABLE IF EXISTS `sop_document_log`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `sop_document_log` (
  `id` int NOT NULL AUTO_INCREMENT,
  `sop_document_id` int DEFAULT NULL,
  `action` enum('create','update','archive','approve','invalidate','review','new_version') DEFAULT NULL,
  `performed_by` int DEFAULT NULL,
  `performed_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `note` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_soplog_doc` (`sop_document_id`),
  KEY `fk_soplog_user` (`performed_by`),
  CONSTRAINT `fk_soplog_doc` FOREIGN KEY (`sop_document_id`) REFERENCES `sop_documents` (`id`),
  CONSTRAINT `fk_soplog_user` FOREIGN KEY (`performed_by`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `sop_document_log`
--

LOCK TABLES `sop_document_log` WRITE;
/*!40000 ALTER TABLE `sop_document_log` DISABLE KEYS */;
/*!40000 ALTER TABLE `sop_document_log` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `sop_documents`
--

DROP TABLE IF EXISTS `sop_documents`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `sop_documents` (
  `id` int NOT NULL AUTO_INCREMENT,
  `code` varchar(50) DEFAULT NULL,
  `name` varchar(255) DEFAULT NULL,
  `version` varchar(40) DEFAULT NULL,
  `status` enum('draft','active','archived','obsolete') DEFAULT NULL,
  `created_by` int DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `approved_by` int DEFAULT NULL,
  `approved_at` datetime DEFAULT NULL,
  `file_path` varchar(255) DEFAULT NULL,
  `notes` text,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `code` (`code`),
  KEY `fk_sop_created` (`created_by`),
  KEY `fk_sop_approved` (`approved_by`),
  CONSTRAINT `fk_sop_approved` FOREIGN KEY (`approved_by`) REFERENCES `users` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_sop_created` FOREIGN KEY (`created_by`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `sop_documents`
--

LOCK TABLES `sop_documents` WRITE;
/*!40000 ALTER TABLE `sop_documents` DISABLE KEYS */;
/*!40000 ALTER TABLE `sop_documents` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `stock_levels`
--

DROP TABLE IF EXISTS `stock_levels`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `stock_levels` (
  `id` int NOT NULL AUTO_INCREMENT,
  `part_id` int DEFAULT NULL,
  `warehouse_id` int DEFAULT NULL,
  `quantity` int DEFAULT NULL,
  `min_threshold` int DEFAULT NULL,
  `max_threshold` int DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_sl_part` (`part_id`),
  KEY `fk_sl_warehouse` (`warehouse_id`),
  CONSTRAINT `fk_sl_part` FOREIGN KEY (`part_id`) REFERENCES `parts` (`id`),
  CONSTRAINT `fk_sl_warehouse` FOREIGN KEY (`warehouse_id`) REFERENCES `warehouses` (`id`),
  CONSTRAINT `chk_qty_nonneg` CHECK ((`quantity` >= 0)),
  CONSTRAINT `chk_sl_ranges` CHECK (((`min_threshold` is null) or (`max_threshold` is null) or (`max_threshold` >= `min_threshold`)))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `stock_levels`
--

LOCK TABLES `stock_levels` WRITE;
/*!40000 ALTER TABLE `stock_levels` DISABLE KEYS */;
/*!40000 ALTER TABLE `stock_levels` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `supplier_risk_audit`
--

DROP TABLE IF EXISTS `supplier_risk_audit`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `supplier_risk_audit` (
  `id` int NOT NULL AUTO_INCREMENT,
  `supplier_id` int DEFAULT NULL,
  `audit_date` date DEFAULT NULL,
  `score` int DEFAULT NULL,
  `performed_by` int DEFAULT NULL,
  `findings` text,
  `corrective_actions` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_sra_supplier` (`supplier_id`),
  KEY `fk_sra_user` (`performed_by`),
  CONSTRAINT `fk_sra_supplier` FOREIGN KEY (`supplier_id`) REFERENCES `suppliers` (`id`),
  CONSTRAINT `fk_sra_user` FOREIGN KEY (`performed_by`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `supplier_risk_audit`
--

LOCK TABLES `supplier_risk_audit` WRITE;
/*!40000 ALTER TABLE `supplier_risk_audit` DISABLE KEYS */;
/*!40000 ALTER TABLE `supplier_risk_audit` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `suppliers`
--

DROP TABLE IF EXISTS `suppliers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `suppliers` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(100) DEFAULT NULL,
  `code` varchar(50) DEFAULT NULL,
  `oib` varchar(40) DEFAULT NULL,
  `contact` varchar(100) DEFAULT NULL,
  `email` varchar(100) DEFAULT NULL,
  `phone` varchar(50) DEFAULT NULL,
  `address` text,
  `type` varchar(40) DEFAULT NULL,
  `status` varchar(40) DEFAULT NULL,
  `contract_start` date DEFAULT NULL,
  `contract_end` date DEFAULT NULL,
  `cert_doc` varchar(255) DEFAULT NULL,
  `valid_until` date DEFAULT NULL,
  `risk_score` int DEFAULT '50',
  `last_audit` date DEFAULT NULL,
  `comment` text,
  `digital_signature` varchar(128) DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `status_id` int DEFAULT NULL,
  `type_id` int DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `idx_sup_status_id` (`status_id`),
  KEY `idx_sup_type_id` (`type_id`),
  CONSTRAINT `fk_sup_status` FOREIGN KEY (`status_id`) REFERENCES `ref_value` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_sup_type` FOREIGN KEY (`type_id`) REFERENCES `ref_value` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `suppliers`
--

LOCK TABLES `suppliers` WRITE;
/*!40000 ALTER TABLE `suppliers` DISABLE KEYS */;
/*!40000 ALTER TABLE `suppliers` ENABLE KEYS */;
UNLOCK TABLES;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_sup_sync` BEFORE INSERT ON `suppliers` FOR EACH ROW BEGIN
  CALL ref_touch('supplier_status', NEW.status, NEW.status);
  IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.status_id = LAST_INSERT_ID(); END IF;
  CALL ref_touch('supplier_type', NEW.type, NEW.type);
  IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.type_id = LAST_INSERT_ID(); END IF;
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_sup_sync_u` BEFORE UPDATE ON `suppliers` FOR EACH ROW BEGIN
  IF (NEW.status <=> OLD.status) = 0 THEN
    CALL ref_touch('supplier_status', NEW.status, NEW.status);
    IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.status_id = LAST_INSERT_ID(); END IF;
  END IF;
  IF (NEW.type <=> OLD.type) = 0 THEN
    CALL ref_touch('supplier_type', NEW.type, NEW.type);
    IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.type_id = LAST_INSERT_ID(); END IF;
  END IF;
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Table structure for table `system_event_log`
--

DROP TABLE IF EXISTS `system_event_log`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `system_event_log` (
  `id` int NOT NULL AUTO_INCREMENT,
  `event_time` datetime DEFAULT CURRENT_TIMESTAMP,
  `timestamp` datetime DEFAULT CURRENT_TIMESTAMP,
  `user_id` int DEFAULT NULL,
  `event_type` varchar(100) DEFAULT NULL,
  `action` varchar(100) DEFAULT NULL,
  `table_name` varchar(100) DEFAULT NULL,
  `related_module` varchar(100) DEFAULT NULL,
  `record_id` int DEFAULT NULL,
  `field_name` varchar(100) DEFAULT NULL,
  `old_value` text,
  `new_value` text,
  `description` text,
  `details` text,
  `source_ip` varchar(45) DEFAULT NULL,
  `device_info` varchar(255) DEFAULT NULL,
  `session_id` varchar(100) DEFAULT NULL,
  `severity` varchar(20) DEFAULT NULL,
  `processed` tinyint(1) DEFAULT '0',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_sel_user` (`user_id`),
  KEY `idx_sel_time_sev` (`timestamp`,`severity`),
  CONSTRAINT `fk_sel_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=14939 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `system_event_log`
--

LOCK TABLES `system_event_log` WRITE;
/*!40000 ALTER TABLE `system_event_log` DISABLE KEYS */;
INSERT INTO `system_event_log` VALUES (14933,'2025-08-29 07:52:22','2025-08-29 07:52:22',1,'LOGIN_ATTEMPT','LOGIN_ATTEMPT','users','',1,NULL,NULL,NULL,'username=darko; note=attempt','username=darko; note=attempt','192.168.108.54','OS=Microsoft Windows NT 10.0.26100.0; FW=.NET 8.0.19; Arch=X64/X64; Platform=WinUI; Mfr=LENOVO; Model=11ED002CCR; Host=Y-077; User=amir; Domain=yasenka; App=1.0.0.0(0); Sess=560db2f41abe4bbb8f45f96a; IPv4=192.168.108.54','560db2f41abe4bbb8f45f96ab872f8fc','info',0,'2025-08-29 07:52:22','2025-08-29 07:52:22'),(14934,'2025-08-29 07:52:22','2025-08-29 07:52:22',1,'LOGIN_SUCCESS','LOGIN_SUCCESS','users','',1,NULL,NULL,NULL,'username=darko; auth=local','username=darko; auth=local','192.168.108.54','OS=Microsoft Windows NT 10.0.26100.0; FW=.NET 8.0.19; Arch=X64/X64; Platform=WinUI; Mfr=LENOVO; Model=11ED002CCR; Host=Y-077; User=amir; Domain=yasenka; App=1.0.0.0(0); Sess=560db2f41abe4bbb8f45f96a; IPv4=192.168.108.54','560db2f41abe4bbb8f45f96ab872f8fc','info',0,'2025-08-29 07:52:22','2025-08-29 07:52:22'),(14935,'2025-08-29 07:56:05','2025-08-29 07:56:05',NULL,'DbError','DbError','-','DatabaseService',NULL,NULL,NULL,NULL,'Unknown column \'password\' in \'field list\'','Unknown column \'password\' in \'field list\'','system','server','','warning',0,'2025-08-29 07:56:05','2025-08-29 07:56:05'),(14936,'2025-08-29 07:56:06','2025-08-29 07:56:06',NULL,'DbError','DbError','-','DatabaseService',NULL,NULL,NULL,NULL,'Unknown column \'password\' in \'field list\'','Unknown column \'password\' in \'field list\'','system','server','','warning',0,'2025-08-29 07:56:06','2025-08-29 07:56:06'),(14937,'2025-08-29 07:56:06','2025-08-29 07:56:06',NULL,'DbError','DbError','-','DatabaseService',NULL,NULL,NULL,NULL,'Unknown column \'password\' in \'field list\'','Unknown column \'password\' in \'field list\'','system','server','','warning',0,'2025-08-29 07:56:06','2025-08-29 07:56:06'),(14938,'2025-08-29 07:56:06','2025-08-29 07:56:06',NULL,'DbError','DbError','-','DatabaseService',NULL,NULL,NULL,NULL,'Unknown column \'password\' in \'field list\'','Unknown column \'password\' in \'field list\'','system','server','','error',0,'2025-08-29 07:56:06','2025-08-29 07:56:06');
/*!40000 ALTER TABLE `system_event_log` ENABLE KEYS */;
UNLOCK TABLES;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_system_event_log_bi` BEFORE INSERT ON `system_event_log` FOR EACH ROW BEGIN
  
  IF NEW.event_time IS NULL THEN SET NEW.event_time = CURRENT_TIMESTAMP; END IF;
  IF NEW.`timestamp` IS NULL THEN SET NEW.`timestamp` = NEW.event_time; END IF;

  
  IF NEW.event_type IS NULL AND NEW.`action` IS NOT NULL THEN SET NEW.event_type = NEW.`action`; END IF;
  IF NEW.`action` IS NULL THEN SET NEW.`action` = NEW.event_type; END IF;

  
  IF NEW.description IS NULL AND NEW.`details` IS NOT NULL THEN SET NEW.description = NEW.`details`; END IF;
  IF NEW.`details` IS NULL THEN SET NEW.`details` = NEW.description; END IF;
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_system_event_log_fk_guard` BEFORE INSERT ON `system_event_log` FOR EACH ROW BEGIN
  IF NEW.user_id IS NOT NULL THEN
    IF (SELECT COUNT(*) FROM users WHERE id = NEW.user_id) = 0 THEN
      SET NEW.user_id = NULL;
    END IF;
  END IF;
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_system_event_log_bu` BEFORE UPDATE ON `system_event_log` FOR EACH ROW BEGIN
  
  IF (NEW.event_time <=> OLD.event_time) = 0 THEN
    SET NEW.`timestamp` = NEW.event_time;
  END IF;

  
  IF ((NEW.event_type <=> OLD.event_type) = 0) OR ((NEW.`action` <=> OLD.`action`) = 0) THEN
    IF (NEW.event_type <=> OLD.event_type) = 0 THEN
      SET NEW.`action` = NEW.event_type;
    ELSEIF (NEW.`action` <=> OLD.`action`) = 0 THEN
      SET NEW.event_type = NEW.`action`;
    END IF;
  END IF;

  
  IF ((NEW.description <=> OLD.description) = 0) OR ((NEW.`details` <=> OLD.`details`) = 0) THEN
    IF (NEW.description <=> OLD.description) = 0 THEN
      SET NEW.`details` = NEW.description;
    ELSEIF (NEW.`details` <=> OLD.`details`) = 0 THEN
      SET NEW.description = NEW.`details`;
    END IF;
  END IF;
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Table structure for table `system_parameters`
--

DROP TABLE IF EXISTS `system_parameters`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `system_parameters` (
  `id` int NOT NULL AUTO_INCREMENT,
  `param_name` varchar(100) DEFAULT NULL,
  `param_value` text,
  `updated_by` int DEFAULT NULL,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `note` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `param_name` (`param_name`),
  KEY `fk_sysparam_user` (`updated_by`),
  CONSTRAINT `fk_sysparam_user` FOREIGN KEY (`updated_by`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `system_parameters`
--

LOCK TABLES `system_parameters` WRITE;
/*!40000 ALTER TABLE `system_parameters` DISABLE KEYS */;
/*!40000 ALTER TABLE `system_parameters` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tags`
--

DROP TABLE IF EXISTS `tags`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tags` (
  `id` int NOT NULL AUTO_INCREMENT,
  `tag` varchar(60) NOT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `tag` (`tag`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tags`
--

LOCK TABLES `tags` WRITE;
/*!40000 ALTER TABLE `tags` DISABLE KEYS */;
/*!40000 ALTER TABLE `tags` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tenants`
--

DROP TABLE IF EXISTS `tenants`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tenants` (
  `id` int NOT NULL AUTO_INCREMENT,
  `code` varchar(40) DEFAULT NULL,
  `name` varchar(100) DEFAULT NULL,
  `contact` varchar(100) DEFAULT NULL,
  `is_active` tinyint(1) DEFAULT '1',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `code` (`code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tenants`
--

LOCK TABLES `tenants` WRITE;
/*!40000 ALTER TABLE `tenants` DISABLE KEYS */;
/*!40000 ALTER TABLE `tenants` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `user_audit`
--

DROP TABLE IF EXISTS `user_audit`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `user_audit` (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_id` int DEFAULT NULL,
  `action` varchar(40) NOT NULL,
  `description` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `source_ip` varchar(45) DEFAULT NULL,
  `device_info` varchar(255) DEFAULT NULL,
  `session_id` varchar(100) DEFAULT NULL,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `details` text GENERATED ALWAYS AS (`description`) VIRTUAL,
  PRIMARY KEY (`id`),
  KEY `fk_ua_user` (`user_id`),
  CONSTRAINT `fk_ua_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `user_audit`
--

LOCK TABLES `user_audit` WRITE;
/*!40000 ALTER TABLE `user_audit` DISABLE KEYS */;
/*!40000 ALTER TABLE `user_audit` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `user_esignatures`
--

DROP TABLE IF EXISTS `user_esignatures`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `user_esignatures` (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_id` int DEFAULT NULL,
  `signed_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `action` varchar(100) DEFAULT NULL,
  `table_name` varchar(100) DEFAULT NULL,
  `record_id` int DEFAULT NULL,
  `signature_hash` varchar(255) DEFAULT NULL,
  `method` enum('pin','certificate','biometric','password') DEFAULT NULL,
  `status` enum('valid','revoked','pending') DEFAULT 'valid',
  `ip_address` varchar(45) DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_ues_user` (`user_id`),
  CONSTRAINT `fk_ues_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `user_esignatures`
--

LOCK TABLES `user_esignatures` WRITE;
/*!40000 ALTER TABLE `user_esignatures` DISABLE KEYS */;
/*!40000 ALTER TABLE `user_esignatures` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `user_login_audit`
--

DROP TABLE IF EXISTS `user_login_audit`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `user_login_audit` (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_id` int DEFAULT NULL,
  `login_time` datetime DEFAULT CURRENT_TIMESTAMP,
  `logout_time` datetime DEFAULT NULL,
  `session_token` varchar(128) DEFAULT NULL,
  `ip_address` varchar(45) DEFAULT NULL,
  `device_info` varchar(255) DEFAULT NULL,
  `success` tinyint(1) DEFAULT '1',
  `reason` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `timestamp` datetime GENERATED ALWAYS AS (`login_time`) VIRTUAL,
  `action` varchar(20) GENERATED ALWAYS AS (if((`success` = 1),_cp852'LOGIN',_cp852'LOGIN_FAIL')) VIRTUAL,
  `details` text GENERATED ALWAYS AS (`reason`) VIRTUAL,
  PRIMARY KEY (`id`),
  KEY `fk_ula_user` (`user_id`),
  CONSTRAINT `fk_ula_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `user_login_audit`
--

LOCK TABLES `user_login_audit` WRITE;
/*!40000 ALTER TABLE `user_login_audit` DISABLE KEYS */;
/*!40000 ALTER TABLE `user_login_audit` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `user_permissions`
--

DROP TABLE IF EXISTS `user_permissions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `user_permissions` (
  `user_id` int NOT NULL,
  `permission_id` int NOT NULL,
  `allowed` tinyint(1) NOT NULL,
  `reason` varchar(255) DEFAULT NULL,
  `granted_by` int DEFAULT NULL,
  `granted_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `expires_at` datetime DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`user_id`,`permission_id`),
  KEY `fk_up_perm` (`permission_id`),
  KEY `fk_up_by` (`granted_by`),
  CONSTRAINT `fk_up_by` FOREIGN KEY (`granted_by`) REFERENCES `users` (`id`),
  CONSTRAINT `fk_up_perm` FOREIGN KEY (`permission_id`) REFERENCES `permissions` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_up_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `user_permissions`
--

LOCK TABLES `user_permissions` WRITE;
/*!40000 ALTER TABLE `user_permissions` DISABLE KEYS */;
INSERT INTO `user_permissions` VALUES (1,4,1,'bootstrap grant',NULL,'2025-08-28 14:05:44',NULL,'2025-08-28 14:05:44','2025-08-28 14:05:44');
/*!40000 ALTER TABLE `user_permissions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `user_roles`
--

DROP TABLE IF EXISTS `user_roles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `user_roles` (
  `user_id` int NOT NULL,
  `role_id` int NOT NULL,
  `assigned_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `assigned_by` int DEFAULT NULL,
  `expires_at` datetime DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `granted_by` int DEFAULT NULL,
  `granted_at` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`user_id`,`role_id`),
  KEY `fk_ur_role` (`role_id`),
  KEY `fk_ur_assigned_by` (`assigned_by`),
  KEY `ix_user_roles_granted_by` (`granted_by`),
  CONSTRAINT `fk_ur_assigned_by` FOREIGN KEY (`assigned_by`) REFERENCES `users` (`id`),
  CONSTRAINT `fk_ur_role` FOREIGN KEY (`role_id`) REFERENCES `roles` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_ur_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_user_roles_granted_by` FOREIGN KEY (`granted_by`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `user_roles`
--

LOCK TABLES `user_roles` WRITE;
/*!40000 ALTER TABLE `user_roles` DISABLE KEYS */;
INSERT INTO `user_roles` VALUES (1,1,'2025-08-25 11:36:22',NULL,NULL,'2025-08-25 11:36:22','2025-08-25 11:36:22',NULL,'2025-08-28 11:52:14'),(8,1,'2025-08-28 12:07:05',NULL,NULL,'2025-08-28 12:07:05','2025-08-28 12:07:05',1,'2025-08-28 12:07:05');
/*!40000 ALTER TABLE `user_roles` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `user_subscriptions`
--

DROP TABLE IF EXISTS `user_subscriptions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `user_subscriptions` (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_id` int NOT NULL,
  `template_id` int NOT NULL,
  `enabled` tinyint(1) DEFAULT '1',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `ux_user_tpl` (`user_id`,`template_id`),
  KEY `fk_us_tpl` (`template_id`),
  CONSTRAINT `fk_us_tpl` FOREIGN KEY (`template_id`) REFERENCES `notification_templates` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_us_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `user_subscriptions`
--

LOCK TABLES `user_subscriptions` WRITE;
/*!40000 ALTER TABLE `user_subscriptions` DISABLE KEYS */;
INSERT INTO `user_subscriptions` VALUES (1,1,3,1,'2025-08-28 12:36:27','2025-08-28 12:36:27'),(2,1,8,1,'2025-08-28 12:36:27','2025-08-28 12:36:27'),(3,1,1,1,'2025-08-28 12:36:27','2025-08-28 12:36:27'),(4,1,2,1,'2025-08-28 12:36:27','2025-08-28 12:36:27');
/*!40000 ALTER TABLE `user_subscriptions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `user_training`
--

DROP TABLE IF EXISTS `user_training`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `user_training` (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_id` int DEFAULT NULL,
  `training_type` varchar(100) DEFAULT NULL,
  `training_date` date DEFAULT NULL,
  `valid_until` date DEFAULT NULL,
  `certificate_file` varchar(255) DEFAULT NULL,
  `provider` varchar(100) DEFAULT NULL,
  `status` enum('planned','completed','expired') DEFAULT NULL,
  `notes` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_ut_user` (`user_id`),
  CONSTRAINT `fk_ut_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `user_training`
--

LOCK TABLES `user_training` WRITE;
/*!40000 ALTER TABLE `user_training` DISABLE KEYS */;
/*!40000 ALTER TABLE `user_training` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `users`
--

DROP TABLE IF EXISTS `users`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `users` (
  `id` int NOT NULL AUTO_INCREMENT,
  `username` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `password_hash` varchar(128) NOT NULL,
  `full_name` varchar(100) NOT NULL,
  `role` varchar(30) DEFAULT NULL,
  `active` tinyint(1) DEFAULT '1',
  `is_locked` tinyint(1) DEFAULT '0',
  `failed_logins` int DEFAULT '0',
  `last_failed_login` datetime DEFAULT NULL,
  `email` varchar(100) DEFAULT NULL,
  `phone` varchar(40) DEFAULT NULL,
  `last_login` datetime DEFAULT NULL,
  `password_reset_required` tinyint(1) DEFAULT '0',
  `is_deleted` tinyint(1) DEFAULT '0',
  `deleted_at` datetime DEFAULT NULL,
  `digital_signature` varchar(128) DEFAULT NULL,
  `department_id` int DEFAULT NULL,
  `department_name` varchar(80) DEFAULT NULL,
  `is_two_factor_enabled` tinyint(1) DEFAULT '0',
  `privacy_consent_version` varchar(20) DEFAULT NULL,
  `privacy_consent_date` datetime DEFAULT NULL,
  `custom_attributes` text,
  `security_anomaly_score` decimal(5,3) DEFAULT NULL,
  `is_system_account` tinyint(1) DEFAULT '0',
  `notification_channel` varchar(24) DEFAULT NULL,
  `external_provider_id` varchar(255) DEFAULT NULL,
  `external_provider_type` varchar(40) DEFAULT NULL,
  `federated_unique_id` varchar(255) DEFAULT NULL,
  `global_federated_id` varchar(255) DEFAULT NULL,
  `preferred_culture` varchar(16) DEFAULT NULL,
  `last_change_signature` varchar(256) DEFAULT NULL,
  `public_key` longtext,
  `created_at` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `last_modified` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `last_modified_by_id` int DEFAULT NULL,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `failed_login_attempts` int DEFAULT '0',
  `tenant_id` int DEFAULT NULL,
  `job_title_id` int DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `ux_users_username_lower` ((lower(`username`))),
  KEY `fk_users_last_modified_by` (`last_modified_by_id`),
  KEY `fk_users_department` (`department_id`),
  KEY `fk_users_tenant_id` (`tenant_id`),
  KEY `fk_users_job_title` (`job_title_id`),
  CONSTRAINT `fk_users_department` FOREIGN KEY (`department_id`) REFERENCES `departments` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_users_job_title` FOREIGN KEY (`job_title_id`) REFERENCES `job_titles` (`id`),
  CONSTRAINT `fk_users_last_modified_by` FOREIGN KEY (`last_modified_by_id`) REFERENCES `users` (`id`),
  CONSTRAINT `fk_users_tenant_id` FOREIGN KEY (`tenant_id`) REFERENCES `tenants` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `users`
--

LOCK TABLES `users` WRITE;
/*!40000 ALTER TABLE `users` DISABLE KEYS */;
INSERT INTO `users` VALUES (1,'darko','B0gPuehbk5avBvAGzxyVAkryUxxl+1Bc+9Ct0eLzFXM=','Darko M.',NULL,1,0,0,NULL,NULL,NULL,'2025-08-29 07:52:22',0,0,NULL,NULL,NULL,NULL,0,NULL,NULL,NULL,NULL,0,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'2025-08-25 09:36:22','2025-08-29 05:52:22',NULL,'2025-08-29 07:52:22',0,NULL,NULL),(7,'system','B0gPuehbk5avBvAGzxyVAkryUxxl+1Bc+9Ct0eLzFXM=','System Account',NULL,1,0,0,NULL,NULL,NULL,NULL,0,0,NULL,NULL,NULL,NULL,0,NULL,NULL,NULL,NULL,1,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'2025-08-25 11:15:25','2025-08-25 11:15:25',NULL,'2025-08-25 13:15:25',0,NULL,NULL),(8,'amir','B0gPuehbk5avBvAGzxyVAkryUxxl+1Bc+9Ct0eLzFXM=','Amir Reslan',NULL,1,0,0,NULL,NULL,NULL,NULL,0,0,NULL,NULL,NULL,NULL,0,NULL,NULL,NULL,NULL,0,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'2025-08-25 11:15:25','2025-08-25 11:15:25',NULL,'2025-08-25 13:15:25',0,NULL,NULL);
/*!40000 ALTER TABLE `users` ENABLE KEYS */;
UNLOCK TABLES;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_users_bi_login_attempts_sync` BEFORE INSERT ON `users` FOR EACH ROW BEGIN
  SET NEW.failed_login_attempts = COALESCE(NEW.failed_login_attempts, NEW.failed_logins, 0);
  SET NEW.failed_logins         = COALESCE(NEW.failed_login_attempts, NEW.failed_logins, 0);
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_users_bu_login_attempts_sync` BEFORE UPDATE ON `users` FOR EACH ROW BEGIN
  IF (NEW.failed_login_attempts <=> OLD.failed_login_attempts) = 0
     OR (NEW.failed_logins <=> OLD.failed_logins) = 0 THEN
    SET @v := COALESCE(NEW.failed_login_attempts, NEW.failed_logins, 0);
    SET NEW.failed_login_attempts = @v;
    SET NEW.failed_logins         = @v;
  END IF;
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Table structure for table `validations`
--

DROP TABLE IF EXISTS `validations`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `validations` (
  `id` int NOT NULL AUTO_INCREMENT,
  `type` varchar(40) DEFAULT NULL,
  `machine_id` int DEFAULT NULL,
  `component_id` int DEFAULT NULL,
  `validation_date` date DEFAULT NULL,
  `status` varchar(40) DEFAULT NULL,
  `doc_file` varchar(255) DEFAULT NULL,
  `signed_by` varchar(128) DEFAULT NULL,
  `next_due` date DEFAULT NULL,
  `comment` text,
  `digital_signature` varchar(128) DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_val_machine` (`machine_id`),
  KEY `fk_val_component` (`component_id`),
  CONSTRAINT `fk_val_component` FOREIGN KEY (`component_id`) REFERENCES `machine_components` (`id`),
  CONSTRAINT `fk_val_machine` FOREIGN KEY (`machine_id`) REFERENCES `machines` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `validations`
--

LOCK TABLES `validations` WRITE;
/*!40000 ALTER TABLE `validations` DISABLE KEYS */;
/*!40000 ALTER TABLE `validations` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Temporary view structure for view `vw_calibrations_filter`
--

DROP TABLE IF EXISTS `vw_calibrations_filter`;
/*!50001 DROP VIEW IF EXISTS `vw_calibrations_filter`*/;
SET @saved_cs_client     = @@character_set_client;
/*!50503 SET character_set_client = utf8mb4 */;
/*!50001 CREATE VIEW `vw_calibrations_filter` AS SELECT 
 1 AS `id`,
 1 AS `component_name`,
 1 AS `supplier_name`,
 1 AS `calibration_date`,
 1 AS `next_due`,
 1 AS `result`,
 1 AS `comment`,
 1 AS `digital_signature`*/;
SET character_set_client = @saved_cs_client;

--
-- Temporary view structure for view `vw_scheduled_jobs_due`
--

DROP TABLE IF EXISTS `vw_scheduled_jobs_due`;
/*!50001 DROP VIEW IF EXISTS `vw_scheduled_jobs_due`*/;
SET @saved_cs_client     = @@character_set_client;
/*!50503 SET character_set_client = utf8mb4 */;
/*!50001 CREATE VIEW `vw_scheduled_jobs_due` AS SELECT 
 1 AS `id`,
 1 AS `name`,
 1 AS `job_type`,
 1 AS `entity_type`,
 1 AS `entity_id`,
 1 AS `status`,
 1 AS `next_due`,
 1 AS `recurrence_pattern`,
 1 AS `cron_expression`,
 1 AS `last_executed`,
 1 AS `last_result`,
 1 AS `escalation_level`,
 1 AS `escalation_note`,
 1 AS `chain_job_id`,
 1 AS `is_critical`,
 1 AS `needs_acknowledgment`,
 1 AS `acknowledged_by`,
 1 AS `acknowledged_at`,
 1 AS `alert_on_failure`,
 1 AS `retries`,
 1 AS `max_retries`,
 1 AS `last_error`,
 1 AS `iot_device_id`,
 1 AS `extra_params`,
 1 AS `created_by`,
 1 AS `created_at`,
 1 AS `last_modified`,
 1 AS `last_modified_by`,
 1 AS `digital_signature`,
 1 AS `device_info`,
 1 AS `session_id`,
 1 AS `ip_address`,
 1 AS `comment`*/;
SET character_set_client = @saved_cs_client;

--
-- Temporary view structure for view `vw_sensor_data_enriched`
--

DROP TABLE IF EXISTS `vw_sensor_data_enriched`;
/*!50001 DROP VIEW IF EXISTS `vw_sensor_data_enriched`*/;
SET @saved_cs_client     = @@character_set_client;
/*!50503 SET character_set_client = utf8mb4 */;
/*!50001 CREATE VIEW `vw_sensor_data_enriched` AS SELECT 
 1 AS `id`,
 1 AS `component_id`,
 1 AS `sensor_type_id`,
 1 AS `unit_id`,
 1 AS `value`,
 1 AS `timestamp`,
 1 AS `created_at`,
 1 AS `sensor_type_name`,
 1 AS `unit_name`*/;
SET character_set_client = @saved_cs_client;

--
-- Temporary view structure for view `vw_stock_current`
--

DROP TABLE IF EXISTS `vw_stock_current`;
/*!50001 DROP VIEW IF EXISTS `vw_stock_current`*/;
SET @saved_cs_client     = @@character_set_client;
/*!50503 SET character_set_client = utf8mb4 */;
/*!50001 CREATE VIEW `vw_stock_current` AS SELECT 
 1 AS `part_id`,
 1 AS `part_code`,
 1 AS `part_name`,
 1 AS `warehouse_id`,
 1 AS `warehouse_name`,
 1 AS `quantity`,
 1 AS `min_threshold`,
 1 AS `max_threshold`*/;
SET character_set_client = @saved_cs_client;

--
-- Temporary view structure for view `vw_work_orders_enriched`
--

DROP TABLE IF EXISTS `vw_work_orders_enriched`;
/*!50001 DROP VIEW IF EXISTS `vw_work_orders_enriched`*/;
SET @saved_cs_client     = @@character_set_client;
/*!50503 SET character_set_client = utf8mb4 */;
/*!50001 CREATE VIEW `vw_work_orders_enriched` AS SELECT 
 1 AS `id`,
 1 AS `machine_id`,
 1 AS `component_id`,
 1 AS `type`,
 1 AS `created_by`,
 1 AS `assigned_to`,
 1 AS `date_open`,
 1 AS `date_close`,
 1 AS `description`,
 1 AS `result`,
 1 AS `status`,
 1 AS `digital_signature`,
 1 AS `priority`,
 1 AS `related_incident`,
 1 AS `created_at`,
 1 AS `updated_at`,
 1 AS `status_id`,
 1 AS `type_id`,
 1 AS `priority_id`,
 1 AS `tenant_id`,
 1 AS `status_name`,
 1 AS `type_name`,
 1 AS `priority_name`,
 1 AS `machine_name`,
 1 AS `component_name`*/;
SET character_set_client = @saved_cs_client;

--
-- Temporary view structure for view `vw_work_orders_user`
--

DROP TABLE IF EXISTS `vw_work_orders_user`;
/*!50001 DROP VIEW IF EXISTS `vw_work_orders_user`*/;
SET @saved_cs_client     = @@character_set_client;
/*!50503 SET character_set_client = utf8mb4 */;
/*!50001 CREATE VIEW `vw_work_orders_user` AS SELECT 
 1 AS `id`,
 1 AS `machine_id`,
 1 AS `component_id`,
 1 AS `type`,
 1 AS `created_by`,
 1 AS `assigned_to`,
 1 AS `date_open`,
 1 AS `date_close`,
 1 AS `description`,
 1 AS `result`,
 1 AS `status`,
 1 AS `digital_signature`,
 1 AS `priority`,
 1 AS `related_incident`,
 1 AS `created_at`,
 1 AS `updated_at`,
 1 AS `status_id`,
 1 AS `type_id`,
 1 AS `priority_id`,
 1 AS `tenant_id`*/;
SET character_set_client = @saved_cs_client;

--
-- Table structure for table `warehouses`
--

DROP TABLE IF EXISTS `warehouses`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `warehouses` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(100) DEFAULT NULL,
  `location` varchar(255) DEFAULT NULL,
  `responsible_id` int DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `location_id` int DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_wh_user` (`responsible_id`),
  KEY `fk_wh_location` (`location_id`),
  CONSTRAINT `fk_wh_location` FOREIGN KEY (`location_id`) REFERENCES `locations` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_wh_user` FOREIGN KEY (`responsible_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `warehouses`
--

LOCK TABLES `warehouses` WRITE;
/*!40000 ALTER TABLE `warehouses` DISABLE KEYS */;
/*!40000 ALTER TABLE `warehouses` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `work_order_audit`
--

DROP TABLE IF EXISTS `work_order_audit`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `work_order_audit` (
  `id` int NOT NULL AUTO_INCREMENT,
  `work_order_id` int NOT NULL,
  `user_id` int NOT NULL,
  `action` enum('CREATE','UPDATE','DELETE','SIGN','EXPORT','ROLLBACK') NOT NULL,
  `old_value` text,
  `new_value` text,
  `changed_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `source_ip` varchar(45) DEFAULT NULL,
  `device_info` varchar(255) DEFAULT NULL,
  `incident_id` int DEFAULT NULL,
  `capa_id` int DEFAULT NULL,
  `note` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_woa_wo` (`work_order_id`),
  KEY `fk_woa_user` (`user_id`),
  CONSTRAINT `fk_woa_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_woa_wo` FOREIGN KEY (`work_order_id`) REFERENCES `work_orders` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `work_order_audit`
--

LOCK TABLES `work_order_audit` WRITE;
/*!40000 ALTER TABLE `work_order_audit` DISABLE KEYS */;
/*!40000 ALTER TABLE `work_order_audit` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `work_order_checklist_item`
--

DROP TABLE IF EXISTS `work_order_checklist_item`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `work_order_checklist_item` (
  `id` int NOT NULL AUTO_INCREMENT,
  `work_order_id` int NOT NULL,
  `item_id` int NOT NULL,
  `result` varchar(255) DEFAULT NULL,
  `ok` tinyint(1) DEFAULT NULL,
  `note` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `ux_wocli` (`work_order_id`,`item_id`),
  KEY `fk_wocli_item` (`item_id`),
  CONSTRAINT `fk_wocli_item` FOREIGN KEY (`item_id`) REFERENCES `checklist_items` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_wocli_wo` FOREIGN KEY (`work_order_id`) REFERENCES `work_orders` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `work_order_checklist_item`
--

LOCK TABLES `work_order_checklist_item` WRITE;
/*!40000 ALTER TABLE `work_order_checklist_item` DISABLE KEYS */;
/*!40000 ALTER TABLE `work_order_checklist_item` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `work_order_comments`
--

DROP TABLE IF EXISTS `work_order_comments`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `work_order_comments` (
  `id` int NOT NULL AUTO_INCREMENT,
  `work_order_id` int NOT NULL,
  `user_id` int NOT NULL,
  `comment` text,
  `revision_no` int DEFAULT '1',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_woc_wo` (`work_order_id`),
  KEY `fk_woc_user` (`user_id`),
  CONSTRAINT `fk_woc_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_woc_wo` FOREIGN KEY (`work_order_id`) REFERENCES `work_orders` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `work_order_comments`
--

LOCK TABLES `work_order_comments` WRITE;
/*!40000 ALTER TABLE `work_order_comments` DISABLE KEYS */;
/*!40000 ALTER TABLE `work_order_comments` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `work_order_parts`
--

DROP TABLE IF EXISTS `work_order_parts`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `work_order_parts` (
  `work_order_id` int NOT NULL,
  `part_id` int NOT NULL,
  `quantity` int DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`work_order_id`,`part_id`),
  KEY `fk_wop_part` (`part_id`),
  CONSTRAINT `fk_wop_part` FOREIGN KEY (`part_id`) REFERENCES `parts` (`id`),
  CONSTRAINT `fk_wop_wo` FOREIGN KEY (`work_order_id`) REFERENCES `work_orders` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `work_order_parts`
--

LOCK TABLES `work_order_parts` WRITE;
/*!40000 ALTER TABLE `work_order_parts` DISABLE KEYS */;
/*!40000 ALTER TABLE `work_order_parts` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `work_order_signatures`
--

DROP TABLE IF EXISTS `work_order_signatures`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `work_order_signatures` (
  `id` int NOT NULL AUTO_INCREMENT,
  `work_order_id` int NOT NULL,
  `user_id` int NOT NULL,
  `signature_hash` varchar(255) DEFAULT NULL,
  `signed_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `pin_used` varchar(20) DEFAULT NULL,
  `signature_type` enum('zakljucavanje','odobrenje','potvrda') DEFAULT 'zakljucavanje',
  `note` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_wos_wo` (`work_order_id`),
  KEY `fk_wos_user` (`user_id`),
  CONSTRAINT `fk_wos_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_wos_wo` FOREIGN KEY (`work_order_id`) REFERENCES `work_orders` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `work_order_signatures`
--

LOCK TABLES `work_order_signatures` WRITE;
/*!40000 ALTER TABLE `work_order_signatures` DISABLE KEYS */;
/*!40000 ALTER TABLE `work_order_signatures` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `work_order_status_log`
--

DROP TABLE IF EXISTS `work_order_status_log`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `work_order_status_log` (
  `id` int NOT NULL AUTO_INCREMENT,
  `work_order_id` int NOT NULL,
  `old_status` varchar(50) DEFAULT NULL,
  `new_status` varchar(50) DEFAULT NULL,
  `changed_by` int DEFAULT NULL,
  `changed_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `note` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_wosl_wo` (`work_order_id`),
  KEY `fk_wosl_user` (`changed_by`),
  CONSTRAINT `fk_wosl_user` FOREIGN KEY (`changed_by`) REFERENCES `users` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_wosl_wo` FOREIGN KEY (`work_order_id`) REFERENCES `work_orders` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `work_order_status_log`
--

LOCK TABLES `work_order_status_log` WRITE;
/*!40000 ALTER TABLE `work_order_status_log` DISABLE KEYS */;
/*!40000 ALTER TABLE `work_order_status_log` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `work_orders`
--

DROP TABLE IF EXISTS `work_orders`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `work_orders` (
  `id` int NOT NULL AUTO_INCREMENT,
  `machine_id` int DEFAULT NULL,
  `component_id` int DEFAULT NULL,
  `type` enum('preventivni','korektivni','vanredni') DEFAULT NULL,
  `created_by` int DEFAULT NULL,
  `assigned_to` int DEFAULT NULL,
  `date_open` date DEFAULT NULL,
  `date_close` date DEFAULT NULL,
  `description` text,
  `result` text,
  `status` enum('otvoren','u_tijeku','zavrsen','odbijen','na_─Źekanju','planiran','otkazan') DEFAULT 'otvoren',
  `digital_signature` varchar(128) DEFAULT NULL,
  `priority` enum('nizak','srednji','visok','kritican') DEFAULT 'srednji',
  `related_incident` int DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `status_id` int DEFAULT NULL,
  `type_id` int DEFAULT NULL,
  `priority_id` int DEFAULT NULL,
  `tenant_id` int DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_wo_comp` (`component_id`),
  KEY `fk_wo_created` (`created_by`),
  KEY `fk_wo_assigned` (`assigned_to`),
  KEY `fk_wo_incident` (`related_incident`),
  KEY `idx_wo_status_id` (`status_id`),
  KEY `idx_wo_type_id` (`type_id`),
  KEY `idx_wo_priority_id` (`priority_id`),
  KEY `idx_wo_machine_stat` (`machine_id`,`status_id`),
  KEY `fk_wo_tenant_id` (`tenant_id`),
  CONSTRAINT `fk_wo_assigned` FOREIGN KEY (`assigned_to`) REFERENCES `users` (`id`),
  CONSTRAINT `fk_wo_comp` FOREIGN KEY (`component_id`) REFERENCES `machine_components` (`id`),
  CONSTRAINT `fk_wo_created` FOREIGN KEY (`created_by`) REFERENCES `users` (`id`),
  CONSTRAINT `fk_wo_incident` FOREIGN KEY (`related_incident`) REFERENCES `incident_log` (`id`),
  CONSTRAINT `fk_wo_machine` FOREIGN KEY (`machine_id`) REFERENCES `machines` (`id`),
  CONSTRAINT `fk_wo_priority` FOREIGN KEY (`priority_id`) REFERENCES `ref_value` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_wo_status` FOREIGN KEY (`status_id`) REFERENCES `ref_value` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_wo_tenant_id` FOREIGN KEY (`tenant_id`) REFERENCES `tenants` (`id`),
  CONSTRAINT `fk_wo_type` FOREIGN KEY (`type_id`) REFERENCES `ref_value` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `work_orders`
--

LOCK TABLES `work_orders` WRITE;
/*!40000 ALTER TABLE `work_orders` DISABLE KEYS */;
/*!40000 ALTER TABLE `work_orders` ENABLE KEYS */;
UNLOCK TABLES;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_wo_sync` BEFORE INSERT ON `work_orders` FOR EACH ROW BEGIN
  
  CALL ref_touch('work_order_status', NEW.status, NEW.status);
  IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.status_id = LAST_INSERT_ID(); END IF;
  
  CALL ref_touch('work_order_type', NEW.type, NEW.type);
  IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.type_id = LAST_INSERT_ID(); END IF;
  
  CALL ref_touch('priority', NEW.priority, NEW.priority);
  IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.priority_id = LAST_INSERT_ID(); END IF;
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50017 DEFINER=`root`@`localhost`*/ /*!50003 TRIGGER `trg_wo_sync_u` BEFORE UPDATE ON `work_orders` FOR EACH ROW BEGIN
  IF (NEW.status <=> OLD.status) = 0 THEN
    CALL ref_touch('work_order_status', NEW.status, NEW.status);
    IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.status_id = LAST_INSERT_ID(); END IF;
  END IF;
  IF (NEW.type <=> OLD.type) = 0 THEN
    CALL ref_touch('work_order_type', NEW.type, NEW.type);
    IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.type_id = LAST_INSERT_ID(); END IF;
  END IF;
  IF (NEW.priority <=> OLD.priority) = 0 THEN
    CALL ref_touch('priority', NEW.priority, NEW.priority);
    IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.priority_id = LAST_INSERT_ID(); END IF;
  END IF;
END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Dumping events for database 'yasgmp'
--
/*!50106 SET @save_time_zone= @@TIME_ZONE */ ;
/*!50106 DROP EVENT IF EXISTS `ev_autotimestamps` */;
DELIMITER ;;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;;
/*!50003 SET character_set_client  = cp852 */ ;;
/*!50003 SET character_set_results = cp852 */ ;;
/*!50003 SET collation_connection  = cp852_general_ci */ ;;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;;
/*!50003 SET @saved_time_zone      = @@time_zone */ ;;
/*!50003 SET time_zone             = 'SYSTEM' */ ;;
/*!50106 CREATE*/ /*!50117 DEFINER=`root`@`localhost`*/ /*!50106 EVENT `ev_autotimestamps` ON SCHEDULE EVERY 1 DAY STARTS '2025-08-25 12:36:22' ON COMPLETION NOT PRESERVE ENABLE DO CALL ensure_timestamps_all() */ ;;
/*!50003 SET time_zone             = @saved_time_zone */ ;;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;;
/*!50003 SET character_set_client  = @saved_cs_client */ ;;
/*!50003 SET character_set_results = @saved_cs_results */ ;;
/*!50003 SET collation_connection  = @saved_col_connection */ ;;
DELIMITER ;
/*!50106 SET TIME_ZONE= @save_time_zone */ ;

--
-- Dumping routines for database 'yasgmp'
--
/*!50003 DROP FUNCTION IF EXISTS `CURRENT_USER_ID` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` FUNCTION `CURRENT_USER_ID`() RETURNS int
    DETERMINISTIC
    SQL SECURITY INVOKER
BEGIN
  
  RETURN NULL;
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `add_column_if_missing` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `add_column_if_missing`(IN p_tbl VARCHAR(64), IN p_col VARCHAR(64), IN p_def TEXT)
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.tables
              WHERE table_schema = DATABASE() AND table_name = p_tbl) THEN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                    WHERE table_schema = DATABASE()
                      AND table_name   = p_tbl
                      AND column_name  = p_col) THEN
      SET @sql := CONCAT('ALTER TABLE `',p_tbl,'` ADD COLUMN `',p_col,'` ',p_def);
      PREPARE ps FROM @sql; EXECUTE ps; DEALLOCATE PREPARE ps;
    END IF;
  END IF;
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `add_fk_if_missing` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `add_fk_if_missing`(IN p_tbl VARCHAR(64), IN p_fk VARCHAR(64), IN p_ddl TEXT)
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.tables
              WHERE table_schema = DATABASE() AND table_name = p_tbl) THEN
    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints
                    WHERE table_schema   = DATABASE()
                      AND table_name     = p_tbl
                      AND constraint_type = 'FOREIGN KEY'
                      AND constraint_name = p_fk) THEN
      SET @sql := p_ddl; PREPARE ps FROM @sql; EXECUTE ps; DEALLOCATE PREPARE ps;
    END IF;
  END IF;
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `add_index_if_missing` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `add_index_if_missing`(IN p_tbl VARCHAR(64), IN p_idx VARCHAR(64), IN p_ddl TEXT)
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.tables
              WHERE table_schema = DATABASE() AND table_name = p_tbl) THEN
    IF NOT EXISTS (SELECT 1 FROM information_schema.statistics
                    WHERE table_schema = DATABASE()
                      AND table_name   = p_tbl
                      AND index_name   = p_idx) THEN
      SET @sql := p_ddl; PREPARE ps FROM @sql; EXECUTE ps; DEALLOCATE PREPARE ps;
    END IF;
  END IF;
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `backfill_catalog_ids` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `backfill_catalog_ids`()
BEGIN
  
  UPDATE work_orders wo
  JOIN ref_domain d ON d.code='work_order_status'
  JOIN ref_value rv ON rv.domain_id=d.id AND rv.code=wo.status
  SET wo.status_id=rv.id
  WHERE wo.status IS NOT NULL;

  UPDATE work_orders wo
  JOIN ref_domain d ON d.code='work_order_type'
  JOIN ref_value rv ON rv.domain_id=d.id AND rv.code=wo.type
  SET wo.type_id=rv.id
  WHERE wo.type IS NOT NULL;

  UPDATE work_orders wo
  JOIN ref_domain d ON d.code='priority'
  JOIN ref_value rv ON rv.domain_id=d.id AND rv.code=wo.priority
  SET wo.priority_id=rv.id
  WHERE wo.priority IS NOT NULL;

  
  UPDATE inspections i
  JOIN ref_domain d1 ON d1.code='inspection_type'
  JOIN ref_value rv1 ON rv1.domain_id=d1.id AND rv1.code=i.type
  SET i.type_id=rv1.id
  WHERE i.type IS NOT NULL;

  UPDATE inspections i
  JOIN ref_domain d2 ON d2.code='inspection_result'
  JOIN ref_value rv2 ON rv2.domain_id=d2.id AND rv2.code=i.result
  SET i.result_id=rv2.id
  WHERE i.result IS NOT NULL;

  
  UPDATE calibrations c
  JOIN ref_domain d ON d.code='calibration_result'
  JOIN ref_value rv ON rv.domain_id=d.id AND rv.code=c.result
  SET c.result_id=rv.id
  WHERE c.result IS NOT NULL;

  
  UPDATE quality_events q
  JOIN ref_domain d ON d.code='quality_event_type'
  JOIN ref_value rv ON rv.domain_id=d.id AND rv.code=q.event_type
  SET q.type_id=rv.id
  WHERE q.event_type IS NOT NULL;

  UPDATE quality_events q
  JOIN ref_domain d ON d.code='quality_status'
  JOIN ref_value rv ON rv.domain_id=d.id AND rv.code=q.status
  SET q.status_id=rv.id
  WHERE q.status IS NOT NULL;

  
  UPDATE machines m
  JOIN ref_domain d ON d.code='asset_status'
  JOIN ref_value rv ON rv.domain_id=d.id AND rv.code=m.status
  SET m.status_id=rv.id
  WHERE m.status IS NOT NULL;

  UPDATE machines m
  JOIN ref_domain d ON d.code='lifecycle_phase'
  JOIN ref_value rv ON rv.domain_id=d.id AND rv.code=m.lifecycle_phase
  SET m.lifecycle_phase_id=rv.id
  WHERE m.lifecycle_phase IS NOT NULL;

  
  UPDATE suppliers s
  JOIN ref_domain d1 ON d1.code='supplier_status'
  JOIN ref_value rv1 ON rv1.domain_id=d1.id AND rv1.code=s.status
  SET s.status_id=rv1.id
  WHERE s.status IS NOT NULL;

  UPDATE suppliers s
  JOIN ref_domain d2 ON d2.code='supplier_type'
  JOIN ref_value rv2 ON rv2.domain_id=d2.id AND rv2.code=s.type
  SET s.type_id=rv2.id
  WHERE s.type IS NOT NULL;
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `ensure_timestamps_all` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `ensure_timestamps_all`()
BEGIN
  DECLARE done INT DEFAULT 0;
  DECLARE tbl VARCHAR(100);
  DECLARE cur CURSOR FOR
    SELECT TABLE_NAME
      FROM INFORMATION_SCHEMA.TABLES
     WHERE TABLE_SCHEMA = DATABASE()
       AND TABLE_TYPE = 'BASE TABLE';
  DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = 1;

  OPEN cur;
  read_loop: LOOP
    FETCH cur INTO tbl;
    IF done THEN
      LEAVE read_loop;
    END IF;

    IF NOT EXISTS (
      SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
       WHERE TABLE_SCHEMA = DATABASE()
         AND TABLE_NAME = tbl
         AND COLUMN_NAME = 'created_at'
    ) THEN
      SET @s = CONCAT('ALTER TABLE ', tbl,
                      ' ADD COLUMN created_at DATETIME DEFAULT CURRENT_TIMESTAMP');
      PREPARE stmt FROM @s; EXECUTE stmt; DEALLOCATE PREPARE stmt;
    END IF;

    IF NOT EXISTS (
      SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
       WHERE TABLE_SCHEMA = DATABASE()
         AND TABLE_NAME = tbl
         AND COLUMN_NAME = 'updated_at'
    ) THEN
      SET @s = CONCAT('ALTER TABLE ', tbl,
                      ' ADD COLUMN updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP');
      PREPARE stmt FROM @s; EXECUTE stmt; DEALLOCATE PREPARE stmt;
    END IF;

  END LOOP;
  CLOSE cur;
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `migrate_enum_to_fk` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `migrate_enum_to_fk`(
    IN p_table        VARCHAR(64),
    IN p_old_col      VARCHAR(64),
    IN p_new_col      VARCHAR(64),
    IN p_domain_code  VARCHAR(50),
    IN p_default_code VARCHAR(100)
)
BEGIN
  DECLARE v_domain_id  INT;
  DECLARE v_fk_name    VARCHAR(128);

  
  SELECT id INTO v_domain_id
    FROM lookup_domain
   WHERE domain_code = p_domain_code;

  
  CALL add_column_if_missing(p_table, CONCAT('`',p_new_col,'`'),'INT NULL');

  
  SET @sql := CONCAT(
    'UPDATE ',p_table,' t ',
    'JOIN lookup_value v ',
    '  ON v.domain_id = ',v_domain_id,
    ' AND v.value_code COLLATE utf8mb4_unicode_ci = ',
    '     COALESCE(t.',p_old_col,',\'',p_default_code,'\') ',
    '       COLLATE utf8mb4_unicode_ci ',
    'SET t.',p_new_col,' = v.id ',
    'WHERE t.',p_new_col,' IS NULL');
  PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

  
  SET v_fk_name := CONCAT('fk_',p_table,'_',p_new_col);
  CALL add_fk_if_missing(
        p_table,
        v_fk_name,
        CONCAT(
          'ALTER TABLE ',p_table,
          ' ADD CONSTRAINT ',v_fk_name,
          ' FOREIGN KEY (',p_new_col,') REFERENCES lookup_value(id)')
  );
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `ref_touch` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `ref_touch`(IN p_domain_code VARCHAR(60), IN p_code VARCHAR(80), IN p_name VARCHAR(160))
BEGIN
  IF p_code IS NULL OR p_code = '' THEN
    SELECT NULL;
  ELSE
    INSERT INTO ref_domain(code,name) VALUES (p_domain_code,p_domain_code)
      ON DUPLICATE KEY UPDATE id=LAST_INSERT_ID(id);
    SET @dom_id := LAST_INSERT_ID();

    INSERT INTO ref_value(domain_id,code,name,is_active)
      VALUES (@dom_id,p_code,COALESCE(p_name,p_code),1)
      ON DUPLICATE KEY UPDATE id=LAST_INSERT_ID(id), name=VALUES(name), is_active=1;
  END IF;
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `sp_log_calibration_export` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = cp852 */ ;
/*!50003 SET character_set_results = cp852 */ ;
/*!50003 SET collation_connection  = cp852_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_log_calibration_export`(
    IN p_user_id INT,
    IN p_format ENUM('excel','pdf'),
    IN p_component_id INT,
    IN p_date_from DATE,
    IN p_date_to DATE,
    IN p_file_path VARCHAR(255)
)
BEGIN
    INSERT INTO calibration_export_log (
        user_id, export_format, filter_component_id, filter_date_from, filter_date_to, file_path
    ) VALUES (
        p_user_id, p_format, p_component_id, p_date_from, p_date_to, p_file_path
    );
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Current Database: `yasgmp`
--

USE `yasgmp`;

--
-- Final view structure for view `vw_calibrations_filter`
--

/*!50001 DROP VIEW IF EXISTS `vw_calibrations_filter`*/;
/*!50001 SET @saved_cs_client          = @@character_set_client */;
/*!50001 SET @saved_cs_results         = @@character_set_results */;
/*!50001 SET @saved_col_connection     = @@collation_connection */;
/*!50001 SET character_set_client      = cp852 */;
/*!50001 SET character_set_results     = cp852 */;
/*!50001 SET collation_connection      = cp852_general_ci */;
/*!50001 CREATE ALGORITHM=UNDEFINED */
/*!50013 DEFINER=`root`@`localhost` SQL SECURITY DEFINER */
/*!50001 VIEW `vw_calibrations_filter` AS select `c`.`id` AS `id`,`mc`.`name` AS `component_name`,`s`.`name` AS `supplier_name`,`c`.`calibration_date` AS `calibration_date`,`c`.`next_due` AS `next_due`,`c`.`result` AS `result`,`c`.`comment` AS `comment`,`c`.`digital_signature` AS `digital_signature` from ((`calibrations` `c` left join `machine_components` `mc` on((`c`.`component_id` = `mc`.`id`))) left join `suppliers` `s` on((`c`.`supplier_id` = `s`.`id`))) */;
/*!50001 SET character_set_client      = @saved_cs_client */;
/*!50001 SET character_set_results     = @saved_cs_results */;
/*!50001 SET collation_connection      = @saved_col_connection */;

--
-- Final view structure for view `vw_scheduled_jobs_due`
--

/*!50001 DROP VIEW IF EXISTS `vw_scheduled_jobs_due`*/;
/*!50001 SET @saved_cs_client          = @@character_set_client */;
/*!50001 SET @saved_cs_results         = @@character_set_results */;
/*!50001 SET @saved_col_connection     = @@collation_connection */;
/*!50001 SET character_set_client      = cp852 */;
/*!50001 SET character_set_results     = cp852 */;
/*!50001 SET collation_connection      = cp852_general_ci */;
/*!50001 CREATE ALGORITHM=UNDEFINED */
/*!50013 DEFINER=`root`@`localhost` SQL SECURITY DEFINER */
/*!50001 VIEW `vw_scheduled_jobs_due` AS select `scheduled_jobs`.`id` AS `id`,`scheduled_jobs`.`name` AS `name`,`scheduled_jobs`.`job_type` AS `job_type`,`scheduled_jobs`.`entity_type` AS `entity_type`,`scheduled_jobs`.`entity_id` AS `entity_id`,`scheduled_jobs`.`status` AS `status`,`scheduled_jobs`.`next_due` AS `next_due`,`scheduled_jobs`.`recurrence_pattern` AS `recurrence_pattern`,`scheduled_jobs`.`cron_expression` AS `cron_expression`,`scheduled_jobs`.`last_executed` AS `last_executed`,`scheduled_jobs`.`last_result` AS `last_result`,`scheduled_jobs`.`escalation_level` AS `escalation_level`,`scheduled_jobs`.`escalation_note` AS `escalation_note`,`scheduled_jobs`.`chain_job_id` AS `chain_job_id`,`scheduled_jobs`.`is_critical` AS `is_critical`,`scheduled_jobs`.`needs_acknowledgment` AS `needs_acknowledgment`,`scheduled_jobs`.`acknowledged_by` AS `acknowledged_by`,`scheduled_jobs`.`acknowledged_at` AS `acknowledged_at`,`scheduled_jobs`.`alert_on_failure` AS `alert_on_failure`,`scheduled_jobs`.`retries` AS `retries`,`scheduled_jobs`.`max_retries` AS `max_retries`,`scheduled_jobs`.`last_error` AS `last_error`,`scheduled_jobs`.`iot_device_id` AS `iot_device_id`,`scheduled_jobs`.`extra_params` AS `extra_params`,`scheduled_jobs`.`created_by` AS `created_by`,`scheduled_jobs`.`created_at` AS `created_at`,`scheduled_jobs`.`last_modified` AS `last_modified`,`scheduled_jobs`.`last_modified_by` AS `last_modified_by`,`scheduled_jobs`.`digital_signature` AS `digital_signature`,`scheduled_jobs`.`device_info` AS `device_info`,`scheduled_jobs`.`session_id` AS `session_id`,`scheduled_jobs`.`ip_address` AS `ip_address`,`scheduled_jobs`.`comment` AS `comment` from `scheduled_jobs` where ((`scheduled_jobs`.`status` in ('scheduled','in_progress','pending_ack','overdue')) and (`scheduled_jobs`.`next_due` <= now())) */;
/*!50001 SET character_set_client      = @saved_cs_client */;
/*!50001 SET character_set_results     = @saved_cs_results */;
/*!50001 SET collation_connection      = @saved_col_connection */;

--
-- Final view structure for view `vw_sensor_data_enriched`
--

/*!50001 DROP VIEW IF EXISTS `vw_sensor_data_enriched`*/;
/*!50001 SET @saved_cs_client          = @@character_set_client */;
/*!50001 SET @saved_cs_results         = @@character_set_results */;
/*!50001 SET @saved_col_connection     = @@collation_connection */;
/*!50001 SET character_set_client      = utf8mb4 */;
/*!50001 SET character_set_results     = utf8mb4 */;
/*!50001 SET collation_connection      = utf8mb4_0900_ai_ci */;
/*!50001 CREATE ALGORITHM=MERGE */
/*!50013 DEFINER=`root`@`localhost` SQL SECURITY INVOKER */
/*!50001 VIEW `vw_sensor_data_enriched` AS select `sdl`.`id` AS `id`,`sdl`.`component_id` AS `component_id`,`sdl`.`sensor_type_id` AS `sensor_type_id`,`sdl`.`unit_id` AS `unit_id`,`sdl`.`value` AS `value`,`sdl`.`timestamp` AS `timestamp`,`sdl`.`created_at` AS `created_at`,`lv`.`value_label` AS `sensor_type_name`,`mu`.`name` AS `unit_name` from ((`sensor_data_logs` `sdl` left join `lookup_value` `lv` on((`lv`.`id` = `sdl`.`sensor_type_id`))) left join `measurement_units` `mu` on((`mu`.`id` = `sdl`.`unit_id`))) */;
/*!50001 SET character_set_client      = @saved_cs_client */;
/*!50001 SET character_set_results     = @saved_cs_results */;
/*!50001 SET collation_connection      = @saved_col_connection */;

--
-- Final view structure for view `vw_stock_current`
--

/*!50001 DROP VIEW IF EXISTS `vw_stock_current`*/;
/*!50001 SET @saved_cs_client          = @@character_set_client */;
/*!50001 SET @saved_cs_results         = @@character_set_results */;
/*!50001 SET @saved_col_connection     = @@collation_connection */;
/*!50001 SET character_set_client      = utf8mb4 */;
/*!50001 SET character_set_results     = utf8mb4 */;
/*!50001 SET collation_connection      = utf8mb4_0900_ai_ci */;
/*!50001 CREATE ALGORITHM=MERGE */
/*!50013 DEFINER=`root`@`localhost` SQL SECURITY INVOKER */
/*!50001 VIEW `vw_stock_current` AS select `p`.`id` AS `part_id`,`p`.`code` AS `part_code`,`p`.`name` AS `part_name`,`w`.`id` AS `warehouse_id`,coalesce(`w`.`name`,`l`.`name`) AS `warehouse_name`,`sl`.`quantity` AS `quantity`,`sl`.`min_threshold` AS `min_threshold`,`sl`.`max_threshold` AS `max_threshold` from (((`stock_levels` `sl` join `parts` `p` on((`p`.`id` = `sl`.`part_id`))) left join `warehouses` `w` on((`w`.`id` = `sl`.`warehouse_id`))) left join `locations` `l` on((`l`.`id` = `w`.`location_id`))) */;
/*!50001 SET character_set_client      = @saved_cs_client */;
/*!50001 SET character_set_results     = @saved_cs_results */;
/*!50001 SET collation_connection      = @saved_col_connection */;

--
-- Final view structure for view `vw_work_orders_enriched`
--

/*!50001 DROP VIEW IF EXISTS `vw_work_orders_enriched`*/;
/*!50001 SET @saved_cs_client          = @@character_set_client */;
/*!50001 SET @saved_cs_results         = @@character_set_results */;
/*!50001 SET @saved_col_connection     = @@collation_connection */;
/*!50001 SET character_set_client      = utf8mb4 */;
/*!50001 SET character_set_results     = utf8mb4 */;
/*!50001 SET collation_connection      = utf8mb4_0900_ai_ci */;
/*!50001 CREATE ALGORITHM=MERGE */
/*!50013 DEFINER=`root`@`localhost` SQL SECURITY INVOKER */
/*!50001 VIEW `vw_work_orders_enriched` AS select `wo`.`id` AS `id`,`wo`.`machine_id` AS `machine_id`,`wo`.`component_id` AS `component_id`,`wo`.`type` AS `type`,`wo`.`created_by` AS `created_by`,`wo`.`assigned_to` AS `assigned_to`,`wo`.`date_open` AS `date_open`,`wo`.`date_close` AS `date_close`,`wo`.`description` AS `description`,`wo`.`result` AS `result`,`wo`.`status` AS `status`,`wo`.`digital_signature` AS `digital_signature`,`wo`.`priority` AS `priority`,`wo`.`related_incident` AS `related_incident`,`wo`.`created_at` AS `created_at`,`wo`.`updated_at` AS `updated_at`,`wo`.`status_id` AS `status_id`,`wo`.`type_id` AS `type_id`,`wo`.`priority_id` AS `priority_id`,`wo`.`tenant_id` AS `tenant_id`,`vs`.`value_label` AS `status_name`,`vt`.`value_label` AS `type_name`,`vp`.`value_label` AS `priority_name`,`m`.`name` AS `machine_name`,`mc`.`name` AS `component_name` from (((((`work_orders` `wo` left join `lookup_value` `vs` on((`vs`.`id` = `wo`.`status_id`))) left join `lookup_value` `vt` on((`vt`.`id` = `wo`.`type_id`))) left join `lookup_value` `vp` on((`vp`.`id` = `wo`.`priority_id`))) left join `machines` `m` on((`m`.`id` = `wo`.`machine_id`))) left join `machine_components` `mc` on((`mc`.`id` = `wo`.`component_id`))) */;
/*!50001 SET character_set_client      = @saved_cs_client */;
/*!50001 SET character_set_results     = @saved_cs_results */;
/*!50001 SET collation_connection      = @saved_col_connection */;

--
-- Final view structure for view `vw_work_orders_user`
--

/*!50001 DROP VIEW IF EXISTS `vw_work_orders_user`*/;
/*!50001 SET @saved_cs_client          = @@character_set_client */;
/*!50001 SET @saved_cs_results         = @@character_set_results */;
/*!50001 SET @saved_col_connection     = @@collation_connection */;
/*!50001 SET character_set_client      = cp852 */;
/*!50001 SET character_set_results     = cp852 */;
/*!50001 SET collation_connection      = cp852_general_ci */;
/*!50001 CREATE ALGORITHM=MERGE */
/*!50013 DEFINER=`root`@`localhost` SQL SECURITY DEFINER */
/*!50001 VIEW `vw_work_orders_user` AS select `wo`.`id` AS `id`,`wo`.`machine_id` AS `machine_id`,`wo`.`component_id` AS `component_id`,`wo`.`type` AS `type`,`wo`.`created_by` AS `created_by`,`wo`.`assigned_to` AS `assigned_to`,`wo`.`date_open` AS `date_open`,`wo`.`date_close` AS `date_close`,`wo`.`description` AS `description`,`wo`.`result` AS `result`,`wo`.`status` AS `status`,`wo`.`digital_signature` AS `digital_signature`,`wo`.`priority` AS `priority`,`wo`.`related_incident` AS `related_incident`,`wo`.`created_at` AS `created_at`,`wo`.`updated_at` AS `updated_at`,`wo`.`status_id` AS `status_id`,`wo`.`type_id` AS `type_id`,`wo`.`priority_id` AS `priority_id`,`wo`.`tenant_id` AS `tenant_id` from (`work_orders` `wo` join `users` `u` on((`u`.`id` = `CURRENT_USER_ID`()))) where ((`wo`.`tenant_id` = `u`.`tenant_id`) or (`u`.`tenant_id` is null)) */;
/*!50001 SET character_set_client      = @saved_cs_client */;
/*!50001 SET character_set_results     = @saved_cs_results */;
/*!50001 SET collation_connection      = @saved_col_connection */;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2025-08-29  8:29:53
