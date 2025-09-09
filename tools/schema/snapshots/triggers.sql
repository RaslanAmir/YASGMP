-- Triggers snapshot for yasgmp - 2025-09-05T07:34:23.5464817Z

-- trg_admin_activity_log_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_admin_activity_log_ad_audit` AFTER DELETE ON `admin_activity_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'admin_activity_log', 'AdminActivityLogPage', OLD.id, NULL,
     NULL, NULL, CONCAT('admin_activity_log delete'), 'system', 'server', '', 'warning');
END;

-- trg_admin_activity_log_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_admin_activity_log_ai_audit` AFTER INSERT ON `admin_activity_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'admin_activity_log', 'AdminActivityLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('admin_activity_log insert'), 'system', 'server', '', 'info');
END;

-- trg_admin_activity_log_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_admin_activity_log_au_audit` AFTER UPDATE ON `admin_activity_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'admin_activity_log', 'AdminActivityLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('admin_activity_log update'), 'system', 'server', '', 'info');

END;

-- trg_admin_activity_log_fk_guard
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_admin_activity_log_fk_guard` BEFORE INSERT ON `admin_activity_log` FOR EACH ROW BEGIN
  IF NEW.admin_id IS NOT NULL THEN
    IF (SELECT COUNT(*) FROM users WHERE id = NEW.admin_id) = 0 THEN
      SET NEW.admin_id = NULL;
    END IF;
  END IF;
END;

-- trg_api_audit_log_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_api_audit_log_ad_audit` AFTER DELETE ON `api_audit_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'api_audit_log', 'ApiAuditLogPage', OLD.id, NULL,
     NULL, NULL, CONCAT('api_audit_log delete'), 'system', 'server', '', 'warning');
END;

-- trg_api_audit_log_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_api_audit_log_ai_audit` AFTER INSERT ON `api_audit_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'api_audit_log', 'ApiAuditLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('api_audit_log insert'), 'system', 'server', '', 'info');
END;

-- trg_api_audit_log_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_api_audit_log_au_audit` AFTER UPDATE ON `api_audit_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'api_audit_log', 'ApiAuditLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('api_audit_log update'), 'system', 'server', '', 'info');

END;

-- trg_api_keys_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_api_keys_ad_audit` AFTER DELETE ON `api_keys` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'api_keys', 'ApiKeysPage', OLD.id, NULL,
     NULL, NULL, CONCAT('api_keys delete'), 'system', 'server', '', 'warning');
END;

-- trg_api_keys_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_api_keys_ai_audit` AFTER INSERT ON `api_keys` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'api_keys', 'ApiKeysPage', NEW.id, NULL,
     NULL, NULL, CONCAT('api_keys insert'), 'system', 'server', '', 'info');
END;

-- trg_api_keys_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_api_keys_au_audit` AFTER UPDATE ON `api_keys` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'api_keys', 'ApiKeysPage', NEW.id, NULL,
     NULL, NULL, CONCAT('api_keys update'), 'system', 'server', '', 'info');

END;

-- trg_api_usage_log_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_api_usage_log_ad_audit` AFTER DELETE ON `api_usage_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'api_usage_log', 'ApiUsageLogPage', OLD.id, NULL,
     NULL, NULL, CONCAT('api_usage_log delete'), 'system', 'server', '', 'warning');
END;

-- trg_api_usage_log_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_api_usage_log_ai_audit` AFTER INSERT ON `api_usage_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'api_usage_log', 'ApiUsageLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('api_usage_log insert'), 'system', 'server', '', 'info');
END;

-- trg_api_usage_log_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_api_usage_log_au_audit` AFTER UPDATE ON `api_usage_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'api_usage_log', 'ApiUsageLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('api_usage_log update'), 'system', 'server', '', 'info');

END;

-- trg_attachments_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_attachments_ad_audit` AFTER DELETE ON `attachments` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'attachments', 'AttachmentsPage', OLD.id, NULL,
     NULL, NULL, CONCAT('attachments delete'), 'system', 'server', '', 'warning');
END;

-- trg_attachments_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_attachments_ai_audit` AFTER INSERT ON `attachments` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'attachments', 'AttachmentsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('attachments insert'), 'system', 'server', '', 'info');
END;

-- trg_attachments_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_attachments_au_audit` AFTER UPDATE ON `attachments` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'attachments', 'AttachmentsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('attachments update'), 'system', 'server', '', 'info');

END;

-- trg_buildings_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_buildings_ad_audit` AFTER DELETE ON `buildings` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'buildings', 'BuildingsPage', OLD.id, NULL,
     NULL, NULL, CONCAT('buildings delete', CONCAT('; code=', COALESCE(OLD.code,'')), CONCAT('; name=', COALESCE(OLD.name,''))), 'system', 'server', '', 'warning');
END;

-- trg_buildings_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_buildings_ai_audit` AFTER INSERT ON `buildings` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'buildings', 'BuildingsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('buildings insert', CONCAT('; code=', COALESCE(NEW.code,'')), CONCAT('; name=', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');
END;

-- trg_buildings_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_buildings_au_audit` AFTER UPDATE ON `buildings` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'buildings', 'BuildingsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('buildings update', CONCAT('; code: ', COALESCE(OLD.code,''), ' → ', COALESCE(NEW.code,'')), CONCAT('; name: ', COALESCE(OLD.name,''), ' → ', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');

  IF COALESCE(OLD.code,'') <> COALESCE(NEW.code,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'buildings', 'BuildingsPage', NEW.id, 'code',
       OLD.code, NEW.code, 'buildings update: code changed', 'system', 'server', '', 'info');
  END IF;

  IF COALESCE(OLD.name,'') <> COALESCE(NEW.name,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'buildings', 'BuildingsPage', NEW.id, 'name',
       OLD.name, NEW.name, 'buildings update: name changed', 'system', 'server', '', 'info');
  END IF;
END;

-- trg_calibrations_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_calibrations_ad_audit` AFTER DELETE ON `calibrations` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'calibrations', 'CalibrationsPage', OLD.id, NULL,
     NULL, NULL, CONCAT('calibrations delete'), 'system', 'server', '', 'warning');
END;

-- trg_calibrations_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_calibrations_ai_audit` AFTER INSERT ON `calibrations` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'calibrations', 'CalibrationsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('calibrations insert'), 'system', 'server', '', 'info');
END;

-- trg_calibrations_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_calibrations_au_audit` AFTER UPDATE ON `calibrations` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'calibrations', 'CalibrationsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('calibrations update'), 'system', 'server', '', 'info');

END;

-- trg_calibrations_delete
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_calibrations_delete` AFTER DELETE ON `calibrations` FOR EACH ROW BEGIN
    INSERT INTO calibration_audit_log (
        calibration_id, user_id, action, old_value, source_ip
    ) VALUES (
        OLD.id, OLD.last_modified_by_id, 'DELETE', OLD.comment, OLD.source_ip
    );

    INSERT INTO system_event_log(user_id, event_type, table_name, related_module, record_id, description, source_ip, severity)
    VALUES (OLD.last_modified_by_id, 'DELETE', 'calibrations', 'CalibrationModule', OLD.id,
            CONCAT('Deleted calibration record ID=', OLD.id), 'system', 'critical');
END;

-- trg_calibrations_insert
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_calibrations_insert` AFTER INSERT ON `calibrations` FOR EACH ROW BEGIN
    INSERT INTO calibration_audit_log (
        calibration_id, user_id, action, new_value, source_ip
    ) VALUES (
        NEW.id, NEW.last_modified_by_id, 'CREATE',
        CONCAT('Created calibration for component ', NEW.component_id), NEW.source_ip
    );

    INSERT INTO system_event_log(user_id, event_type, table_name, related_module, record_id, description, source_ip, severity)
    VALUES (NEW.last_modified_by_id, 'CREATE', 'calibrations', 'CalibrationModule', NEW.id,
            CONCAT('Created calibration record ID=', NEW.id), NEW.source_ip, 'audit');
END;

-- trg_calibrations_update
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_calibrations_update` AFTER UPDATE ON `calibrations` FOR EACH ROW BEGIN
    INSERT INTO calibration_audit_log (
        calibration_id, user_id, action, old_value, new_value, source_ip
    ) VALUES (
        NEW.id, NEW.last_modified_by_id, 'UPDATE', OLD.comment, NEW.comment, NEW.source_ip
    );

    INSERT INTO system_event_log(user_id, event_type, table_name, related_module, record_id, field_name, old_value, new_value, description, source_ip, severity)
    VALUES (NEW.last_modified_by_id, 'UPDATE', 'calibrations', 'CalibrationModule', NEW.id,
            'comment', OLD.comment, NEW.comment, 'Updated calibration record', NEW.source_ip, 'audit');
END;

-- trg_calibration_audit_log_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_calibration_audit_log_ad_audit` AFTER DELETE ON `calibration_audit_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'calibration_audit_log', 'CalibrationAuditLogPage', OLD.id, NULL,
     NULL, NULL, CONCAT('calibration_audit_log delete'), 'system', 'server', '', 'warning');
END;

-- trg_calibration_audit_log_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_calibration_audit_log_ai_audit` AFTER INSERT ON `calibration_audit_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'calibration_audit_log', 'CalibrationAuditLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('calibration_audit_log insert'), 'system', 'server', '', 'info');
END;

-- trg_calibration_audit_log_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_calibration_audit_log_au_audit` AFTER UPDATE ON `calibration_audit_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'calibration_audit_log', 'CalibrationAuditLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('calibration_audit_log update'), 'system', 'server', '', 'info');

END;

-- trg_calibration_export_log_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_calibration_export_log_ad_audit` AFTER DELETE ON `calibration_export_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'calibration_export_log', 'CalibrationExportLogPage', OLD.id, NULL,
     NULL, NULL, CONCAT('calibration_export_log delete'), 'system', 'server', '', 'warning');
END;

-- trg_calibration_export_log_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_calibration_export_log_ai_audit` AFTER INSERT ON `calibration_export_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'calibration_export_log', 'CalibrationExportLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('calibration_export_log insert'), 'system', 'server', '', 'info');
END;

-- trg_calibration_export_log_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_calibration_export_log_au_audit` AFTER UPDATE ON `calibration_export_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'calibration_export_log', 'CalibrationExportLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('calibration_export_log update'), 'system', 'server', '', 'info');

END;

-- trg_calibration_sensors_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_calibration_sensors_ad_audit` AFTER DELETE ON `calibration_sensors` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'calibration_sensors', 'CalibrationSensorsPage', OLD.id, NULL,
     NULL, NULL, CONCAT('calibration_sensors delete'), 'system', 'server', '', 'warning');
END;

-- trg_calibration_sensors_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_calibration_sensors_ai_audit` AFTER INSERT ON `calibration_sensors` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'calibration_sensors', 'CalibrationSensorsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('calibration_sensors insert'), 'system', 'server', '', 'info');
END;

-- trg_calibration_sensors_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_calibration_sensors_au_audit` AFTER UPDATE ON `calibration_sensors` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'calibration_sensors', 'CalibrationSensorsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('calibration_sensors update'), 'system', 'server', '', 'info');

END;

-- trg_cal_sync2
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_cal_sync2` BEFORE INSERT ON `calibrations` FOR EACH ROW BEGIN
  CALL ref_touch('calibration_result', NEW.result, NEW.result);
  IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.result_id = LAST_INSERT_ID(); END IF;
END;

-- trg_cal_sync2_u
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_cal_sync2_u` BEFORE UPDATE ON `calibrations` FOR EACH ROW BEGIN
  IF (NEW.result <=> OLD.result) = 0 THEN
    CALL ref_touch('calibration_result', NEW.result, NEW.result);
    IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.result_id = LAST_INSERT_ID(); END IF;
  END IF;
END;

-- trg_capa_actions_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_capa_actions_ad_audit` AFTER DELETE ON `capa_actions` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'capa_actions', 'CapaActionsPage', OLD.id, NULL,
     NULL, NULL, CONCAT('capa_actions delete'), 'system', 'server', '', 'warning');
END;

-- trg_capa_actions_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_capa_actions_ai_audit` AFTER INSERT ON `capa_actions` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'capa_actions', 'CapaActionsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('capa_actions insert'), 'system', 'server', '', 'info');
END;

-- trg_capa_actions_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_capa_actions_au_audit` AFTER UPDATE ON `capa_actions` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'capa_actions', 'CapaActionsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('capa_actions update'), 'system', 'server', '', 'info');

END;

-- trg_capa_action_log_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_capa_action_log_ad_audit` AFTER DELETE ON `capa_action_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'capa_action_log', 'CapaActionLogPage', OLD.id, NULL,
     NULL, NULL, CONCAT('capa_action_log delete'), 'system', 'server', '', 'warning');
END;

-- trg_capa_action_log_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_capa_action_log_ai_audit` AFTER INSERT ON `capa_action_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'capa_action_log', 'CapaActionLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('capa_action_log insert'), 'system', 'server', '', 'info');
END;

-- trg_capa_action_log_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_capa_action_log_au_audit` AFTER UPDATE ON `capa_action_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'capa_action_log', 'CapaActionLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('capa_action_log update'), 'system', 'server', '', 'info');

END;

-- trg_capa_cases_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_capa_cases_ad_audit` AFTER DELETE ON `capa_cases` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'capa_cases', 'CapaCasesPage', OLD.id, NULL,
     NULL, NULL, CONCAT('capa_cases delete'), 'system', 'server', '', 'warning');
END;

-- trg_capa_cases_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_capa_cases_ai_audit` AFTER INSERT ON `capa_cases` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'capa_cases', 'CapaCasesPage', NEW.id, NULL,
     NULL, NULL, CONCAT('capa_cases insert'), 'system', 'server', '', 'info');
END;

-- trg_capa_cases_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_capa_cases_au_audit` AFTER UPDATE ON `capa_cases` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'capa_cases', 'CapaCasesPage', NEW.id, NULL,
     NULL, NULL, CONCAT('capa_cases update'), 'system', 'server', '', 'info');

END;

-- trg_capa_status_history_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_capa_status_history_ad_audit` AFTER DELETE ON `capa_status_history` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'capa_status_history', 'CapaStatusHistoryPage', OLD.id, NULL,
     NULL, NULL, CONCAT('capa_status_history delete'), 'system', 'server', '', 'warning');
END;

-- trg_capa_status_history_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_capa_status_history_ai_audit` AFTER INSERT ON `capa_status_history` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'capa_status_history', 'CapaStatusHistoryPage', NEW.id, NULL,
     NULL, NULL, CONCAT('capa_status_history insert'), 'system', 'server', '', 'info');
END;

-- trg_capa_status_history_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_capa_status_history_au_audit` AFTER UPDATE ON `capa_status_history` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'capa_status_history', 'CapaStatusHistoryPage', NEW.id, NULL,
     NULL, NULL, CONCAT('capa_status_history update'), 'system', 'server', '', 'info');

END;

-- trg_capa_sync
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_capa_sync` BEFORE INSERT ON `capa_cases` FOR EACH ROW BEGIN
  CALL ref_touch('capa_status', NEW.status, NEW.status);
  IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.status_id = LAST_INSERT_ID(); END IF;
END;

-- trg_capa_sync_u
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_capa_sync_u` BEFORE UPDATE ON `capa_cases` FOR EACH ROW BEGIN
  IF (NEW.status <=> OLD.status) = 0 THEN
    CALL ref_touch('capa_status', NEW.status, NEW.status);
    IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.status_id = LAST_INSERT_ID(); END IF;
  END IF;
END;

