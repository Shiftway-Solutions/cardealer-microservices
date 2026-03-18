using BillingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BillingService.Infrastructure.Persistence;

public class BillingDbContext : DbContext
{
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<StripeCustomer> StripeCustomers => Set<StripeCustomer>();
    public DbSet<EarlyBirdMember> EarlyBirdMembers => Set<EarlyBirdMember>();
    public DbSet<AzulTransaction> AzulTransactions => Set<AzulTransaction>();
    // AUDIT FIX: OklaCoins entities were missing from DbContext — wallet and transactions
    // were defined as domain entities but never registered, making the entire OKLA Coins
    // feature non-functional (controller returned hardcoded mock data)
    public DbSet<OklaCoinsWallet> OklaCoinsWallets => Set<OklaCoinsWallet>();
    public DbSet<OklaCoinsTransaction> OklaCoinsTransactions => Set<OklaCoinsTransaction>();

    // RETENTION FIX: Track every subscription plan change for churn analytics
    public DbSet<SubscriptionChangeHistory> SubscriptionChangeHistory => Set<SubscriptionChangeHistory>();

    // KPI AUDIT FIX: CAC tracking — acquisition source and marketing spend per channel
    public DbSet<AcquisitionTracking> AcquisitionTrackings => Set<AcquisitionTracking>();
    public DbSet<MarketingSpend> MarketingSpends => Set<MarketingSpend>();

    // CONTRA #6 FIX: Payment reconciliation — daily Stripe↔OKLA DB audit
    public DbSet<ReconciliationReport> ReconciliationReports => Set<ReconciliationReport>();
    public DbSet<ReconciliationDiscrepancy> ReconciliationDiscrepancies => Set<ReconciliationDiscrepancy>();
    public DbSet<ReportPurchase> ReportPurchases => Set<ReportPurchase>();

