using MediatR;
using Microsoft.Extensions.Logging;
using AdminService.Domain.Entities;
using AdminService.Domain.Enums;
using AdminService.Domain.Interfaces;
using AdminService.Application.Exceptions;

namespace AdminService.Application.UseCases.AdminUsers;

/// <summary>
/// Handler para obtener lista de admin users
/// </summary>
public class GetAdminUsersQueryHandler : IRequestHandler<GetAdminUsersQuery, PaginatedResult<AdminUserDto>>
{
    private readonly IAdminUserRepository _adminUserRepository;
    private readonly ILogger<GetAdminUsersQueryHandler> _logger;

    public GetAdminUsersQueryHandler(
        IAdminUserRepository adminUserRepository,
        ILogger<GetAdminUsersQueryHandler> logger)
    {
        _adminUserRepository = adminUserRepository;
        _logger = logger;
    }

    public async Task<PaginatedResult<AdminUserDto>> Handle(GetAdminUsersQuery request, CancellationToken cancellationToken)
    {
        AdminRole? role = null;
        if (!string.IsNullOrEmpty(request.Role) && Enum.TryParse<AdminRole>(request.Role, out var parsedRole))
        {
            role = parsedRole;
        }

        var (admins, totalCount) = await _adminUserRepository.GetAllAsync(
            page: request.Page,
            pageSize: request.PageSize,
            roleFilter: role,
            isActiveFilter: request.IsActive,
            cancellationToken: cancellationToken
        );

        var dtos = admins.Select(a => new AdminUserDto
        {
            Id = a.Id,
            Email = a.Email,
            FirstName = a.FullName.Split(' ').FirstOrDefault() ?? string.Empty,
            LastName = a.FullName.Split(' ').Skip(1).FirstOrDefault() ?? string.Empty,
            Role = a.Role.ToString(),
            Permissions = a.CustomPermissions.ToArray(),
            IsActive = a.IsActive,
            CreatedAt = a.CreatedAt,
            LastLoginAt = a.LastLoginAt,
            AvatarUrl = a.AvatarUrl
        }).ToList();

        return new PaginatedResult<AdminUserDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}

/// <summary>
/// Handler para obtener detalle de admin user
/// </summary>
public class GetAdminUserQueryHandler : IRequestHandler<GetAdminUserQuery, AdminUserDetailDto?>
{
    private readonly IAdminUserRepository _adminUserRepository;
    private readonly IAdminActionLogRepository _actionLogRepository;
    private readonly ILogger<GetAdminUserQueryHandler> _logger;

    public GetAdminUserQueryHandler(
        IAdminUserRepository adminUserRepository,
        IAdminActionLogRepository actionLogRepository,
        ILogger<GetAdminUserQueryHandler> logger)
    {
        _adminUserRepository = adminUserRepository;
        _actionLogRepository = actionLogRepository;
        _logger = logger;
    }

    public async Task<AdminUserDetailDto?> Handle(GetAdminUserQuery request, CancellationToken cancellationToken)
    {
        var admin = await _adminUserRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (admin == null)
        {
            return null;
        }

        // Get recent actions
        var recentActions = await _actionLogRepository.GetRecentByAdminIdAsync(request.UserId, 10);

        // Get action counts for this month
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var monthlyActions = await _actionLogRepository.GetCountByAdminIdAsync(request.UserId, startOfMonth, DateTime.UtcNow);

        var nameParts = admin.FullName.Split(' ', 2);

        return new AdminUserDetailDto
        {
            Id = admin.Id,
            Email = admin.Email,
            FirstName = nameParts.FirstOrDefault() ?? string.Empty,
            LastName = nameParts.Length > 1 ? nameParts[1] : string.Empty,
            Phone = admin.PhoneNumber,
            Role = admin.Role.ToString(),
            Permissions = admin.CustomPermissions.ToArray(),
            IsActive = admin.IsActive,
            TwoFactorEnabled = admin.MfaEnabled,
            CreatedAt = admin.CreatedAt,
            UpdatedAt = admin.UpdatedAt,
            LastLoginAt = admin.LastLoginAt,
            AvatarUrl = admin.AvatarUrl,
            TotalActionsThisMonth = monthlyActions,
            RecentActions = recentActions.Select(a => new RecentActionDto
            {
                Action = a.Action,
                Description = a.Description ?? string.Empty,
                Timestamp = a.Timestamp
            }).ToList()
        };
    }
}

/// <summary>
/// Handler para obtener dashboard de admin
/// </summary>
public class GetAdminDashboardQueryHandler : IRequestHandler<GetAdminDashboardQuery, AdminDashboardDto>
{
    private readonly IStatisticsRepository _statsRepository;
    private readonly IAdminActionLogRepository _actionLogRepository;
    private readonly ILogger<GetAdminDashboardQueryHandler> _logger;