-- trg_checklist_items_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_checklist_items_ad_audit` AFTER DELETE ON `checklist_items` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'checklist_items', 'ChecklistItemsPage', OLD.id, NULL,
     NULL, NULL, CONCAT('checklist_items delete'), 'system', 'server', '', 'warning');
END;

-- trg_checklist_items_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_checklist_items_ai_audit` AFTER INSERT ON `checklist_items` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'checklist_items', 'ChecklistItemsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('checklist_items insert'), 'system', 'server', '', 'info');
END;

-- trg_checklist_items_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_checklist_items_au_audit` AFTER UPDATE ON `checklist_items` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'checklist_items', 'ChecklistItemsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('checklist_items update'), 'system', 'server', '', 'info');

END;

-- trg_checklist_templates_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_checklist_templates_ad_audit` AFTER DELETE ON `checklist_templates` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'checklist_templates', 'ChecklistTemplatesPage', OLD.id, NULL,
     NULL, NULL, CONCAT('checklist_templates delete', CONCAT('; code=', COALESCE(OLD.code,'')), CONCAT('; name=', COALESCE(OLD.name,''))), 'system', 'server', '', 'warning');
END;

-- trg_checklist_templates_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_checklist_templates_ai_audit` AFTER INSERT ON `checklist_templates` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'checklist_templates', 'ChecklistTemplatesPage', NEW.id, NULL,
     NULL, NULL, CONCAT('checklist_templates insert', CONCAT('; code=', COALESCE(NEW.code,'')), CONCAT('; name=', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');
END;

-- trg_checklist_templates_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_checklist_templates_au_audit` AFTER UPDATE ON `checklist_templates` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'checklist_templates', 'ChecklistTemplatesPage', NEW.id, NULL,
     NULL, NULL, CONCAT('checklist_templates update', CONCAT('; code: ', COALESCE(OLD.code,''), ' → ', COALESCE(NEW.code,'')), CONCAT('; name: ', COALESCE(OLD.name,''), ' → ', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');

  IF COALESCE(OLD.code,'') <> COALESCE(NEW.code,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'checklist_templates', 'ChecklistTemplatesPage', NEW.id, 'code',
       OLD.code, NEW.code, 'checklist_templates update: code changed', 'system', 'server', '', 'info');
  END IF;

  IF COALESCE(OLD.name,'') <> COALESCE(NEW.name,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'checklist_templates', 'ChecklistTemplatesPage', NEW.id, 'name',
       OLD.name, NEW.name, 'checklist_templates update: name changed', 'system', 'server', '', 'info');
  END IF;
END;

-- trg_comments_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_comments_ad_audit` AFTER DELETE ON `comments` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'comments', 'CommentsPage', OLD.id, NULL,
     NULL, NULL, CONCAT('comments delete'), 'system', 'server', '', 'warning');
END;

-- trg_comments_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_comments_ai_audit` AFTER INSERT ON `comments` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'comments', 'CommentsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('comments insert'), 'system', 'server', '', 'info');
END;

-- trg_comments_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_comments_au_audit` AFTER UPDATE ON `comments` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'comments', 'CommentsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('comments update'), 'system', 'server', '', 'info');

END;

-- trg_component_devices_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_component_devices_ad_audit` AFTER DELETE ON `component_devices` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'component_devices', 'ComponentDevicesPage', OLD.id, NULL,
     NULL, NULL, CONCAT('component_devices delete'), 'system', 'server', '', 'warning');
END;

-- trg_component_devices_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_component_devices_ai_audit` AFTER INSERT ON `component_devices` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'component_devices', 'ComponentDevicesPage', NEW.id, NULL,
     NULL, NULL, CONCAT('component_devices insert'), 'system', 'server', '', 'info');
END;

-- trg_component_devices_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_component_devices_au_audit` AFTER UPDATE ON `component_devices` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'component_devices', 'ComponentDevicesPage', NEW.id, NULL,
     NULL, NULL, CONCAT('component_devices update'), 'system', 'server', '', 'info');

END;

-- trg_component_models_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_component_models_ad_audit` AFTER DELETE ON `component_models` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'component_models', 'ComponentModelsPage', OLD.id, NULL,
     NULL, NULL, CONCAT('component_models delete'), 'system', 'server', '', 'warning');
END;

-- trg_component_models_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_component_models_ai_audit` AFTER INSERT ON `component_models` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'component_models', 'ComponentModelsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('component_models insert'), 'system', 'server', '', 'info');
END;

-- trg_component_models_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_component_models_au_audit` AFTER UPDATE ON `component_models` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'component_models', 'ComponentModelsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('component_models update'), 'system', 'server', '', 'info');

END;

-- trg_component_parts_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_component_parts_ad_audit` AFTER DELETE ON `component_parts` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'component_parts', 'ComponentPartsPage', OLD.id, NULL,
     NULL, NULL, CONCAT('component_parts delete'), 'system', 'server', '', 'warning');
END;

-- trg_component_parts_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_component_parts_ai_audit` AFTER INSERT ON `component_parts` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'component_parts', 'ComponentPartsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('component_parts insert'), 'system', 'server', '', 'info');
END;

-- trg_component_parts_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_component_parts_au_audit` AFTER UPDATE ON `component_parts` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'component_parts', 'ComponentPartsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('component_parts update'), 'system', 'server', '', 'info');

END;

-- trg_component_qualifications_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_component_qualifications_ad_audit` AFTER DELETE ON `component_qualifications` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'component_qualifications', 'ComponentQualificationsPage', OLD.id, NULL,
     NULL, NULL, CONCAT('component_qualifications delete'), 'system', 'server', '', 'warning');
END;

-- trg_component_qualifications_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_component_qualifications_ai_audit` AFTER INSERT ON `component_qualifications` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'component_qualifications', 'ComponentQualificationsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('component_qualifications insert'), 'system', 'server', '', 'info');
END;

-- trg_component_qualifications_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_component_qualifications_au_audit` AFTER UPDATE ON `component_qualifications` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'component_qualifications', 'ComponentQualificationsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('component_qualifications update'), 'system', 'server', '', 'info');

END;

-- trg_component_types_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_component_types_ad_audit` AFTER DELETE ON `component_types` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'component_types', 'ComponentTypesPage', OLD.id, NULL,
     NULL, NULL, CONCAT('component_types delete', CONCAT('; code=', COALESCE(OLD.code,'')), CONCAT('; name=', COALESCE(OLD.name,''))), 'system', 'server', '', 'warning');
END;

-- trg_component_types_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_component_types_ai_audit` AFTER INSERT ON `component_types` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'component_types', 'ComponentTypesPage', NEW.id, NULL,
     NULL, NULL, CONCAT('component_types insert', CONCAT('; code=', COALESCE(NEW.code,'')), CONCAT('; name=', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');
END;

-- trg_component_types_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_component_types_au_audit` AFTER UPDATE ON `component_types` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'component_types', 'ComponentTypesPage', NEW.id, NULL,
     NULL, NULL, CONCAT('component_types update', CONCAT('; code: ', COALESCE(OLD.code,''), ' → ', COALESCE(NEW.code,'')), CONCAT('; name: ', COALESCE(OLD.name,''), ' → ', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');

  IF COALESCE(OLD.code,'') <> COALESCE(NEW.code,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'component_types', 'ComponentTypesPage', NEW.id, 'code',
       OLD.code, NEW.code, 'component_types update: code changed', 'system', 'server', '', 'info');
  END IF;

  IF COALESCE(OLD.name,'') <> COALESCE(NEW.name,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'component_types', 'ComponentTypesPage', NEW.id, 'name',
       OLD.name, NEW.name, 'component_types update: name changed', 'system', 'server', '', 'info');
  END IF;
END;

-- trg_config_change_log_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_config_change_log_ad_audit` AFTER DELETE ON `config_change_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'config_change_log', 'ConfigChangeLogPage', OLD.id, NULL,
     NULL, NULL, CONCAT('config_change_log delete'), 'system', 'server', '', 'warning');
END;

-- trg_config_change_log_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_config_change_log_ai_audit` AFTER INSERT ON `config_change_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'config_change_log', 'ConfigChangeLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('config_change_log insert'), 'system', 'server', '', 'info');
END;

-- trg_config_change_log_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_config_change_log_au_audit` AFTER UPDATE ON `config_change_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'config_change_log', 'ConfigChangeLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('config_change_log update'), 'system', 'server', '', 'info');

END;

-- trg_contractor_interventions_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_contractor_interventions_ad_audit` AFTER DELETE ON `contractor_interventions` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'contractor_interventions', 'ContractorInterventionsPage', OLD.id, NULL,
     NULL, NULL, CONCAT('contractor_interventions delete'), 'system', 'server', '', 'warning');
END;

-- trg_contractor_interventions_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_contractor_interventions_ai_audit` AFTER INSERT ON `contractor_interventions` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'contractor_interventions', 'ContractorInterventionsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('contractor_interventions insert'), 'system', 'server', '', 'info');
END;

-- trg_contractor_interventions_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_contractor_interventions_au_audit` AFTER UPDATE ON `contractor_interventions` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'contractor_interventions', 'ContractorInterventionsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('contractor_interventions update'), 'system', 'server', '', 'info');

END;

-- trg_contractor_intervention_audit_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_contractor_intervention_audit_ad_audit` AFTER DELETE ON `contractor_intervention_audit` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'contractor_intervention_audit', 'ContractorInterventionAuditPage', OLD.id, NULL,
     NULL, NULL, CONCAT('contractor_intervention_audit delete'), 'system', 'server', '', 'warning');
END;

-- trg_contractor_intervention_audit_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_contractor_intervention_audit_ai_audit` AFTER INSERT ON `contractor_intervention_audit` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'contractor_intervention_audit', 'ContractorInterventionAuditPage', NEW.id, NULL,
     NULL, NULL, CONCAT('contractor_intervention_audit insert'), 'system', 'server', '', 'info');
END;

-- trg_contractor_intervention_audit_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_contractor_intervention_audit_au_audit` AFTER UPDATE ON `contractor_intervention_audit` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'contractor_intervention_audit', 'ContractorInterventionAuditPage', NEW.id, NULL,
     NULL, NULL, CONCAT('contractor_intervention_audit update'), 'system', 'server', '', 'info');

END;

-- trg_cs_sync
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_cs_sync` BEFORE INSERT ON `calibration_sensors` FOR EACH ROW BEGIN
  CALL ref_touch('sensor_type', NEW.sensor_type, NEW.sensor_type);
  IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.sensor_type_id = LAST_INSERT_ID(); END IF;
END;

-- trg_cs_sync_u
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_cs_sync_u` BEFORE UPDATE ON `calibration_sensors` FOR EACH ROW BEGIN
  IF (NEW.sensor_type <=> OLD.sensor_type) = 0 THEN
    CALL ref_touch('sensor_type', NEW.sensor_type, NEW.sensor_type);
    IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.sensor_type_id = LAST_INSERT_ID(); END IF;
  END IF;
END;

-- trg_dashboards_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_dashboards_ad_audit` AFTER DELETE ON `dashboards` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'dashboards', 'DashboardsPage', OLD.id, NULL,
     NULL, NULL, CONCAT('dashboards delete'), 'system', 'server', '', 'warning');
END;

-- trg_dashboards_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_dashboards_ai_audit` AFTER INSERT ON `dashboards` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'dashboards', 'DashboardsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('dashboards insert'), 'system', 'server', '', 'info');
END;

-- trg_dashboards_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_dashboards_au_audit` AFTER UPDATE ON `dashboards` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'dashboards', 'DashboardsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('dashboards update'), 'system', 'server', '', 'info');

END;

-- trg_delegated_permissions_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_delegated_permissions_ad_audit` AFTER DELETE ON `delegated_permissions` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'delegated_permissions', 'DelegatedPermissionsPage', OLD.id, NULL,
     NULL, NULL, CONCAT('delegated_permissions delete'), 'system', 'server', '', 'warning');
END;

-- trg_delegated_permissions_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_delegated_permissions_ai_audit` AFTER INSERT ON `delegated_permissions` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'delegated_permissions', 'DelegatedPermissionsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('delegated_permissions insert'), 'system', 'server', '', 'info');
END;

-- trg_delegated_permissions_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_delegated_permissions_au_audit` AFTER UPDATE ON `delegated_permissions` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'delegated_permissions', 'DelegatedPermissionsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('delegated_permissions update'), 'system', 'server', '', 'info');

END;

-- trg_delete_log_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_delete_log_ad_audit` AFTER DELETE ON `delete_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'delete_log', 'DeleteLogPage', OLD.id, NULL,
     NULL, NULL, CONCAT('delete_log delete'), 'system', 'server', '', 'warning');
END;

-- trg_delete_log_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_delete_log_ai_audit` AFTER INSERT ON `delete_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'delete_log', 'DeleteLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('delete_log insert'), 'system', 'server', '', 'info');
END;

-- trg_delete_log_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_delete_log_au_audit` AFTER UPDATE ON `delete_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'delete_log', 'DeleteLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('delete_log update'), 'system', 'server', '', 'info');

END;

-- trg_departments_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_departments_ad_audit` AFTER DELETE ON `departments` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'departments', 'DepartmentsPage', OLD.id, NULL,
     NULL, NULL, CONCAT('departments delete', CONCAT('; code=', COALESCE(OLD.code,'')), CONCAT('; name=', COALESCE(OLD.name,''))), 'system', 'server', '', 'warning');
END;

-- trg_departments_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_departments_ai_audit` AFTER INSERT ON `departments` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'departments', 'DepartmentsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('departments insert', CONCAT('; code=', COALESCE(NEW.code,'')), CONCAT('; name=', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');
END;

-- trg_departments_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_departments_au_audit` AFTER UPDATE ON `departments` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'departments', 'DepartmentsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('departments update', CONCAT('; code: ', COALESCE(OLD.code,''), ' → ', COALESCE(NEW.code,'')), CONCAT('; name: ', COALESCE(OLD.name,''), ' → ', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');

  IF COALESCE(OLD.code,'') <> COALESCE(NEW.code,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'departments', 'DepartmentsPage', NEW.id, 'code',
       OLD.code, NEW.code, 'departments update: code changed', 'system', 'server', '', 'info');
  END IF;

  IF COALESCE(OLD.name,'') <> COALESCE(NEW.name,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'departments', 'DepartmentsPage', NEW.id, 'name',
       OLD.name, NEW.name, 'departments update: name changed', 'system', 'server', '', 'info');
  END IF;
END;

-- trg_deviations_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_deviations_ad_audit` AFTER DELETE ON `deviations` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'deviations', 'DeviationsPage', OLD.id, NULL,
     NULL, NULL, CONCAT('deviations delete'), 'system', 'server', '', 'warning');
END;

-- trg_deviations_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_deviations_ai_audit` AFTER INSERT ON `deviations` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'deviations', 'DeviationsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('deviations insert'), 'system', 'server', '', 'info');
END;

-- trg_deviations_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_deviations_au_audit` AFTER UPDATE ON `deviations` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'deviations', 'DeviationsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('deviations update'), 'system', 'server', '', 'info');

END;

-- trg_deviation_audit_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_deviation_audit_ad_audit` AFTER DELETE ON `deviation_audit` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'deviation_audit', 'DeviationAuditPage', OLD.id, NULL,
     NULL, NULL, CONCAT('deviation_audit delete'), 'system', 'server', '', 'warning');
END;

-- trg_deviation_audit_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_deviation_audit_ai_audit` AFTER INSERT ON `deviation_audit` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'deviation_audit', 'DeviationAuditPage', NEW.id, NULL,
     NULL, NULL, CONCAT('deviation_audit insert'), 'system', 'server', '', 'info');
END;

