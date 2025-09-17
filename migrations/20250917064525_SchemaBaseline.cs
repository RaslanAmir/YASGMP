using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YasGmp.EfMigrations.Migrations
{
    /// <inheritdoc />
    public partial class SchemaBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "api_audit_log",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    api_key_id = table.Column<int>(type: "int", nullable: true),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    action = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ip_address = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    request_details = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    details = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_api_audit_log", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AttachmentAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AttachmentId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Device = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SignatureHash = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActionAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttachmentAuditLogs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AuditLogEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TableName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EntityId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    SourceIp = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeviceInfo = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SessionId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DigitalSignature = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OldValue = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NewValue = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EntityType = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChangedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IpAddress = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PerformedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogEntries", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "buildings",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    site_id = table.Column<int>(type: "int", nullable: false),
                    code = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_buildings", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "calibration_sensors",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    component_id = table.Column<int>(type: "int", nullable: true),
                    sensor_type = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    range_min = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    range_max = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    unit = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    calibration_interval_days = table.Column<int>(type: "int", nullable: true),
                    iot_device_id = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    sensor_type_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_calibration_sensors", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "capa_actions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    capa_id = table.Column<int>(type: "int", nullable: false),
                    action_type = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    due_date = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    completed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_capa_actions", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "checklist_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    template_id = table.Column<int>(type: "int", nullable: false),
                    item_order = table.Column<int>(type: "int", nullable: true),
                    label = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    expected = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    required = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_checklist_items", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "checklist_templates",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    code = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_checklist_templates", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "comments",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    entity = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    entity_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    comment = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comments", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "component_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_component_types", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "export_audit_log",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    device_info = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    source_ip = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    filter_criteria = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    file_path = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    timestamp = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    export_type = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_export_audit_log", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "IntegrationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SystemName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApiEndpoint = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequestTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RequestJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResponseJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StatusCode = table.Column<int>(type: "int", nullable: false),
                    Processed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationLogs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "iot_event_audit",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iot_event_audit", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "job_titles",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    title = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_titles", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "KpiWidgets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Title = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Value = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    ValueText = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Unit = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Icon = table.Column<string>(type: "varchar(48)", maxLength: 48, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Color = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Trend = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsAlert = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DrilldownKey = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KpiWidgets", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "locations",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_locations", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "lookup_domain",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    domain_code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lookup_domain", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "machine_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_machine_statuses", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "machine_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_machine_types", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "manufacturers",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_manufacturers", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "measurement_units",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    code = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_measurement_units", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "notification_templates",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    subject = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    body = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    channel = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_templates", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Title = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Message = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Priority = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Entity = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EntityId = table.Column<int>(type: "int", nullable: true),
                    Link = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Recipients = table.Column<string>(type: "varchar(2048)", maxLength: 2048, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RecipientList = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RecipientId = table.Column<int>(type: "int", nullable: true),
                    Recipient = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SenderId = table.Column<int>(type: "int", nullable: true),
                    Sender = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SenderUserId = table.Column<int>(type: "int", nullable: true),
                    SenderUserName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AckedByUserId = table.Column<int>(type: "int", nullable: true),
                    AckedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    MutedUntil = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IpAddress = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeviceInfo = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SessionId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ref_domain",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ref_domain", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "responsible_parties",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_responsible_parties", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ScheduledJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    JobType = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NextDue = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RecurrencePattern = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EntityType = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EntityId = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeviceInfo = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SessionId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IpAddress = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DigitalSignature = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EscalationNote = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Comment = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledJobs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sensor_models",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    vendor = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    model_code = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sensor_type_code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    unit_code = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sensor_models", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sites",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    code = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    timezone = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sites", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SupplierAudits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ActionType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Details = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActionTimestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    SourceIp = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeviceInfo = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DigitalSignature = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SupplierVersion = table.Column<int>(type: "int", nullable: true),
                    JsonSnapshotBefore = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    JsonSnapshotAfter = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExtensionDataJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierAudits", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    tag = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tags", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tenants",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    contact = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "rooms",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    building_id = table.Column<int>(type: "int", nullable: false),
                    code = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    classification = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rooms", x => x.id);
                    table.ForeignKey(
                        name: "FK_rooms_buildings_building_id",
                        column: x => x.building_id,
                        principalTable: "buildings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "component_models",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    component_type_id = table.Column<int>(type: "int", nullable: true),
                    model_code = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    model_name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_component_models", x => x.id);
                    table.ForeignKey(
                        name: "FK_component_models_component_types_component_type_id",
                        column: x => x.component_type_id,
                        principalTable: "component_types",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "failure_modes",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    component_type_id = table.Column<int>(type: "int", nullable: true),
                    code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    severity_default = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_failure_modes", x => x.id);
                    table.ForeignKey(
                        name: "FK_failure_modes_component_types_component_type_id",
                        column: x => x.component_type_id,
                        principalTable: "component_types",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "iot_gateways",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    location_id = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iot_gateways", x => x.id);
                    table.ForeignKey(
                        name: "FK_iot_gateways_locations_location_id",
                        column: x => x.location_id,
                        principalTable: "locations",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "lookup_value",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    domain_id = table.Column<int>(type: "int", nullable: false),
                    value_code = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    value_label = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    sort_order = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lookup_value", x => x.id);
                    table.ForeignKey(
                        name: "FK_lookup_value_lookup_domain_domain_id",
                        column: x => x.domain_id,
                        principalTable: "lookup_domain",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "machine_models",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    manufacturer_id = table.Column<int>(type: "int", nullable: true),
                    machine_type_id = table.Column<int>(type: "int", nullable: true),
                    model_code = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    model_name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_machine_models", x => x.id);
                    table.ForeignKey(
                        name: "FK_machine_models_machine_types_machine_type_id",
                        column: x => x.machine_type_id,
                        principalTable: "machine_types",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_machine_models_manufacturers_manufacturer_id",
                        column: x => x.manufacturer_id,
                        principalTable: "manufacturers",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sensor_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    code = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    default_unit_id = table.Column<int>(type: "int", nullable: true),
                    accuracy = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    range_min = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    range_max = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sensor_types", x => x.id);
                    table.ForeignKey(
                        name: "FK_sensor_types_measurement_units_default_unit_id",
                        column: x => x.default_unit_id,
                        principalTable: "measurement_units",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ref_value",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    domain_id = table.Column<int>(type: "int", nullable: false),
                    code = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    label = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    sort_order = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ref_value", x => x.id);
                    table.ForeignKey(
                        name: "FK_ref_value_ref_domain_domain_id",
                        column: x => x.domain_id,
                        principalTable: "ref_domain",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "entity_tags",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    entity = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    entity_id = table.Column<int>(type: "int", nullable: false),
                    tag_id = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entity_tags", x => x.id);
                    table.ForeignKey(
                        name: "FK_entity_tags_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "iot_devices",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    device_uid = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    vendor = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    model = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    firmware = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    gateway_id = table.Column<int>(type: "int", nullable: true),
                    last_seen = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iot_devices", x => x.id);
                    table.ForeignKey(
                        name: "FK_iot_devices_iot_gateways_gateway_id",
                        column: x => x.gateway_id,
                        principalTable: "iot_gateways",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "suppliers",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    vat_number = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    oib = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    contact = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    email = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    phone = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    website = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    supplier_type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    notes = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    contract_file = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    address = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    city = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    country = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    contract_start = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    contract_end = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    cert_doc = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    valid_until = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    risk_score = table.Column<int>(type: "int", nullable: true),
                    last_audit = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    comment = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    digital_signature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    status_id = table.Column<int>(type: "int", nullable: true),
                    type_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_suppliers", x => x.id);
                    table.ForeignKey(
                        name: "FK_suppliers_ref_value_status_id",
                        column: x => x.status_id,
                        principalTable: "ref_value",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_suppliers_ref_value_type_id",
                        column: x => x.type_id,
                        principalTable: "ref_value",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "admin_activity_log",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    admin_id = table.Column<int>(type: "int", nullable: false),
                    activity_time = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    activity = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    affected_table = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    affected_record_id = table.Column<int>(type: "int", nullable: true),
                    details = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    source_ip = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    device_name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    session_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    digital_signature = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    change_version = table.Column<int>(type: "int", nullable: false),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    note = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_activity_log", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "api_keys",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    key_value = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    owner_id = table.Column<int>(type: "int", nullable: true),
                    description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    last_used_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_api_keys", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "api_usage_log",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    api_key_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    call_time = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    endpoint = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    method = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    @params = table.Column<string>(name: "params", type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    response_code = table.Column<int>(type: "int", nullable: false),
                    duration_ms = table.Column<int>(type: "int", nullable: false),
                    success = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    error_message = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    source_ip = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_api_usage_log", x => x.Id);
                    table.ForeignKey(
                        name: "FK_api_usage_log_api_keys_api_key_id",
                        column: x => x.api_key_id,
                        principalTable: "api_keys",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "attachments",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    file_name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    file_path = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    file_type = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    file_size = table.Column<long>(type: "bigint", nullable: true),
                    entity_type = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    entity_id = table.Column<int>(type: "int", nullable: true),
                    file_content = table.Column<byte[]>(type: "LONGBLOB", nullable: true),
                    ocr_text = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    file_hash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    uploaded_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    uploaded_by_id = table.Column<int>(type: "int", nullable: false),
                    is_approved = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    approved_by_id = table.Column<int>(type: "int", nullable: true),
                    approved_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    expiry_date = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    digital_signature = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ip_address = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    device_info = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    session_id = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    note = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    notes = table.Column<string>(type: "varchar(2048)", maxLength: 2048, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    change_version = table.Column<int>(type: "int", nullable: false),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ai_score = table.Column<double>(type: "double", nullable: true),
                    chain_id = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    version_uid = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChangeControlId = table.Column<int>(type: "int", nullable: true),
                    DeviationId = table.Column<int>(type: "int", nullable: true),
                    ExternalContractorId = table.Column<int>(type: "int", nullable: true),
                    IncidentActionId = table.Column<int>(type: "int", nullable: true),
                    IncidentId = table.Column<int>(type: "int", nullable: true),
                    IncidentReportId = table.Column<int>(type: "int", nullable: true),
                    MaintenanceExecutionLogId = table.Column<int>(type: "int", nullable: true),
                    NotificationId = table.Column<int>(type: "int", nullable: true),
                    NotificationLogId = table.Column<int>(type: "int", nullable: true),
                    PartUsageId = table.Column<int>(type: "int", nullable: true),
                    SparePartId = table.Column<int>(type: "int", nullable: true),
                    TrainingRecordId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attachments", x => x.id);
                    table.ForeignKey(
                        name: "FK_attachments_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Notifications",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    action = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_time = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    table_name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    record_id = table.Column<int>(type: "int", nullable: true),
                    field_name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    old_value = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    new_value = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    details = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    source_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    device_info = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    severity = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    digital_signature = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    change_version = table.Column<int>(type: "int", nullable: false),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    note = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId1 = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BackupHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BackupTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    BackupType = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FilePath = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PerformedBy = table.Column<int>(type: "int", nullable: true),
                    FileHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Verified = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RestoreOfBackupId = table.Column<int>(type: "int", nullable: true),
                    RestoreTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RestoreBy = table.Column<int>(type: "int", nullable: true),
                    RestoreNote = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DigitalSignature = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceIp = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ChangeVersion = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackupHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BackupHistories_BackupHistories_RestoreOfBackupId",
                        column: x => x.RestoreOfBackupId,
                        principalTable: "BackupHistories",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "calibration_audit",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    calibration_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    action = table.Column<int>(type: "int", nullable: false),
                    details = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    old_value = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    new_value = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    changed_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    note = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    source_ip = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    device_info = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    digital_signature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_calibration_audit", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "calibration_audit_log",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    calibration_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    action = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    old_value = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    new_value = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    changed_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    source_ip = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    note = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_calibration_audit_log", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "calibration_export_log",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    export_time = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    export_format = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    filter_component_id = table.Column<int>(type: "int", nullable: true),
                    filter_date_from = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    filter_date_to = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    file_path = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_calibration_export_log", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "calibrations",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    component_id = table.Column<int>(type: "int", nullable: false),
                    supplier_id = table.Column<int>(type: "int", nullable: true),
                    calibration_date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    next_due = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    cert_doc = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    result = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    comment = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    digital_signature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    last_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    last_modified_by_id = table.Column<int>(type: "int", nullable: true),
                    source_ip = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    approved = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    approved_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    approved_by_id = table.Column<int>(type: "int", nullable: true),
                    previous_calibration_id = table.Column<int>(type: "int", nullable: true),
                    next_calibration_id = table.Column<int>(type: "int", nullable: true),
                    change_version = table.Column<int>(type: "int", nullable: false),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ComponentId1 = table.Column<int>(type: "int", nullable: true),
                    MachineId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_calibrations", x => x.id);
                    table.ForeignKey(
                        name: "FK_calibrations_calibrations_next_calibration_id",
                        column: x => x.next_calibration_id,
                        principalTable: "calibrations",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_calibrations_calibrations_previous_calibration_id",
                        column: x => x.previous_calibration_id,
                        principalTable: "calibrations",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "capa_action_log",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    capa_case_id = table.Column<int>(type: "int", nullable: false),
                    action_type = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    performed_by_id = table.Column<int>(type: "int", nullable: true),
                    performed_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    note = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    digital_signature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    source_ip = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    change_version = table.Column<int>(type: "int", nullable: false),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_capa_action_log", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "capa_cases",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    component_id = table.Column<int>(type: "int", nullable: false),
                    date_open = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_close = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    assigned_to_id = table.Column<int>(type: "int", nullable: true),
                    priority = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    root_cause = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    corrective_action = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    preventive_action = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    actions = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    doc_file = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    digital_signature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    last_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    last_modified_by_id = table.Column<int>(type: "int", nullable: true),
                    approved = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    approved_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    approved_by_id = table.Column<int>(type: "int", nullable: true),
                    SourceIp = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status_id = table.Column<int>(type: "int", nullable: true),
                    root_cause_reference = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    linked_findings = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    notes = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    comments = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    change_version = table.Column<int>(type: "int", nullable: false),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ComponentId1 = table.Column<int>(type: "int", nullable: true),
                    SupplierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_capa_cases", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "capa_status_history",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    capa_case_id = table.Column<int>(type: "int", nullable: false),
                    old_status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    new_status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    changed_by = table.Column<int>(type: "int", nullable: true),
                    changed_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    note = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    source_ip = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    digital_signature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    change_version = table.Column<int>(type: "int", nullable: false),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_capa_status_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_capa_status_history_capa_cases_capa_case_id",
                        column: x => x.capa_case_id,
                        principalTable: "capa_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ChangeControls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateRequested = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RequestedById = table.Column<int>(type: "int", nullable: true),
                    MachineId = table.Column<int>(type: "int", nullable: true),
                    ComponentId = table.Column<int>(type: "int", nullable: true),
                    Reason = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ImpactAssessment = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RiskAssessment = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CapaCaseId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AssignedToId = table.Column<int>(type: "int", nullable: true),
                    DateReviewed = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ReviewedById = table.Column<int>(type: "int", nullable: true),
                    ReviewNotes = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateApproved = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ApprovedById = table.Column<int>(type: "int", nullable: true),
                    DateImplemented = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ImplementationNotes = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DocFile = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DigitalSignature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastModified = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastModifiedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeControls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChangeControls_capa_cases_CapaCaseId",
                        column: x => x.CapaCaseId,
                        principalTable: "capa_cases",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "component_devices",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    component_id = table.Column<int>(type: "int", nullable: false),
                    device_id = table.Column<int>(type: "int", nullable: false),
                    sensor_model_id = table.Column<int>(type: "int", nullable: true),
                    started_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ended_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_component_devices", x => x.id);
                    table.ForeignKey(
                        name: "FK_component_devices_iot_devices_device_id",
                        column: x => x.device_id,
                        principalTable: "iot_devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_component_devices_sensor_models_sensor_model_id",
                        column: x => x.sensor_model_id,
                        principalTable: "sensor_models",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "component_parts",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    component_id = table.Column<int>(type: "int", nullable: false),
                    part_id = table.Column<int>(type: "int", nullable: false),
                    nominal_qty = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_component_parts", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "component_qualifications",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    component_id = table.Column<int>(type: "int", nullable: true),
                    type = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    qualification_date = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    next_due = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    status = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    doc_file = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    signed_by = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    certificate_number = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    supplier_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_component_qualifications", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "components",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    machine_id = table.Column<int>(type: "int", nullable: false),
                    machine_name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    type = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sop_doc = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    install_date = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    digital_signature = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    last_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    last_modified_by_id = table.Column<int>(type: "int", nullable: false),
                    source_ip = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    serial_number = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    warranty_until = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    supplier = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    comments = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    lifecycle_state = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    change_version = table.Column<int>(type: "int", nullable: false),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_components", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "config_change_log",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    change_time = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    changed_by = table.Column<int>(type: "int", nullable: true),
                    config_name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    old_value = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    new_value = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    change_type = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    note = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    source_ip = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    action = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    details = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_config_change_log", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "contractor_intervention_audit",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    intervention_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    action = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    details = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    changed_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    source_ip = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    device_info = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    session_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    digital_signature = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    old_value = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    new_value = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    note = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contractor_intervention_audit", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "contractor_interventions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    contractor_id = table.Column<int>(type: "int", nullable: false),
                    component_id = table.Column<int>(type: "int", nullable: false),
                    intervention_date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    reason = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    result = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    gmp_compliance = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    doc_file = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    digital_signature = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    start_date = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    end_date = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    notes = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    last_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    last_modified_by_id = table.Column<int>(type: "int", nullable: false),
                    source_ip = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    comments = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    change_version = table.Column<int>(type: "int", nullable: false),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ExternalContractorId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contractor_interventions", x => x.id);
                    table.ForeignKey(
                        name: "FK_contractor_interventions_components_component_id",
                        column: x => x.component_id,
                        principalTable: "components",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ContractorAuditLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ExternalContractorId = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Action = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractorAuditLog", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "dashboards",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    dashboard_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    config_json = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dashboards", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "delegated_permissions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    permission_id = table.Column<int>(type: "int", nullable: false),
                    from_user_id = table.Column<int>(type: "int", nullable: false),
                    to_user_id = table.Column<int>(type: "int", nullable: false),
                    start_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    end_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    reason = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    is_revoked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    approved_by = table.Column<int>(type: "int", nullable: true),
                    note = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    change_version = table.Column<int>(type: "int", nullable: false),
                    digital_signature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    session_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    source_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_delegated_permissions", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "delete_log",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    deleted_by = table.Column<int>(type: "int", nullable: true),
                    table_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    record_id = table.Column<int>(type: "int", nullable: true),
                    delete_type = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reason = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    recoverable = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    backup_file = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    source_ip = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    note = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    action = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    details = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_delete_log", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    manager_id = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_departments", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "deviation_audit",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    deviation_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    action = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    details = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    changed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    device_info = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    source_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    session_id = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    digital_signature = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    regulatory_status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ai_anomaly_score = table.Column<decimal>(type: "decimal(5,4)", nullable: true),
                    validated = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    audit_trail_id = table.Column<int>(type: "int", nullable: true),
                    comment = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    old_value = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    new_value = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    signature_type = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    signature_method = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    signature_valid = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    export_status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    export_time = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    exported_by = table.Column<int>(type: "int", nullable: true),
                    restored_from_snapshot = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    restoration_reference = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    approval_status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    approval_time = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    approved_by = table.Column<int>(type: "int", nullable: true),
                    deleted = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    deleted_by = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    related_file = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    related_photo = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    iot_event_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deviation_audit", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Deviations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReportedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ReportedById = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsCritical = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Status = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AssignedInvestigatorId = table.Column<int>(type: "int", nullable: true),
                    AssignedInvestigatorName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InvestigationStartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RootCause = table.Column<string>(type: "varchar(800)", maxLength: 800, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CorrectiveActions = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LinkedCapaId = table.Column<int>(type: "int", nullable: true),
                    ClosureComment = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClosedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    AttachmentIds = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DigitalSignature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RiskScore = table.Column<int>(type: "int", nullable: false),
                    AnomalyScore = table.Column<double>(type: "double", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastModifiedById = table.Column<int>(type: "int", nullable: true),
                    SourceIp = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AuditNote = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deviations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Deviations_capa_cases_LinkedCapaId",
                        column: x => x.LinkedCapaId,
                        principalTable: "capa_cases",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "digital_signatures",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    table_name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    record_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    signature_hash = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    method = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    signed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    device_info = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ip_address = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    note = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    public_key = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SparePartId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_digital_signatures", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "document_audit_events",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    documentcontrol_id = table.Column<int>(type: "int", nullable: false),
                    timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    action = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_audit_events", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "document_versions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    documentcontrol_id = table.Column<int>(type: "int", nullable: false),
                    revision = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    file_path = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_id = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    status = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    note = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_versions", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "documentcontrol",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    title = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    document_type = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    revision = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    file_path = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    attachments = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    revision_history = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status_history = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    linked_change_controls = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    expiry_date = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    device_info = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    session_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ip_address = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_id = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    approved_by_id = table.Column<int>(type: "int", nullable: true),
                    approved_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    assigned_to = table.Column<int>(type: "int", nullable: true),
                    digital_signature = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_locked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    related_module = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    related_record_id = table.Column<int>(type: "int", nullable: true),
                    QualificationId = table.Column<int>(type: "int", nullable: true),
                    RiskAssessmentId = table.Column<int>(type: "int", nullable: true),
                    TrainingRecordId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documentcontrol", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "entity_audit_log",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    source_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    device_info = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    entity = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    entity_id = table.Column<int>(type: "int", nullable: true),
                    action = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    details = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    session_id = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    digital_signature = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    signature_hash = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entity_audit_log", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "export_print_log",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    export_time = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    format = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    table_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    filter_used = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    file_path = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    source_ip = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    note = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    details = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_export_print_log", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ExternalContractors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CompanyName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RegistrationNumber = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "varchar(48)", maxLength: 48, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContactPerson = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Phone = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Address = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Certificates = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsBlacklisted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    BlacklistReason = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RiskScore = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    SupplierId = table.Column<int>(type: "int", nullable: true),
                    DigitalSignature = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalContractors", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ForensicUserChangeLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ChangedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ChangedBy = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TargetUserId = table.Column<int>(type: "int", nullable: true),
                    OldRole = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NewRole = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OldStatus = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    NewStatus = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    Note = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceIp = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DigitalSignature = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChangeVersion = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForensicUserChangeLogs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "incident_audit",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    incident_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    action = table.Column<int>(type: "int", nullable: false),
                    old_value = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    new_value = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    action_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    note = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    source_ip = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    digital_signature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    capa_id = table.Column<int>(type: "int", nullable: true),
                    work_order_id = table.Column<int>(type: "int", nullable: true),
                    device_info = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    integrity_hash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    inspector_note = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incident_audit", x => x.id);
                    table.ForeignKey(
                        name: "FK_incident_audit_capa_cases_capa_id",
                        column: x => x.capa_id,
                        principalTable: "capa_cases",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "IncidentActions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IncidentId = table.Column<int>(type: "int", nullable: false),
                    ActionType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActionAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Result = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RootCause = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DigitalSignature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceIp = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastModified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastModifiedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentActions", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "IncidentAuditLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IncidentReportId = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Action = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentAuditLog", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "IncidentLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DetectedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ReportedById = table.Column<int>(type: "int", nullable: true),
                    Severity = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Resolved = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ResolvedById = table.Column<int>(type: "int", nullable: true),
                    ActionsTaken = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FollowUp = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceIp = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DigitalSignature = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RootCause = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CapaCaseId = table.Column<int>(type: "int", nullable: true),
                    Attachments = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastModified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastModifiedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncidentLogs_capa_cases_CapaCaseId",
                        column: x => x.CapaCaseId,
                        principalTable: "capa_cases",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "IncidentReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Title = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IncidentType = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Severity = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DetectedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ReportedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReportedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ReportedById = table.Column<int>(type: "int", nullable: true),
                    AssignedTo = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AssignedToId = table.Column<int>(type: "int", nullable: true),
                    Area = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RootCause = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ImpactScore = table.Column<int>(type: "int", nullable: true),
                    LinkedCAPA = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    MachineId = table.Column<int>(type: "int", nullable: true),
                    ComponentId = table.Column<int>(type: "int", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ResolvedById = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeviceInfo = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SessionId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IpAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentReports", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "incidents",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    type = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    priority = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    detected_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    reported_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    reported_by_id = table.Column<int>(type: "int", nullable: true),
                    assigned_to_id = table.Column<int>(type: "int", nullable: true),
                    work_order_id = table.Column<int>(type: "int", nullable: true),
                    capa_case_id = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    root_cause = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    closed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    closed_by_id = table.Column<int>(type: "int", nullable: true),
                    digital_signature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    last_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    last_modified_by_id = table.Column<int>(type: "int", nullable: true),
                    source_ip = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    notes = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    anomaly_score = table.Column<double>(type: "double", nullable: true),
                    risk_level = table.Column<int>(type: "int", nullable: false),
                    assigned_investigator = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    classification = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    linked_deviation_id = table.Column<int>(type: "int", nullable: true),
                    linked_capa_id = table.Column<int>(type: "int", nullable: true),
                    closure_comment = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_critical = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incidents", x => x.id);
                    table.ForeignKey(
                        name: "FK_incidents_capa_cases_capa_case_id",
                        column: x => x.capa_case_id,
                        principalTable: "capa_cases",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "inspection",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    inspection_date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    inspector_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    type = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    result = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    related_machine_id = table.Column<int>(type: "int", nullable: true),
                    doc_file = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    notes = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    digital_signature = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    last_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    last_modified_by_id = table.Column<int>(type: "int", nullable: false),
                    source_ip = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    capa_case_id = table.Column<int>(type: "int", nullable: true),
                    audit_finding_ref = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    attachments_json = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    approval_workflow_json = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ai_analysis = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SupplierId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inspection", x => x.id);
                    table.ForeignKey(
                        name: "FK_inspection_capa_cases_capa_case_id",
                        column: x => x.capa_case_id,
                        principalTable: "capa_cases",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "inventory_transactions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    part_id = table.Column<int>(type: "int", nullable: false),
                    warehouse_id = table.Column<int>(type: "int", nullable: false),
                    transaction_type = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    quantity = table.Column<int>(type: "int", nullable: false),
                    transaction_date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    performed_by = table.Column<int>(type: "int", nullable: true),
                    related_document = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    note = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_transactions", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "iot_anomaly_log",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    sensor_data_id = table.Column<int>(type: "int", nullable: true),
                    detected_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    detected_by = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    severity = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    resolved = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    resolved_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    resolution_note = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iot_anomaly_log", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "iot_sensor_data",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    device_id = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    component_id = table.Column<int>(type: "int", nullable: true),
                    data_type = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    value = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    unit = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    status = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    anomaly_detected = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    processed = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    note = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    unit_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iot_sensor_data", x => x.id);
                    table.ForeignKey(
                        name: "FK_iot_sensor_data_measurement_units_unit_id",
                        column: x => x.unit_id,
                        principalTable: "measurement_units",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "irregularities_log",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    related_type = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    related_id = table.Column<int>(type: "int", nullable: true),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    detected_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    status = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    note = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    details = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_irregularities_log", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LogEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Username = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserRole = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Action = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Entity = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RecordId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FieldName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OldValue = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NewValue = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Details = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsApproved = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    DigitalSignature = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EntryHash = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceIp = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeviceInfo = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SessionId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Severity = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RelatedCaseId = table.Column<int>(type: "int", nullable: true),
                    RelatedCaseType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Regulator = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GeoLocation = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AnomalyScore = table.Column<double>(type: "double", nullable: true),
                    IsAutomated = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Note = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LinkedAttachmentPath = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PreviousLogEntryId = table.Column<int>(type: "int", nullable: true),
                    NextLogEntryId = table.Column<int>(type: "int", nullable: true),
                    ExtensionJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogEntries", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LoginAttemptLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AttemptTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    UsernameAttempt = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Success = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FailReason = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeviceInfo = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceIp = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GeoLocation = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DigitalSignature = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LockedOut = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FailedAttemptsCount = table.Column<int>(type: "int", nullable: false),
                    SessionId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TwoFactorAttempted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TwoFactorSuccess = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    AnomalyScore = table.Column<double>(type: "double", nullable: true),
                    IsAutomated = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Note = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PreviousLoginAttemptId = table.Column<int>(type: "int", nullable: true),
                    NextLoginAttemptId = table.Column<int>(type: "int", nullable: true),
                    ExtensionJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginAttemptLogs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MachineComponents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MachineId = table.Column<int>(type: "int", nullable: true),
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Model = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InstallDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    PurchaseDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    WarrantyUntil = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    WarrantyExpiry = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SerialNumber = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Supplier = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RfidTag = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IoTDeviceId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LifecyclePhase = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsCritical = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Note = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DigitalSignature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notes = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SopDoc = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastModified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastModifiedById = table.Column<int>(type: "int", nullable: false),
                    SourceIp = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Documents = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ChangeVersion = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MachineComponents", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MachineLifecycleEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MachineId = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    EventDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PerformedById = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DocFile = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DigitalSignature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceIp = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastModified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastModifiedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MachineLifecycleEvents", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "machines",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    code = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    machine_type = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    model = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    manufacturer = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    location = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    responsible_party = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    install_date = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    procurement_date = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    warranty_until = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    status = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    urs_doc = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    decommission_date = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    decommission_reason = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    serial_number = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    acquisition_cost = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    rfid_tag = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    qr_code = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    internal_code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    qr_payload = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    iot_device_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cloud_device_guid = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_critical = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    lifecycle_phase = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    note = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    last_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    last_modified_by_id = table.Column<int>(type: "int", nullable: false),
                    digital_signature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_machines", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ppm_plans",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    machine_id = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    plan_json = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    effective_from = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    effective_to = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    last_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ppm_plans", x => x.id);
                    table.ForeignKey(
                        name: "FK_ppm_plans_machines_machine_id",
                        column: x => x.machine_id,
                        principalTable: "machines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MaintenanceExecutionLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PlanId = table.Column<int>(type: "int", nullable: false),
                    ExecutedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExecutedById = table.Column<int>(type: "int", nullable: false),
                    MaintenanceType = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Result = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RootCause = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PerformedActions = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UsedParts = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notes = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChecklistFile = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DigitalSignature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EntryHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeviceInfo = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceIp = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastModifiedById = table.Column<int>(type: "int", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsApproved = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ApprovedById = table.Column<int>(type: "int", nullable: true),
                    RelatedCaseId = table.Column<int>(type: "int", nullable: true),
                    RelatedCaseType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IoTDeviceId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AnomalyScore = table.Column<double>(type: "double", nullable: true),
                    InspectorNote = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsAutomated = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceExecutionLogs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mobile_device_log",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    device_info = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    login_time = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    logout_time = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    os_version = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    location = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ip_address = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mobile_device_log", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "notification_queue",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    template_id = table.Column<int>(type: "int", nullable: true),
                    recipient_user_id = table.Column<int>(type: "int", nullable: true),
                    channel = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    payload = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    scheduled_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    sent_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    status = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    last_error = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_queue", x => x.id);
                    table.ForeignKey(
                        name: "FK_notification_queue_notification_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "notification_templates",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "NotificationChannelEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    NotificationId = table.Column<int>(type: "int", nullable: false),
                    Channel = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EventTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Event = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Target = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Response = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceIp = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeviceInfo = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NotificationLogId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationChannelEvents", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "NotificationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    NotificationType = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RelatedType = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RelatedId = table.Column<int>(type: "int", nullable: true),
                    Message = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SentAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DeliveredAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ReadAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ActionedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Priority = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEscalated = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FallbackChannel = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DigitalSignature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceIp = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GeoLocation = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AnomalyScore = table.Column<double>(type: "double", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastModifiedById = table.Column<int>(type: "int", nullable: true),
                    AuditNote = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationLogs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "part_bom",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    parent_part_id = table.Column<int>(type: "int", nullable: false),
                    child_part_id = table.Column<int>(type: "int", nullable: false),
                    quantity = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_part_bom", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PartChangeLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PartId = table.Column<int>(type: "int", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ChangedById = table.Column<int>(type: "int", nullable: false),
                    FieldName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OldValue = table.Column<string>(type: "varchar(2048)", maxLength: 2048, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NewValue = table.Column<string>(type: "varchar(2048)", maxLength: 2048, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reason = table.Column<string>(type: "varchar(400)", maxLength: 400, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reference = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DigitalSignature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceIp = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AnomalyScore = table.Column<double>(type: "double", nullable: true),
                    Note = table.Column<string>(type: "varchar(400)", maxLength: 400, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartChangeLogs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Parts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Category = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Barcode = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RFID = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SerialOrLot = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DefaultSupplierId = table.Column<int>(type: "int", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    Stock = table.Column<int>(type: "int", nullable: false),
                    MinStockAlert = table.Column<int>(type: "int", nullable: true),
                    Location = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Image = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Images = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Documents = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WarrantyUntil = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Blocked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RegulatoryCertificates = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DigitalSignature = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastModified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastModifiedById = table.Column<int>(type: "int", nullable: false),
                    SourceIp = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AnomalyScore = table.Column<double>(type: "double", nullable: true),
                    Supplier = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parts", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PartSupplierPrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PartId = table.Column<int>(type: "int", nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    SupplierName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UnitPrice = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    VatPercent = table.Column<double>(type: "double", nullable: false),
                    PriceWithVat = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    Currency = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Region = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DiscountPercent = table.Column<double>(type: "double", nullable: true),
                    Surcharge = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    MinOrderQuantity = table.Column<int>(type: "int", nullable: true),
                    LeadTimeDays = table.Column<int>(type: "int", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ValidUntil = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsBlocked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    BlockReason = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContractDocument = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastModified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastModifiedById = table.Column<int>(type: "int", nullable: false),
                    DigitalSignature = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceIp = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AnomalyScore = table.Column<double>(type: "double", nullable: true),
                    Note = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SparePartId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartSupplierPrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartSupplierPrices_Parts_PartId",
                        column: x => x.PartId,
                        principalTable: "Parts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PartUsageAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PartUsageId = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Action = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartUsageAuditLogs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PartUsages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    WorkOrderId = table.Column<int>(type: "int", nullable: true),
                    MachineComponentId = table.Column<int>(type: "int", nullable: true),
                    PartId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    SerialNumber = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LotNumber = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExpiryDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UnitCost = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    TotalCost = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    SupplierId = table.Column<int>(type: "int", nullable: true),
                    IssuedById = table.Column<int>(type: "int", nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Warranty = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartUsages_MachineComponents_MachineComponentId",
                        column: x => x.MachineComponentId,
                        principalTable: "MachineComponents",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PartUsages_Parts_PartId",
                        column: x => x.PartId,
                        principalTable: "Parts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "permission_change_log",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    changed_by = table.Column<int>(type: "int", nullable: false),
                    change_type = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    role_id = table.Column<int>(type: "int", nullable: true),
                    permission_id = table.Column<int>(type: "int", nullable: true),
                    action = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reason = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    change_time = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    source_ip = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    session_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    details = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permission_change_log", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "permission_requests",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    permission_id = table.Column<int>(type: "int", nullable: false),
                    requested_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    reason = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reviewed_by = table.Column<int>(type: "int", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    review_comment = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permission_requests", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PermissionRole",
                columns: table => new
                {
                    PermissionsId = table.Column<int>(type: "int", nullable: false),
                    RolesId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionRole", x => new { x.PermissionsId, x.RolesId });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    permission_type = table.Column<int>(type: "int", nullable: true),
                    code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    group = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    parent_id = table.Column<int>(type: "int", nullable: true),
                    compliance_tags = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    critical = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    last_modified = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    last_modified_by_id = table.Column<int>(type: "int", nullable: true),
                    digital_signature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    change_version = table.Column<int>(type: "int", nullable: false),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    note = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.id);
                    table.ForeignKey(
                        name: "FK_permissions_permissions_parent_id",
                        column: x => x.parent_id,
                        principalTable: "permissions",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PermissionUser",
                columns: table => new
                {
                    PermissionsId = table.Column<int>(type: "int", nullable: false),
                    UsersId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionUser", x => new { x.PermissionsId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_PermissionUser_permissions_PermissionsId",
                        column: x => x.PermissionsId,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "photo",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    work_order_id = table.Column<int>(type: "int", nullable: true),
                    component_id = table.Column<int>(type: "int", nullable: true),
                    file_path = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    comment = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    uploaded_by_id = table.Column<int>(type: "int", nullable: true),
                    uploaded_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    hash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    watermark = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_approved = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    approved_by_id = table.Column<int>(type: "int", nullable: true),
                    approved_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    digital_signature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    source_ip = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    session_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    original_file_name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    recognition_data = table.Column<string>(type: "varchar(4096)", maxLength: 4096, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    latitude = table.Column<double>(type: "double", nullable: true),
                    longitude = table.Column<double>(type: "double", nullable: true),
                    device_info = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    version = table.Column<int>(type: "int", nullable: true),
                    is_original = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    previous_photo_id = table.Column<int>(type: "int", nullable: true),
                    next_photo_id = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    note = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    last_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    last_modified_by_id = table.Column<int>(type: "int", nullable: true),
                    IncidentReportId = table.Column<int>(type: "int", nullable: true),
                    MachineComponentId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_photo", x => x.id);
                    table.ForeignKey(
                        name: "FK_photo_IncidentReports_IncidentReportId",
                        column: x => x.IncidentReportId,
                        principalTable: "IncidentReports",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_photo_MachineComponents_MachineComponentId",
                        column: x => x.MachineComponentId,
                        principalTable: "MachineComponents",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_photo_components_component_id",
                        column: x => x.component_id,
                        principalTable: "components",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_photo_photo_next_photo_id",
                        column: x => x.next_photo_id,
                        principalTable: "photo",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_photo_photo_previous_photo_id",
                        column: x => x.previous_photo_id,
                        principalTable: "photo",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PreventiveMaintenancePlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(1500)", maxLength: 1500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MachineId = table.Column<int>(type: "int", nullable: true),
                    ComponentId = table.Column<int>(type: "int", nullable: true),
                    Frequency = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChecklistFile = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResponsibleUserId = table.Column<int>(type: "int", nullable: true),
                    LastExecuted = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    NextDue = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RiskScore = table.Column<double>(type: "double", nullable: true),
                    AiRecommendation = table.Column<string>(type: "varchar(2048)", maxLength: 2048, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DigitalSignature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastModified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastModifiedById = table.Column<int>(type: "int", nullable: true),
                    SourceIp = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SessionId = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GeoLocation = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Attachments = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Version = table.Column<int>(type: "int", nullable: true),
                    PreviousVersionId = table.Column<int>(type: "int", nullable: true),
                    IsActiveVersion = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsAutomated = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RequiresNotification = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AnomalyScore = table.Column<double>(type: "double", nullable: true),
                    Note = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreventiveMaintenancePlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreventiveMaintenancePlans_PreventiveMaintenancePlans_Previo~",
                        column: x => x.PreviousVersionId,
                        principalTable: "PreventiveMaintenancePlans",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PreventiveMaintenancePlans_components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "components",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_PreventiveMaintenancePlans_machines_MachineId",
                        column: x => x.MachineId,
                        principalTable: "machines",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ProductRecallLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RelatedType = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RelatedId = table.Column<int>(type: "int", nullable: false),
                    RecallDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Reason = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RecalledById = table.Column<int>(type: "int", nullable: false),
                    RecallAction = table.Column<string>(type: "varchar(1500)", maxLength: 1500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RecallResult = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClosedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ClosedById = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Attachments = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DigitalSignature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceIp = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SessionId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AnomalyScore = table.Column<double>(type: "double", nullable: true),
                    Regulator = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LinkedIncidentId = table.Column<int>(type: "int", nullable: true),
                    LinkedCapaId = table.Column<int>(type: "int", nullable: true),
                    LinkedInspectionId = table.Column<int>(type: "int", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastModifiedById = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductRecallLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductRecallLogs_capa_cases_LinkedCapaId",
                        column: x => x.LinkedCapaId,
                        principalTable: "capa_cases",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_ProductRecallLogs_incidents_LinkedIncidentId",
                        column: x => x.LinkedIncidentId,
                        principalTable: "incidents",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_ProductRecallLogs_inspection_LinkedInspectionId",
                        column: x => x.LinkedInspectionId,
                        principalTable: "inspection",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "QualificationAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    QualificationId = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Action = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualificationAuditLogs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Qualifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MachineId = table.Column<int>(type: "int", nullable: true),
                    ComponentId = table.Column<int>(type: "int", nullable: true),
                    SupplierId = table.Column<int>(type: "int", nullable: true),
                    QualifiedById = table.Column<int>(type: "int", nullable: true),
                    ApprovedById = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DigitalSignature = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CertificateNumber = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Qualifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Qualifications_MachineComponents_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "MachineComponents",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Qualifications_machines_MachineId",
                        column: x => x.MachineId,
                        principalTable: "machines",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "quality_events",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    event_type = table.Column<string>(type: "varchar(14)", maxLength: 14, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_open = table.Column<DateTime>(type: "date", nullable: true),
                    date_close = table.Column<DateTime>(type: "date", nullable: true),
                    description = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    related_machine = table.Column<int>(type: "int", nullable: true),
                    related_component = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    actions = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    doc_file = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    digital_signature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by_id = table.Column<int>(type: "int", nullable: true),
                    last_modified_by_id = table.Column<int>(type: "int", nullable: true),
                    last_modified = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    status_id = table.Column<int>(type: "int", nullable: true),
                    type_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quality_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_qe_component",
                        column: x => x.related_component,
                        principalTable: "MachineComponents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_qe_machine",
                        column: x => x.related_machine,
                        principalTable: "machines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "report_schedule",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    report_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    schedule_type = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    format = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    recipients = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    last_generated = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    next_due = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    status = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    generated_by = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_schedule", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Title = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GeneratedOn = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    GeneratedById = table.Column<int>(type: "int", nullable: false),
                    FilePath = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DigitalSignature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReportType = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VersionNo = table.Column<int>(type: "int", nullable: false),
                    Parameters = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Attachments = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LinkedEntityId = table.Column<int>(type: "int", nullable: true),
                    LinkedEntityType = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Regulator = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AnomalyScore = table.Column<double>(type: "double", nullable: true),
                    AnalyticsJson = table.Column<string>(type: "varchar(2048)", maxLength: 2048, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastModified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastModifiedById = table.Column<int>(type: "int", nullable: false),
                    SourceIp = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SessionId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeviceInfo = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GeoLocation = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RequalificationSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ComponentId = table.Column<int>(type: "int", nullable: false),
                    LastQualified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    NextDue = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Method = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResponsibleUserId = table.Column<int>(type: "int", nullable: true),
                    ProtocolFile = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notes = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DigitalSignature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastModified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastModifiedById = table.Column<int>(type: "int", nullable: false),
                    SourceIp = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SessionId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GeoLocation = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AttachmentsJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Regulator = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AnomalyScore = table.Column<double>(type: "double", nullable: true),
                    AnalyticsJson = table.Column<string>(type: "varchar(2048)", maxLength: 2048, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RelatedCaseId = table.Column<int>(type: "int", nullable: true),
                    RelatedCaseType = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequalificationSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequalificationSchedules_components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "components",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RiskAssessmentAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RiskAssessmentId = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Action = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskAssessmentAuditLogs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RiskAssessments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Category = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Area = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AssessedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AssessedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Probability = table.Column<int>(type: "int", nullable: false),
                    Detection = table.Column<int>(type: "int", nullable: false),
                    RiskScore = table.Column<int>(type: "int", nullable: true),
                    RiskLevel = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Mitigation = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActionPlan = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OwnerId = table.Column<int>(type: "int", nullable: true),
                    ApprovedById = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ReviewDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DigitalSignature = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Attachments = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeviceInfo = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SessionId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IpAddress = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskAssessments", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "role_audit",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    role_id = table.Column<int>(type: "int", nullable: true),
                    action = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    source_ip = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    device_info = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    session_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    details = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_audit", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    role_id = table.Column<int>(type: "int", nullable: false),
                    permission_id = table.Column<int>(type: "int", nullable: false),
                    allowed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    assigned_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    assigned_by = table.Column<int>(type: "int", nullable: true),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    reason = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    change_version = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    digital_signature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    session_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    source_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    note = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => new { x.role_id, x.permission_id });
                    table.ForeignKey(
                        name: "FK_role_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    org_unit = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    compliance_tags = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by_id = table.Column<int>(type: "int", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    last_modified_by_id = table.Column<int>(type: "int", nullable: true),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    notes = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RoleUser",
                columns: table => new
                {
                    RolesId = table.Column<int>(type: "int", nullable: false),
                    UsersId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleUser", x => new { x.RolesId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_RoleUser_roles_RolesId",
                        column: x => x.RolesId,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "scheduled_job_audit_log",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    scheduled_job_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    action = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    old_value = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    new_value = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    changed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    source_ip = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    device_info = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    session_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    digital_signature = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    note = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    details = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scheduled_job_audit_log", x => x.id);
                    table.ForeignKey(
                        name: "FK_scheduled_job_audit_log_ScheduledJobs_scheduled_job_id",
                        column: x => x.scheduled_job_id,
                        principalTable: "ScheduledJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SchemaMigrationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MigrationTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    MigratedById = table.Column<int>(type: "int", nullable: false),
                    Username = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SchemaVersion = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MigrationScript = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RollbackScript = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceIp = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeviceInfo = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Success = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DigitalSignature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EntryHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AttachmentsJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Regulator = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AnomalyScore = table.Column<double>(type: "double", nullable: true),
                    Note = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchemaMigrationLogs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SensitiveDataAccessLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Username = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AccessTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TableName = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RecordId = table.Column<int>(type: "int", nullable: false),
                    FieldName = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AccessType = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceIp = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeviceInfo = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SessionId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Purpose = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApprovedById = table.Column<int>(type: "int", nullable: true),
                    ApproverName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApprovedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ApprovalMethod = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DigitalSignature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EntryHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GeoLocation = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Severity = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AnomalyScore = table.Column<double>(type: "double", nullable: true),
                    Note = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensitiveDataAccessLogs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sensor_data_log",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    component_id = table.Column<int>(type: "int", nullable: false),
                    sensor_type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sensor_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    value = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    unit = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    device_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    session_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    location = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    geo_coordinates = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    data_hash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    chain_hash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    digital_signature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    comment = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    data_file = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    approved_by_id = table.Column<int>(type: "int", nullable: true),
                    approved_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    anomaly_score = table.Column<double>(type: "double", nullable: true),
                    is_anomaly = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    tag = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    note = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sensor_data_log", x => x.id);
                    table.ForeignKey(
                        name: "FK_sensor_data_log_components_component_id",
                        column: x => x.component_id,
                        principalTable: "components",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SessionLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    SessionId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LoginAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LogoutAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IpAddress = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeviceInfo = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsImpersonated = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ImpersonatedById = table.Column<int>(type: "int", nullable: true),
                    IsTemporaryEscalation = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Note = table.Column<string>(type: "varchar(400)", maxLength: 400, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId1 = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionLogs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SettingAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SettingId = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Action = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettingAuditLogs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Key = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Value = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DefaultValue = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ValueType = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MinValue = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaxValue = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Category = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Subcategory = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsSensitive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsGlobal = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    RoleId = table.Column<int>(type: "int", nullable: true),
                    ApprovedById = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DigitalSignature = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedById = table.Column<int>(type: "int", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Settings_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SettingVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SettingId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChangedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ChangedById = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettingVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SettingVersions_Settings_SettingId",
                        column: x => x.SettingId,
                        principalTable: "Settings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sop_document_log",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    sop_document_id = table.Column<int>(type: "int", nullable: true),
                    action = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    performed_by = table.Column<int>(type: "int", nullable: true),
                    performed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    note = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sop_document_log", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SopDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(400)", maxLength: 400, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Process = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Language = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateIssued = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateExpiry = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    NextReviewDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    FilePath = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Attachments = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResponsibleUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    VersionNo = table.Column<int>(type: "int", nullable: false),
                    FileHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DigitalSignature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChainHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApproverIds = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApprovalTimestamps = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReviewNotes = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PdfMetadata = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RelatedType = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RelatedId = table.Column<int>(type: "int", nullable: true),
                    Comment = table.Column<string>(type: "varchar(400)", maxLength: 400, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastModified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastModifiedById = table.Column<int>(type: "int", nullable: false),
                    SourceIp = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AiTags = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SopDocuments", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SpareParts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DefaultSupplierId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DigitalSignature = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastModified = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastModifiedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpareParts", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SqlQueryAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Username = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QueryTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    QueryText = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QueryType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TableName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AffectedTables = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RecordIds = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AffectedRows = table.Column<int>(type: "int", nullable: true),
                    Success = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DurationMs = table.Column<int>(type: "int", nullable: true),
                    SourceIp = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClientApp = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SessionId = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeviceInfo = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsAutomated = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ExportType = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OldDataSnapshot = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NewDataSnapshot = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContextDetails = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EntryHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DigitalSignature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChainHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Severity = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AnomalyScore = table.Column<double>(type: "double", nullable: true),
                    Note = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GeoLocation = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RelatedCaseId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SqlQueryAuditLogs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "stock_levels",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    part_id = table.Column<int>(type: "int", nullable: false),
                    warehouse_id = table.Column<int>(type: "int", nullable: false),
                    quantity = table.Column<int>(type: "int", nullable: false),
                    min_threshold = table.Column<int>(type: "int", nullable: false),
                    max_threshold = table.Column<int>(type: "int", nullable: false),
                    auto_reorder_triggered = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    days_below_min = table.Column<int>(type: "int", nullable: false),
                    alarm_status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    anomaly_score = table.Column<double>(type: "double", nullable: true),
                    last_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    last_modified_by_id = table.Column<int>(type: "int", nullable: false),
                    source_ip = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    geo_location = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    comment = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    digital_signature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    entry_hash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    old_state_snapshot = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    new_state_snapshot = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_automated = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    session_id = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    related_case_id = table.Column<int>(type: "int", nullable: true),
                    related_case_type = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SparePartId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_levels", x => x.id);
                    table.ForeignKey(
                        name: "FK_stock_levels_SpareParts_SparePartId",
                        column: x => x.SparePartId,
                        principalTable: "SpareParts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "fk_sl_part",
                        column: x => x.part_id,
                        principalTable: "Parts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "StockChangeLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PartId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ChangedById = table.Column<int>(type: "int", nullable: false),
                    ChangeType = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Delta = table.Column<int>(type: "int", nullable: false),
                    NewStock = table.Column<int>(type: "int", nullable: false),
                    BatchNumber = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reason = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reference = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DigitalSignature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceIp = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Attachment = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "varchar(400)", maxLength: 400, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AnomalyScore = table.Column<double>(type: "double", nullable: true),
                    IsCorrection = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockChangeLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockChangeLogs_Parts_PartId",
                        column: x => x.PartId,
                        principalTable: "Parts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "supplier",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    vat_number = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    address = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    city = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    country = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    email = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    phone = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    website = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    supplier_type = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    notes = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    contract_file = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cooperation_start = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    cooperation_end = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    gmp_comment = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    digital_signature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    entry_hash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    last_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    last_modified_by_id = table.Column<int>(type: "int", nullable: false),
                    source_ip = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_qualified = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    last_qualification_check = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    external_certification_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    registered_authorities = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    risk_level = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    anomaly_score = table.Column<double>(type: "double", nullable: true),
                    session_id = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_ai_flagged = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "supplier_risk_audit",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    supplier_id = table.Column<int>(type: "int", nullable: true),
                    audit_date = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    score = table.Column<int>(type: "int", nullable: true),
                    performed_by = table.Column<int>(type: "int", nullable: true),
                    findings = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    corrective_actions = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_risk_audit", x => x.id);
                    table.ForeignKey(
                        name: "FK_supplier_risk_audit_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "system_event_log",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    event_time = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    username = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    event_type = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    table_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    record_id = table.Column<int>(type: "int", nullable: true),
                    field_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    old_value = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    new_value = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    digital_signature = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    entry_hash = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    source_ip = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    mac_address = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    device_info = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    geo_location = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    session_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    anomaly_score = table.Column<double>(type: "double", nullable: true),
                    severity = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    regulator = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    related_case_id = table.Column<int>(type: "int", nullable: true),
                    related_case_type = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    processed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    details = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChangeVersion = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_event_log", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "system_parameters",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    param_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    param_value = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    updated_by = table.Column<int>(type: "int", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    note = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    digital_signature = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_parameters", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TrainingAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TrainingRecordId = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Action = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingAuditLogs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TrainingLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TrainingType = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TrainingName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Passed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Trainer = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AdditionalTrainers = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DocumentPath = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CertificationStatus = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExpiryDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RetrainingDue = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DigitalSignature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceIp = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastModified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastModifiedById = table.Column<int>(type: "int", nullable: false),
                    EntryHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReminderStatus = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TestPassed = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    TestScore = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RegulatoryRequirement = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingLogs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TrainingRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "varchar(48)", maxLength: 48, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Format = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TrainingDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TrainerId = table.Column<int>(type: "int", nullable: true),
                    TraineeId = table.Column<int>(type: "int", nullable: true),
                    RoleId = table.Column<int>(type: "int", nullable: true),
                    DocumentId = table.Column<int>(type: "int", nullable: true),
                    CertificateNumber = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TestScore = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    IsELearning = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TraineeSignature = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TrainerSignature = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingRecords_documentcontrol_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "documentcontrol",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_TrainingRecords_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    username = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    password = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    full_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    role = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    email = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    phone = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    digital_signature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    department_id = table.Column<int>(type: "int", nullable: true),
                    department_name = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_two_factor_enabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    last_login = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    is_locked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    failed_login_attempts = table.Column<int>(type: "int", nullable: false),
                    last_failed_login = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    password_reset_required = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    external_provider_id = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    external_provider_type = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    federated_unique_id = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    preferred_culture = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    last_change_signature = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    global_federated_id = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    public_key = table.Column<string>(type: "LONGTEXT", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    last_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn),
                    last_modified_by_id = table.Column<int>(type: "int", nullable: true),
                    change_version = table.Column<int>(type: "int", nullable: false),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    privacy_consent_version = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    privacy_consent_date = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    custom_attributes = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    security_anomaly_score = table.Column<double>(type: "double", nullable: true),
                    is_system_account = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    notification_channel = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SopDocumentId = table.Column<int>(type: "int", nullable: true),
                    TrainingRecordId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.ForeignKey(
                        name: "FK_users_SopDocuments_SopDocumentId",
                        column: x => x.SopDocumentId,
                        principalTable: "SopDocuments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_users_TrainingRecords_TrainingRecordId",
                        column: x => x.TrainingRecordId,
                        principalTable: "TrainingRecords",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_users_users_last_modified_by_id",
                        column: x => x.last_modified_by_id,
                        principalTable: "users",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "user_audit",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    action = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    source_ip = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    device_info = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    session_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    details = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_audit", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_audit_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "user_esignatures",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    signed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    action = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    table_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    record_id = table.Column<int>(type: "int", nullable: true),
                    signature_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    method = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ip_address = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_esignatures", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_esignatures_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "user_login_audit",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    login_time = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    logout_time = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ip_address = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    device_info = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    success = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    two_factor_ok = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    sso_used = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    biometric_used = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    session_token = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    geo_location = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    risk_score = table.Column<double>(type: "double", nullable: true),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reason = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    digital_signature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    note = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_login_audit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_login_audit_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "user_permissions",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false),
                    permission_id = table.Column<int>(type: "int", nullable: false),
                    allowed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    assigned_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    assigned_by = table.Column<int>(type: "int", nullable: true),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    reason = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    change_version = table.Column<int>(type: "int", nullable: false),
                    digital_signature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    session_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    source_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    note = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_permissions", x => new { x.user_id, x.permission_id });
                    table.ForeignKey(
                        name: "FK_user_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_permissions_users_assigned_by",
                        column: x => x.assigned_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_user_permissions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "user_role_assignments",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    role_id = table.Column<int>(type: "int", nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    granted_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    granted_by = table.Column<int>(type: "int", nullable: true),
                    note = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    digital_signature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    source_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    device_info = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    session_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    change_version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_role_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_role_assignments_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_role_assignments_users_granted_by",
                        column: x => x.granted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_user_role_assignments_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false),
                    role_id = table.Column<int>(type: "int", nullable: false),
                    assigned_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    assigned_by = table.Column<int>(type: "int", nullable: true),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    reason = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    change_version = table.Column<int>(type: "int", nullable: false),
                    digital_signature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    session_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    source_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    note = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "FK_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_roles_users_assigned_by",
                        column: x => x.assigned_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "user_subscriptions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    template_id = table.Column<int>(type: "int", nullable: false),
                    enabled = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_subscriptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_subscriptions_notification_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "notification_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_subscriptions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "user_training",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    training_type = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    training_date = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    valid_until = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    certificate_file = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    provider = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    notes = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_training", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_training_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "user_window_layouts",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    page_type = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    pos_x = table.Column<int>(type: "int", nullable: true),
                    pos_y = table.Column<int>(type: "int", nullable: true),
                    width = table.Column<int>(type: "int", nullable: false),
                    height = table.Column<int>(type: "int", nullable: false),
                    saved_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_window_layouts", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_window_layouts_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserActivityLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActionTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Details = table.Column<string>(type: "varchar(2048)", maxLength: 2048, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Entity = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RecordId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceIp = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeviceInfo = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SessionId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DigitalSignature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Severity = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AnomalyScore = table.Column<double>(type: "double", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserActivityLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserActivityLogs_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserPermissionOverrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false),
                    IsGranted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    GrantedById = table.Column<int>(type: "int", nullable: true),
                    GrantedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPermissionOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPermissionOverrides_permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserPermissionOverrides_users_GrantedById",
                        column: x => x.GrantedById,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserPermissionOverrides_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserRoleHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    OldRole = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NewRole = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChangedById = table.Column<int>(type: "int", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Reason = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "varchar(400)", maxLength: 400, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DigitalSignature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceIp = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeviceInfo = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SessionId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoleHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoleHistories_users_ChangedById",
                        column: x => x.ChangedById,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRoleHistories_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "warehouses",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    location = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    responsible_id = table.Column<int>(type: "int", nullable: false),
                    qr_code = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    note = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by_id = table.Column<int>(type: "int", nullable: true),
                    last_modified = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    last_modified_by_id = table.Column<int>(type: "int", nullable: true),
                    digital_signature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    io_tdevice_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    climate_mode = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    entry_hash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    source_ip = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_qualified = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    last_qualified = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    session_id = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    anomaly_score = table.Column<double>(type: "double", nullable: true),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    location_id = table.Column<int>(type: "int", nullable: true),
                    PartId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_warehouses", x => x.id);
                    table.ForeignKey(
                        name: "FK_warehouses_Parts_PartId",
                        column: x => x.PartId,
                        principalTable: "Parts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_warehouses_users_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_warehouses_users_last_modified_by_id",
                        column: x => x.last_modified_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_wh_user",
                        column: x => x.responsible_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "work_orders",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    title = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    task_description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    priority = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_open = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    due_date = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    date_close = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    closed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    requested_by_id = table.Column<int>(type: "int", nullable: false),
                    created_by_id = table.Column<int>(type: "int", nullable: false),
                    assigned_to_id = table.Column<int>(type: "int", nullable: false),
                    machine_id = table.Column<int>(type: "int", nullable: false),
                    component_id = table.Column<int>(type: "int", nullable: true),
                    capa_case_id = table.Column<int>(type: "int", nullable: true),
                    incident_id = table.Column<int>(type: "int", nullable: true),
                    result = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    notes = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    digital_signature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    last_modified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    last_modified_by_id = table.Column<int>(type: "int", nullable: true),
                    device_info = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    source_ip = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    session_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    document_path = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    next_due = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    external_ref = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    entry_hash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    audit_flag = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    anomaly_score = table.Column<double>(type: "double", nullable: true),
                    PhotoBeforeIds = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PhotoAfterIds = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ComponentId1 = table.Column<int>(type: "int", nullable: true),
                    PreventiveMaintenancePlanId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    UserId1 = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_orders", x => x.id);
                    table.ForeignKey(
                        name: "FK_work_orders_MachineComponents_component_id",
                        column: x => x.component_id,
                        principalTable: "MachineComponents",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_work_orders_PreventiveMaintenancePlans_PreventiveMaintenance~",
                        column: x => x.PreventiveMaintenancePlanId,
                        principalTable: "PreventiveMaintenancePlans",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_work_orders_capa_cases_capa_case_id",
                        column: x => x.capa_case_id,
                        principalTable: "capa_cases",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_work_orders_components_ComponentId1",
                        column: x => x.ComponentId1,
                        principalTable: "components",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_work_orders_incidents_incident_id",
                        column: x => x.incident_id,
                        principalTable: "incidents",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_work_orders_machines_machine_id",
                        column: x => x.machine_id,
                        principalTable: "machines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_work_orders_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_work_orders_users_UserId1",
                        column: x => x.UserId1,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_work_orders_users_assigned_to_id",
                        column: x => x.assigned_to_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_orders_users_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_orders_users_last_modified_by_id",
                        column: x => x.last_modified_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_work_orders_users_requested_by_id",
                        column: x => x.requested_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WarehouseStocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PartId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    WarehouseName = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LocationDetail = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    MinStock = table.Column<int>(type: "int", nullable: true),
                    MaxStock = table.Column<int>(type: "int", nullable: true),
                    Reserved = table.Column<int>(type: "int", nullable: false),
                    Blocked = table.Column<int>(type: "int", nullable: false),
                    BatchNumber = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SerialNumber = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExpiryDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastPhysicalCount = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    BlockReason = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RecallReference = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsUnderRecall = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastModifiedById = table.Column<int>(type: "int", nullable: false),
                    DigitalSignature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceIp = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CertificateDocs = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InspectionNote = table.Column<string>(type: "varchar(400)", maxLength: 400, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseStocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseStocks_Parts_PartId",
                        column: x => x.PartId,
                        principalTable: "Parts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WarehouseStocks_users_LastModifiedById",
                        column: x => x.LastModifiedById,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseStocks_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "warehouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "work_order_audit",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    work_order_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    action = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    old_value = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    new_value = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    changed_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    source_ip = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    device_info = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    session_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    incident_id = table.Column<int>(type: "int", nullable: true),
                    capa_id = table.Column<int>(type: "int", nullable: true),
                    note = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    digital_signature = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    integrity_hash = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_order_audit", x => x.id);
                    table.ForeignKey(
                        name: "FK_work_order_audit_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_order_audit_work_orders_work_order_id",
                        column: x => x.work_order_id,
                        principalTable: "work_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "work_order_checklist_item",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    work_order_id = table.Column<int>(type: "int", nullable: false),
                    item_id = table.Column<int>(type: "int", nullable: false),
                    result = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ok = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    note = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_order_checklist_item", x => x.id);
                    table.ForeignKey(
                        name: "FK_work_order_checklist_item_checklist_items_item_id",
                        column: x => x.item_id,
                        principalTable: "checklist_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_work_order_checklist_item_work_orders_work_order_id",
                        column: x => x.work_order_id,
                        principalTable: "work_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "work_order_comment",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    work_order_id = table.Column<int>(type: "int", nullable: false),
                    text = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    type = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    revision_no = table.Column<int>(type: "int", nullable: false),
                    digital_signature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_critical = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    source_ip = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_order_comment", x => x.id);
                    table.ForeignKey(
                        name: "FK_work_order_comment_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_order_comment_work_orders_work_order_id",
                        column: x => x.work_order_id,
                        principalTable: "work_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "work_order_signature",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    work_order_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    signed_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    type = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    hash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    pin_used = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    device_info = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ip_address = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_incident = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    note = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_order_signature", x => x.id);
                    table.ForeignKey(
                        name: "FK_work_order_signature_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_order_signature_work_orders_work_order_id",
                        column: x => x.work_order_id,
                        principalTable: "work_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "work_order_status_log",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    work_order_id = table.Column<int>(type: "int", nullable: false),
                    old_status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    new_status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    changed_by_id = table.Column<int>(type: "int", nullable: false),
                    changed_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    reason = table.Column<string>(type: "varchar(400)", maxLength: 400, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_incident = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    device_info = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ip_address = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    note = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_order_status_log", x => x.id);
                    table.ForeignKey(
                        name: "FK_work_order_status_log_users_changed_by_id",
                        column: x => x.changed_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_order_status_log_work_orders_work_order_id",
                        column: x => x.work_order_id,
                        principalTable: "work_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WorkOrderAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    WorkOrderId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FilePath = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Hash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Watermark = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Comment = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UploadedById = table.Column<int>(type: "int", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsApproved = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    ApprovedById = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    SourceIp = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SessionId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    OriginalFileName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RecognitionData = table.Column<string>(type: "varchar(2048)", maxLength: 2048, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VirusScanStatus = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CloudStorageReference = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastModified = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastModifiedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrderAttachments_users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WorkOrderAttachments_users_LastModifiedById",
                        column: x => x.LastModifiedById,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WorkOrderAttachments_users_UploadedById",
                        column: x => x.UploadedById,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkOrderAttachments_work_orders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "work_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WorkOrderParts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    WorkOrderId = table.Column<int>(type: "int", nullable: false),
                    PartId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UnitPrice = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    Currency = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WarehouseId = table.Column<int>(type: "int", nullable: true),
                    CapaCaseId = table.Column<int>(type: "int", nullable: true),
                    IncidentId = table.Column<int>(type: "int", nullable: true),
                    UsedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UsedById = table.Column<int>(type: "int", nullable: true),
                    DigitalSignature = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceIp = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeviceInfo = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SessionId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastModifiedById = table.Column<int>(type: "int", nullable: true),
                    SparePartId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderParts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrderParts_Parts_PartId",
                        column: x => x.PartId,
                        principalTable: "Parts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkOrderParts_SpareParts_SparePartId",
                        column: x => x.SparePartId,
                        principalTable: "SpareParts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkOrderParts_capa_cases_CapaCaseId",
                        column: x => x.CapaCaseId,
                        principalTable: "capa_cases",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_WorkOrderParts_incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "incidents",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_WorkOrderParts_users_UsedById",
                        column: x => x.UsedById,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WorkOrderParts_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "warehouses",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_WorkOrderParts_work_orders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "work_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "fk_adminact_user",
                table: "admin_activity_log",
                column: "admin_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_activity_log_UserId",
                table: "admin_activity_log",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "fk_apikey_owner",
                table: "api_keys",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "key_value",
                table: "api_keys",
                column: "key_value",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_api_usage_log_api_key_id",
                table: "api_usage_log",
                column: "api_key_id");

            migrationBuilder.CreateIndex(
                name: "IX_api_usage_log_user_id",
                table: "api_usage_log",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_attachments_approved_by_id",
                table: "attachments",
                column: "approved_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_attachments_ChangeControlId",
                table: "attachments",
                column: "ChangeControlId");

            migrationBuilder.CreateIndex(
                name: "IX_attachments_DeviationId",
                table: "attachments",
                column: "DeviationId");

            migrationBuilder.CreateIndex(
                name: "IX_attachments_ExternalContractorId",
                table: "attachments",
                column: "ExternalContractorId");

            migrationBuilder.CreateIndex(
                name: "IX_attachments_IncidentActionId",
                table: "attachments",
                column: "IncidentActionId");

            migrationBuilder.CreateIndex(
                name: "IX_attachments_IncidentId",
                table: "attachments",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_attachments_IncidentReportId",
                table: "attachments",
                column: "IncidentReportId");

            migrationBuilder.CreateIndex(
                name: "IX_attachments_MaintenanceExecutionLogId",
                table: "attachments",
                column: "MaintenanceExecutionLogId");

            migrationBuilder.CreateIndex(
                name: "IX_attachments_NotificationId",
                table: "attachments",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_attachments_NotificationLogId",
                table: "attachments",
                column: "NotificationLogId");

            migrationBuilder.CreateIndex(
                name: "IX_attachments_PartUsageId",
                table: "attachments",
                column: "PartUsageId");

            migrationBuilder.CreateIndex(
                name: "IX_attachments_SparePartId",
                table: "attachments",
                column: "SparePartId");

            migrationBuilder.CreateIndex(
                name: "IX_attachments_TrainingRecordId",
                table: "attachments",
                column: "TrainingRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_attachments_uploaded_by_id",
                table: "attachments",
                column: "uploaded_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_UserId1",
                table: "audit_logs",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_BackupHistories_PerformedBy",
                table: "BackupHistories",
                column: "PerformedBy");

            migrationBuilder.CreateIndex(
                name: "IX_BackupHistories_RestoreBy",
                table: "BackupHistories",
                column: "RestoreBy");

            migrationBuilder.CreateIndex(
                name: "IX_BackupHistories_RestoreOfBackupId",
                table: "BackupHistories",
                column: "RestoreOfBackupId");

            migrationBuilder.CreateIndex(
                name: "IX_calibration_audit_calibration_id",
                table: "calibration_audit",
                column: "calibration_id");

            migrationBuilder.CreateIndex(
                name: "IX_calibration_audit_log_calibration_id",
                table: "calibration_audit_log",
                column: "calibration_id");

            migrationBuilder.CreateIndex(
                name: "IX_calibration_audit_log_user_id",
                table: "calibration_audit_log",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_calibration_export_log_user_id",
                table: "calibration_export_log",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_calibrations_approved_by_id",
                table: "calibrations",
                column: "approved_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_calibrations_component_id",
                table: "calibrations",
                column: "component_id");

            migrationBuilder.CreateIndex(
                name: "IX_calibrations_ComponentId1",
                table: "calibrations",
                column: "ComponentId1");

            migrationBuilder.CreateIndex(
                name: "IX_calibrations_last_modified_by_id",
                table: "calibrations",
                column: "last_modified_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_calibrations_MachineId",
                table: "calibrations",
                column: "MachineId");

            migrationBuilder.CreateIndex(
                name: "IX_calibrations_next_calibration_id",
                table: "calibrations",
                column: "next_calibration_id");

            migrationBuilder.CreateIndex(
                name: "IX_calibrations_previous_calibration_id",
                table: "calibrations",
                column: "previous_calibration_id");

            migrationBuilder.CreateIndex(
                name: "IX_calibrations_supplier_id",
                table: "calibrations",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "IX_capa_action_log_capa_case_id",
                table: "capa_action_log",
                column: "capa_case_id");

            migrationBuilder.CreateIndex(
                name: "IX_capa_action_log_performed_by_id",
                table: "capa_action_log",
                column: "performed_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_capa_cases_approved_by_id",
                table: "capa_cases",
                column: "approved_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_capa_cases_assigned_to_id",
                table: "capa_cases",
                column: "assigned_to_id");

            migrationBuilder.CreateIndex(
                name: "IX_capa_cases_component_id",
                table: "capa_cases",
                column: "component_id");

            migrationBuilder.CreateIndex(
                name: "IX_capa_cases_ComponentId1",
                table: "capa_cases",
                column: "ComponentId1");

            migrationBuilder.CreateIndex(
                name: "IX_capa_cases_last_modified_by_id",
                table: "capa_cases",
                column: "last_modified_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_capa_cases_SupplierId",
                table: "capa_cases",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_capa_status_history_capa_case_id",
                table: "capa_status_history",
                column: "capa_case_id");

            migrationBuilder.CreateIndex(
                name: "IX_capa_status_history_changed_by",
                table: "capa_status_history",
                column: "changed_by");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeControls_ApprovedById",
                table: "ChangeControls",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeControls_AssignedToId",
                table: "ChangeControls",
                column: "AssignedToId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeControls_CapaCaseId",
                table: "ChangeControls",
                column: "CapaCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeControls_ComponentId",
                table: "ChangeControls",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeControls_LastModifiedById",
                table: "ChangeControls",
                column: "LastModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeControls_MachineId",
                table: "ChangeControls",
                column: "MachineId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeControls_RequestedById",
                table: "ChangeControls",
                column: "RequestedById");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeControls_ReviewedById",
                table: "ChangeControls",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_component_devices_component_id",
                table: "component_devices",
                column: "component_id");

            migrationBuilder.CreateIndex(
                name: "IX_component_devices_device_id",
                table: "component_devices",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "IX_component_devices_sensor_model_id",
                table: "component_devices",
                column: "sensor_model_id");

            migrationBuilder.CreateIndex(
                name: "IX_component_models_component_type_id",
                table: "component_models",
                column: "component_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_component_parts_component_id",
                table: "component_parts",
                column: "component_id");

            migrationBuilder.CreateIndex(
                name: "IX_component_parts_part_id",
                table: "component_parts",
                column: "part_id");

            migrationBuilder.CreateIndex(
                name: "IX_component_qualifications_component_id",
                table: "component_qualifications",
                column: "component_id");

            migrationBuilder.CreateIndex(
                name: "IX_components_last_modified_by_id",
                table: "components",
                column: "last_modified_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_components_machine_id",
                table: "components",
                column: "machine_id");

            migrationBuilder.CreateIndex(
                name: "IX_config_change_log_changed_by",
                table: "config_change_log",
                column: "changed_by");

            migrationBuilder.CreateIndex(
                name: "fk_cia_intervention",
                table: "contractor_intervention_audit",
                column: "intervention_id");

            migrationBuilder.CreateIndex(
                name: "fk_cia_user",
                table: "contractor_intervention_audit",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_contractor_interventions_component_id",
                table: "contractor_interventions",
                column: "component_id");

            migrationBuilder.CreateIndex(
                name: "IX_contractor_interventions_contractor_id",
                table: "contractor_interventions",
                column: "contractor_id");

            migrationBuilder.CreateIndex(
                name: "IX_contractor_interventions_ExternalContractorId",
                table: "contractor_interventions",
                column: "ExternalContractorId");

            migrationBuilder.CreateIndex(
                name: "IX_contractor_interventions_last_modified_by_id",
                table: "contractor_interventions",
                column: "last_modified_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_ContractorAuditLog_ExternalContractorId",
                table: "ContractorAuditLog",
                column: "ExternalContractorId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractorAuditLog_UserId",
                table: "ContractorAuditLog",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_dashboards_created_by",
                table: "dashboards",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_delegated_permissions_approved_by",
                table: "delegated_permissions",
                column: "approved_by");

            migrationBuilder.CreateIndex(
                name: "IX_delegated_permissions_from_user_id",
                table: "delegated_permissions",
                column: "from_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_delegated_permissions_permission_id",
                table: "delegated_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "IX_delegated_permissions_to_user_id",
                table: "delegated_permissions",
                column: "to_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_delete_log_deleted_by",
                table: "delete_log",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_departments_manager_id",
                table: "departments",
                column: "manager_id");

            migrationBuilder.CreateIndex(
                name: "IX_deviation_audit_deviation_id",
                table: "deviation_audit",
                column: "deviation_id");

            migrationBuilder.CreateIndex(
                name: "IX_Deviations_AssignedInvestigatorId",
                table: "Deviations",
                column: "AssignedInvestigatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Deviations_LastModifiedById",
                table: "Deviations",
                column: "LastModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_Deviations_LinkedCapaId",
                table: "Deviations",
                column: "LinkedCapaId");

            migrationBuilder.CreateIndex(
                name: "IX_Deviations_ReportedById",
                table: "Deviations",
                column: "ReportedById");

            migrationBuilder.CreateIndex(
                name: "IX_digital_signatures_SparePartId",
                table: "digital_signatures",
                column: "SparePartId");

            migrationBuilder.CreateIndex(
                name: "IX_digital_signatures_user_id",
                table: "digital_signatures",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_document_audit_events_documentcontrol_id",
                table: "document_audit_events",
                column: "documentcontrol_id");

            migrationBuilder.CreateIndex(
                name: "IX_document_audit_events_user_id",
                table: "document_audit_events",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_document_versions_created_by_id",
                table: "document_versions",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_document_versions_documentcontrol_id",
                table: "document_versions",
                column: "documentcontrol_id");

            migrationBuilder.CreateIndex(
                name: "IX_documentcontrol_approved_by_id",
                table: "documentcontrol",
                column: "approved_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_documentcontrol_created_by_id",
                table: "documentcontrol",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_documentcontrol_QualificationId",
                table: "documentcontrol",
                column: "QualificationId");

            migrationBuilder.CreateIndex(
                name: "IX_documentcontrol_RiskAssessmentId",
                table: "documentcontrol",
                column: "RiskAssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_documentcontrol_TrainingRecordId",
                table: "documentcontrol",
                column: "TrainingRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_entity_audit_log_user_id",
                table: "entity_audit_log",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_entity_tags_tag_id",
                table: "entity_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_export_print_log_user_id",
                table: "export_print_log",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalContractors_SupplierId",
                table: "ExternalContractors",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_failure_modes_component_type_id",
                table: "failure_modes",
                column: "component_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_ForensicUserChangeLogs_ChangedBy",
                table: "ForensicUserChangeLogs",
                column: "ChangedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ForensicUserChangeLogs_TargetUserId",
                table: "ForensicUserChangeLogs",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_incident_audit_capa_id",
                table: "incident_audit",
                column: "capa_id");

            migrationBuilder.CreateIndex(
                name: "IX_incident_audit_incident_id",
                table: "incident_audit",
                column: "incident_id");

            migrationBuilder.CreateIndex(
                name: "IX_incident_audit_user_id",
                table: "incident_audit",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_incident_audit_work_order_id",
                table: "incident_audit",
                column: "work_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentActions_IncidentId",
                table: "IncidentActions",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentActions_LastModifiedById",
                table: "IncidentActions",
                column: "LastModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentActions_UserId",
                table: "IncidentActions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentAuditLog_IncidentReportId",
                table: "IncidentAuditLog",
                column: "IncidentReportId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentAuditLog_UserId",
                table: "IncidentAuditLog",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentLogs_CapaCaseId",
                table: "IncidentLogs",
                column: "CapaCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentLogs_LastModifiedById",
                table: "IncidentLogs",
                column: "LastModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentLogs_ReportedById",
                table: "IncidentLogs",
                column: "ReportedById");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentLogs_ResolvedById",
                table: "IncidentLogs",
                column: "ResolvedById");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentReports_ComponentId",
                table: "IncidentReports",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentReports_MachineId",
                table: "IncidentReports",
                column: "MachineId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentReports_ReportedById",
                table: "IncidentReports",
                column: "ReportedById");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentReports_ResolvedById",
                table: "IncidentReports",
                column: "ResolvedById");

            migrationBuilder.CreateIndex(
                name: "IX_incidents_assigned_to_id",
                table: "incidents",
                column: "assigned_to_id");

            migrationBuilder.CreateIndex(
                name: "IX_incidents_capa_case_id",
                table: "incidents",
                column: "capa_case_id");

            migrationBuilder.CreateIndex(
                name: "IX_incidents_closed_by_id",
                table: "incidents",
                column: "closed_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_incidents_last_modified_by_id",
                table: "incidents",
                column: "last_modified_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_incidents_reported_by_id",
                table: "incidents",
                column: "reported_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_incidents_work_order_id",
                table: "incidents",
                column: "work_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_inspection_capa_case_id",
                table: "inspection",
                column: "capa_case_id");

            migrationBuilder.CreateIndex(
                name: "IX_inspection_last_modified_by_id",
                table: "inspection",
                column: "last_modified_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_inspection_related_machine_id",
                table: "inspection",
                column: "related_machine_id");

            migrationBuilder.CreateIndex(
                name: "IX_inspection_SupplierId",
                table: "inspection",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "fk_it_part",
                table: "inventory_transactions",
                column: "part_id");

            migrationBuilder.CreateIndex(
                name: "fk_it_user",
                table: "inventory_transactions",
                column: "performed_by");

            migrationBuilder.CreateIndex(
                name: "fk_it_warehouse",
                table: "inventory_transactions",
                column: "warehouse_id");

            migrationBuilder.CreateIndex(
                name: "IX_iot_anomaly_log_sensor_data_id",
                table: "iot_anomaly_log",
                column: "sensor_data_id");

            migrationBuilder.CreateIndex(
                name: "IX_iot_devices_gateway_id",
                table: "iot_devices",
                column: "gateway_id");

            migrationBuilder.CreateIndex(
                name: "IX_iot_gateways_location_id",
                table: "iot_gateways",
                column: "location_id");

            migrationBuilder.CreateIndex(
                name: "IX_iot_sensor_data_component_id",
                table: "iot_sensor_data",
                column: "component_id");

            migrationBuilder.CreateIndex(
                name: "IX_iot_sensor_data_unit_id",
                table: "iot_sensor_data",
                column: "unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_irregularities_log_user_id",
                table: "irregularities_log",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_UserId",
                table: "LogEntries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttemptLogs_UserId",
                table: "LoginAttemptLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_lookup_value_domain_id",
                table: "lookup_value",
                column: "domain_id");

            migrationBuilder.CreateIndex(
                name: "IX_machine_models_machine_type_id",
                table: "machine_models",
                column: "machine_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_machine_models_manufacturer_id",
                table: "machine_models",
                column: "manufacturer_id");

            migrationBuilder.CreateIndex(
                name: "IX_MachineComponents_LastModifiedById",
                table: "MachineComponents",
                column: "LastModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_MachineComponents_MachineId",
                table: "MachineComponents",
                column: "MachineId");

            migrationBuilder.CreateIndex(
                name: "IX_MachineLifecycleEvents_LastModifiedById",
                table: "MachineLifecycleEvents",
                column: "LastModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_MachineLifecycleEvents_MachineId",
                table: "MachineLifecycleEvents",
                column: "MachineId");

            migrationBuilder.CreateIndex(
                name: "IX_MachineLifecycleEvents_PerformedById",
                table: "MachineLifecycleEvents",
                column: "PerformedById");

            migrationBuilder.CreateIndex(
                name: "IX_machines_code",
                table: "machines",
                column: "code");

            migrationBuilder.CreateIndex(
                name: "IX_machines_last_modified_by_id",
                table: "machines",
                column: "last_modified_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceExecutionLogs_ApprovedById",
                table: "MaintenanceExecutionLogs",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceExecutionLogs_ExecutedById",
                table: "MaintenanceExecutionLogs",
                column: "ExecutedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceExecutionLogs_LastModifiedById",
                table: "MaintenanceExecutionLogs",
                column: "LastModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceExecutionLogs_PlanId",
                table: "MaintenanceExecutionLogs",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_mobile_device_log_user_id",
                table: "mobile_device_log",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_queue_recipient_user_id",
                table: "notification_queue",
                column: "recipient_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_queue_template_id",
                table: "notification_queue",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationChannelEvents_NotificationLogId",
                table: "NotificationChannelEvents",
                column: "NotificationLogId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_LastModifiedById",
                table: "NotificationLogs",
                column: "LastModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_UserId",
                table: "NotificationLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_part_bom_child_part_id",
                table: "part_bom",
                column: "child_part_id");

            migrationBuilder.CreateIndex(
                name: "IX_part_bom_parent_part_id",
                table: "part_bom",
                column: "parent_part_id");

            migrationBuilder.CreateIndex(
                name: "IX_PartChangeLogs_ChangedById",
                table: "PartChangeLogs",
                column: "ChangedById");

            migrationBuilder.CreateIndex(
                name: "IX_PartChangeLogs_PartId",
                table: "PartChangeLogs",
                column: "PartId");

            migrationBuilder.CreateIndex(
                name: "IX_Parts_DefaultSupplierId",
                table: "Parts",
                column: "DefaultSupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Parts_LastModifiedById",
                table: "Parts",
                column: "LastModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_PartSupplierPrices_LastModifiedById",
                table: "PartSupplierPrices",
                column: "LastModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_PartSupplierPrices_PartId",
                table: "PartSupplierPrices",
                column: "PartId");

            migrationBuilder.CreateIndex(
                name: "IX_PartSupplierPrices_SparePartId",
                table: "PartSupplierPrices",
                column: "SparePartId");

            migrationBuilder.CreateIndex(
                name: "IX_PartSupplierPrices_SupplierId",
                table: "PartSupplierPrices",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_PartUsageAuditLogs_PartUsageId",
                table: "PartUsageAuditLogs",
                column: "PartUsageId");

            migrationBuilder.CreateIndex(
                name: "IX_PartUsageAuditLogs_UserId",
                table: "PartUsageAuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PartUsages_IssuedById",
                table: "PartUsages",
                column: "IssuedById");

            migrationBuilder.CreateIndex(
                name: "IX_PartUsages_MachineComponentId",
                table: "PartUsages",
                column: "MachineComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_PartUsages_PartId",
                table: "PartUsages",
                column: "PartId");

            migrationBuilder.CreateIndex(
                name: "IX_PartUsages_SupplierId",
                table: "PartUsages",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_PartUsages_WorkOrderId",
                table: "PartUsages",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_permission_change_log_changed_by",
                table: "permission_change_log",
                column: "changed_by");

            migrationBuilder.CreateIndex(
                name: "IX_permission_change_log_permission_id",
                table: "permission_change_log",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "IX_permission_change_log_role_id",
                table: "permission_change_log",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_permission_change_log_user_id",
                table: "permission_change_log",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_permission_requests_permission_id",
                table: "permission_requests",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "IX_permission_requests_reviewed_by",
                table: "permission_requests",
                column: "reviewed_by");

            migrationBuilder.CreateIndex(
                name: "IX_permission_requests_user_id",
                table: "permission_requests",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRole_RolesId",
                table: "PermissionRole",
                column: "RolesId");

            migrationBuilder.CreateIndex(
                name: "IX_permissions_created_by",
                table: "permissions",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_permissions_last_modified_by_id",
                table: "permissions",
                column: "last_modified_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_permissions_parent_id",
                table: "permissions",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionUser_UsersId",
                table: "PermissionUser",
                column: "UsersId");

            migrationBuilder.CreateIndex(
                name: "IX_photo_approved_by_id",
                table: "photo",
                column: "approved_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_photo_component_id",
                table: "photo",
                column: "component_id");

            migrationBuilder.CreateIndex(
                name: "IX_photo_IncidentReportId",
                table: "photo",
                column: "IncidentReportId");

            migrationBuilder.CreateIndex(
                name: "IX_photo_last_modified_by_id",
                table: "photo",
                column: "last_modified_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_photo_MachineComponentId",
                table: "photo",
                column: "MachineComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_photo_next_photo_id",
                table: "photo",
                column: "next_photo_id");

            migrationBuilder.CreateIndex(
                name: "IX_photo_previous_photo_id",
                table: "photo",
                column: "previous_photo_id");

            migrationBuilder.CreateIndex(
                name: "IX_photo_uploaded_by_id",
                table: "photo",
                column: "uploaded_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_photo_work_order_id",
                table: "photo",
                column: "work_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_ppm_plans_machine_id",
                table: "ppm_plans",
                column: "machine_id");

            migrationBuilder.CreateIndex(
                name: "IX_PreventiveMaintenancePlans_ComponentId",
                table: "PreventiveMaintenancePlans",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_PreventiveMaintenancePlans_LastModifiedById",
                table: "PreventiveMaintenancePlans",
                column: "LastModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_PreventiveMaintenancePlans_MachineId",
                table: "PreventiveMaintenancePlans",
                column: "MachineId");

            migrationBuilder.CreateIndex(
                name: "IX_PreventiveMaintenancePlans_PreviousVersionId",
                table: "PreventiveMaintenancePlans",
                column: "PreviousVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_PreventiveMaintenancePlans_ResponsibleUserId",
                table: "PreventiveMaintenancePlans",
                column: "ResponsibleUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductRecallLogs_ClosedById",
                table: "ProductRecallLogs",
                column: "ClosedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductRecallLogs_LastModifiedById",
                table: "ProductRecallLogs",
                column: "LastModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductRecallLogs_LinkedCapaId",
                table: "ProductRecallLogs",
                column: "LinkedCapaId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductRecallLogs_LinkedIncidentId",
                table: "ProductRecallLogs",
                column: "LinkedIncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductRecallLogs_LinkedInspectionId",
                table: "ProductRecallLogs",
                column: "LinkedInspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductRecallLogs_RecalledById",
                table: "ProductRecallLogs",
                column: "RecalledById");

            migrationBuilder.CreateIndex(
                name: "IX_QualificationAuditLogs_QualificationId",
                table: "QualificationAuditLogs",
                column: "QualificationId");

            migrationBuilder.CreateIndex(
                name: "IX_QualificationAuditLogs_UserId",
                table: "QualificationAuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Qualifications_ApprovedById",
                table: "Qualifications",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_Qualifications_ComponentId",
                table: "Qualifications",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_Qualifications_MachineId",
                table: "Qualifications",
                column: "MachineId");

            migrationBuilder.CreateIndex(
                name: "IX_Qualifications_QualifiedById",
                table: "Qualifications",
                column: "QualifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_Qualifications_SupplierId",
                table: "Qualifications",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "fk_qe_component",
                table: "quality_events",
                column: "related_component");

            migrationBuilder.CreateIndex(
                name: "fk_qe_machine",
                table: "quality_events",
                column: "related_machine");

            migrationBuilder.CreateIndex(
                name: "idx_qe_status_id",
                table: "quality_events",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "idx_qe_type_id",
                table: "quality_events",
                column: "type_id");

            migrationBuilder.CreateIndex(
                name: "IX_quality_events_created_by_id",
                table: "quality_events",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_quality_events_last_modified_by_id",
                table: "quality_events",
                column: "last_modified_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_ref_value_domain_id",
                table: "ref_value",
                column: "domain_id");

            migrationBuilder.CreateIndex(
                name: "IX_report_schedule_generated_by",
                table: "report_schedule",
                column: "generated_by");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_GeneratedById",
                table: "Reports",
                column: "GeneratedById");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_LastModifiedById",
                table: "Reports",
                column: "LastModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_RequalificationSchedules_ComponentId",
                table: "RequalificationSchedules",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_RequalificationSchedules_LastModifiedById",
                table: "RequalificationSchedules",
                column: "LastModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_RequalificationSchedules_ResponsibleUserId",
                table: "RequalificationSchedules",
                column: "ResponsibleUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssessmentAuditLogs_RiskAssessmentId",
                table: "RiskAssessmentAuditLogs",
                column: "RiskAssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssessmentAuditLogs_UserId",
                table: "RiskAssessmentAuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssessments_ApprovedById",
                table: "RiskAssessments",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssessments_OwnerId",
                table: "RiskAssessments",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_role_audit_role_id",
                table: "role_audit",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_assigned_by",
                table: "role_permissions",
                column: "assigned_by");

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_permission_id",
                table: "role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "IX_roles_created_by_id",
                table: "roles",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_roles_last_modified_by_id",
                table: "roles",
                column: "last_modified_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_RoleUser_UsersId",
                table: "RoleUser",
                column: "UsersId");

            migrationBuilder.CreateIndex(
                name: "IX_rooms_building_id",
                table: "rooms",
                column: "building_id");

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_job_audit_log_scheduled_job_id",
                table: "scheduled_job_audit_log",
                column: "scheduled_job_id");

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_job_audit_log_user_id",
                table: "scheduled_job_audit_log",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_SchemaMigrationLogs_MigratedById",
                table: "SchemaMigrationLogs",
                column: "MigratedById");

            migrationBuilder.CreateIndex(
                name: "IX_SensitiveDataAccessLogs_ApprovedById",
                table: "SensitiveDataAccessLogs",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_SensitiveDataAccessLogs_UserId",
                table: "SensitiveDataAccessLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_sensor_data_log_approved_by_id",
                table: "sensor_data_log",
                column: "approved_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_sensor_data_log_component_id",
                table: "sensor_data_log",
                column: "component_id");

            migrationBuilder.CreateIndex(
                name: "IX_sensor_types_default_unit_id",
                table: "sensor_types",
                column: "default_unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogs_ImpersonatedById",
                table: "SessionLogs",
                column: "ImpersonatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogs_UserId",
                table: "SessionLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogs_UserId1",
                table: "SessionLogs",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_SettingAuditLogs_SettingId",
                table: "SettingAuditLogs",
                column: "SettingId");

            migrationBuilder.CreateIndex(
                name: "IX_SettingAuditLogs_UserId",
                table: "SettingAuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Settings_ApprovedById",
                table: "Settings",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_Settings_RoleId",
                table: "Settings",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Settings_UpdatedById",
                table: "Settings",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Settings_UserId",
                table: "Settings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SettingVersions_ChangedById",
                table: "SettingVersions",
                column: "ChangedById");

            migrationBuilder.CreateIndex(
                name: "IX_SettingVersions_SettingId",
                table: "SettingVersions",
                column: "SettingId");

            migrationBuilder.CreateIndex(
                name: "IX_sop_document_log_performed_by",
                table: "sop_document_log",
                column: "performed_by");

            migrationBuilder.CreateIndex(
                name: "IX_sop_document_log_sop_document_id",
                table: "sop_document_log",
                column: "sop_document_id");

            migrationBuilder.CreateIndex(
                name: "IX_SopDocuments_CreatedById",
                table: "SopDocuments",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SopDocuments_LastModifiedById",
                table: "SopDocuments",
                column: "LastModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_SopDocuments_ResponsibleUserId",
                table: "SopDocuments",
                column: "ResponsibleUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SpareParts_DefaultSupplierId",
                table: "SpareParts",
                column: "DefaultSupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SqlQueryAuditLogs_UserId",
                table: "SqlQueryAuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "fk_sl_part",
                table: "stock_levels",
                column: "part_id");

            migrationBuilder.CreateIndex(
                name: "fk_sl_warehouse",
                table: "stock_levels",
                column: "warehouse_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_levels_last_modified_by_id",
                table: "stock_levels",
                column: "last_modified_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_levels_SparePartId",
                table: "stock_levels",
                column: "SparePartId");

            migrationBuilder.CreateIndex(
                name: "IX_StockChangeLogs_ChangedById",
                table: "StockChangeLogs",
                column: "ChangedById");

            migrationBuilder.CreateIndex(
                name: "IX_StockChangeLogs_PartId",
                table: "StockChangeLogs",
                column: "PartId");

            migrationBuilder.CreateIndex(
                name: "IX_StockChangeLogs_WarehouseId",
                table: "StockChangeLogs",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_last_modified_by_id",
                table: "supplier",
                column: "last_modified_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_risk_audit_performed_by",
                table: "supplier_risk_audit",
                column: "performed_by");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_risk_audit_supplier_id",
                table: "supplier_risk_audit",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "IX_suppliers_status_id",
                table: "suppliers",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "IX_suppliers_type_id",
                table: "suppliers",
                column: "type_id");

            migrationBuilder.CreateIndex(
                name: "IX_system_event_log_user_id",
                table: "system_event_log",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_system_parameters_updated_by",
                table: "system_parameters",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAuditLogs_TrainingRecordId",
                table: "TrainingAuditLogs",
                column: "TrainingRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAuditLogs_UserId",
                table: "TrainingAuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingLogs_LastModifiedById",
                table: "TrainingLogs",
                column: "LastModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingLogs_UserId",
                table: "TrainingLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRecords_DocumentId",
                table: "TrainingRecords",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRecords_RoleId",
                table: "TrainingRecords",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRecords_TraineeId",
                table: "TrainingRecords",
                column: "TraineeId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRecords_TrainerId",
                table: "TrainingRecords",
                column: "TrainerId");

            migrationBuilder.CreateIndex(
                name: "IX_user_audit_user_id",
                table: "user_audit",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_esignatures_user_id",
                table: "user_esignatures",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_login_audit_user_id",
                table: "user_login_audit",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_permissions_assigned_by",
                table: "user_permissions",
                column: "assigned_by");

            migrationBuilder.CreateIndex(
                name: "IX_user_permissions_permission_id",
                table: "user_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_role_assignments_granted_by",
                table: "user_role_assignments",
                column: "granted_by");

            migrationBuilder.CreateIndex(
                name: "IX_user_role_assignments_role_id",
                table: "user_role_assignments",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_role_assignments_user_id",
                table: "user_role_assignments",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_assigned_by",
                table: "user_roles",
                column: "assigned_by");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_subscriptions_template_id",
                table: "user_subscriptions",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_subscriptions_user_id",
                table: "user_subscriptions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_training_user_id",
                table: "user_training",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_window_layouts_user_id",
                table: "user_window_layouts",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_UserActivityLogs_UserId",
                table: "UserActivityLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissionOverrides_GrantedById",
                table: "UserPermissionOverrides",
                column: "GrantedById");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissionOverrides_PermissionId",
                table: "UserPermissionOverrides",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissionOverrides_UserId",
                table: "UserPermissionOverrides",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleHistories_ChangedById",
                table: "UserRoleHistories",
                column: "ChangedById");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleHistories_UserId",
                table: "UserRoleHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_users_last_modified_by_id",
                table: "users",
                column: "last_modified_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_SopDocumentId",
                table: "users",
                column: "SopDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_users_TrainingRecordId",
                table: "users",
                column: "TrainingRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_users_username",
                table: "users",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "fk_wh_location",
                table: "warehouses",
                column: "location_id");

            migrationBuilder.CreateIndex(
                name: "fk_wh_user",
                table: "warehouses",
                column: "responsible_id");

            migrationBuilder.CreateIndex(
                name: "IX_warehouses_created_by_id",
                table: "warehouses",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_warehouses_last_modified_by_id",
                table: "warehouses",
                column: "last_modified_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_warehouses_PartId",
                table: "warehouses",
                column: "PartId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStocks_LastModifiedById",
                table: "WarehouseStocks",
                column: "LastModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStocks_PartId",
                table: "WarehouseStocks",
                column: "PartId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStocks_WarehouseId",
                table: "WarehouseStocks",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_work_order_audit_changed_at",
                table: "work_order_audit",
                column: "changed_at");

            migrationBuilder.CreateIndex(
                name: "IX_work_order_audit_user_id",
                table: "work_order_audit",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_order_audit_work_order_id",
                table: "work_order_audit",
                column: "work_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_order_checklist_item_item_id",
                table: "work_order_checklist_item",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_order_checklist_item_work_order_id",
                table: "work_order_checklist_item",
                column: "work_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_order_comment_user_id",
                table: "work_order_comment",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_order_comment_work_order_id",
                table: "work_order_comment",
                column: "work_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_order_signature_user_id",
                table: "work_order_signature",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_order_signature_work_order_id",
                table: "work_order_signature",
                column: "work_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_order_status_log_changed_by_id",
                table: "work_order_status_log",
                column: "changed_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_order_status_log_work_order_id",
                table: "work_order_status_log",
                column: "work_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_orders_assigned_to_id",
                table: "work_orders",
                column: "assigned_to_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_orders_capa_case_id",
                table: "work_orders",
                column: "capa_case_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_orders_component_id",
                table: "work_orders",
                column: "component_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_orders_ComponentId1",
                table: "work_orders",
                column: "ComponentId1");

            migrationBuilder.CreateIndex(
                name: "IX_work_orders_created_by_id",
                table: "work_orders",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_orders_incident_id",
                table: "work_orders",
                column: "incident_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_orders_last_modified_by_id",
                table: "work_orders",
                column: "last_modified_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_orders_machine_id",
                table: "work_orders",
                column: "machine_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_orders_PreventiveMaintenancePlanId",
                table: "work_orders",
                column: "PreventiveMaintenancePlanId");

            migrationBuilder.CreateIndex(
                name: "IX_work_orders_requested_by_id",
                table: "work_orders",
                column: "requested_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_orders_UserId",
                table: "work_orders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_work_orders_UserId1",
                table: "work_orders",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderAttachments_ApprovedById",
                table: "WorkOrderAttachments",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderAttachments_LastModifiedById",
                table: "WorkOrderAttachments",
                column: "LastModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderAttachments_UploadedById",
                table: "WorkOrderAttachments",
                column: "UploadedById");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderAttachments_WorkOrderId",
                table: "WorkOrderAttachments",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderParts_CapaCaseId",
                table: "WorkOrderParts",
                column: "CapaCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderParts_IncidentId",
                table: "WorkOrderParts",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderParts_PartId",
                table: "WorkOrderParts",
                column: "PartId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderParts_SparePartId",
                table: "WorkOrderParts",
                column: "SparePartId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderParts_UsedById",
                table: "WorkOrderParts",
                column: "UsedById");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderParts_WarehouseId",
                table: "WorkOrderParts",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderParts_WorkOrderId",
                table: "WorkOrderParts",
                column: "WorkOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_admin_activity_log_users_UserId",
                table: "admin_activity_log",
                column: "UserId",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_adminact_user",
                table: "admin_activity_log",
                column: "admin_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_apikey_owner",
                table: "api_keys",
                column: "owner_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_api_usage_log_users_user_id",
                table: "api_usage_log",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_attachments_ChangeControls_ChangeControlId",
                table: "attachments",
                column: "ChangeControlId",
                principalTable: "ChangeControls",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_attachments_Deviations_DeviationId",
                table: "attachments",
                column: "DeviationId",
                principalTable: "Deviations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_attachments_ExternalContractors_ExternalContractorId",
                table: "attachments",
                column: "ExternalContractorId",
                principalTable: "ExternalContractors",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_attachments_IncidentActions_IncidentActionId",
                table: "attachments",
                column: "IncidentActionId",
                principalTable: "IncidentActions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_attachments_IncidentReports_IncidentReportId",
                table: "attachments",
                column: "IncidentReportId",
                principalTable: "IncidentReports",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_attachments_MaintenanceExecutionLogs_MaintenanceExecutionLog~",
                table: "attachments",
                column: "MaintenanceExecutionLogId",
                principalTable: "MaintenanceExecutionLogs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_attachments_NotificationLogs_NotificationLogId",
                table: "attachments",
                column: "NotificationLogId",
                principalTable: "NotificationLogs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_attachments_PartUsages_PartUsageId",
                table: "attachments",
                column: "PartUsageId",
                principalTable: "PartUsages",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_attachments_SpareParts_SparePartId",
                table: "attachments",
                column: "SparePartId",
                principalTable: "SpareParts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_attachments_TrainingRecords_TrainingRecordId",
                table: "attachments",
                column: "TrainingRecordId",
                principalTable: "TrainingRecords",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_attachments_incidents_IncidentId",
                table: "attachments",
                column: "IncidentId",
                principalTable: "incidents",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_attachments_users_approved_by_id",
                table: "attachments",
                column: "approved_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_attachments_users_uploaded_by_id",
                table: "attachments",
                column: "uploaded_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_audit_logs_users_UserId1",
                table: "audit_logs",
                column: "UserId1",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_audit_logs_users_user_id",
                table: "audit_logs",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BackupHistories_users_PerformedBy",
                table: "BackupHistories",
                column: "PerformedBy",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_BackupHistories_users_RestoreBy",
                table: "BackupHistories",
                column: "RestoreBy",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_calibration_audit_calibrations_calibration_id",
                table: "calibration_audit",
                column: "calibration_id",
                principalTable: "calibrations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_calibration_audit_log_calibrations_calibration_id",
                table: "calibration_audit_log",
                column: "calibration_id",
                principalTable: "calibrations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_calibration_audit_log_users_user_id",
                table: "calibration_audit_log",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_calibration_export_log_users_user_id",
                table: "calibration_export_log",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_calibrations_MachineComponents_component_id",
                table: "calibrations",
                column: "component_id",
                principalTable: "MachineComponents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_calibrations_components_ComponentId1",
                table: "calibrations",
                column: "ComponentId1",
                principalTable: "components",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_calibrations_machines_MachineId",
                table: "calibrations",
                column: "MachineId",
                principalTable: "machines",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_calibrations_supplier_supplier_id",
                table: "calibrations",
                column: "supplier_id",
                principalTable: "supplier",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_calibrations_users_approved_by_id",
                table: "calibrations",
                column: "approved_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_calibrations_users_last_modified_by_id",
                table: "calibrations",
                column: "last_modified_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_capa_action_log_capa_cases_capa_case_id",
                table: "capa_action_log",
                column: "capa_case_id",
                principalTable: "capa_cases",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_capa_action_log_users_performed_by_id",
                table: "capa_action_log",
                column: "performed_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_capa_cases_MachineComponents_component_id",
                table: "capa_cases",
                column: "component_id",
                principalTable: "MachineComponents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_capa_cases_components_ComponentId1",
                table: "capa_cases",
                column: "ComponentId1",
                principalTable: "components",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_capa_cases_supplier_SupplierId",
                table: "capa_cases",
                column: "SupplierId",
                principalTable: "supplier",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_capa_cases_users_approved_by_id",
                table: "capa_cases",
                column: "approved_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_capa_cases_users_assigned_to_id",
                table: "capa_cases",
                column: "assigned_to_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_capa_cases_users_last_modified_by_id",
                table: "capa_cases",
                column: "last_modified_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_capa_status_history_users_changed_by",
                table: "capa_status_history",
                column: "changed_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ChangeControls_MachineComponents_ComponentId",
                table: "ChangeControls",
                column: "ComponentId",
                principalTable: "MachineComponents",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ChangeControls_machines_MachineId",
                table: "ChangeControls",
                column: "MachineId",
                principalTable: "machines",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_ChangeControls_users_ApprovedById",
                table: "ChangeControls",
                column: "ApprovedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ChangeControls_users_AssignedToId",
                table: "ChangeControls",
                column: "AssignedToId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ChangeControls_users_LastModifiedById",
                table: "ChangeControls",
                column: "LastModifiedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ChangeControls_users_RequestedById",
                table: "ChangeControls",
                column: "RequestedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ChangeControls_users_ReviewedById",
                table: "ChangeControls",
                column: "ReviewedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_component_devices_MachineComponents_component_id",
                table: "component_devices",
                column: "component_id",
                principalTable: "MachineComponents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_component_parts_MachineComponents_component_id",
                table: "component_parts",
                column: "component_id",
                principalTable: "MachineComponents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_component_parts_Parts_part_id",
                table: "component_parts",
                column: "part_id",
                principalTable: "Parts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_component_qualifications_MachineComponents_component_id",
                table: "component_qualifications",
                column: "component_id",
                principalTable: "MachineComponents",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_components_machines_machine_id",
                table: "components",
                column: "machine_id",
                principalTable: "machines",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_components_users_last_modified_by_id",
                table: "components",
                column: "last_modified_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_config_change_log_users_changed_by",
                table: "config_change_log",
                column: "changed_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_cia_intervention",
                table: "contractor_intervention_audit",
                column: "intervention_id",
                principalTable: "contractor_interventions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_cia_user",
                table: "contractor_intervention_audit",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_contractor_interventions_ExternalContractors_ExternalContrac~",
                table: "contractor_interventions",
                column: "ExternalContractorId",
                principalTable: "ExternalContractors",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_contractor_interventions_users_contractor_id",
                table: "contractor_interventions",
                column: "contractor_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_contractor_interventions_users_last_modified_by_id",
                table: "contractor_interventions",
                column: "last_modified_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ContractorAuditLog_ExternalContractors_ExternalContractorId",
                table: "ContractorAuditLog",
                column: "ExternalContractorId",
                principalTable: "ExternalContractors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ContractorAuditLog_users_UserId",
                table: "ContractorAuditLog",
                column: "UserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_dashboards_users_created_by",
                table: "dashboards",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_delegated_permissions_permissions_permission_id",
                table: "delegated_permissions",
                column: "permission_id",
                principalTable: "permissions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_delegated_permissions_users_approved_by",
                table: "delegated_permissions",
                column: "approved_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_delegated_permissions_users_from_user_id",
                table: "delegated_permissions",
                column: "from_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_delegated_permissions_users_to_user_id",
                table: "delegated_permissions",
                column: "to_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_delete_log_users_deleted_by",
                table: "delete_log",
                column: "deleted_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_departments_users_manager_id",
                table: "departments",
                column: "manager_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_deviation_audit_Deviations_deviation_id",
                table: "deviation_audit",
                column: "deviation_id",
                principalTable: "Deviations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Deviations_users_AssignedInvestigatorId",
                table: "Deviations",
                column: "AssignedInvestigatorId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Deviations_users_LastModifiedById",
                table: "Deviations",
                column: "LastModifiedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Deviations_users_ReportedById",
                table: "Deviations",
                column: "ReportedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_digital_signatures_SpareParts_SparePartId",
                table: "digital_signatures",
                column: "SparePartId",
                principalTable: "SpareParts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_digital_signatures_users_user_id",
                table: "digital_signatures",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_document_audit_events_documentcontrol_documentcontrol_id",
                table: "document_audit_events",
                column: "documentcontrol_id",
                principalTable: "documentcontrol",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_document_audit_events_users_user_id",
                table: "document_audit_events",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_document_versions_documentcontrol_documentcontrol_id",
                table: "document_versions",
                column: "documentcontrol_id",
                principalTable: "documentcontrol",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_document_versions_users_created_by_id",
                table: "document_versions",
                column: "created_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_documentcontrol_Qualifications_QualificationId",
                table: "documentcontrol",
                column: "QualificationId",
                principalTable: "Qualifications",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_documentcontrol_RiskAssessments_RiskAssessmentId",
                table: "documentcontrol",
                column: "RiskAssessmentId",
                principalTable: "RiskAssessments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_documentcontrol_TrainingRecords_TrainingRecordId",
                table: "documentcontrol",
                column: "TrainingRecordId",
                principalTable: "TrainingRecords",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_documentcontrol_users_approved_by_id",
                table: "documentcontrol",
                column: "approved_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_documentcontrol_users_created_by_id",
                table: "documentcontrol",
                column: "created_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_entity_audit_log_users_user_id",
                table: "entity_audit_log",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_export_print_log_users_user_id",
                table: "export_print_log",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ExternalContractors_supplier_SupplierId",
                table: "ExternalContractors",
                column: "SupplierId",
                principalTable: "supplier",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_ForensicUserChangeLogs_users_ChangedBy",
                table: "ForensicUserChangeLogs",
                column: "ChangedBy",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ForensicUserChangeLogs_users_TargetUserId",
                table: "ForensicUserChangeLogs",
                column: "TargetUserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_incident_audit_incidents_incident_id",
                table: "incident_audit",
                column: "incident_id",
                principalTable: "incidents",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_incident_audit_users_user_id",
                table: "incident_audit",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_incident_audit_work_orders_work_order_id",
                table: "incident_audit",
                column: "work_order_id",
                principalTable: "work_orders",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_IncidentActions_incidents_IncidentId",
                table: "IncidentActions",
                column: "IncidentId",
                principalTable: "incidents",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IncidentActions_users_LastModifiedById",
                table: "IncidentActions",
                column: "LastModifiedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_IncidentActions_users_UserId",
                table: "IncidentActions",
                column: "UserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IncidentAuditLog_IncidentReports_IncidentReportId",
                table: "IncidentAuditLog",
                column: "IncidentReportId",
                principalTable: "IncidentReports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IncidentAuditLog_users_UserId",
                table: "IncidentAuditLog",
                column: "UserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_IncidentLogs_users_LastModifiedById",
                table: "IncidentLogs",
                column: "LastModifiedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_IncidentLogs_users_ReportedById",
                table: "IncidentLogs",
                column: "ReportedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_IncidentLogs_users_ResolvedById",
                table: "IncidentLogs",
                column: "ResolvedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_IncidentReports_MachineComponents_ComponentId",
                table: "IncidentReports",
                column: "ComponentId",
                principalTable: "MachineComponents",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_IncidentReports_machines_MachineId",
                table: "IncidentReports",
                column: "MachineId",
                principalTable: "machines",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_IncidentReports_users_ReportedById",
                table: "IncidentReports",
                column: "ReportedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_IncidentReports_users_ResolvedById",
                table: "IncidentReports",
                column: "ResolvedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_incidents_users_assigned_to_id",
                table: "incidents",
                column: "assigned_to_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_incidents_users_closed_by_id",
                table: "incidents",
                column: "closed_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_incidents_users_last_modified_by_id",
                table: "incidents",
                column: "last_modified_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_incidents_users_reported_by_id",
                table: "incidents",
                column: "reported_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_incidents_work_orders_work_order_id",
                table: "incidents",
                column: "work_order_id",
                principalTable: "work_orders",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_inspection_machines_related_machine_id",
                table: "inspection",
                column: "related_machine_id",
                principalTable: "machines",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_inspection_supplier_SupplierId",
                table: "inspection",
                column: "SupplierId",
                principalTable: "supplier",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_inspection_users_last_modified_by_id",
                table: "inspection",
                column: "last_modified_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_it_part",
                table: "inventory_transactions",
                column: "part_id",
                principalTable: "SpareParts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_it_user",
                table: "inventory_transactions",
                column: "performed_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_it_warehouse",
                table: "inventory_transactions",
                column: "warehouse_id",
                principalTable: "warehouses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_iot_anomaly_log_iot_sensor_data_sensor_data_id",
                table: "iot_anomaly_log",
                column: "sensor_data_id",
                principalTable: "iot_sensor_data",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_iot_sensor_data_MachineComponents_component_id",
                table: "iot_sensor_data",
                column: "component_id",
                principalTable: "MachineComponents",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_irregularities_log_users_user_id",
                table: "irregularities_log",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_LogEntries_users_UserId",
                table: "LogEntries",
                column: "UserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_LoginAttemptLogs_users_UserId",
                table: "LoginAttemptLogs",
                column: "UserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_MachineComponents_machines_MachineId",
                table: "MachineComponents",
                column: "MachineId",
                principalTable: "machines",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MachineComponents_users_LastModifiedById",
                table: "MachineComponents",
                column: "LastModifiedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MachineLifecycleEvents_machines_MachineId",
                table: "MachineLifecycleEvents",
                column: "MachineId",
                principalTable: "machines",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MachineLifecycleEvents_users_LastModifiedById",
                table: "MachineLifecycleEvents",
                column: "LastModifiedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_MachineLifecycleEvents_users_PerformedById",
                table: "MachineLifecycleEvents",
                column: "PerformedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_machines_users_last_modified_by_id",
                table: "machines",
                column: "last_modified_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenanceExecutionLogs_PreventiveMaintenancePlans_PlanId",
                table: "MaintenanceExecutionLogs",
                column: "PlanId",
                principalTable: "PreventiveMaintenancePlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenanceExecutionLogs_users_ApprovedById",
                table: "MaintenanceExecutionLogs",
                column: "ApprovedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenanceExecutionLogs_users_ExecutedById",
                table: "MaintenanceExecutionLogs",
                column: "ExecutedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenanceExecutionLogs_users_LastModifiedById",
                table: "MaintenanceExecutionLogs",
                column: "LastModifiedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_mobile_device_log_users_user_id",
                table: "mobile_device_log",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_notification_queue_users_recipient_user_id",
                table: "notification_queue",
                column: "recipient_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationChannelEvents_NotificationLogs_NotificationLogId",
                table: "NotificationChannelEvents",
                column: "NotificationLogId",
                principalTable: "NotificationLogs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationLogs_users_LastModifiedById",
                table: "NotificationLogs",
                column: "LastModifiedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationLogs_users_UserId",
                table: "NotificationLogs",
                column: "UserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_part_bom_Parts_child_part_id",
                table: "part_bom",
                column: "child_part_id",
                principalTable: "Parts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_part_bom_Parts_parent_part_id",
                table: "part_bom",
                column: "parent_part_id",
                principalTable: "Parts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PartChangeLogs_Parts_PartId",
                table: "PartChangeLogs",
                column: "PartId",
                principalTable: "Parts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PartChangeLogs_users_ChangedById",
                table: "PartChangeLogs",
                column: "ChangedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Parts_supplier_DefaultSupplierId",
                table: "Parts",
                column: "DefaultSupplierId",
                principalTable: "supplier",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_Parts_users_LastModifiedById",
                table: "Parts",
                column: "LastModifiedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PartSupplierPrices_SpareParts_SparePartId",
                table: "PartSupplierPrices",
                column: "SparePartId",
                principalTable: "SpareParts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PartSupplierPrices_supplier_SupplierId",
                table: "PartSupplierPrices",
                column: "SupplierId",
                principalTable: "supplier",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PartSupplierPrices_users_LastModifiedById",
                table: "PartSupplierPrices",
                column: "LastModifiedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PartUsageAuditLogs_PartUsages_PartUsageId",
                table: "PartUsageAuditLogs",
                column: "PartUsageId",
                principalTable: "PartUsages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PartUsageAuditLogs_users_UserId",
                table: "PartUsageAuditLogs",
                column: "UserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PartUsages_supplier_SupplierId",
                table: "PartUsages",
                column: "SupplierId",
                principalTable: "supplier",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_PartUsages_users_IssuedById",
                table: "PartUsages",
                column: "IssuedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PartUsages_work_orders_WorkOrderId",
                table: "PartUsages",
                column: "WorkOrderId",
                principalTable: "work_orders",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_permission_change_log_permissions_permission_id",
                table: "permission_change_log",
                column: "permission_id",
                principalTable: "permissions",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_permission_change_log_roles_role_id",
                table: "permission_change_log",
                column: "role_id",
                principalTable: "roles",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_permission_change_log_users_changed_by",
                table: "permission_change_log",
                column: "changed_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_permission_change_log_users_user_id",
                table: "permission_change_log",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_permission_requests_permissions_permission_id",
                table: "permission_requests",
                column: "permission_id",
                principalTable: "permissions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_permission_requests_users_reviewed_by",
                table: "permission_requests",
                column: "reviewed_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_permission_requests_users_user_id",
                table: "permission_requests",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PermissionRole_permissions_PermissionsId",
                table: "PermissionRole",
                column: "PermissionsId",
                principalTable: "permissions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PermissionRole_roles_RolesId",
                table: "PermissionRole",
                column: "RolesId",
                principalTable: "roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_permissions_users_created_by",
                table: "permissions",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_permissions_users_last_modified_by_id",
                table: "permissions",
                column: "last_modified_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PermissionUser_users_UsersId",
                table: "PermissionUser",
                column: "UsersId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_photo_users_approved_by_id",
                table: "photo",
                column: "approved_by_id",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_photo_users_last_modified_by_id",
                table: "photo",
                column: "last_modified_by_id",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_photo_users_uploaded_by_id",
                table: "photo",
                column: "uploaded_by_id",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_photo_work_orders_work_order_id",
                table: "photo",
                column: "work_order_id",
                principalTable: "work_orders",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_PreventiveMaintenancePlans_users_LastModifiedById",
                table: "PreventiveMaintenancePlans",
                column: "LastModifiedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PreventiveMaintenancePlans_users_ResponsibleUserId",
                table: "PreventiveMaintenancePlans",
                column: "ResponsibleUserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductRecallLogs_users_ClosedById",
                table: "ProductRecallLogs",
                column: "ClosedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductRecallLogs_users_LastModifiedById",
                table: "ProductRecallLogs",
                column: "LastModifiedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductRecallLogs_users_RecalledById",
                table: "ProductRecallLogs",
                column: "RecalledById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QualificationAuditLogs_Qualifications_QualificationId",
                table: "QualificationAuditLogs",
                column: "QualificationId",
                principalTable: "Qualifications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QualificationAuditLogs_users_UserId",
                table: "QualificationAuditLogs",
                column: "UserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Qualifications_supplier_SupplierId",
                table: "Qualifications",
                column: "SupplierId",
                principalTable: "supplier",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_Qualifications_users_ApprovedById",
                table: "Qualifications",
                column: "ApprovedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Qualifications_users_QualifiedById",
                table: "Qualifications",
                column: "QualifiedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_quality_events_users_created_by_id",
                table: "quality_events",
                column: "created_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_quality_events_users_last_modified_by_id",
                table: "quality_events",
                column: "last_modified_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_report_schedule_users_generated_by",
                table: "report_schedule",
                column: "generated_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_users_GeneratedById",
                table: "Reports",
                column: "GeneratedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_users_LastModifiedById",
                table: "Reports",
                column: "LastModifiedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RequalificationSchedules_users_LastModifiedById",
                table: "RequalificationSchedules",
                column: "LastModifiedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RequalificationSchedules_users_ResponsibleUserId",
                table: "RequalificationSchedules",
                column: "ResponsibleUserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_RiskAssessmentAuditLogs_RiskAssessments_RiskAssessmentId",
                table: "RiskAssessmentAuditLogs",
                column: "RiskAssessmentId",
                principalTable: "RiskAssessments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RiskAssessmentAuditLogs_users_UserId",
                table: "RiskAssessmentAuditLogs",
                column: "UserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_RiskAssessments_users_ApprovedById",
                table: "RiskAssessments",
                column: "ApprovedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_RiskAssessments_users_OwnerId",
                table: "RiskAssessments",
                column: "OwnerId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_role_audit_roles_role_id",
                table: "role_audit",
                column: "role_id",
                principalTable: "roles",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_role_permissions_roles_role_id",
                table: "role_permissions",
                column: "role_id",
                principalTable: "roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_role_permissions_users_assigned_by",
                table: "role_permissions",
                column: "assigned_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_roles_users_created_by_id",
                table: "roles",
                column: "created_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_roles_users_last_modified_by_id",
                table: "roles",
                column: "last_modified_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_RoleUser_users_UsersId",
                table: "RoleUser",
                column: "UsersId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_scheduled_job_audit_log_users_user_id",
                table: "scheduled_job_audit_log",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SchemaMigrationLogs_users_MigratedById",
                table: "SchemaMigrationLogs",
                column: "MigratedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SensitiveDataAccessLogs_users_ApprovedById",
                table: "SensitiveDataAccessLogs",
                column: "ApprovedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SensitiveDataAccessLogs_users_UserId",
                table: "SensitiveDataAccessLogs",
                column: "UserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_sensor_data_log_users_approved_by_id",
                table: "sensor_data_log",
                column: "approved_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionLogs_users_ImpersonatedById",
                table: "SessionLogs",
                column: "ImpersonatedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionLogs_users_UserId",
                table: "SessionLogs",
                column: "UserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionLogs_users_UserId1",
                table: "SessionLogs",
                column: "UserId1",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_SettingAuditLogs_Settings_SettingId",
                table: "SettingAuditLogs",
                column: "SettingId",
                principalTable: "Settings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SettingAuditLogs_users_UserId",
                table: "SettingAuditLogs",
                column: "UserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Settings_users_ApprovedById",
                table: "Settings",
                column: "ApprovedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Settings_users_UpdatedById",
                table: "Settings",
                column: "UpdatedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Settings_users_UserId",
                table: "Settings",
                column: "UserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SettingVersions_users_ChangedById",
                table: "SettingVersions",
                column: "ChangedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_sop_document_log_SopDocuments_sop_document_id",
                table: "sop_document_log",
                column: "sop_document_id",
                principalTable: "SopDocuments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_sop_document_log_users_performed_by",
                table: "sop_document_log",
                column: "performed_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SopDocuments_users_CreatedById",
                table: "SopDocuments",
                column: "CreatedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SopDocuments_users_LastModifiedById",
                table: "SopDocuments",
                column: "LastModifiedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SopDocuments_users_ResponsibleUserId",
                table: "SopDocuments",
                column: "ResponsibleUserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SpareParts_supplier_DefaultSupplierId",
                table: "SpareParts",
                column: "DefaultSupplierId",
                principalTable: "supplier",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_SqlQueryAuditLogs_users_UserId",
                table: "SqlQueryAuditLogs",
                column: "UserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_levels_users_last_modified_by_id",
                table: "stock_levels",
                column: "last_modified_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sl_warehouse",
                table: "stock_levels",
                column: "warehouse_id",
                principalTable: "warehouses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockChangeLogs_users_ChangedById",
                table: "StockChangeLogs",
                column: "ChangedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockChangeLogs_warehouses_WarehouseId",
                table: "StockChangeLogs",
                column: "WarehouseId",
                principalTable: "warehouses",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_supplier_users_last_modified_by_id",
                table: "supplier",
                column: "last_modified_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_supplier_risk_audit_users_performed_by",
                table: "supplier_risk_audit",
                column: "performed_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_system_event_log_users_user_id",
                table: "system_event_log",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_system_parameters_users_updated_by",
                table: "system_parameters",
                column: "updated_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingAuditLogs_TrainingRecords_TrainingRecordId",
                table: "TrainingAuditLogs",
                column: "TrainingRecordId",
                principalTable: "TrainingRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingAuditLogs_users_UserId",
                table: "TrainingAuditLogs",
                column: "UserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingLogs_users_LastModifiedById",
                table: "TrainingLogs",
                column: "LastModifiedById",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingLogs_users_UserId",
                table: "TrainingLogs",
                column: "UserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingRecords_users_TraineeId",
                table: "TrainingRecords",
                column: "TraineeId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingRecords_users_TrainerId",
                table: "TrainingRecords",
                column: "TrainerId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_capa_cases_users_approved_by_id",
                table: "capa_cases");

            migrationBuilder.DropForeignKey(
                name: "FK_capa_cases_users_assigned_to_id",
                table: "capa_cases");

            migrationBuilder.DropForeignKey(
                name: "FK_capa_cases_users_last_modified_by_id",
                table: "capa_cases");

            migrationBuilder.DropForeignKey(
                name: "FK_components_users_last_modified_by_id",
                table: "components");

            migrationBuilder.DropForeignKey(
                name: "FK_documentcontrol_users_approved_by_id",
                table: "documentcontrol");

            migrationBuilder.DropForeignKey(
                name: "FK_documentcontrol_users_created_by_id",
                table: "documentcontrol");

            migrationBuilder.DropForeignKey(
                name: "FK_incidents_users_assigned_to_id",
                table: "incidents");

            migrationBuilder.DropForeignKey(
                name: "FK_incidents_users_closed_by_id",
                table: "incidents");

            migrationBuilder.DropForeignKey(
                name: "FK_incidents_users_last_modified_by_id",
                table: "incidents");

            migrationBuilder.DropForeignKey(
                name: "FK_incidents_users_reported_by_id",
                table: "incidents");

            migrationBuilder.DropForeignKey(
                name: "FK_MachineComponents_users_LastModifiedById",
                table: "MachineComponents");

            migrationBuilder.DropForeignKey(
                name: "FK_machines_users_last_modified_by_id",
                table: "machines");

            migrationBuilder.DropForeignKey(
                name: "FK_PreventiveMaintenancePlans_users_LastModifiedById",
                table: "PreventiveMaintenancePlans");

            migrationBuilder.DropForeignKey(
                name: "FK_PreventiveMaintenancePlans_users_ResponsibleUserId",
                table: "PreventiveMaintenancePlans");

            migrationBuilder.DropForeignKey(
                name: "FK_Qualifications_users_ApprovedById",
                table: "Qualifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Qualifications_users_QualifiedById",
                table: "Qualifications");

            migrationBuilder.DropForeignKey(
                name: "FK_RiskAssessments_users_ApprovedById",
                table: "RiskAssessments");

            migrationBuilder.DropForeignKey(
                name: "FK_RiskAssessments_users_OwnerId",
                table: "RiskAssessments");

            migrationBuilder.DropForeignKey(
                name: "FK_roles_users_created_by_id",
                table: "roles");

            migrationBuilder.DropForeignKey(
                name: "FK_roles_users_last_modified_by_id",
                table: "roles");

            migrationBuilder.DropForeignKey(
                name: "FK_SopDocuments_users_CreatedById",
                table: "SopDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_SopDocuments_users_LastModifiedById",
                table: "SopDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_SopDocuments_users_ResponsibleUserId",
                table: "SopDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_supplier_users_last_modified_by_id",
                table: "supplier");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainingRecords_users_TraineeId",
                table: "TrainingRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainingRecords_users_TrainerId",
                table: "TrainingRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_work_orders_users_UserId",
                table: "work_orders");

            migrationBuilder.DropForeignKey(
                name: "FK_work_orders_users_UserId1",
                table: "work_orders");

            migrationBuilder.DropForeignKey(
                name: "FK_work_orders_users_assigned_to_id",
                table: "work_orders");

            migrationBuilder.DropForeignKey(
                name: "FK_work_orders_users_created_by_id",
                table: "work_orders");

            migrationBuilder.DropForeignKey(
                name: "FK_work_orders_users_last_modified_by_id",
                table: "work_orders");

            migrationBuilder.DropForeignKey(
                name: "FK_work_orders_users_requested_by_id",
                table: "work_orders");

            migrationBuilder.DropForeignKey(
                name: "FK_documentcontrol_TrainingRecords_TrainingRecordId",
                table: "documentcontrol");

            migrationBuilder.DropForeignKey(
                name: "FK_work_orders_incidents_incident_id",
                table: "work_orders");

            migrationBuilder.DropTable(
                name: "admin_activity_log");

            migrationBuilder.DropTable(
                name: "api_audit_log");

            migrationBuilder.DropTable(
                name: "api_usage_log");

            migrationBuilder.DropTable(
                name: "AttachmentAuditLogs");

            migrationBuilder.DropTable(
                name: "attachments");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "AuditLogEntries");

            migrationBuilder.DropTable(
                name: "BackupHistories");

            migrationBuilder.DropTable(
                name: "calibration_audit");

            migrationBuilder.DropTable(
                name: "calibration_audit_log");

            migrationBuilder.DropTable(
                name: "calibration_export_log");

            migrationBuilder.DropTable(
                name: "calibration_sensors");

            migrationBuilder.DropTable(
                name: "capa_action_log");

            migrationBuilder.DropTable(
                name: "capa_actions");

            migrationBuilder.DropTable(
                name: "capa_status_history");

            migrationBuilder.DropTable(
                name: "checklist_templates");

            migrationBuilder.DropTable(
                name: "comments");

            migrationBuilder.DropTable(
                name: "component_devices");

            migrationBuilder.DropTable(
                name: "component_models");

            migrationBuilder.DropTable(
                name: "component_parts");

            migrationBuilder.DropTable(
                name: "component_qualifications");

            migrationBuilder.DropTable(
                name: "config_change_log");

            migrationBuilder.DropTable(
                name: "contractor_intervention_audit");

            migrationBuilder.DropTable(
                name: "ContractorAuditLog");

            migrationBuilder.DropTable(
                name: "dashboards");

            migrationBuilder.DropTable(
                name: "delegated_permissions");

            migrationBuilder.DropTable(
                name: "delete_log");

            migrationBuilder.DropTable(
                name: "departments");

            migrationBuilder.DropTable(
                name: "deviation_audit");

            migrationBuilder.DropTable(
                name: "digital_signatures");

            migrationBuilder.DropTable(
                name: "document_audit_events");

            migrationBuilder.DropTable(
                name: "document_versions");

            migrationBuilder.DropTable(
                name: "entity_audit_log");

            migrationBuilder.DropTable(
                name: "entity_tags");

            migrationBuilder.DropTable(
                name: "export_audit_log");

            migrationBuilder.DropTable(
                name: "export_print_log");

            migrationBuilder.DropTable(
                name: "failure_modes");

            migrationBuilder.DropTable(
                name: "ForensicUserChangeLogs");

            migrationBuilder.DropTable(
                name: "incident_audit");

            migrationBuilder.DropTable(
                name: "IncidentAuditLog");

            migrationBuilder.DropTable(
                name: "IncidentLogs");

            migrationBuilder.DropTable(
                name: "IntegrationLogs");

            migrationBuilder.DropTable(
                name: "inventory_transactions");

            migrationBuilder.DropTable(
                name: "iot_anomaly_log");

            migrationBuilder.DropTable(
                name: "iot_event_audit");

            migrationBuilder.DropTable(
                name: "irregularities_log");

            migrationBuilder.DropTable(
                name: "job_titles");

            migrationBuilder.DropTable(
                name: "KpiWidgets");

            migrationBuilder.DropTable(
                name: "LogEntries");

            migrationBuilder.DropTable(
                name: "LoginAttemptLogs");

            migrationBuilder.DropTable(
                name: "lookup_value");

            migrationBuilder.DropTable(
                name: "machine_models");

            migrationBuilder.DropTable(
                name: "machine_statuses");

            migrationBuilder.DropTable(
                name: "MachineLifecycleEvents");

            migrationBuilder.DropTable(
                name: "mobile_device_log");

            migrationBuilder.DropTable(
                name: "notification_queue");

            migrationBuilder.DropTable(
                name: "NotificationChannelEvents");

            migrationBuilder.DropTable(
                name: "part_bom");

            migrationBuilder.DropTable(
                name: "PartChangeLogs");

            migrationBuilder.DropTable(
                name: "PartSupplierPrices");

            migrationBuilder.DropTable(
                name: "PartUsageAuditLogs");

            migrationBuilder.DropTable(
                name: "permission_change_log");

            migrationBuilder.DropTable(
                name: "permission_requests");

            migrationBuilder.DropTable(
                name: "PermissionRole");

            migrationBuilder.DropTable(
                name: "PermissionUser");

            migrationBuilder.DropTable(
                name: "photo");

            migrationBuilder.DropTable(
                name: "ppm_plans");

            migrationBuilder.DropTable(
                name: "ProductRecallLogs");

            migrationBuilder.DropTable(
                name: "QualificationAuditLogs");

            migrationBuilder.DropTable(
                name: "quality_events");

            migrationBuilder.DropTable(
                name: "report_schedule");

            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DropTable(
                name: "RequalificationSchedules");

            migrationBuilder.DropTable(
                name: "responsible_parties");

            migrationBuilder.DropTable(
                name: "RiskAssessmentAuditLogs");

            migrationBuilder.DropTable(
                name: "role_audit");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "RoleUser");

            migrationBuilder.DropTable(
                name: "rooms");

            migrationBuilder.DropTable(
                name: "scheduled_job_audit_log");

            migrationBuilder.DropTable(
                name: "SchemaMigrationLogs");

            migrationBuilder.DropTable(
                name: "SensitiveDataAccessLogs");

            migrationBuilder.DropTable(
                name: "sensor_data_log");

            migrationBuilder.DropTable(
                name: "sensor_types");

            migrationBuilder.DropTable(
                name: "SessionLogs");

            migrationBuilder.DropTable(
                name: "SettingAuditLogs");

            migrationBuilder.DropTable(
                name: "SettingVersions");

            migrationBuilder.DropTable(
                name: "sites");

            migrationBuilder.DropTable(
                name: "sop_document_log");

            migrationBuilder.DropTable(
                name: "SqlQueryAuditLogs");

            migrationBuilder.DropTable(
                name: "stock_levels");

            migrationBuilder.DropTable(
                name: "StockChangeLogs");

            migrationBuilder.DropTable(
                name: "supplier_risk_audit");

            migrationBuilder.DropTable(
                name: "SupplierAudits");

            migrationBuilder.DropTable(
                name: "system_event_log");

            migrationBuilder.DropTable(
                name: "system_parameters");

            migrationBuilder.DropTable(
                name: "tenants");

            migrationBuilder.DropTable(
                name: "TrainingAuditLogs");

            migrationBuilder.DropTable(
                name: "TrainingLogs");

            migrationBuilder.DropTable(
                name: "user_audit");

            migrationBuilder.DropTable(
                name: "user_esignatures");

            migrationBuilder.DropTable(
                name: "user_login_audit");

            migrationBuilder.DropTable(
                name: "user_permissions");

            migrationBuilder.DropTable(
                name: "user_role_assignments");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "user_subscriptions");

            migrationBuilder.DropTable(
                name: "user_training");

            migrationBuilder.DropTable(
                name: "user_window_layouts");

            migrationBuilder.DropTable(
                name: "UserActivityLogs");

            migrationBuilder.DropTable(
                name: "UserPermissionOverrides");

            migrationBuilder.DropTable(
                name: "UserRoleHistories");

            migrationBuilder.DropTable(
                name: "WarehouseStocks");

            migrationBuilder.DropTable(
                name: "work_order_audit");

            migrationBuilder.DropTable(
                name: "work_order_checklist_item");

            migrationBuilder.DropTable(
                name: "work_order_comment");

            migrationBuilder.DropTable(
                name: "work_order_signature");

            migrationBuilder.DropTable(
                name: "work_order_status_log");

            migrationBuilder.DropTable(
                name: "WorkOrderAttachments");

            migrationBuilder.DropTable(
                name: "WorkOrderParts");

            migrationBuilder.DropTable(
                name: "api_keys");

            migrationBuilder.DropTable(
                name: "ChangeControls");

            migrationBuilder.DropTable(
                name: "IncidentActions");

            migrationBuilder.DropTable(
                name: "MaintenanceExecutionLogs");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "calibrations");

            migrationBuilder.DropTable(
                name: "iot_devices");

            migrationBuilder.DropTable(
                name: "sensor_models");

            migrationBuilder.DropTable(
                name: "contractor_interventions");

            migrationBuilder.DropTable(
                name: "Deviations");

            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.DropTable(
                name: "component_types");

            migrationBuilder.DropTable(
                name: "iot_sensor_data");

            migrationBuilder.DropTable(
                name: "lookup_domain");

            migrationBuilder.DropTable(
                name: "machine_types");

            migrationBuilder.DropTable(
                name: "manufacturers");

            migrationBuilder.DropTable(
                name: "NotificationLogs");

            migrationBuilder.DropTable(
                name: "PartUsages");

            migrationBuilder.DropTable(
                name: "IncidentReports");

            migrationBuilder.DropTable(
                name: "inspection");

            migrationBuilder.DropTable(
                name: "buildings");

            migrationBuilder.DropTable(
                name: "ScheduledJobs");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "suppliers");

            migrationBuilder.DropTable(
                name: "notification_templates");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "checklist_items");

            migrationBuilder.DropTable(
                name: "SpareParts");

            migrationBuilder.DropTable(
                name: "warehouses");

            migrationBuilder.DropTable(
                name: "iot_gateways");

            migrationBuilder.DropTable(
                name: "ExternalContractors");

            migrationBuilder.DropTable(
                name: "measurement_units");

            migrationBuilder.DropTable(
                name: "ref_value");

            migrationBuilder.DropTable(
                name: "Parts");

            migrationBuilder.DropTable(
                name: "locations");

            migrationBuilder.DropTable(
                name: "ref_domain");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "SopDocuments");

            migrationBuilder.DropTable(
                name: "TrainingRecords");

            migrationBuilder.DropTable(
                name: "documentcontrol");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "Qualifications");

            migrationBuilder.DropTable(
                name: "RiskAssessments");

            migrationBuilder.DropTable(
                name: "incidents");

            migrationBuilder.DropTable(
                name: "work_orders");

            migrationBuilder.DropTable(
                name: "PreventiveMaintenancePlans");

            migrationBuilder.DropTable(
                name: "capa_cases");

            migrationBuilder.DropTable(
                name: "MachineComponents");

            migrationBuilder.DropTable(
                name: "components");

            migrationBuilder.DropTable(
                name: "supplier");

            migrationBuilder.DropTable(
                name: "machines");
        }
    }
}
