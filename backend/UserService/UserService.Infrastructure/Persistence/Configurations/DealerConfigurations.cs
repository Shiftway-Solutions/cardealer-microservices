using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserService.Domain.Entities;

namespace UserService.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuración de Entity Framework para la entidad Dealer
/// </summary>
public class DealerConfiguration : IEntityTypeConfiguration<Dealer>
{
    public void Configure(EntityTypeBuilder<Dealer> builder)
    {
        builder.ToTable("Dealers");

        builder.HasKey(d => d.Id);

        // Información básica
        builder.Property(d => d.BusinessName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(d => d.TradeName)
            .HasMaxLength(200);

        builder.Property(d => d.Description)
            .HasMaxLength(2000);

        builder.Property(d => d.DealerType)
            .HasConversion<int>();

        // Contacto
        builder.Property(d => d.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(d => d.Phone)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(d => d.WhatsApp)
            .HasMaxLength(30);

        builder.Property(d => d.Website)
            .HasMaxLength(500);

        // Ubicación
        builder.Property(d => d.Address)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(d => d.City)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.State)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.ZipCode)
            .HasMaxLength(20);

        builder.Property(d => d.Country)
            .HasMaxLength(10)
            .HasDefaultValue("DO");

        // Branding
        builder.Property(d => d.LogoUrl)
            .HasMaxLength(1000);

        builder.Property(d => d.BannerUrl)
            .HasMaxLength(1000);

        builder.Property(d => d.PrimaryColor)
            .HasMaxLength(10);

        // Documentación legal
        builder.Property(d => d.BusinessRegistrationNumber)
            .HasMaxLength(50);

        builder.Property(d => d.TaxId)
            .HasMaxLength(50);

        builder.Property(d => d.DealerLicenseNumber)
            .HasMaxLength(100);

        builder.Property(d => d.BusinessLicenseDocumentUrl)
            .HasMaxLength(1000);

        // Verificación
        builder.Property(d => d.VerificationStatus)
            .HasConversion<int>()
            .HasDefaultValue(DealerVerificationStatus.Pending);

        builder.Property(d => d.VerificationNotes)
            .HasMaxLength(1000);

        builder.Property(d => d.RejectionReason)
            .HasMaxLength(1000);

        // Métricas
        builder.Property(d => d.AverageRating)
            .HasPrecision(3, 2);

        // Configuración
        builder.Property(d => d.BusinessHours)
            .HasColumnType("jsonb"); // PostgreSQL JSON

        builder.Property(d => d.SocialMediaLinks)
            .HasColumnType("jsonb");

        builder.Property(d => d.NotificationSettingsJson)
            .HasColumnType("jsonb");

        builder.Property(d => d.SecuritySettingsJson)
            .HasColumnType("jsonb");

        // Índices
        builder.HasIndex(d => d.Email).IsUnique();
        builder.HasIndex(d => d.BusinessRegistrationNumber).IsUnique();
        builder.HasIndex(d => d.OwnerUserId);
        builder.HasIndex(d => d.VerificationStatus);
        builder.HasIndex(d => d.City);
        builder.HasIndex(d => new { d.Latitude, d.Longitude });

        // Soft delete filter
        builder.HasQueryFilter(d => !d.IsDeleted);

        // Relación con empleados (DealerEmployee ya existe en EmployeeEntities.cs)
        builder.HasMany(d => d.Employees)
            .WithOne()
            .HasForeignKey(e => e.DealerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// NOTE: SellerProfileConfiguration is defined in SellerProfileConfigurations.cs (more complete version with snake_case)
// The following class is commented to avoid duplicate definition

/*
/// <summary>
/// Configuración de Entity Framework para la entidad SellerProfile (DEPRECATED - use SellerProfileConfigurations.cs)
/// </summary>
public class SellerProfileConfiguration_DEPRECATED : IEntityTypeConfiguration<SellerProfile>
{
    public void Configure(EntityTypeBuilder<SellerProfile> builder)
    {
        builder.ToTable("SellerProfiles");

        builder.HasKey(s => s.Id);

        // Índice único para UserId (un usuario solo puede tener un perfil de vendedor)
        builder.HasIndex(s => s.UserId).IsUnique();

        // Información personal
        builder.Property(s => s.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Nationality)
            .HasMaxLength(50);

        // Contacto
        builder.Property(s => s.Phone)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(s => s.AlternatePhone)
            .HasMaxLength(30);

        builder.Property(s => s.WhatsApp)
            .HasMaxLength(30);

        builder.Property(s => s.Email)
            .IsRequired()
            .HasMaxLength(256);

        // Ubicación
        builder.Property(s => s.Address)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(s => s.City)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.State)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.ZipCode)
            .HasMaxLength(20);

        builder.Property(s => s.Country)
            .HasMaxLength(10)
            .HasDefaultValue("DO");

        // Verificación
        builder.Property(s => s.VerificationStatus)
            .HasConversion<int>()
            .HasDefaultValue(SellerVerificationStatus.NotSubmitted);

        builder.Property(s => s.VerificationNotes)
            .HasMaxLength(1000);

        builder.Property(s => s.RejectionReason)
            .HasMaxLength(1000);

        // Métricas
        builder.Property(s => s.AverageRating)
            .HasPrecision(3, 2);

        // Configuración
        builder.Property(s => s.PreferredContactMethod)
            .HasMaxLength(20);

        // Índices
        builder.HasIndex(s => s.VerificationStatus);
        builder.HasIndex(s => s.City);
        builder.HasIndex(s => new { s.Latitude, s.Longitude });

        // Soft delete filter
        builder.HasQueryFilter(s => !s.IsDeleted);

        // Relación con documentos de identidad
        builder.HasMany(s => s.IdentityDocuments)
            .WithOne(d => d.SellerProfile)
            .HasForeignKey(d => d.SellerProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
*/

/// <summary>
/// Configuración de Entity Framework para la entidad IdentityDocument
/// </summary>
public class IdentityDocumentConfiguration : IEntityTypeConfiguration<IdentityDocument>
{
    public void Configure(EntityTypeBuilder<IdentityDocument> builder)
    {
        builder.ToTable("IdentityDocuments");

        builder.HasKey(d => d.Id);

        // Información del documento
        builder.Property(d => d.DocumentType)
            .HasConversion<int>();

        builder.Property(d => d.DocumentNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(d => d.IssuingCountry)
            .HasMaxLength(10);

        // Imágenes
        builder.Property(d => d.FrontImageUrl)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(d => d.BackImageUrl)
            .HasMaxLength(1000);

        builder.Property(d => d.SelfieWithDocumentUrl)
            .HasMaxLength(1000);

        // Datos extraídos
        builder.Property(d => d.ExtractedFullName)
            .HasMaxLength(200);

        builder.Property(d => d.ExtractedAddress)
            .HasMaxLength(500);

        // Verificación
        builder.Property(d => d.Status)
            .HasConversion<int>()
            .HasDefaultValue(DocumentVerificationStatus.Pending);

        builder.Property(d => d.VerificationNotes)
            .HasMaxLength(1000);

        builder.Property(d => d.RejectionReason)
            .HasMaxLength(1000);

        // Índices
        builder.HasIndex(d => d.SellerProfileId);
        builder.HasIndex(d => d.Status);
        builder.HasIndex(d => new { d.DocumentType, d.DocumentNumber });

        // Soft delete filter
        builder.HasQueryFilter(d => !d.IsDeleted);
    }
}