-- trg_deviation_audit_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_deviation_audit_au_audit` AFTER UPDATE ON `deviation_audit` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'deviation_audit', 'DeviationAuditPage', NEW.id, NULL,
     NULL, NULL, CONCAT('deviation_audit update'), 'system', 'server', '', 'info');

END;

-- trg_digital_signatures_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_digital_signatures_ad_audit` AFTER DELETE ON `digital_signatures` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'digital_signatures', 'DigitalSignaturesPage', OLD.id, NULL,
     NULL, NULL, CONCAT('digital_signatures delete'), 'system', 'server', '', 'warning');
END;

-- trg_digital_signatures_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_digital_signatures_ai_audit` AFTER INSERT ON `digital_signatures` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'digital_signatures', 'DigitalSignaturesPage', NEW.id, NULL,
     NULL, NULL, CONCAT('digital_signatures insert'), 'system', 'server', '', 'info');
END;

-- trg_digital_signatures_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_digital_signatures_au_audit` AFTER UPDATE ON `digital_signatures` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'digital_signatures', 'DigitalSignaturesPage', NEW.id, NULL,
     NULL, NULL, CONCAT('digital_signatures update'), 'system', 'server', '', 'info');

END;

-- trg_document_versions_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_document_versions_ad_audit` AFTER DELETE ON `document_versions` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'document_versions', 'DocumentVersionsPage', OLD.id, NULL,
     NULL, NULL, CONCAT('document_versions delete'), 'system', 'server', '', 'warning');
END;

-- trg_document_versions_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_document_versions_ai_audit` AFTER INSERT ON `document_versions` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'document_versions', 'DocumentVersionsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('document_versions insert'), 'system', 'server', '', 'info');
END;

-- trg_document_versions_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_document_versions_au_audit` AFTER UPDATE ON `document_versions` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'document_versions', 'DocumentVersionsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('document_versions update'), 'system', 'server', '', 'info');

END;

-- trg_entity_audit_log_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_entity_audit_log_ad_audit` AFTER DELETE ON `entity_audit_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'entity_audit_log', 'EntityAuditLogPage', OLD.id, NULL,
     NULL, NULL, CONCAT('entity_audit_log delete'), 'system', 'server', '', 'warning');
END;

-- trg_entity_audit_log_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_entity_audit_log_ai_audit` AFTER INSERT ON `entity_audit_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'entity_audit_log', 'EntityAuditLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('entity_audit_log insert'), 'system', 'server', '', 'info');
END;

-- trg_entity_audit_log_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_entity_audit_log_au_audit` AFTER UPDATE ON `entity_audit_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'entity_audit_log', 'EntityAuditLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('entity_audit_log update'), 'system', 'server', '', 'info');

END;

-- trg_entity_tags_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_entity_tags_ad_audit` AFTER DELETE ON `entity_tags` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'entity_tags', 'EntityTagsPage', OLD.id, NULL,
     NULL, NULL, CONCAT('entity_tags delete'), 'system', 'server', '', 'warning');
END;

-- trg_entity_tags_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_entity_tags_ai_audit` AFTER INSERT ON `entity_tags` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'entity_tags', 'EntityTagsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('entity_tags insert'), 'system', 'server', '', 'info');
END;

-- trg_entity_tags_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_entity_tags_au_audit` AFTER UPDATE ON `entity_tags` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'entity_tags', 'EntityTagsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('entity_tags update'), 'system', 'server', '', 'info');

END;

-- trg_export_print_log_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_export_print_log_ad_audit` AFTER DELETE ON `export_print_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'export_print_log', 'ExportPrintLogPage', OLD.id, NULL,
     NULL, NULL, CONCAT('export_print_log delete'), 'system', 'server', '', 'warning');
END;

-- trg_export_print_log_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_export_print_log_ai_audit` AFTER INSERT ON `export_print_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'export_print_log', 'ExportPrintLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('export_print_log insert'), 'system', 'server', '', 'info');
END;

-- trg_export_print_log_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_export_print_log_au_audit` AFTER UPDATE ON `export_print_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'export_print_log', 'ExportPrintLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('export_print_log update'), 'system', 'server', '', 'info');

END;

-- trg_external_contractors_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_external_contractors_ad_audit` AFTER DELETE ON `external_contractors` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'external_contractors', 'ExternalContractorsPage', OLD.id, NULL,
     NULL, NULL, CONCAT('external_contractors delete', CONCAT('; name=', COALESCE(OLD.name,''))), 'system', 'server', '', 'warning');
END;

-- trg_external_contractors_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_external_contractors_ai_audit` AFTER INSERT ON `external_contractors` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'external_contractors', 'ExternalContractorsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('external_contractors insert', CONCAT('; name=', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');
END;

-- trg_external_contractors_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_external_contractors_au_audit` AFTER UPDATE ON `external_contractors` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'external_contractors', 'ExternalContractorsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('external_contractors update', CONCAT('; name: ', COALESCE(OLD.name,''), ' → ', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');

  IF COALESCE(OLD.name,'') <> COALESCE(NEW.name,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'external_contractors', 'ExternalContractorsPage', NEW.id, 'name',
       OLD.name, NEW.name, 'external_contractors update: name changed', 'system', 'server', '', 'info');
  END IF;
END;

-- trg_failure_modes_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_failure_modes_ad_audit` AFTER DELETE ON `failure_modes` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'failure_modes', 'FailureModesPage', OLD.id, NULL,
     NULL, NULL, CONCAT('failure_modes delete', CONCAT('; code=', COALESCE(OLD.code,''))), 'system', 'server', '', 'warning');
END;

-- trg_failure_modes_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_failure_modes_ai_audit` AFTER INSERT ON `failure_modes` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'failure_modes', 'FailureModesPage', NEW.id, NULL,
     NULL, NULL, CONCAT('failure_modes insert', CONCAT('; code=', COALESCE(NEW.code,''))), 'system', 'server', '', 'info');
END;

-- trg_failure_modes_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_failure_modes_au_audit` AFTER UPDATE ON `failure_modes` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'failure_modes', 'FailureModesPage', NEW.id, NULL,
     NULL, NULL, CONCAT('failure_modes update', CONCAT('; code: ', COALESCE(OLD.code,''), ' → ', COALESCE(NEW.code,''))), 'system', 'server', '', 'info');

  IF COALESCE(OLD.code,'') <> COALESCE(NEW.code,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'failure_modes', 'FailureModesPage', NEW.id, 'code',
       OLD.code, NEW.code, 'failure_modes update: code changed', 'system', 'server', '', 'info');
  END IF;
END;

-- trg_forensic_user_change_log_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_forensic_user_change_log_ad_audit` AFTER DELETE ON `forensic_user_change_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'forensic_user_change_log', 'ForensicUserChangeLogPage', OLD.id, NULL,
     NULL, NULL, CONCAT('forensic_user_change_log delete'), 'system', 'server', '', 'warning');
END;

-- trg_forensic_user_change_log_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_forensic_user_change_log_ai_audit` AFTER INSERT ON `forensic_user_change_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'forensic_user_change_log', 'ForensicUserChangeLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('forensic_user_change_log insert'), 'system', 'server', '', 'info');
END;

-- trg_forensic_user_change_log_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_forensic_user_change_log_au_audit` AFTER UPDATE ON `forensic_user_change_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'forensic_user_change_log', 'ForensicUserChangeLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('forensic_user_change_log update'), 'system', 'server', '', 'info');

END;

-- trg_incident_log_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_incident_log_ad_audit` AFTER DELETE ON `incident_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'incident_log', 'IncidentLogPage', OLD.id, NULL,
     NULL, NULL, CONCAT('incident_log delete'), 'system', 'server', '', 'warning');
END;

-- trg_incident_log_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_incident_log_ai_audit` AFTER INSERT ON `incident_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'incident_log', 'IncidentLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('incident_log insert'), 'system', 'server', '', 'info');
END;

-- trg_incident_log_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_incident_log_au_audit` AFTER UPDATE ON `incident_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'incident_log', 'IncidentLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('incident_log update'), 'system', 'server', '', 'info');

END;

-- trg_inc_sync
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_inc_sync` BEFORE INSERT ON `incident_log` FOR EACH ROW BEGIN
  CALL ref_touch('severity', NEW.severity, NEW.severity);
  IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.severity_id = LAST_INSERT_ID(); END IF;
END;

-- trg_inc_sync_u
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_inc_sync_u` BEFORE UPDATE ON `incident_log` FOR EACH ROW BEGIN
  IF (NEW.severity <=> OLD.severity) = 0 THEN
    CALL ref_touch('severity', NEW.severity, NEW.severity);
    IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.severity_id = LAST_INSERT_ID(); END IF;
  END IF;
END;

-- trg_inspections_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_inspections_ad_audit` AFTER DELETE ON `inspections` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'inspections', 'InspectionsPage', OLD.id, NULL,
     NULL, NULL, CONCAT('inspections delete'), 'system', 'server', '', 'warning');
END;

-- trg_inspections_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_inspections_ai_audit` AFTER INSERT ON `inspections` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'inspections', 'InspectionsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('inspections insert'), 'system', 'server', '', 'info');
END;

-- trg_inspections_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_inspections_au_audit` AFTER UPDATE ON `inspections` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'inspections', 'InspectionsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('inspections update'), 'system', 'server', '', 'info');

END;

-- trg_insp_sync
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_insp_sync` BEFORE INSERT ON `inspections` FOR EACH ROW BEGIN
  CALL ref_touch('inspection_type', NEW.type, NEW.type);
  IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.type_id = LAST_INSERT_ID(); END IF;
  CALL ref_touch('inspection_result', NEW.result, NEW.result);
  IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.result_id = LAST_INSERT_ID(); END IF;
END;

-- trg_insp_sync_u
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_insp_sync_u` BEFORE UPDATE ON `inspections` FOR EACH ROW BEGIN
  IF (NEW.type <=> OLD.type) = 0 THEN
    CALL ref_touch('inspection_type', NEW.type, NEW.type);
    IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.type_id = LAST_INSERT_ID(); END IF;
  END IF;
  IF (NEW.result <=> OLD.result) = 0 THEN
    CALL ref_touch('inspection_result', NEW.result, NEW.result);
    IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.result_id = LAST_INSERT_ID(); END IF;
  END IF;
END;

-- trg_integration_log_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_integration_log_ad_audit` AFTER DELETE ON `integration_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'integration_log', 'IntegrationLogPage', OLD.id, NULL,
     NULL, NULL, CONCAT('integration_log delete'), 'system', 'server', '', 'warning');
END;

-- trg_integration_log_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_integration_log_ai_audit` AFTER INSERT ON `integration_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'integration_log', 'IntegrationLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('integration_log insert'), 'system', 'server', '', 'info');
END;

-- trg_integration_log_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_integration_log_au_audit` AFTER UPDATE ON `integration_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'integration_log', 'IntegrationLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('integration_log update'), 'system', 'server', '', 'info');

END;

-- trg_inventory_transactions_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_inventory_transactions_ad_audit` AFTER DELETE ON `inventory_transactions` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'inventory_transactions', 'InventoryTransactionsPage', OLD.id, NULL,
     NULL, NULL, CONCAT('inventory_transactions delete'), 'system', 'server', '', 'warning');
END;

-- trg_inventory_transactions_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_inventory_transactions_ai_audit` AFTER INSERT ON `inventory_transactions` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'inventory_transactions', 'InventoryTransactionsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('inventory_transactions insert'), 'system', 'server', '', 'info');
END;

-- trg_inventory_transactions_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_inventory_transactions_au_audit` AFTER UPDATE ON `inventory_transactions` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'inventory_transactions', 'InventoryTransactionsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('inventory_transactions update'), 'system', 'server', '', 'info');

END;

-- trg_iot_anomaly_log_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_iot_anomaly_log_ad_audit` AFTER DELETE ON `iot_anomaly_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'iot_anomaly_log', 'IotAnomalyLogPage', OLD.id, NULL,
     NULL, NULL, CONCAT('iot_anomaly_log delete'), 'system', 'server', '', 'warning');
END;

-- trg_iot_anomaly_log_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_iot_anomaly_log_ai_audit` AFTER INSERT ON `iot_anomaly_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'iot_anomaly_log', 'IotAnomalyLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('iot_anomaly_log insert'), 'system', 'server', '', 'info');
END;

-- trg_iot_anomaly_log_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_iot_anomaly_log_au_audit` AFTER UPDATE ON `iot_anomaly_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'iot_anomaly_log', 'IotAnomalyLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('iot_anomaly_log update'), 'system', 'server', '', 'info');

END;

-- trg_iot_devices_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_iot_devices_ad_audit` AFTER DELETE ON `iot_devices` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'iot_devices', 'IotDevicesPage', OLD.id, NULL,
     NULL, NULL, CONCAT('iot_devices delete'), 'system', 'server', '', 'warning');
END;

-- trg_iot_devices_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_iot_devices_ai_audit` AFTER INSERT ON `iot_devices` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'iot_devices', 'IotDevicesPage', NEW.id, NULL,
     NULL, NULL, CONCAT('iot_devices insert'), 'system', 'server', '', 'info');
END;

-- trg_iot_devices_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_iot_devices_au_audit` AFTER UPDATE ON `iot_devices` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'iot_devices', 'IotDevicesPage', NEW.id, NULL,
     NULL, NULL, CONCAT('iot_devices update'), 'system', 'server', '', 'info');

END;

-- trg_iot_event_audit_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_iot_event_audit_ad_audit` AFTER DELETE ON `iot_event_audit` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'iot_event_audit', 'IotEventAuditPage', OLD.id, NULL,
     NULL, NULL, CONCAT('iot_event_audit delete'), 'system', 'server', '', 'warning');
END;

-- trg_iot_event_audit_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_iot_event_audit_ai_audit` AFTER INSERT ON `iot_event_audit` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'iot_event_audit', 'IotEventAuditPage', NEW.id, NULL,
     NULL, NULL, CONCAT('iot_event_audit insert'), 'system', 'server', '', 'info');
END;

-- trg_iot_event_audit_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_iot_event_audit_au_audit` AFTER UPDATE ON `iot_event_audit` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'iot_event_audit', 'IotEventAuditPage', NEW.id, NULL,
     NULL, NULL, CONCAT('iot_event_audit update'), 'system', 'server', '', 'info');

END;

-- trg_iot_gateways_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_iot_gateways_ad_audit` AFTER DELETE ON `iot_gateways` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'iot_gateways', 'IotGatewaysPage', OLD.id, NULL,
     NULL, NULL, CONCAT('iot_gateways delete', CONCAT('; code=', COALESCE(OLD.code,'')), CONCAT('; name=', COALESCE(OLD.name,''))), 'system', 'server', '', 'warning');
END;

-- trg_iot_gateways_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_iot_gateways_ai_audit` AFTER INSERT ON `iot_gateways` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'iot_gateways', 'IotGatewaysPage', NEW.id, NULL,
     NULL, NULL, CONCAT('iot_gateways insert', CONCAT('; code=', COALESCE(NEW.code,'')), CONCAT('; name=', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');
END;

-- trg_iot_gateways_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_iot_gateways_au_audit` AFTER UPDATE ON `iot_gateways` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'iot_gateways', 'IotGatewaysPage', NEW.id, NULL,
     NULL, NULL, CONCAT('iot_gateways update', CONCAT('; code: ', COALESCE(OLD.code,''), ' → ', COALESCE(NEW.code,'')), CONCAT('; name: ', COALESCE(OLD.name,''), ' → ', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');

  IF COALESCE(OLD.code,'') <> COALESCE(NEW.code,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'iot_gateways', 'IotGatewaysPage', NEW.id, 'code',
       OLD.code, NEW.code, 'iot_gateways update: code changed', 'system', 'server', '', 'info');
  END IF;

  IF COALESCE(OLD.name,'') <> COALESCE(NEW.name,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'iot_gateways', 'IotGatewaysPage', NEW.id, 'name',
       OLD.name, NEW.name, 'iot_gateways update: name changed', 'system', 'server', '', 'info');
  END IF;
END;

-- trg_iot_sensor_data_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_iot_sensor_data_ad_audit` AFTER DELETE ON `iot_sensor_data` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'iot_sensor_data', 'IotSensorDataPage', OLD.id, NULL,
     NULL, NULL, CONCAT('iot_sensor_data delete'), 'system', 'server', '', 'warning');
END;

-- trg_iot_sensor_data_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_iot_sensor_data_ai_audit` AFTER INSERT ON `iot_sensor_data` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'iot_sensor_data', 'IotSensorDataPage', NEW.id, NULL,
     NULL, NULL, CONCAT('iot_sensor_data insert'), 'system', 'server', '', 'info');
END;

-- trg_iot_sensor_data_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_iot_sensor_data_au_audit` AFTER UPDATE ON `iot_sensor_data` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'iot_sensor_data', 'IotSensorDataPage', NEW.id, NULL,
     NULL, NULL, CONCAT('iot_sensor_data update'), 'system', 'server', '', 'info');

