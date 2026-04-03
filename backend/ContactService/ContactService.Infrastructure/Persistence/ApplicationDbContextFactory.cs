using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using CarDealer.Shared.MultiTenancy;

namespace ContactService.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core migrations.
/// Used by `dotnet ef migrations add` to create ApplicationDbContext without a running host.
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=contactservice;Username=postgres;Password=postgres");

        var tenantContext = new DesignTimeTenantContext();
        return new ApplicationDbContext(optionsBuilder.Options, tenantContext);
    }

    private class DesignTimeTenantContext : ITenantContext
    {
        public Guid? CurrentDealerId => null;
        public bool HasDealerContext => false;
    }
}
