using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContactService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BUG_S22_3_FixEncryptedVarcharWidth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ContactMessages_IsRead",
                table: "ContactMessages");

            migrationBuilder.DropIndex(
                name: "IX_ContactMessages_SentAt",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ContactRequests");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ContactMessages");

            migrationBuilder.RenameIndex(
                name: "IX_ContactRequests_DealerId",
                table: "ContactRequests",
                newName: "IX_ContactRequest_DealerId");

            migrationBuilder.RenameIndex(
                name: "IX_ContactMessages_DealerId",
                table: "ContactMessages",
                newName: "IX_ContactMessage_DealerId");

            migrationBuilder.AlterColumn<string>(
                name: "Subject",
                table: "ContactRequests",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ContactRequests",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Open",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true,
                oldDefaultValue: "Open");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "ContactRequests",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ContactRequests",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldDefaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "ContactRequests",
                type: "character varying(5000)",
                maxLength: 5000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "ContactRequests",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldDefaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "BuyerPhone",
                table: "ContactRequests",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BuyerEmail",
                table: "ContactRequests",
                type: "character varying(254)",
                maxLength: 254,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "Fbclid",
                table: "ContactRequests",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gclid",
                table: "ContactRequests",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LandingPage",
                table: "ContactRequests",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UtmCampaign",
                table: "ContactRequests",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UtmContent",
                table: "ContactRequests",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UtmMedium",
                table: "ContactRequests",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UtmSource",
                table: "ContactRequests",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UtmTerm",
                table: "ContactRequests",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "ContactMessages",
                type: "character varying(5000)",
                maxLength: 5000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ContactMessages",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "SenderName",
                table: "ContactMessages",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ContactMessages_IsRead_IsFromBuyer",
                table: "ContactMessages",
                columns: new[] { "IsRead", "IsFromBuyer" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ContactMessages_IsRead_IsFromBuyer",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "Fbclid",
                table: "ContactRequests");

            migrationBuilder.DropColumn(
                name: "Gclid",
                table: "ContactRequests");

            migrationBuilder.DropColumn(
                name: "LandingPage",
                table: "ContactRequests");

            migrationBuilder.DropColumn(
                name: "UtmCampaign",
                table: "ContactRequests");

            migrationBuilder.DropColumn(
                name: "UtmContent",
                table: "ContactRequests");

            migrationBuilder.DropColumn(
                name: "UtmMedium",
                table: "ContactRequests");

            migrationBuilder.DropColumn(
                name: "UtmSource",
                table: "ContactRequests");

            migrationBuilder.DropColumn(
                name: "UtmTerm",
                table: "ContactRequests");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "SenderName",
                table: "ContactMessages");

            migrationBuilder.RenameIndex(
                name: "IX_ContactRequest_DealerId",
                table: "ContactRequests",
                newName: "IX_ContactRequests_DealerId");

            migrationBuilder.RenameIndex(
                name: "IX_ContactMessage_DealerId",
                table: "ContactMessages",
                newName: "IX_ContactMessages_DealerId");

            migrationBuilder.AlterColumn<string>(
                name: "Subject",
                table: "ContactRequests",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ContactRequests",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                defaultValue: "Open",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Open");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "ContactRequests",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ContactRequests",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "ContactRequests",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(5000)",
                oldMaxLength: 5000);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "ContactRequests",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "BuyerPhone",
                table: "ContactRequests",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BuyerEmail",
                table: "ContactRequests",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(254)",
                oldMaxLength: 254);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "ContactRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "ContactMessages",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(5000)",
                oldMaxLength: 5000);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "ContactMessages",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContactMessages_IsRead",
                table: "ContactMessages",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_ContactMessages_SentAt",
                table: "ContactMessages",
                column: "SentAt");
        }
    }
}
