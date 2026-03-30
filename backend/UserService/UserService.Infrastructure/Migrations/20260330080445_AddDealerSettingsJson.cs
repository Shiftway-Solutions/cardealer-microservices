using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDealerSettingsJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NotificationSettingsJson",
                table: "Dealers",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecuritySettingsJson",
                table: "Dealers",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotificationSettingsJson",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "SecuritySettingsJson",
                table: "Dealers");
        }
    }
}
