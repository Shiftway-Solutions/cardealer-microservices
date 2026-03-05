using Microsoft.EntityFrameworkCore;
using VehiclesSaleService.Domain.Entities;
using CarDealer.Shared.MultiTenancy;
using CarDealer.Shared.Persistence;

namespace VehiclesSaleService.Infrastructure.Persistence;

public class ApplicationDbContext : MultiTenantDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantContext tenantContext)
        : base(options, tenantContext)
    {
    }

    // ========================================
    // VEHICLE ENTITIES
    // ========================================
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<VehicleImage> VehicleImages => Set<VehicleImage>();
    public DbSet<VehicleMake> VehicleMakes => Set<VehicleMake>();
    public DbSet<VehicleModel> VehicleModels => Set<VehicleModel>();
    public DbSet<VehicleTrim> VehicleTrims => Set<VehicleTrim>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<VehicleViewHistory> VehicleViewHistories => Set<VehicleViewHistory>();

    // ========================================
    // HOMEPAGE SECTION ENTITIES
    // ========================================
    public DbSet<HomepageSectionConfig> HomepageSectionConfigs => Set<HomepageSectionConfig>();
    public DbSet<VehicleHomepageSection> VehicleHomepageSections => Set<VehicleHomepageSection>();

    // ========================================
    // SALE TRANSACTIONS
    // ========================================
    public DbSet<SaleTransaction> SaleTransactions => Set<SaleTransaction>();

    // ========================================
    // LEADS & MESSAGING
    // ========================================
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<LeadMessage> LeadMessages => Set<LeadMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ========================================
        // VEHICLE CONFIGURATION
        // ========================================

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.ToTable("vehicles");
            entity.HasKey(v => v.Id);

            // Información básica
            entity.Property(v => v.Title)
                .IsRequired()
                .HasMaxLength(300);

            entity.Property(v => v.Description)
                .HasMaxLength(10000);

            entity.Property(v => v.Price)
                .HasPrecision(18, 2);

            entity.Property(v => v.Currency)
                .HasMaxLength(3)
                .HasDefaultValue("USD");

            entity.Property(v => v.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(v => v.SellerName)
                .HasMaxLength(200);

            // Identificación
            entity.Property(v => v.VIN)
                .HasMaxLength(17);

            entity.Property(v => v.StockNumber)
                .HasMaxLength(50);

            // Marca / Modelo
            entity.Property(v => v.Make)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(v => v.Model)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(v => v.Trim)
                .HasMaxLength(100);

            entity.Property(v => v.Generation)
                .HasMaxLength(50);

            // Tipo y carrocería
            entity.Property(v => v.VehicleType)
                .HasConversion<string>()
                .HasMaxLength(30);

            entity.Property(v => v.BodyStyle)
                .HasConversion<string>()
                .HasMaxLength(30);

            // Motor y transmisión
            entity.Property(v => v.FuelType)
                .HasConversion<string>()
                .HasMaxLength(30);

            entity.Property(v => v.EngineSize)
                .HasMaxLength(20);

            entity.Property(v => v.Transmission)
                .HasConversion<string>()
                .HasMaxLength(30);

            entity.Property(v => v.DriveType)
                .HasConversion<string>()
                .HasMaxLength(20);

            // Kilometraje
            entity.Property(v => v.MileageUnit)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(v => v.Condition)
                .HasConversion<string>()
                .HasMaxLength(30);

            // Apariencia
            entity.Property(v => v.ExteriorColor)
                .HasMaxLength(50);

            entity.Property(v => v.InteriorColor)
                .HasMaxLength(50);

            entity.Property(v => v.InteriorMaterial)
                .HasMaxLength(50);

            // Ubicación
            entity.Property(v => v.City)
                .HasMaxLength(100);

            entity.Property(v => v.State)
                .HasMaxLength(100);

            entity.Property(v => v.ZipCode)
                .HasMaxLength(20);

            entity.Property(v => v.Country)
                .HasMaxLength(100)
                .HasDefaultValue("USA");

            // Certificaciones
            entity.Property(v => v.CertificationProgram)
                .HasMaxLength(100);

            entity.Property(v => v.CarfaxReportUrl)
                .HasMaxLength(500);

            entity.Property(v => v.ServiceHistoryNotes)
                .HasMaxLength(5000);

            entity.Property(v => v.WarrantyInfo)
                .HasMaxLength(2000);

            // JSON columns
            entity.Property(v => v.FeaturesJson)
                .HasColumnType("jsonb")
                .HasDefaultValue("[]");

            entity.Property(v => v.PackagesJson)
                .HasColumnType("jsonb")
                .HasDefaultValue("[]");

            // Timestamps
            entity.Property(v => v.CreatedAt)
                .HasDefaultValueSql("NOW()");

            entity.Property(v => v.UpdatedAt)
                .HasDefaultValueSql("NOW()");

            // Índices para búsqueda y filtrado
            entity.HasIndex(v => v.DealerId);
            entity.HasIndex(v => v.SellerId);
            entity.HasIndex(v => v.Status);
            entity.HasIndex(v => v.Price);
            entity.HasIndex(v => v.Year);
            entity.HasIndex(v => v.Make);
            entity.HasIndex(v => v.Model);
            entity.HasIndex(v => v.MakeId);
            entity.HasIndex(v => v.ModelId);
            entity.HasIndex(v => v.VehicleType);
            entity.HasIndex(v => v.BodyStyle);
            entity.HasIndex(v => v.FuelType);
            entity.HasIndex(v => v.Transmission);
            entity.HasIndex(v => v.Mileage);
            entity.HasIndex(v => v.Condition);
            entity.HasIndex(v => v.State);
            entity.HasIndex(v => v.City);
            entity.HasIndex(v => v.ZipCode);
            entity.HasIndex(v => v.CreatedAt);
            entity.HasIndex(v => v.IsFeatured);
            entity.HasIndex(v => v.IsDeleted);
            entity.HasIndex(v => v.VIN).IsUnique();

            // Índice compuesto para búsquedas comunes
            entity.HasIndex(v => new { v.Make, v.Model, v.Year });
            entity.HasIndex(v => new { v.Status, v.IsDeleted });
            entity.HasIndex(v => new { v.State, v.City });

            // ✅ AUDIT FIX: Soft-delete query filter — deleted vehicles excluded from all queries
            entity.HasQueryFilter(v => !v.IsDeleted);

            // ✅ AUDIT FIX: Concurrency control
            entity.Property(v => v.ConcurrencyStamp)
                .IsConcurrencyToken()
                .HasMaxLength(36);

            // Relaciones
            entity.HasOne(v => v.Category)
                .WithMany(c => c.Vehicles)
                .HasForeignKey(v => v.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(v => v.Images)
                .WithOne(i => i.Vehicle)
                .HasForeignKey(i => i.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ========================================
        // VEHICLE IMAGE CONFIGURATION
        // ========================================

        modelBuilder.Entity<VehicleImage>(entity =>
        {
            entity.ToTable("vehicle_images");
            entity.HasKey(i => i.Id);

            entity.Property(i => i.Url)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(i => i.ThumbnailUrl)
                .HasMaxLength(500);

            entity.Property(i => i.CreatedAt)
                .HasDefaultValueSql("NOW()");

            entity.HasIndex(i => i.VehicleId);
            entity.HasIndex(i => i.DealerId);
            entity.HasIndex(i => new { i.VehicleId, i.SortOrder });
            entity.HasIndex(i => new { i.VehicleId, i.IsPrimary });
        });

        // ========================================
        // VEHICLE MAKE CONFIGURATION (Catálogo de marcas)
        // ========================================

        modelBuilder.Entity<VehicleMake>(entity =>
        {
            entity.ToTable("vehicle_makes");
            entity.HasKey(m => m.Id);

            entity.Property(m => m.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(m => m.LogoUrl)
                .HasMaxLength(500);

            entity.Property(m => m.Country)
                .HasMaxLength(100);

            entity.HasIndex(m => m.Name).IsUnique();
            entity.HasIndex(m => m.IsActive);
            entity.HasIndex(m => m.SortOrder);

            entity.HasMany(m => m.Models)
                .WithOne(model => model.Make)
                .HasForeignKey(model => model.MakeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ========================================
        // VEHICLE MODEL CONFIGURATION (Catálogo de modelos)
        // ========================================

        modelBuilder.Entity<VehicleModel>(entity =>
        {
            entity.ToTable("vehicle_models");
            entity.HasKey(m => m.Id);

            entity.Property(m => m.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(m => m.VehicleType)
                .HasConversion<string>()
                .HasMaxLength(30);

            entity.Property(m => m.DefaultBodyStyle)
                .HasConversion<string>()
                .HasMaxLength(30);

            entity.HasIndex(m => m.MakeId);
            entity.HasIndex(m => m.Name);
            entity.HasIndex(m => m.IsActive);
            entity.HasIndex(m => new { m.MakeId, m.Name }).IsUnique();

            entity.HasMany(m => m.Trims)
                .WithOne(t => t.Model)
                .HasForeignKey(t => t.ModelId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ========================================
        // VEHICLE TRIM CONFIGURATION (Versiones/Trims)
        // ========================================

        modelBuilder.Entity<VehicleTrim>(entity =>
        {
            entity.ToTable("vehicle_trims");
            entity.HasKey(t => t.Id);

            entity.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(t => t.BaseMSRP)
                .HasPrecision(18, 2);

            entity.Property(t => t.EngineSize)
                .HasMaxLength(200);

            entity.Property(t => t.Transmission)
                .HasConversion<string>()
                .HasMaxLength(30);

            entity.Property(t => t.DriveType)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(t => t.FuelType)
                .HasConversion<string>()
                .HasMaxLength(30);

            entity.HasIndex(t => t.ModelId);
            entity.HasIndex(t => t.Year);
            entity.HasIndex(t => t.IsActive);
            entity.HasIndex(t => new { t.ModelId, t.Year, t.Name }).IsUnique();
        });

        // ========================================
        // CATEGORY CONFIGURATION
        // ========================================

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(c => c.Slug)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(c => c.Description)
                .HasMaxLength(500);

            entity.Property(c => c.IconUrl)
                .HasMaxLength(500);

            entity.Property(c => c.CreatedAt)
                .HasDefaultValueSql("NOW()");

            entity.HasIndex(c => c.Slug).IsUnique();
            entity.HasIndex(c => c.ParentId);
            entity.HasIndex(c => c.IsActive);
            entity.HasIndex(c => c.Level);

            // Self-referencing relationship
            entity.HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ========================================
        // SEED DATA - Categorías para vehículos en venta
        // ========================================

        SeedCategories(modelBuilder);
    }

    private void SeedCategories(ModelBuilder modelBuilder)
    {
        var carsId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var trucksId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var suvsId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var motorcyclesId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var boatsId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var rvsId = Guid.Parse("66666666-6666-6666-6666-666666666666");

        modelBuilder.Entity<Category>().HasData(
            new Category
            {
                Id = carsId,
                Name = "Cars",
                Slug = "cars",
                Description = "Sedans, coupes, hatchbacks and sports cars",
                Level = 0,
                SortOrder = 1,
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Category
            {
                Id = trucksId,
                Name = "Trucks",
                Slug = "trucks",
                Description = "Pickup trucks and commercial trucks",
                Level = 0,
                SortOrder = 2,
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Category
            {
                Id = suvsId,
                Name = "SUVs & Crossovers",
                Slug = "suvs-crossovers",
                Description = "Sport utility vehicles and crossovers",
                Level = 0,
                SortOrder = 3,
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Category
            {
                Id = motorcyclesId,
                Name = "Motorcycles",
                Slug = "motorcycles",
                Description = "Sport bikes, cruisers and ATVs",
                Level = 0,
                SortOrder = 4,
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Category
            {
                Id = boatsId,
                Name = "Boats & Watercraft",
                Slug = "boats-watercraft",
                Description = "Boats, jet skis and watercraft",
                Level = 0,
                SortOrder = 5,
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Category
            {
                Id = rvsId,
                Name = "RVs & Campers",
                Slug = "rvs-campers",
                Description = "Recreational vehicles and campers",
                Level = 0,
                SortOrder = 6,
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // ========================================
        // HOMEPAGE SECTION CONFIG CONFIGURATION
        // ========================================

        modelBuilder.Entity<HomepageSectionConfig>(entity =>
        {
            entity.ToTable("homepage_section_configs");
            entity.HasKey(s => s.Id);

            entity.Property(s => s.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(s => s.Slug)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(s => s.Description)
                .HasMaxLength(500);

            entity.Property(s => s.Icon)
                .HasMaxLength(100);

            entity.Property(s => s.AccentColor)
                .HasMaxLength(30)
                .HasDefaultValue("blue");

            entity.Property(s => s.ViewAllHref)
                .HasMaxLength(200);

            entity.Property(s => s.Subtitle)
                .HasMaxLength(200);

            entity.Property(s => s.LayoutType)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.HasIndex(s => s.Slug).IsUnique();
            entity.HasIndex(s => s.DisplayOrder);
            entity.HasIndex(s => s.IsActive);
        });

        // Seed default homepage sections
        var carouselId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var sedanesId = Guid.Parse("10000000-0000-0000-0000-000000000002");
        var suvsSecId = Guid.Parse("10000000-0000-0000-0000-000000000003");
        var camionetasId = Guid.Parse("10000000-0000-0000-0000-000000000004");
        var deportivosId = Guid.Parse("10000000-0000-0000-0000-000000000005");
        var destacadosId = Guid.Parse("10000000-0000-0000-0000-000000000006");
        var lujoId = Guid.Parse("10000000-0000-0000-0000-000000000007");

        modelBuilder.Entity<HomepageSectionConfig>().HasData(
            new HomepageSectionConfig
            {
                Id = carouselId,
                Name = "Carousel Principal",
                Slug = "carousel",
                Description = "Carousel hero principal del homepage",
                DisplayOrder = 1,
                MaxItems = 10,
                IsActive = true,
                Icon = "FaCar",
                AccentColor = "blue",
                ViewAllHref = "/vehicles",
                LayoutType = SectionLayoutType.Hero,
                Subtitle = "Los mejores vehículos del momento",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new HomepageSectionConfig
            {
                Id = sedanesId,
                Name = "Sedanes",
                Slug = "sedanes",
                Description = "Sedanes elegantes y confortables",
                DisplayOrder = 2,
                MaxItems = 10,
                IsActive = true,
                Icon = "FaCar",
                AccentColor = "blue",
                ViewAllHref = "/vehicles?bodyStyle=Sedan",
                LayoutType = SectionLayoutType.Carousel,
                Subtitle = "Elegancia y confort para tu día a día",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new HomepageSectionConfig
            {
                Id = suvsSecId,
                Name = "SUVs",
                Slug = "suvs",
                Description = "SUVs y Crossovers versátiles",
                DisplayOrder = 3,
                MaxItems = 10,
                IsActive = true,
                Icon = "FaCar",
                AccentColor = "blue",
                ViewAllHref = "/vehicles?bodyStyle=SUV",
                LayoutType = SectionLayoutType.Carousel,
                Subtitle = "Espacio, potencia y versatilidad",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new HomepageSectionConfig
            {
                Id = camionetasId,
                Name = "Camionetas",
                Slug = "camionetas",
                Description = "Pickups y camionetas de trabajo",
                DisplayOrder = 4,
                MaxItems = 10,
                IsActive = true,
                Icon = "FaTruck",
                AccentColor = "blue",
                ViewAllHref = "/vehicles?bodyStyle=Pickup",
                LayoutType = SectionLayoutType.Carousel,
                Subtitle = "Potencia y capacidad para cualquier trabajo",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new HomepageSectionConfig
            {
                Id = deportivosId,
                Name = "Deportivos",
                Slug = "deportivos",
                Description = "Autos deportivos y de alto rendimiento",
                DisplayOrder = 5,
                MaxItems = 10,
                IsActive = true,
                Icon = "FaCar",
                AccentColor = "red",
                ViewAllHref = "/vehicles?bodyStyle=SportsCar",
                LayoutType = SectionLayoutType.Carousel,
                Subtitle = "Velocidad y adrenalina en cada curva",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new HomepageSectionConfig
            {
                Id = destacadosId,
                Name = "Destacados",
                Slug = "destacados",
                Description = "Vehículos destacados de la semana",
                DisplayOrder = 6,
                MaxItems = 10,
                IsActive = true,
                Icon = "FiStar",
                AccentColor = "amber",
                ViewAllHref = "/vehicles?featured=true",
                LayoutType = SectionLayoutType.Grid,
                Subtitle = "Selección exclusiva de nuestros mejores anuncios",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new HomepageSectionConfig
            {
                Id = lujoId,
                Name = "Lujo",
                Slug = "lujo",
                Description = "Vehículos de lujo y premium",
                DisplayOrder = 7,
                MaxItems = 10,
                IsActive = true,
                Icon = "FiStar",
                AccentColor = "purple",
                ViewAllHref = "/vehicles?minPrice=80000",
                LayoutType = SectionLayoutType.Carousel,
                Subtitle = "Exclusividad y prestigio",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // ========================================
        // VEHICLE HOMEPAGE SECTION (Many-to-Many)
        // ========================================

        modelBuilder.Entity<VehicleHomepageSection>(entity =>
        {
            entity.ToTable("vehicle_homepage_sections");
            entity.HasKey(vhs => vhs.Id);

            entity.HasOne(vhs => vhs.Vehicle)
                .WithMany(v => v.HomepageSectionAssignments)
                .HasForeignKey(vhs => vhs.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(vhs => vhs.HomepageSectionConfig)
                .WithMany(s => s.VehicleSections)
                .HasForeignKey(vhs => vhs.HomepageSectionConfigId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(vhs => new { vhs.VehicleId, vhs.HomepageSectionConfigId }).IsUnique();
            entity.HasIndex(vhs => vhs.HomepageSectionConfigId);
            entity.HasIndex(vhs => vhs.SortOrder);
            entity.HasIndex(vhs => vhs.IsPinned);
        });

        // ========================================
        // FAVORITES CONFIGURATION
        // ========================================

        modelBuilder.Entity<Favorite>(entity =>
        {
            entity.ToTable("favorites");
            entity.HasKey(f => f.Id);

            entity.Property(f => f.UserId).IsRequired();
            entity.Property(f => f.VehicleId).IsRequired();
            entity.Property(f => f.Notes).HasMaxLength(500);
            entity.Property(f => f.CreatedAt).IsRequired();

            // Relación con Vehicle
            entity.HasOne(f => f.Vehicle)
                .WithMany()
                .HasForeignKey(f => f.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);

            // Índices
            entity.HasIndex(f => f.UserId);
            entity.HasIndex(f => f.VehicleId);
            entity.HasIndex(f => new { f.UserId, f.VehicleId }).IsUnique();
        });

        // ========================================
        // SALE TRANSACTION CONFIGURATION
        // ========================================

        modelBuilder.Entity<SaleTransaction>(entity =>
        {
            entity.ToTable("sale_transactions");
            entity.HasKey(st => st.Id);
            entity.Property(st => st.Id).ValueGeneratedNever();
            entity.Property(st => st.VehicleId).IsRequired();
            entity.Property(st => st.SellerId).IsRequired();
            entity.Property(st => st.SellerType).HasMaxLength(50);
            entity.Property(st => st.BuyerEmail).HasMaxLength(255);
            entity.Property(st => st.ListedPrice).HasPrecision(18, 2);
            entity.Property(st => st.SalePrice).HasPrecision(18, 2);
            entity.Property(st => st.Currency).HasMaxLength(3).HasDefaultValue("DOP");
            entity.Property(st => st.VehicleTitle).HasMaxLength(500);
            entity.Property(st => st.Vin).HasMaxLength(17);
            entity.Property(st => st.Make).HasMaxLength(100);
            entity.Property(st => st.Model).HasMaxLength(100);
            entity.Property(st => st.IpAddress).HasMaxLength(45);
            entity.Property(st => st.UserAgent).HasMaxLength(500);
            entity.Property(st => st.Notes).HasMaxLength(2000);
            entity.Property(st => st.TenantId).HasMaxLength(100);
            entity.Property(st => st.ConfidenceLevel)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.HasIndex(st => st.VehicleId);
            entity.HasIndex(st => st.SellerId);
            entity.HasIndex(st => st.BuyerId);
            entity.HasIndex(st => st.SoldAt);
            entity.HasIndex(st => st.BuyerEmail);

            entity.Ignore(st => st.PriceDifference);
            entity.Ignore(st => st.DiscountPercentage);
            entity.Ignore(st => st.DaysToSell);
        });

        // ========================================
        // LEAD CONFIGURATION
        // ========================================

        modelBuilder.Entity<Lead>(entity =>
        {
            entity.ToTable("leads");
            entity.HasKey(l => l.Id);
            entity.Property(l => l.Id).ValueGeneratedNever();
            entity.Property(l => l.VehicleId).IsRequired();
            entity.Property(l => l.SellerId).IsRequired();
            entity.Property(l => l.BuyerName).IsRequired().HasMaxLength(200);
            entity.Property(l => l.BuyerEmail).IsRequired().HasMaxLength(254);
            entity.Property(l => l.BuyerPhone).HasMaxLength(20);
            entity.Property(l => l.Message).IsRequired().HasMaxLength(2000);
            entity.Property(l => l.VehicleTitle).HasMaxLength(300);
            entity.Property(l => l.VehicleImageUrl).HasMaxLength(500);
            entity.Property(l => l.VehiclePrice).HasPrecision(18, 2);
            entity.Property(l => l.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(l => l.Source).HasConversion<string>().HasMaxLength(20);
            entity.Property(l => l.IpAddress).HasMaxLength(45);
            entity.Property(l => l.UserAgent).HasMaxLength(500);
            entity.Property(l => l.CreatedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(l => l.Vehicle)
                .WithMany()
                .HasForeignKey(l => l.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(l => l.Messages)
                .WithOne(m => m.Lead)
                .HasForeignKey(m => m.LeadId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(l => l.SellerId);
            entity.HasIndex(l => l.DealerId);
            entity.HasIndex(l => l.VehicleId);
            entity.HasIndex(l => l.BuyerEmail);
            entity.HasIndex(l => l.Status);
            entity.HasIndex(l => l.CreatedAt);
        });

        modelBuilder.Entity<LeadMessage>(entity =>
        {
            entity.ToTable("lead_messages");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Id).ValueGeneratedNever();
            entity.Property(m => m.SenderId).IsRequired();
            entity.Property(m => m.SenderName).IsRequired().HasMaxLength(200);
            entity.Property(m => m.SenderRole).HasConversion<string>().HasMaxLength(20);
            entity.Property(m => m.Content).IsRequired().HasMaxLength(5000);
            entity.Property(m => m.CreatedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(m => m.LeadId);
            entity.HasIndex(m => m.CreatedAt);
        });
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is Vehicle vehicle)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        vehicle.CreatedAt = utcNow;
                        vehicle.UpdatedAt = utcNow;
                        vehicle.ConcurrencyStamp = Guid.NewGuid().ToString();
                        break;
                    case EntityState.Modified:
                        vehicle.UpdatedAt = utcNow;
                        vehicle.ConcurrencyStamp = Guid.NewGuid().ToString();
                        break;
                    case EntityState.Deleted:
                        // ✅ AUDIT FIX: Convert hard delete to soft delete
                        entry.State = EntityState.Modified;
                        vehicle.IsDeleted = true;
                        vehicle.UpdatedAt = utcNow;
                        vehicle.ConcurrencyStamp = Guid.NewGuid().ToString();
                        break;
                }
            }
            else if (entry.Entity is VehicleImage image && entry.State == EntityState.Added)
            {
                if (image.CreatedAt == default)
                    image.CreatedAt = utcNow;
            }
            else if (entry.Entity is Favorite fav && entry.State == EntityState.Added)
            {
                if (fav.CreatedAt == default)
                    fav.CreatedAt = utcNow;
            }
        }
    }
}
