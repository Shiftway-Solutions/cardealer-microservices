using System;
using System.Collections.Generic;
using UserService.Domain.Entities;

namespace UserService.Application.DTOs;

#region Dealer DTOs

/// <summary>
/// Full dealer information
/// </summary>
public class DealerDto
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }

    // Basic Info
    public string BusinessName { get; set; } = string.Empty;
    public string? TradeName { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public DealerType DealerType { get; set; }

    // Contact
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? WhatsApp { get; set; }
    public string? Website { get; set; }

    // Location
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string? ZipCode { get; set; }
    public string Country { get; set; } = "DO";
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // Branding
    public string? LogoUrl { get; set; }
    public string? BannerUrl { get; set; }
    public string? PrimaryColor { get; set; }

    // Legal
    public string? BusinessRegistrationNumber { get; set; }
    public string? TaxId { get; set; }
    public string? DealerLicenseNumber { get; set; }
    public DateTime? LicenseExpiryDate { get; set; }

    // Verification
    public DealerVerificationStatus VerificationStatus { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? RejectionReason { get; set; }

    // Metrics
    public int TotalListings { get; set; }
    public int ActiveListings { get; set; }
    public int TotalSales { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public int ResponseTimeMinutes { get; set; }

    // Configuration
    public bool IsActive { get; set; }
    public bool AcceptsFinancing { get; set; }
    public bool AcceptsTradeIn { get; set; }
    public bool OffersWarranty { get; set; }
    public bool HomeDelivery { get; set; }
    public string? BusinessHours { get; set; }
    public string? SocialMediaLinks { get; set; }

    // Subscription
    public int MaxListings { get; set; }
    public bool IsFeatured { get; set; }
    public DateTime? FeaturedUntil { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Related
    public List<DealerEmployeeDto>? Employees { get; set; }
    public DealerSubscriptionDto? CurrentSubscription { get; set; }
}

/// <summary>
/// Summary view of dealer for listings
/// </summary>
public class DealerSummaryDto
{
    public Guid Id { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public DealerType DealerType { get; set; }
    public string? LogoUrl { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DealerVerificationStatus VerificationStatus { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public int ActiveListings { get; set; }
}

#endregion

#region Create/Update Requests

/// <summary>
/// Request to create a new dealer
/// </summary>
public class CreateDealerRequest
{
    public Guid OwnerUserId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string? TradeName { get; set; }
    public string? Description { get; set; }
    public DealerType DealerType { get; set; } = DealerType.Independent;

    // Contact
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? WhatsApp { get; set; }
    public string? Website { get; set; }

    // Location
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string? ZipCode { get; set; }
    public string Country { get; set; } = "DO";
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // Legal
    public string? BusinessRegistrationNumber { get; set; }
    public string? TaxId { get; set; }
    public string? DealerLicenseNumber { get; set; }
    public DateTime? LicenseExpiryDate { get; set; }

    // Optional branding
    public string? LogoUrl { get; set; }
    public string? BannerUrl { get; set; }
}

/// <summary>
/// Request to update a dealer
/// </summary>
public class UpdateDealerRequest
{
    public string? BusinessName { get; set; }
    public string? TradeName { get; set; }
    public string? Description { get; set; }
    public DealerType? DealerType { get; set; }

    // Contact
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public string? Website { get; set; }

    // Location
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? Country { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // Branding
    public string? LogoUrl { get; set; }
    public string? BannerUrl { get; set; }
    public string? PrimaryColor { get; set; }

    // Configuration
    public bool? AcceptsFinancing { get; set; }
    public bool? AcceptsTradeIn { get; set; }
    public bool? OffersWarranty { get; set; }
    public bool? HomeDelivery { get; set; }
    public string? BusinessHours { get; set; }
    public string? SocialMediaLinks { get; set; }

    public bool? IsActive { get; set; }
}

/// <summary>
/// Request to verify/reject a dealer
/// </summary>
public class VerifyDealerRequest
{
    public bool IsVerified { get; set; }
    public Guid VerifiedByUserId { get; set; }
    public string? Notes { get; set; }
}

public class PaginatedDealersResponse
{
    public List<DealerSummaryDto> Dealers { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

#endregion

#region Dealer Employee DTOs

public class DealerEmployeeDto
{
    public Guid Id { get; set; }
    public Guid DealerId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty; // DealerRole enum as string
    public string Status { get; set; } = string.Empty; // EmployeeStatus enum as string
    public string Permissions { get; set; } = "[]"; // JSON array
    public DateTime InvitationDate { get; set; }
    public DateTime? ActivationDate { get; set; }

    // From User (populated via join)
    public string? UserFullName { get; set; }
    public string? UserEmail { get; set; }
}

public class InviteDealerEmployeeRequest
{
    public Guid DealerId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Sales"; // DealerRole: Owner, Manager, Sales, Finance, Admin
    public string? Permissions { get; set; } // JSON array of permissions
    public string? Notes { get; set; }
}

public class UpdateDealerEmployeeRequest
{
    public string? Role { get; set; } // DealerRole as string
    public string? Status { get; set; } // EmployeeStatus: Pending, Active, Suspended
    public string? Permissions { get; set; }
    public string? Notes { get; set; }
}

#endregion

#region Subscription DTOs

public class DealerSubscriptionDto
{
    public Guid Id { get; set; }
    public Guid DealerId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string PlanType { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public int MaxListings { get; set; }
    public bool IncludesFeatured { get; set; }
    public bool IncludesAnalytics { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool AutoRenew { get; set; }
}

#endregion
