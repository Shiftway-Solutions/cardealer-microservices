using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediaService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddImageHealthScanColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add broken_image flag (default false)
            migrationBuilder.AddColumn<bool>(
                name: "BrokenImage",
                table: "media_assets",
                type: "boolean",
                nullable: true,
                defaultValue: false);

            // Add broken_image_detected_at timestamp
            migrationBuilder.AddColumn<DateTime>(
                name: "BrokenImageDetectedAt",
                table: "media_assets",
                type: "timestamp with time zone",
                nullable: true);

            // Add broken_image_http_status code
            migrationBuilder.AddColumn<int>(
                name: "BrokenImageHttpStatus",
                table: "media_assets",
                type: "integer",
                nullable: true);

            // Add last_health_check_at timestamp
            migrationBuilder.AddColumn<DateTime>(
                name: "LastHealthCheckAt",
                table: "media_assets",
                type: "timestamp with time zone",
                nullable: true);

            // Index for querying broken images efficiently (partial index — only broken=true)
            migrationBuilder.CreateIndex(
                name: "IX_media_assets_BrokenImage",
                table: "media_assets",
                column: "BrokenImage",
                filter: "\"BrokenImage\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_media_assets_BrokenImage",
                table: "media_assets");

            migrationBuilder.DropColumn(name: "LastHealthCheckAt", table: "media_assets");
            migrationBuilder.DropColumn(name: "BrokenImageHttpStatus", table: "media_assets");
            migrationBuilder.DropColumn(name: "BrokenImageDetectedAt", table: "media_assets");
            migrationBuilder.DropColumn(name: "BrokenImage", table: "media_assets");
        }
    }
}
