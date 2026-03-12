using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;

namespace UserService.Application.UseCases.Users.UpdateAccountType;

/// <summary>
/// Command to update a user's AccountType.
/// Restricted to Admin/SuperAdmin roles.
/// Used for promoting users (e.g., Buyer → Seller, Buyer → Admin) with full audit trail.
/// </summary>
public record UpdateAccountTypeCommand(
    Guid UserId,
    string AccountType,
    string PerformedBy,
    string? Reason = null,
    string? IpAddress = null
) : IRequest<UpdateAccountTypeResult>;

public record UpdateAccountTypeResult(
    bool Success,
    string? Error = null,
    string? PreviousAccountType = null,
    string? NewAccountType = null
);

public class UpdateAccountTypeHandler : IRequestHandler<UpdateAccountTypeCommand, UpdateAccountTypeResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditServiceClient _auditServiceClient;
    private readonly ILogger<UpdateAccountTypeHandler> _logger;

    // Only these transitions are allowed for security
    private static readonly HashSet<AccountType> AllowedTargetTypes = new()
    {
        AccountType.Buyer,
        AccountType.Seller,
        AccountType.Dealer,
        AccountType.Admin,
        AccountType.PlatformEmployee
    };

    public UpdateAccountTypeHandler(
        IUserRepository userRepository,
        IAuditServiceClient auditServiceClient,
        ILogger<UpdateAccountTypeHandler> logger)
    {
        _userRepository = userRepository;
        _auditServiceClient = auditServiceClient;
        _logger = logger;
    }

    public async Task<UpdateAccountTypeResult> Handle(UpdateAccountTypeCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate AccountType string
        if (!Enum.TryParse<AccountType>(request.AccountType, ignoreCase: true, out var newAccountType))
        {
            return new UpdateAccountTypeResult(false,
                Error: $"Invalid AccountType: '{request.AccountType}'. Valid values: {string.Join(", ", Enum.GetNames<AccountType>())}");
        }

        // 2. Validate allowed target types (prevent setting Guest or DealerEmployee via this endpoint)
        if (!AllowedTargetTypes.Contains(newAccountType))
        {
            return new UpdateAccountTypeResult(false,
                Error: $"AccountType '{newAccountType}' cannot be set via this endpoint");
        }

        // 3. Find user
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
        {
            return new UpdateAccountTypeResult(false, Error: $"User with ID '{request.UserId}' not found");
        }

        var previousType = user.AccountType;

        // 4. Skip if already the same type
        if (user.AccountType == newAccountType)
        {
            return new UpdateAccountTypeResult(true,
                PreviousAccountType: previousType.ToString(),
                NewAccountType: newAccountType.ToString());
        }

        // 5. Update
        user.AccountType = newAccountType;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        // 6. Audit log
        try
        {
            var changes = $"AccountType changed from {previousType} to {newAccountType}. Reason: {request.Reason ?? "N/A"}. IP: {request.IpAddress ?? "N/A"}";
            await _auditServiceClient.LogUserUpdatedAsync(request.UserId, changes, request.PerformedBy);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log audit for AccountType change on user {UserId}, but update was successful", request.UserId);
        }

        _logger.LogInformation(
            "AccountType updated for user {UserId} ({Email}): {PreviousType} → {NewType} by {PerformedBy}",
            user.Id, user.Email, previousType, newAccountType, request.PerformedBy);

        return new UpdateAccountTypeResult(true,
            PreviousAccountType: previousType.ToString(),
            NewAccountType: newAccountType.ToString());
    }
}
