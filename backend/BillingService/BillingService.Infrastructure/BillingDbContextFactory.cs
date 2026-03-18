using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using BillingService.Infrastructure.Persistence;

namespace BillingService.Infrastructure;

/// <summary>
/// EF Core design-time factory — used only by `dotnet ef migrations` tooling.
/// Reads the connection string from the DATABASE_URL env var (for CI) or falls back
/// to a local dev Postgres instance.
/// </summary>
public class BillingDbContextFactory : IDesignTimeDbContextFactory<BillingDbContext>
{
    public BillingDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? "Host=localhost;Port=5432;Database=billing_dev;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<BillingDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new BillingDbContext(optionsBuilder.Options);
    }
}
