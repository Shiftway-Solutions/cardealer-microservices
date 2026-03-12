using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeLocationFieldsWithIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create indexes for Location fields (FASE 2)

            // Composite index: City + State (most common search)
            migrationBuilder.CreateIndex(
                name: "idx_seller_profiles_city_state",
                table: "SellerProfiles",
                columns: new[] { "City", "State" },
                filter: "\"City\" IS NOT NULL AND \"State\" IS NOT NULL");

            // Simple index: State (filter by province)
            migrationBuilder.CreateIndex(
                name: "idx_seller_profiles_state",
                table: "SellerProfiles",
                column: "State",
                filter: "\"State\" IS NOT NULL");

            // Simple index: City (filter by city)
            migrationBuilder.CreateIndex(
                name: "idx_seller_profiles_city",
                table: "SellerProfiles",
                column: "City",
                filter: "\"City\" IS NOT NULL");

            // Simple index: ZipCode (filter by postal code)
            migrationBuilder.CreateIndex(
                name: "idx_seller_profiles_zipcode",
                table: "SellerProfiles",
                column: "ZipCode",
                filter: "\"ZipCode\" IS NOT NULL");

            // Composite index: VerificationStatus + Location (verified sellers in location)
            migrationBuilder.CreateIndex(
                name: "idx_seller_profiles_verification_location",
                table: "SellerProfiles",
                columns: new[] { "VerificationStatus", "City", "State" },
                filter: "\"VerificationStatus\" = 3");

            // GIN index: Specialties + Location (for verified sellers)
            migrationBuilder.CreateIndex(
                name: "idx_seller_profiles_specialties_location",
                table: "SellerProfiles",
                column: "Specialties",
                filter: "\"VerificationStatus\" = 3")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop all location indexes
            migrationBuilder.DropIndex(
                name: "idx_seller_profiles_city_state",
                table: "SellerProfiles");

            migrationBuilder.DropIndex(
                name: "idx_seller_profiles_state",
                table: "SellerProfiles");

            migrationBuilder.DropIndex(
                name: "idx_seller_profiles_city",
                table: "SellerProfiles");

            migrationBuilder.DropIndex(
                name: "idx_seller_profiles_zipcode",
                table: "SellerProfiles");

            migrationBuilder.DropIndex(
                name: "idx_seller_profiles_verification_location",
                table: "SellerProfiles");

            migrationBuilder.DropIndex(
                name: "idx_seller_profiles_specialties_location",
                table: "SellerProfiles");
        }
    }
}
