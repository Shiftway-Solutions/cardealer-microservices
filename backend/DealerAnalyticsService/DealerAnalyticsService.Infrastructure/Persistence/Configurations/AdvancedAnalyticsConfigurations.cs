using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DealerAnalyticsService.Domain.Entities;

namespace DealerAnalyticsService.Infrastructure.Persistence.Configurations;

public class DealerSnapshotConfiguration : IEntityTypeConfiguration<DealerSnapshot>
{
    public void Configure(EntityTypeBuilder<DealerSnapshot> builder)
    {
        builder.ToTable("dealer_snapshots");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.DealerId)
            .HasColumnName("dealer_id")
            .IsRequired();

        builder.Property(e => e.SnapshotDate)
            .HasColumnName("snapshot_date")
            .IsRequired();

        // Inventory metrics
        builder.Property(e => e.TotalVehicles).HasColumnName("total_vehicles");
        builder.Property(e => e.ActiveVehicles).HasColumnName("active_vehicles");
        builder.Property(e => e.SoldVehicles).HasColumnName("sold_vehicles");
        builder.Property(e => e.PendingVehicles).HasColumnName("pending_vehicles");
        builder.Property(e => e.TotalInventoryValue).HasColumnName("total_inventory_value").HasPrecision(18, 2);
        builder.Property(e => e.AvgVehiclePrice).HasColumnName("avg_vehicle_price").HasPrecision(18, 2);
        builder.Property(e => e.AvgDaysOnMarket).HasColumnName("avg_days_on_market");
        builder.Property(e => e.VehiclesOver60Days).HasColumnName("vehicles_over_60_days");

        // Engagement metrics
        builder.Property(e => e.TotalViews).HasColumnName("total_views");
        builder.Property(e => e.UniqueViews).HasColumnName("unique_views");
        builder.Property(e => e.TotalContacts).HasColumnName("total_contacts");
        builder.Property(e => e.PhoneCalls).HasColumnName("phone_calls");
        builder.Property(e => e.WhatsAppMessages).HasColumnName("whatsapp_messages");
        builder.Property(e => e.EmailInquiries).HasColumnName("email_inquiries");
        builder.Property(e => e.TotalFavorites).HasColumnName("total_favorites");
        builder.Property(e => e.SearchImpressions).HasColumnName("search_impressions");
        builder.Property(e => e.SearchClicks).HasColumnName("search_clicks");

        // Lead metrics
        builder.Property(e => e.NewLeads).HasColumnName("new_leads");
        builder.Property(e => e.QualifiedLeads).HasColumnName("qualified_leads");
        builder.Property(e => e.HotLeads).HasColumnName("hot_leads");
        builder.Property(e => e.ConvertedLeads).HasColumnName("converted_leads");
        builder.Property(e => e.LeadConversionRate).HasColumnName("lead_conversion_rate");
        builder.Property(e => e.AvgResponseTimeMinutes).HasColumnName("avg_response_time_minutes");

        // Revenue metrics
        builder.Property(e => e.TotalRevenue).HasColumnName("total_revenue").HasPrecision(18, 2);
        builder.Property(e => e.AvgTransactionValue).HasColumnName("avg_transaction_value").HasPrecision(18, 2);
        builder.Property(e => e.TransactionCount).HasColumnName("transaction_count");

        // Timestamps
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        // Ignore computed properties
        builder.Ignore(e => e.ClickThroughRate);
        builder.Ignore(e => e.ContactRate);
        builder.Ignore(e => e.FavoriteRate);
        builder.Ignore(e => e.InventoryTurnoverRate);
        builder.Ignore(e => e.AgingRate);

        // Indices
        builder.HasIndex(e => e.DealerId).HasDatabaseName("ix_dealer_snapshots_dealer_id");
        builder.HasIndex(e => e.SnapshotDate).HasDatabaseName("ix_dealer_snapshots_date");
        builder.HasIndex(e => new { e.DealerId, e.SnapshotDate }).IsUnique().HasDatabaseName("ix_dealer_snapshots_dealer_date");
    }
}

