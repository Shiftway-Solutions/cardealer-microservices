using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.DTOs;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;

namespace UserService.Application.UseCases.Sellers.CreateSellerProfile;

public record CreateSellerProfileCommand(CreateSellerProfileRequest Request) : IRequest<SellerProfileDto>;

public class CreateSellerProfileCommandHandler : IRequestHandler<CreateSellerProfileCommand, SellerProfileDto>
{
    private readonly ISellerProfileRepository _sellerProfileRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<CreateSellerProfileCommandHandler> _logger;

    public CreateSellerProfileCommandHandler(
        ISellerProfileRepository sellerProfileRepository,
        IUserRepository userRepository,
        ILogger<CreateSellerProfileCommandHandler> logger)
    {
        _sellerProfileRepository = sellerProfileRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<SellerProfileDto> Handle(CreateSellerProfileCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        // Verify user exists
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
        {
            throw new InvalidOperationException($"User {request.UserId} not found");
        }

        // Check if user already has a seller profile
        var existingProfile = await _sellerProfileRepository.GetByUserIdAsync(request.UserId);
        if (existingProfile != null)
        {
            throw new InvalidOperationException("User already has a seller profile");
        }

        var profile = new SellerProfile
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            FullName = request.FullName,
            DateOfBirth = request.DateOfBirth,
            Nationality = request.Nationality,

            // FASE 3: Phone and Email removed - use User entity properties instead
            WhatsApp = request.WhatsApp,

            // Location
            Address = request.Address,
            City = request.City,
            State = request.State,
            ZipCode = request.ZipCode,
            Country = request.Country,
            Latitude = request.Latitude,
            Longitude = request.Longitude,

            // Specialties
            Specialties = request.Specialties ?? Array.Empty<string>(),

            // Defaults
            VerificationStatus = SellerVerificationStatus.NotSubmitted,
            IsActive = true,
            AcceptsOffers = request.AcceptsOffers,
            ShowPhone = request.ShowPhone,
            ShowLocation = request.ShowLocation,
            PreferredContactMethod = request.PreferredContactMethod,

            // Limits
            MaxActiveListings = 3, // Default without verification
            CanSellHighValue = false,

            // Metrics
            AverageRating = 0,
            TotalReviews = 0,
            TotalListings = 0,
            ActiveListings = 0,
            TotalSales = 0,
            ResponseTimeMinutes = 0,

            CreatedAt = DateTime.UtcNow
        };

        await _sellerProfileRepository.AddAsync(profile);

        _logger.LogInformation("Created seller profile {ProfileId} for user {UserId}", profile.Id, request.UserId);

        return MapToDto(profile);
    }

    private static SellerProfileDto MapToDto(SellerProfile profile)
    {
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
            Latitude = profile.Latitude,
            Longitude = profile.Longitude,
            VerificationStatus = profile.VerificationStatus,
            VerifiedAt = profile.VerifiedAt,
            RejectionReason = profile.RejectionReason,
            TotalListings = profile.TotalListings,
            ActiveListings = profile.ActiveListings,
            TotalSales = profile.TotalSales,
            AverageRating = profile.AverageRating,
            TotalReviews = profile.TotalReviews,
            ResponseTimeMinutes = profile.ResponseTimeMinutes,
            IsActive = profile.IsActive,
            AcceptsOffers = profile.AcceptsOffers,
            ShowPhone = profile.ShowPhone,
            ShowLocation = profile.ShowLocation,
            PreferredContactMethod = profile.PreferredContactMethod,
            MaxActiveListings = profile.MaxActiveListings,
            CanSellHighValue = profile.CanSellHighValue,
            Specialties = profile.Specialties,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        };
    }
}