END;

-- trg_irregularities_log_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_irregularities_log_ad_audit` AFTER DELETE ON `irregularities_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'irregularities_log', 'IrregularitiesLogPage', OLD.id, NULL,
     NULL, NULL, CONCAT('irregularities_log delete'), 'system', 'server', '', 'warning');
END;

-- trg_irregularities_log_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_irregularities_log_ai_audit` AFTER INSERT ON `irregularities_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'irregularities_log', 'IrregularitiesLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('irregularities_log insert'), 'system', 'server', '', 'info');
END;

-- trg_irregularities_log_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_irregularities_log_au_audit` AFTER UPDATE ON `irregularities_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'irregularities_log', 'IrregularitiesLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('irregularities_log update'), 'system', 'server', '', 'info');

END;

-- trg_job_titles_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_job_titles_ad_audit` AFTER DELETE ON `job_titles` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'job_titles', 'JobTitlesPage', OLD.id, NULL,
     NULL, NULL, CONCAT('job_titles delete'), 'system', 'server', '', 'warning');
END;

-- trg_job_titles_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_job_titles_ai_audit` AFTER INSERT ON `job_titles` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'job_titles', 'JobTitlesPage', NEW.id, NULL,
     NULL, NULL, CONCAT('job_titles insert'), 'system', 'server', '', 'info');
END;

-- trg_job_titles_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_job_titles_au_audit` AFTER UPDATE ON `job_titles` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'job_titles', 'JobTitlesPage', NEW.id, NULL,
     NULL, NULL, CONCAT('job_titles update'), 'system', 'server', '', 'info');

END;

-- trg_locations_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_locations_ad_audit` AFTER DELETE ON `locations` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'locations', 'LocationsPage', OLD.id, NULL,
     NULL, NULL, CONCAT('locations delete', CONCAT('; code=', COALESCE(OLD.code,'')), CONCAT('; name=', COALESCE(OLD.name,''))), 'system', 'server', '', 'warning');
END;

-- trg_locations_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_locations_ai_audit` AFTER INSERT ON `locations` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'locations', 'LocationsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('locations insert', CONCAT('; code=', COALESCE(NEW.code,'')), CONCAT('; name=', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');
END;

-- trg_locations_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_locations_au_audit` AFTER UPDATE ON `locations` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'locations', 'LocationsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('locations update', CONCAT('; code: ', COALESCE(OLD.code,''), ' → ', COALESCE(NEW.code,'')), CONCAT('; name: ', COALESCE(OLD.name,''), ' → ', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');

  IF COALESCE(OLD.code,'') <> COALESCE(NEW.code,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'locations', 'LocationsPage', NEW.id, 'code',
       OLD.code, NEW.code, 'locations update: code changed', 'system', 'server', '', 'info');
  END IF;

  IF COALESCE(OLD.name,'') <> COALESCE(NEW.name,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'locations', 'LocationsPage', NEW.id, 'name',
       OLD.name, NEW.name, 'locations update: name changed', 'system', 'server', '', 'info');
  END IF;
END;

-- trg_lookup_domain_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_lookup_domain_ad_audit` AFTER DELETE ON `lookup_domain` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'lookup_domain', 'LookupDomainPage', OLD.id, NULL,
     NULL, NULL, CONCAT('lookup_domain delete'), 'system', 'server', '', 'warning');
END;

-- trg_lookup_domain_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_lookup_domain_ai_audit` AFTER INSERT ON `lookup_domain` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'lookup_domain', 'LookupDomainPage', NEW.id, NULL,
     NULL, NULL, CONCAT('lookup_domain insert'), 'system', 'server', '', 'info');
END;

-- trg_lookup_domain_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_lookup_domain_au_audit` AFTER UPDATE ON `lookup_domain` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'lookup_domain', 'LookupDomainPage', NEW.id, NULL,
     NULL, NULL, CONCAT('lookup_domain update'), 'system', 'server', '', 'info');

END;

-- trg_lookup_value_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_lookup_value_ad_audit` AFTER DELETE ON `lookup_value` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'lookup_value', 'LookupValuePage', OLD.id, NULL,
     NULL, NULL, CONCAT('lookup_value delete'), 'system', 'server', '', 'warning');
END;

-- trg_lookup_value_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_lookup_value_ai_audit` AFTER INSERT ON `lookup_value` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'lookup_value', 'LookupValuePage', NEW.id, NULL,
     NULL, NULL, CONCAT('lookup_value insert'), 'system', 'server', '', 'info');
END;

-- trg_lookup_value_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_lookup_value_au_audit` AFTER UPDATE ON `lookup_value` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'lookup_value', 'LookupValuePage', NEW.id, NULL,
     NULL, NULL, CONCAT('lookup_value update'), 'system', 'server', '', 'info');

END;

-- trg_machines_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_machines_ad_audit` AFTER DELETE ON `machines` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'machines', 'MachinesPage', OLD.id, NULL,
     NULL, NULL, CONCAT('machines delete', CONCAT('; code=', COALESCE(OLD.code,'')), CONCAT('; name=', COALESCE(OLD.name,''))), 'system', 'server', '', 'warning');
END;

-- trg_machines_ai
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_machines_ai` AFTER INSERT ON `machines` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id,
     field_name, old_value, new_value, description, source_ip,
     device_info, session_id, severity)
  VALUES
    (NULL, 'MCH_CREATE_DBTRG', 'machines', 'MachineModule', NEW.id,
     NULL, NULL, NULL,
     CONCAT('Machine created (db trigger). code=', COALESCE(NEW.code,'')),
     'db-trigger', 'server', '', 'audit');
END;

-- trg_machines_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_machines_ai_audit` AFTER INSERT ON `machines` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'machines', 'MachinesPage', NEW.id, NULL,
     NULL, NULL, CONCAT('machines insert', CONCAT('; code=', COALESCE(NEW.code,'')), CONCAT('; name=', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');
END;

-- trg_machines_au
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_machines_au` AFTER UPDATE ON `machines` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id,
     field_name, old_value, new_value, description, source_ip,
     device_info, session_id, severity)
  VALUES
    (NULL, 'MCH_UPDATE_DBTRG', 'machines', 'MachineModule', NEW.id,
     NULL, NULL, NULL,
     CONCAT('Machine updated (db trigger). code=', COALESCE(NEW.code,'')),
     'db-trigger', 'server', '', 'audit');
END;

-- trg_machines_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_machines_au_audit` AFTER UPDATE ON `machines` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'machines', 'MachinesPage', NEW.id, NULL,
     NULL, NULL, CONCAT('machines update', CONCAT('; code: ', COALESCE(OLD.code,''), ' → ', COALESCE(NEW.code,'')), CONCAT('; name: ', COALESCE(OLD.name,''), ' → ', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');

  IF COALESCE(OLD.code,'') <> COALESCE(NEW.code,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'machines', 'MachinesPage', NEW.id, 'code',
       OLD.code, NEW.code, 'machines update: code changed', 'system', 'server', '', 'info');
  END IF;

  IF COALESCE(OLD.name,'') <> COALESCE(NEW.name,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'machines', 'MachinesPage', NEW.id, 'name',
       OLD.name, NEW.name, 'machines update: name changed', 'system', 'server', '', 'info');
  END IF;
END;

-- trg_machines_bi
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_machines_bi` BEFORE INSERT ON `machines` FOR EACH ROW BEGIN
  SET NEW.status = fn_normalize_machine_status(NEW.status);
  -- If your schema has last_modified (it does in the app), set it if caller didn't provide it
  SET NEW.last_modified = COALESCE(NEW.last_modified, NOW());
END;

-- trg_machines_bu
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_machines_bu` BEFORE UPDATE ON `machines` FOR EACH ROW BEGIN
  SET NEW.status = fn_normalize_machine_status(NEW.status);
  SET NEW.last_modified = NOW();
END;

-- trg_machine_components_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_machine_components_ad_audit` AFTER DELETE ON `machine_components` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'machine_components', 'MachineComponentsPage', OLD.id, NULL,
     NULL, NULL, CONCAT('machine_components delete', CONCAT('; code=', COALESCE(OLD.code,'')), CONCAT('; name=', COALESCE(OLD.name,''))), 'system', 'server', '', 'warning');
END;

-- trg_machine_components_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_machine_components_ai_audit` AFTER INSERT ON `machine_components` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'machine_components', 'MachineComponentsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('machine_components insert', CONCAT('; code=', COALESCE(NEW.code,'')), CONCAT('; name=', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');
END;

-- trg_machine_components_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_machine_components_au_audit` AFTER UPDATE ON `machine_components` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'machine_components', 'MachineComponentsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('machine_components update', CONCAT('; code: ', COALESCE(OLD.code,''), ' → ', COALESCE(NEW.code,'')), CONCAT('; name: ', COALESCE(OLD.name,''), ' → ', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');

  IF COALESCE(OLD.code,'') <> COALESCE(NEW.code,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'machine_components', 'MachineComponentsPage', NEW.id, 'code',
       OLD.code, NEW.code, 'machine_components update: code changed', 'system', 'server', '', 'info');
  END IF;

  IF COALESCE(OLD.name,'') <> COALESCE(NEW.name,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'machine_components', 'MachineComponentsPage', NEW.id, 'name',
       OLD.name, NEW.name, 'machine_components update: name changed', 'system', 'server', '', 'info');
  END IF;
END;

-- trg_machine_lifecycle_event_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_machine_lifecycle_event_ad_audit` AFTER DELETE ON `machine_lifecycle_event` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'machine_lifecycle_event', 'MachineLifecycleEventPage', OLD.id, NULL,
     NULL, NULL, CONCAT('machine_lifecycle_event delete'), 'system', 'server', '', 'warning');
END;

-- trg_machine_lifecycle_event_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_machine_lifecycle_event_ai_audit` AFTER INSERT ON `machine_lifecycle_event` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'machine_lifecycle_event', 'MachineLifecycleEventPage', NEW.id, NULL,
     NULL, NULL, CONCAT('machine_lifecycle_event insert'), 'system', 'server', '', 'info');
END;

-- trg_machine_lifecycle_event_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_machine_lifecycle_event_au_audit` AFTER UPDATE ON `machine_lifecycle_event` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'machine_lifecycle_event', 'MachineLifecycleEventPage', NEW.id, NULL,
     NULL, NULL, CONCAT('machine_lifecycle_event update'), 'system', 'server', '', 'info');

END;

-- trg_machine_models_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_machine_models_ad_audit` AFTER DELETE ON `machine_models` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'machine_models', 'MachineModelsPage', OLD.id, NULL,
     NULL, NULL, CONCAT('machine_models delete'), 'system', 'server', '', 'warning');
END;

-- trg_machine_models_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_machine_models_ai_audit` AFTER INSERT ON `machine_models` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'machine_models', 'MachineModelsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('machine_models insert'), 'system', 'server', '', 'info');
END;

-- trg_machine_models_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_machine_models_au_audit` AFTER UPDATE ON `machine_models` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'machine_models', 'MachineModelsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('machine_models update'), 'system', 'server', '', 'info');

END;

