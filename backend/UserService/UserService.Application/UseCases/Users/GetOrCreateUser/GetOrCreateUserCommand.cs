using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace UserService.Application.UseCases.Users.GetOrCreateUser
{
    /// <summary>
    /// Command to get existing user or create a new one if not exists.
    /// Used primarily for OAuth users who authenticate via AuthService but
    /// don't have a profile in UserService yet.
    /// </summary>
    public record GetOrCreateUserCommand(
        Guid UserId,
        string Email,
        string FirstName,
        string LastName,
        string? AvatarUrl = null
    ) : IRequest<UserDto>;

    public class GetOrCreateUserCommandHandler : IRequestHandler<GetOrCreateUserCommand, UserDto>
    {
        private readonly IUserRepository _userRepository;
        private readonly IAuditServiceClient _auditClient;
        private readonly INotificationServiceClient _notificationClient;
        private readonly ILogger<GetOrCreateUserCommandHandler> _logger;

        public GetOrCreateUserCommandHandler(
            IUserRepository userRepository,
            IAuditServiceClient auditClient,
            INotificationServiceClient notificationClient,
            ILogger<GetOrCreateUserCommandHandler> logger)
        {
            _userRepository = userRepository;
            _auditClient = auditClient;
            _notificationClient = notificationClient;
            _logger = logger;
        }

        public async Task<UserDto> Handle(GetOrCreateUserCommand request, CancellationToken cancellationToken)
        {
            // First, try to get existing user by ID
            var user = await _userRepository.GetByIdAsync(request.UserId);

            if (user != null)
            {
                _logger.LogDebug("User {UserId} found in UserService", request.UserId);
                return MapToDto(user);
            }

            // User doesn't exist, also check by email
            user = await _userRepository.GetByEmailAsync(request.Email);

            if (user != null)
            {
                _logger.LogWarning("User with email {Email} exists but with different ID. Returning existing user.", request.Email);
                return MapToDto(user);
            }

            // Create new user profile (OAuth user sync)
            _logger.LogInformation("Creating UserService profile for OAuth user {UserId} ({Email})",
                request.UserId, request.Email);

            user = new User
            {
                Id = request.UserId, // Use the same ID from AuthService
                Email = request.Email.ToLower().Trim(),
                PasswordHash = string.Empty, // OAuth users don't have password in UserService
                FirstName = request.FirstName?.Trim() ?? string.Empty,
                LastName = request.LastName?.Trim() ?? string.Empty,
                PhoneNumber = string.Empty,
                ProfilePicture = request.AvatarUrl,
                IsActive = true,
                EmailConfirmed = true, // OAuth emails are pre-verified
                IsEmailVerified = true,
                AccountType = AccountType.Buyer,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user);

            _logger.LogInformation("UserService profile created successfully for OAuth user {UserId}", user.Id);

            // Auditoría (fire-and-forget, no bloquea)
            _ = _auditClient.LogUserCreatedAsync(user.Id, user.Email, "oauth_sync");

            // Notificación de bienvenida (fire-and-forget)
            _ = _notificationClient.SendWelcomeEmailAsync(user.Email, user.FirstName, user.LastName);

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
                Bio = null, // Not yet implemented in User entity
                Location = user.BusinessAddress,
                City = user.City,
                Province = user.Province,
                AccountType = user.AccountType.ToString().ToLowerInvariant(),
                IsVerified = user.IsVerified,
                IsEmailVerified = user.IsEmailVerified,
                IsPhoneVerified = false, // Not yet tracked
                VehicleCount = 0, // Would need to query VehiclesSaleService
                ReviewCount = 0, // Would need to query ReviewService
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
