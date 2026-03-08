using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using ReviewService.Application.Features.Reviews.Commands;
using ReviewService.Application.Features.Reviews.Queries;
using ReviewService.Application.DTOs;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace ReviewService.Api.Controllers;

/// <summary>
/// API para gestión de reviews de vendedores/dealers
/// Sprint 14: Sistema básico + Sprint 15: Votos, Badges, Solicitudes automáticas
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ReviewsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ReviewsController> _logger;

    public ReviewsController(IMediator mediator, ILogger<ReviewsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Obtener reviews de un vendedor con paginación y filtros
    /// </summary>
    /// <param name="sellerId">ID del vendedor</param>
    /// <param name="page">Número de página</param>
    /// <param name="pageSize">Tamaño de página</param>
    /// <param name="rating">Filtrar por rating específico</param>
    /// <param name="onlyVerified">Solo compras verificadas</param>
    /// <returns>Lista paginada de reviews</returns>
    [HttpGet("seller/{sellerId:guid}")]
    [ProducesResponseType(typeof(PagedReviewsDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<PagedReviewsDto>> GetSellerReviews(
        [FromRoute] Guid sellerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? rating = null,
        [FromQuery] bool? onlyVerified = null)
    {
        try
        {
            var filters = new ReviewFiltersDto
            {
                Rating = rating,
                OnlyVerified = onlyVerified
            };

            var query = new GetSellerReviewsQuery
            {
                SellerId = sellerId,
                Page = page,
                PageSize = pageSize,
                Filters = filters,
                OnlyApproved = true
            };

            var result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reviews for seller {SellerId}", sellerId);
            return StatusCode(500, "Error interno del servidor");
        }
    }

    /// <summary>
    /// Obtener estadísticas de reviews de un vendedor
    /// </summary>
    /// <param name="sellerId">ID del vendedor</param>
    /// <returns>Estadísticas agregadas</returns>
    [HttpGet("seller/{sellerId:guid}/summary")]
    [ProducesResponseType(typeof(ReviewSummaryDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<ReviewSummaryDto>> GetReviewSummary([FromRoute] Guid sellerId)
    {
        try
        {
            var query = new GetReviewSummaryQuery { SellerId = sellerId };
            var result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            return NotFound(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting review summary for seller {SellerId}", sellerId);
            return StatusCode(500, "Error interno del servidor");
        }
    }

    /// <summary>
    /// Obtener una review específica por ID
    /// </summary>
    /// <param name="reviewId">ID de la review</param>
    /// <returns>Datos de la review</returns>
    [HttpGet("{reviewId:guid}")]
    [ProducesResponseType(typeof(ReviewDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<ReviewDto>> GetReview([FromRoute] Guid reviewId)
    {
        try
        {
            var query = new GetReviewByIdQuery { ReviewId = reviewId };
            var result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            return NotFound(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting review {ReviewId}", reviewId);
            return StatusCode(500, "Error interno del servidor");
        }
    }

    /// <summary>
    /// Crear una nueva review (requiere autenticación)
    /// </summary>
    /// <param name="dto">Datos de la review</param>
    /// <returns>Review creada</returns>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ReviewDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<ReviewDto>> CreateReview([FromBody] CreateReviewDto dto)
    {
        try
        {
            // Obtener información del usuario autenticado
            var buyerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var buyerNameClaim = User.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(buyerIdClaim) || !Guid.TryParse(buyerIdClaim, out var buyerId))
            {
                return Unauthorized("Token inválido");
            }

            var command = new CreateReviewCommand
            {
                BuyerId = buyerId,
                SellerId = dto.SellerId,
                VehicleId = dto.VehicleId,
                OrderId = dto.OrderId,
                Rating = dto.Rating,
                Title = dto.Title,
                Content = dto.Content,
                BuyerName = buyerNameClaim ?? "Usuario Anónimo",
                BuyerPhotoUrl = null // Se puede obtener del UserService si está disponible
            };

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return CreatedAtAction(nameof(GetReview), new { reviewId = result.Value!.Id }, result.Value);
            }

            return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating review");
            return StatusCode(500, "Error interno del servidor");
        }
    }

    /// <summary>
    /// Actualizar una review existente (solo el autor)
    /// </summary>
    /// <param name="reviewId">ID de la review</param>
    /// <param name="dto">Nuevos datos</param>
    /// <returns>Review actualizada</returns>
    [HttpPut("{reviewId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ReviewDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<ReviewDto>> UpdateReview([FromRoute] Guid reviewId, [FromBody] UpdateReviewDto dto)
    {
        try
        {
            var buyerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(buyerIdClaim) || !Guid.TryParse(buyerIdClaim, out var buyerId))
            {
                return Unauthorized("Token inválido");
            }

            var command = new UpdateReviewCommand
            {
                ReviewId = reviewId,
                BuyerId = buyerId,
                Rating = dto.Rating,
                Title = dto.Title,
                Content = dto.Content
            };

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            // Si el error contiene "forbidden" o "unauthorized", retornar 403
            if (result.Error.Contains("no autorizado") || result.Error.Contains("forbidden"))
            {
                // NOTE: Forbid(string) sets auth scheme, not error body — use StatusCode instead
                return StatusCode(403, new { error = result.Error });
            }

            return BadRequest(new { error = result.Error });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting review {ReviewId}", reviewId);
            return StatusCode(500, new { error = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Eliminar una review (solo el autor)
    /// </summary>
    /// <param name="reviewId">ID de la review</param>
    /// <returns>Confirmación</returns>
    [HttpDelete("{reviewId:guid}")]
    [Authorize]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult> DeleteReview([FromRoute] Guid reviewId)
    {
        try
        {
            var buyerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(buyerIdClaim) || !Guid.TryParse(buyerIdClaim, out var buyerId))
            {
                return Unauthorized("Token inválido");
            }

            var command = new DeleteReviewCommand
            {
                ReviewId = reviewId,
                BuyerId = buyerId
            };

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return Ok(new { message = "Review eliminada exitosamente" });
            }

            if (result.Error.Contains("no autorizado") || result.Error.Contains("forbidden"))
            {
                return StatusCode(403, new { error = result.Error });
            }

            return BadRequest(new { error = result.Error });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting review {ReviewId}", reviewId);
            return StatusCode(500, new { error = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Moderar una review (solo admins)
    /// </summary>
    /// <param name="reviewId">ID de la review</param>
    /// <param name="isApproved">Aprobar o rechazar</param>
    /// <param name="rejectionReason">Razón de rechazo (opcional)</param>
    /// <returns>Confirmación</returns>
    [HttpPost("{reviewId:guid}/moderate")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult> ModerateReview(
        [FromRoute] Guid reviewId,
        [FromQuery] bool isApproved,
        [FromQuery] string? rejectionReason = null)
    {
        try
        {
            var moderatorIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(moderatorIdClaim) || !Guid.TryParse(moderatorIdClaim, out var moderatorId))
            {
                return Unauthorized("Token inválido");
            }

            var command = new ModerateReviewCommand
            {
                ReviewId = reviewId,
                ModeratorId = moderatorId,
                IsApproved = isApproved,
                RejectionReason = rejectionReason
            };

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                var status = isApproved ? "aprobada" : "rechazada";
                return Ok(new { message = $"Review {status} exitosamente" });
            }

            return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moderating review {ReviewId}", reviewId);
            return StatusCode(500, "Error interno del servidor");
        }
    }

    /// <summary>
    /// Sprint 15 - Vendedor responde a una review
    /// </summary>
    /// <param name="reviewId">ID de la review</param>
    /// <param name="request">Respuesta del vendedor</param>
    /// <returns>Confirmación</returns>
    [HttpPost("{reviewId:guid}/respond")]
    [Authorize]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> RespondToReview(
        [FromRoute] Guid reviewId,
        [FromBody] SellerResponseRequest request)
    {
        try
        {
            var sellerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(sellerIdClaim) || !Guid.TryParse(sellerIdClaim, out var sellerId))
            {
                return Unauthorized("Token inválido");
            }

            var command = new RespondToReviewCommand(reviewId, sellerId, request.ResponseText);
            await _mediator.Send(command);

            _logger.LogInformation("Seller {SellerId} responded to review {ReviewId}", sellerId, reviewId);

            return Ok(new { message = "Respuesta publicada exitosamente" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            // NOTE: Forbid(string) sets auth scheme, not error body — use StatusCode instead
            return StatusCode(403, new { error = "No autorizado para realizar esta acción" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error responding to review {ReviewId}", reviewId);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    #region Sprint 15 - Votos de Utilidad

    /// <summary>
    /// Sprint 15 - Votar si una review es útil
    /// </summary>
    /// <param name="reviewId">ID de la review</param>
    /// <param name="dto">Datos del voto</param>
    /// <returns>Estadísticas actualizadas</returns>
    [HttpPost("{reviewId:guid}/vote")]
    [Authorize]
    [ProducesResponseType(typeof(VoteResultDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<VoteResultDto>> VoteHelpful(
        [FromRoute] Guid reviewId,
        [FromBody] VoteHelpfulDto dto)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized("Token inválido");
            }

            var command = new VoteHelpfulCommand
            {
                ReviewId = reviewId,
                UserId = userId,
                IsHelpful = dto.IsHelpful,
                UserIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString()
            };

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            return BadRequest(new { message = result.Error });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error voting on review {ReviewId}", reviewId);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Sprint 15 - Obtener estadísticas de votos de una review
    /// </summary>
    /// <param name="reviewId">ID de la review</param>
    /// <returns>Estadísticas de votos</returns>
    [HttpGet("{reviewId:guid}/vote-stats")]
    [ProducesResponseType(typeof(ReviewVoteStatsDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<ReviewVoteStatsDto>> GetVoteStats([FromRoute] Guid reviewId)
    {
        try
        {
            // Obtener usuario actual si está autenticado
            Guid? currentUserId = null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
            {
                currentUserId = userId;
            }

            var query = new GetReviewVoteStatsQuery
            {
                ReviewId = reviewId,
                CurrentUserId = currentUserId
            };

            var result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            return NotFound(new { message = result.Error });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vote stats for review {ReviewId}", reviewId);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    #endregion

    #region Sprint 15 - Badges de Vendedor

    /// <summary>
    /// Sprint 15 - Obtener badges de un vendedor
    /// </summary>
    /// <param name="sellerId">ID del vendedor</param>
    /// <returns>Lista de badges activos</returns>
    [HttpGet("seller/{sellerId:guid}/badges")]
    [ProducesResponseType(typeof(List<SellerBadgeDto>), 200)]
    public async Task<ActionResult<List<SellerBadgeDto>>> GetSellerBadges([FromRoute] Guid sellerId)
    {
        try
        {
            var query = new GetSellerBadgesQuery
            {
                SellerId = sellerId,
                OnlyActive = true
            };

            var result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            return BadRequest(new { message = result.Error });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting badges for seller {SellerId}", sellerId);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Sprint 15 - Recalcular badges de un vendedor (admin only)
    /// </summary>
    /// <param name="sellerId">ID del vendedor</param>
    /// <returns>Badges actualizados</returns>
    [HttpPost("seller/{sellerId:guid}/badges/recalculate")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(BadgeUpdateResultDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<BadgeUpdateResultDto>> RecalculateBadges([FromRoute] Guid sellerId)
    {
        try
        {
            var command = new UpdateBadgesCommand { SellerId = sellerId };
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            return BadRequest(new { message = result.Error });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating badges for seller {SellerId}", sellerId);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    #endregion

    #region Buyer Reviews & Moderation

    /// <summary>
    /// Obtener reviews escritas por un comprador (historial del comprador)
    /// </summary>
    /// <param name="buyerId">ID del comprador</param>
    /// <param name="page">Número de página</param>
    /// <param name="pageSize">Tamaño de página</param>
    [HttpGet("buyer/{buyerId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(PagedReviewsDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<PagedReviewsDto>> GetBuyerReviews(
        [FromRoute] Guid buyerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
            {
                if (userId != buyerId && !User.IsInRole("Admin"))
                {
                    return StatusCode(403, new { error = "No tiene permiso para ver estas reviews" });
                }
            }

            var query = new GetBuyerReviewsQuery { BuyerId = buyerId, Page = page, PageSize = pageSize };
            var result = await _mediator.Send(query);

            if (result.IsSuccess)
                return Ok(result.Value);

            return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reviews for buyer {BuyerId}", buyerId);
            return StatusCode(500, "Error interno del servidor");
        }
    }

    /// <summary>
    /// Reportar una review para moderación
    /// </summary>
    /// <param name="reviewId">ID de la review a reportar</param>
    /// <param name="request">Razón del reporte</param>
    [HttpPost("{reviewId:guid}/report")]
    [Authorize]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> ReportReview(
        [FromRoute] Guid reviewId,
        [FromBody] ReportReviewRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized("Token inválido");

            var command = new ReportReviewCommand
            {
                ReviewId = reviewId,
                ReportedByUserId = userId,
                Reason = request.Reason
            };

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(new { message = "Review reportada. Será revisada por nuestro equipo." });

            return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reporting review {ReviewId}", reviewId);
            return StatusCode(500, "Error interno del servidor");
        }
    }

    #endregion

    #region Sprint 15 - Solicitudes Automáticas de Review

    /// <summary>
    /// Sprint 15 - Enviar solicitud de review post-compra
    /// </summary>
    /// <param name="dto">Datos de la solicitud</param>
    /// <returns>Resultado de la solicitud</returns>
    [HttpPost("requests")]
    [Authorize(Roles = "Admin,System")]
    [ProducesResponseType(typeof(ReviewRequestResultDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<ReviewRequestResultDto>> SendReviewRequest([FromBody] CreateReviewRequestDto dto)
    {
        try
        {
            var command = new SendReviewRequestCommand
            {
                BuyerId = dto.BuyerId,
                SellerId = dto.SellerId,
                VehicleId = dto.VehicleId,
                OrderId = dto.OrderId,
                BuyerEmail = dto.BuyerEmail,
                BuyerName = dto.BuyerName,
                VehicleTitle = dto.VehicleTitle,
                SellerName = dto.SellerName,
                PurchaseDate = dto.PurchaseDate
            };

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                return CreatedAtAction(nameof(GetPendingRequests), new { buyerId = dto.BuyerId }, result.Value);
            }

            return BadRequest(new { message = result.Error });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending review request for order {OrderId}", dto.OrderId);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Sprint 15 - Obtener solicitudes de review pendientes para un comprador
    /// </summary>
    /// <param name="buyerId">ID del comprador</param>
    /// <returns>Lista de solicitudes pendientes</returns>
    [HttpGet("requests/buyer/{buyerId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(List<ReviewRequestDto>), 200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<List<ReviewRequestDto>>> GetPendingRequests([FromRoute] Guid buyerId)
    {
        try
        {
            // Verificar que el usuario autenticado es el mismo que busca
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
            {
                if (userId != buyerId && !User.IsInRole("Admin"))
                {
                    return StatusCode(403, new { error = "No tiene permiso para ver estas solicitudes" });
                }
            }

            var query = new GetPendingReviewRequestsQuery { BuyerId = buyerId };
            var result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            return BadRequest(new { message = result.Error });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending review requests for buyer {BuyerId}", buyerId);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Sprint 15 - Obtener mis solicitudes de review pendientes
    /// </summary>
    /// <returns>Lista de solicitudes pendientes del usuario actual</returns>
    [HttpGet("requests/mine")]
    [Authorize]
    [ProducesResponseType(typeof(List<ReviewRequestDto>), 200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<List<ReviewRequestDto>>> GetMyPendingRequests()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized("Token inválido");
            }

            var query = new GetPendingReviewRequestsQuery { BuyerId = userId };
            var result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            return BadRequest(new { message = result.Error });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending review requests");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    #endregion

    #region Admin endpoints

    /// <summary>
    /// Obtener todas las reviews con paginación y filtros (admin)
    /// </summary>
    [HttpGet("admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(AdminReviewListDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<AdminReviewListDto>> GetAdminReviews(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null)
    {
        try
        {
            var query = new GetAdminReviewsQuery { Page = page, PageSize = pageSize, Search = search, Status = status };
            var result = await _mediator.Send(query);
            if (result.IsSuccess)
                return Ok(result.Value);
            return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting admin reviews");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener reviews reportadas/flagged (admin)
    /// </summary>
    [HttpGet("admin/reported")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(List<ReportedAdminReviewDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<List<ReportedAdminReviewDto>>> GetReportedReviews()
    {
        try
        {
            var query = new GetAdminReviewsQuery { Page = 1, PageSize = 200, Status = "reported" };
            var result = await _mediator.Send(query);
            if (!result.IsSuccess)
                return BadRequest(result.Error);

            var reported = result.Value!.Items.Select(r => new ReportedAdminReviewDto
            {
                Id           = r.Id,
                AuthorName   = r.AuthorName,
                AuthorAvatar = r.AuthorAvatar,
                TargetName   = r.TargetName,
                TargetType   = r.TargetType,
                Rating       = r.Rating,
                Title        = r.Title,
                Comment      = r.Comment,
                CreatedAt    = r.CreatedAt,
                Status       = r.Status,
                Reports      = r.Reports,
                ReportCount  = 1,
                ReportReasons = Array.Empty<string>(),
                LastReportedAt = r.CreatedAt
            }).ToList();

            return Ok(reported);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reported reviews");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener estadísticas globales de reviews (admin)
    /// </summary>
    [HttpGet("admin/stats")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(AdminReviewStatsDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<AdminReviewStatsDto>> GetAdminReviewStats()
    {
        try
        {
            var result = await _mediator.Send(new GetAdminReviewStatsQuery());
            if (result.IsSuccess)
                return Ok(result.Value);
            return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting admin review stats");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Eliminar una review sin validación de propiedad (admin)
    /// </summary>
    [HttpDelete("admin/{reviewId:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> AdminDeleteReview([FromRoute] Guid reviewId)
    {
        try
        {
            var result = await _mediator.Send(new AdminDeleteReviewCommand { ReviewId = reviewId });
            if (result.IsSuccess)
                return Ok(new { message = "Review eliminada exitosamente" });
            return NotFound(new { message = result.Error });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting review {ReviewId} (admin)", reviewId);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    #endregion
}

/// <summary>
/// Request para respuesta de vendedor
/// </summary>
public record SellerResponseRequest(string ResponseText);

/// <summary>
/// Request para reportar una review
/// </summary>
public record ReportReviewRequest(string Reason);