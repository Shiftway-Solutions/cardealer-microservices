using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceFingerprintToUserSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add DeviceFingerprint column (nullable — existing sessions get NULL,
            // which the deduplication logic handles gracefully by falling through to
            // create a new session. New logins will always populate this field.)
            migrationBuilder.AddColumn<string>(
                name: "DeviceFingerprint",
                table: "UserSessions",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            // Composite index for fast fingerprint-based deduplication on login
            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId_DeviceFingerprint",
                table: "UserSessions",
                columns: new[] { "UserId", "DeviceFingerprint", "IsRevoked" });

            // Back-fill fingerprint for existing active sessions from known browser/OS combos
            // so that users with existing Chrome/Windows sessions aren't prompted for a new one.
            // Uses native PostgreSQL 11+ sha256() — no pgcrypto extension needed.
            migrationBuilder.Sql(@"
                UPDATE ""UserSessions""
                SET ""DeviceFingerprint"" = substring(
                    encode(
                        sha256(convert_to(""Browser"" || '|' || ""OperatingSystem"" || '|' || ""DeviceInfo"", 'UTF8')),
                        'hex'
                    ),
                    1, 16
                )
                WHERE ""DeviceFingerprint"" IS NULL
                  AND ""Browser"" <> 'Unknown Browser'
                  AND ""IsRevoked"" = FALSE;
            ");

            // Revoke old unknown-browser duplicate sessions to clean up accumulated noise
            // (sessions where Browser='Unknown Browser' and OS='Unknown OS' are k8s pod artifacts)
            migrationBuilder.Sql(@"
                UPDATE ""UserSessions""
                SET ""IsRevoked"" = TRUE,
                    ""RevokedAt"" = NOW() AT TIME ZONE 'utc',
                    ""RevokedReason"" = 'auto-cleanup: duplicate unknown-browser session'
                WHERE ""Browser"" = 'Unknown Browser'
                  AND ""OperatingSystem"" = 'Unknown OS'
                  AND ""IsRevoked"" = FALSE
                  AND ""Id"" NOT IN (
                    SELECT DISTINCT ON (""UserId"") ""Id""
                    FROM ""UserSessions""
                    WHERE ""Browser"" = 'Unknown Browser'
                      AND ""OperatingSystem"" = 'Unknown OS'
                      AND ""IsRevoked"" = FALSE
                    ORDER BY ""UserId"", ""LastActiveAt"" DESC
                  );
            ");

            // Back-fill fingerprint for the surviving unknown-browser sessions
            migrationBuilder.Sql(@"
                UPDATE ""UserSessions""
                SET ""DeviceFingerprint"" = substring(
                    encode(
                        sha256(convert_to(""Browser"" || '|' || ""OperatingSystem"" || '|' || ""DeviceInfo"", 'UTF8')),
                        'hex'
                    ),
                    1, 16
                )
                WHERE ""DeviceFingerprint"" IS NULL
                  AND ""IsRevoked"" = FALSE;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserSessions_UserId_DeviceFingerprint",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "DeviceFingerprint",
                table: "UserSessions");
        }
    }
}
