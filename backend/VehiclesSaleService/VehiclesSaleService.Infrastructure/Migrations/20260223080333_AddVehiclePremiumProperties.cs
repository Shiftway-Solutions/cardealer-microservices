using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VehiclesSaleService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVehiclePremiumProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "vehicles",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "FeaturedPriority",
                table: "vehicles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "FeaturedUntil",
                table: "vehicles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPremium",
                table: "vehicles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "LinkedCampaignId",
                table: "vehicles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "leads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    SellerId = table.Column<Guid>(type: "uuid", nullable: false),
                    DealerId = table.Column<Guid>(type: "uuid", nullable: true),
                    BuyerId = table.Column<Guid>(type: "uuid", nullable: true),
                    BuyerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BuyerEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    BuyerPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    VehicleTitle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    VehiclePrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    VehicleImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ContactedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_leads_vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lead_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LeadId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SenderRole = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lead_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lead_messages_leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 23, 8, 3, 32, 833, DateTimeKind.Utc).AddTicks(6300));

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 23, 8, 3, 32, 833, DateTimeKind.Utc).AddTicks(6300));

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 23, 8, 3, 32, 833, DateTimeKind.Utc).AddTicks(6300));

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 23, 8, 3, 32, 833, DateTimeKind.Utc).AddTicks(6310));

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 23, 8, 3, 32, 833, DateTimeKind.Utc).AddTicks(6310));

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 23, 8, 3, 32, 833, DateTimeKind.Utc).AddTicks(6310));

            migrationBuilder.UpdateData(
                table: "homepage_section_configs",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 23, 8, 3, 32, 833, DateTimeKind.Utc).AddTicks(6880));

            migrationBuilder.UpdateData(
                table: "homepage_section_configs",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 23, 8, 3, 32, 833, DateTimeKind.Utc).AddTicks(6890));

            migrationBuilder.UpdateData(
                table: "homepage_section_configs",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 23, 8, 3, 32, 833, DateTimeKind.Utc).AddTicks(6890));

            migrationBuilder.UpdateData(
                table: "homepage_section_configs",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 23, 8, 3, 32, 833, DateTimeKind.Utc).AddTicks(6890));

            migrationBuilder.UpdateData(
                table: "homepage_section_configs",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 23, 8, 3, 32, 833, DateTimeKind.Utc).AddTicks(6900));

            migrationBuilder.UpdateData(
                table: "homepage_section_configs",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 23, 8, 3, 32, 833, DateTimeKind.Utc).AddTicks(6900));

            migrationBuilder.UpdateData(
                table: "homepage_section_configs",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 23, 8, 3, 32, 833, DateTimeKind.Utc).AddTicks(6900));

            migrationBuilder.CreateIndex(
                name: "IX_lead_messages_CreatedAt",
                table: "lead_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_lead_messages_LeadId",
                table: "lead_messages",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_leads_BuyerEmail",
                table: "leads",
                column: "BuyerEmail");

            migrationBuilder.CreateIndex(
                name: "IX_leads_CreatedAt",
                table: "leads",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_leads_DealerId",
                table: "leads",
                column: "DealerId");

            migrationBuilder.CreateIndex(
                name: "IX_leads_SellerId",
                table: "leads",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "IX_leads_Status",
                table: "leads",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_leads_VehicleId",
                table: "leads",
                column: "VehicleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lead_messages");

            migrationBuilder.DropTable(
                name: "leads");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "FeaturedPriority",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "FeaturedUntil",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "IsPremium",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "LinkedCampaignId",
                table: "vehicles");

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "UpdatedAt",
                value: new DateTime(2026, 1, 29, 10, 5, 2, 370, DateTimeKind.Utc).AddTicks(2020));

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "UpdatedAt",
                value: new DateTime(2026, 1, 29, 10, 5, 2, 370, DateTimeKind.Utc).AddTicks(2040));

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "UpdatedAt",
                value: new DateTime(2026, 1, 29, 10, 5, 2, 370, DateTimeKind.Utc).AddTicks(2060));

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "UpdatedAt",
                value: new DateTime(2026, 1, 29, 10, 5, 2, 370, DateTimeKind.Utc).AddTicks(2060));

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "UpdatedAt",
                value: new DateTime(2026, 1, 29, 10, 5, 2, 370, DateTimeKind.Utc).AddTicks(2060));

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                column: "UpdatedAt",
                value: new DateTime(2026, 1, 29, 10, 5, 2, 370, DateTimeKind.Utc).AddTicks(2060));

            migrationBuilder.UpdateData(
                table: "homepage_section_configs",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "UpdatedAt",
                value: new DateTime(2026, 1, 29, 10, 5, 2, 370, DateTimeKind.Utc).AddTicks(2730));

            migrationBuilder.UpdateData(
                table: "homepage_section_configs",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "UpdatedAt",
                value: new DateTime(2026, 1, 29, 10, 5, 2, 370, DateTimeKind.Utc).AddTicks(2740));

            migrationBuilder.UpdateData(
                table: "homepage_section_configs",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "UpdatedAt",
                value: new DateTime(2026, 1, 29, 10, 5, 2, 370, DateTimeKind.Utc).AddTicks(2740));

            migrationBuilder.UpdateData(
                table: "homepage_section_configs",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "UpdatedAt",
                value: new DateTime(2026, 1, 29, 10, 5, 2, 370, DateTimeKind.Utc).AddTicks(2740));

            migrationBuilder.UpdateData(
                table: "homepage_section_configs",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "UpdatedAt",
                value: new DateTime(2026, 1, 29, 10, 5, 2, 370, DateTimeKind.Utc).AddTicks(2740));

            migrationBuilder.UpdateData(
                table: "homepage_section_configs",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "UpdatedAt",
                value: new DateTime(2026, 1, 29, 10, 5, 2, 370, DateTimeKind.Utc).AddTicks(2740));

            migrationBuilder.UpdateData(
                table: "homepage_section_configs",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                column: "UpdatedAt",
                value: new DateTime(2026, 1, 29, 10, 5, 2, 370, DateTimeKind.Utc).AddTicks(2740));
        }
    }
}
