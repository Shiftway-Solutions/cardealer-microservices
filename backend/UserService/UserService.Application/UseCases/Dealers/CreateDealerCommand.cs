using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;
using UserService.Application.Metrics;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;
using CarDealer.Contracts.Events.Dealer;

namespace UserService.Application.UseCases.Dealers.CreateDealer;

public record CreateDealerCommand(
    CreateDealerRequest Request,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<DealerDto>;

public class CreateDealerCommandHandler : IRequestHandler<CreateDealerCommand, DealerDto>
{
    private readonly IDealerRepository _dealerRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IAuditServiceClient _auditClient;
    private readonly UserServiceMetrics _metrics;
    private readonly ILogger<CreateDealerCommandHandler> _logger;

    public CreateDealerCommandHandler(
        IDealerRepository dealerRepository,
        IUserRepository userRepository,
        IEventPublisher eventPublisher,
        IAuditServiceClient auditClient,
        UserServiceMetrics metrics,
        ILogger<CreateDealerCommandHandler> logger)
    {
        _dealerRepository = dealerRepository;
        _userRepository = userRepository;
        _eventPublisher = eventPublisher;
        _auditClient = auditClient;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task<DealerDto> Handle(CreateDealerCommand command, CancellationToken cancellationToken)
    {
        _metrics.RecordDealerRegistrationRequested();

        var request = command.Request;

        // ──────────────────────────────────────────────────────────
        // 1. Verify owner user exists
        // ──────────────────────────────────────────────────────────
        var owner = await _userRepository.GetByIdAsync(request.OwnerUserId);
        if (owner == null)
        {
            _metrics.RecordDealerRegistrationFailed("user_not_found");
            throw new KeyNotFoundException($"User {request.OwnerUserId} not found");
        }

        // ──────────────────────────────────────────────────────────
        // 2. Reject if user already has a dealer account
        // ──────────────────────────────────────────────────────────
        var existingDealer = await _dealerRepository.GetByOwnerIdAsync(request.OwnerUserId);
        if (existingDealer != null)
        {
            _metrics.RecordDealerRegistrationFailed("already_dealer");
            throw new InvalidOperationException("ALREADY_DEALER");
        }

        // ──────────────────────────────────────────────────────────
        // 3. Create dealer entity with Pending verification
        // ──────────────────────────────────────────────────────────
        var dealer = new Dealer
        {
            Id = Guid.NewGuid(),
            OwnerUserId = request.OwnerUserId,
            BusinessName = request.BusinessName,
            TradeName = request.TradeName,
            Slug = GenerateSlug(request.BusinessName, request.City),
            Description = request.Description,
            DealerType = request.DealerType,

            // Contact
            Email = request.Email,
            Phone = request.Phone,
            WhatsApp = request.WhatsApp,
            Website = request.Website,

            // Location
            Address = request.Address,
            City = request.City,
            State = request.State,
            ZipCode = request.ZipCode,
            Country = request.Country,
            Latitude = request.Latitude,
            Longitude = request.Longitude,

            // Legal
            BusinessRegistrationNumber = request.BusinessRegistrationNumber,
            TaxId = request.TaxId,
            DealerLicenseNumber = request.DealerLicenseNumber,
            LicenseExpiryDate = request.LicenseExpiryDate,

            // Branding
            LogoUrl = request.LogoUrl,
            BannerUrl = request.BannerUrl,

            // Defaults — dealer starts as Pending until admin approves
            VerificationStatus = DealerVerificationStatus.Pending,
            IsActive = false,
            AverageRating = 0,
            TotalReviews = 0,
            TotalListings = 0,
            ActiveListings = 0,
            TotalSales = 0,
            MaxListings = 10,
            CreatedAt = DateTime.UtcNow
        };

        await _dealerRepository.AddAsync(dealer);

        owner.AccountType = AccountType.Dealer;
        owner.DealerId = dealer.Id;
        owner.DealerRole = DealerRole.Owner;
        owner.BusinessName = dealer.BusinessName;
        owner.BusinessPhone = dealer.Phone;
        owner.BusinessAddress = dealer.Address;
        owner.RNC = dealer.TaxId;
        owner.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(owner);

        _logger.LogInformation(
            "Created dealer registration {DealerId} (BusinessName={BusinessName}) for user {UserId} — status Pending",
            dealer.Id, dealer.BusinessName, request.OwnerUserId);

        // ──────────────────────────────────────────────────────────
        // 4. Publish DealerRegistrationRequestedEvent
        // ──────────────────────────────────────────────────────────
        try
        {
            await _eventPublisher.PublishAsync(new DealerRegistrationRequestedEvent
            {
                DealerId = dealer.Id,
                CompanyName = dealer.BusinessName,
                OwnerUserId = dealer.OwnerUserId,
                RequestedAt = dealer.CreatedAt
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to publish DealerRegistrationRequestedEvent for dealer {DealerId}. " +
                "Dealer was created successfully. Event will be retried via DLQ.", dealer.Id);
        }

        // ──────────────────────────────────────────────────────────
        // 5. Audit log
        // ──────────────────────────────────────────────────────────
        try
        {
            await _auditClient.LogDealerRegistrationAsync(
                dealer.Id, dealer.OwnerUserId, dealer.BusinessName, request.OwnerUserId.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log audit for dealer registration {DealerId}", dealer.Id);
        }

        _metrics.RecordDealerRegistrationCreated();

        return MapToDto(dealer);
    }

    internal static DealerDto MapToDto(Dealer dealer)
    {
        return new DealerDto
        {
            Id = dealer.Id,
            OwnerUserId = dealer.OwnerUserId,
            BusinessName = dealer.BusinessName,
            TradeName = dealer.TradeName,
            Slug = dealer.Slug,
            Description = dealer.Description,
            DealerType = dealer.DealerType,
            Email = dealer.Email,
            Phone = dealer.Phone,
            WhatsApp = dealer.WhatsApp,
            Website = dealer.Website,
            Address = dealer.Address,
            City = dealer.City,
            State = dealer.State,
            ZipCode = dealer.ZipCode,
            Country = dealer.Country,
            Latitude = dealer.Latitude,
            Longitude = dealer.Longitude,
            LogoUrl = dealer.LogoUrl,
            BannerUrl = dealer.BannerUrl,
            PrimaryColor = dealer.PrimaryColor,
            BusinessRegistrationNumber = dealer.BusinessRegistrationNumber,
            TaxId = dealer.TaxId,
            DealerLicenseNumber = dealer.DealerLicenseNumber,
            LicenseExpiryDate = dealer.LicenseExpiryDate,
            VerificationStatus = dealer.VerificationStatus,
            VerifiedAt = dealer.VerifiedAt,
            RejectionReason = dealer.RejectionReason,
            TotalListings = dealer.TotalListings,
            ActiveListings = dealer.ActiveListings,
            TotalSales = dealer.TotalSales,
            AverageRating = dealer.AverageRating,
            TotalReviews = dealer.TotalReviews,
            ResponseTimeMinutes = dealer.ResponseTimeMinutes,
            IsActive = dealer.IsActive,
            AcceptsFinancing = dealer.AcceptsFinancing,
            AcceptsTradeIn = dealer.AcceptsTradeIn,
            OffersWarranty = dealer.OffersWarranty,
            HomeDelivery = dealer.HomeDelivery,
            BusinessHours = dealer.BusinessHours,
            SocialMediaLinks = dealer.SocialMediaLinks,
            MaxListings = dealer.MaxListings,
            IsFeatured = dealer.IsFeatured,
            FeaturedUntil = dealer.FeaturedUntil,
            CreatedAt = dealer.CreatedAt,
            UpdatedAt = dealer.UpdatedAt
        };
    }

    /// <summary>
    /// Generate a URL-friendly slug from business name and city.
    /// Example: "Auto Plaza" + "Santo Domingo" → "auto-plaza-santo-domingo"
    /// </summary>
    private static string GenerateSlug(string businessName, string? city)
    {
        var raw = string.IsNullOrWhiteSpace(city)
            ? businessName
            : $"{businessName} {city}";

        // Normalize: lowercase, replace accented chars, strip non-alnum, collapse dashes
        var slug = raw.ToLowerInvariant()
            .Replace('á', 'a').Replace('é', 'e').Replace('í', 'i')
            .Replace('ó', 'o').Replace('ú', 'u').Replace('ñ', 'n')
            .Replace('ü', 'u');

        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[\s-]+", "-");
        slug = slug.Trim('-');

        return slug;
    }
}
