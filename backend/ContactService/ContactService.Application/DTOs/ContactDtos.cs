namespace ContactService.Application.DTOs;

/// <summary>
/// Summary DTO for listing contact requests.
/// </summary>
public record ContactRequestSummaryDto
{
    public Guid Id { get; init; }
    public Guid? VehicleId { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string BuyerName { get; init; } = string.Empty;
    public string BuyerEmail { get; init; } = string.Empty;
    public string? BuyerPhone { get; init; }
    public string? SellerName { get; init; }
    public string? VehicleTitle { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? RespondedAt { get; init; }
    public int MessageCount { get; init; }
    public int UnreadCount { get; init; }
    public string? LastMessage { get; init; }
}

/// <summary>
/// Detailed DTO for a single contact request with messages.
/// </summary>
public record ContactRequestDetailDto
{
    public Guid Id { get; init; }
    public Guid? VehicleId { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string BuyerName { get; init; } = string.Empty;
    public string BuyerEmail { get; init; } = string.Empty;
    public string? BuyerPhone { get; init; }
    public string? SellerName { get; init; }
    public string? VehicleTitle { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? RespondedAt { get; init; }
    public List<ContactMessageDto> Messages { get; init; } = new();
}

/// <summary>
/// DTO for an individual message in a contact request thread.
/// </summary>
public record ContactMessageDto
{
    public Guid Id { get; init; }
    public Guid SenderId { get; init; }
    public string Message { get; init; } = string.Empty;
    public bool IsFromBuyer { get; init; }
    public bool IsRead { get; init; }
    public DateTime SentAt { get; init; }
}