    public GetAdminDashboardQueryHandler(
        IStatisticsRepository statsRepository,
        IAdminActionLogRepository actionLogRepository,
        ILogger<GetAdminDashboardQueryHandler> logger)
    {
        _statsRepository = statsRepository;
        _actionLogRepository = actionLogRepository;
        _logger = logger;
    }

    public async Task<AdminDashboardDto> Handle(GetAdminDashboardQuery request, CancellationToken cancellationToken)
    {
        var stats = await _statsRepository.GetPlatformStatsAsync();
        var recentActivity = await _actionLogRepository.GetRecentAsync(20);

        return new AdminDashboardDto
        {
            Stats = new DashboardStatsDto
            {
                TotalDealers = stats.TotalDealers,
                ActiveDealers = stats.ActiveDealers,
                PendingDealers = stats.PendingDealers,
                TotalUsers = stats.TotalUsers,
                TotalListings = stats.TotalListings,
                PendingListings = stats.PendingListings,
                TotalTransactions = stats.TotalTransactions,
                MonthlyRevenue = stats.MonthlyRevenue,
                OpenTickets = stats.OpenTickets
            },
            PendingItems = new List<PendingItemSummaryDto>
            {
                new() { Type = "Dealers", Count = stats.PendingDealers, Priority = "High", Link = "/admin/dealers?status=pending" },
                new() { Type = "Listings", Count = stats.PendingListings, Priority = "Normal", Link = "/admin/moderation" },
                new() { Type = "Tickets", Count = stats.OpenTickets, Priority = "Normal", Link = "/admin/support" }
            },
            RecentActivity = recentActivity.Select(a => new RecentActionDto
            {
                Action = a.Action,
                Description = a.Description ?? string.Empty,
                Timestamp = a.Timestamp
            }).ToList()
        };
    }
}

/// <summary>
/// Handler para obtener items pendientes
/// </summary>
public class GetPendingItemsQueryHandler : IRequestHandler<GetPendingItemsQuery, PendingItemsDto>
{
    private readonly IStatisticsRepository _statsRepository;
    private readonly IModerationRepository _moderationRepository;
    private readonly ILogger<GetPendingItemsQueryHandler> _logger;

    public GetPendingItemsQueryHandler(
        IStatisticsRepository statsRepository,
        IModerationRepository moderationRepository,
        ILogger<GetPendingItemsQueryHandler> logger)
    {
        _statsRepository = statsRepository;
        _moderationRepository = moderationRepository;
        _logger = logger;
    }

