using BillingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BillingService.Infrastructure.Persistence.Configurations;

public class AzulTransactionConfiguration : IEntityTypeConfiguration<AzulTransaction>
{
    public void Configure(EntityTypeBuilder<AzulTransaction> builder)
    {
        builder.ToTable("azul_transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.OrderNumber)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("order_number");

        builder.Property(t => t.AzulOrderId)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("azul_order_id");

        builder.Property(t => t.Amount)
            .HasColumnType("decimal(18,2)")
            .HasColumnName("amount");

        builder.Property(t => t.ITBIS)
            .HasColumnType("decimal(18,2)")
            .HasColumnName("itbis");

        builder.Property(t => t.AuthorizationCode)
            .HasMaxLength(20)
            .HasColumnName("authorization_code");

        builder.Property(t => t.ResponseCode)
            .HasMaxLength(20)
            .HasColumnName("response_code");

        builder.Property(t => t.IsoCode)
            .HasMaxLength(10)
            .HasColumnName("iso_code");

        builder.Property(t => t.ResponseMessage)
            .HasMaxLength(255)
            .HasColumnName("response_message");

        builder.Property(t => t.ErrorDescription)
            .HasMaxLength(1000)
            .HasColumnName("error_description");

        builder.Property(t => t.RRN)
            .HasMaxLength(50)
            .HasColumnName("rrn");

        builder.Property(t => t.TransactionDateTime)
            .HasColumnName("transaction_datetime");

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(t => t.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasColumnName("status");

        builder.Property(t => t.DataVaultToken)
            .HasMaxLength(100)
            .HasColumnName("data_vault_token");

        builder.Property(t => t.DataVaultExpiration)
            .HasMaxLength(10)
            .HasColumnName("data_vault_expiration");

        builder.Property(t => t.DataVaultBrand)
            .HasMaxLength(50)
            .HasColumnName("data_vault_brand");

        builder.Property(t => t.UserId)
            .HasColumnName("user_id");

        builder.Property(t => t.CustomerEmail)
            .HasMaxLength(255)
            .HasColumnName("customer_email");

        builder.Property(t => t.CustomerName)
            .HasMaxLength(255)
            .HasColumnName("customer_name");

        builder.Property(t => t.IpAddress)
            .HasMaxLength(50)
            .HasColumnName("ip_address");

        builder.Property(t => t.UserAgent)
            .HasMaxLength(500)
            .HasColumnName("user_agent");

        // Indexes
        builder.HasIndex(t => t.OrderNumber)
            .HasDatabaseName("idx_azul_transactions_order_number");

        builder.HasIndex(t => t.AzulOrderId)
            .HasDatabaseName("idx_azul_transactions_azul_order_id");

        builder.HasIndex(t => t.UserId)
            .HasDatabaseName("idx_azul_transactions_user_id");

        builder.HasIndex(t => t.Status)
            .HasDatabaseName("idx_azul_transactions_status");

        builder.HasIndex(t => t.TransactionDateTime)
            .HasDatabaseName("idx_azul_transactions_datetime");
    }
}
