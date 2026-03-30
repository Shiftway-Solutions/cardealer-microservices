using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountTypeAndSellerFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                DECLARE
                    account_type_data_type text;
                BEGIN
                    SELECT data_type
                    INTO account_type_data_type
                    FROM information_schema.columns
                    WHERE table_schema = 'public'
                      AND table_name = 'Users'
                      AND column_name = 'AccountType';

                    IF account_type_data_type IS NULL THEN
                        ALTER TABLE "Users"
                            ADD COLUMN "AccountType" character varying(50) NOT NULL DEFAULT 'Individual';
                    ELSIF account_type_data_type = 'integer' THEN
                        ALTER TABLE "Users"
                            ALTER COLUMN "AccountType" DROP DEFAULT;

                        ALTER TABLE "Users"
                            ALTER COLUMN "AccountType" TYPE character varying(50)
                            USING CASE "AccountType"
                                WHEN 0 THEN 'Guest'
                                WHEN 1 THEN 'Individual'
                                WHEN 2 THEN 'Dealer'
                                WHEN 3 THEN 'DealerEmployee'
                                WHEN 4 THEN 'Admin'
                                WHEN 5 THEN 'PlatformEmployee'
                                WHEN 6 THEN 'Seller'
                                ELSE 'Individual'
                            END;

                        UPDATE "Users"
                        SET "AccountType" = 'Individual'
                        WHERE "AccountType" IS NULL OR "AccountType" = '';

                        ALTER TABLE "Users"
                            ALTER COLUMN "AccountType" SET DEFAULT 'Individual',
                            ALTER COLUMN "AccountType" SET NOT NULL;
                    ELSE
                        UPDATE "Users"
                        SET "AccountType" = 'Individual'
                        WHERE "AccountType" IS NULL OR "AccountType" = '';

                        ALTER TABLE "Users"
                            ALTER COLUMN "AccountType" SET DEFAULT 'Individual',
                            ALTER COLUMN "AccountType" SET NOT NULL;
                    END IF;
                END $$;
                """);

            // AvatarUrl and Bio already exist from previous migration
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                DECLARE
                    account_type_data_type text;
                BEGIN
                    SELECT data_type
                    INTO account_type_data_type
                    FROM information_schema.columns
                    WHERE table_schema = 'public'
                      AND table_name = 'Users'
                      AND column_name = 'AccountType';

                    IF account_type_data_type = 'character varying' THEN
                        ALTER TABLE "Users"
                            ALTER COLUMN "AccountType" DROP DEFAULT;

                        ALTER TABLE "Users"
                            ALTER COLUMN "AccountType" TYPE integer
                            USING CASE "AccountType"
                                WHEN 'Guest' THEN 0
                                WHEN 'Buyer' THEN 1
                                WHEN 'Individual' THEN 1
                                WHEN 'Dealer' THEN 2
                                WHEN 'DealerEmployee' THEN 3
                                WHEN 'Admin' THEN 4
                                WHEN 'PlatformEmployee' THEN 5
                                WHEN 'Seller' THEN 6
                                ELSE 1
                            END;

                        ALTER TABLE "Users"
                            ALTER COLUMN "AccountType" SET DEFAULT 1,
                            ALTER COLUMN "AccountType" SET NOT NULL;
                    END IF;
                END $$;
                """);
        }
    }
}
