-- Schema snapshot for yasgmp (tables) - 2025-09-05T07:34:23.2952454Z

-- admin_activity_log
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
  `user?` varchar(255) DEFAULT NULL,
  `device_name` varchar(128) DEFAULT NULL,
  `session_id` varchar(64) DEFAULT NULL,
  `digital_signature` varchar(256) DEFAULT NULL,
  `change_version` int DEFAULT NULL,
  `is_deleted` tinyint(1) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_adminact_user` (`admin_id`),
  CONSTRAINT `fk_adminact_user` FOREIGN KEY (`admin_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- api_audit_log
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

-- api_keys
CREATE TABLE `api_keys` (
  `id` int NOT NULL AUTO_INCREMENT,
  `key_value` varchar(255) NOT NULL,
  `description` varchar(255) DEFAULT NULL,
  `owner_id` int DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `is_active` tinyint(1) DEFAULT '1',
  `last_used_at` datetime DEFAULT NULL,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `usage_logs` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `key_value` (`key_value`),
  KEY `fk_apikey_owner` (`owner_id`),
  CONSTRAINT `fk_apikey_owner` FOREIGN KEY (`owner_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- api_usage_log
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
  `api_key` varchar(255) DEFAULT NULL,
  `user` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_api_usage_key` (`api_key_id`),
  KEY `fk_api_usage_user` (`user_id`),
  CONSTRAINT `fk_api_usage_key` FOREIGN KEY (`api_key_id`) REFERENCES `api_keys` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_api_usage_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- attachments
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
  `name` varchar(255) DEFAULT NULL,
  `file_size` bigint DEFAULT NULL,
  `entity_type` varchar(255) DEFAULT NULL,
  `entity_id` int DEFAULT NULL,
  `file_content` longblob,
  `ocr_text` varchar(255) DEFAULT NULL,
  `file_hash` varchar(255) DEFAULT NULL,
  `uploaded_by_id` int DEFAULT NULL,
  `user?` varchar(255) DEFAULT NULL,
  `is_approved` tinyint(1) DEFAULT NULL,
  `approved_by_id` int DEFAULT NULL,
  `approved_at` datetime DEFAULT NULL,
  `expiry_date` datetime DEFAULT NULL,
  `digital_signature` varchar(255) DEFAULT NULL,
  `ip_address` varchar(255) DEFAULT NULL,
  `device_info` varchar(255) DEFAULT NULL,
  `session_id` varchar(255) DEFAULT NULL,
  `status` varchar(255) DEFAULT NULL,
  `notes` varchar(255) DEFAULT NULL,
  `change_version` int DEFAULT NULL,
  `is_deleted` tinyint(1) DEFAULT NULL,
  `ai_score` decimal(10,2) DEFAULT NULL,
  `chain_id` varchar(255) DEFAULT NULL,
  `version_uid` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_att_user` (`uploaded_by`),
  CONSTRAINT `fk_att_user` FOREIGN KEY (`uploaded_by`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- buildings
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

-- calibration_audit_log
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
  `calibration` varchar(255) DEFAULT NULL,
  `user?` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_cal_audit_cal` (`calibration_id`),
  KEY `fk_cal_audit_user` (`user_id`),
  CONSTRAINT `fk_cal_audit_cal` FOREIGN KEY (`calibration_id`) REFERENCES `calibrations` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_cal_audit_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- calibration_export_log
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
  `user?` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_cel_user` (`user_id`),
  CONSTRAINT `fk_cel_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- calibration_sensors
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

-- calibrations
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
  `machine_component` varchar(255) DEFAULT NULL,
  `supplier?` varchar(255) DEFAULT NULL,
  `user?` varchar(255) DEFAULT NULL,
  `approved` tinyint(1) DEFAULT NULL,
  `approved_at` datetime DEFAULT NULL,
  `approved_by_id` int DEFAULT NULL,
  `previous_calibration_id` int DEFAULT NULL,
  `calibration?` varchar(255) DEFAULT NULL,
  `next_calibration_id` int DEFAULT NULL,
  `change_version` int DEFAULT NULL,
  `is_deleted` tinyint(1) DEFAULT NULL,
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

-- capa_action_log
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
  `capa_case` varchar(255) DEFAULT NULL,
  `performed_by_id` int DEFAULT NULL,
  `user?` varchar(255) DEFAULT NULL,
  `digital_signature` varchar(128) DEFAULT NULL,
  `source_ip` varchar(45) DEFAULT NULL,
  `change_version` int DEFAULT NULL,
  `is_deleted` tinyint(1) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_cal_case` (`capa_case_id`),
  KEY `fk_cal_user` (`performed_by`),
  CONSTRAINT `fk_cal_case` FOREIGN KEY (`capa_case_id`) REFERENCES `capa_cases` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_cal_user` FOREIGN KEY (`performed_by`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- capa_actions
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

-- capa_cases
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
  `title` varchar(255) DEFAULT NULL,
  `description` varchar(255) DEFAULT NULL,
  `machine_component` varchar(255) DEFAULT NULL,
  `user` varchar(255) DEFAULT NULL,
  `priority` varchar(255) DEFAULT NULL,
  `root_cause` varchar(255) DEFAULT NULL,
  `corrective_action` varchar(255) DEFAULT NULL,
  `preventive_action` varchar(255) DEFAULT NULL,
  `digital_signature` varchar(255) DEFAULT NULL,
  `last_modified` datetime DEFAULT NULL,
  `last_modified_by_id` int DEFAULT NULL,
  `approved` tinyint(1) DEFAULT NULL,
  `approved_at` datetime DEFAULT NULL,
  `approved_by_id` int DEFAULT NULL,
  `source_ip` varchar(255) DEFAULT NULL,
  `root_cause_reference` varchar(255) DEFAULT NULL,
  `linked_findings` varchar(255) DEFAULT NULL,
  `notes` varchar(255) DEFAULT NULL,
  `comments` varchar(255) DEFAULT NULL,
  `change_version` int DEFAULT NULL,
  `is_deleted` tinyint(1) DEFAULT NULL,
  `assigned_to_id` int DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_capa_component` (`component_id`),
  KEY `idx_capa_status_id` (`status_id`),
  CONSTRAINT `fk_capa_component` FOREIGN KEY (`component_id`) REFERENCES `machine_components` (`id`),
  CONSTRAINT `fk_capa_status` FOREIGN KEY (`status_id`) REFERENCES `ref_value` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- capa_status_history
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
  `capa_case` varchar(255) DEFAULT NULL,
  `source_ip` varchar(45) DEFAULT NULL,
  `digital_signature` varchar(128) DEFAULT NULL,
  `change_version` int DEFAULT NULL,
  `is_deleted` tinyint(1) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_csh_case` (`capa_case_id`),
  KEY `fk_csh_user` (`changed_by`),
  CONSTRAINT `fk_csh_case` FOREIGN KEY (`capa_case_id`) REFERENCES `capa_cases` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_csh_user` FOREIGN KEY (`changed_by`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- change_controls
CREATE TABLE `change_controls` (
  `id` int NOT NULL AUTO_INCREMENT,
  `description` varchar(255) DEFAULT NULL,
  `title` varchar(255) DEFAULT NULL,
  `date_requested` varchar(255) DEFAULT NULL,
  `code` varchar(255) DEFAULT NULL,
  `status` varchar(255) DEFAULT NULL,
  `requested_by_id` int DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- checklist_items
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

-- checklist_templates
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

-- comments
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

-- component_devices
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

-- component_models
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

-- component_parts
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

-- component_qualifications
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
  `certificate_number` varchar(255) DEFAULT NULL,
  `supplier_id` int DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_cq_component` (`component_id`),
  CONSTRAINT `fk_cq_component` FOREIGN KEY (`component_id`) REFERENCES `machine_components` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- component_types
CREATE TABLE `component_types` (
  `id` int NOT NULL AUTO_INCREMENT,
  `code` varchar(50) DEFAULT NULL,
  `name` varchar(120) NOT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `code` (`code`)
) ENGINE=InnoDB AUTO_INCREMENT=41 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- config_change_log
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

-- contractor_intervention_audit
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
  `description` varchar(255) DEFAULT NULL,
  `timestamp` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_cia_intervention` (`intervention_id`),
  KEY `fk_cia_user` (`user_id`),
  CONSTRAINT `fk_cia_intervention` FOREIGN KEY (`intervention_id`) REFERENCES `contractor_interventions` (`id`),
  CONSTRAINT `fk_cia_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- contractor_interventions
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
  `user` varchar(255) DEFAULT NULL,
  `component` varchar(255) DEFAULT NULL,
  `digital_signature` varchar(255) DEFAULT NULL,
  `start_date` datetime DEFAULT NULL,
  `end_date` datetime DEFAULT NULL,
  `notes` varchar(255) DEFAULT NULL,
  `last_modified` datetime DEFAULT NULL,
  `last_modified_by_id` int DEFAULT NULL,
  `source_ip` varchar(255) DEFAULT NULL,
  `comments` varchar(255) DEFAULT NULL,
  `status` varchar(255) DEFAULT NULL,
  `change_version` int DEFAULT NULL,
  `is_deleted` tinyint(1) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_ci_contractor` (`contractor_id`),
  KEY `fk_ci_component` (`component_id`),
  CONSTRAINT `fk_ci_component` FOREIGN KEY (`component_id`) REFERENCES `machine_components` (`id`),
  CONSTRAINT `fk_ci_contractor` FOREIGN KEY (`contractor_id`) REFERENCES `external_contractors` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- dashboards
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

-- delegated_permissions
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
  `permission?` varchar(255) DEFAULT NULL,
  `user?` varchar(255) DEFAULT NULL,
  `start_at` datetime DEFAULT NULL,
  `end_at` datetime DEFAULT NULL,
  `is_active` tinyint(1) DEFAULT NULL,
  `is_revoked` tinyint(1) DEFAULT NULL,
  `approved_by` int DEFAULT NULL,
  `note` varchar(512) DEFAULT NULL,
  `change_version` int DEFAULT NULL,
  `digital_signature` varchar(128) DEFAULT NULL,
  `session_id` varchar(64) DEFAULT NULL,
  `source_ip` varchar(64) DEFAULT NULL,
  `code` varchar(255) DEFAULT NULL,
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

-- delete_log
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

-- departments
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

-- deviation_audit
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

-- deviations
CREATE TABLE `deviations` (
  `id` int NOT NULL AUTO_INCREMENT,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `code` varchar(40) DEFAULT NULL,
  `title` varchar(200) DEFAULT NULL,
  `description` varchar(4000) DEFAULT NULL,
  `reported_at` datetime DEFAULT NULL,
  `reported_by_id` int DEFAULT NULL,
  `user` varchar(255) DEFAULT NULL,
  `severity` varchar(16) DEFAULT NULL,
  `is_critical` tinyint(1) DEFAULT NULL,
  `status` varchar(255) DEFAULT NULL,
  `assigned_investigator_id` int DEFAULT NULL,
  `user?` varchar(255) DEFAULT NULL,
  `assigned_investigator_name` varchar(100) DEFAULT NULL,
  `investigation_started_at` datetime DEFAULT NULL,
  `root_cause` varchar(800) DEFAULT NULL,
  `corrective_actions` varchar(255) DEFAULT NULL,
  `linked_capa_id` int DEFAULT NULL,
  `capa_case?` varchar(255) DEFAULT NULL,
  `closure_comment` varchar(2000) DEFAULT NULL,
  `closed_at` datetime DEFAULT NULL,
  `attachment_ids` varchar(255) DEFAULT NULL,
  `attachments` varchar(255) DEFAULT NULL,
  `digital_signature` varchar(128) DEFAULT NULL,
  `risk_score` int DEFAULT NULL,
  `anomaly_score` decimal(10,2) DEFAULT NULL,
  `last_modified` datetime DEFAULT NULL,
  `last_modified_by_id` int DEFAULT NULL,
  `source_ip` varchar(64) DEFAULT NULL,
  `audit_note` varchar(1000) DEFAULT NULL,
  `audit_trail` varchar(255) DEFAULT NULL,
  `string>` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- digital_signatures
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
  `public_key` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_dsig_user` (`user_id`),
  CONSTRAINT `fk_dsig_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- document_versions
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
  `documentcontrol_id` int DEFAULT NULL,
  `document` varchar(255) DEFAULT NULL,
  `revision` varchar(255) DEFAULT NULL,
  `created_by_id` int DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_docv_user` (`created_by`),
  CONSTRAINT `fk_docv_user` FOREIGN KEY (`created_by`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- documentcontrol
CREATE TABLE `documentcontrol` (
  `id` int NOT NULL AUTO_INCREMENT,
  `device_info` varchar(255) DEFAULT NULL,
  `file_path` varchar(255) DEFAULT NULL,
  `description` varchar(255) DEFAULT NULL,
  `title` varchar(255) DEFAULT NULL,
  `linked_change_controls` varchar(255) DEFAULT NULL,
  `revision` varchar(255) DEFAULT NULL,
  `code` varchar(255) DEFAULT NULL,
  `status` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- entity_audit_log
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
) ENGINE=InnoDB AUTO_INCREMENT=12 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- entity_tags
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

-- export_audit_log
CREATE TABLE `export_audit_log` (
  `id` int NOT NULL AUTO_INCREMENT,
  `device_info` varchar(255) DEFAULT NULL,
  `source_ip` varchar(255) DEFAULT NULL,
  `filter_criteria` varchar(255) DEFAULT NULL,
  `file_path` varchar(255) DEFAULT NULL,
  `timestamp` varchar(255) DEFAULT NULL,
  `export_type` varchar(255) DEFAULT NULL,
  `user_id` int DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- export_print_log
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

-- external_contractors
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
  `company_name` varchar(255) DEFAULT NULL,
  `registration_number` varchar(255) DEFAULT NULL,
  `type` varchar(255) DEFAULT NULL,
  `contact_person` varchar(255) DEFAULT NULL,
  `address` varchar(255) DEFAULT NULL,
  `certificates` varchar(255) DEFAULT NULL,
  `is_blacklisted` tinyint(1) DEFAULT NULL,
  `blacklist_reason` varchar(255) DEFAULT NULL,
  `risk_score` decimal(10,2) DEFAULT NULL,
  `supplier_id` int DEFAULT NULL,
  `supplier?` varchar(255) DEFAULT NULL,
  `interventions` varchar(255) DEFAULT NULL,
  `attachments` varchar(255) DEFAULT NULL,
  `digital_signature` varchar(255) DEFAULT NULL,
  `audit_logs` varchar(255) DEFAULT NULL,
  `note` varchar(255) DEFAULT NULL,
  `code` varchar(255) DEFAULT NULL,
  `comment` varchar(255) DEFAULT NULL,
  `status` varchar(255) DEFAULT NULL,
  `cooperation_start` varchar(255) DEFAULT NULL,
  `cooperation_end` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- failure_modes
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

-- forensic_user_change_log
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
  `changed_by_user` varchar(255) DEFAULT NULL,
  `target_user` varchar(255) DEFAULT NULL,
  `digital_signature` varchar(255) DEFAULT NULL,
  `change_version` int DEFAULT NULL,
  `is_deleted` tinyint(1) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_fucl_changed_by` (`changed_by`),
  KEY `fk_fucl_target_user` (`target_user_id`),
  CONSTRAINT `fk_fucl_changed_by` FOREIGN KEY (`changed_by`) REFERENCES `users` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_fucl_target_user` FOREIGN KEY (`target_user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- incident_audit
CREATE TABLE `incident_audit` (
  `id` int NOT NULL AUTO_INCREMENT,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- incident_log
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
  `reported_by_id` int DEFAULT NULL,
  `resolved_by_id` int DEFAULT NULL,
  `digital_signature` varchar(255) DEFAULT NULL,
  `root_cause` varchar(255) DEFAULT NULL,
  `capa_case_id` int DEFAULT NULL,
  `capa_case` varchar(255) DEFAULT NULL,
  `attachments` varchar(255) DEFAULT NULL,
  `last_modified` datetime DEFAULT NULL,
  `last_modified_by_id` int DEFAULT NULL,
  `last_modified_by` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_inc_reported_by` (`reported_by`),
  KEY `fk_inc_resolved_by` (`resolved_by`),
  KEY `idx_inc_sev_id` (`severity_id`),
  CONSTRAINT `fk_inc_reported_by` FOREIGN KEY (`reported_by`) REFERENCES `users` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_inc_resolved_by` FOREIGN KEY (`resolved_by`) REFERENCES `users` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_inc_sev` FOREIGN KEY (`severity_id`) REFERENCES `ref_value` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- inspections
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

-- integration_log
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

-- inventory_transactions
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
  `performed_by_id` int DEFAULT NULL,
  `spare_part?` varchar(255) DEFAULT NULL,
  `warehouse?` varchar(255) DEFAULT NULL,
  `user?` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_it_part` (`part_id`),
  KEY `fk_it_warehouse` (`warehouse_id`),
  KEY `fk_it_user` (`performed_by`),
  CONSTRAINT `fk_it_part` FOREIGN KEY (`part_id`) REFERENCES `parts` (`id`),
  CONSTRAINT `fk_it_user` FOREIGN KEY (`performed_by`) REFERENCES `users` (`id`),
  CONSTRAINT `fk_it_warehouse` FOREIGN KEY (`warehouse_id`) REFERENCES `warehouses` (`id`),
  CONSTRAINT `chk_it_qty_nonneg` CHECK (((`quantity` is null) or (`quantity` >= 0)))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- iot_anomaly_log
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

-- iot_devices
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

-- iot_event_audit
CREATE TABLE `iot_event_audit` (
  `id` int NOT NULL AUTO_INCREMENT,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- iot_gateways
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

-- iot_sensor_data
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

-- irregularities_log
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

-- job_titles
CREATE TABLE `job_titles` (
  `id` int NOT NULL AUTO_INCREMENT,
  `title` varchar(100) DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `title` (`title`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- locations
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

-- lookup_domain
CREATE TABLE `lookup_domain` (
  `id` int NOT NULL AUTO_INCREMENT,
  `domain_code` varchar(50) DEFAULT NULL,
  `description` varchar(255) DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `domain_code` (`domain_code`)
) ENGINE=InnoDB AUTO_INCREMENT=18 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- lookup_value
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

-- machine_components
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
  `last_modified_by_id` int DEFAULT NULL,
  `machine?` varchar(255) DEFAULT NULL,
  `description` varchar(255) DEFAULT NULL,
  `model` varchar(255) DEFAULT NULL,
  `purchase_date` datetime DEFAULT NULL,
  `warranty_until` datetime DEFAULT NULL,
  `warranty_expiry` datetime DEFAULT NULL,
  `serial_number` varchar(255) DEFAULT NULL,
  `supplier` varchar(255) DEFAULT NULL,
  `rfid_tag` varchar(255) DEFAULT NULL,
  `io_tdevice_id` varchar(255) DEFAULT NULL,
  `lifecycle_phase` varchar(255) DEFAULT NULL,
  `is_critical` tinyint(1) DEFAULT NULL,
  `note` varchar(255) DEFAULT NULL,
  `digital_signature` varchar(255) DEFAULT NULL,
  `notes` varchar(255) DEFAULT NULL,
  `last_modified` datetime DEFAULT NULL,
  `user?` varchar(255) DEFAULT NULL,
  `source_ip` varchar(255) DEFAULT NULL,
  `documents` varchar(255) DEFAULT NULL,
  `icollection<photo>` varchar(255) DEFAULT NULL,
  `icollection<calibration>` varchar(255) DEFAULT NULL,
  `icollection<capa_case>` varchar(255) DEFAULT NULL,
  `icollection<work_order>` varchar(255) DEFAULT NULL,
  `change_version` int DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `code` (`code`),
  KEY `fk_mc_status` (`status_id`),
  KEY `fk_comp_component_type` (`component_type_id`),
  KEY `ix_machine_components_machine_id` (`machine_id`),
  KEY `ix_machine_components_code` (`code`),
  CONSTRAINT `fk_comp_component_type` FOREIGN KEY (`component_type_id`) REFERENCES `component_types` (`id`),
  CONSTRAINT `fk_mc_machine` FOREIGN KEY (`machine_id`) REFERENCES `machines` (`id`),
  CONSTRAINT `fk_mc_status` FOREIGN KEY (`status_id`) REFERENCES `ref_value` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_mc_type` FOREIGN KEY (`component_type_id`) REFERENCES `component_types` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- machine_lifecycle_event
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
  `machine` varchar(255) DEFAULT NULL,
  `performed_by_id` int DEFAULT NULL,
  `digital_signature` varchar(128) DEFAULT NULL,
  `source_ip` varchar(45) DEFAULT NULL,
  `last_modified` datetime DEFAULT NULL,
  `last_modified_by_id` int DEFAULT NULL,
  `last_modified_by` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_mle_machine` (`machine_id`),
  KEY `fk_mle_user` (`performed_by`),
  KEY `fk_mle_type` (`event_type_id`),
  CONSTRAINT `fk_mle_machine` FOREIGN KEY (`machine_id`) REFERENCES `machines` (`id`),
  CONSTRAINT `fk_mle_type` FOREIGN KEY (`event_type_id`) REFERENCES `ref_value` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_mle_user` FOREIGN KEY (`performed_by`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- machine_models
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

-- machine_types
CREATE TABLE `machine_types` (
  `id` int NOT NULL AUTO_INCREMENT,
  `code` varchar(50) DEFAULT NULL,
  `name` varchar(120) NOT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `code` (`code`)
) ENGINE=InnoDB AUTO_INCREMENT=41 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- machines
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
  `qr_code` varchar(128) DEFAULT NULL,
  `iot_device_id` varchar(64) DEFAULT NULL,
  `cloud_device_guid` varchar(64) DEFAULT NULL,
  `is_critical` tinyint(1) DEFAULT '0',
  `lifecycle_phase` varchar(30) DEFAULT NULL,
  `note` varchar(200) DEFAULT NULL,
  `status` varchar(30) NOT NULL DEFAULT 'active',
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
  `purchase_date` datetime DEFAULT NULL,
  `warranty_expiry` datetime DEFAULT NULL,
  `notes` varchar(255) DEFAULT NULL,
  `icollection<machine_component>` varchar(255) DEFAULT NULL,
  `icollection<machine_lifecycle_event>` varchar(255) DEFAULT NULL,
  `icollection<capa_case>` varchar(255) DEFAULT NULL,
  `icollection<quality_event>` varchar(255) DEFAULT NULL,
  `icollection<validation>` varchar(255) DEFAULT NULL,
  `icollection<inspection>` varchar(255) DEFAULT NULL,
  `icollection<work_order>` varchar(255) DEFAULT NULL,
  `icollection<photo>` varchar(255) DEFAULT NULL,
  `icollection<attachment>` varchar(255) DEFAULT NULL,
  `user?` varchar(255) DEFAULT NULL,
  `icollection<calibration>` varchar(255) DEFAULT NULL,
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

-- manufacturers
CREATE TABLE `manufacturers` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(150) NOT NULL,
  `website` varchar(255) DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `name` (`name`)
) ENGINE=InnoDB AUTO_INCREMENT=37 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- measurement_units
CREATE TABLE `measurement_units` (
  `id` int NOT NULL AUTO_INCREMENT,
  `code` varchar(10) DEFAULT NULL,
  `name` varchar(50) DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `code` (`code`)
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- mobile_device_log
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

-- notification_queue
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

-- notification_templates
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

-- part_bom
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

-- part_supplier_prices
CREATE TABLE `part_supplier_prices` (
  `id` int NOT NULL AUTO_INCREMENT,
  `part_id` int DEFAULT NULL,
  `supplier_id` int DEFAULT NULL,
  `unit_price` decimal(10,2) DEFAULT NULL,
  `currency` varchar(10) DEFAULT NULL,
  `valid_until` date DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `part` varchar(255) DEFAULT NULL,
  `supplier` varchar(255) DEFAULT NULL,
  `supplier_name` varchar(255) DEFAULT NULL,
  `vat_percent` decimal(10,2) DEFAULT NULL,
  `price_with_vat` decimal(10,2) DEFAULT NULL,
  `region` varchar(255) DEFAULT NULL,
  `discount_percent` decimal(10,2) DEFAULT NULL,
  `surcharge` decimal(10,2) DEFAULT NULL,
  `min_order_quantity` int DEFAULT NULL,
  `lead_time_days` int DEFAULT NULL,
  `valid_from` datetime DEFAULT NULL,
  `is_blocked` tinyint(1) DEFAULT NULL,
  `block_reason` varchar(255) DEFAULT NULL,
  `contract_document` varchar(255) DEFAULT NULL,
  `last_modified` datetime DEFAULT NULL,
  `last_modified_by_id` int DEFAULT NULL,
  `last_modified_by` varchar(255) DEFAULT NULL,
  `digital_signature` varchar(255) DEFAULT NULL,
  `source_ip` varchar(255) DEFAULT NULL,
  `anomaly_score` decimal(10,2) DEFAULT NULL,
  `note` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_psp_part` (`part_id`),
  KEY `fk_psp_supplier` (`supplier_id`),
  CONSTRAINT `fk_psp_part` FOREIGN KEY (`part_id`) REFERENCES `parts` (`id`),
  CONSTRAINT `fk_psp_supplier` FOREIGN KEY (`supplier_id`) REFERENCES `suppliers` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- parts
CREATE TABLE `parts` (
  `id` int NOT NULL AUTO_INCREMENT,
  `code` varchar(50) DEFAULT NULL,
  `name` varchar(100) DEFAULT NULL,
  `default_supplier_id` int DEFAULT NULL,
  `description` text,
  `status` enum('active','obsolete','reorder') DEFAULT 'active',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `category` varchar(255) DEFAULT NULL,
  `barcode` varchar(255) DEFAULT NULL,
  `rfid` varchar(255) DEFAULT NULL,
  `serial_or_lot` varchar(255) DEFAULT NULL,
  `default_supplier` varchar(255) DEFAULT NULL,
  `supplier_prices` varchar(255) DEFAULT NULL,
  `price` decimal(10,2) DEFAULT NULL,
  `stock` int DEFAULT NULL,
  `min_stock_alert` int DEFAULT NULL,
  `warehouse_stocks` varchar(255) DEFAULT NULL,
  `stock_history` varchar(255) DEFAULT NULL,
  `location` varchar(255) DEFAULT NULL,
  `image` varchar(255) DEFAULT NULL,
  `images` varchar(255) DEFAULT NULL,
  `documents` varchar(255) DEFAULT NULL,
  `warranty_until` datetime DEFAULT NULL,
  `expiry_date` datetime DEFAULT NULL,
  `blocked` tinyint(1) DEFAULT NULL,
  `regulatory_certificates` varchar(255) DEFAULT NULL,
  `digital_signature` varchar(255) DEFAULT NULL,
  `last_modified` datetime DEFAULT NULL,
  `last_modified_by_id` int DEFAULT NULL,
  `last_modified_by` varchar(255) DEFAULT NULL,
  `source_ip` varchar(255) DEFAULT NULL,
  `change_logs` varchar(255) DEFAULT NULL,
  `work_order_parts` varchar(255) DEFAULT NULL,
  `warehouses` varchar(255) DEFAULT NULL,
  `note` varchar(255) DEFAULT NULL,
  `anomaly_score` decimal(10,2) DEFAULT NULL,
  `supplier` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `code` (`code`),
  KEY `fk_p_supplier` (`default_supplier_id`),
  CONSTRAINT `fk_p_supplier` FOREIGN KEY (`default_supplier_id`) REFERENCES `suppliers` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- permission_change_log
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

-- permission_requests
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

-- permissions
CREATE TABLE `permissions` (
  `id` int NOT NULL AUTO_INCREMENT,
  `code` varchar(100) NOT NULL,
  `description` varchar(255) DEFAULT NULL,
  `module` varchar(100) DEFAULT NULL,
  `critical` tinyint(1) DEFAULT '0',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `permission_type` varchar(255) DEFAULT NULL,
  `name` varchar(120) DEFAULT NULL,
  `group` varchar(80) DEFAULT NULL,
  `parent_id` int DEFAULT NULL,
  `permission?` varchar(255) DEFAULT NULL,
  `icollection<permission>` varchar(255) DEFAULT NULL,
  `compliance_tags` varchar(120) DEFAULT NULL,
  `created_by` int DEFAULT NULL,
  `user?` varchar(255) DEFAULT NULL,
  `last_modified` datetime DEFAULT NULL,
  `last_modified_by_id` int DEFAULT NULL,
  `digital_signature` varchar(128) DEFAULT NULL,
  `change_version` int DEFAULT NULL,
  `is_deleted` tinyint(1) DEFAULT NULL,
  `note` varchar(512) DEFAULT NULL,
  `icollection<role>` varchar(255) DEFAULT NULL,
  `icollection<user>` varchar(255) DEFAULT NULL,
  `icollection<role_permission>` varchar(255) DEFAULT NULL,
  `icollection<user_permission>` varchar(255) DEFAULT NULL,
  `icollection<delegated_permission>` varchar(255) DEFAULT NULL,
  `string` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `code` (`code`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- photos
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

-- ppm_plans
CREATE TABLE `ppm_plans` (
  `id` int NOT NULL AUTO_INCREMENT,
  `machine_id` int NOT NULL,
  `title` varchar(200) NOT NULL,
  `plan_json` json DEFAULT NULL,
  `effective_from` date DEFAULT NULL,
  `effective_to` date DEFAULT NULL,
  `status` varchar(30) NOT NULL DEFAULT 'active',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `last_modified` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_ppm_plans_machine` (`machine_id`),
  CONSTRAINT `fk_ppm_plans_machine` FOREIGN KEY (`machine_id`) REFERENCES `machines` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- preventive_maintenance_plans
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
  `machine` varchar(255) DEFAULT NULL,
  `component` varchar(255) DEFAULT NULL,
  `responsible_user` varchar(255) DEFAULT NULL,
  `execution_history` varchar(255) DEFAULT NULL,
  `risk_score` decimal(10,2) DEFAULT NULL,
  `ai_recommendation` varchar(2048) DEFAULT NULL,
  `last_modified` datetime DEFAULT NULL,
  `last_modified_by_id` int DEFAULT NULL,
  `last_modified_by` varchar(255) DEFAULT NULL,
  `source_ip` varchar(45) DEFAULT NULL,
  `session_id` varchar(80) DEFAULT NULL,
  `geo_location` varchar(100) DEFAULT NULL,
  `attachments` varchar(255) DEFAULT NULL,
  `version` int DEFAULT NULL,
  `previous_version_id` int DEFAULT NULL,
  `previous_version` varchar(255) DEFAULT NULL,
  `is_active_version` tinyint(1) DEFAULT NULL,
  `linked_work_orders` varchar(255) DEFAULT NULL,
  `is_automated` tinyint(1) DEFAULT NULL,
  `requires_notification` tinyint(1) DEFAULT NULL,
  `anomaly_score` decimal(10,2) DEFAULT NULL,
  `note` varchar(512) DEFAULT NULL,
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

-- quality_events
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
  `related_machine_id` int DEFAULT NULL,
  `related_component_id` int DEFAULT NULL,
  `digital_signature` varchar(128) DEFAULT NULL,
  `created_by_id` int DEFAULT NULL,
  `created_by` varchar(255) DEFAULT NULL,
  `last_modified_by_id` int DEFAULT NULL,
  `last_modified_by` varchar(255) DEFAULT NULL,
  `last_modified` datetime DEFAULT NULL,
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

-- ref_domain
CREATE TABLE `ref_domain` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(64) NOT NULL,
  `description` varchar(255) DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_ref_domain_name` (`name`)
) ENGINE=InnoDB AUTO_INCREMENT=14 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- ref_value
CREATE TABLE `ref_value` (
  `id` int NOT NULL AUTO_INCREMENT,
  `domain_id` int NOT NULL,
  `code` varchar(255) NOT NULL,
  `label` varchar(255) DEFAULT NULL,
  `is_active` tinyint(1) DEFAULT '1',
  `sort_order` int DEFAULT '0',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_ref_value_domain_code` (`domain_id`,`code`),
  KEY `ix_ref_value_domain` (`domain_id`),
  CONSTRAINT `fk_ref_value_domain` FOREIGN KEY (`domain_id`) REFERENCES `ref_domain` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- report_schedule
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

-- requalification_schedule
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
  `component` varchar(255) DEFAULT NULL,
  `responsible_user_id` int DEFAULT NULL,
  `responsible_user` varchar(255) DEFAULT NULL,
  `protocol_file` varchar(255) DEFAULT NULL,
  `status` varchar(30) DEFAULT NULL,
  `digital_signature` varchar(128) DEFAULT NULL,
  `last_modified` datetime DEFAULT NULL,
  `last_modified_by_id` int DEFAULT NULL,
  `last_modified_by` varchar(255) DEFAULT NULL,
  `source_ip` varchar(64) DEFAULT NULL,
  `session_id` varchar(100) DEFAULT NULL,
  `geo_location` varchar(128) DEFAULT NULL,
  `attachments_json` varchar(255) DEFAULT NULL,
  `regulator` varchar(40) DEFAULT NULL,
  `anomaly_score` decimal(10,2) DEFAULT NULL,
  `analytics_json` varchar(2048) DEFAULT NULL,
  `related_case_id` int DEFAULT NULL,
  `related_case_type` varchar(40) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_requal_component` (`component_id`),
  CONSTRAINT `fk_requal_component` FOREIGN KEY (`component_id`) REFERENCES `machine_components` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- risk_assessments
CREATE TABLE `risk_assessments` (
  `id` int NOT NULL AUTO_INCREMENT,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- role_audit
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
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- role_permissions
CREATE TABLE `role_permissions` (
  `role_id` int NOT NULL,
  `permission_id` int NOT NULL,
  `allowed` tinyint(1) DEFAULT '1',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `assigned_by_id` int DEFAULT NULL,
  `assigned_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `change_version` int DEFAULT '1',
  `role?` varchar(255) DEFAULT NULL,
  `permission?` varchar(255) DEFAULT NULL,
  `assigned_by` int DEFAULT NULL,
  `user?` varchar(255) DEFAULT NULL,
  `expires_at` datetime DEFAULT NULL,
  `reason` varchar(255) DEFAULT NULL,
  `is_active` tinyint(1) DEFAULT NULL,
  `digital_signature` varchar(128) DEFAULT NULL,
  `session_id` varchar(64) DEFAULT NULL,
  `source_ip` varchar(64) DEFAULT NULL,
  `note` varchar(512) DEFAULT NULL,
  `code` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`role_id`,`permission_id`),
  KEY `fk_rp_perm` (`permission_id`),
  KEY `fk_rp_assigned_by_id` (`assigned_by_id`),
  CONSTRAINT `fk_rp_assigned_by_id` FOREIGN KEY (`assigned_by_id`) REFERENCES `users` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_rp_perm` FOREIGN KEY (`permission_id`) REFERENCES `permissions` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_rp_role` FOREIGN KEY (`role_id`) REFERENCES `roles` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- roles
CREATE TABLE `roles` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(50) NOT NULL,
  `description` varchar(255) DEFAULT NULL,
  `org_unit` varchar(80) DEFAULT NULL,
  `compliance_tags` varchar(120) DEFAULT NULL,
  `is_deleted` tinyint(1) NOT NULL DEFAULT '0',
  `notes` varchar(512) DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `created_by_id` int DEFAULT NULL,
  `last_modified_by_id` int DEFAULT NULL,
  `version` int NOT NULL DEFAULT '1',
  `icollection<permission>` varchar(255) DEFAULT NULL,
  `icollection<user>` varchar(255) DEFAULT NULL,
  `icollection<role_permission>` varchar(255) DEFAULT NULL,
  `user?` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `name` (`name`),
  KEY `fk_roles_created_by` (`created_by_id`),
  KEY `fk_roles_last_modified_by` (`last_modified_by_id`),
  CONSTRAINT `fk_roles_created_by` FOREIGN KEY (`created_by_id`) REFERENCES `users` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_roles_last_modified_by` FOREIGN KEY (`last_modified_by_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=18 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- rooms
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

-- scheduled_job_audit_log
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

-- scheduled_jobs
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
  `last_modified_at` datetime DEFAULT NULL,
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

-- schema_migration_log
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
  `migrated_by_id` int DEFAULT NULL,
  `username` varchar(80) DEFAULT NULL,
  `rollback_script` varchar(255) DEFAULT NULL,
  `device_info` varchar(255) DEFAULT NULL,
  `digital_signature` varchar(128) DEFAULT NULL,
  `entry_hash` varchar(128) DEFAULT NULL,
  `attachments_json` varchar(255) DEFAULT NULL,
  `regulator` varchar(40) DEFAULT NULL,
  `anomaly_score` decimal(10,2) DEFAULT NULL,
  `note` varchar(1000) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_mig_user` (`migrated_by`),
  CONSTRAINT `fk_mig_user` FOREIGN KEY (`migrated_by`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- sensitive_data_access_log
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
  `user` varchar(255) DEFAULT NULL,
  `username` varchar(80) DEFAULT NULL,
  `device_info` varchar(255) DEFAULT NULL,
  `session_id` varchar(100) DEFAULT NULL,
  `approved_by_id` int DEFAULT NULL,
  `approver_name` varchar(100) DEFAULT NULL,
  `approved_at` datetime DEFAULT NULL,
  `approval_method` varchar(40) DEFAULT NULL,
  `digital_signature` varchar(128) DEFAULT NULL,
  `entry_hash` varchar(128) DEFAULT NULL,
  `geo_location` varchar(100) DEFAULT NULL,
  `severity` varchar(24) DEFAULT NULL,
  `anomaly_score` decimal(10,2) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_sdal_user` (`user_id`),
  KEY `fk_sdal_appr` (`approved_by`),
  CONSTRAINT `fk_sdal_appr` FOREIGN KEY (`approved_by`) REFERENCES `users` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_sdal_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- sensor_data_logs
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

-- sensor_models
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

-- sensor_types
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

-- session_log
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
  `user` varchar(255) DEFAULT NULL,
  `session_id` varchar(100) DEFAULT NULL,
  `login_at` datetime DEFAULT NULL,
  `logout_at` datetime DEFAULT NULL,
  `is_impersonated` tinyint(1) DEFAULT NULL,
  `impersonated_by_id` int DEFAULT NULL,
  `impersonated_by` varchar(255) DEFAULT NULL,
  `is_temporary_escalation` tinyint(1) DEFAULT NULL,
  `note` varchar(400) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_sl_user` (`user_id`),
  KEY `fk_sl_by` (`terminated_by`),
  CONSTRAINT `fk_sl_by` FOREIGN KEY (`terminated_by`) REFERENCES `users` (`id`),
  CONSTRAINT `fk_sl_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- settings
CREATE TABLE `settings` (
  `id` int NOT NULL AUTO_INCREMENT,
  `updated_at` datetime DEFAULT NULL,
  `updated_by_id` int DEFAULT NULL,
  `value` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- sites
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

-- sop_document_log
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

-- sop_documents
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
  `description` varchar(400) DEFAULT NULL,
  `process` varchar(80) DEFAULT NULL,
  `language` varchar(10) DEFAULT NULL,
  `date_issued` datetime DEFAULT NULL,
  `date_expiry` datetime DEFAULT NULL,
  `next_review_date` datetime DEFAULT NULL,
  `attachments` varchar(255) DEFAULT NULL,
  `responsible_user_id` int DEFAULT NULL,
  `responsible_user` varchar(255) DEFAULT NULL,
  `created_by_id` int DEFAULT NULL,
  `version_no` int DEFAULT NULL,
  `file_hash` varchar(128) DEFAULT NULL,
  `digital_signature` varchar(128) DEFAULT NULL,
  `chain_hash` varchar(128) DEFAULT NULL,
  `approver_ids` varchar(255) DEFAULT NULL,
  `approvers` varchar(255) DEFAULT NULL,
  `approval_timestamps` varchar(255) DEFAULT NULL,
  `review_notes` varchar(1000) DEFAULT NULL,
  `pdf_metadata` varchar(1024) DEFAULT NULL,
  `related_type` varchar(40) DEFAULT NULL,
  `related_id` int DEFAULT NULL,
  `comment` varchar(400) DEFAULT NULL,
  `last_modified` datetime DEFAULT NULL,
  `last_modified_by_id` int DEFAULT NULL,
  `last_modified_by` varchar(255) DEFAULT NULL,
  `source_ip` varchar(64) DEFAULT NULL,
  `ai_tags` varchar(512) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `code` (`code`),
  KEY `fk_sop_created` (`created_by`),
  KEY `fk_sop_approved` (`approved_by`),
  CONSTRAINT `fk_sop_approved` FOREIGN KEY (`approved_by`) REFERENCES `users` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_sop_created` FOREIGN KEY (`created_by`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- stock_levels
CREATE TABLE `stock_levels` (
  `id` int NOT NULL AUTO_INCREMENT,
  `part_id` int DEFAULT NULL,
  `warehouse_id` int DEFAULT NULL,
  `quantity` int DEFAULT NULL,
  `min_threshold` int DEFAULT NULL,
  `max_threshold` int DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `part` varchar(255) DEFAULT NULL,
  `warehouse` varchar(255) DEFAULT NULL,
  `auto_reorder_triggered` tinyint(1) DEFAULT NULL,
  `days_below_min` int DEFAULT NULL,
  `alarm_status` varchar(30) DEFAULT NULL,
  `anomaly_score` decimal(10,2) DEFAULT NULL,
  `last_modified` datetime DEFAULT NULL,
  `last_modified_by_id` int DEFAULT NULL,
  `last_modified_by` varchar(255) DEFAULT NULL,
  `source_ip` varchar(45) DEFAULT NULL,
  `geo_location` varchar(100) DEFAULT NULL,
  `comment` varchar(255) DEFAULT NULL,
  `digital_signature` varchar(128) DEFAULT NULL,
  `entry_hash` varchar(128) DEFAULT NULL,
  `old_state_snapshot` varchar(255) DEFAULT NULL,
  `new_state_snapshot` varchar(255) DEFAULT NULL,
  `is_automated` tinyint(1) DEFAULT NULL,
  `session_id` varchar(80) DEFAULT NULL,
  `related_case_id` int DEFAULT NULL,
  `related_case_type` varchar(30) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_sl_part` (`part_id`),
  KEY `fk_sl_warehouse` (`warehouse_id`),
  CONSTRAINT `fk_sl_part` FOREIGN KEY (`part_id`) REFERENCES `parts` (`id`),
  CONSTRAINT `fk_sl_warehouse` FOREIGN KEY (`warehouse_id`) REFERENCES `warehouses` (`id`),
  CONSTRAINT `chk_qty_nonneg` CHECK ((`quantity` >= 0)),
  CONSTRAINT `chk_sl_ranges` CHECK (((`min_threshold` is null) or (`max_threshold` is null) or (`max_threshold` >= `min_threshold`)))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- supplier
CREATE TABLE `supplier` (
  `id` int NOT NULL AUTO_INCREMENT,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- supplier_audit
CREATE TABLE `supplier_audit` (
  `id` int NOT NULL AUTO_INCREMENT,
  `digital_signature` varchar(255) DEFAULT NULL,
  `changed_at` datetime DEFAULT NULL,
  `source_ip` varchar(255) DEFAULT NULL,
  `details` varchar(255) DEFAULT NULL,
  `device_info` varchar(255) DEFAULT NULL,
  `supplier_id` int DEFAULT NULL,
  `action` varchar(255) DEFAULT NULL,
  `user_id` int DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- supplier_risk_audit
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

-- suppliers
CREATE TABLE `suppliers` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(100) DEFAULT NULL,
  `vat_number` varchar(40) DEFAULT NULL,
  `code` varchar(50) DEFAULT NULL,
  `oib` varchar(40) DEFAULT NULL,
  `contact` varchar(100) DEFAULT NULL,
  `email` varchar(100) DEFAULT NULL,
  `phone` varchar(50) DEFAULT NULL,
  `website` varchar(200) DEFAULT NULL,
  `supplier_type` varchar(40) DEFAULT NULL,
  `notes` text,
  `contract_file` varchar(255) DEFAULT NULL,
  `address` text,
  `city` varchar(80) DEFAULT NULL,
  `country` varchar(80) DEFAULT NULL,
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

-- system_event_log
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
  `ts_utc` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `username` varchar(128) DEFAULT NULL,
  `digital_signature` varchar(256) DEFAULT NULL,
  `entry_hash` varchar(256) DEFAULT NULL,
  `mac_address` varchar(64) DEFAULT NULL,
  `geo_location` varchar(128) DEFAULT NULL,
  `regulator` varchar(64) DEFAULT NULL,
  `related_case_id` int DEFAULT NULL,
  `related_case_type` varchar(64) DEFAULT NULL,
  `anomaly_score` double DEFAULT NULL,
  `user` varchar(255) DEFAULT NULL,
  `change_version` int DEFAULT NULL,
  `is_deleted` tinyint(1) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_sel_user` (`user_id`),
  KEY `idx_sel_time_sev` (`timestamp`,`severity`),
  KEY `ix_system_event_log_ts` (`ts_utc`),
  KEY `ix_event_table_record_ts` (`table_name`,`record_id`,`ts_utc`),
  KEY `ix_event_type_ts` (`event_type`,`ts_utc`),
  CONSTRAINT `fk_sel_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=15299 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- system_parameters
CREATE TABLE `system_parameters` (
  `id` int NOT NULL AUTO_INCREMENT,
  `param_name` varchar(100) DEFAULT NULL,
  `param_value` text,
  `updated_by` int DEFAULT NULL,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `note` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `digital_signature` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `param_name` (`param_name`),
  KEY `fk_sysparam_user` (`updated_by`),
  CONSTRAINT `fk_sysparam_user` FOREIGN KEY (`updated_by`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- tags
CREATE TABLE `tags` (
  `id` int NOT NULL AUTO_INCREMENT,
  `tag` varchar(60) NOT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `tag` (`tag`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- tenants
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

-- user_audit
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
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- user_esignatures
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

-- user_login_audit
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
  `two_factor_ok` tinyint(1) DEFAULT NULL,
  `sso_used` tinyint(1) DEFAULT NULL,
  `biometric_used` tinyint(1) DEFAULT NULL,
  `geo_location` varchar(128) DEFAULT NULL,
  `risk_score` double DEFAULT NULL,
  `status` varchar(32) DEFAULT NULL,
  `digital_signature` varchar(128) DEFAULT NULL,
  `note` text,
  `user` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_ula_user` (`user_id`),
  CONSTRAINT `fk_ula_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- user_permissions
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
  `user` varchar(255) DEFAULT NULL,
  `permission` varchar(255) DEFAULT NULL,
  `assigned_at` datetime DEFAULT NULL,
  `assigned_by` int DEFAULT NULL,
  `is_active` tinyint(1) DEFAULT NULL,
  `change_version` int DEFAULT NULL,
  `digital_signature` varchar(128) DEFAULT NULL,
  `session_id` varchar(64) DEFAULT NULL,
  `source_ip` varchar(64) DEFAULT NULL,
  `note` varchar(512) DEFAULT NULL,
  `code` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`user_id`,`permission_id`),
  KEY `fk_up_perm` (`permission_id`),
  KEY `fk_up_by` (`granted_by`),
  CONSTRAINT `fk_up_by` FOREIGN KEY (`granted_by`) REFERENCES `users` (`id`),
  CONSTRAINT `fk_up_perm` FOREIGN KEY (`permission_id`) REFERENCES `permissions` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_up_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- user_roles
CREATE TABLE `user_roles` (
  `user_id` int NOT NULL,
  `role_id` int NOT NULL,
  `assigned_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `assigned_by_id` int DEFAULT NULL,
  `expires_at` datetime DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `granted_by` int DEFAULT NULL,
  `granted_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `is_active` tinyint(1) DEFAULT '1',
  `change_version` int DEFAULT '1',
  `digital_signature` varchar(256) DEFAULT NULL,
  `user` varchar(255) DEFAULT NULL,
  `role` varchar(255) DEFAULT NULL,
  `assigned_by` int DEFAULT NULL,
  `reason` varchar(255) DEFAULT NULL,
  `session_id` varchar(64) DEFAULT NULL,
  `source_ip` varchar(64) DEFAULT NULL,
  `note` varchar(512) DEFAULT NULL,
  PRIMARY KEY (`user_id`,`role_id`),
  KEY `fk_ur_role` (`role_id`),
  KEY `ix_user_roles_granted_by` (`granted_by`),
  KEY `fk_ur_assigned_by_id` (`assigned_by_id`),
  CONSTRAINT `fk_ur_assigned_by_id` FOREIGN KEY (`assigned_by_id`) REFERENCES `users` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_ur_role` FOREIGN KEY (`role_id`) REFERENCES `roles` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_ur_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_user_roles_granted_by` FOREIGN KEY (`granted_by`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- user_subscriptions
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

-- user_training
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

-- user_window_layouts
CREATE TABLE `user_window_layouts` (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_id` int NOT NULL,
  `page_type` varchar(200) NOT NULL,
  `pos_x` int DEFAULT NULL,
  `pos_y` int DEFAULT NULL,
  `width` int NOT NULL,
  `height` int NOT NULL,
  `saved_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `ux_user_page` (`user_id`,`page_type`),
  CONSTRAINT `fk_uwl_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- users
CREATE TABLE `users` (
  `id` int NOT NULL AUTO_INCREMENT,
  `username` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `password` varchar(128) NOT NULL,
  `password_hash` varchar(256) DEFAULT NULL,
  `full_name` varchar(100) NOT NULL,
  `role` varchar(30) DEFAULT NULL,
  `role_id` int DEFAULT NULL,
  `active` tinyint(1) DEFAULT '1',
  `is_locked` tinyint(1) DEFAULT '0',
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
  `change_version` int NOT NULL DEFAULT '1',
  `failed_login_attempts` int DEFAULT '0',
  `tenant_id` int DEFAULT NULL,
  `job_title_id` int DEFAULT NULL,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `user?` varchar(255) DEFAULT NULL,
  `icollection<role>` varchar(255) DEFAULT NULL,
  `icollection<permission>` varchar(255) DEFAULT NULL,
  `icollection<delegated_permission>` varchar(255) DEFAULT NULL,
  `icollection<audit_log>` varchar(255) DEFAULT NULL,
  `icollection<digital_signature>` varchar(255) DEFAULT NULL,
  `icollection<session_log>` varchar(255) DEFAULT NULL,
  `icollection<work_order>` varchar(255) DEFAULT NULL,
  `icollection<photo>` varchar(255) DEFAULT NULL,
  `icollection<attachment>` varchar(255) DEFAULT NULL,
  `icollection<admin_activity_log>` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `ux_users_username_lower` ((lower(`username`))),
  KEY `fk_users_last_modified_by` (`last_modified_by_id`),
  KEY `fk_users_department` (`department_id`),
  KEY `fk_users_tenant_id` (`tenant_id`),
  KEY `fk_users_job_title` (`job_title_id`),
  KEY `fk_users_role` (`role_id`),
  KEY `idx_users_username_insensitive` ((lower(`username`))),
  CONSTRAINT `fk_users_department` FOREIGN KEY (`department_id`) REFERENCES `departments` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_users_job_title` FOREIGN KEY (`job_title_id`) REFERENCES `job_titles` (`id`),
  CONSTRAINT `fk_users_last_modified_by` FOREIGN KEY (`last_modified_by_id`) REFERENCES `users` (`id`),
  CONSTRAINT `fk_users_role` FOREIGN KEY (`role_id`) REFERENCES `roles` (`id`),
  CONSTRAINT `fk_users_tenant_id` FOREIGN KEY (`tenant_id`) REFERENCES `tenants` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=10 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- validation_audit
CREATE TABLE `validation_audit` (
  `id` int NOT NULL AUTO_INCREMENT,
  `digital_signature` varchar(255) DEFAULT NULL,
  `changed_at` datetime DEFAULT NULL,
  `source_ip` varchar(255) DEFAULT NULL,
  `details` varchar(255) DEFAULT NULL,
  `device_info` varchar(255) DEFAULT NULL,
  `validation_id` int DEFAULT NULL,
  `action` varchar(255) DEFAULT NULL,
  `user_id` int DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- validations
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
  `code` varchar(40) DEFAULT NULL,
  `machine?` varchar(255) DEFAULT NULL,
  `machine_component?` varchar(255) DEFAULT NULL,
  `date_start` datetime DEFAULT NULL,
  `date_end` datetime DEFAULT NULL,
  `signed_by_id` int DEFAULT NULL,
  `user?` varchar(255) DEFAULT NULL,
  `signed_by_name` varchar(100) DEFAULT NULL,
  `entry_hash` varchar(128) DEFAULT NULL,
  `created_by_id` int DEFAULT NULL,
  `last_modified` datetime DEFAULT NULL,
  `last_modified_by_id` int DEFAULT NULL,
  `source_ip` varchar(64) DEFAULT NULL,
  `workflow_status` varchar(40) DEFAULT NULL,
  `additional_signers` varchar(512) DEFAULT NULL,
  `regulator` varchar(60) DEFAULT NULL,
  `anomaly_score` decimal(10,2) DEFAULT NULL,
  `linked_capa_id` int DEFAULT NULL,
  `capa_case?` varchar(255) DEFAULT NULL,
  `signature_timestamp` datetime DEFAULT NULL,
  `session_id` varchar(80) DEFAULT NULL,
  `documentation` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_val_machine` (`machine_id`),
  KEY `fk_val_component` (`component_id`),
  CONSTRAINT `fk_val_component` FOREIGN KEY (`component_id`) REFERENCES `machine_components` (`id`),
  CONSTRAINT `fk_val_machine` FOREIGN KEY (`machine_id`) REFERENCES `machines` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- warehouses
CREATE TABLE `warehouses` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(100) DEFAULT NULL,
  `location` varchar(255) DEFAULT NULL,
  `responsible_id` int DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `location_id` int DEFAULT NULL,
  `responsible` varchar(255) DEFAULT NULL,
  `qr_code` varchar(255) DEFAULT NULL,
  `note` varchar(500) DEFAULT NULL,
  `created_by_id` int DEFAULT NULL,
  `created_by` varchar(255) DEFAULT NULL,
  `last_modified` datetime DEFAULT NULL,
  `last_modified_by_id` int DEFAULT NULL,
  `last_modified_by` varchar(255) DEFAULT NULL,
  `digital_signature` varchar(128) DEFAULT NULL,
  `status` varchar(30) DEFAULT NULL,
  `io_tdevice_id` varchar(64) DEFAULT NULL,
  `climate_mode` varchar(60) DEFAULT NULL,
  `compliance_docs` varchar(255) DEFAULT NULL,
  `entry_hash` varchar(128) DEFAULT NULL,
  `source_ip` varchar(45) DEFAULT NULL,
  `is_qualified` tinyint(1) DEFAULT NULL,
  `last_qualified` datetime DEFAULT NULL,
  `session_id` varchar(80) DEFAULT NULL,
  `anomaly_score` decimal(10,2) DEFAULT NULL,
  `is_deleted` tinyint(1) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_wh_user` (`responsible_id`),
  KEY `fk_wh_location` (`location_id`),
  CONSTRAINT `fk_wh_location` FOREIGN KEY (`location_id`) REFERENCES `locations` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_wh_user` FOREIGN KEY (`responsible_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- work_order_audit
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
  `work_order` varchar(255) DEFAULT NULL,
  `user` varchar(255) DEFAULT NULL,
  `session_id` varchar(64) DEFAULT NULL,
  `digital_signature` varchar(256) DEFAULT NULL,
  `integrity_hash` varchar(256) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_woa_wo` (`work_order_id`),
  KEY `fk_woa_user` (`user_id`),
  CONSTRAINT `fk_woa_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_woa_wo` FOREIGN KEY (`work_order_id`) REFERENCES `work_orders` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- work_order_checklist_item
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

-- work_order_comments
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

-- work_order_parts
CREATE TABLE `work_order_parts` (
  `work_order_id` int NOT NULL,
  `part_id` int NOT NULL,
  `quantity` int DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `id` int DEFAULT NULL,
  `unit_of_measure` varchar(255) DEFAULT NULL,
  `unit_price` decimal(10,2) DEFAULT NULL,
  `currency` varchar(255) DEFAULT NULL,
  `warehouse_id` int DEFAULT NULL,
  `capa_case_id` int DEFAULT NULL,
  `incident_id` int DEFAULT NULL,
  `used_at` datetime DEFAULT NULL,
  `used_by_id` int DEFAULT NULL,
  `digital_signature` varchar(255) DEFAULT NULL,
  `source_ip` varchar(255) DEFAULT NULL,
  `device_info` varchar(255) DEFAULT NULL,
  `session_id` varchar(255) DEFAULT NULL,
  `note` varchar(255) DEFAULT NULL,
  `last_modified_at` datetime DEFAULT NULL,
  `last_modified_by_id` int DEFAULT NULL,
  `work_order?` varchar(255) DEFAULT NULL,
  `part?` varchar(255) DEFAULT NULL,
  `warehouse?` varchar(255) DEFAULT NULL,
  `user?` varchar(255) DEFAULT NULL,
  `capa_case?` varchar(255) DEFAULT NULL,
  `incident?` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`work_order_id`,`part_id`),
  KEY `fk_wop_part` (`part_id`),
  CONSTRAINT `fk_wop_part` FOREIGN KEY (`part_id`) REFERENCES `parts` (`id`),
  CONSTRAINT `fk_wop_wo` FOREIGN KEY (`work_order_id`) REFERENCES `work_orders` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- work_order_signatures
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

-- work_order_status_log
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
  `work_order` varchar(255) DEFAULT NULL,
  `changed_by_id` int DEFAULT NULL,
  `reason` varchar(400) DEFAULT NULL,
  `is_incident` tinyint(1) DEFAULT NULL,
  `device_info` varchar(255) DEFAULT NULL,
  `ip_address` varchar(45) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_wosl_wo` (`work_order_id`),
  KEY `fk_wosl_user` (`changed_by`),
  CONSTRAINT `fk_wosl_user` FOREIGN KEY (`changed_by`) REFERENCES `users` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_wosl_wo` FOREIGN KEY (`work_order_id`) REFERENCES `work_orders` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- work_orders
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
  `title` varchar(255) DEFAULT NULL,
  `task_description` varchar(255) DEFAULT NULL,
  `due_date` datetime DEFAULT NULL,
  `closed_at` datetime DEFAULT NULL,
  `requested_by_id` int DEFAULT NULL,
  `user?` varchar(255) DEFAULT NULL,
  `created_by_id` int DEFAULT NULL,
  `assigned_to_id` int DEFAULT NULL,
  `machine?` varchar(255) DEFAULT NULL,
  `machine_component?` varchar(255) DEFAULT NULL,
  `capa_case_id` int DEFAULT NULL,
  `capa_case?` varchar(255) DEFAULT NULL,
  `incident_id` int DEFAULT NULL,
  `incident?` varchar(255) DEFAULT NULL,
  `notes` varchar(255) DEFAULT NULL,
  `last_modified` datetime DEFAULT NULL,
  `last_modified_by_id` int DEFAULT NULL,
  `device_info` varchar(255) DEFAULT NULL,
  `source_ip` varchar(255) DEFAULT NULL,
  `session_id` varchar(255) DEFAULT NULL,
  `document_path` varchar(255) DEFAULT NULL,
  `next_due` datetime DEFAULT NULL,
  `external_ref` varchar(255) DEFAULT NULL,
  `entry_hash` varchar(255) DEFAULT NULL,
  `audit_flag` tinyint(1) DEFAULT NULL,
  `anomaly_score` decimal(10,2) DEFAULT NULL,
  `photo_before_ids` varchar(255) DEFAULT NULL,
  `photo_after_ids` varchar(255) DEFAULT NULL,
  `icollection<work_order_part>` varchar(255) DEFAULT NULL,
  `icollection<photo>` varchar(255) DEFAULT NULL,
  `icollection<work_order_comment>` varchar(255) DEFAULT NULL,
  `icollection<work_order_status_log>` varchar(255) DEFAULT NULL,
  `icollection<work_order_signature>` varchar(255) DEFAULT NULL,
  `icollection<work_order_audit>` varchar(255) DEFAULT NULL,
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
