using MediatR;
using UserService.Domain.Interfaces;

namespace UserService.Application.UseCases.Dealers.GetDealer;

/// <summary>
/// Public query for listing active dealers with pagination.
/// Used by the dealer directory page (/dealers/directorio).
/// </summary>
public record ListPublicDealersQuery(
    string? SearchTerm,
    string? Province,
    int Page = 1,
    int PageSize = 12
) : IRequest<PublicDealerListResponse>;

/// <summary>
/// Response matching the frontend PaginatedResponse format.
/// Field names match the frontend DealerDto interface exactly.
/// </summary>
public class PublicDealerListResponse
{
    public List<PublicDealerListItem> Items { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new();
}

public class PaginationInfo
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>
/// DTO with field names matching the frontend DealerDto interface.
/// Maps UserService Dealer entity fields to frontend-expected names.
/// </summary>
public class PublicDealerListItem
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? Rnc { get; set; }
    public string Type { get; set; } = "independent";
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? WhatsAppNumber { get; set; }
    public string? Website { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? BannerUrl { get; set; }
    public string Status { get; set; } = "active";
    public string VerificationStatus { get; set; } = "pending";
    public string CurrentPlan { get; set; } = "libre";
    public bool IsSubscriptionActive { get; set; }
    public int MaxActiveListings { get; set; }
    public int CurrentActiveListings { get; set; }
    public decimal? Rating { get; set; }
    public int? ReviewCount { get; set; }
    public int? AvgResponseTimeMinutes { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string? VerifiedAt { get; set; }
    public string? Description { get; set; }
    public string? Slug { get; set; }
}

public class ListPublicDealersQueryHandler : IRequestHandler<ListPublicDealersQuery, PublicDealerListResponse>
{
    private readonly IDealerRepository _dealerRepository;

    public ListPublicDealersQueryHandler(IDealerRepository dealerRepository)
    {
        _dealerRepository = dealerRepository;
    }

    public async Task<PublicDealerListResponse> Handle(ListPublicDealersQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 50);

        var dealers = await _dealerRepository.SearchAsync(
            searchTerm: request.SearchTerm,
            city: null,
            state: request.Province,
            dealerType: null,
            isVerified: null,
            page: page,
            pageSize: pageSize);

        var totalItems = await _dealerRepository.SearchCountAsync(
            searchTerm: request.SearchTerm,
            city: null,
            state: request.Province,
            dealerType: null,
            isVerified: null);

        var totalPages = totalItems > 0 ? (int)Math.Ceiling((double)totalItems / pageSize) : 0;

        return new PublicDealerListResponse
        {
            Items = dealers.Select(d => new PublicDealerListItem
            {
                Id = d.Id.ToString(),
                UserId = d.OwnerUserId.ToString(),
                BusinessName = d.BusinessName,
                LegalName = d.TradeName,
                Rnc = d.BusinessRegistrationNumber,
                Type = d.DealerType.ToString().ToLowerInvariant(),
                Email = d.Email,
                Phone = d.Phone,
                WhatsAppNumber = d.WhatsApp,
                Website = d.Website,
                Address = d.Address,
                City = d.City,
                Province = d.State,
                LogoUrl = d.LogoUrl,
                BannerUrl = d.BannerUrl,
                Status = d.IsActive ? "active" : "inactive",
                VerificationStatus = d.VerificationStatus.ToString().ToLowerInvariant(),
                CurrentPlan = "libre",
                IsSubscriptionActive = d.SubscriptionId.HasValue,
                MaxActiveListings = d.MaxListings,
                CurrentActiveListings = d.ActiveListings,
                Rating = d.AverageRating > 0 ? d.AverageRating : null,
                ReviewCount = d.TotalReviews > 0 ? d.TotalReviews : null,
                AvgResponseTimeMinutes = d.ResponseTimeMinutes > 0 ? d.ResponseTimeMinutes : null,
                CreatedAt = d.CreatedAt.ToString("o"),
                VerifiedAt = d.VerifiedAt?.ToString("o"),
                Description = d.Description,
                Slug = d.Slug,
            }).ToList(),
            Pagination = new PaginationInfo
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
            }
        };
    }
}