-- trg_machine_types_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_machine_types_ad_audit` AFTER DELETE ON `machine_types` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'machine_types', 'MachineTypesPage', OLD.id, NULL,
     NULL, NULL, CONCAT('machine_types delete', CONCAT('; code=', COALESCE(OLD.code,'')), CONCAT('; name=', COALESCE(OLD.name,''))), 'system', 'server', '', 'warning');
END;

-- trg_machine_types_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_machine_types_ai_audit` AFTER INSERT ON `machine_types` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'machine_types', 'MachineTypesPage', NEW.id, NULL,
     NULL, NULL, CONCAT('machine_types insert', CONCAT('; code=', COALESCE(NEW.code,'')), CONCAT('; name=', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');
END;

-- trg_machine_types_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_machine_types_au_audit` AFTER UPDATE ON `machine_types` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'machine_types', 'MachineTypesPage', NEW.id, NULL,
     NULL, NULL, CONCAT('machine_types update', CONCAT('; code: ', COALESCE(OLD.code,''), ' → ', COALESCE(NEW.code,'')), CONCAT('; name: ', COALESCE(OLD.name,''), ' → ', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');

  IF COALESCE(OLD.code,'') <> COALESCE(NEW.code,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'machine_types', 'MachineTypesPage', NEW.id, 'code',
       OLD.code, NEW.code, 'machine_types update: code changed', 'system', 'server', '', 'info');
  END IF;

  IF COALESCE(OLD.name,'') <> COALESCE(NEW.name,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'machine_types', 'MachineTypesPage', NEW.id, 'name',
       OLD.name, NEW.name, 'machine_types update: name changed', 'system', 'server', '', 'info');
  END IF;
END;

-- trg_manufacturers_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_manufacturers_ad_audit` AFTER DELETE ON `manufacturers` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'manufacturers', 'ManufacturersPage', OLD.id, NULL,
     NULL, NULL, CONCAT('manufacturers delete', CONCAT('; name=', COALESCE(OLD.name,''))), 'system', 'server', '', 'warning');
END;

-- trg_manufacturers_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_manufacturers_ai_audit` AFTER INSERT ON `manufacturers` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'manufacturers', 'ManufacturersPage', NEW.id, NULL,
     NULL, NULL, CONCAT('manufacturers insert', CONCAT('; name=', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');
END;

-- trg_manufacturers_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_manufacturers_au_audit` AFTER UPDATE ON `manufacturers` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'manufacturers', 'ManufacturersPage', NEW.id, NULL,
     NULL, NULL, CONCAT('manufacturers update', CONCAT('; name: ', COALESCE(OLD.name,''), ' → ', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');

  IF COALESCE(OLD.name,'') <> COALESCE(NEW.name,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'manufacturers', 'ManufacturersPage', NEW.id, 'name',
       OLD.name, NEW.name, 'manufacturers update: name changed', 'system', 'server', '', 'info');
  END IF;
END;

-- trg_measurement_units_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_measurement_units_ad_audit` AFTER DELETE ON `measurement_units` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'measurement_units', 'MeasurementUnitsPage', OLD.id, NULL,
     NULL, NULL, CONCAT('measurement_units delete', CONCAT('; code=', COALESCE(OLD.code,'')), CONCAT('; name=', COALESCE(OLD.name,''))), 'system', 'server', '', 'warning');
END;

-- trg_measurement_units_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_measurement_units_ai_audit` AFTER INSERT ON `measurement_units` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'measurement_units', 'MeasurementUnitsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('measurement_units insert', CONCAT('; code=', COALESCE(NEW.code,'')), CONCAT('; name=', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');
END;

-- trg_measurement_units_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_measurement_units_au_audit` AFTER UPDATE ON `measurement_units` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'measurement_units', 'MeasurementUnitsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('measurement_units update', CONCAT('; code: ', COALESCE(OLD.code,''), ' → ', COALESCE(NEW.code,'')), CONCAT('; name: ', COALESCE(OLD.name,''), ' → ', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');

  IF COALESCE(OLD.code,'') <> COALESCE(NEW.code,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'measurement_units', 'MeasurementUnitsPage', NEW.id, 'code',
       OLD.code, NEW.code, 'measurement_units update: code changed', 'system', 'server', '', 'info');
  END IF;

  IF COALESCE(OLD.name,'') <> COALESCE(NEW.name,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'measurement_units', 'MeasurementUnitsPage', NEW.id, 'name',
       OLD.name, NEW.name, 'measurement_units update: name changed', 'system', 'server', '', 'info');
  END IF;
END;

-- trg_mle_sync
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_mle_sync` BEFORE INSERT ON `machine_lifecycle_event` FOR EACH ROW BEGIN
  CALL ref_touch('lifecycle_phase', NEW.event_type, NEW.event_type);
  IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.event_type_id = LAST_INSERT_ID(); END IF;
END;

-- trg_mle_sync_u
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_mle_sync_u` BEFORE UPDATE ON `machine_lifecycle_event` FOR EACH ROW BEGIN
  IF (NEW.event_type <=> OLD.event_type) = 0 THEN
    CALL ref_touch('lifecycle_phase', NEW.event_type, NEW.event_type);
    IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.event_type_id = LAST_INSERT_ID(); END IF;
  END IF;
END;

-- trg_mobile_device_log_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_mobile_device_log_ad_audit` AFTER DELETE ON `mobile_device_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'mobile_device_log', 'MobileDeviceLogPage', OLD.id, NULL,
     NULL, NULL, CONCAT('mobile_device_log delete'), 'system', 'server', '', 'warning');
END;

-- trg_mobile_device_log_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_mobile_device_log_ai_audit` AFTER INSERT ON `mobile_device_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'mobile_device_log', 'MobileDeviceLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('mobile_device_log insert'), 'system', 'server', '', 'info');
END;

-- trg_mobile_device_log_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_mobile_device_log_au_audit` AFTER UPDATE ON `mobile_device_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'mobile_device_log', 'MobileDeviceLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('mobile_device_log update'), 'system', 'server', '', 'info');

END;

-- trg_m_sync
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_m_sync` BEFORE INSERT ON `machines` FOR EACH ROW BEGIN
  CALL ref_touch('asset_status', NEW.status, NEW.status);
  IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.status_id = LAST_INSERT_ID(); END IF;
  CALL ref_touch('lifecycle_phase', NEW.lifecycle_phase, NEW.lifecycle_phase);
  IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.lifecycle_phase_id = LAST_INSERT_ID(); END IF;
END;

-- trg_m_sync_u
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_m_sync_u` BEFORE UPDATE ON `machines` FOR EACH ROW BEGIN
  IF (NEW.status <=> OLD.status) = 0 THEN
    CALL ref_touch('asset_status', NEW.status, NEW.status);
    IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.status_id = LAST_INSERT_ID(); END IF;
  END IF;
  IF (NEW.lifecycle_phase <=> OLD.lifecycle_phase) = 0 THEN
    CALL ref_touch('lifecycle_phase', NEW.lifecycle_phase, NEW.lifecycle_phase);
    IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.lifecycle_phase_id = LAST_INSERT_ID(); END IF;
  END IF;
END;

-- trg_notification_queue_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_notification_queue_ad_audit` AFTER DELETE ON `notification_queue` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'notification_queue', 'NotificationQueuePage', OLD.id, NULL,
     NULL, NULL, CONCAT('notification_queue delete'), 'system', 'server', '', 'warning');
END;

-- trg_notification_queue_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_notification_queue_ai_audit` AFTER INSERT ON `notification_queue` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'notification_queue', 'NotificationQueuePage', NEW.id, NULL,
     NULL, NULL, CONCAT('notification_queue insert'), 'system', 'server', '', 'info');
END;

-- trg_notification_queue_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_notification_queue_au_audit` AFTER UPDATE ON `notification_queue` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'notification_queue', 'NotificationQueuePage', NEW.id, NULL,
     NULL, NULL, CONCAT('notification_queue update'), 'system', 'server', '', 'info');

END;

-- trg_notification_templates_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_notification_templates_ad_audit` AFTER DELETE ON `notification_templates` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'notification_templates', 'NotificationTemplatesPage', OLD.id, NULL,
     NULL, NULL, CONCAT('notification_templates delete', CONCAT('; code=', COALESCE(OLD.code,'')), CONCAT('; name=', COALESCE(OLD.name,''))), 'system', 'server', '', 'warning');
END;

-- trg_notification_templates_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_notification_templates_ai_audit` AFTER INSERT ON `notification_templates` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'notification_templates', 'NotificationTemplatesPage', NEW.id, NULL,
     NULL, NULL, CONCAT('notification_templates insert', CONCAT('; code=', COALESCE(NEW.code,'')), CONCAT('; name=', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');
END;

-- trg_notification_templates_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_notification_templates_au_audit` AFTER UPDATE ON `notification_templates` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'notification_templates', 'NotificationTemplatesPage', NEW.id, NULL,
     NULL, NULL, CONCAT('notification_templates update', CONCAT('; code: ', COALESCE(OLD.code,''), ' → ', COALESCE(NEW.code,'')), CONCAT('; name: ', COALESCE(OLD.name,''), ' → ', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');

  IF COALESCE(OLD.code,'') <> COALESCE(NEW.code,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'notification_templates', 'NotificationTemplatesPage', NEW.id, 'code',
       OLD.code, NEW.code, 'notification_templates update: code changed', 'system', 'server', '', 'info');
  END IF;

  IF COALESCE(OLD.name,'') <> COALESCE(NEW.name,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'notification_templates', 'NotificationTemplatesPage', NEW.id, 'name',
       OLD.name, NEW.name, 'notification_templates update: name changed', 'system', 'server', '', 'info');
  END IF;
END;

-- trg_parts_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_parts_ad_audit` AFTER DELETE ON `parts` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'parts', 'PartsPage', OLD.id, NULL,
     NULL, NULL, CONCAT('parts delete', CONCAT('; code=', COALESCE(OLD.code,'')), CONCAT('; name=', COALESCE(OLD.name,''))), 'system', 'server', '', 'warning');
END;

-- trg_parts_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_parts_ai_audit` AFTER INSERT ON `parts` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'parts', 'PartsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('parts insert', CONCAT('; code=', COALESCE(NEW.code,'')), CONCAT('; name=', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');
END;

-- trg_parts_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_parts_au_audit` AFTER UPDATE ON `parts` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'parts', 'PartsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('parts update', CONCAT('; code: ', COALESCE(OLD.code,''), ' → ', COALESCE(NEW.code,'')), CONCAT('; name: ', COALESCE(OLD.name,''), ' → ', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');

  IF COALESCE(OLD.code,'') <> COALESCE(NEW.code,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'parts', 'PartsPage', NEW.id, 'code',
       OLD.code, NEW.code, 'parts update: code changed', 'system', 'server', '', 'info');
  END IF;

  IF COALESCE(OLD.name,'') <> COALESCE(NEW.name,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'parts', 'PartsPage', NEW.id, 'name',
       OLD.name, NEW.name, 'parts update: name changed', 'system', 'server', '', 'info');
  END IF;
END;

-- trg_part_bom_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_part_bom_ad_audit` AFTER DELETE ON `part_bom` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'part_bom', 'PartBomPage', OLD.id, NULL,
     NULL, NULL, CONCAT('part_bom delete'), 'system', 'server', '', 'warning');
END;

-- trg_part_bom_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_part_bom_ai_audit` AFTER INSERT ON `part_bom` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'part_bom', 'PartBomPage', NEW.id, NULL,
     NULL, NULL, CONCAT('part_bom insert'), 'system', 'server', '', 'info');
END;

-- trg_part_bom_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_part_bom_au_audit` AFTER UPDATE ON `part_bom` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'part_bom', 'PartBomPage', NEW.id, NULL,
     NULL, NULL, CONCAT('part_bom update'), 'system', 'server', '', 'info');

END;

-- trg_part_supplier_prices_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_part_supplier_prices_ad_audit` AFTER DELETE ON `part_supplier_prices` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'part_supplier_prices', 'PartSupplierPricesPage', OLD.id, NULL,
     NULL, NULL, CONCAT('part_supplier_prices delete'), 'system', 'server', '', 'warning');
END;

-- trg_part_supplier_prices_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_part_supplier_prices_ai_audit` AFTER INSERT ON `part_supplier_prices` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'part_supplier_prices', 'PartSupplierPricesPage', NEW.id, NULL,
     NULL, NULL, CONCAT('part_supplier_prices insert'), 'system', 'server', '', 'info');
END;

-- trg_part_supplier_prices_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_part_supplier_prices_au_audit` AFTER UPDATE ON `part_supplier_prices` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'part_supplier_prices', 'PartSupplierPricesPage', NEW.id, NULL,
     NULL, NULL, CONCAT('part_supplier_prices update'), 'system', 'server', '', 'info');

END;

-- trg_permissions_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_permissions_ad_audit` AFTER DELETE ON `permissions` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'permissions', 'PermissionsPage', OLD.id, NULL,
     NULL, NULL, CONCAT('permissions delete', CONCAT('; code=', COALESCE(OLD.code,''))), 'system', 'server', '', 'warning');
END;

-- trg_permissions_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_permissions_ai_audit` AFTER INSERT ON `permissions` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'permissions', 'PermissionsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('permissions insert', CONCAT('; code=', COALESCE(NEW.code,''))), 'system', 'server', '', 'info');
END;

-- trg_permissions_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_permissions_au_audit` AFTER UPDATE ON `permissions` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'permissions', 'PermissionsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('permissions update', CONCAT('; code: ', COALESCE(OLD.code,''), ' → ', COALESCE(NEW.code,''))), 'system', 'server', '', 'info');

  IF COALESCE(OLD.code,'') <> COALESCE(NEW.code,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'permissions', 'PermissionsPage', NEW.id, 'code',
       OLD.code, NEW.code, 'permissions update: code changed', 'system', 'server', '', 'info');
  END IF;
END;

-- trg_permission_change_log_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_permission_change_log_ad_audit` AFTER DELETE ON `permission_change_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'permission_change_log', 'PermissionChangeLogPage', OLD.id, NULL,
     NULL, NULL, CONCAT('permission_change_log delete'), 'system', 'server', '', 'warning');
END;

-- trg_permission_change_log_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_permission_change_log_ai_audit` AFTER INSERT ON `permission_change_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'permission_change_log', 'PermissionChangeLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('permission_change_log insert'), 'system', 'server', '', 'info');
END;

-- trg_permission_change_log_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_permission_change_log_au_audit` AFTER UPDATE ON `permission_change_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'permission_change_log', 'PermissionChangeLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('permission_change_log update'), 'system', 'server', '', 'info');

END;

-- trg_permission_requests_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_permission_requests_ad_audit` AFTER DELETE ON `permission_requests` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'permission_requests', 'PermissionRequestsPage', OLD.id, NULL,
     NULL, NULL, CONCAT('permission_requests delete'), 'system', 'server', '', 'warning');
END;

-- trg_permission_requests_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_permission_requests_ai_audit` AFTER INSERT ON `permission_requests` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'permission_requests', 'PermissionRequestsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('permission_requests insert'), 'system', 'server', '', 'info');
END;

-- trg_permission_requests_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_permission_requests_au_audit` AFTER UPDATE ON `permission_requests` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'permission_requests', 'PermissionRequestsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('permission_requests update'), 'system', 'server', '', 'info');

END;

-- trg_photos_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_photos_ad_audit` AFTER DELETE ON `photos` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'photos', 'PhotosPage', OLD.id, NULL,
     NULL, NULL, CONCAT('photos delete'), 'system', 'server', '', 'warning');
END;

-- trg_photos_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_photos_ai_audit` AFTER INSERT ON `photos` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'photos', 'PhotosPage', NEW.id, NULL,
     NULL, NULL, CONCAT('photos insert'), 'system', 'server', '', 'info');
END;

-- trg_photos_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_photos_au_audit` AFTER UPDATE ON `photos` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'photos', 'PhotosPage', NEW.id, NULL,
     NULL, NULL, CONCAT('photos update'), 'system', 'server', '', 'info');

END;

-- trg_ppm_plans_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_ppm_plans_ad_audit` AFTER DELETE ON `ppm_plans` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'ppm_plans', 'PpmPlansPage', OLD.id, NULL,
     NULL, NULL, CONCAT('ppm_plans delete'), 'system', 'server', '', 'warning');
END;

-- trg_ppm_plans_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_ppm_plans_ai_audit` AFTER INSERT ON `ppm_plans` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'ppm_plans', 'PpmPlansPage', NEW.id, NULL,
     NULL, NULL, CONCAT('ppm_plans insert'), 'system', 'server', '', 'info');
END;

-- trg_ppm_plans_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_ppm_plans_au_audit` AFTER UPDATE ON `ppm_plans` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'ppm_plans', 'PpmPlansPage', NEW.id, NULL,
     NULL, NULL, CONCAT('ppm_plans update'), 'system', 'server', '', 'info');

END;

-- trg_preventive_maintenance_plans_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_preventive_maintenance_plans_ad_audit` AFTER DELETE ON `preventive_maintenance_plans` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'preventive_maintenance_plans', 'PreventiveMaintenancePlansPage', OLD.id, NULL,
     NULL, NULL, CONCAT('preventive_maintenance_plans delete', CONCAT('; code=', COALESCE(OLD.code,'')), CONCAT('; name=', COALESCE(OLD.name,''))), 'system', 'server', '', 'warning');
END;

-- trg_preventive_maintenance_plans_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_preventive_maintenance_plans_ai_audit` AFTER INSERT ON `preventive_maintenance_plans` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'preventive_maintenance_plans', 'PreventiveMaintenancePlansPage', NEW.id, NULL,
     NULL, NULL, CONCAT('preventive_maintenance_plans insert', CONCAT('; code=', COALESCE(NEW.code,'')), CONCAT('; name=', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');
END;

-- trg_preventive_maintenance_plans_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_preventive_maintenance_plans_au_audit` AFTER UPDATE ON `preventive_maintenance_plans` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'preventive_maintenance_plans', 'PreventiveMaintenancePlansPage', NEW.id, NULL,
     NULL, NULL, CONCAT('preventive_maintenance_plans update', CONCAT('; code: ', COALESCE(OLD.code,''), ' → ', COALESCE(NEW.code,'')), CONCAT('; name: ', COALESCE(OLD.name,''), ' → ', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');

  IF COALESCE(OLD.code,'') <> COALESCE(NEW.code,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'preventive_maintenance_plans', 'PreventiveMaintenancePlansPage', NEW.id, 'code',
       OLD.code, NEW.code, 'preventive_maintenance_plans update: code changed', 'system', 'server', '', 'info');
  END IF;

  IF COALESCE(OLD.name,'') <> COALESCE(NEW.name,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'preventive_maintenance_plans', 'PreventiveMaintenancePlansPage', NEW.id, 'name',
       OLD.name, NEW.name, 'preventive_maintenance_plans update: name changed', 'system', 'server', '', 'info');
  END IF;
END;

-- trg_qe_sync
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_qe_sync` BEFORE INSERT ON `quality_events` FOR EACH ROW BEGIN
  CALL ref_touch('quality_event_type', NEW.event_type, NEW.event_type);
  IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.type_id = LAST_INSERT_ID(); END IF;
  CALL ref_touch('quality_status', NEW.status, NEW.status);
  IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.status_id = LAST_INSERT_ID(); END IF;
END;

-- trg_qe_sync_u
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_qe_sync_u` BEFORE UPDATE ON `quality_events` FOR EACH ROW BEGIN
  IF (NEW.event_type <=> OLD.event_type) = 0 THEN
    CALL ref_touch('quality_event_type', NEW.event_type, NEW.event_type);
    IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.type_id = LAST_INSERT_ID(); END IF;
  END IF;
  IF (NEW.status <=> OLD.status) = 0 THEN
    CALL ref_touch('quality_status', NEW.status, NEW.status);
    IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.status_id = LAST_INSERT_ID(); END IF;
  END IF;
END;

-- trg_quality_events_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_quality_events_ad_audit` AFTER DELETE ON `quality_events` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'quality_events', 'QualityEventsPage', OLD.id, NULL,
     NULL, NULL, CONCAT('quality_events delete'), 'system', 'server', '', 'warning');
END;

-- trg_quality_events_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_quality_events_ai_audit` AFTER INSERT ON `quality_events` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'quality_events', 'QualityEventsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('quality_events insert'), 'system', 'server', '', 'info');
END;

-- trg_quality_events_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_quality_events_au_audit` AFTER UPDATE ON `quality_events` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'quality_events', 'QualityEventsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('quality_events update'), 'system', 'server', '', 'info');

END;

-- trg_report_schedule_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_report_schedule_ad_audit` AFTER DELETE ON `report_schedule` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'report_schedule', 'ReportSchedulePage', OLD.id, NULL,
     NULL, NULL, CONCAT('report_schedule delete'), 'system', 'server', '', 'warning');
END;

-- trg_report_schedule_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_report_schedule_ai_audit` AFTER INSERT ON `report_schedule` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'report_schedule', 'ReportSchedulePage', NEW.id, NULL,
     NULL, NULL, CONCAT('report_schedule insert'), 'system', 'server', '', 'info');
