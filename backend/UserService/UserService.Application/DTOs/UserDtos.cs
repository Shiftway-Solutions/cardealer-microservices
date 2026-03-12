using System;
using System.Collections.Generic;

namespace UserService.Application.DTOs
{
    /// <summary>
    /// User profile DTO - matches frontend UserProfileDto expectations
    /// </summary>
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public string? Location { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public string AccountType { get; set; } = "buyer";
        public bool IsVerified { get; set; }
        public bool IsEmailVerified { get; set; }
        public bool IsPhoneVerified { get; set; }
        public int VehicleCount { get; set; }
        public int ReviewCount { get; set; }
        public double AverageRating { get; set; }
        public double ResponseRate { get; set; }
        public DateTime MemberSince { get; set; }
        public DateTime? LastActive { get; set; }
        public List<UserBadgeDto> Badges { get; set; } = new();
        public string PreferredLocale { get; set; } = "es-DO";
        public string PreferredCurrency { get; set; } = "DOP";

        // Legacy fields for backwards compatibility
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool EmailConfirmed { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    /// <summary>
    /// User badge DTO
    /// </summary>
    public class UserBadgeDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EarnedAt { get; set; }
    }

    public class CreateUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request for updating user profile via PUT /api/users/me
    /// </summary>
    public class UpdateProfileRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public string? Bio { get; set; }
        public string? Location { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public string? PreferredLocale { get; set; }
        public string? PreferredCurrency { get; set; }
    }

    public class UpdateUserRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public bool? IsActive { get; set; }
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class PaginatedUsersResponse
    {
        public List<UserDto> Users { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }

    /// <summary>
    /// Response for GET /api/users/me/vehicles
    /// </summary>
    public class UserVehiclesResponse
    {
        public List<UserVehicleDto> Vehicles { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int TotalPages { get; set; }
    }

    /// <summary>
    /// Vehicle DTO for user's vehicles listing
    /// Field names match the frontend TypeScript interface (camelCase via JSON serialization)
    /// </summary>
    public class UserVehicleDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "DOP";
        public string Status { get; set; } = string.Empty;
        // Frontend expects "imageUrl" not "mainImageUrl"
        public string? ImageUrl { get; set; }
        public int Year { get; set; }
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Mileage { get; set; }
        public string? Transmission { get; set; }
        public string? FuelType { get; set; }
        // Frontend expects "viewCount" not "views"
        public int ViewCount { get; set; }
        // Frontend expects "inquiryCount" not "favorites"
        public int InquiryCount { get; set; }
        public bool IsFeatured { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>
    /// Response for GET /api/users/me/stats
    /// </summary>
    public class UserStatsResponse
    {
        public int VehiclesPublished { get; set; }
        public int VehiclesSold { get; set; }
        public int TotalViews { get; set; }
        public int TotalInquiries { get; set; }
        public double ResponseRate { get; set; }
        public string AverageResponseTime { get; set; } = "N/A";
        public int ReviewCount { get; set; }
        public double AverageRating { get; set; }
    }
}
