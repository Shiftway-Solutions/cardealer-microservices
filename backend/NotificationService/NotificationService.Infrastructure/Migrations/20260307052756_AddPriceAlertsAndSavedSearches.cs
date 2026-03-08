using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotificationService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceAlertsAndSavedSearches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PriceAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    VehicleImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CurrentPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TargetPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PriceDropPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyByEmail = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyByPush = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyBySms = table.Column<bool>(type: "boolean", nullable: false),
                    TriggeredCount = table.Column<int>(type: "integer", nullable: false),
                    LastNotifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SavedSearches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CriteriaJson = table.Column<string>(type: "jsonb", nullable: false),
                    NotifyOnNewResults = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyByEmail = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyByPush = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationFrequency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "daily"),
                    MatchCount = table.Column<int>(type: "integer", nullable: false),
                    LastMatchAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastNotifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedSearches", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PriceAlerts_UserId",
                table: "PriceAlerts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceAlerts_UserId_IsActive",
                table: "PriceAlerts",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceAlerts_VehicleId",
                table: "PriceAlerts",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceAlerts_VehicleId_IsActive",
                table: "PriceAlerts",
                columns: new[] { "VehicleId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SavedSearches_IsActive_NotifyOnNewResults",
                table: "SavedSearches",
                columns: new[] { "IsActive", "NotifyOnNewResults" });

            migrationBuilder.CreateIndex(
                name: "IX_SavedSearches_UserId",
                table: "SavedSearches",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PriceAlerts");

            migrationBuilder.DropTable(
                name: "SavedSearches");
        }
    }
}
