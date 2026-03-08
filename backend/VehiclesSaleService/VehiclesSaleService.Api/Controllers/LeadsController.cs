using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehiclesSaleService.Domain.Entities;
using VehiclesSaleService.Infrastructure.Persistence;
using Microsoft.AspNetCore.RateLimiting;

namespace VehiclesSaleService.Api.Controllers;

/// <summary>
/// Gestión de leads (mensajes de compradores a vendedores).
/// Los sellers y dealers pueden ver, responder y gestionar sus leads.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("VehiclesPolicy")]
public class LeadsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<LeadsController> _logger;

    public LeadsController(ApplicationDbContext db, ILogger<LeadsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Crear un lead — un comprador contacta al vendedor de un vehículo.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LeadResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateLead([FromBody] CreateLeadRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.BuyerName) || string.IsNullOrWhiteSpace(request.BuyerEmail))
            return BadRequest(new { error = "Nombre y email del comprador son requeridos." });

        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { error = "El mensaje es requerido." });

        // Security: Sanitize all string inputs (this endpoint is anonymous and bypasses MediatR/ValidationBehavior)
        if (ContainsSuspiciousInput(request.BuyerName) || ContainsSuspiciousInput(request.BuyerEmail) ||
            ContainsSuspiciousInput(request.Message) || ContainsSuspiciousInput(request.BuyerPhone))
            return BadRequest(new { error = "La solicitud contiene caracteres no permitidos." });

        var vehicle = await _db.Vehicles
            .AsNoTracking()
            .Where(v => v.Id == request.VehicleId && !v.IsDeleted)
            .Select(v => new { v.Id, v.Title, v.Price, v.SellerId, v.DealerId, Image = v.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault() })
            .FirstOrDefaultAsync(ct);

        if (vehicle == null)
            return NotFound(new { error = "Vehículo no encontrado." });

        var buyerId = GetUserId();

        var lead = new Lead
        {
            VehicleId = vehicle.Id,
            SellerId = vehicle.SellerId,
            DealerId = vehicle.DealerId,
            BuyerId = buyerId != Guid.Empty ? buyerId : null,
            BuyerName = request.BuyerName.Trim(),
            BuyerEmail = request.BuyerEmail.Trim().ToLowerInvariant(),
            BuyerPhone = request.BuyerPhone?.Trim(),
            Message = request.Message.Trim(),
            VehicleTitle = vehicle.Title,
            VehiclePrice = vehicle.Price,
            VehicleImageUrl = vehicle.Image,
            Source = LeadSource.ContactForm,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()
        };

        // Add initial message
        lead.Messages.Add(new LeadMessage
        {
            LeadId = lead.Id,
            SenderId = buyerId != Guid.Empty ? buyerId : Guid.Empty,
            SenderName = request.BuyerName.Trim(),
            SenderRole = MessageSenderRole.Buyer,
            Content = request.Message.Trim()
        });

        _db.Set<Lead>().Add(lead);

        // Increment inquiry count on vehicle
        var vehicleEntity = await _db.Vehicles.FindAsync(new object[] { vehicle.Id }, ct);
        if (vehicleEntity != null)
        {
            vehicleEntity.InquiryCount = vehicleEntity.InquiryCount + 1;
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Lead {LeadId} created for vehicle {VehicleId} by {BuyerEmail}",
            lead.Id, vehicle.Id, lead.BuyerEmail);

        return CreatedAtAction(nameof(GetLead), new { id = lead.Id }, MapToDto(lead));
    }

    /// <summary>
    /// Obtener un lead por ID (solo el seller propietario).
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(LeadDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLead(Guid id, CancellationToken ct)
    {
        var lead = await _db.Set<Lead>()
            .Include(l => l.Messages.OrderBy(m => m.CreatedAt))
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id, ct);

        if (lead == null)
            return NotFound(new { error = "Lead no encontrado." });

        // Authorization: only the seller can see their leads
        var userId = GetUserId();
        if (lead.SellerId != userId)
            return Forbid();

        return Ok(MapToDetailDto(lead));
    }

    /// <summary>
    /// Listar leads del seller autenticado.
    /// </summary>
    [HttpGet("seller/{sellerId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(LeadListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSellerLeads(
        Guid sellerId,
        [FromQuery] LeadStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (sellerId != userId)
            return Forbid();

        var query = _db.Set<Lead>()
            .AsNoTracking()
            .Where(l => l.SellerId == sellerId);

        if (status.HasValue)
            query = query.Where(l => l.Status == status.Value);

        var total = await query.CountAsync(ct);
        var leads = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Ok(new LeadListResponse
        {
            Items = leads.Select(MapToDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// Listar leads de un dealer.
    /// </summary>
    [HttpGet("dealer/{dealerId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(LeadListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDealerLeads(
        Guid dealerId,
        [FromQuery] LeadStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = _db.Set<Lead>()
            .AsNoTracking()
            .Where(l => l.DealerId == dealerId);

        if (status.HasValue)
            query = query.Where(l => l.Status == status.Value);

        var total = await query.CountAsync(ct);
        var leads = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Ok(new LeadListResponse
        {
            Items = leads.Select(MapToDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// Responder a un lead (el seller envía un mensaje al comprador).
    /// </summary>
    [HttpPost("{id:guid}/reply")]
    [Authorize]
    [ProducesResponseType(typeof(LeadMessageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReplyToLead(Guid id, [FromBody] ReplyLeadRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { error = "El mensaje es requerido." });

        var lead = await _db.Set<Lead>().FindAsync(new object[] { id }, ct);
        if (lead == null)
            return NotFound(new { error = "Lead no encontrado." });

        var userId = GetUserId();
        if (lead.SellerId != userId)
            return Forbid();

        var message = new LeadMessage
        {
            LeadId = lead.Id,
            SenderId = userId,
            SenderName = User.FindFirst("name")?.Value ?? "Seller",
            SenderRole = MessageSenderRole.Seller,
            Content = request.Message.Trim()
        };

        _db.Set<LeadMessage>().Add(message);

        // Update lead status if it's new
        if (lead.Status == LeadStatus.New)
        {
            lead.Status = LeadStatus.Contacted;
            lead.ContactedAt = DateTime.UtcNow;
        }
        lead.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Seller {SellerId} replied to lead {LeadId}", userId, id);

        return Ok(new LeadMessageDto
        {
            Id = message.Id,
            SenderId = message.SenderId,
            SenderName = message.SenderName,
            SenderRole = message.SenderRole.ToString(),
            Content = message.Content,
            CreatedAt = message.CreatedAt
        });
    }

    /// <summary>
    /// Obtener mensajes de un lead.
    /// </summary>
    [HttpGet("{id:guid}/messages")]
    [Authorize]
    [ProducesResponseType(typeof(List<LeadMessageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeadMessages(Guid id, CancellationToken ct)
    {
        var lead = await _db.Set<Lead>().AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, ct);
        if (lead == null)
            return NotFound(new { error = "Lead no encontrado." });

        var userId = GetUserId();
        if (lead.SellerId != userId)
            return Forbid();

        var messages = await _db.Set<LeadMessage>()
            .AsNoTracking()
            .Where(m => m.LeadId == id)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new LeadMessageDto
            {
                Id = m.Id,
                SenderId = m.SenderId,
                SenderName = m.SenderName,
                SenderRole = m.SenderRole.ToString(),
                Content = m.Content,
                IsRead = m.IsRead,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(messages);
    }

    /// <summary>
    /// Actualizar estado de un lead.
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateLeadStatus(Guid id, [FromBody] UpdateLeadStatusRequest request, CancellationToken ct)
    {
        var lead = await _db.Set<Lead>().FindAsync(new object[] { id }, ct);
        if (lead == null)
            return NotFound(new { error = "Lead no encontrado." });

        var userId = GetUserId();
        if (lead.SellerId != userId)
            return Forbid();

        lead.Status = request.Status;
        lead.UpdatedAt = DateTime.UtcNow;

        if (request.Status == LeadStatus.Closed || request.Status == LeadStatus.Lost)
            lead.ClosedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Ok(new { success = true, status = lead.Status.ToString() });
    }

    /// <summary>
    /// Estadísticas de leads para un seller.
    /// </summary>
    [HttpGet("seller/{sellerId:guid}/stats")]
    [Authorize]
    [ProducesResponseType(typeof(LeadStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeadStats(Guid sellerId, CancellationToken ct)
    {
        var userId = GetUserId();
        if (sellerId != userId)
            return Forbid();

        var stats = await _db.Set<Lead>()
            .Where(l => l.SellerId == sellerId)
            .GroupBy(_ => 1)
            .Select(g => new LeadStatsDto
            {
                TotalLeads = g.Count(),
                NewLeads = g.Count(l => l.Status == LeadStatus.New),
                ContactedLeads = g.Count(l => l.Status == LeadStatus.Contacted),
                NegotiatingLeads = g.Count(l => l.Status == LeadStatus.Negotiating),
                ClosedLeads = g.Count(l => l.Status == LeadStatus.Closed),
                LostLeads = g.Count(l => l.Status == LeadStatus.Lost)
            })
            .FirstOrDefaultAsync(ct) ?? new LeadStatsDto();

        return Ok(stats);
    }

    // === Helpers ===

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    /// <summary>
    /// Basic XSS/SQLi detection for anonymous endpoints that bypass MediatR ValidationBehavior.
    /// </summary>
    private static bool ContainsSuspiciousInput(string? input)
    {
        if (string.IsNullOrEmpty(input)) return false;
        var lower = input.ToLowerInvariant();
        return lower.Contains("<script") || lower.Contains("javascript:") ||
               lower.Contains("onerror=") || lower.Contains("onload=") ||
               lower.Contains("'; drop") || lower.Contains("1=1") ||
               lower.Contains("union select") || lower.Contains("--") && lower.Contains("'");
    }

    private static LeadResponseDto MapToDto(Lead lead) => new()
    {
        Id = lead.Id,
        VehicleId = lead.VehicleId,
        VehicleTitle = lead.VehicleTitle,
        VehiclePrice = lead.VehiclePrice,
        VehicleImageUrl = lead.VehicleImageUrl,
        BuyerName = lead.BuyerName,
        BuyerEmail = lead.BuyerEmail,
        BuyerPhone = lead.BuyerPhone,
        Message = lead.Message,
        Status = lead.Status.ToString(),
        Source = lead.Source.ToString(),
        CreatedAt = lead.CreatedAt,
        ContactedAt = lead.ContactedAt
    };

    private static LeadDetailDto MapToDetailDto(Lead lead) => new()
    {
        Id = lead.Id,
        VehicleId = lead.VehicleId,
        VehicleTitle = lead.VehicleTitle,
        VehiclePrice = lead.VehiclePrice,
        VehicleImageUrl = lead.VehicleImageUrl,
        BuyerName = lead.BuyerName,
        BuyerEmail = lead.BuyerEmail,
        BuyerPhone = lead.BuyerPhone,
        Message = lead.Message,
        Status = lead.Status.ToString(),
        Source = lead.Source.ToString(),
        CreatedAt = lead.CreatedAt,
        ContactedAt = lead.ContactedAt,
        Messages = lead.Messages.Select(m => new LeadMessageDto
        {
            Id = m.Id,
            SenderId = m.SenderId,
            SenderName = m.SenderName,
            SenderRole = m.SenderRole.ToString(),
            Content = m.Content,
            IsRead = m.IsRead,
            CreatedAt = m.CreatedAt
        }).ToList()
    };
}

// === DTOs ===

public class CreateLeadRequest
{
    public Guid VehicleId { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string BuyerEmail { get; set; } = string.Empty;
    public string? BuyerPhone { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ReplyLeadRequest
{
    public string Message { get; set; } = string.Empty;
}

public class UpdateLeadStatusRequest
{
    public LeadStatus Status { get; set; }
}

public class LeadResponseDto
{
    public Guid Id { get; set; }
    public Guid VehicleId { get; set; }
    public string VehicleTitle { get; set; } = string.Empty;
    public decimal? VehiclePrice { get; set; }
    public string? VehicleImageUrl { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string BuyerEmail { get; set; } = string.Empty;
    public string? BuyerPhone { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ContactedAt { get; set; }
}

public class LeadDetailDto : LeadResponseDto
{
    public List<LeadMessageDto> Messages { get; set; } = new();
}

public class LeadMessageDto
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string SenderRole { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class LeadStatsDto
{
    public int TotalLeads { get; set; }
    public int NewLeads { get; set; }
    public int ContactedLeads { get; set; }
    public int NegotiatingLeads { get; set; }
    public int ClosedLeads { get; set; }
    public int LostLeads { get; set; }
}

public class LeadListResponse
{
    public List<LeadResponseDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
