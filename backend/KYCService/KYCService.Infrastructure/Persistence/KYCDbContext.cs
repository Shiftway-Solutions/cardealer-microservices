using Microsoft.EntityFrameworkCore;
using KYCService.Domain.Entities;

namespace KYCService.Infrastructure.Persistence;

public class KYCDbContext : DbContext
{
    public KYCDbContext(DbContextOptions<KYCDbContext> options) : base(options)
    {
    }

    public DbSet<KYCProfile> KYCProfiles => Set<KYCProfile>();
    public DbSet<KYCDocument> KYCDocuments => Set<KYCDocument>();
    public DbSet<KYCVerification> KYCVerifications => Set<KYCVerification>();
    public DbSet<KYCRiskAssessment> KYCRiskAssessments => Set<KYCRiskAssessment>();
    public DbSet<SuspiciousTransactionReport> SuspiciousTransactionReports => Set<SuspiciousTransactionReport>();
    public DbSet<WatchlistEntry> WatchlistEntries => Set<WatchlistEntry>();

    // Draft (autosave del wizard frontend)
    public DbSet<KYCProfileDraft> KYCProfileDrafts => Set<KYCProfileDraft>();

    // Security entities
    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();
    public DbSet<KYCAuditLog> KYCAuditLogs => Set<KYCAuditLog>();
    public DbSet<RateLimitEntry> RateLimitEntries => Set<RateLimitEntry>();
    public DbSet<KYCSagaState> KYCSagaStates => Set<KYCSagaState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // KYCProfile
        modelBuilder.Entity<KYCProfile>(entity =>
        {
            entity.ToTable("kyc_profiles");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.EntityType).HasColumnName("entity_type");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.RiskLevel).HasColumnName("risk_level");
            entity.Property(e => e.RiskScore).HasColumnName("risk_score");
            entity.Property(e => e.RiskFactors).HasColumnName("risk_factors")
                .HasConversion(
                    v => string.Join("|||", v),
                    v => v.Split("|||", StringSplitOptions.RemoveEmptyEntries).ToList()
                );

            entity.Property(e => e.FullName).HasColumnName("full_name").HasMaxLength(200);
            entity.Property(e => e.MiddleName).HasColumnName("middle_name").HasMaxLength(100);
            entity.Property(e => e.LastName).HasColumnName("last_name").HasMaxLength(100);
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(e => e.PlaceOfBirth).HasColumnName("place_of_birth").HasMaxLength(200);
            entity.Property(e => e.Nationality).HasColumnName("nationality").HasMaxLength(50);
            entity.Property(e => e.Gender).HasColumnName("gender").HasMaxLength(20);

            entity.Property(e => e.PrimaryDocumentType).HasColumnName("primary_document_type");
            entity.Property(e => e.PrimaryDocumentNumber).HasColumnName("primary_document_number").HasMaxLength(50);
            entity.Property(e => e.PrimaryDocumentExpiry).HasColumnName("primary_document_expiry");
            entity.Property(e => e.PrimaryDocumentCountry).HasColumnName("primary_document_country").HasMaxLength(5);

            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(200);
            entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(30);
            entity.Property(e => e.MobilePhone).HasColumnName("mobile_phone").HasMaxLength(30);

            entity.Property(e => e.Address).HasColumnName("address").HasMaxLength(500);
            entity.Property(e => e.Sector).HasColumnName("sector").HasMaxLength(100);
            entity.Property(e => e.City).HasColumnName("city").HasMaxLength(100);
            entity.Property(e => e.Province).HasColumnName("province").HasMaxLength(100);
            entity.Property(e => e.PostalCode).HasColumnName("postal_code").HasMaxLength(20);
            entity.Property(e => e.Country).HasColumnName("country").HasMaxLength(5);

            entity.Property(e => e.Occupation).HasColumnName("occupation").HasMaxLength(100);
            entity.Property(e => e.EmployerName).HasColumnName("employer_name").HasMaxLength(200);
            entity.Property(e => e.SourceOfFunds).HasColumnName("source_of_funds").HasMaxLength(500);
            entity.Property(e => e.ExpectedTransactionVolume).HasColumnName("expected_transaction_volume").HasMaxLength(100);
            entity.Property(e => e.EstimatedAnnualIncome).HasColumnName("estimated_annual_income").HasPrecision(18, 2);