    public BillingDbContext(DbContextOptions<BillingDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DealerId).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Plan);
            entity.HasIndex(e => e.StripeCustomerId);
            entity.HasIndex(e => e.StripeSubscriptionId);

            entity.Property(e => e.PricePerCycle).HasPrecision(18, 2);
            entity.Property(e => e.Currency).HasMaxLength(3);
            entity.Property(e => e.StripeCustomerId).HasMaxLength(100);
            entity.Property(e => e.StripeSubscriptionId).HasMaxLength(100);
            entity.Property(e => e.StripePaymentMethodId).HasMaxLength(100);
            entity.Property(e => e.CancellationReason).HasMaxLength(500);

            // ✅ AUDIT FIX: Concurrency token
            entity.Property(e => e.ConcurrencyStamp)
                .IsConcurrencyToken()
                .HasMaxLength(36);

            // ✅ AUDIT FIX: Soft delete filter
            entity.HasQueryFilter(e => !e.IsDeleted);
            entity.HasIndex(e => e.IsDeleted);

            // ✅ AUDIT FIX: Navigation properties (one-to-many)
            entity.HasMany(e => e.Payments)
                .WithOne(p => p.Subscription)
                .HasForeignKey(p => p.SubscriptionId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.Invoices)
                .WithOne(i => i.Subscription)
                .HasForeignKey(i => i.SubscriptionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DealerId);
            entity.HasIndex(e => e.SubscriptionId);
            entity.HasIndex(e => e.InvoiceId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.StripePaymentIntentId);

            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.RefundedAmount).HasPrecision(18, 2);
            entity.Property(e => e.Currency).HasMaxLength(3);
            entity.Property(e => e.StripePaymentIntentId).HasMaxLength(100);
            entity.Property(e => e.StripeChargeId).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ReceiptUrl).HasMaxLength(500);
            entity.Property(e => e.FailureReason).HasMaxLength(500);
            entity.Property(e => e.RefundReason).HasMaxLength(500);

            // ✅ AUDIT FIX: Concurrency token
            entity.Property(e => e.ConcurrencyStamp)
                .IsConcurrencyToken()
                .HasMaxLength(36);

            // ✅ AUDIT FIX: FK to Invoice
            entity.HasOne(e => e.Invoice)
                .WithMany()
                .HasForeignKey(e => e.InvoiceId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DealerId);
            entity.HasIndex(e => e.InvoiceNumber).IsUnique();
            entity.HasIndex(e => e.SubscriptionId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.StripeInvoiceId);

            entity.Property(e => e.InvoiceNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Subtotal).HasPrecision(18, 2);
            entity.Property(e => e.TaxAmount).HasPrecision(18, 2);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.PaidAmount).HasPrecision(18, 2);
            entity.Property(e => e.Currency).HasMaxLength(3);
            entity.Property(e => e.StripeInvoiceId).HasMaxLength(100);
            entity.Property(e => e.PdfUrl).HasMaxLength(500);
            entity.Property(e => e.Notes).HasMaxLength(2000);

            // ✅ AUDIT FIX: Concurrency token
            entity.Property(e => e.ConcurrencyStamp)
                .IsConcurrencyToken()
                .HasMaxLength(36);
        });

        modelBuilder.Entity<StripeCustomer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DealerId).IsUnique();
            entity.HasIndex(e => e.StripeCustomerId).IsUnique();

            entity.Property(e => e.StripeCustomerId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.Metadata).HasMaxLength(4000);
            entity.Property(e => e.DefaultPaymentMethodId).HasMaxLength(100);
        });

        modelBuilder.Entity<EarlyBirdMember>(entity =>
        {
            entity.ToTable("early_bird_members");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => e.HasUsedBenefit);
            entity.HasIndex(e => e.FreeUntil);

            entity.Property(e => e.Id)
                .HasColumnName("Id");

            entity.Property(e => e.UserId)
                .IsRequired()
                .HasColumnName("UserId");

            entity.Property(e => e.EnrolledAt)
                .IsRequired()
                .HasColumnName("EnrolledAt");

            entity.Property(e => e.FreeUntil)
                .IsRequired()
                .HasColumnName("FreeUntil");

            entity.Property(e => e.HasUsedBenefit)
                .IsRequired()
                .HasColumnName("HasUsedBenefit")
                .HasDefaultValue(false);

            entity.Property(e => e.BenefitUsedAt)
                .HasColumnName("BenefitUsedAt");

            entity.Property(e => e.SubscriptionIdWhenUsed)
                .HasMaxLength(100)
                .HasColumnName("SubscriptionIdWhenUsed");

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasColumnName("CreatedAt")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Apply AzulTransaction configuration
        modelBuilder.ApplyConfiguration(new Configurations.AzulTransactionConfiguration());

        // ========================================
        // OKLA Coins — Wallet & Transactions
        // AUDIT FIX: These entities were domain-only with no persistence.
        // ========================================
        modelBuilder.Entity<OklaCoinsWallet>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DealerId).IsUnique();

            entity.Property(e => e.Currency).HasMaxLength(10).HasDefaultValue("USD");
        });

        modelBuilder.Entity<OklaCoinsTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.WalletId);
            entity.HasIndex(e => e.DealerId);
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.AmountUsd).HasPrecision(18, 2);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.PackageSlug).HasMaxLength(100);
        });

        // RETENTION FIX: Subscription Change History for churn analytics
        modelBuilder.Entity<SubscriptionChangeHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DealerId);
            entity.HasIndex(e => e.SubscriptionId);
            entity.HasIndex(e => e.Direction);
            entity.HasIndex(e => e.ChangedAt);
            entity.HasIndex(e => new { e.DealerId, e.ChangedAt });

            entity.Property(e => e.OldPrice).HasPrecision(18, 2);
            entity.Property(e => e.NewPrice).HasPrecision(18, 2);
            entity.Property(e => e.Currency).HasMaxLength(3);
            entity.Property(e => e.ReasonDetails).HasMaxLength(1000);
            entity.Property(e => e.ChangedBy).HasMaxLength(100);
            entity.Property(e => e.StripeEventId).HasMaxLength(100);

            entity.HasOne(e => e.Subscription)
                .WithMany()
                .HasForeignKey(e => e.SubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ========================================
        // KPI AUDIT FIX: Acquisition Tracking for CAC
        // ========================================
        modelBuilder.Entity<AcquisitionTracking>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DealerId).IsUnique();
            entity.HasIndex(e => e.Channel);
            entity.HasIndex(e => e.RegisteredAt);
            entity.HasIndex(e => e.ConvertedToPaid);
            entity.HasIndex(e => new { e.Channel, e.RegisteredAt });

            entity.Property(e => e.AcquisitionCostUsd).HasPrecision(18, 2);
            entity.Property(e => e.CampaignId).HasMaxLength(200);
            entity.Property(e => e.CampaignName).HasMaxLength(300);
            entity.Property(e => e.UtmSource).HasMaxLength(200);
            entity.Property(e => e.UtmMedium).HasMaxLength(200);
            entity.Property(e => e.UtmCampaign).HasMaxLength(200);
            entity.Property(e => e.UtmContent).HasMaxLength(500);
            entity.Property(e => e.UtmTerm).HasMaxLength(200);
            entity.Property(e => e.ReferralCode).HasMaxLength(50);
            entity.Property(e => e.LandingPage).HasMaxLength(500);
            entity.Property(e => e.Country).HasMaxLength(5);
            entity.Property(e => e.Notes).HasMaxLength(1000);
        });

        // ========================================
        // KPI AUDIT FIX: Marketing Spend for aggregated CAC
        // ========================================
        modelBuilder.Entity<MarketingSpend>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Year, e.Month, e.Channel });
            entity.HasIndex(e => e.Channel);
            entity.HasIndex(e => e.CampaignId);

            entity.Property(e => e.SpendUsd).HasPrecision(18, 2);
            entity.Property(e => e.CampaignId).HasMaxLength(200);
            entity.Property(e => e.CampaignName).HasMaxLength(300);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
        });

        // ========================================
        // Report Purchases (OKLA Score™ one-time reports)
        // ========================================
        modelBuilder.Entity<ReportPurchase>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.VehicleId, e.BuyerEmail });
            entity.HasIndex(e => e.BuyerEmail);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.StripePaymentIntentId).IsUnique();
            entity.HasIndex(e => e.Status);

            entity.Property(e => e.VehicleId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ProductId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.BuyerEmail).HasMaxLength(256).IsRequired();
            entity.Property(e => e.StripePaymentIntentId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Currency).HasMaxLength(3);
            entity.Property(e => e.ConcurrencyStamp)
                .IsConcurrencyToken()
                .HasMaxLength(36);
        });

        // ========================================
        // CONTRA #6 FIX: Payment Reconciliation Reports
        // ========================================
        modelBuilder.Entity<ReconciliationReport>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Period);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.StartedAt);

            entity.Property(e => e.Period).HasMaxLength(7).IsRequired(); // YYYY-MM
            entity.Property(e => e.TotalDiscrepancyAmount).HasPrecision(18, 2);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.Property(e => e.TriggeredBy).HasMaxLength(200);

            entity.HasMany(e => e.Discrepancies)
                .WithOne(d => d.Report)
                .HasForeignKey(d => d.ReportId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReconciliationDiscrepancy>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ReportId);
            entity.HasIndex(e => e.DealerId);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Severity);
            entity.HasIndex(e => new { e.IsAutoResolved, e.ResolvedAt });

            entity.Property(e => e.StripePaymentIntentId).HasMaxLength(200);
            entity.Property(e => e.StripeSubscriptionId).HasMaxLength(200);
            entity.Property(e => e.StripeInvoiceId).HasMaxLength(200);
            entity.Property(e => e.StripeCustomerId).HasMaxLength(200);
            entity.Property(e => e.StripeAmount).HasPrecision(18, 2);
            entity.Property(e => e.OklaAmount).HasPrecision(18, 2);
            entity.Property(e => e.AmountDifference).HasPrecision(18, 2);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.SuggestedAction).HasMaxLength(1000);
            entity.Property(e => e.ResolutionNotes).HasMaxLength(2000);
            entity.Property(e => e.ResolvedBy).HasMaxLength(200);
        });
    }

    // ✅ AUDIT FIX: Auto-update timestamps and concurrency stamps
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Modified)
            {
                // Update ConcurrencyStamp on any modified entity that has one
                var concurrencyProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "ConcurrencyStamp");
                if (concurrencyProp != null)
                {
                    concurrencyProp.CurrentValue = Guid.NewGuid().ToString();
                }

                // Update UpdatedAt on any modified entity that has one
                var updatedAtProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAt");
                if (updatedAtProp != null)
                {
                    updatedAtProp.CurrentValue = utcNow;
                }
            }
            else if (entry.State == EntityState.Added)
            {
                var createdAtProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "CreatedAt");
                if (createdAtProp != null && createdAtProp.CurrentValue is DateTime dt && dt == default)
                {
                    createdAtProp.CurrentValue = utcNow;
                }

                var concurrencyProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "ConcurrencyStamp");
                if (concurrencyProp != null)
                {
                    concurrencyProp.CurrentValue = Guid.NewGuid().ToString();
                }
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
