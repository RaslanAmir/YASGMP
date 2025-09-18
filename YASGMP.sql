  `status` varchar(255) DEFAULT NULL,
  `requested_by_id` int DEFAULT NULL,
  `assigned_to_id` int DEFAULT NULL,
  `date_assigned` datetime DEFAULT NULL,
  `last_modified` datetime DEFAULT NULL,
  `last_modified_by_id` int DEFAULT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
