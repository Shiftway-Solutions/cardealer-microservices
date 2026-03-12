using MediatR;
using Microsoft.Extensions.Logging;
using AdminService.Application.Interfaces;

namespace AdminService.Application.UseCases.PlatformUsers;

/// <summary>
/// Handler for getting platform users with filtering
/// </summary>
public class GetPlatformUsersQueryHandler : IRequestHandler<GetPlatformUsersQuery, PaginatedUserResult>
{
    private readonly IPlatformUserService _userService;
    private readonly ILogger<GetPlatformUsersQueryHandler> _logger;

    public GetPlatformUsersQueryHandler(
        IPlatformUserService userService,
        ILogger<GetPlatformUsersQueryHandler> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public async Task<PaginatedUserResult> Handle(GetPlatformUsersQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching platform users with filters: search={Search}, type={Type}, status={Status}",
            request.Search, request.Type, request.Status);

        return await _userService.GetUsersAsync(
            search: request.Search,
            type: request.Type,
            status: request.Status,
            verified: request.Verified,
            page: request.Page,
            pageSize: request.PageSize,
            cancellationToken: cancellationToken);
    }
}

/// <summary>
/// Handler for getting platform user statistics
/// </summary>
public class GetPlatformUserStatsQueryHandler : IRequestHandler<GetPlatformUserStatsQuery, PlatformUserStatsDto>
{
    private readonly IPlatformUserService _userService;
    private readonly ILogger<GetPlatformUserStatsQueryHandler> _logger;

    public GetPlatformUserStatsQueryHandler(
        IPlatformUserService userService,
        ILogger<GetPlatformUserStatsQueryHandler> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public async Task<PlatformUserStatsDto> Handle(GetPlatformUserStatsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching platform user statistics");

        return await _userService.GetUserStatsAsync(cancellationToken);
    }
}

/// <summary>
/// Handler for getting a single platform user
/// </summary>
public class GetPlatformUserQueryHandler : IRequestHandler<GetPlatformUserQuery, PlatformUserDetailDto?>
{
    private readonly IPlatformUserService _userService;
    private readonly ILogger<GetPlatformUserQueryHandler> _logger;

    public GetPlatformUserQueryHandler(
        IPlatformUserService userService,
        ILogger<GetPlatformUserQueryHandler> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public async Task<PlatformUserDetailDto?> Handle(GetPlatformUserQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching platform user {UserId}", request.UserId);

        return await _userService.GetUserByIdAsync(request.UserId, cancellationToken);
    }
}

/// <summary>
/// Handler for updating platform user status
/// </summary>
public class UpdatePlatformUserStatusCommandHandler : IRequestHandler<UpdatePlatformUserStatusCommand, Unit>
{
    private readonly IPlatformUserService _userService;
    private readonly ILogger<UpdatePlatformUserStatusCommandHandler> _logger;

    public UpdatePlatformUserStatusCommandHandler(
        IPlatformUserService userService,
        ILogger<UpdatePlatformUserStatusCommandHandler> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public async Task<Unit> Handle(UpdatePlatformUserStatusCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating platform user {UserId} status to {Status}",
            request.UserId, request.Status);

        await _userService.UpdateUserStatusAsync(request.UserId, request.Status, request.Reason, cancellationToken);
        return Unit.Value;
    }
}

/// <summary>
/// Handler for verifying a platform user
/// </summary>
public class VerifyPlatformUserCommandHandler : IRequestHandler<VerifyPlatformUserCommand, Unit>
{
    private readonly IPlatformUserService _userService;
    private readonly ILogger<VerifyPlatformUserCommandHandler> _logger;

    public VerifyPlatformUserCommandHandler(
        IPlatformUserService userService,
        ILogger<VerifyPlatformUserCommandHandler> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public async Task<Unit> Handle(VerifyPlatformUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Verifying platform user {UserId}", request.UserId);

        await _userService.VerifyUserAsync(request.UserId, cancellationToken);
        return Unit.Value;
    }
}

/// <summary>
/// Handler for deleting a platform user
/// </summary>
public class DeletePlatformUserCommandHandler : IRequestHandler<DeletePlatformUserCommand, Unit>
{
    private readonly IPlatformUserService _userService;
    private readonly ILogger<DeletePlatformUserCommandHandler> _logger;

    public DeletePlatformUserCommandHandler(
        IPlatformUserService userService,
        ILogger<DeletePlatformUserCommandHandler> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public async Task<Unit> Handle(DeletePlatformUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting platform user {UserId}", request.UserId);

        await _userService.DeleteUserAsync(request.UserId, cancellationToken);
        return Unit.Value;
    }
}
