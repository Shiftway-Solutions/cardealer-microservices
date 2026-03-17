using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillingService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrencyAndDeclineReasonToAzulTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add currency column — tracks DOP/USD for Dominican card reconciliation.
            // DEFAULT 'DOP' so existing rows are populated correctly.
            migrationBuilder.AddColumn<string>(
                name: "currency",
                table: "azul_transactions",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "DOP");

            // Add decline_reason_localized column — Spanish decline reason for Dominican banks.
            migrationBuilder.AddColumn<string>(
                name: "decline_reason_localized",
                table: "azul_transactions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "currency",
                table: "azul_transactions");

            migrationBuilder.DropColumn(
                name: "decline_reason_localized",
                table: "azul_transactions");
        }
    }
}
