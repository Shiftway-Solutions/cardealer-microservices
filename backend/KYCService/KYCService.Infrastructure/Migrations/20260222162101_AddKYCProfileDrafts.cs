using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KYCService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddKYCProfileDrafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "storage_key",
                table: "kyc_documents",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.CreateTable(
                name: "kyc_profile_drafts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_step = table.Column<int>(type: "integer", nullable: false),
                    form_data = table.Column<string>(type: "jsonb", nullable: false),
                    is_submitted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kyc_profile_drafts", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_kyc_profile_drafts_expires_at",
                table: "kyc_profile_drafts",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_kyc_profile_drafts_is_submitted",
                table: "kyc_profile_drafts",
                column: "is_submitted");

            migrationBuilder.CreateIndex(
                name: "IX_kyc_profile_drafts_user_id",
                table: "kyc_profile_drafts",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "kyc_profile_drafts");

            migrationBuilder.AlterColumn<string>(
                name: "storage_key",
                table: "kyc_documents",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