public class VehiclePerformanceConfiguration : IEntityTypeConfiguration<VehiclePerformance>
{
    public void Configure(EntityTypeBuilder<VehiclePerformance> builder)
    {
        builder.ToTable("vehicle_performances");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.VehicleId).HasColumnName("vehicle_id").IsRequired();
        builder.Property(e => e.DealerId).HasColumnName("dealer_id").IsRequired();
        builder.Property(e => e.VehicleTitle).HasColumnName("vehicle_title").HasMaxLength(255);
        builder.Property(e => e.VehicleMake).HasColumnName("vehicle_make").HasMaxLength(100);
        builder.Property(e => e.VehicleModel).HasColumnName("vehicle_model").HasMaxLength(100);
        builder.Property(e => e.VehicleYear).HasColumnName("vehicle_year");
        builder.Property(e => e.VehiclePrice).HasColumnName("vehicle_price").HasPrecision(18, 2);
        builder.Property(e => e.VehicleThumbnailUrl).HasColumnName("vehicle_thumbnail_url").HasMaxLength(500);
        builder.Property(e => e.Date).HasColumnName("date").IsRequired();

        // View metrics
        builder.Property(e => e.Views).HasColumnName("views");
        builder.Property(e => e.UniqueViews).HasColumnName("unique_views");

        // Contact metrics
        builder.Property(e => e.Contacts).HasColumnName("contacts");
        builder.Property(e => e.PhoneCalls).HasColumnName("phone_calls");
        builder.Property(e => e.WhatsAppClicks).HasColumnName("whatsapp_clicks");
        builder.Property(e => e.EmailInquiries).HasColumnName("email_inquiries");

        // Engagement metrics
        builder.Property(e => e.Favorites).HasColumnName("favorites");
        builder.Property(e => e.ShareClicks).HasColumnName("share_clicks");
        builder.Property(e => e.SearchImpressions).HasColumnName("search_impressions");
        builder.Property(e => e.SearchClicks).HasColumnName("search_clicks");
        builder.Property(e => e.AvgViewDurationSeconds).HasColumnName("avg_view_duration_seconds");
        builder.Property(e => e.PhotoGalleryViews).HasColumnName("photo_gallery_views");

        // Status
        builder.Property(e => e.DaysOnMarket).HasColumnName("days_on_market");
        builder.Property(e => e.IsSold).HasColumnName("is_sold");
        builder.Property(e => e.SoldDate).HasColumnName("sold_date");

        // Scores
        builder.Property(e => e.EngagementScore).HasColumnName("engagement_score");
        builder.Property(e => e.PerformanceScore).HasColumnName("performance_score");

        // Timestamps
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        // Ignore computed properties
        builder.Ignore(e => e.ClickThroughRate);
        builder.Ignore(e => e.ContactRate);
        builder.Ignore(e => e.FavoriteRate);

        // Indices
        builder.HasIndex(e => e.VehicleId).HasDatabaseName("ix_vehicle_performances_vehicle_id");
        builder.HasIndex(e => e.DealerId).HasDatabaseName("ix_vehicle_performances_dealer_id");
        builder.HasIndex(e => e.Date).HasDatabaseName("ix_vehicle_performances_date");
        builder.HasIndex(e => new { e.VehicleId, e.Date }).IsUnique().HasDatabaseName("ix_vehicle_performances_vehicle_date");
        builder.HasIndex(e => e.PerformanceScore).HasDatabaseName("ix_vehicle_performances_score");
    }
}

public class LeadFunnelMetricsConfiguration : IEntityTypeConfiguration<LeadFunnelMetrics>
{
    public void Configure(EntityTypeBuilder<LeadFunnelMetrics> builder)
    {
        builder.ToTable("lead_funnel_metrics");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.DealerId).HasColumnName("dealer_id").IsRequired();
        builder.Property(e => e.PeriodStart).HasColumnName("period_start").IsRequired();
        builder.Property(e => e.PeriodEnd).HasColumnName("period_end").IsRequired();
        builder.Property(e => e.PeriodType).HasColumnName("period_type").HasMaxLength(20);

        // Funnel stages
        builder.Property(e => e.Impressions).HasColumnName("impressions");
        builder.Property(e => e.Views).HasColumnName("views");
        builder.Property(e => e.Contacts).HasColumnName("contacts");
        builder.Property(e => e.Qualified).HasColumnName("qualified");
        builder.Property(e => e.Negotiation).HasColumnName("negotiation");
        builder.Property(e => e.Converted).HasColumnName("converted");

        // Source breakdown
        builder.Property(e => e.OrganicImpressions).HasColumnName("organic_impressions");
        builder.Property(e => e.PaidImpressions).HasColumnName("paid_impressions");
        builder.Property(e => e.ReferralImpressions).HasColumnName("referral_impressions");
        builder.Property(e => e.DirectViews).HasColumnName("direct_views");
        builder.Property(e => e.SearchViews).HasColumnName("search_views");