END;

-- trg_report_schedule_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_report_schedule_au_audit` AFTER UPDATE ON `report_schedule` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'report_schedule', 'ReportSchedulePage', NEW.id, NULL,
     NULL, NULL, CONCAT('report_schedule update'), 'system', 'server', '', 'info');

END;

-- trg_requalification_schedule_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_requalification_schedule_ad_audit` AFTER DELETE ON `requalification_schedule` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'requalification_schedule', 'RequalificationSchedulePage', OLD.id, NULL,
     NULL, NULL, CONCAT('requalification_schedule delete'), 'system', 'server', '', 'warning');
END;

-- trg_requalification_schedule_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_requalification_schedule_ai_audit` AFTER INSERT ON `requalification_schedule` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'requalification_schedule', 'RequalificationSchedulePage', NEW.id, NULL,
     NULL, NULL, CONCAT('requalification_schedule insert'), 'system', 'server', '', 'info');
END;

-- trg_requalification_schedule_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_requalification_schedule_au_audit` AFTER UPDATE ON `requalification_schedule` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'requalification_schedule', 'RequalificationSchedulePage', NEW.id, NULL,
     NULL, NULL, CONCAT('requalification_schedule update'), 'system', 'server', '', 'info');

END;

-- trg_roles_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_roles_ad_audit` AFTER DELETE ON `roles` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'roles', 'RolesPage', OLD.id, NULL,
     NULL, NULL, CONCAT('roles delete', CONCAT('; name=', COALESCE(OLD.name,''))), 'system', 'server', '', 'warning');
END;

-- trg_roles_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_roles_ai_audit` AFTER INSERT ON `roles` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'roles', 'RolesPage', NEW.id, NULL,
     NULL, NULL, CONCAT('roles insert', CONCAT('; name=', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');
END;

-- trg_roles_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_roles_au_audit` AFTER UPDATE ON `roles` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'roles', 'RolesPage', NEW.id, NULL,
     NULL, NULL, CONCAT('roles update', CONCAT('; name: ', COALESCE(OLD.name,''), ' → ', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');

  IF COALESCE(OLD.name,'') <> COALESCE(NEW.name,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'roles', 'RolesPage', NEW.id, 'name',
       OLD.name, NEW.name, 'roles update: name changed', 'system', 'server', '', 'info');
  END IF;
END;

-- trg_role_audit_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_role_audit_ad_audit` AFTER DELETE ON `role_audit` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'role_audit', 'RoleAuditPage', OLD.id, NULL,
     NULL, NULL, CONCAT('role_audit delete'), 'system', 'server', '', 'warning');
END;

-- trg_role_audit_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_role_audit_ai_audit` AFTER INSERT ON `role_audit` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'role_audit', 'RoleAuditPage', NEW.id, NULL,
     NULL, NULL, CONCAT('role_audit insert'), 'system', 'server', '', 'info');
END;

-- trg_role_audit_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_role_audit_au_audit` AFTER UPDATE ON `role_audit` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'role_audit', 'RoleAuditPage', NEW.id, NULL,
     NULL, NULL, CONCAT('role_audit update'), 'system', 'server', '', 'info');

END;

-- trg_role_permissions_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_role_permissions_ad_audit` AFTER DELETE ON `role_permissions` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'role_permissions', 'RolePermissionsPage', OLD.role_id, NULL,
     NULL, NULL, CONCAT('role_permissions delete'), 'system', 'server', '', 'warning');
END;

-- trg_role_permissions_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_role_permissions_ai_audit` AFTER INSERT ON `role_permissions` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'role_permissions', 'RolePermissionsPage', NEW.role_id, NULL,
     NULL, NULL, CONCAT('role_permissions insert'), 'system', 'server', '', 'info');
END;

-- trg_role_permissions_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_role_permissions_au_audit` AFTER UPDATE ON `role_permissions` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'role_permissions', 'RolePermissionsPage', NEW.role_id, NULL,
     NULL, NULL, CONCAT('role_permissions update'), 'system', 'server', '', 'info');

END;

-- trg_rooms_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_rooms_ad_audit` AFTER DELETE ON `rooms` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'rooms', 'RoomsPage', OLD.id, NULL,
     NULL, NULL, CONCAT('rooms delete', CONCAT('; code=', COALESCE(OLD.code,'')), CONCAT('; name=', COALESCE(OLD.name,''))), 'system', 'server', '', 'warning');
END;

-- trg_rooms_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_rooms_ai_audit` AFTER INSERT ON `rooms` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'rooms', 'RoomsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('rooms insert', CONCAT('; code=', COALESCE(NEW.code,'')), CONCAT('; name=', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');
END;

-- trg_rooms_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_rooms_au_audit` AFTER UPDATE ON `rooms` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'rooms', 'RoomsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('rooms update', CONCAT('; code: ', COALESCE(OLD.code,''), ' → ', COALESCE(NEW.code,'')), CONCAT('; name: ', COALESCE(OLD.name,''), ' → ', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');

  IF COALESCE(OLD.code,'') <> COALESCE(NEW.code,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'rooms', 'RoomsPage', NEW.id, 'code',
       OLD.code, NEW.code, 'rooms update: code changed', 'system', 'server', '', 'info');
  END IF;

  IF COALESCE(OLD.name,'') <> COALESCE(NEW.name,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'rooms', 'RoomsPage', NEW.id, 'name',
       OLD.name, NEW.name, 'rooms update: name changed', 'system', 'server', '', 'info');
  END IF;
END;

-- trg_scheduled_jobs_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_scheduled_jobs_ad_audit` AFTER DELETE ON `scheduled_jobs` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'scheduled_jobs', 'ScheduledJobsPage', OLD.id, NULL,
     NULL, NULL, CONCAT('scheduled_jobs delete', CONCAT('; name=', COALESCE(OLD.name,''))), 'system', 'server', '', 'warning');
END;

-- trg_scheduled_jobs_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_scheduled_jobs_ai_audit` AFTER INSERT ON `scheduled_jobs` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'scheduled_jobs', 'ScheduledJobsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('scheduled_jobs insert', CONCAT('; name=', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');
END;

-- trg_scheduled_jobs_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_scheduled_jobs_au_audit` AFTER UPDATE ON `scheduled_jobs` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'scheduled_jobs', 'ScheduledJobsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('scheduled_jobs update', CONCAT('; name: ', COALESCE(OLD.name,''), ' → ', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');

  IF COALESCE(OLD.name,'') <> COALESCE(NEW.name,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'scheduled_jobs', 'ScheduledJobsPage', NEW.id, 'name',
       OLD.name, NEW.name, 'scheduled_jobs update: name changed', 'system', 'server', '', 'info');
  END IF;
END;

-- trg_scheduled_jobs_delete
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_scheduled_jobs_delete` AFTER DELETE ON `scheduled_jobs` FOR EACH ROW BEGIN
    INSERT INTO scheduled_job_audit_log (
        scheduled_job_id, user_id, action, old_value, source_ip, device_info, session_id, note
    ) VALUES (
        OLD.id, OLD.last_modified_by, 'DELETE',
        OLD.comment, OLD.ip_address, OLD.device_info, OLD.session_id, OLD.comment
    );
END;

-- trg_scheduled_jobs_insert
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_scheduled_jobs_insert` AFTER INSERT ON `scheduled_jobs` FOR EACH ROW BEGIN
    INSERT INTO scheduled_job_audit_log (
        scheduled_job_id, user_id, action, new_value, source_ip, device_info, session_id, digital_signature, note
    ) VALUES (
        NEW.id, NEW.created_by, 'CREATE',
        CONCAT('Created job: ', NEW.name, ' [', NEW.job_type, ']'),
        NEW.ip_address, NEW.device_info, NEW.session_id, NEW.digital_signature, NEW.comment
    );
END;

-- trg_scheduled_jobs_update
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_scheduled_jobs_update` AFTER UPDATE ON `scheduled_jobs` FOR EACH ROW BEGIN
    INSERT INTO scheduled_job_audit_log (
        scheduled_job_id, user_id, action, old_value, new_value, source_ip, device_info, session_id, digital_signature, note
    ) VALUES (
        NEW.id, NEW.last_modified_by, 'UPDATE',
        OLD.comment, NEW.comment,
        NEW.ip_address, NEW.device_info, NEW.session_id, NEW.digital_signature, NEW.comment
    );
END;

-- trg_scheduled_job_audit_log_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_scheduled_job_audit_log_ad_audit` AFTER DELETE ON `scheduled_job_audit_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'scheduled_job_audit_log', 'ScheduledJobAuditLogPage', OLD.id, NULL,
     NULL, NULL, CONCAT('scheduled_job_audit_log delete'), 'system', 'server', '', 'warning');
END;

-- trg_scheduled_job_audit_log_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_scheduled_job_audit_log_ai_audit` AFTER INSERT ON `scheduled_job_audit_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'scheduled_job_audit_log', 'ScheduledJobAuditLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('scheduled_job_audit_log insert'), 'system', 'server', '', 'info');
END;

-- trg_scheduled_job_audit_log_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_scheduled_job_audit_log_au_audit` AFTER UPDATE ON `scheduled_job_audit_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'scheduled_job_audit_log', 'ScheduledJobAuditLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('scheduled_job_audit_log update'), 'system', 'server', '', 'info');

END;

-- trg_schema_migration_log_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_schema_migration_log_ad_audit` AFTER DELETE ON `schema_migration_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'schema_migration_log', 'SchemaMigrationLogPage', OLD.id, NULL,
     NULL, NULL, CONCAT('schema_migration_log delete'), 'system', 'server', '', 'warning');
END;

-- trg_schema_migration_log_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_schema_migration_log_ai_audit` AFTER INSERT ON `schema_migration_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'schema_migration_log', 'SchemaMigrationLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('schema_migration_log insert'), 'system', 'server', '', 'info');
END;

-- trg_schema_migration_log_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_schema_migration_log_au_audit` AFTER UPDATE ON `schema_migration_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'schema_migration_log', 'SchemaMigrationLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('schema_migration_log update'), 'system', 'server', '', 'info');

END;

-- trg_sdl_sync
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_sdl_sync` BEFORE INSERT ON `sensor_data_logs` FOR EACH ROW BEGIN
  CALL ref_touch('sensor_type', NEW.sensor_type, NEW.sensor_type);
  IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.sensor_type_id = LAST_INSERT_ID(); END IF;

  
  IF NEW.unit IS NOT NULL THEN
    INSERT INTO units(code,name,quantity) VALUES (NEW.unit, NEW.unit, NULL)
    ON DUPLICATE KEY UPDATE id=LAST_INSERT_ID(id), name=VALUES(name);
    SET NEW.unit_id = LAST_INSERT_ID();
  END IF;
END;

-- trg_sdl_sync_u
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_sdl_sync_u` BEFORE UPDATE ON `sensor_data_logs` FOR EACH ROW BEGIN
  IF (NEW.sensor_type <=> OLD.sensor_type) = 0 THEN
    CALL ref_touch('sensor_type', NEW.sensor_type, NEW.sensor_type);
    IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.sensor_type_id = LAST_INSERT_ID(); END IF;
  END IF;

  IF (NEW.unit <=> OLD.unit) = 0 AND NEW.unit IS NOT NULL THEN
    INSERT INTO units(code,name) VALUES (NEW.unit, NEW.unit)
    ON DUPLICATE KEY UPDATE id=LAST_INSERT_ID(id), name=VALUES(name);
    SET NEW.unit_id = LAST_INSERT_ID();
  END IF;
END;

-- trg_sensitive_access_fk_guard
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_sensitive_access_fk_guard` BEFORE INSERT ON `sensitive_data_access_log` FOR EACH ROW BEGIN
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
END;

-- trg_sensitive_data_access_log_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_sensitive_data_access_log_ad_audit` AFTER DELETE ON `sensitive_data_access_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'sensitive_data_access_log', 'SensitiveDataAccessLogPage', OLD.id, NULL,
     NULL, NULL, CONCAT('sensitive_data_access_log delete'), 'system', 'server', '', 'warning');
END;

-- trg_sensitive_data_access_log_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_sensitive_data_access_log_ai_audit` AFTER INSERT ON `sensitive_data_access_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'sensitive_data_access_log', 'SensitiveDataAccessLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('sensitive_data_access_log insert'), 'system', 'server', '', 'info');
END;

-- trg_sensitive_data_access_log_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_sensitive_data_access_log_au_audit` AFTER UPDATE ON `sensitive_data_access_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'sensitive_data_access_log', 'SensitiveDataAccessLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('sensitive_data_access_log update'), 'system', 'server', '', 'info');

END;

-- trg_sensor_data_logs_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_sensor_data_logs_ad_audit` AFTER DELETE ON `sensor_data_logs` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'sensor_data_logs', 'SensorDataLogsPage', OLD.id, NULL,
     NULL, NULL, CONCAT('sensor_data_logs delete'), 'system', 'server', '', 'warning');
END;

-- trg_sensor_data_logs_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_sensor_data_logs_ai_audit` AFTER INSERT ON `sensor_data_logs` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'sensor_data_logs', 'SensorDataLogsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('sensor_data_logs insert'), 'system', 'server', '', 'info');
END;

-- trg_sensor_data_logs_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_sensor_data_logs_au_audit` AFTER UPDATE ON `sensor_data_logs` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'sensor_data_logs', 'SensorDataLogsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('sensor_data_logs update'), 'system', 'server', '', 'info');

END;

