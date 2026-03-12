using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;

namespace AuthService.Infrastructure.Persistence;

/// <summary>
/// Seeds the default admin user and required roles after database migration.
/// This admin account should be replaced with a real admin account after initial setup.
/// </summary>
public static class AdminSeeder
{
    // Admin credentials from environment variables — NEVER hardcode in production
    public static string DefaultAdminEmail =>
        Environment.GetEnvironmentVariable("ADMIN_SEED_EMAIL") ?? "admin@okla.local";
    public static string DefaultAdminPassword =>
        Environment.GetEnvironmentVariable("ADMIN_SEED_PASSWORD")
        ?? throw new InvalidOperationException(
            "ADMIN_SEED_PASSWORD environment variable is required. "
            + "Set it before running the application.");
    public const string DefaultAdminFirstName = "Admin";
    public const string DefaultAdminLastName = "Default";

    /// <summary>
    /// Seeds required roles and the default admin user.
    /// </summary>
    public static async Task SeedAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger logger)
    {
        try
        {
            // 1. Seed Roles
            await SeedRolesAsync(roleManager, logger);

            // 2. Seed Default Admin User
            await SeedDefaultAdminAsync(context, userManager, logger);

            logger.LogInformation("Admin seeding completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during admin seeding");
            throw;
        }
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
    {
        var requiredRoles = new[] { "Admin", "Compliance", "User", "Dealer", "SuperAdmin", "Seller", "PlatformEmployee", "DealerEmployee" };

        foreach (var roleName in requiredRoles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (result.Succeeded)
                {
                    logger.LogInformation("Created role: {RoleName}", roleName);
                }
                else
                {
                    logger.LogWarning("Failed to create role {RoleName}: {Errors}",
                        roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }

    private static async Task SeedDefaultAdminAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger logger)
    {
        // Check if default admin already exists
        var existingAdmin = await userManager.FindByEmailAsync(DefaultAdminEmail);
        if (existingAdmin != null)
        {
            logger.LogInformation("Default admin user already exists, skipping creation");

            // Ensure admin has correct roles and AccountType even if already exists
            await EnsureAdminConfigurationAsync(userManager, existingAdmin, context, logger);
            return;
        }

        // Create the default admin user using UserManager (handles password hashing)
        // UserManager internally creates the user, so we need to set properties after creation
        var adminUser = new ApplicationUser(DefaultAdminEmail, DefaultAdminEmail, "temp-hash-will-be-replaced");

        // Reset the password hash since UserManager will set it properly
        // We use reflection or direct SQL to work around the protected constructor
        var createResult = await userManager.CreateAsync(adminUser, DefaultAdminPassword);

        if (!createResult.Succeeded)
        {
            // If constructor-based creation fails, try direct database insertion
            logger.LogWarning("UserManager creation failed, attempting direct database seed: {Errors}",
                string.Join(", ", createResult.Errors.Select(e => e.Description)));

            await SeedAdminDirectlyAsync(context, userManager, logger);
            return;
        }

        logger.LogInformation("Created default admin user: {Email}", DefaultAdminEmail);

        // Update AccountType and profile info
        adminUser.AccountType = AccountType.Admin;
        adminUser.FirstName = DefaultAdminFirstName;
        adminUser.LastName = DefaultAdminLastName;
        adminUser.EmailConfirmed = true;

        await userManager.UpdateAsync(adminUser);

        // Assign roles
        await EnsureAdminRolesAsync(userManager, adminUser, logger);

        logger.LogWarning(
            "⚠️  DEFAULT ADMIN CREATED - Email: {Email}. " +
            "IMPORTANT: Create a real admin account and delete this default user!",
            DefaultAdminEmail);
    }

    private static async Task SeedAdminDirectlyAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger logger)
    {
        // Check again in case of race condition
        var existingAdmin = await context.Users.FirstOrDefaultAsync(u => u.Email == DefaultAdminEmail);
        if (existingAdmin != null)
        {
            await EnsureAdminConfigurationAsync(userManager, existingAdmin, context, logger);
            return;
        }

        // Generate password hash using UserManager's hasher
        var passwordHasher = new PasswordHasher<ApplicationUser>();
        var passwordHash = passwordHasher.HashPassword(null!, DefaultAdminPassword);

        // Insert directly via SQL to bypass constructor
        var userId = Guid.NewGuid().ToString();
        var securityStamp = Guid.NewGuid().ToString("N").ToUpperInvariant();
        var concurrencyStamp = Guid.NewGuid().ToString();

        await context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO ""Users"" (
                ""Id"", ""UserName"", ""NormalizedUserName"", ""Email"", ""NormalizedEmail"",
                ""EmailConfirmed"", ""PasswordHash"", ""SecurityStamp"", ""ConcurrencyStamp"",
                ""PhoneNumberConfirmed"", ""TwoFactorEnabled"", ""LockoutEnabled"", ""AccessFailedCount"",
                ""FirstName"", ""LastName"", ""CreatedAt"", ""UpdatedAt"", ""AccountType""
            ) VALUES (
                {userId}, {DefaultAdminEmail}, {DefaultAdminEmail.ToUpperInvariant()}, {DefaultAdminEmail}, {DefaultAdminEmail.ToUpperInvariant()},
                {true}, {passwordHash}, {securityStamp}, {concurrencyStamp},
                {false}, {false}, {true}, {0},
                {DefaultAdminFirstName}, {DefaultAdminLastName}, {DateTime.UtcNow}, {DateTime.UtcNow}, {(int)AccountType.Admin}
            ) ON CONFLICT (""Id"") DO NOTHING");

        // Assign roles via SQL
        var adminRoleId = await context.Roles.Where(r => r.Name == "Admin").Select(r => r.Id).FirstOrDefaultAsync();
        var complianceRoleId = await context.Roles.Where(r => r.Name == "Compliance").Select(r => r.Id).FirstOrDefaultAsync();
        var superAdminRoleId = await context.Roles.Where(r => r.Name == "SuperAdmin").Select(r => r.Id).FirstOrDefaultAsync();

        if (!string.IsNullOrEmpty(adminRoleId))
        {
            await context.Database.ExecuteSqlInterpolatedAsync(
                $@"INSERT INTO ""AspNetUserRoles"" (""UserId"", ""RoleId"") VALUES ({userId}, {adminRoleId}) ON CONFLICT DO NOTHING");
        }
        if (!string.IsNullOrEmpty(complianceRoleId))
        {
            await context.Database.ExecuteSqlInterpolatedAsync(
                $@"INSERT INTO ""AspNetUserRoles"" (""UserId"", ""RoleId"") VALUES ({userId}, {complianceRoleId}) ON CONFLICT DO NOTHING");
        }
        if (!string.IsNullOrEmpty(superAdminRoleId))
        {
            await context.Database.ExecuteSqlInterpolatedAsync(
                $@"INSERT INTO ""AspNetUserRoles"" (""UserId"", ""RoleId"") VALUES ({userId}, {superAdminRoleId}) ON CONFLICT DO NOTHING");
        }

        logger.LogInformation("Created default admin user via direct SQL: {Email}", DefaultAdminEmail);
        logger.LogWarning(
            "⚠️  DEFAULT ADMIN CREATED - Email: {Email}. " +
            "IMPORTANT: Create a real admin account and delete this default user!",
            DefaultAdminEmail);
    }

    private static async Task EnsureAdminConfigurationAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser adminUser,
        ApplicationDbContext context,
        ILogger logger)
    {
        // Ensure AccountType is Admin
        if (adminUser.AccountType != AccountType.Admin)
        {
            adminUser.AccountType = AccountType.Admin;
            await userManager.UpdateAsync(adminUser);
            logger.LogInformation("Updated admin user AccountType to Admin");
        }

        // Ensure email is confirmed
        if (!adminUser.EmailConfirmed)
        {
            adminUser.EmailConfirmed = true;
            await userManager.UpdateAsync(adminUser);
        }

        // Ensure roles
        await EnsureAdminRolesAsync(userManager, adminUser, logger);
    }

    private static async Task EnsureAdminRolesAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser adminUser,
        ILogger logger)
    {
        var requiredRoles = new[] { "Admin", "Compliance", "SuperAdmin" };

        foreach (var role in requiredRoles)
        {
            if (!await userManager.IsInRoleAsync(adminUser, role))
            {
                var result = await userManager.AddToRoleAsync(adminUser, role);
                if (result.Succeeded)
                {
                    logger.LogInformation("Added role {Role} to admin user", role);
                }
            }
        }
    }
}