        // Contact type breakdown
        builder.Property(e => e.PhoneContacts).HasColumnName("phone_contacts");
        builder.Property(e => e.WhatsAppContacts).HasColumnName("whatsapp_contacts");
        builder.Property(e => e.EmailContacts).HasColumnName("email_contacts");
        builder.Property(e => e.ChatContacts).HasColumnName("chat_contacts");

        // Lead quality
        builder.Property(e => e.HotLeads).HasColumnName("hot_leads");
        builder.Property(e => e.WarmLeads).HasColumnName("warm_leads");
        builder.Property(e => e.ColdLeads).HasColumnName("cold_leads");

        // Timing metrics
        builder.Property(e => e.AvgTimeToFirstContact).HasColumnName("avg_time_to_first_contact");
        builder.Property(e => e.AvgTimeToQualification).HasColumnName("avg_time_to_qualification");
        builder.Property(e => e.AvgTimeToClose).HasColumnName("avg_time_to_close");

        // Revenue attribution
        builder.Property(e => e.AttributedRevenue).HasColumnName("attributed_revenue").HasPrecision(18, 2);
        builder.Property(e => e.AvgDealValue).HasColumnName("avg_deal_value").HasPrecision(18, 2);
        builder.Property(e => e.CostPerLead).HasColumnName("cost_per_lead").HasPrecision(18, 2);
        builder.Property(e => e.CostPerAcquisition).HasColumnName("cost_per_acquisition").HasPrecision(18, 2);
        builder.Property(e => e.ReturnOnInvestment).HasColumnName("return_on_investment");

        // Timestamps
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        // Ignore computed properties
        builder.Ignore(e => e.ImpressionsToViews);
        builder.Ignore(e => e.ViewsToContacts);
        builder.Ignore(e => e.ContactsToQualified);
        builder.Ignore(e => e.QualifiedToNegotiation);
        builder.Ignore(e => e.NegotiationToConverted);
        builder.Ignore(e => e.QualifiedToConverted);
        builder.Ignore(e => e.OverallConversion);

        // Indices
        builder.HasIndex(e => e.DealerId).HasDatabaseName("ix_lead_funnel_dealer_id");
        builder.HasIndex(e => e.PeriodStart).HasDatabaseName("ix_lead_funnel_period_start");
        builder.HasIndex(e => new { e.DealerId, e.PeriodStart, e.PeriodEnd }).HasDatabaseName("ix_lead_funnel_dealer_period");
    }
}

public class DealerBenchmarkConfiguration : IEntityTypeConfiguration<DealerBenchmark>
{
    public void Configure(EntityTypeBuilder<DealerBenchmark> builder)
    {
        builder.ToTable("dealer_benchmarks");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.DealerId).HasColumnName("dealer_id").IsRequired();
        builder.Property(e => e.Date).HasColumnName("date").IsRequired();
        builder.Property(e => e.Period).HasColumnName("period").HasMaxLength(20);

        // Dealer metrics
        builder.Property(e => e.AvgDaysOnMarket).HasColumnName("avg_days_on_market");
        builder.Property(e => e.ConversionRate).HasColumnName("conversion_rate");
        builder.Property(e => e.AvgResponseTimeMinutes).HasColumnName("avg_response_time_minutes");
        builder.Property(e => e.CustomerSatisfaction).HasColumnName("customer_satisfaction");
        builder.Property(e => e.ListingQualityScore).HasColumnName("listing_quality_score");
        builder.Property(e => e.AvgVehiclePrice).HasColumnName("avg_vehicle_price").HasPrecision(18, 2);
        builder.Property(e => e.ActiveListings).HasColumnName("active_listings");
        builder.Property(e => e.MonthlySales).HasColumnName("monthly_sales");
        builder.Property(e => e.ViewsPerListing).HasColumnName("views_per_listing");
        builder.Property(e => e.ContactsPerListing).HasColumnName("contacts_per_listing");

        // Market averages
        builder.Property(e => e.MarketAvgDaysOnMarket).HasColumnName("market_avg_days_on_market");
        builder.Property(e => e.MarketAvgConversionRate).HasColumnName("market_avg_conversion_rate");
        builder.Property(e => e.MarketAvgResponseTime).HasColumnName("market_avg_response_time");
        builder.Property(e => e.MarketAvgSatisfaction).HasColumnName("market_avg_satisfaction");
        builder.Property(e => e.MarketAvgListingQuality).HasColumnName("market_avg_listing_quality");
        builder.Property(e => e.MarketAvgPrice).HasColumnName("market_avg_price").HasPrecision(18, 2);
        builder.Property(e => e.MarketAvgViewsPerListing).HasColumnName("market_avg_views_per_listing");
        builder.Property(e => e.MarketAvgContactsPerListing).HasColumnName("market_avg_contacts_per_listing");

