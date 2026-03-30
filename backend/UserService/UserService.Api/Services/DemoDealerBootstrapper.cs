using Microsoft.EntityFrameworkCore;
using UserService.Domain.Entities;
using UserService.Infrastructure.Persistence;

namespace UserService.Api.Services;

public sealed class DemoDealerBootstrapper
{
    private const string DemoDealerEmail = "nmateo@okla.com.do";
    private const string DemoBusinessName = "Auto Premium RD";
    private const string DemoTradeName = "Auto Premium";
    private const string DemoDescription = "Dealer premium especializado en vehiculos de lujo y alta gama.";
    private const string DemoPhone = "+18095552001";
    private const string DemoWhatsApp = "+18295552001";
    private const string DemoWebsite = "https://autopremiumrd.com.do";
    private const string DemoAddress = "Av. Abraham Lincoln #456, Piantini";
    private const string DemoCity = "Santo Domingo";
    private const string DemoState = "Distrito Nacional";
    private const string DemoZipCode = "10125";
    private const string DemoCountry = "DO";
    private const string DemoTaxId = "401-123456-1";
    private const string DemoLogoUrl = "https://cdn.okla.com.do/dealers/autopremium/logo.png";
    private const string DemoBannerUrl = "https://cdn.okla.com.do/dealers/autopremium/banner.jpg";

    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<DemoDealerBootstrapper> _logger;

    public DemoDealerBootstrapper(
        ApplicationDbContext context,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<DemoDealerBootstrapper> logger)
    {
        _context = context;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public async Task BootstrapAsync(CancellationToken cancellationToken = default)
    {
        if (!_environment.IsDevelopment())
        {
            return;
        }

        var featureEnabled = _configuration.GetValue<bool>("Features:BootstrapDemoDealer", true);
        if (!featureEnabled)
        {
            return;
        }

        var owner = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == DemoDealerEmail.ToLower(), cancellationToken);

        if (owner == null)
        {
            _logger.LogDebug("Skipping demo dealer bootstrap because {Email} is not present.", DemoDealerEmail);
            return;
        }

        var dealer = await _context.Dealers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.OwnerUserId == owner.Id, cancellationToken);

