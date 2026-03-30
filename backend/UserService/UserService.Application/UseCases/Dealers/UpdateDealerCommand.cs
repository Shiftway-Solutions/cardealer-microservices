using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.DTOs;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;
using UserService.Shared.Exceptions;

namespace UserService.Application.UseCases.Dealers.UpdateDealer;

public record UpdateDealerCommand(Guid DealerId, UpdateDealerRequest Request) : IRequest<DealerDto>;

public class UpdateDealerCommandHandler : IRequestHandler<UpdateDealerCommand, DealerDto>
{
    private readonly IDealerRepository _dealerRepository;
    private readonly ILogger<UpdateDealerCommandHandler> _logger;

    public UpdateDealerCommandHandler(
        IDealerRepository dealerRepository,
        ILogger<UpdateDealerCommandHandler> logger)
    {
        _dealerRepository = dealerRepository;
        _logger = logger;
    }

    public async Task<DealerDto> Handle(UpdateDealerCommand command, CancellationToken cancellationToken)
    {
        var dealer = await _dealerRepository.GetByIdAsync(command.DealerId);
        if (dealer == null)
        {
            throw new NotFoundException($"Dealer {command.DealerId} not found");
        }

        var request = command.Request;

        // Update only provided fields
        if (request.BusinessName != null) dealer.BusinessName = request.BusinessName;
        if (request.TradeName != null) dealer.TradeName = request.TradeName;

        // Regenerate slug if BusinessName or City changed
        if (request.BusinessName != null || request.City != null)
        {
            dealer.Slug = GenerateSlug(dealer.BusinessName, request.City ?? dealer.City);
        }
        if (request.Description != null) dealer.Description = request.Description;
        if (request.DealerType.HasValue) dealer.DealerType = request.DealerType.Value;

        // Contact
        if (request.Email != null) dealer.Email = request.Email;
        if (request.Phone != null) dealer.Phone = request.Phone;
        if (request.WhatsApp != null) dealer.WhatsApp = request.WhatsApp;
        if (request.Website != null) dealer.Website = request.Website;

        // Location
        if (request.Address != null) dealer.Address = request.Address;
        if (request.City != null) dealer.City = request.City;
        if (request.State != null) dealer.State = request.State;
        if (request.ZipCode != null) dealer.ZipCode = request.ZipCode;
        if (request.Country != null) dealer.Country = request.Country;
        if (request.Latitude.HasValue) dealer.Latitude = request.Latitude.Value;
        if (request.Longitude.HasValue) dealer.Longitude = request.Longitude.Value;

        // Branding
        if (request.LogoUrl != null) dealer.LogoUrl = request.LogoUrl;
        if (request.BannerUrl != null) dealer.BannerUrl = request.BannerUrl;
        if (request.PrimaryColor != null) dealer.PrimaryColor = request.PrimaryColor;

        // Configuration
        if (request.AcceptsFinancing.HasValue) dealer.AcceptsFinancing = request.AcceptsFinancing.Value;
        if (request.AcceptsTradeIn.HasValue) dealer.AcceptsTradeIn = request.AcceptsTradeIn.Value;
        if (request.OffersWarranty.HasValue) dealer.OffersWarranty = request.OffersWarranty.Value;
        if (request.HomeDelivery.HasValue) dealer.HomeDelivery = request.HomeDelivery.Value;
        if (request.BusinessHours != null) dealer.BusinessHours = request.BusinessHours;
        if (request.SocialMediaLinks != null) dealer.SocialMediaLinks = request.SocialMediaLinks;
        if (request.IsActive.HasValue) dealer.IsActive = request.IsActive.Value;

        dealer.UpdatedAt = DateTime.UtcNow;

        await _dealerRepository.UpdateAsync(dealer);

        _logger.LogInformation("Updated dealer {DealerId}", dealer.Id);

        return new DealerDto
        {
            Id = dealer.Id,
            OwnerUserId = dealer.OwnerUserId,
            BusinessName = dealer.BusinessName,
            TradeName = dealer.TradeName,
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
            Slug = dealer.Slug,
            CreatedAt = dealer.CreatedAt,
            UpdatedAt = dealer.UpdatedAt
        };
    }

    private static string GenerateSlug(string businessName, string? city)
    {
        var raw = string.IsNullOrWhiteSpace(city)
            ? businessName
            : $"{businessName} {city}";

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
