using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YasGMP.Data.Migrations
{
    public partial class AttachmentContentIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE attachments MODIFY file_path VARCHAR(512) NULL;");
            migrationBuilder.Sql("CREATE UNIQUE INDEX ux_attachments_sha_size ON attachments (file_hash, file_size);");
            migrationBuilder.Sql("UPDATE attachments SET file_path = NULL WHERE file_content IS NOT NULL;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX ux_attachments_sha_size ON attachments;");
        }
    }
}