    public async Task<PendingItemsDto> Handle(GetPendingItemsQuery request, CancellationToken cancellationToken)
    {
        var pendingDealers = await _statsRepository.GetPendingDealersAsync(20);
        var pendingListings = await _moderationRepository.GetPendingListingsAsync(20);
        var pendingReports = await _moderationRepository.GetPendingReportsAsync(20);

        return new PendingItemsDto
        {
            PendingDealers = pendingDealers.Select(d => new PendingDealerDto
            {
                Id = d.Id,
                BusinessName = d.BusinessName,
                Email = d.Email,
                SubmittedAt = d.SubmittedAt,
                DaysPending = (int)(DateTime.UtcNow - d.SubmittedAt).TotalDays
            }).ToList(),
            PendingListings = pendingListings.Select(l => new PendingListingDto
            {
                Id = l.Id,
                Title = l.Title,
                DealerName = l.DealerName,
                SubmittedAt = l.SubmittedAt
            }).ToList(),
            PendingReports = pendingReports.Select(r => new PendingReportDto
            {
                Id = r.Id,
                Type = r.Type,
                TargetType = r.TargetType,
                TargetId = r.TargetId,
                ReportedAt = r.ReportedAt,
                ReportCount = r.ReportCount
            }).ToList()
        };
    }
}

/// <summary>
/// Handler para actualizar rol de admin
/// </summary>
public class UpdateAdminRoleCommandHandler : IRequestHandler<UpdateAdminRoleCommand, Unit>
{
    private readonly IAdminUserRepository _adminUserRepository;
    private readonly ILogger<UpdateAdminRoleCommandHandler> _logger;

    public UpdateAdminRoleCommandHandler(
        IAdminUserRepository adminUserRepository,
        ILogger<UpdateAdminRoleCommandHandler> logger)
    {
        _adminUserRepository = adminUserRepository;
        _logger = logger;
    }

    public async Task<Unit> Handle(UpdateAdminRoleCommand request, CancellationToken cancellationToken)
    {
        var admin = await _adminUserRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (admin == null)
        {
            throw new NotFoundException($"Admin user {request.UserId} not found");
        }

        if (!Enum.TryParse<AdminRole>(request.Role, out var role))
        {
            throw new BadRequestException($"Invalid role: {request.Role}");
        }

        admin.Role = role;
        if (request.Permissions != null)
        {
            admin.CustomPermissions = request.Permissions.ToList();
        }
        admin.UpdatedAt = DateTime.UtcNow;

        await _adminUserRepository.UpdateAsync(admin, cancellationToken);

        _logger.LogInformation("Admin user {UserId} role updated to {Role}", request.UserId, role);

        return Unit.Value;
    }
}

/// <summary>
/// Handler para suspender admin
/// </summary>
public class SuspendAdminUserCommandHandler : IRequestHandler<SuspendAdminUserCommand, Unit>
{
    private readonly IAdminUserRepository _adminUserRepository;
    private readonly ILogger<SuspendAdminUserCommandHandler> _logger;

    public SuspendAdminUserCommandHandler(
        IAdminUserRepository adminUserRepository,
        ILogger<SuspendAdminUserCommandHandler> logger)
    {
        _adminUserRepository = adminUserRepository;
        _logger = logger;
    }

    public async Task<Unit> Handle(SuspendAdminUserCommand request, CancellationToken cancellationToken)
    {
        var admin = await _adminUserRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (admin == null)
        {
            throw new NotFoundException($"Admin user {request.UserId} not found");
        }

        admin.IsActive = false;
        admin.UpdatedAt = DateTime.UtcNow;
        // Note: AdminUser doesn't have Notes property, we'd need to add a SuspensionReason
        // For now, just update status

        await _adminUserRepository.UpdateAsync(admin, cancellationToken);

        _logger.LogInformation("Admin user {UserId} suspended", request.UserId);

        return Unit.Value;
    }
}

/// <summary>
/// Handler para reactivar admin
/// </summary>
public class ReactivateAdminUserCommandHandler : IRequestHandler<ReactivateAdminUserCommand, Unit>
{
    private readonly IAdminUserRepository _adminUserRepository;
    private readonly ILogger<ReactivateAdminUserCommandHandler> _logger;

    public ReactivateAdminUserCommandHandler(
        IAdminUserRepository adminUserRepository,
        ILogger<ReactivateAdminUserCommandHandler> logger)
    {
        _adminUserRepository = adminUserRepository;
        _logger = logger;
    }