-- trg_sensor_models_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_sensor_models_ad_audit` AFTER DELETE ON `sensor_models` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'sensor_models', 'SensorModelsPage', OLD.id, NULL,
     NULL, NULL, CONCAT('sensor_models delete'), 'system', 'server', '', 'warning');
END;

-- trg_sensor_models_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_sensor_models_ai_audit` AFTER INSERT ON `sensor_models` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'sensor_models', 'SensorModelsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('sensor_models insert'), 'system', 'server', '', 'info');
END;

-- trg_sensor_models_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_sensor_models_au_audit` AFTER UPDATE ON `sensor_models` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'sensor_models', 'SensorModelsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('sensor_models update'), 'system', 'server', '', 'info');

END;

-- trg_sensor_types_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_sensor_types_ad_audit` AFTER DELETE ON `sensor_types` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'sensor_types', 'SensorTypesPage', OLD.id, NULL,
     NULL, NULL, CONCAT('sensor_types delete', CONCAT('; code=', COALESCE(OLD.code,'')), CONCAT('; name=', COALESCE(OLD.name,''))), 'system', 'server', '', 'warning');
END;

-- trg_sensor_types_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_sensor_types_ai_audit` AFTER INSERT ON `sensor_types` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'sensor_types', 'SensorTypesPage', NEW.id, NULL,
     NULL, NULL, CONCAT('sensor_types insert', CONCAT('; code=', COALESCE(NEW.code,'')), CONCAT('; name=', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');
END;

-- trg_sensor_types_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_sensor_types_au_audit` AFTER UPDATE ON `sensor_types` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'sensor_types', 'SensorTypesPage', NEW.id, NULL,
     NULL, NULL, CONCAT('sensor_types update', CONCAT('; code: ', COALESCE(OLD.code,''), ' → ', COALESCE(NEW.code,'')), CONCAT('; name: ', COALESCE(OLD.name,''), ' → ', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');

  IF COALESCE(OLD.code,'') <> COALESCE(NEW.code,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'sensor_types', 'SensorTypesPage', NEW.id, 'code',
       OLD.code, NEW.code, 'sensor_types update: code changed', 'system', 'server', '', 'info');
  END IF;

  IF COALESCE(OLD.name,'') <> COALESCE(NEW.name,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'sensor_types', 'SensorTypesPage', NEW.id, 'name',
       OLD.name, NEW.name, 'sensor_types update: name changed', 'system', 'server', '', 'info');
  END IF;
END;

-- trg_session_log_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_session_log_ad_audit` AFTER DELETE ON `session_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'session_log', 'SessionLogPage', OLD.id, NULL,
     NULL, NULL, CONCAT('session_log delete'), 'system', 'server', '', 'warning');
END;

-- trg_session_log_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_session_log_ai_audit` AFTER INSERT ON `session_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'session_log', 'SessionLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('session_log insert'), 'system', 'server', '', 'info');
END;

-- trg_session_log_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_session_log_au_audit` AFTER UPDATE ON `session_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'session_log', 'SessionLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('session_log update'), 'system', 'server', '', 'info');

END;

-- trg_sites_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_sites_ad_audit` AFTER DELETE ON `sites` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'sites', 'SitesPage', OLD.id, NULL,
     NULL, NULL, CONCAT('sites delete', CONCAT('; code=', COALESCE(OLD.code,'')), CONCAT('; name=', COALESCE(OLD.name,''))), 'system', 'server', '', 'warning');
END;

-- trg_sites_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_sites_ai_audit` AFTER INSERT ON `sites` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'sites', 'SitesPage', NEW.id, NULL,
     NULL, NULL, CONCAT('sites insert', CONCAT('; code=', COALESCE(NEW.code,'')), CONCAT('; name=', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');
END;

-- trg_sites_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_sites_au_audit` AFTER UPDATE ON `sites` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'sites', 'SitesPage', NEW.id, NULL,
     NULL, NULL, CONCAT('sites update', CONCAT('; code: ', COALESCE(OLD.code,''), ' → ', COALESCE(NEW.code,'')), CONCAT('; name: ', COALESCE(OLD.name,''), ' → ', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');

  IF COALESCE(OLD.code,'') <> COALESCE(NEW.code,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'sites', 'SitesPage', NEW.id, 'code',
       OLD.code, NEW.code, 'sites update: code changed', 'system', 'server', '', 'info');
  END IF;

  IF COALESCE(OLD.name,'') <> COALESCE(NEW.name,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'sites', 'SitesPage', NEW.id, 'name',
       OLD.name, NEW.name, 'sites update: name changed', 'system', 'server', '', 'info');
  END IF;
END;

-- trg_sop_documents_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_sop_documents_ad_audit` AFTER DELETE ON `sop_documents` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'sop_documents', 'SopDocumentsPage', OLD.id, NULL,
     NULL, NULL, CONCAT('sop_documents delete', CONCAT('; code=', COALESCE(OLD.code,'')), CONCAT('; name=', COALESCE(OLD.name,''))), 'system', 'server', '', 'warning');
END;

-- trg_sop_documents_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_sop_documents_ai_audit` AFTER INSERT ON `sop_documents` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'sop_documents', 'SopDocumentsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('sop_documents insert', CONCAT('; code=', COALESCE(NEW.code,'')), CONCAT('; name=', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');
END;

-- trg_sop_documents_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_sop_documents_au_audit` AFTER UPDATE ON `sop_documents` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'sop_documents', 'SopDocumentsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('sop_documents update', CONCAT('; code: ', COALESCE(OLD.code,''), ' → ', COALESCE(NEW.code,'')), CONCAT('; name: ', COALESCE(OLD.name,''), ' → ', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');

  IF COALESCE(OLD.code,'') <> COALESCE(NEW.code,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'sop_documents', 'SopDocumentsPage', NEW.id, 'code',
       OLD.code, NEW.code, 'sop_documents update: code changed', 'system', 'server', '', 'info');
  END IF;

  IF COALESCE(OLD.name,'') <> COALESCE(NEW.name,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'sop_documents', 'SopDocumentsPage', NEW.id, 'name',
       OLD.name, NEW.name, 'sop_documents update: name changed', 'system', 'server', '', 'info');
  END IF;
END;

-- trg_sop_document_log_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_sop_document_log_ad_audit` AFTER DELETE ON `sop_document_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'sop_document_log', 'SopDocumentLogPage', OLD.id, NULL,
     NULL, NULL, CONCAT('sop_document_log delete'), 'system', 'server', '', 'warning');
END;

-- trg_sop_document_log_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_sop_document_log_ai_audit` AFTER INSERT ON `sop_document_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'sop_document_log', 'SopDocumentLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('sop_document_log insert'), 'system', 'server', '', 'info');
END;

-- trg_sop_document_log_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_sop_document_log_au_audit` AFTER UPDATE ON `sop_document_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'sop_document_log', 'SopDocumentLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('sop_document_log update'), 'system', 'server', '', 'info');

END;

-- trg_stock_levels_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_stock_levels_ad_audit` AFTER DELETE ON `stock_levels` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'stock_levels', 'StockLevelsPage', OLD.id, NULL,
     NULL, NULL, CONCAT('stock_levels delete'), 'system', 'server', '', 'warning');
END;

-- trg_stock_levels_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_stock_levels_ai_audit` AFTER INSERT ON `stock_levels` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'stock_levels', 'StockLevelsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('stock_levels insert'), 'system', 'server', '', 'info');
END;

-- trg_stock_levels_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_stock_levels_au_audit` AFTER UPDATE ON `stock_levels` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'stock_levels', 'StockLevelsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('stock_levels update'), 'system', 'server', '', 'info');

END;

-- trg_suppliers_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_suppliers_ad_audit` AFTER DELETE ON `suppliers` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'suppliers', 'SuppliersPage', OLD.id, NULL,
     NULL, NULL, CONCAT('suppliers delete', CONCAT('; code=', COALESCE(OLD.code,'')), CONCAT('; name=', COALESCE(OLD.name,''))), 'system', 'server', '', 'warning');
END;

-- trg_suppliers_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_suppliers_ai_audit` AFTER INSERT ON `suppliers` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'suppliers', 'SuppliersPage', NEW.id, NULL,
     NULL, NULL, CONCAT('suppliers insert', CONCAT('; code=', COALESCE(NEW.code,'')), CONCAT('; name=', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');
END;

-- trg_suppliers_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_suppliers_au_audit` AFTER UPDATE ON `suppliers` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'suppliers', 'SuppliersPage', NEW.id, NULL,
     NULL, NULL, CONCAT('suppliers update', CONCAT('; code: ', COALESCE(OLD.code,''), ' → ', COALESCE(NEW.code,'')), CONCAT('; name: ', COALESCE(OLD.name,''), ' → ', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');

  IF COALESCE(OLD.code,'') <> COALESCE(NEW.code,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'suppliers', 'SuppliersPage', NEW.id, 'code',
       OLD.code, NEW.code, 'suppliers update: code changed', 'system', 'server', '', 'info');
  END IF;

  IF COALESCE(OLD.name,'') <> COALESCE(NEW.name,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'suppliers', 'SuppliersPage', NEW.id, 'name',
       OLD.name, NEW.name, 'suppliers update: name changed', 'system', 'server', '', 'info');
  END IF;
END;

-- trg_supplier_risk_audit_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_supplier_risk_audit_ad_audit` AFTER DELETE ON `supplier_risk_audit` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'supplier_risk_audit', 'SupplierRiskAuditPage', OLD.id, NULL,
     NULL, NULL, CONCAT('supplier_risk_audit delete'), 'system', 'server', '', 'warning');
END;

-- trg_supplier_risk_audit_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_supplier_risk_audit_ai_audit` AFTER INSERT ON `supplier_risk_audit` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'supplier_risk_audit', 'SupplierRiskAuditPage', NEW.id, NULL,
     NULL, NULL, CONCAT('supplier_risk_audit insert'), 'system', 'server', '', 'info');
END;

-- trg_supplier_risk_audit_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_supplier_risk_audit_au_audit` AFTER UPDATE ON `supplier_risk_audit` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'supplier_risk_audit', 'SupplierRiskAuditPage', NEW.id, NULL,
     NULL, NULL, CONCAT('supplier_risk_audit update'), 'system', 'server', '', 'info');

END;

-- trg_sup_sync
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_sup_sync` BEFORE INSERT ON `suppliers` FOR EACH ROW BEGIN
  CALL ref_touch('supplier_status', NEW.status, NEW.status);
  IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.status_id = LAST_INSERT_ID(); END IF;
  CALL ref_touch('supplier_type', NEW.type, NEW.type);
  IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.type_id = LAST_INSERT_ID(); END IF;
END;

-- trg_sup_sync_u
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_sup_sync_u` BEFORE UPDATE ON `suppliers` FOR EACH ROW BEGIN
  IF (NEW.status <=> OLD.status) = 0 THEN
    CALL ref_touch('supplier_status', NEW.status, NEW.status);
    IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.status_id = LAST_INSERT_ID(); END IF;
  END IF;
  IF (NEW.type <=> OLD.type) = 0 THEN
    CALL ref_touch('supplier_type', NEW.type, NEW.type);
    IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.type_id = LAST_INSERT_ID(); END IF;
  END IF;
END;

-- trg_system_event_log_bi
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_system_event_log_bi` BEFORE INSERT ON `system_event_log` FOR EACH ROW BEGIN
  
  IF NEW.event_time IS NULL THEN SET NEW.event_time = CURRENT_TIMESTAMP; END IF;
  IF NEW.`timestamp` IS NULL THEN SET NEW.`timestamp` = NEW.event_time; END IF;

  
  IF NEW.event_type IS NULL AND NEW.`action` IS NOT NULL THEN SET NEW.event_type = NEW.`action`; END IF;
  IF NEW.`action` IS NULL THEN SET NEW.`action` = NEW.event_type; END IF;

  
  IF NEW.description IS NULL AND NEW.`details` IS NOT NULL THEN SET NEW.description = NEW.`details`; END IF;
  IF NEW.`details` IS NULL THEN SET NEW.`details` = NEW.description; END IF;
END;

-- trg_system_event_log_bu
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_system_event_log_bu` BEFORE UPDATE ON `system_event_log` FOR EACH ROW BEGIN
  
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
END;

-- trg_system_event_log_fk_guard
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_system_event_log_fk_guard` BEFORE INSERT ON `system_event_log` FOR EACH ROW BEGIN
  IF NEW.user_id IS NOT NULL THEN
    IF (SELECT COUNT(*) FROM users WHERE id = NEW.user_id) = 0 THEN
      SET NEW.user_id = NULL;
    END IF;
  END IF;
END;

-- trg_system_parameters_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_system_parameters_ad_audit` AFTER DELETE ON `system_parameters` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'system_parameters', 'SystemParametersPage', OLD.id, NULL,
     NULL, NULL, CONCAT('system_parameters delete'), 'system', 'server', '', 'warning');
END;

-- trg_system_parameters_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_system_parameters_ai_audit` AFTER INSERT ON `system_parameters` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'system_parameters', 'SystemParametersPage', NEW.id, NULL,
     NULL, NULL, CONCAT('system_parameters insert'), 'system', 'server', '', 'info');
END;

-- trg_system_parameters_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_system_parameters_au_audit` AFTER UPDATE ON `system_parameters` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'system_parameters', 'SystemParametersPage', NEW.id, NULL,
     NULL, NULL, CONCAT('system_parameters update'), 'system', 'server', '', 'info');

END;

-- trg_tags_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_tags_ad_audit` AFTER DELETE ON `tags` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'tags', 'TagsPage', OLD.id, NULL,
     NULL, NULL, CONCAT('tags delete'), 'system', 'server', '', 'warning');
END;

-- trg_tags_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_tags_ai_audit` AFTER INSERT ON `tags` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'tags', 'TagsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('tags insert'), 'system', 'server', '', 'info');
END;

-- trg_tags_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_tags_au_audit` AFTER UPDATE ON `tags` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'tags', 'TagsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('tags update'), 'system', 'server', '', 'info');

END;

-- trg_tenants_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_tenants_ad_audit` AFTER DELETE ON `tenants` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'tenants', 'TenantsPage', OLD.id, NULL,
     NULL, NULL, CONCAT('tenants delete', CONCAT('; code=', COALESCE(OLD.code,'')), CONCAT('; name=', COALESCE(OLD.name,''))), 'system', 'server', '', 'warning');
END;

-- trg_tenants_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_tenants_ai_audit` AFTER INSERT ON `tenants` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'tenants', 'TenantsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('tenants insert', CONCAT('; code=', COALESCE(NEW.code,'')), CONCAT('; name=', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');
END;

-- trg_tenants_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_tenants_au_audit` AFTER UPDATE ON `tenants` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'tenants', 'TenantsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('tenants update', CONCAT('; code: ', COALESCE(OLD.code,''), ' → ', COALESCE(NEW.code,'')), CONCAT('; name: ', COALESCE(OLD.name,''), ' → ', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');

  IF COALESCE(OLD.code,'') <> COALESCE(NEW.code,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'tenants', 'TenantsPage', NEW.id, 'code',
       OLD.code, NEW.code, 'tenants update: code changed', 'system', 'server', '', 'info');
  END IF;

  IF COALESCE(OLD.name,'') <> COALESCE(NEW.name,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'tenants', 'TenantsPage', NEW.id, 'name',
       OLD.name, NEW.name, 'tenants update: name changed', 'system', 'server', '', 'info');
  END IF;
END;

-- trg_users_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_users_ad_audit` AFTER DELETE ON `users` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'users', 'UsersPage', OLD.id, NULL,
     NULL, NULL, CONCAT('users delete', CONCAT('; username=', COALESCE(OLD.username,''))), 'system', 'server', '', 'warning');
END;

