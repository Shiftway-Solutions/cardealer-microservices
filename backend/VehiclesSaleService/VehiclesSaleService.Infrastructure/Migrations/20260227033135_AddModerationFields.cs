using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VehiclesSaleService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddModerationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "vehicles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedBy",
                table: "vehicles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModerationNotes",
                table: "vehicles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "vehicles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RejectedBy",
                table: "vehicles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RejectionCount",
                table: "vehicles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "vehicles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedForReviewAt",
                table: "vehicles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 27, 3, 31, 35, 207, DateTimeKind.Utc).AddTicks(8400));

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 27, 3, 31, 35, 207, DateTimeKind.Utc).AddTicks(8410));

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 27, 3, 31, 35, 207, DateTimeKind.Utc).AddTicks(8410));

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 27, 3, 31, 35, 207, DateTimeKind.Utc).AddTicks(8410));

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 27, 3, 31, 35, 207, DateTimeKind.Utc).AddTicks(8410));

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 27, 3, 31, 35, 207, DateTimeKind.Utc).AddTicks(8410));

            migrationBuilder.UpdateData(
                table: "homepage_section_configs",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 27, 3, 31, 35, 207, DateTimeKind.Utc).AddTicks(9060));

            migrationBuilder.UpdateData(
                table: "homepage_section_configs",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 27, 3, 31, 35, 207, DateTimeKind.Utc).AddTicks(9070));

            migrationBuilder.UpdateData(
                table: "homepage_section_configs",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 27, 3, 31, 35, 207, DateTimeKind.Utc).AddTicks(9070));

            migrationBuilder.UpdateData(
                table: "homepage_section_configs",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 27, 3, 31, 35, 207, DateTimeKind.Utc).AddTicks(9070));

            migrationBuilder.UpdateData(
                table: "homepage_section_configs",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 27, 3, 31, 35, 207, DateTimeKind.Utc).AddTicks(9070));

            migrationBuilder.UpdateData(
                table: "homepage_section_configs",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 27, 3, 31, 35, 207, DateTimeKind.Utc).AddTicks(9070));

            migrationBuilder.UpdateData(
                table: "homepage_section_configs",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 27, 3, 31, 35, 207, DateTimeKind.Utc).AddTicks(9070));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "ModerationNotes",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "RejectedBy",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "RejectionCount",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "SubmittedForReviewAt",
                table: "vehicles");

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
        }
    }
}
