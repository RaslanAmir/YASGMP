using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YasGMP.Data.Migrations
{
    public partial class AttachmentInfrastructure : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE attachments MODIFY uploaded_by_id INT NULL;");

            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS attachment_links (
    id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    attachment_id INT NOT NULL,
    entity_type VARCHAR(64) NOT NULL,
    entity_id INT NOT NULL,
    linked_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    linked_by_id INT NULL,
    KEY ix_attachment_links_entity (entity_type, entity_id),
    KEY ix_attachment_links_attachment (attachment_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS retention_policies (
    id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    attachment_id INT NOT NULL,
    policy_name VARCHAR(128) NOT NULL,
    retain_until DATETIME NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_id INT NULL,
    notes VARCHAR(512) NULL,
    KEY ix_retention_attachment (attachment_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

            migrationBuilder.Sql("DROP TABLE IF EXISTS work_order_photos;");

            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS work_order_photos (
    id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    work_order_id INT NOT NULL,
    attachment_id INT NOT NULL,
    kind VARCHAR(16) NULL,
    uploaded_by INT NULL,
    uploaded_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    KEY ix_work_order_photos_work_order (work_order_id),
    KEY ix_work_order_photos_attachment (attachment_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS work_order_photos;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS retention_policies;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS attachment_links;");
        }
    }
}

