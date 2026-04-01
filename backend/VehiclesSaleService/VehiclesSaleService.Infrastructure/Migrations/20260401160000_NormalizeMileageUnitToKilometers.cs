using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VehiclesSaleService.Infrastructure.Migrations
{
    /// <summary>
    /// FIX UF-085: Normalize MileageUnit — legacy seed data tagged km values as 'Miles'.
    /// RD market uses km exclusively. Update all rows to 'Kilometers'.
    /// </summary>
    public partial class NormalizeMileageUnitToKilometers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE vehicles SET \"MileageUnit\" = 'Kilometers' WHERE \"MileageUnit\" = 'Miles' OR \"MileageUnit\" = 'mi';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally left empty — reverting would re-introduce incorrect labels.
        }
    }
}
