using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.DTOs;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;
using UserService.Shared.Exceptions;

namespace UserService.Application.UseCases.Sellers.VerifySellerProfile;

public record VerifySellerProfileCommand(Guid SellerId, VerifySellerProfileRequest Request) : IRequest<SellerProfileDto>;

public class VerifySellerProfileCommandHandler : IRequestHandler<VerifySellerProfileCommand, SellerProfileDto>
{
    private readonly ISellerProfileRepository _sellerProfileRepository;
    private readonly ILogger<VerifySellerProfileCommandHandler> _logger;

    public VerifySellerProfileCommandHandler(
        ISellerProfileRepository sellerProfileRepository,
        ILogger<VerifySellerProfileCommandHandler> logger)
    {
        _sellerProfileRepository = sellerProfileRepository;
        _logger = logger;
    }

    public async Task<SellerProfileDto> Handle(VerifySellerProfileCommand command, CancellationToken cancellationToken)
    {
        var profile = await _sellerProfileRepository.GetByIdAsync(command.SellerId);
        if (profile == null)
        {
            throw new NotFoundException($"Seller profile {command.SellerId} not found");
        }

        var request = command.Request;

        if (request.IsVerified)
        {
            profile.VerificationStatus = SellerVerificationStatus.Verified;
            profile.VerifiedAt = DateTime.UtcNow;
            profile.VerifiedByUserId = request.VerifiedByUserId;
            profile.VerificationNotes = request.Notes;
            profile.RejectionReason = null;

            // Upgrade limits for verified sellers
            profile.MaxActiveListings = 10;
            profile.CanSellHighValue = true;

            // Set verification expiry (1 year)
            profile.VerificationExpiresAt = DateTime.UtcNow.AddYears(1);

            _logger.LogInformation("Seller profile {ProfileId} verified by user {VerifiedByUserId}",
                profile.Id, request.VerifiedByUserId);
        }
        else
        {
            profile.VerificationStatus = SellerVerificationStatus.Rejected;
            profile.RejectionReason = request.Notes;
            profile.VerifiedAt = null;
            profile.VerifiedByUserId = null;
            profile.VerificationExpiresAt = null;

            // Keep default limits
            profile.MaxActiveListings = 3;
            profile.CanSellHighValue = false;

            _logger.LogInformation("Seller profile {ProfileId} rejected. Reason: {Reason}",
                profile.Id, request.Notes);
        }

        profile.UpdatedAt = DateTime.UtcNow;

        await _sellerProfileRepository.UpdateAsync(profile);

        return new SellerProfileDto
        {
            Id = profile.Id,
            UserId = profile.UserId,
            FullName = profile.FullName,
            DateOfBirth = profile.DateOfBirth,
            Nationality = profile.Nationality,
            Bio = profile.Bio,
            AvatarUrl = profile.AvatarUrl,
            // FASE 3: Phone and Email removed - use User entity properties instead
            WhatsApp = profile.WhatsApp,
            Address = profile.Address,
            City = profile.City,
            State = profile.State,
            ZipCode = profile.ZipCode,
            Country = profile.Country,
            VerificationStatus = profile.VerificationStatus,
            VerifiedAt = profile.VerifiedAt,
            RejectionReason = profile.RejectionReason,
            AverageRating = profile.AverageRating,
            TotalReviews = profile.TotalReviews,
            ActiveListings = profile.ActiveListings,
            TotalSales = profile.TotalSales,
            IsActive = profile.IsActive,
            MaxActiveListings = profile.MaxActiveListings,
            CanSellHighValue = profile.CanSellHighValue,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        };
    }
}
