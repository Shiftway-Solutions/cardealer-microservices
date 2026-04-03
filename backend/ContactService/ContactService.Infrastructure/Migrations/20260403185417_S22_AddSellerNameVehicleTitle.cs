using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContactService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class S22_AddSellerNameVehicleTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SellerName",
                table: "ContactRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleTitle",
                table: "ContactRequests",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SellerName",
                table: "ContactRequests");

            migrationBuilder.DropColumn(
                name: "VehicleTitle",
                table: "ContactRequests");
        }
    }
}
