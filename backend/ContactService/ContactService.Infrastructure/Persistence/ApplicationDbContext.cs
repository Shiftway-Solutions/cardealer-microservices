using Microsoft.EntityFrameworkCore;
using ContactService.Domain.Entities;
using CarDealer.Shared.MultiTenancy;

namespace ContactService.Infrastructure.Persistence
{
    public class ApplicationDbContext : MultiTenantDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantContext tenantContext)
            : base(options, tenantContext)
        {
        }

        public DbSet<ContactRequest> ContactRequests => Set<ContactRequest>();
        public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}