-- trg_users_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_users_ai_audit` AFTER INSERT ON `users` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'users', 'UsersPage', NEW.id, NULL,
     NULL, NULL, CONCAT('users insert', CONCAT('; username=', COALESCE(NEW.username,''))), 'system', 'server', '', 'info');
END;

-- trg_users_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_users_au_audit` AFTER UPDATE ON `users` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'users', 'UsersPage', NEW.id, NULL,
     NULL, NULL, CONCAT('users update', CONCAT('; username: ', COALESCE(OLD.username,''), ' → ', COALESCE(NEW.username,''))), 'system', 'server', '', 'info');

END;

-- trg_user_audit_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_user_audit_ad_audit` AFTER DELETE ON `user_audit` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'user_audit', 'UserAuditPage', OLD.id, NULL,
     NULL, NULL, CONCAT('user_audit delete'), 'system', 'server', '', 'warning');
END;

-- trg_user_audit_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_user_audit_ai_audit` AFTER INSERT ON `user_audit` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'user_audit', 'UserAuditPage', NEW.id, NULL,
     NULL, NULL, CONCAT('user_audit insert'), 'system', 'server', '', 'info');
END;

-- trg_user_audit_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_user_audit_au_audit` AFTER UPDATE ON `user_audit` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'user_audit', 'UserAuditPage', NEW.id, NULL,
     NULL, NULL, CONCAT('user_audit update'), 'system', 'server', '', 'info');

END;

-- trg_user_esignatures_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_user_esignatures_ad_audit` AFTER DELETE ON `user_esignatures` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'user_esignatures', 'UserEsignaturesPage', OLD.id, NULL,
     NULL, NULL, CONCAT('user_esignatures delete'), 'system', 'server', '', 'warning');
END;

-- trg_user_esignatures_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_user_esignatures_ai_audit` AFTER INSERT ON `user_esignatures` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'user_esignatures', 'UserEsignaturesPage', NEW.id, NULL,
     NULL, NULL, CONCAT('user_esignatures insert'), 'system', 'server', '', 'info');
END;

-- trg_user_esignatures_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_user_esignatures_au_audit` AFTER UPDATE ON `user_esignatures` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'user_esignatures', 'UserEsignaturesPage', NEW.id, NULL,
     NULL, NULL, CONCAT('user_esignatures update'), 'system', 'server', '', 'info');

END;

-- trg_user_login_audit_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_user_login_audit_ad_audit` AFTER DELETE ON `user_login_audit` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'user_login_audit', 'UserLoginAuditPage', OLD.id, NULL,
     NULL, NULL, CONCAT('user_login_audit delete'), 'system', 'server', '', 'warning');
END;

-- trg_user_login_audit_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_user_login_audit_ai_audit` AFTER INSERT ON `user_login_audit` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'user_login_audit', 'UserLoginAuditPage', NEW.id, NULL,
     NULL, NULL, CONCAT('user_login_audit insert'), 'system', 'server', '', 'info');
END;

-- trg_user_login_audit_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_user_login_audit_au_audit` AFTER UPDATE ON `user_login_audit` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'user_login_audit', 'UserLoginAuditPage', NEW.id, NULL,
     NULL, NULL, CONCAT('user_login_audit update'), 'system', 'server', '', 'info');

END;

-- trg_user_permissions_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_user_permissions_ad_audit` AFTER DELETE ON `user_permissions` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'user_permissions', 'UserPermissionsPage', OLD.user_id, NULL,
     NULL, NULL, CONCAT('user_permissions delete'), 'system', 'server', '', 'warning');
END;

-- trg_user_permissions_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_user_permissions_ai_audit` AFTER INSERT ON `user_permissions` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'user_permissions', 'UserPermissionsPage', NEW.user_id, NULL,
     NULL, NULL, CONCAT('user_permissions insert'), 'system', 'server', '', 'info');
END;

-- trg_user_permissions_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_user_permissions_au_audit` AFTER UPDATE ON `user_permissions` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'user_permissions', 'UserPermissionsPage', NEW.user_id, NULL,
     NULL, NULL, CONCAT('user_permissions update'), 'system', 'server', '', 'info');

END;

-- trg_user_roles_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_user_roles_ad_audit` AFTER DELETE ON `user_roles` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'user_roles', 'UserRolesPage', OLD.user_id, NULL,
     NULL, NULL, CONCAT('user_roles delete'), 'system', 'server', '', 'warning');
END;

-- trg_user_roles_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_user_roles_ai_audit` AFTER INSERT ON `user_roles` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'user_roles', 'UserRolesPage', NEW.user_id, NULL,
     NULL, NULL, CONCAT('user_roles insert'), 'system', 'server', '', 'info');
END;

-- trg_user_roles_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_user_roles_au_audit` AFTER UPDATE ON `user_roles` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'user_roles', 'UserRolesPage', NEW.user_id, NULL,
     NULL, NULL, CONCAT('user_roles update'), 'system', 'server', '', 'info');

END;

-- trg_user_subscriptions_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_user_subscriptions_ad_audit` AFTER DELETE ON `user_subscriptions` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'user_subscriptions', 'UserSubscriptionsPage', OLD.id, NULL,
     NULL, NULL, CONCAT('user_subscriptions delete'), 'system', 'server', '', 'warning');
END;

-- trg_user_subscriptions_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_user_subscriptions_ai_audit` AFTER INSERT ON `user_subscriptions` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'user_subscriptions', 'UserSubscriptionsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('user_subscriptions insert'), 'system', 'server', '', 'info');
END;

-- trg_user_subscriptions_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_user_subscriptions_au_audit` AFTER UPDATE ON `user_subscriptions` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'user_subscriptions', 'UserSubscriptionsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('user_subscriptions update'), 'system', 'server', '', 'info');

END;

-- trg_user_training_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_user_training_ad_audit` AFTER DELETE ON `user_training` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'user_training', 'UserTrainingPage', OLD.id, NULL,
     NULL, NULL, CONCAT('user_training delete'), 'system', 'server', '', 'warning');
END;

-- trg_user_training_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_user_training_ai_audit` AFTER INSERT ON `user_training` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'user_training', 'UserTrainingPage', NEW.id, NULL,
     NULL, NULL, CONCAT('user_training insert'), 'system', 'server', '', 'info');
END;

-- trg_user_training_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_user_training_au_audit` AFTER UPDATE ON `user_training` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'user_training', 'UserTrainingPage', NEW.id, NULL,
     NULL, NULL, CONCAT('user_training update'), 'system', 'server', '', 'info');

END;

-- trg_validations_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_validations_ad_audit` AFTER DELETE ON `validations` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'validations', 'ValidationsPage', OLD.id, NULL,
     NULL, NULL, CONCAT('validations delete'), 'system', 'server', '', 'warning');
END;

-- trg_validations_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_validations_ai_audit` AFTER INSERT ON `validations` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'validations', 'ValidationsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('validations insert'), 'system', 'server', '', 'info');
END;

-- trg_validations_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_validations_au_audit` AFTER UPDATE ON `validations` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'validations', 'ValidationsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('validations update'), 'system', 'server', '', 'info');

END;

-- trg_warehouses_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_warehouses_ad_audit` AFTER DELETE ON `warehouses` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'warehouses', 'WarehousesPage', OLD.id, NULL,
     NULL, NULL, CONCAT('warehouses delete', CONCAT('; name=', COALESCE(OLD.name,''))), 'system', 'server', '', 'warning');
END;

-- trg_warehouses_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_warehouses_ai_audit` AFTER INSERT ON `warehouses` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'warehouses', 'WarehousesPage', NEW.id, NULL,
     NULL, NULL, CONCAT('warehouses insert', CONCAT('; name=', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');
END;

-- trg_warehouses_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_warehouses_au_audit` AFTER UPDATE ON `warehouses` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'warehouses', 'WarehousesPage', NEW.id, NULL,
     NULL, NULL, CONCAT('warehouses update', CONCAT('; name: ', COALESCE(OLD.name,''), ' → ', COALESCE(NEW.name,''))), 'system', 'server', '', 'info');

  IF COALESCE(OLD.name,'') <> COALESCE(NEW.name,'') THEN
    INSERT INTO system_event_log
      (user_id, event_type, table_name, related_module, record_id, field_name,
       old_value, new_value, description, source_ip, device_info, session_id, severity)
    VALUES
      (NULL, 'UPDATE', 'warehouses', 'WarehousesPage', NEW.id, 'name',
       OLD.name, NEW.name, 'warehouses update: name changed', 'system', 'server', '', 'info');
  END IF;
END;

-- trg_work_orders_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_work_orders_ad_audit` AFTER DELETE ON `work_orders` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'work_orders', 'WorkOrdersPage', OLD.id, NULL,
     NULL, NULL, CONCAT('work_orders delete'), 'system', 'server', '', 'warning');
END;

-- trg_work_orders_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_work_orders_ai_audit` AFTER INSERT ON `work_orders` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'work_orders', 'WorkOrdersPage', NEW.id, NULL,
     NULL, NULL, CONCAT('work_orders insert'), 'system', 'server', '', 'info');
END;

-- trg_work_orders_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_work_orders_au_audit` AFTER UPDATE ON `work_orders` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'work_orders', 'WorkOrdersPage', NEW.id, NULL,
     NULL, NULL, CONCAT('work_orders update'), 'system', 'server', '', 'info');

END;

-- trg_work_order_audit_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_work_order_audit_ad_audit` AFTER DELETE ON `work_order_audit` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'work_order_audit', 'WorkOrderAuditPage', OLD.id, NULL,
     NULL, NULL, CONCAT('work_order_audit delete'), 'system', 'server', '', 'warning');
END;

-- trg_work_order_audit_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_work_order_audit_ai_audit` AFTER INSERT ON `work_order_audit` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'work_order_audit', 'WorkOrderAuditPage', NEW.id, NULL,
     NULL, NULL, CONCAT('work_order_audit insert'), 'system', 'server', '', 'info');
END;

-- trg_work_order_audit_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_work_order_audit_au_audit` AFTER UPDATE ON `work_order_audit` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'work_order_audit', 'WorkOrderAuditPage', NEW.id, NULL,
     NULL, NULL, CONCAT('work_order_audit update'), 'system', 'server', '', 'info');

END;

-- trg_work_order_checklist_item_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_work_order_checklist_item_ad_audit` AFTER DELETE ON `work_order_checklist_item` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'work_order_checklist_item', 'WorkOrderChecklistItemPage', OLD.id, NULL,
     NULL, NULL, CONCAT('work_order_checklist_item delete'), 'system', 'server', '', 'warning');
END;

-- trg_work_order_checklist_item_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_work_order_checklist_item_ai_audit` AFTER INSERT ON `work_order_checklist_item` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'work_order_checklist_item', 'WorkOrderChecklistItemPage', NEW.id, NULL,
     NULL, NULL, CONCAT('work_order_checklist_item insert'), 'system', 'server', '', 'info');
END;

-- trg_work_order_checklist_item_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_work_order_checklist_item_au_audit` AFTER UPDATE ON `work_order_checklist_item` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'work_order_checklist_item', 'WorkOrderChecklistItemPage', NEW.id, NULL,
     NULL, NULL, CONCAT('work_order_checklist_item update'), 'system', 'server', '', 'info');

END;

-- trg_work_order_comments_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_work_order_comments_ad_audit` AFTER DELETE ON `work_order_comments` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'work_order_comments', 'WorkOrderCommentsPage', OLD.id, NULL,
     NULL, NULL, CONCAT('work_order_comments delete'), 'system', 'server', '', 'warning');
END;

-- trg_work_order_comments_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_work_order_comments_ai_audit` AFTER INSERT ON `work_order_comments` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'work_order_comments', 'WorkOrderCommentsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('work_order_comments insert'), 'system', 'server', '', 'info');
END;

-- trg_work_order_comments_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_work_order_comments_au_audit` AFTER UPDATE ON `work_order_comments` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'work_order_comments', 'WorkOrderCommentsPage', NEW.id, NULL,
     NULL, NULL, CONCAT('work_order_comments update'), 'system', 'server', '', 'info');

END;

-- trg_work_order_parts_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_work_order_parts_ad_audit` AFTER DELETE ON `work_order_parts` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'work_order_parts', 'WorkOrderPartsPage', OLD.work_order_id, NULL,
     NULL, NULL, CONCAT('work_order_parts delete'), 'system', 'server', '', 'warning');
END;

-- trg_work_order_parts_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_work_order_parts_ai_audit` AFTER INSERT ON `work_order_parts` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'work_order_parts', 'WorkOrderPartsPage', NEW.work_order_id, NULL,
     NULL, NULL, CONCAT('work_order_parts insert'), 'system', 'server', '', 'info');
END;

-- trg_work_order_parts_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_work_order_parts_au_audit` AFTER UPDATE ON `work_order_parts` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'work_order_parts', 'WorkOrderPartsPage', NEW.work_order_id, NULL,
     NULL, NULL, CONCAT('work_order_parts update'), 'system', 'server', '', 'info');

END;

-- trg_work_order_signatures_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_work_order_signatures_ad_audit` AFTER DELETE ON `work_order_signatures` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'work_order_signatures', 'WorkOrderSignaturesPage', OLD.id, NULL,
     NULL, NULL, CONCAT('work_order_signatures delete'), 'system', 'server', '', 'warning');
END;

-- trg_work_order_signatures_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_work_order_signatures_ai_audit` AFTER INSERT ON `work_order_signatures` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'work_order_signatures', 'WorkOrderSignaturesPage', NEW.id, NULL,
     NULL, NULL, CONCAT('work_order_signatures insert'), 'system', 'server', '', 'info');
END;

-- trg_work_order_signatures_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_work_order_signatures_au_audit` AFTER UPDATE ON `work_order_signatures` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'work_order_signatures', 'WorkOrderSignaturesPage', NEW.id, NULL,
     NULL, NULL, CONCAT('work_order_signatures update'), 'system', 'server', '', 'info');

END;

-- trg_work_order_status_log_ad_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_work_order_status_log_ad_audit` AFTER DELETE ON `work_order_status_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'DELETE', 'work_order_status_log', 'WorkOrderStatusLogPage', OLD.id, NULL,
     NULL, NULL, CONCAT('work_order_status_log delete'), 'system', 'server', '', 'warning');
END;

-- trg_work_order_status_log_ai_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_work_order_status_log_ai_audit` AFTER INSERT ON `work_order_status_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'INSERT', 'work_order_status_log', 'WorkOrderStatusLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('work_order_status_log insert'), 'system', 'server', '', 'info');
END;

-- trg_work_order_status_log_au_audit
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_work_order_status_log_au_audit` AFTER UPDATE ON `work_order_status_log` FOR EACH ROW BEGIN
  INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity)
  VALUES
    (NULL, 'UPDATE', 'work_order_status_log', 'WorkOrderStatusLogPage', NEW.id, NULL,
     NULL, NULL, CONCAT('work_order_status_log update'), 'system', 'server', '', 'info');

END;

-- trg_wo_sync
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_wo_sync` BEFORE INSERT ON `work_orders` FOR EACH ROW BEGIN
  
  CALL ref_touch('work_order_status', NEW.status, NEW.status);
  IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.status_id = LAST_INSERT_ID(); END IF;
  
  CALL ref_touch('work_order_type', NEW.type, NEW.type);
  IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.type_id = LAST_INSERT_ID(); END IF;
  
  CALL ref_touch('priority', NEW.priority, NEW.priority);
  IF LAST_INSERT_ID() IS NOT NULL THEN SET NEW.priority_id = LAST_INSERT_ID(); END IF;
END;

-- trg_wo_sync_u
CREATE DEFINER=`root`@`localhost` TRIGGER `trg_wo_sync_u` BEFORE UPDATE ON `work_orders` FOR EACH ROW BEGIN
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
END;