        // Percentile rankings
        builder.Property(e => e.DaysOnMarketPercentile).HasColumnName("days_on_market_percentile");
        builder.Property(e => e.ConversionRatePercentile).HasColumnName("conversion_rate_percentile");
        builder.Property(e => e.ResponseTimePercentile).HasColumnName("response_time_percentile");
        builder.Property(e => e.SatisfactionPercentile).HasColumnName("satisfaction_percentile");
        builder.Property(e => e.ListingQualityPercentile).HasColumnName("listing_quality_percentile");
        builder.Property(e => e.SalesPercentile).HasColumnName("sales_percentile");
        builder.Property(e => e.EngagementPercentile).HasColumnName("engagement_percentile");

        // Rankings
        builder.Property(e => e.OverallRank).HasColumnName("overall_rank");
        builder.Property(e => e.TotalDealers).HasColumnName("total_dealers");
        builder.Property(e => e.CategoryRank).HasColumnName("category_rank");
        builder.Property(e => e.TotalInCategory).HasColumnName("total_in_category");

        // Tier
        builder.Property(e => e.Tier).HasColumnName("tier").HasConversion<string>().HasMaxLength(50);

        // Store lists as JSON
        builder.Property(e => e.ImprovementAreas)
            .HasColumnName("improvement_areas")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
            );

        builder.Property(e => e.Strengths)
            .HasColumnName("strengths")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
            );

        // Timestamps
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        // Ignore computed properties
        builder.Ignore(e => e.IsBetterThanMarketDaysOnMarket);
        builder.Ignore(e => e.IsBetterThanMarketConversion);
        builder.Ignore(e => e.IsBetterThanMarketResponseTime);
        builder.Ignore(e => e.IsBetterThanMarketSatisfaction);
        builder.Ignore(e => e.TierName);

        // Indices
        builder.HasIndex(e => e.DealerId).HasDatabaseName("ix_dealer_benchmarks_dealer_id");
        builder.HasIndex(e => e.Date).HasDatabaseName("ix_dealer_benchmarks_date");
        builder.HasIndex(e => new { e.DealerId, e.Date }).IsUnique().HasDatabaseName("ix_dealer_benchmarks_dealer_date");
        builder.HasIndex(e => e.OverallRank).HasDatabaseName("ix_dealer_benchmarks_rank");
        builder.HasIndex(e => e.Tier).HasDatabaseName("ix_dealer_benchmarks_tier");
    }
}

public class DealerAlertConfiguration : IEntityTypeConfiguration<DealerAlert>
{
    public void Configure(EntityTypeBuilder<DealerAlert> builder)
    {
        builder.ToTable("dealer_alerts");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.DealerId).HasColumnName("dealer_id").IsRequired();
        builder.Property(e => e.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(50);
        builder.Property(e => e.Severity).HasColumnName("severity").HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);

        builder.Property(e => e.Title).HasColumnName("title").HasMaxLength(255).IsRequired();
        builder.Property(e => e.Message).HasColumnName("message").HasMaxLength(1000);
        builder.Property(e => e.ActionUrl).HasColumnName("action_url").HasMaxLength(500);
        builder.Property(e => e.ActionLabel).HasColumnName("action_label").HasMaxLength(100);

        // Metadata as JSON
        builder.Property(e => e.MetadataJson).HasColumnName("metadata_json");

        // Related entities
        builder.Property(e => e.RelatedVehicleId).HasColumnName("related_vehicle_id");
        builder.Property(e => e.RelatedLeadId).HasColumnName("related_lead_id");
        builder.Property(e => e.TriggerCondition).HasColumnName("trigger_condition").HasMaxLength(200);
        builder.Property(e => e.CurrentValue).HasColumnName("current_value");
        builder.Property(e => e.ThresholdValue).HasColumnName("threshold_value");

        // Status flags
        builder.Property(e => e.IsRead).HasColumnName("is_read");
        builder.Property(e => e.ReadAt).HasColumnName("read_at");
        builder.Property(e => e.IsDismissed).HasColumnName("is_dismissed");
        builder.Property(e => e.DismissedAt).HasColumnName("dismissed_at");
        builder.Property(e => e.IsActedUpon).HasColumnName("is_acted_upon");
        builder.Property(e => e.ActedUponAt).HasColumnName("acted_upon_at");
        builder.Property(e => e.ExpiresAt).HasColumnName("expires_at");

