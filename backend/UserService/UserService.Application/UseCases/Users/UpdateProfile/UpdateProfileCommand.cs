using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;
using UserService.Application.DTOs;
using UserService.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace UserService.Application.UseCases.Users.UpdateProfile
{
    /// <summary>
    /// Command to update the current user's profile (used by /api/users/me PUT)
    /// </summary>
    public record UpdateProfileCommand(
        Guid UserId,
        string? FirstName,
        string? LastName,
        string? Phone,
        string? Bio,
        string? Location,
        string? City,
        string? Province,
        string? PreferredLocale,
        string? PreferredCurrency
    ) : IRequest<UserDto>;

    public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, UserDto>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UpdateProfileCommandHandler> _logger;

        public UpdateProfileCommandHandler(
            IUserRepository userRepository,
            ILogger<UpdateProfileCommandHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<UserDto> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.UserId);

            if (user == null)
            {
                throw new NotFoundException($"User {request.UserId} not found");
            }

            // Update fields if provided
            if (!string.IsNullOrWhiteSpace(request.FirstName))
                user.FirstName = request.FirstName.Trim();

            if (!string.IsNullOrWhiteSpace(request.LastName))
                user.LastName = request.LastName.Trim();

            if (request.Phone != null)
                user.PhoneNumber = request.Phone.Trim();

            // Note: Bio is not in the User entity - would need to add it
            // For now, we skip it

            if (request.Location != null)
                user.BusinessAddress = request.Location.Trim();

            if (request.City != null)
                user.City = request.City.Trim();

            if (request.Province != null)
                user.Province = request.Province.Trim();

            if (!string.IsNullOrWhiteSpace(request.PreferredLocale))
                user.PreferredLocale = request.PreferredLocale.Trim();

            if (!string.IsNullOrWhiteSpace(request.PreferredCurrency))
                user.PreferredCurrency = request.PreferredCurrency.Trim();

            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("User profile updated: {UserId}", user.Id);

            return MapToDto(user);
        }

        private static UserDto MapToDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                Phone = user.PhoneNumber,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                AvatarUrl = user.ProfilePicture,
                Bio = null,
                Location = user.BusinessAddress,
                City = user.City,
                Province = user.Province,
                AccountType = user.AccountType.ToString().ToLowerInvariant(),
                IsVerified = user.IsVerified,
                IsEmailVerified = user.IsEmailVerified,
                IsPhoneVerified = false,
                VehicleCount = 0,
                ReviewCount = 0,
                AverageRating = 0.0,
                ResponseRate = 0.0,
                MemberSince = user.CreatedAt,
                LastActive = user.LastLoginAt,
                Badges = new List<UserBadgeDto>(),
                PreferredLocale = user.PreferredLocale ?? "es-DO",
                PreferredCurrency = user.PreferredCurrency ?? "DOP",
                IsActive = user.IsActive,
                EmailConfirmed = user.EmailConfirmed,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }
    }
}