    public async Task<Unit> Handle(ReactivateAdminUserCommand request, CancellationToken cancellationToken)
    {
        var admin = await _adminUserRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (admin == null)
        {
            throw new NotFoundException($"Admin user {request.UserId} not found");
        }

        admin.IsActive = true;
        admin.UpdatedAt = DateTime.UtcNow;

        await _adminUserRepository.UpdateAsync(admin, cancellationToken);

        _logger.LogInformation("Admin user {UserId} reactivated", request.UserId);

        return Unit.Value;
    }
}

/// <summary>
/// Handler para obtener activity log
/// </summary>
public class GetAdminActivityQueryHandler : IRequestHandler<GetAdminActivityQuery, PaginatedResult<AdminActivityDto>>
{
    private readonly IAdminActionLogRepository _actionLogRepository;
    private readonly ILogger<GetAdminActivityQueryHandler> _logger;

    public GetAdminActivityQueryHandler(
        IAdminActionLogRepository actionLogRepository,
        ILogger<GetAdminActivityQueryHandler> logger)
    {
        _actionLogRepository = actionLogRepository;
        _logger = logger;
    }

    public async Task<PaginatedResult<AdminActivityDto>> Handle(GetAdminActivityQuery request, CancellationToken cancellationToken)
    {
        var (logs, totalCount) = await _actionLogRepository.GetAllAsync(
            adminId: request.AdminId,
            action: request.Action,
            from: request.From ?? DateTime.UtcNow.AddMonths(-1),
            to: request.To ?? DateTime.UtcNow,
            page: request.Page,
            pageSize: request.PageSize
        );

        var dtos = logs.Select(l => new AdminActivityDto
        {
            Id = l.Id,
            AdminId = l.AdminId,
            AdminName = l.AdminName ?? "System",
            Action = l.Action,
            Description = l.Description ?? string.Empty,
            TargetType = l.TargetType,
            TargetId = l.TargetId,
            Timestamp = l.Timestamp,
            IpAddress = l.IpAddress
        }).ToList();

        return new PaginatedResult<AdminActivityDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}

/// <summary>
/// Handler para obtener settings
/// </summary>
public class GetPlatformSettingsQueryHandler : IRequestHandler<GetPlatformSettingsQuery, PlatformSettingsDto>
{
    private readonly IStatisticsRepository _statsRepository;
    private readonly ILogger<GetPlatformSettingsQueryHandler> _logger;

    public GetPlatformSettingsQueryHandler(
        IStatisticsRepository statsRepository,
        ILogger<GetPlatformSettingsQueryHandler> logger)
    {
        _statsRepository = statsRepository;
        _logger = logger;
    }

    public async Task<PlatformSettingsDto> Handle(GetPlatformSettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = await _statsRepository.GetPlatformSettingsAsync();

        return new PlatformSettingsDto
        {
            MaintenanceMode = settings.MaintenanceMode,
            MaintenanceMessage = settings.MaintenanceMessage,
            RegistrationEnabled = settings.RegistrationEnabled,
            DealerRegistrationEnabled = settings.DealerRegistrationEnabled,
            EarlyBirdActive = settings.EarlyBirdActive,
            EarlyBirdEndDate = settings.EarlyBirdEndDate,
            EarlyBirdDiscount = settings.EarlyBirdDiscount,
            FeatureFlags = settings.FeatureFlags ?? new Dictionary<string, object>(),
            Limits = settings.Limits ?? new Dictionary<string, object>()
        };
    }
}

/// <summary>
/// Handler para actualizar settings
/// </summary>
public class UpdatePlatformSettingsCommandHandler : IRequestHandler<UpdatePlatformSettingsCommand, Unit>
{
    private readonly IStatisticsRepository _statsRepository;
    private readonly ILogger<UpdatePlatformSettingsCommandHandler> _logger;

    public UpdatePlatformSettingsCommandHandler(
        IStatisticsRepository statsRepository,
        ILogger<UpdatePlatformSettingsCommandHandler> logger)
    {
        _statsRepository = statsRepository;
        _logger = logger;
    }

    public async Task<Unit> Handle(UpdatePlatformSettingsCommand request, CancellationToken cancellationToken)
    {
        await _statsRepository.UpdatePlatformSettingsAsync(request.Settings);

        _logger.LogInformation("Platform settings updated");

        return Unit.Value;
    }
}
