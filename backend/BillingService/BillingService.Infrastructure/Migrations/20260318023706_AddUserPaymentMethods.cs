using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillingService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPaymentMethods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "Subscriptions",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Subscriptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "Payments",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "Invoices",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AcquisitionTrackings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DealerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    CampaignId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CampaignName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    UtmSource = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UtmMedium = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UtmCampaign = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UtmContent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UtmTerm = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AcquisitionCostUsd = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConvertedToPaid = table.Column<bool>(type: "boolean", nullable: false),
                    ConvertedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConvertedToPlan = table.Column<int>(type: "integer", nullable: true),
                    DaysToConversion = table.Column<int>(type: "integer", nullable: true),
                    ReferredByDealerId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReferralCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LandingPage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Country = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcquisitionTrackings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarketingSpends",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    CampaignId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CampaignName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    SpendUsd = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Impressions = table.Column<long>(type: "bigint", nullable: false),
                    Clicks = table.Column<long>(type: "bigint", nullable: false),
                    Signups = table.Column<int>(type: "integer", nullable: false),
                    PaidConversions = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketingSpends", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OklaCoinsTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    DealerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false),
                    BalanceAfter = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PackageSlug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AdvertisingProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: true),
                    AmountUsd = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    AdminUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OklaCoinsTransactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OklaCoinsWallets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DealerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Balance = table.Column<int>(type: "integer", nullable: false),
                    TotalPurchased = table.Column<int>(type: "integer", nullable: false),
                    TotalBonus = table.Column<int>(type: "integer", nullable: false),
                    TotalFromPlan = table.Column<int>(type: "integer", nullable: false),
                    TotalSpent = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    LastPlanCreditDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OklaCoinsWallets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReconciliationReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Period = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TotalSubscriptionsChecked = table.Column<int>(type: "integer", nullable: false),
                    TotalPaymentsChecked = table.Column<int>(type: "integer", nullable: false),
                    TotalInvoicesChecked = table.Column<int>(type: "integer", nullable: false),
                    DiscrepancyCount = table.Column<int>(type: "integer", nullable: false),
                    TotalDiscrepancyAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AutoResolvedCount = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TriggeredBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportPurchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProductId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BuyerEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    StripePaymentIntentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AmountCents = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportPurchases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionChangeHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DealerId = table.Column<Guid>(type: "uuid", nullable: false),
                    OldPlan = table.Column<int>(type: "integer", nullable: false),
                    NewPlan = table.Column<int>(type: "integer", nullable: false),
                    OldStatus = table.Column<int>(type: "integer", nullable: false),
                    NewStatus = table.Column<int>(type: "integer", nullable: false),
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    ReasonType = table.Column<int>(type: "integer", nullable: true),
                    ReasonDetails = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    OldPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NewPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChangedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StripeEventId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionChangeHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionChangeHistory_Subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPaymentMethods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Gateway = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProviderId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    NickName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UsageCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPaymentMethods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReconciliationDiscrepancies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    DealerId = table.Column<Guid>(type: "uuid", nullable: true),
                    StripePaymentIntentId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StripeSubscriptionId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StripeInvoiceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StripeCustomerId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OklaPaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    OklaSubscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    StripeAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OklaAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AmountDifference = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SuggestedAction = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IsAutoResolved = table.Column<bool>(type: "boolean", nullable: false),
                    ResolutionNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationDiscrepancies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReconciliationDiscrepancies_ReconciliationReports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "ReconciliationReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_IsDeleted",
                table: "Subscriptions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_AcquisitionTrackings_Channel",
                table: "AcquisitionTrackings",
                column: "Channel");

            migrationBuilder.CreateIndex(
                name: "IX_AcquisitionTrackings_Channel_RegisteredAt",
                table: "AcquisitionTrackings",
                columns: new[] { "Channel", "RegisteredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AcquisitionTrackings_ConvertedToPaid",
                table: "AcquisitionTrackings",
                column: "ConvertedToPaid");

            migrationBuilder.CreateIndex(
                name: "IX_AcquisitionTrackings_DealerId",
                table: "AcquisitionTrackings",
                column: "DealerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcquisitionTrackings_RegisteredAt",
                table: "AcquisitionTrackings",
                column: "RegisteredAt");

            migrationBuilder.CreateIndex(
                name: "IX_MarketingSpends_CampaignId",
                table: "MarketingSpends",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketingSpends_Channel",
                table: "MarketingSpends",
                column: "Channel");

            migrationBuilder.CreateIndex(
                name: "IX_MarketingSpends_Year_Month_Channel",
                table: "MarketingSpends",
                columns: new[] { "Year", "Month", "Channel" });

            migrationBuilder.CreateIndex(
                name: "IX_OklaCoinsTransactions_CreatedAt",
                table: "OklaCoinsTransactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OklaCoinsTransactions_DealerId",
                table: "OklaCoinsTransactions",
                column: "DealerId");

            migrationBuilder.CreateIndex(
                name: "IX_OklaCoinsTransactions_WalletId",
                table: "OklaCoinsTransactions",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "IX_OklaCoinsWallets_DealerId",
                table: "OklaCoinsWallets",
                column: "DealerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationDiscrepancies_DealerId",
                table: "ReconciliationDiscrepancies",
                column: "DealerId");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationDiscrepancies_IsAutoResolved_ResolvedAt",
                table: "ReconciliationDiscrepancies",
                columns: new[] { "IsAutoResolved", "ResolvedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationDiscrepancies_ReportId",
                table: "ReconciliationDiscrepancies",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationDiscrepancies_Severity",
                table: "ReconciliationDiscrepancies",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationDiscrepancies_Type",
                table: "ReconciliationDiscrepancies",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationReports_Period",
                table: "ReconciliationReports",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationReports_StartedAt",
                table: "ReconciliationReports",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationReports_Status",
                table: "ReconciliationReports",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ReportPurchases_BuyerEmail",
                table: "ReportPurchases",
                column: "BuyerEmail");

            migrationBuilder.CreateIndex(
                name: "IX_ReportPurchases_Status",
                table: "ReportPurchases",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ReportPurchases_StripePaymentIntentId",
                table: "ReportPurchases",
                column: "StripePaymentIntentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportPurchases_UserId",
                table: "ReportPurchases",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportPurchases_VehicleId_BuyerEmail",
                table: "ReportPurchases",
                columns: new[] { "VehicleId", "BuyerEmail" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionChangeHistory_ChangedAt",
                table: "SubscriptionChangeHistory",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionChangeHistory_DealerId",
                table: "SubscriptionChangeHistory",
                column: "DealerId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionChangeHistory_DealerId_ChangedAt",
                table: "SubscriptionChangeHistory",
                columns: new[] { "DealerId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionChangeHistory_Direction",
                table: "SubscriptionChangeHistory",
                column: "Direction");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionChangeHistory_SubscriptionId",
                table: "SubscriptionChangeHistory",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPaymentMethods_UserId",
                table: "UserPaymentMethods",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPaymentMethods_UserId_ProviderId",
                table: "UserPaymentMethods",
                columns: new[] { "UserId", "ProviderId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Subscriptions_SubscriptionId",
                table: "Invoices",
                column: "SubscriptionId",
                principalTable: "Subscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Invoices_InvoiceId",
                table: "Payments",
                column: "InvoiceId",
                principalTable: "Invoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Subscriptions_SubscriptionId",
                table: "Payments",
                column: "SubscriptionId",
                principalTable: "Subscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Subscriptions_SubscriptionId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Invoices_InvoiceId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Subscriptions_SubscriptionId",
                table: "Payments");

            migrationBuilder.DropTable(
                name: "AcquisitionTrackings");

            migrationBuilder.DropTable(
                name: "MarketingSpends");

            migrationBuilder.DropTable(
                name: "OklaCoinsTransactions");

            migrationBuilder.DropTable(
                name: "OklaCoinsWallets");

            migrationBuilder.DropTable(
                name: "ReconciliationDiscrepancies");

            migrationBuilder.DropTable(
                name: "ReportPurchases");

            migrationBuilder.DropTable(
                name: "SubscriptionChangeHistory");

            migrationBuilder.DropTable(
                name: "UserPaymentMethods");

            migrationBuilder.DropTable(
                name: "ReconciliationReports");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_IsDeleted",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "Invoices");
        }
    }
}
