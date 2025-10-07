using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YasGMP.Data.Migrations
{
    /// <summary>
    /// Represents the Attachment Retention Enhancements.
    /// </summary>
    public partial class AttachmentRetentionEnhancements : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE attachments CHANGE COLUMN file_hash sha256 CHAR(64) NULL;");
            migrationBuilder.Sql("ALTER TABLE attachments ADD COLUMN tenant_id INT NULL AFTER uploaded_by_id;");
            migrationBuilder.Sql("ALTER TABLE attachments ADD COLUMN encrypted TINYINT(1) NOT NULL DEFAULT 0 AFTER status;");
            migrationBuilder.Sql("ALTER TABLE attachments ADD COLUMN encryption_metadata TEXT NULL AFTER encrypted;");
            migrationBuilder.Sql("ALTER TABLE attachments ADD COLUMN soft_deleted_at DATETIME NULL AFTER is_deleted;");
            migrationBuilder.Sql("ALTER TABLE attachments ADD CONSTRAINT fk_attachments_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE SET NULL;");
            migrationBuilder.Sql("ALTER TABLE retention_policies ADD COLUMN min_retain_days INT NULL AFTER retain_until;");
            migrationBuilder.Sql("ALTER TABLE retention_policies ADD COLUMN max_retain_days INT NULL AFTER min_retain_days;");
            migrationBuilder.Sql("ALTER TABLE retention_policies ADD COLUMN legal_hold TINYINT(1) NOT NULL DEFAULT 0 AFTER max_retain_days;");
            migrationBuilder.Sql("ALTER TABLE retention_policies ADD COLUMN delete_mode VARCHAR(32) NOT NULL DEFAULT 'soft' AFTER legal_hold;");
            migrationBuilder.Sql("ALTER TABLE retention_policies ADD COLUMN review_required TINYINT(1) NOT NULL DEFAULT 0 AFTER delete_mode;");
            migrationBuilder.Sql("ALTER TABLE attachments DROP INDEX IF EXISTS ux_attachments_sha_size;");
            migrationBuilder.Sql("CREATE UNIQUE INDEX ux_attachments_sha256_size ON attachments (sha256, file_size);");
            migrationBuilder.Sql("UPDATE attachments SET encrypted = (status LIKE 'encrypted%');");
            migrationBuilder.Sql("UPDATE attachments SET soft_deleted_at = uploaded_at WHERE is_deleted = 1;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE attachments DROP INDEX IF EXISTS ux_attachments_sha256_size;");
            migrationBuilder.Sql("ALTER TABLE attachments DROP FOREIGN KEY IF EXISTS fk_attachments_tenant;");
            migrationBuilder.Sql("ALTER TABLE attachments DROP COLUMN tenant_id;");
            migrationBuilder.Sql("ALTER TABLE attachments DROP COLUMN encrypted;");
            migrationBuilder.Sql("ALTER TABLE attachments DROP COLUMN encryption_metadata;");
            migrationBuilder.Sql("ALTER TABLE attachments DROP COLUMN soft_deleted_at;");
            migrationBuilder.Sql("ALTER TABLE retention_policies DROP COLUMN min_retain_days;");
            migrationBuilder.Sql("ALTER TABLE retention_policies DROP COLUMN max_retain_days;");
            migrationBuilder.Sql("ALTER TABLE retention_policies DROP COLUMN legal_hold;");
            migrationBuilder.Sql("ALTER TABLE retention_policies DROP COLUMN delete_mode;");
            migrationBuilder.Sql("ALTER TABLE retention_policies DROP COLUMN review_required;");
            migrationBuilder.Sql("ALTER TABLE attachments CHANGE COLUMN sha256 file_hash VARCHAR(128) NULL;");
            migrationBuilder.Sql("CREATE UNIQUE INDEX ux_attachments_sha_size ON attachments (file_hash, file_size);");
        }
    }
}