            entity.Property(e => e.IsPEP).HasColumnName("is_pep");
            entity.Property(e => e.PEPPosition).HasColumnName("pep_position").HasMaxLength(200);
            entity.Property(e => e.PEPRelationship).HasColumnName("pep_relationship").HasMaxLength(200);

            entity.Property(e => e.BusinessName).HasColumnName("business_name").HasMaxLength(300);
            entity.Property(e => e.RNC).HasColumnName("rnc").HasMaxLength(15);
            entity.Property(e => e.BusinessType).HasColumnName("business_type").HasMaxLength(100);
            entity.Property(e => e.IncorporationDate).HasColumnName("incorporation_date");
            entity.Property(e => e.LegalRepresentative).HasColumnName("legal_representative").HasMaxLength(200);

            entity.Property(e => e.IdentityVerifiedAt).HasColumnName("identity_verified_at");
            entity.Property(e => e.AddressVerifiedAt).HasColumnName("address_verified_at");
            entity.Property(e => e.IncomeVerifiedAt).HasColumnName("income_verified_at");
            entity.Property(e => e.PEPCheckedAt).HasColumnName("pep_checked_at");
            entity.Property(e => e.SanctionsCheckedAt).HasColumnName("sanctions_checked_at");

            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.ApprovedAt).HasColumnName("approved_at");
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.ApprovalNotes).HasColumnName("approval_notes").HasMaxLength(1000);
            entity.Property(e => e.RejectedAt).HasColumnName("rejected_at");
            entity.Property(e => e.RejectedBy).HasColumnName("rejected_by");
            entity.Property(e => e.RejectionReason).HasColumnName("rejection_reason").HasMaxLength(1000);

            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.LastReviewAt).HasColumnName("last_review_at");
            entity.Property(e => e.NextReviewAt).HasColumnName("next_review_at");

            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => e.PrimaryDocumentNumber);
            entity.HasIndex(e => e.RNC);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.RiskLevel);
            entity.HasIndex(e => e.IsPEP);

            entity.HasMany(e => e.Documents)
                .WithOne(d => d.KYCProfile)
                .HasForeignKey(d => d.KYCProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Verifications)
                .WithOne(v => v.KYCProfile)
                .HasForeignKey(v => v.KYCProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.RiskAssessments)
                .WithOne(r => r.KYCProfile)
                .HasForeignKey(r => r.KYCProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // KYCDocument
        modelBuilder.Entity<KYCDocument>(entity =>
        {
            entity.ToTable("kyc_documents");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.KYCProfileId).HasColumnName("kyc_profile_id");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.DocumentName).HasColumnName("document_name").HasMaxLength(200);
            entity.Property(e => e.FileName).HasColumnName("file_name").HasMaxLength(300);
            entity.Property(e => e.StorageKey).HasColumnName("storage_key").HasMaxLength(500).IsRequired(false);
            entity.Property(e => e.FileUrl).HasColumnName("file_url").HasMaxLength(1000);
            entity.Property(e => e.FileType).HasColumnName("file_type").HasMaxLength(50);
            entity.Property(e => e.FileSize).HasColumnName("file_size");
            entity.Property(e => e.FileHash).HasColumnName("file_hash").HasMaxLength(128);
            entity.Property(e => e.Side).HasColumnName("side").HasMaxLength(50);
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.RejectionReason).HasColumnName("rejection_reason").HasMaxLength(500);
            entity.Property(e => e.ExtractedNumber).HasColumnName("extracted_number").HasMaxLength(50);
            entity.Property(e => e.ExtractedExpiry).HasColumnName("extracted_expiry");
            entity.Property(e => e.ExtractedName).HasColumnName("extracted_name").HasMaxLength(200);
            entity.Property(e => e.UploadedBy).HasColumnName("uploaded_by");
            entity.Property(e => e.UploadedAt).HasColumnName("uploaded_at");
            entity.Property(e => e.VerifiedAt).HasColumnName("verified_at");
            entity.Property(e => e.VerifiedBy).HasColumnName("verified_by");

            entity.HasIndex(e => e.KYCProfileId);
            entity.HasIndex(e => e.Status);
        });

        // KYCVerification
        modelBuilder.Entity<KYCVerification>(entity =>
        {
            entity.ToTable("kyc_verifications");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.KYCProfileId).HasColumnName("kyc_profile_id");
            entity.Property(e => e.VerificationType).HasColumnName("verification_type").HasMaxLength(50);
            entity.Property(e => e.Provider).HasColumnName("provider").HasMaxLength(100);
            entity.Property(e => e.Passed).HasColumnName("passed");
            entity.Property(e => e.FailureReason).HasColumnName("failure_reason").HasMaxLength(500);
            entity.Property(e => e.RawResponse).HasColumnName("raw_response");
            entity.Property(e => e.ConfidenceScore).HasColumnName("confidence_score");
            entity.Property(e => e.VerifiedAt).HasColumnName("verified_at");
            entity.Property(e => e.VerifiedBy).HasColumnName("verified_by");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");

            entity.HasIndex(e => e.KYCProfileId);
            entity.HasIndex(e => e.VerificationType);
        });

        // KYCRiskAssessment
        modelBuilder.Entity<KYCRiskAssessment>(entity =>
        {
            entity.ToTable("kyc_risk_assessments");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.KYCProfileId).HasColumnName("kyc_profile_id");
            entity.Property(e => e.PreviousLevel).HasColumnName("previous_level");
            entity.Property(e => e.NewLevel).HasColumnName("new_level");
            entity.Property(e => e.PreviousScore).HasColumnName("previous_score");
            entity.Property(e => e.NewScore).HasColumnName("new_score");
            entity.Property(e => e.Reason).HasColumnName("reason").HasMaxLength(1000);
            entity.Property(e => e.Factors).HasColumnName("factors")
                .HasConversion(
                    v => string.Join("|||", v),
                    v => v.Split("|||", StringSplitOptions.RemoveEmptyEntries).ToList()
                );
            entity.Property(e => e.RecommendedActions).HasColumnName("recommended_actions")
                .HasConversion(
                    v => string.Join("|||", v),
                    v => v.Split("|||", StringSplitOptions.RemoveEmptyEntries).ToList()
                );
            entity.Property(e => e.AssessedBy).HasColumnName("assessed_by");
            entity.Property(e => e.AssessedByName).HasColumnName("assessed_by_name").HasMaxLength(200);
            entity.Property(e => e.AssessedAt).HasColumnName("assessed_at");

            entity.HasIndex(e => e.KYCProfileId);
        });

        // SuspiciousTransactionReport
        modelBuilder.Entity<SuspiciousTransactionReport>(entity =>
        {
            entity.ToTable("suspicious_transaction_reports");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.KYCProfileId).HasColumnName("kyc_profile_id");
            entity.Property(e => e.TransactionId).HasColumnName("transaction_id");
            entity.Property(e => e.ReportNumber).HasColumnName("report_number").HasMaxLength(50);
            entity.Property(e => e.SuspiciousActivityType).HasColumnName("suspicious_activity_type").HasMaxLength(200);
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(5000);
            entity.Property(e => e.Amount).HasColumnName("amount").HasPrecision(18, 2);
            entity.Property(e => e.Currency).HasColumnName("currency").HasMaxLength(5);
            entity.Property(e => e.RedFlags).HasColumnName("red_flags")
                .HasConversion(
                    v => string.Join("|||", v),
                    v => v.Split("|||", StringSplitOptions.RemoveEmptyEntries).ToList()
                );
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.DetectedAt).HasColumnName("detected_at");
            entity.Property(e => e.ReportingDeadline).HasColumnName("reporting_deadline");
            entity.Property(e => e.UAFReportNumber).HasColumnName("uaf_report_number").HasMaxLength(50);
            entity.Property(e => e.SentToUAFAt).HasColumnName("sent_to_uaf_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedByName).HasColumnName("created_by_name").HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.ApprovedAt).HasColumnName("approved_at");
            entity.Property(e => e.SentBy).HasColumnName("sent_by");

            entity.HasIndex(e => e.ReportNumber).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ReportingDeadline);
        });

        // WatchlistEntry
        modelBuilder.Entity<WatchlistEntry>(entity =>
        {
            entity.ToTable("watchlist_entries");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ListType).HasColumnName("list_type");
            entity.Property(e => e.Source).HasColumnName("source").HasMaxLength(100);
            entity.Property(e => e.FullName).HasColumnName("full_name").HasMaxLength(300);
            entity.Property(e => e.Aliases).HasColumnName("aliases")
                .HasConversion(
                    v => string.Join("|||", v),
                    v => v.Split("|||", StringSplitOptions.RemoveEmptyEntries).ToList()
                );
            entity.Property(e => e.DocumentNumber).HasColumnName("document_number").HasMaxLength(50);
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(e => e.Nationality).HasColumnName("nationality").HasMaxLength(50);
            entity.Property(e => e.Details).HasColumnName("details").HasMaxLength(2000);
            entity.Property(e => e.ListedDate).HasColumnName("listed_date");
            entity.Property(e => e.LastUpdated).HasColumnName("last_updated");
            entity.Property(e => e.IsActive).HasColumnName("is_active");

            entity.HasIndex(e => e.ListType);
            entity.HasIndex(e => e.FullName);
            entity.HasIndex(e => e.DocumentNumber);
            entity.HasIndex(e => e.IsActive);
        });

        // ============================================================================
        // KYC Profile Drafts (autosave wizard)
        // ============================================================================

        modelBuilder.Entity<KYCProfileDraft>(entity =>
        {
            entity.ToTable("kyc_profile_drafts");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CurrentStep).HasColumnName("current_step");
            entity.Property(e => e.FormData).HasColumnName("form_data").HasColumnType("jsonb");
            entity.Property(e => e.IsSubmitted).HasColumnName("is_submitted");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");

            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.IsSubmitted);
        });

        // ============================================================================
        // Security Entities Configuration
        // ============================================================================

        // IdempotencyKey
        modelBuilder.Entity<IdempotencyKey>(entity =>
        {
            entity.ToTable("idempotency_keys");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Key).HasColumnName("key").HasMaxLength(100).IsRequired();
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Method).HasColumnName("method").HasMaxLength(10);
            entity.Property(e => e.Path).HasColumnName("path").HasMaxLength(500);
            entity.Property(e => e.ResponseStatusCode).HasColumnName("response_status_code");
            entity.Property(e => e.ResponseBody).HasColumnName("response_body");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.IsProcessing).HasColumnName("is_processing");

            entity.HasIndex(e => new { e.Key, e.UserId }).IsUnique();
            entity.HasIndex(e => e.ExpiresAt);
        });

        // KYCAuditLog
        modelBuilder.Entity<KYCAuditLog>(entity =>
        {
            entity.ToTable("kyc_audit_logs");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ProfileId).HasColumnName("profile_id");
            entity.Property(e => e.Action).HasColumnName("action");
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(2000);
            entity.Property(e => e.IpAddress).HasColumnName("ip_address").HasMaxLength(50);
            entity.Property(e => e.UserAgent).HasColumnName("user_agent").HasMaxLength(500);
            entity.Property(e => e.Metadata).HasColumnName("metadata");
            entity.Property(e => e.Success).HasColumnName("success");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message").HasMaxLength(2000);
            entity.Property(e => e.DurationMs).HasColumnName("duration_ms");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ProfileId);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.CreatedAt);
        });

        // RateLimitEntry
        modelBuilder.Entity<RateLimitEntry>(entity =>
        {
            entity.ToTable("rate_limit_entries");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Key).HasColumnName("key").HasMaxLength(200).IsRequired();
            entity.Property(e => e.Endpoint).HasColumnName("endpoint").HasMaxLength(500);
            entity.Property(e => e.RequestCount).HasColumnName("request_count");
            entity.Property(e => e.WindowStart).HasColumnName("window_start");
            entity.Property(e => e.WindowEnd).HasColumnName("window_end");

            entity.HasIndex(e => new { e.Key, e.Endpoint });
            entity.HasIndex(e => e.WindowEnd);
        });

        // KYCSagaState
        modelBuilder.Entity<KYCSagaState>(entity =>
        {
            entity.ToTable("kyc_saga_states");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CorrelationId).HasColumnName("correlation_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.CurrentStep).HasColumnName("current_step");
            entity.Property(e => e.TotalSteps).HasColumnName("total_steps");
            entity.Property(e => e.CompletedStepsData).HasColumnName("completed_steps_data");
            entity.Property(e => e.CreatedProfileId).HasColumnName("created_profile_id");
            entity.Property(e => e.CreatedDocumentIds).HasColumnName("created_document_ids")
                .HasConversion(
                    v => string.Join(",", v),
                    v => v.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(Guid.Parse).ToList()
                );
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message").HasMaxLength(2000);
            entity.Property(e => e.FailedAtStep).HasColumnName("failed_at_step");
            entity.Property(e => e.StartedAt).HasColumnName("started_at");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.RolledBackAt).HasColumnName("rolled_back_at");

            entity.HasIndex(e => e.CorrelationId).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
        });
    }
}