        if (dealer == null)
        {
            dealer = new Dealer
            {
                Id = Guid.NewGuid(),
                OwnerUserId = owner.Id,
                BusinessName = DemoBusinessName,
                TradeName = DemoTradeName,
                Slug = BuildSlug(DemoBusinessName, DemoCity),
                Description = DemoDescription,
                DealerType = DealerType.Independent,
                Email = DemoDealerEmail,
                Phone = DemoPhone,
                WhatsApp = DemoWhatsApp,
                Website = DemoWebsite,
                Address = DemoAddress,
                City = DemoCity,
                State = DemoState,
                ZipCode = DemoZipCode,
                Country = DemoCountry,
                LogoUrl = DemoLogoUrl,
                BannerUrl = DemoBannerUrl,
                TaxId = DemoTaxId,
                VerificationStatus = DealerVerificationStatus.Verified,
                VerifiedAt = DateTime.UtcNow,
                IsActive = true,
                AcceptsFinancing = true,
                AcceptsTradeIn = true,
                OffersWarranty = true,
                HomeDelivery = true,
                AverageRating = 4.8m,
                TotalReviews = 156,
                TotalSales = 342,
                TotalListings = 23,
                ActiveListings = 23,
                MaxListings = 50,
                ResponseTimeMinutes = 15,
                CreatedAt = DateTime.UtcNow,
            };

            await _context.Dealers.AddAsync(dealer, cancellationToken);
            _logger.LogInformation(
                "Bootstrapping demo dealer {DealerId} for user {UserId} ({Email}).",
                dealer.Id,
                owner.Id,
                DemoDealerEmail);
        }
        else
        {
            dealer.IsDeleted = false;
            dealer.DeletedAt = null;
            dealer.BusinessName = string.IsNullOrWhiteSpace(dealer.BusinessName) ? DemoBusinessName : dealer.BusinessName;
            dealer.TradeName = string.IsNullOrWhiteSpace(dealer.TradeName) ? DemoTradeName : dealer.TradeName;
            dealer.Slug = string.IsNullOrWhiteSpace(dealer.Slug) ? BuildSlug(dealer.BusinessName, dealer.City) : dealer.Slug;
            dealer.Description = string.IsNullOrWhiteSpace(dealer.Description) ? DemoDescription : dealer.Description;
            dealer.Email = string.IsNullOrWhiteSpace(dealer.Email) ? DemoDealerEmail : dealer.Email;
            dealer.Phone = string.IsNullOrWhiteSpace(dealer.Phone) ? DemoPhone : dealer.Phone;
            dealer.WhatsApp = string.IsNullOrWhiteSpace(dealer.WhatsApp) ? DemoWhatsApp : dealer.WhatsApp;
            dealer.Website = string.IsNullOrWhiteSpace(dealer.Website) ? DemoWebsite : dealer.Website;
            dealer.Address = string.IsNullOrWhiteSpace(dealer.Address) ? DemoAddress : dealer.Address;
            dealer.City = string.IsNullOrWhiteSpace(dealer.City) ? DemoCity : dealer.City;
            dealer.State = string.IsNullOrWhiteSpace(dealer.State) ? DemoState : dealer.State;
            dealer.ZipCode = string.IsNullOrWhiteSpace(dealer.ZipCode) ? DemoZipCode : dealer.ZipCode;
            dealer.Country = string.IsNullOrWhiteSpace(dealer.Country) ? DemoCountry : dealer.Country;
            dealer.LogoUrl = string.IsNullOrWhiteSpace(dealer.LogoUrl) ? DemoLogoUrl : dealer.LogoUrl;
            dealer.BannerUrl = string.IsNullOrWhiteSpace(dealer.BannerUrl) ? DemoBannerUrl : dealer.BannerUrl;
            dealer.TaxId = string.IsNullOrWhiteSpace(dealer.TaxId) ? DemoTaxId : dealer.TaxId;
            dealer.IsActive = true;
            dealer.VerificationStatus = DealerVerificationStatus.Verified;
            dealer.VerifiedAt ??= DateTime.UtcNow;
            dealer.AcceptsFinancing = true;
            dealer.AcceptsTradeIn = true;
            dealer.OffersWarranty = true;
            dealer.HomeDelivery = true;
            dealer.MaxListings = Math.Max(dealer.MaxListings, 50);
            dealer.ResponseTimeMinutes = dealer.ResponseTimeMinutes <= 0 ? 15 : dealer.ResponseTimeMinutes;
            dealer.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation(
                "Synced existing demo dealer {DealerId} for user {UserId} ({Email}).",
                dealer.Id,
                owner.Id,
                DemoDealerEmail);
        }

        owner.AccountType = AccountType.Dealer;
        owner.DealerId = dealer.Id;
        owner.DealerRole = DealerRole.Owner;
        owner.BusinessName = dealer.BusinessName;
        owner.BusinessPhone = dealer.Phone;
        owner.BusinessAddress = dealer.Address;
        owner.RNC = dealer.TaxId;
        owner.City = string.IsNullOrWhiteSpace(owner.City) ? dealer.City : owner.City;
        owner.Province = string.IsNullOrWhiteSpace(owner.Province) ? dealer.State : owner.Province;
        owner.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string BuildSlug(string businessName, string? city)
    {
        var raw = string.IsNullOrWhiteSpace(city)
            ? businessName
            : $"{businessName} {city}";

        var slug = raw.ToLowerInvariant()
            .Replace('a', 'a').Replace('e', 'e').Replace('i', 'i')
            .Replace('o', 'o').Replace('u', 'u').Replace('n', 'n')
            .Replace('u', 'u');

        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[\s-]+", "-");
        return slug.Trim('-');
    }
}