        // Notification tracking
        builder.Property(e => e.EmailSent).HasColumnName("email_sent");
        builder.Property(e => e.EmailSentAt).HasColumnName("email_sent_at");
        builder.Property(e => e.PushSent).HasColumnName("push_sent");
        builder.Property(e => e.PushSentAt).HasColumnName("push_sent_at");
        builder.Property(e => e.SmsSent).HasColumnName("sms_sent");
        builder.Property(e => e.SmsSentAt).HasColumnName("sms_sent_at");

        // Timestamps
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        // Ignore computed properties
        builder.Ignore(e => e.IsExpired);
        builder.Ignore(e => e.Metadata);

        // Indices
        builder.HasIndex(e => e.DealerId).HasDatabaseName("ix_dealer_alerts_dealer_id");
        builder.HasIndex(e => e.Type).HasDatabaseName("ix_dealer_alerts_type");
        builder.HasIndex(e => e.Severity).HasDatabaseName("ix_dealer_alerts_severity");
        builder.HasIndex(e => e.Status).HasDatabaseName("ix_dealer_alerts_status");
        builder.HasIndex(e => e.IsRead).HasDatabaseName("ix_dealer_alerts_is_read");
        builder.HasIndex(e => e.CreatedAt).HasDatabaseName("ix_dealer_alerts_created_at");
        builder.HasIndex(e => new { e.DealerId, e.IsRead, e.IsDismissed }).HasDatabaseName("ix_dealer_alerts_active");
    }
}

public class InventoryAgingConfiguration : IEntityTypeConfiguration<InventoryAging>
{
    public void Configure(EntityTypeBuilder<InventoryAging> builder)
    {
        builder.ToTable("inventory_agings");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.DealerId).HasColumnName("dealer_id").IsRequired();
        builder.Property(e => e.Date).HasColumnName("date").IsRequired();

        // Age buckets - counts
        builder.Property(e => e.Vehicles0To15Days).HasColumnName("vehicles_0_15_days");
        builder.Property(e => e.Vehicles16To30Days).HasColumnName("vehicles_16_30_days");
        builder.Property(e => e.Vehicles31To45Days).HasColumnName("vehicles_31_45_days");
        builder.Property(e => e.Vehicles46To60Days).HasColumnName("vehicles_46_60_days");
        builder.Property(e => e.Vehicles61To90Days).HasColumnName("vehicles_61_90_days");
        builder.Property(e => e.VehiclesOver90Days).HasColumnName("vehicles_over_90_days");

        // Age buckets - values
        builder.Property(e => e.Value0To15Days).HasColumnName("value_0_15_days").HasPrecision(18, 2);
        builder.Property(e => e.Value16To30Days).HasColumnName("value_16_30_days").HasPrecision(18, 2);
        builder.Property(e => e.Value31To45Days).HasColumnName("value_31_45_days").HasPrecision(18, 2);
        builder.Property(e => e.Value46To60Days).HasColumnName("value_46_60_days").HasPrecision(18, 2);
        builder.Property(e => e.Value61To90Days).HasColumnName("value_61_90_days").HasPrecision(18, 2);
        builder.Property(e => e.ValueOver90Days).HasColumnName("value_over_90_days").HasPrecision(18, 2);

        // Metrics
        builder.Property(e => e.AverageDaysOnMarket).HasColumnName("avg_days_on_market");
        builder.Property(e => e.MedianDaysOnMarket).HasColumnName("median_days_on_market");

        // Timestamps
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");

        // Ignore computed properties
        builder.Ignore(e => e.TotalVehicles);
        builder.Ignore(e => e.TotalValue);
        builder.Ignore(e => e.PercentFresh);
        builder.Ignore(e => e.PercentAging);
        builder.Ignore(e => e.AtRiskValue);
        builder.Ignore(e => e.AtRiskCount);

        // Indices
        builder.HasIndex(e => e.DealerId).HasDatabaseName("ix_inventory_agings_dealer_id");
        builder.HasIndex(e => e.Date).HasDatabaseName("ix_inventory_agings_date");
        builder.HasIndex(e => new { e.DealerId, e.Date }).IsUnique().HasDatabaseName("ix_inventory_agings_dealer_date");
    }
}
