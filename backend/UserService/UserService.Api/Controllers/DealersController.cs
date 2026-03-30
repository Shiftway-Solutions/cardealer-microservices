using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;
using UserService.Application.UseCases.Dealers.CreateDealer;
using UserService.Application.UseCases.Dealers.GetDealer;
using UserService.Application.UseCases.Dealers.UpdateDealer;
using System.Security.Claims;
using System.Text.Json;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;

namespace UserService.Api.Controllers;

/// <summary>
/// Controller for dealer (company) registration and management.
/// Dealers must be approved by an admin before they can list vehicles.
/// </summary>
[ApiController]
[Route("api/dealers")]
public class DealersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;
    private readonly IDealerRepository _dealerRepository;
    private static readonly JsonSerializerOptions SettingsJsonOptions = new(JsonSerializerDefaults.Web);

    public DealersController(IMediator mediator, IConfiguration configuration, IDealerRepository dealerRepository)
    {
        _mediator = mediator;
        _configuration = configuration;
        _dealerRepository = dealerRepository;
    }

    /// <summary>
    /// Get list of active dealers with pagination and search.
    /// Public endpoint for the dealer directory page.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ResponseCache(Duration = 120)]
    [ProducesResponseType(typeof(PublicDealerListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListDealers(
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? province = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        var query = new ListPublicDealersQuery(searchTerm, province, page, pageSize);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Register a new dealer (company). Status will be Pending until admin approves.
    /// Requires authentication. The authenticated user becomes the dealer owner.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(DealerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RegisterDealer([FromBody] CreateDealerRequest request)
    {
        // Feature flag check
        var featureEnabled = _configuration.GetValue<bool>("Features:DealerRegistration", true);
        if (!featureEnabled)
        {
            return BadRequest(new ProblemDetails
            {
                Type = "https://okla.com/errors/feature-disabled",
                Title = "Feature Disabled",
                Status = 400,
                Detail = "El registro de dealers no está disponible en este momento."
            });
        }

        var userId = GetAuthenticatedUserId();
        if (userId == null)
        {
            return Unauthorized(new ProblemDetails
            {
                Type = "https://okla.com/errors/unauthorized",
                Title = "Unauthorized",
                Status = 401,
                Detail = "No se pudo identificar al usuario autenticado."
            });
        }

        // Override OwnerUserId with authenticated user
        request.OwnerUserId = userId.Value;

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.FirstOrDefault();

        try
        {
            var command = new CreateDealerCommand(
                Request: request,
                IpAddress: ipAddress,
                UserAgent: userAgent);

            var result = await _mediator.Send(command);

            return CreatedAtAction(nameof(GetDealer), new { dealerId = result.Id }, result);
        }
        catch (InvalidOperationException ex) when (ex.Message == "ALREADY_DEALER")
        {
            return BadRequest(new ProblemDetails
            {
                Type = "https://okla.com/errors/already-dealer",
                Title = "Already a Dealer",
                Status = 400,
                Detail = "Ya tienes una cuenta de dealer registrada.",
                Extensions = { ["errorCode"] = "ALREADY_DEALER" }
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://okla.com/errors/not-found",
                Title = "User Not Found",
                Status = 404,
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Get dealer by ID. Returns full dealer details for verified dealers.
    /// </summary>
    [HttpGet("{dealerId:guid}")]
    [ProducesResponseType(typeof(DealerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDealer(Guid dealerId)
    {
        try
        {
            var query = new GetDealerQuery(dealerId);
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://okla.com/errors/not-found",
                Title = "Dealer Not Found",
                Status = 404,
                Detail = $"Dealer {dealerId} no fue encontrado."
            });
        }
    }

    /// <summary>
    /// Get dealer by URL-friendly slug. Public endpoint for dealer profile pages.
    /// Route: GET /api/dealers/slug/{slug}
    /// Example: GET /api/dealers/slug/auto-plaza-santo-domingo
    /// </summary>
    [HttpGet("slug/{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DealerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDealerBySlug(string slug)
    {
        var query = new GetDealerBySlugQuery(slug);
        var result = await _mediator.Send(query);

        if (result == null)
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://okla.com/errors/not-found",
                Title = "Dealer Not Found",
                Status = 404,
                Detail = $"Dealer con slug '{slug}' no fue encontrado."
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Get all active dealers for XML sitemap generation. Returns minimal data (slug + updatedAt).
    /// Public endpoint — no auth required.
    /// </summary>
    [HttpGet("sitemap")]
    [AllowAnonymous]
    [ResponseCache(Duration = 900)] // 15 min cache — matches frontend revalidate interval
    [ProducesResponseType(typeof(IEnumerable<DealerSitemapEntry>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDealersSitemap()
    {
        var query = new GetDealersSitemapQuery();
        var items = await _mediator.Send(query);
        return Ok(new { items, total = items.Count(), generatedAt = DateTime.UtcNow });
    }

    /// <summary>
    /// Get dealer by owner user ID. Used to check if current user already has a dealer account.
    /// </summary>
    [HttpGet("owner/{userId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(DealerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDealerByOwner(Guid userId)
    {
        var query = new GetDealerByOwnerQuery(userId);
        var result = await _mediator.Send(query);

        if (result == null)
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://okla.com/errors/not-found",
                Title = "Dealer Not Found",
                Status = 404,
                Detail = "El usuario no tiene una cuenta de dealer."
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Get the dealer profile for the currently authenticated user.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(DealerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyDealer()
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var query = new GetDealerByOwnerQuery(userId.Value);
        var result = await _mediator.Send(query);

        if (result == null)
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://okla.com/errors/not-found",
                Title = "Dealer Not Found",
                Status = 404,
                Detail = "No tienes una cuenta de dealer registrada."
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Get dealer settings for the authenticated owner.
    /// </summary>
    [HttpGet("{dealerId:guid}/settings")]
    [Authorize]
    [ProducesResponseType(typeof(DealerSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDealerSettings(Guid dealerId)
    {
        var (dealer, error) = await GetOwnedDealerAsync(dealerId);
        if (error != null)
        {
            return error;
        }

        var ownedDealer = dealer!;
        return Ok(MapDealerSettings(ownedDealer));
    }

    /// <summary>
    /// Update dealer notification preferences for the authenticated owner.
    /// </summary>
    [HttpPut("{dealerId:guid}/settings/notifications")]
    [Authorize]
    [ProducesResponseType(typeof(DealerNotificationSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateNotificationSettings(
        Guid dealerId,
        [FromBody] DealerNotificationSettingsDto request)
    {
        var (dealer, error) = await GetOwnedDealerAsync(dealerId);
        if (error != null)
        {
            return error;
        }

        var ownedDealer = dealer!;
        ownedDealer.NotificationSettingsJson = JsonSerializer.Serialize(request, SettingsJsonOptions);
        await _dealerRepository.UpdateAsync(ownedDealer);

        return Ok(ReadNotificationSettings(ownedDealer));
    }

    /// <summary>
    /// Update dealer operational security preferences for the authenticated owner.
    /// </summary>
    [HttpPut("{dealerId:guid}/settings/security")]
    [Authorize]
    [ProducesResponseType(typeof(DealerSecuritySettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateSecuritySettings(
        Guid dealerId,
        [FromBody] UpdateDealerSecuritySettingsRequest request)
    {
        var (dealer, error) = await GetOwnedDealerAsync(dealerId);
        if (error != null)
        {
            return error;
        }

        var ownedDealer = dealer!;

        if (request.TwoFactorEnabled.HasValue)
        {
            return BadRequest(new ProblemDetails
            {
                Type = "https://okla.com/errors/unsupported-setting",
                Title = "Unsupported Setting",
                Status = 400,
                Detail = "La autenticación de dos factores se administra desde la seguridad general de la cuenta."
            });
        }

        var settings = ReadSecuritySettings(ownedDealer);

        if (request.SessionTimeoutMinutes.HasValue)
        {
            if (request.SessionTimeoutMinutes.Value < 5 || request.SessionTimeoutMinutes.Value > 1440)
            {
                return BadRequest(new ProblemDetails
                {
                    Type = "https://okla.com/errors/validation",
                    Title = "Validation Error",
                    Status = 400,
                    Detail = "El tiempo de expiración de sesión debe estar entre 5 y 1440 minutos."
                });
            }

            settings.SessionTimeoutMinutes = request.SessionTimeoutMinutes.Value;
        }

        ownedDealer.SecuritySettingsJson = JsonSerializer.Serialize(settings, SettingsJsonOptions);
        await _dealerRepository.UpdateAsync(ownedDealer);

        return Ok(ReadSecuritySettings(ownedDealer));
    }

    /// <summary>
    /// Get badge verification status for a dealer (public endpoint).
    /// Returns the 4 criteria evaluation for the "Dealer Verificado OKLA" badge.
    /// </summary>
    [HttpGet("{dealerId:guid}/badge-status")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DealerBadgeEvaluation), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBadgeStatus(
        Guid dealerId,
        [FromServices] IDealerBadgeEvaluator badgeEvaluator)
    {
        try
        {
            var evaluation = await badgeEvaluator.EvaluateAsync(dealerId);
            return Ok(evaluation);
        }
        catch (Exception ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://okla.com/errors/not-found",
                Title = "Dealer Not Found",
                Status = 404,
                Detail = $"Dealer {dealerId} no encontrado."
            });
        }
    }

    /// <summary>
    /// Update dealer profile. Only the owner can update their dealer.
    /// </summary>
    [HttpPut("{dealerId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(DealerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateDealer(Guid dealerId, [FromBody] UpdateDealerRequest request)
    {
        try
        {
            var command = new UpdateDealerCommand(dealerId, request);
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (Exception ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://okla.com/errors/not-found",
                Title = "Dealer Not Found",
                Status = 404,
                Detail = ex.Message
            });
        }
    }

    private async Task<(Dealer? Dealer, IActionResult? Error)> GetOwnedDealerAsync(Guid dealerId)
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
        {
            return (null, Unauthorized(new ProblemDetails
            {
                Type = "https://okla.com/errors/unauthorized",
                Title = "Unauthorized",
                Status = 401,
                Detail = "No se pudo identificar al usuario autenticado."
            }));
        }

        var dealer = await _dealerRepository.GetByIdAsync(dealerId);
        if (dealer == null)
        {
            return (null, NotFound(new ProblemDetails
            {
                Type = "https://okla.com/errors/not-found",
                Title = "Dealer Not Found",
                Status = 404,
                Detail = $"Dealer {dealerId} no fue encontrado."
            }));
        }

        if (dealer.OwnerUserId != userId.Value)
        {
            return (null, Forbid());
        }

        return (dealer, null);
    }

    private static DealerSettingsDto MapDealerSettings(Dealer dealer)
    {
        return new DealerSettingsDto
        {
            DealerId = dealer.Id,
            Notifications = ReadNotificationSettings(dealer),
            Security = ReadSecuritySettings(dealer),
            UpdatedAt = dealer.UpdatedAt ?? dealer.CreatedAt
        };
    }

    private static DealerNotificationSettingsDto ReadNotificationSettings(Dealer dealer)
    {
        if (string.IsNullOrWhiteSpace(dealer.NotificationSettingsJson))
        {
            return new DealerNotificationSettingsDto();
        }

        try
        {
            return JsonSerializer.Deserialize<DealerNotificationSettingsDto>(
                       dealer.NotificationSettingsJson,
                       SettingsJsonOptions)
                   ?? new DealerNotificationSettingsDto();
        }
        catch (JsonException)
        {
            return new DealerNotificationSettingsDto();
        }
    }

    private static DealerSecuritySettingsDto ReadSecuritySettings(Dealer dealer)
    {
        if (string.IsNullOrWhiteSpace(dealer.SecuritySettingsJson))
        {
            return new DealerSecuritySettingsDto();
        }

        try
        {
            var settings = JsonSerializer.Deserialize<DealerSecuritySettingsDto>(
                               dealer.SecuritySettingsJson,
                               SettingsJsonOptions)
                           ?? new DealerSecuritySettingsDto();

            settings.TwoFactorEnabled = false;
            if (settings.SessionTimeoutMinutes < 5)
            {
                settings.SessionTimeoutMinutes = 30;
            }

            return settings;
        }
        catch (JsonException)
        {
            return new DealerSecuritySettingsDto();
        }
    }

    private Guid? GetAuthenticatedUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("userId")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        return userId;
    }
}
